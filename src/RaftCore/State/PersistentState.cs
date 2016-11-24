namespace RaftCore.State
{
    public class PersistentState
    {
        public long CurrentTerm { get; set; }

        public NodeId? VotedFor { get; set; }

        public NodeId SelfId { get; set; }

        public LogEntry GetLogEntry(long index)
        {
            return null;
        }

        public long GetLastLogIndex()
        {
            return 0L;
        }

        public void DeleteLogEntriesStartingAt(long startIndex)
        {
            
        }

        public void ApplyLogEntry(long applyIndex)
        {

        }

        public bool IsUpToDateWithLog(long term, long index)
        {
            return false;
        }

        public void InsertLogEntry(long index, LogEntry entry)
        {
            
        }
    }
}