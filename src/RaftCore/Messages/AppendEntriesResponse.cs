namespace RaftCore.Messages
{
    public class AppendEntriesResponse : Message,IRequireExclusiveAccess
    {
        public bool WasSuccessful { get; set; }
    }
}