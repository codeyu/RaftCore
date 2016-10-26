namespace RaftCore.Storage
{
    public class LogEntry
    {
        public long     Term { get; set; }

        public ICommand Command { get; set; }
    }
}