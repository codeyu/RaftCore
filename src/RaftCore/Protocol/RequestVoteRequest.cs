using RaftCore.Transport;

namespace RaftCore.Protocol
{
    internal class RequestVoteRequest : ITransportRequest,ICarryTerm
    {
        public long Term { get; set; }

        public NodeId CandidateId { get; set; }

        public long LastLogIndex { get; set; }

        public long LastLogTerm { get; set; }
    }
}