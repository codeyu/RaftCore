using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
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
            internal Func<NodeId, ITransportRequest, ITransportResponse> Handler;
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
                    return handler.Handler.Invoke(sender, request);
                }
            }

            return null;
        }

        private ITransportRequest OnAppendEntriesReceived(NodeId sender, AppendEntriesRequest request)
        {
            using (lockObject.AquireWrite())
            {
                if (currentRole == NodeRole.Candidate)
                {
                    ConvertToFollower();
                }

                if (currentRole == NodeRole.Follower)
                {
                    
                }


                if (currentRole == NodeRole.Leader)
                {
                    
                }
            }

            return null;
        }

        private ITransportResponse OnRequestVoteReceived(NodeId sender, RequestVoteRequest request)
        {
            return null;
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

        private void ConvertToFollower()
        {
            logger.LogInformation("Converting to follower");
        }

        private void StartElection()
        {
            logger.LogInformation("Starting election");

            using (var transaction = storage.BeginTransaction(false))
            {
                transaction.CurrentTerm++;

                transaction.VotedFor = nodeIdProvider.LocalNodeId;
                receivedVoteCount = 1;

                electionTimeout.Reset();

           
                transaction.Commit();
            }

        }

        private void AddHandler<T>(Func<NodeId, T, ITransportResponse> handler)
            where T : ITransportRequest
        {
            handlers.Add(new HandlerEntry()
            {
                Condition = r => r is T,
                Handler = (sender,request) => handler.Invoke(sender,(T)request)
            });   
        }
    }
}