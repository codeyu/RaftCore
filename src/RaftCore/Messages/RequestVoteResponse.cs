namespace RaftCore.Messages
{
    public class RequestVoteResponse : Message,IRequireExclusiveAccess
    {
        public bool VoteGranted { get; set; }
    }
}