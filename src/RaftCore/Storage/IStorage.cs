using System;

namespace RaftCore.Storage
{
    public interface IStorage
    {
        IStorageTransaction BeginTransaction(bool readOnly);
    }

    public interface IStorageTransaction : IDisposable
    {
        long        CurrentTerm { get; set; }

        NodeId      VotedFor { get; set; }

        long        GetLastLogIndex();

        LogEntry    GetLogEntryAt(long index);

        void        DeleteLogEntriesStartingAt(long index);

        void        InsertLogEntry(long index, LogEntry entry);

        void        Commit();
    }
}