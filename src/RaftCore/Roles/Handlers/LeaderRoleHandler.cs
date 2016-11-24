using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RaftCore.Commands;
using RaftCore.Messages;
using RaftCore.Queries;
using RaftCore.State;

namespace RaftCore.Roles.Handlers
{
    public class LeaderRoleHandler : RoleHandlerBase
    {
        public LeaderRoleHandler(ILogger<LeaderRoleHandler> logger) 
            : base(logger, NodeRole.Leader)
        {
        }

        public override void OnEnter(RoleContext context, NodeRole newRole)
        {
           SendAppendEntriesRequest(context,null);
        }

        public override Task<IList<object>> OnQueries(RoleContext context, IEnumerable<IQuery> queries, QueryConsistency queryConsistency)
        {
            throw new System.NotImplementedException();
        }

        public override Task OnCommands(RoleContext context, IEnumerable<ICommand> commands)
        {
            return Task.CompletedTask;
        }

        public override void OnTimeout(RoleContext context)
        {
           
        }

        public override void OnHeartbeat(RoleContext context)
        {
            SendAppendEntriesRequest(context,null);
        }

        public override void OnExit(RoleContext context, NodeRole newRole)
        {
            
        }

        private void SendAppendEntriesRequest(RoleContext context,IList<ICommand> commands)
        {
            var logEntries = (commands ?? Enumerable.Empty<ICommand>()).Select(c => new LogEntry()
            {
                Command = c,
                Term = context.PersistentState.CurrentTerm
            }).ToList();

            var lastLogIndex = context.PersistentState.GetLastLogIndex();
            var lastLogEntry = context.PersistentState.GetLogEntry(lastLogIndex);

            var request = new AppendEntriesRequest()
            {
                Entries = logEntries,
                PreviousLogTerm = lastLogEntry?.Term ?? 0L,
                PreviousLogIndex = lastLogIndex,
                LeaderId = context.PersistentState.SelfId,
                LeaderCommitIndex = context.VolatileState.CommitIndex
            };

            context.Broadcast(request);
        }
    }
}