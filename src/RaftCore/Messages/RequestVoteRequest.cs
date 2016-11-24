namespace RaftCore.Messages
{
    public class RequestVoteRequest : Message,IRequireExclusiveAccess
    {
        public NodeId CandidateId { get; set; }

        public long LastLogIndex { get; set; }

        public long LastLogTerm { get; set; }
    }
}