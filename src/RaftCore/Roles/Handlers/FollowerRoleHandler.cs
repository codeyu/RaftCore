using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RaftCore.Commands;
using RaftCore.Messages;
using RaftCore.Queries;

namespace RaftCore.Roles.Handlers
{
    public class FollowerRoleHandler : RoleHandlerBase
    {
        private AppendEntriesRequest lastAppendEntriesRequest;

        public FollowerRoleHandler(ILogger<FollowerRoleHandler> logger) 
            : base(logger, NodeRole.Follower)
        {
            HandleRequest<AppendEntriesRequest>(OnAppendEntriesRequest);
            HandleRequest<RequestVoteRequest>(OnRequestVoteRequest);
        }

        private Message OnRequestVoteRequest(RoleContext context, NodeId nodeId, RequestVoteRequest request)
        {
            if (request.Term >= context.PersistentState.CurrentTerm)
            {
                if (context.PersistentState.VotedFor == null || context.PersistentState.VotedFor == request.CandidateId)
                {
                    if (context.PersistentState.IsUpToDateWithLog(request.LastLogTerm, request.LastLogIndex))
                    {
                        context.PersistentState.VotedFor = request.CandidateId;

                        Log.LogInformation("Voted for {candidate}", request.CandidateId);
                        return new RequestVoteResponse() {VoteGranted = true};
                    }

                    Log.LogInformation("Candidate {candidate} log is not up to date with local log", request.CandidateId);
                }
                else
                {
                    Log.LogInformation("Already voted not for candidate {candidate}", request.CandidateId);
                }
            }
            else
            {
                Log.LogInformation("Vote request term is too small {term}, local {localTerm}",request.Term,context.PersistentState.CurrentTerm);
            }
          
            Log.LogInformation("Refused to vote for {candidate}",request.CandidateId);
            return new RequestVoteResponse() {VoteGranted = false};
        }

        private Message OnAppendEntriesRequest(RoleContext context, NodeId nodeId, AppendEntriesRequest request)
        {
            lastAppendEntriesRequest = request;

            if (request.Term >= context.PersistentState.CurrentTerm)
            {
                var prevEntry = context.PersistentState.GetLogEntry(request.PreviousLogIndex);

                if (prevEntry == null || prevEntry.Term == request.PreviousLogTerm)
                {
                    var lastInsertedEntryIndex = long.MaxValue;
                    var targetLogIndex = request.PreviousLogIndex + 1;

                    foreach (var entry in request.Entries)
                    {
                        var existingEntry = context.PersistentState.GetLogEntry(targetLogIndex);

                        if (existingEntry != null && existingEntry.Term != entry.Term)
                        {
                            Log.LogInformation("Found conflicting entry at index {index}, different terms, deleting following entries",targetLogIndex);

                            context.PersistentState.DeleteLogEntriesStartingAt(targetLogIndex);
                        }

                        Log.LogInformation("Inserting log entry at index {index}",targetLogIndex);
                        context.PersistentState.InsertLogEntry(targetLogIndex,entry);

                        lastInsertedEntryIndex = targetLogIndex;
                        targetLogIndex++;
                    }

                    if (request.LeaderCommitIndex > context.VolatileState.CommitIndex)
                    {
                        Log.LogInformation("Commit index of leader is higher {leadercommitindex}, updating local commit index {localcommitindex}",request.LeaderCommitIndex,context.VolatileState.CommitIndex);

                        context.VolatileState.CommitIndex = Math.Min(request.LeaderCommitIndex, lastInsertedEntryIndex);
                    }

                    Log.LogInformation("Append entries successful");
                    return new AppendEntriesResponse() {WasSuccessful = true};
                }
                else
                {
                    Log.LogInformation("Previous log entry term {term} does not match append entries request term {requestterm}",prevEntry.Term,request.PreviousLogTerm);   
                }
            }
            else
            {
                Log.LogInformation("Append entries request term is too small {term}, local {localTerm}",request.Term,context.PersistentState.CurrentTerm);
            }

            Log.LogInformation("Refused append entries request");
            return new AppendEntriesResponse() { WasSuccessful = false};
        }

        public override void OnEnter(RoleContext context, NodeRole newRole)
        {
            context.PersistentState.VotedFor = null;
            lastAppendEntriesRequest = null;
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
            if (lastAppendEntriesRequest == null && context.PersistentState.VotedFor == null)
            {
                // We did not receive an append entries request within the timeout:
                Log.LogInformation("Did not receive heartbeat, or voted for someone, converting to Candidate");
                context.ConvertTo(NodeRole.Candidate);
            }

            // Reset to wait for next timeout
            lastAppendEntriesRequest = null;
            context.PersistentState.VotedFor = null;
        }

        public override void OnHeartbeat(RoleContext context)
        {
         
        }

        public override void OnExit(RoleContext context, NodeRole newRole)
        {
           
        }
    }
}