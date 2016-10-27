using System;
using System.Diagnostics;
using System.Threading;

namespace RaftCore.Logic
{
    internal sealed class Timeout : IDisposable
    {
        private readonly object     lockObject = new object();

        private readonly Action     timeoutAction;
        private readonly Timer      timer;
        private readonly TimeSpan   interval;
        private readonly int        timerInterval;

        private long timeStamp;

        internal Timeout(TimeSpan interval, Action timeoutAction)
        {
            if (timeoutAction == null) throw new ArgumentNullException(nameof(timeoutAction));
            if (interval.Ticks == 0)
            {
                throw new ArgumentException("Interval cannot be zero",nameof(interval));
            }

            this.interval       = interval;
            this.timeoutAction  = timeoutAction;

            this.timerInterval  = (int)System.Math.Ceiling(interval.TotalMilliseconds/10.0);
   
            this.timer = new Timer(state =>
            {
                var hasElapsed = false;

                lock (lockObject)
                {
                    var elapsedTicks    = Stopwatch.GetTimestamp() - timeStamp;
                    var elapsedMs       = (((double) elapsedTicks/(double) Stopwatch.Frequency)*1000.0);

                    if (elapsedMs > interval.TotalMilliseconds)
                    {
                        hasElapsed = true;
                        timeStamp = Stopwatch.GetTimestamp();
                    }
                }

                if (hasElapsed)
                {
                    timeoutAction.Invoke();
                }
            },null,System.Threading.Timeout.Infinite,System.Threading.Timeout.Infinite);
        }

        internal void Start()
        {
            lock (lockObject)
            {
                this.timeStamp = Stopwatch.GetTimestamp();

                this.timer.Change(timerInterval, timerInterval);
            }
        }

        internal void Stop()
        {
            lock (lockObject)
            {
                this.timer.Change(System.Threading.Timeout.Infinite, System.Threading.Timeout.Infinite);
            }
        }

        internal void Reset()
        {
            lock (lockObject)
            {
                timeStamp = Stopwatch.GetTimestamp();
            }
        }

        public void Dispose()
        {
            timer.Dispose();
        }
    }
}