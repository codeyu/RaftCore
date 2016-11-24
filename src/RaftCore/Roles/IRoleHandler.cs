using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RaftCore.Commands;
using RaftCore.Messages;
using RaftCore.Queries;

namespace RaftCore.Roles
{
    public interface IRoleHandler
    {
        NodeRole            RoleHandled { get; }

        void                OnEnter(RoleContext context, NodeRole newRole);

        Message             OnRequest(RoleContext context,NodeId sender,Message request);

        void                OnResponse(RoleContext context,NodeId sender,object state,Message response,Exception exception);

        Task<IList<object>> OnQueries(RoleContext context, IEnumerable<IQuery> queries,QueryConsistency queryConsistency);

        Task                OnCommands(RoleContext context, IEnumerable<ICommand> commands);

        void                OnTimeout(RoleContext context);

        void                OnHeartbeat(RoleContext context);

        void                OnExit(RoleContext context, NodeRole newRole);
    }
}