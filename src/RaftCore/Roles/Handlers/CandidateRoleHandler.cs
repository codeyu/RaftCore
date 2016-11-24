using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RaftCore.Commands;
using RaftCore.Messages;
using RaftCore.Queries;

namespace RaftCore.Roles.Handlers
{
    public class CandidateRoleHandler : RoleHandlerBase
    {
        private readonly HashSet<NodeId> receivedVotesFrom = new HashSet<NodeId>();

        public CandidateRoleHandler(ILogger<CandidateRoleHandler> logger) 
            : base(logger, NodeRole.Candidate)
        {
            HandleRequest<AppendEntriesRequest>(OnAppendEntriesRequest);

            HandleResponse<RequestVoteRequest,RequestVoteResponse>(OnRequestVoteResponse);
        }

        private void OnRequestVoteResponse(RoleContext context, NodeId senderId,object state, RequestVoteResponse response, Exception exception)
        {
            if (exception != null)
            {
                Log.LogError(1,exception,"RequestVote request returned an error");
                return;
            }

            if (response.Term == context.PersistentState.CurrentTerm)
            {
                if (response.VoteGranted)
                {
                    Log.LogInformation("Received vote from remote node {node}",senderId);

                    if (!receivedVotesFrom.Contains(senderId))
                    {
                        receivedVotesFrom.Add(senderId);

                        if (receivedVotesFrom.Count >= context.Topology.QuorumSize)
                        {
                            Log.LogInformation("Received {votecount} votes, enough for quorum, converting to leader",receivedVotesFrom.Count);

                            context.ConvertTo(NodeRole.Leader);
                        }
                    }
                    else
                    {
                        Log.LogError("Received duplicate vote from remote node {node}",senderId);
                    }
                }
                else
                {
                    Log.LogInformation("Remote node {node} did not vote for us",senderId);
                }
            }
            else
            {
                Log.LogWarning("Received RequestVote response, for incorrect term {responseterm}, expected {term}",response.Term,context.PersistentState.CurrentTerm);
            }
        }

        private Message OnAppendEntriesRequest(RoleContext context, NodeId nodeId, AppendEntriesRequest request)
        {
            if (request.Term == context.PersistentState.CurrentTerm)
            {
                // We received a leader, switch to follower
                Log.LogInformation("Received AppendEntries request from new leader, converting to follower");

                context.ConvertTo(NodeRole.Follower);

                return null;
            }

            Log.LogInformation("Received outdated AppendEntries request, ignoring");
            return null;
        }

        public override void OnEnter(RoleContext context, NodeRole newRole)
        {
            StartElection(context);
        }

        public override Task<IList<object>> OnQueries(RoleContext context, IEnumerable<IQuery> queries, QueryConsistency queryConsistency)
        {
            throw new NotImplementedException();
        }

        public override Task OnCommands(RoleContext context, IEnumerable<ICommand> commands)
        {
            throw new NotImplementedException();
        }

        public override void OnTimeout(RoleContext context)
        {
            StartElection(context);
        }

        public override void OnHeartbeat(RoleContext context)
        {
           
        }

        public override void OnExit(RoleContext context, NodeRole newRole)
        {
           
        }

        private void StartElection(RoleContext context)
        {
            context.PersistentState.CurrentTerm++;

            Log.LogInformation("Starting election cycle in term {term}",context.PersistentState.CurrentTerm);

            context.PersistentState.VotedFor = context.PersistentState.SelfId;
            receivedVotesFrom.Clear();
            receivedVotesFrom.Add(context.PersistentState.SelfId);

            Log.LogInformation("Voted for self");

            context.ResetElectionTimeout();

            var lastLogIndex = context.PersistentState.GetLastLogIndex();
            var lastLogEntry = context.PersistentState.GetLogEntry(lastLogIndex);

            context.Broadcast(new RequestVoteRequest()
            {
                CandidateId = context.PersistentState.SelfId,
                LastLogIndex  = lastLogIndex,
                LastLogTerm = lastLogEntry?.Term ?? 0L
            });
        }
    }
}