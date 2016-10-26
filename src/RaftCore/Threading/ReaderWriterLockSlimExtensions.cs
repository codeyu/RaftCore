using System;
using System.Threading;

namespace RaftCore.Threading
{
    public static class ReaderWriterLockSlimExtensions
    {
        public abstract class LockHolderBase : IDisposable
        {
            private readonly ReaderWriterLockSlim   lockObject;

            public bool WasAquired { get; }

            protected LockHolderBase(ReaderWriterLockSlim lockObject, bool wasAquired)
            {
                this.lockObject = lockObject;
                this.WasAquired = wasAquired;
            }

            protected abstract void OnDispose(ReaderWriterLockSlim lockObject);

            public void Dispose()
            {
                if (WasAquired)
                {
                    OnDispose(lockObject);
                }
            }
        }

        /// <summary>
        /// This is implemented with explicit classes, to avoid allocating closures on action based disposables.
        /// </summary>
        public class ReadLockHolder : LockHolderBase
        {
            public ReadLockHolder(ReaderWriterLockSlim lockObject, bool wasAquired) 
                : base(lockObject, wasAquired)
            {
            }

            protected override void OnDispose(ReaderWriterLockSlim lockObject)
            {
                lockObject.ExitReadLock();
            }
        }

        /// <summary>
        /// This is implemented with explicit classes, to avoid allocating closures on action based disposables.
        /// </summary>
        public class WriteLockHolder : LockHolderBase
        {
            public WriteLockHolder(ReaderWriterLockSlim lockObject, bool wasAquired) 
                : base(lockObject, wasAquired)
            {
            }

            protected override void OnDispose(ReaderWriterLockSlim lockObject)
            {
                lockObject.ExitWriteLock();
            }
        }

        /// <summary>
        /// This is implemented with explicit classes, to avoid allocating closures on action based disposables.
        /// </summary>
        public class UpgradeableReadLockHolder : LockHolderBase
        {
            public UpgradeableReadLockHolder(ReaderWriterLockSlim lockObject, bool wasAquired) 
                : base(lockObject, wasAquired)
            {
            }

            protected override void OnDispose(ReaderWriterLockSlim lockObject)
            {
                lockObject.ExitUpgradeableReadLock();
            }
        }

        public static ReadLockHolder AquireRead(this ReaderWriterLockSlim readerWriterLockSlim)
        {
            if (readerWriterLockSlim == null) throw new ArgumentNullException(nameof(readerWriterLockSlim));

            readerWriterLockSlim.EnterReadLock();

            return new ReadLockHolder(readerWriterLockSlim,true);
        }

        public static WriteLockHolder AquireWrite(this ReaderWriterLockSlim readerWriterLockSlim)
        {
            if (readerWriterLockSlim == null) throw new ArgumentNullException(nameof(readerWriterLockSlim));

            readerWriterLockSlim.EnterWriteLock();

            return new WriteLockHolder(readerWriterLockSlim,true);
        }

        public static UpgradeableReadLockHolder AquireUpgradeableRead(this ReaderWriterLockSlim readerWriterLockSlim)
        {
            if (readerWriterLockSlim == null) throw new ArgumentNullException(nameof(readerWriterLockSlim));

            readerWriterLockSlim.EnterUpgradeableReadLock();

            return new UpgradeableReadLockHolder(readerWriterLockSlim,true);
        }

        public static ReadLockHolder TryAquireRead(this ReaderWriterLockSlim readerWriterLockSlim, int timeout)
        {
            if (readerWriterLockSlim == null) throw new ArgumentNullException(nameof(readerWriterLockSlim));

            var wasAquired = readerWriterLockSlim.TryEnterReadLock(timeout);

            return new ReadLockHolder(readerWriterLockSlim, wasAquired);
        }

        public static WriteLockHolder TryAquireWrite(this ReaderWriterLockSlim readerWriterLockSlim, int timeout)
        {
            if (readerWriterLockSlim == null) throw new ArgumentNullException(nameof(readerWriterLockSlim));

            var wasRequired = readerWriterLockSlim.TryEnterWriteLock(timeout);

            return new WriteLockHolder(readerWriterLockSlim, wasRequired);
        }

        public static UpgradeableReadLockHolder TryAquireUpgradeableRead(this ReaderWriterLockSlim readerWriterLockSlim, int timeout)
        {
            if (readerWriterLockSlim == null) throw new ArgumentNullException(nameof(readerWriterLockSlim));

            var wasRequired = readerWriterLockSlim.TryEnterUpgradeableReadLock(timeout);

            return new UpgradeableReadLockHolder(readerWriterLockSlim, wasRequired);
        }

        public static ReadLockHolder TryAquireRead(this ReaderWriterLockSlim readerWriterLockSlim, TimeSpan timeout)
        {
            if (readerWriterLockSlim == null) throw new ArgumentNullException(nameof(readerWriterLockSlim));

            var wasAquired = readerWriterLockSlim.TryEnterReadLock(timeout);
           
            return new ReadLockHolder(readerWriterLockSlim,wasAquired);
        }

        public static WriteLockHolder TryAquireWrite(this ReaderWriterLockSlim readerWriterLockSlim, TimeSpan timeout)
        {
            if (readerWriterLockSlim == null) throw new ArgumentNullException(nameof(readerWriterLockSlim));

            var wasRequired = readerWriterLockSlim.TryEnterWriteLock(timeout);

            return new WriteLockHolder(readerWriterLockSlim,wasRequired);
        }

        public static UpgradeableReadLockHolder TryAquireUpgradeableRead(this ReaderWriterLockSlim readerWriterLockSlim,TimeSpan timeout)
        {
            if (readerWriterLockSlim == null) throw new ArgumentNullException(nameof(readerWriterLockSlim));

            var wasRequired = readerWriterLockSlim.TryEnterUpgradeableReadLock(timeout);

            return new UpgradeableReadLockHolder(readerWriterLockSlim,wasRequired);
        }
    }
}