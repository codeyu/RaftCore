using System.Collections.Generic;
using RaftCore.Commands;

namespace RaftCore.State
{
    public class LogEntry
    {
        public long             Term        { get; set; }

        public ICommand         Command     { get; set; }
    }
}