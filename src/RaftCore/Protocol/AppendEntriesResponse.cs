using RaftCore.Transport;

namespace RaftCore.Protocol
{
    internal class AppendEntriesResponse : ITransportResponse,ICarryTerm
    {
        public long Term { get; set; }

        public bool WasSuccessful { get; set; }
    }
}