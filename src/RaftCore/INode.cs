using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RaftCore
{
    public enum NodeRole
    {
        Follower,
        Candidate,
        Leader
    }

    public interface INode
    {
        event Action<NodeId>    LeaderChanged;

        NodeId                  Id { get; }

        NodeRole                Role { get; }

        NodeId                  LeaderId { get; }

        Task                    Start();

        Task                    ApplyCommands(IEnumerable<ICommand> commands,bool forwardToLeader);

        Task<IList<object>>     ExecuteQueries(IEnumerable<IQuery> queries,QueryConsistency consistency,CancellationToken cancellationToken);
    }
}