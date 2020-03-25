using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace GazeTrackerCore.Consumer
{
    public sealed class FpsDetector
    {
        private static readonly TimeSpan HistoryLength = TimeSpan.FromSeconds(2);

        private DateTime StartTime = DateTime.Now;
        private readonly Stopwatch Sw = new Stopwatch();
        private readonly ConcurrentQueue<DateTime> FrameTimes = new ConcurrentQueue<DateTime>();

        private DateTime CurrentTime => StartTime + Sw.Elapsed;
        public double Fps => FrameTimes.TryPeek(out var frameTime) ? FrameTimes.Count / (CurrentTime - frameTime).TotalSeconds : 0;

        public FpsDetector(CancellationToken token)
        {
            Sw.Start();
            Task.Factory.StartNew(DiscardOldFrames, token);
        }

        private void DiscardOldFrames()
        {
            while (true)
            {
                while (FrameTimes.TryPeek(out var frameTime) && CurrentTime - frameTime > HistoryLength)
                {
                    FrameTimes.TryDequeue(out _);
                }

                Thread.Sleep(10);
            }
        }

        public void AddFrame()
        {
            FrameTimes.Enqueue(CurrentTime);
        }

        public void Reset()
        {
            while (!FrameTimes.IsEmpty)
            {
                FrameTimes.TryDequeue(out _);
            }
            StartTime = DateTime.Now;
            Sw.Restart();
        }
    }
}
