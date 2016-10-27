using System.Collections.Generic;
using RaftCore.Storage;
using RaftCore.Transport;

namespace RaftCore.Protocol
{
    internal sealed class AppendEntriesRequest : ITransportRequest,ICarryTermUpdate
    {
        public long Term { get; set; }

        public NodeId LeaderId { get; set; }
        public long LeaderCommitIndex { get; set; }

        public long PreviousLogIndex { get; set; }

        public long PreviousLogTerm { get; set; }

        public IEnumerable<LogEntry> Entries { get; set; }
    }
}