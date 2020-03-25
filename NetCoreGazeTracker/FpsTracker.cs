using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace GazeTracker.Tool
{
    public class FpsTracker
    {
        private static readonly DateTime StartTime = DateTime.Now;
        private static readonly Stopwatch Sw = new Stopwatch();
        private static readonly Queue<DateTime> FrameTimes = new Queue<DateTime>();

        private static DateTime CurrentTime => StartTime + Sw.Elapsed;
        public static TimeSpan HistoryLength { get; set; } = TimeSpan.FromSeconds(2);

        public FpsTracker(VideoHandler videoHandler)
        {
            videoHandler.OnBitmapUpdate += (source, args) => AddFrame();
            Sw.Start();
        }

        private static void DiscardOldFrames()
        {
            while (FrameTimes.Count > 0 && CurrentTime - FrameTimes.Peek() > HistoryLength)
                FrameTimes.Dequeue();
        }

        private static void AddFrame()
        {
            FrameTimes.Enqueue(CurrentTime);
            DiscardOldFrames();
        }

        public double GetFps()
        {
            DiscardOldFrames();
            return FrameTimes.Count == 0 ? 0 : FrameTimes.Count / (CurrentTime - FrameTimes.Peek()).TotalSeconds;
        }
    }
}
