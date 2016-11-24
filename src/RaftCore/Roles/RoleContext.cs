using RaftCore.Messages;
using RaftCore.State;
using RaftCore.Topology;

namespace RaftCore.Roles
{
    public abstract class RoleContext
    {
        public LeaderState LeaderState { get; }

        public VolatileState VolatileState { get; }

        public PersistentState PersistentState { get; }

        public NodeTopology Topology { get; }

        public void ConvertTo(NodeRole role)
        {
            
        }

        public void ResetElectionTimeout()
        {
            
        }

        public void Broadcast(Message request,object state = null,bool retry = true, bool includeSelf = false)
        {
            request.Term = PersistentState.CurrentTerm;
        }
    }
}