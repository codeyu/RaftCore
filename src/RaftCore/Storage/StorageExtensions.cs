using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RaftCore.Storage
{
    internal static class StorageExtensions
    {
        internal static LogEntry GetLastLogEntry(this IStorageTransaction transaction)
        {
            if (transaction == null) throw new ArgumentNullException(nameof(transaction));

            var lastIndex = transaction.GetLastLogIndex();

            if (lastIndex >= 0)
            {
                return transaction.GetLogEntryAt(lastIndex);
            }

            return null;
        }
    }
}
