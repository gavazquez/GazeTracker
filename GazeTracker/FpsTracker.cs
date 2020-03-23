using HeadPoseLive;
using System;
using System.Collections.Generic;

namespace OpenFaceOffline
{
    public class FpsTracker
    {
        public TimeSpan HistoryLength { get; set; }
        public FpsTracker()
        {
            HistoryLength = TimeSpan.FromSeconds(2);
        }

        private Queue<DateTime> frameTimes = new Queue<DateTime>();

        private void DiscardOldFrames()
        {
            while (frameTimes.Count > 0 && (MainWindow.CurrentTime - frameTimes.Peek()) > HistoryLength)
                frameTimes.Dequeue();
        }

        public void AddFrame()
        {
            frameTimes.Enqueue(MainWindow.CurrentTime);
            DiscardOldFrames();
        }

        public double GetFPS()
        {
            DiscardOldFrames();

            return frameTimes.Count == 0 ? 0 : frameTimes.Count / (MainWindow.CurrentTime - frameTimes.Peek()).TotalSeconds;
        }
    }
}
