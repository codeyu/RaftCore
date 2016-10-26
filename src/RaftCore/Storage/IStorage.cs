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

        LogEntry    GetEntryAtIndex(long index);

        void        DeleteEntriesStartingWithIndex(long index);

        void        InsertEntry(long index, LogEntry entry);

        void        Commit();
    }
}