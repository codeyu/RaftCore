namespace RaftCore.State
{
    public class VolatileState
    {
        public long CommitIndex { get; set; }

        public long LastAppliedIndex { get; set; }
    }
}