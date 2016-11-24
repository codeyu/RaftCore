using System.Collections.Generic;
using RaftCore.State;

namespace RaftCore.Messages
{
    public class AppendEntriesRequest : Message,IRequireExclusiveAccess
    {
        public NodeId LeaderId { get; set; }

        public long LeaderCommitIndex { get; set; }

        public long PreviousLogIndex { get; set; }

        public long PreviousLogTerm { get; set; }

        public IList<LogEntry> Entries { get; set; }
    }
}