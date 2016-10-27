using RaftCore.Transport;

namespace RaftCore.Protocol
{
    internal class RequestVoteResponse : ITransportResponse,ICarryTermUpdate
    {
        public long Term { get; set; }

        public bool WasGranted { get; set; }
    }
}