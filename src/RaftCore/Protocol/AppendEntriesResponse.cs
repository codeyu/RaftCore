using RaftCore.Transport;

namespace RaftCore.Protocol
{
    internal class AppendEntriesResponse : ITransportResponse,ICarryTermUpdate
    {
        public long Term { get; set; }

        public bool WasSuccessful { get; set; }
    }
}