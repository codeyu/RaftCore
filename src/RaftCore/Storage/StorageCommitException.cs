using System;

namespace RaftCore.Storage
{
    public class StorageCommitException : Exception
    {
        public StorageCommitException()
        {
        }

        public StorageCommitException(string message) : base(message)
        {
        }

        public StorageCommitException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}