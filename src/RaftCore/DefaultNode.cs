using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RaftCore.Math;
using RaftCore.Protocol;
using RaftCore.Storage;
using RaftCore.Threading;
using RaftCore.Transport;
using Timeout = RaftCore.Logic.Timeout;

namespace RaftCore
{
    public class DefaultNode : INode
    {
        internal struct HandlerEntry
        {
            internal Func<ITransportRequest, bool> Condition;
            internal Func<NodeId, ITransportRequest,IStorageTransaction, ITransportResponse> Handler;
            internal Func<IStorageTransaction,ITransportResponse> ErrorHandler;

            internal bool IsWriting;
        }

        private readonly INodeIdProvider        nodeIdProvider;
        private readonly ITransport             transport;
        private readonly IStorage               storage;
        private readonly IStateMachine          stateMachine;
        private readonly ILogger<INode>         logger;

        private readonly ReaderWriterLockSlim   lockObject = new ReaderWriterLockSlim();
        private readonly Timeout                electionTimeout;
        private readonly Timeout                heartbeatTimeout;
        private readonly List<HandlerEntry>     handlers = new List<HandlerEntry>();
       

        private IDisposable listenSubscription;

        private long        commitIndex;
        private long        lastAppliedIndex;
        private NodeRole    currentRole = NodeRole.Follower;
        private NodeId      currentLeaderId = NodeId.Null;

        private int         receivedVoteCount = 0;

        public DefaultNode(INodeIdProvider nodeIdProvider, ITransport transport, IStorage storage, IStateMachine stateMachine, ILogger<INode> logger, TimeSpan? electionTimeout = null)
        {
            if (nodeIdProvider == null) throw new ArgumentNullException(nameof(nodeIdProvider));
            if (transport == null) throw new ArgumentNullException(nameof(transport));
            if (storage == null) throw new ArgumentNullException(nameof(storage));
            if (stateMachine == null) throw new ArgumentNullException(nameof(stateMachine));

            var actualElectionTimeout = electionTimeout ?? TimeSpan.FromMilliseconds(275.0);

            if (actualElectionTimeout.TotalMilliseconds < 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(electionTimeout),"electionTimeout must be at least 1 ms");
            }

            if (logger == null) throw new ArgumentNullException(nameof(logger));

            this.nodeIdProvider     = nodeIdProvider;
            this.transport          = transport;
            this.storage            = storage;
            this.stateMachine       = stateMachine;
            this.logger             = logger;
            this.electionTimeout    = new Timeout(actualElectionTimeout,OnElectionTimeout);
            this.heartbeatTimeout   = new Timeout(TimeSpan.FromMilliseconds(actualElectionTimeout.TotalMilliseconds / 4.0),OnHeartbeatTimeout);

            AddHandler<AppendEntriesRequest>(OnAppendEntriesReceived,transaction => new AppendEntriesResponse() { WasSuccessful = false,Term = transaction.CurrentTerm});
            AddHandler<RequestVoteRequest>(OnRequestVoteReceived,transaction => new RequestVoteResponse() { WasGranted = false,Term = transaction.CurrentTerm});
        }

        public event Action<NodeId> LeaderChanged;
        public NodeId Id => nodeIdProvider.LocalNodeId;

        public NodeRole Role
        {
            get
            {
                using (lockObject.AquireRead())
                {
                    return currentRole;
                }
            }
        }

        public NodeId LeaderId
        {
            get
            {
                using (lockObject.AquireRead())
                {
                    return currentLeaderId;
                }
            }
        }

        public Task Start()
        {
            this.currentLeaderId    = NodeId.Null;
            this.currentRole        = NodeRole.Follower;
            this.commitIndex        = 0;
            this.lastAppliedIndex   = 0;

            this.listenSubscription?.Dispose();
            this.listenSubscription = transport.Listen(OnRequest);
            electionTimeout.Start();

            return Task.CompletedTask;
        }

        public Task ApplyCommands(IEnumerable<ICommand> commands, bool forwardToLeader)
        {
            throw new NotImplementedException();
        }

        public Task<IList<object>> ExecuteQueries(IEnumerable<IQuery> queries, QueryConsistency consistency, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public void Dispose()
        {
            listenSubscription?.Dispose();
            electionTimeout.Dispose();
            heartbeatTimeout.Dispose();
            lockObject.Dispose();
        }

        private void OnElectionTimeout()
        {
            logger.LogInformation("Election timeout");

            using (lockObject.AquireWrite())
            {
                if (currentRole == NodeRole.Follower)
                {
                    ConvertToCandidate();
                }

                if (currentRole == NodeRole.Candidate)
                {
                    StartElection();
                }
            }
        }

        private void OnHeartbeatTimeout()
        {
            
        }

        private ITransportResponse OnRequest(NodeId sender,ITransportRequest request)
        {
            foreach (var handler in handlers)
            {
                if (handler.Condition.Invoke(request))
                {
                    // Take lock
                    using (handler.IsWriting ? (IDisposable)lockObject.AquireWrite() : lockObject.AquireRead())
                    {
                        using (var transaction = storage.BeginTransaction(!handler.IsWriting))
                        {
                            var response = handler.Handler.Invoke(sender, request, transaction);

                            if (handler.IsWriting)
                            {
                                try
                                {
                                    var applyCommandBuffer = new List<ICommand>();

                                    // Apply outstanding entries:
                                    while (commitIndex > lastAppliedIndex)
                                    {
                                        lastAppliedIndex++;
                                        var logEntry = transaction.GetLogEntryAt(lastAppliedIndex);

                                        if (logEntry != null)
                                        {
                                            applyCommandBuffer.Add(logEntry.Command);
                                        }
                                    }

                                    if (applyCommandBuffer.Any())
                                    {
                                        try
                                        {
                                            stateMachine.ApplyCommands(applyCommandBuffer);
                                        }
                                        catch (Exception ex)
                                        {
                                            logger.LogError("Error applying commands to state machine: {Exception}", ex);

                                            return handler.ErrorHandler.Invoke(transaction);
                                        }
                                    }

                                    // Update term based on request/response
                                    UpdateCurrentTerm(transaction,request,response);

                                    transaction.Commit();
                                }
                                catch (StorageCommitException ex)
                                {
                                    logger.LogError("Failed to commit to storage: {Exception}",ex);

                                    return handler.ErrorHandler.Invoke(transaction);
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        private ITransportResponse OnAppendEntriesReceived(NodeId sender, AppendEntriesRequest request,IStorageTransaction transaction)
        {
            var currentTerm = transaction.CurrentTerm;

            if (currentRole == NodeRole.Candidate)
            {
                electionTimeout.Stop();
                OnLeaderChanged(request.LeaderId);
                ConvertToFollower(transaction);
            }

            if (currentRole == NodeRole.Follower)
            {
                if (request.Term < currentTerm)
                {
                    return new AppendEntriesResponse() {WasSuccessful = false, Term = currentTerm};
                }

                var prevLogEntry = transaction.GetLogEntryAt(request.PreviousLogIndex);

                if (prevLogEntry != null && prevLogEntry.Term != request.PreviousLogTerm)
                {
                    return new AppendEntriesResponse() {WasSuccessful = false, Term = currentTerm};
                }

                var lastNewLogIndex = long.MaxValue;

                for (long i = 0; i < request.Entries.LongCount(); i++)
                {
                    var targetIndex = i + request.PreviousLogIndex + 1;
                    var logEntry = request.Entries.ElementAt((int)i);
                    var existingLogEntry = transaction.GetLogEntryAt(targetIndex);

                    if (existingLogEntry != null)
                    {
                        if (existingLogEntry.Term != logEntry.Term)
                        {
                            transaction.DeleteLogEntriesStartingAt(i);
                        }
                    }
                    else
                    {
                        transaction.InsertLogEntry(targetIndex,logEntry);
                        lastNewLogIndex = targetIndex;
                    }
                }

                if (request.LeaderCommitIndex > commitIndex)
                {
                    commitIndex = System.Math.Min(request.LeaderCommitIndex, lastNewLogIndex);
                }

                return new AppendEntriesResponse() {WasSuccessful = true, Term = currentTerm};
            }

            if (currentRole == NodeRole.Leader)
            {
                logger.LogWarning("Received AppendEntries, event though leader");
            }

            return new AppendEntriesResponse() {WasSuccessful = false, Term = currentTerm};
        }

        private ITransportResponse OnRequestVoteReceived(NodeId sender, RequestVoteRequest request,IStorageTransaction transaction)
        {
            var currentTerm = transaction.CurrentTerm;

            if (currentTerm >= request.Term && 
                (transaction.VotedFor.IsNull || transaction.VotedFor == request.CandidateId) &&
                IsCandidateAtLeastAsUpdateToDateAsThis(transaction,request))
            {
                logger.LogInformation("Granting vote to: {Candidate}",request.CandidateId);

                transaction.VotedFor = request.CandidateId;

                try
                {
                    transaction.Commit();
                    electionTimeout.Stop();

                    return new RequestVoteResponse() { Term = currentTerm, WasGranted = true };
                }
                catch (StorageCommitException ex)
                {
                    logger.LogError("Unable to commit vote: {Exception}", ex);
                }
            }

            logger.LogInformation("Refusing vote for: {Candidate}",request.CandidateId);
            return new RequestVoteResponse() {Term = currentTerm, WasGranted = false};    
        }

        private void OnLeaderChanged(NodeId newLeader)
        {
            
        }

        private void ConvertToCandidate()
        {
            logger.LogInformation("Converting to candidate");

            StartElection();
        }

        private void ConvertToLeader()
        {
            logger.LogInformation("Converting to leader");

            OnLeaderChanged(nodeIdProvider.LocalNodeId);
        }

        private void ConvertToFollower(IStorageTransaction transaction)
        {
            logger.LogInformation("Converting to follower");

            transaction.VotedFor = NodeId.Null;
        }

        private void StartElection()
        {
            logger.LogInformation("Starting election");

            using (var transaction = storage.BeginTransaction(false))
            {
                transaction.CurrentTerm++;

                transaction.VotedFor = nodeIdProvider.LocalNodeId;
                receivedVoteCount = 1;

                electionTimeout.Start();

                transaction.Commit();

                var startedTerm = transaction.CurrentTerm;
                var lastLogEntry = transaction.GetLastLogEntry();
                var remoteNodeIds = transport.RemoteNodes.Select(n => n.Id).ToList();

                transport.Broadcast<RequestVoteResponse>(remoteNodeIds,new RequestVoteRequest()
                    {
                        CandidateId = nodeIdProvider.LocalNodeId,
                        LastLogIndex = transaction.GetLastLogIndex(),
                        Term = startedTerm,
                        LastLogTerm = lastLogEntry?.Term ?? 0,
                    }, (voterId,response) =>
                    {
                        if (response.Term == startedTerm)
                        {
                            // Accept vote:
                            receivedVoteCount++;

                            if (receivedVoteCount >= remoteNodeIds.Count().GetMajorityCount())
                            {
                                electionTimeout.Stop();
                                ConvertToLeader();
                            }
                        }
                    },
                    (voterId, ex) =>
                    {
                        
                    },CancellationToken.None);
            }

        }

        private void UpdateCurrentTerm(IStorageTransaction transaction,ITransportRequest request, ITransportResponse response)
        {
            var requestTerm = (request as ICarryTermUpdate)?.Term ?? 0;
            var responseTerm = (response as ICarryTermUpdate)?.Term ?? 0;

            var maxTerm = System.Math.Max(responseTerm, requestTerm);

            if (maxTerm > transaction.CurrentTerm)
            {
                transaction.CurrentTerm = maxTerm;
             
                ConvertToFollower(transaction);
            }
        }

        private bool IsCandidateAtLeastAsUpdateToDateAsThis(IStorageTransaction transaction, RequestVoteRequest request)
        {
            var lastEntry = transaction.GetLastLogEntry();

            if (lastEntry == null)
            {
                return true;
            }
            else
            {
                if (request.LastLogTerm >= lastEntry.Term)
                {
                    return true;
                }

                if (request.LastLogTerm == lastEntry.Term)
                {
                    if (request.LastLogIndex >= transaction.GetLastLogIndex())
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void AddHandler<T>(Func<NodeId, T,IStorageTransaction,ITransportResponse> handler,Func<IStorageTransaction,ITransportResponse> errorHandler = null)
            where T : ITransportRequest
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            handlers.Add(new HandlerEntry()
            {
                Condition = r => r is T,
                Handler = (sender,request,transaction) => handler.Invoke(sender,(T)request,transaction),
                ErrorHandler = errorHandler,
                IsWriting = errorHandler != null
            });
        }
    }
}