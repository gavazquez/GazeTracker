using GazeTrackerCore.Structures;
using OpenCVWrappers;
using System;
using System.Threading;
using System.Threading.Tasks.Dataflow;

namespace GazeTrackerCore.Producer.Base
{
    public abstract class FrameProducer : IDisposable
    {
        private float Fx;
        private float Fy;
        private float Cx;
        private float Cy;

        private CancellationTokenSource cts = new CancellationTokenSource();

        protected abstract RawImage GetNextFrame();
        protected abstract RawImage GetGrayFrame();
        protected abstract float GetFx();
        protected abstract float GetFy();
        protected abstract float GetCx();
        protected abstract float GetCy();

        public void ReadFrames(BroadcastBlock<FrameData> broadcast)
        {
            while (true)
            {
                if (Fx == 0 || Fy == 0 || Cx == 0 || Cy == 0)
                {
                    Fx = GetFx();
                    Fy = GetFy();
                    Cx = GetCx();
                    Cy = GetCy();
                }

                if (cts.Token.IsCancellationRequested) return;

                broadcast.Post(new FrameData(GetNextFrame(), GetGrayFrame(), Fx, Fy, Cx, Cy));
            }
        }

        public virtual void Dispose()
        {
            cts.Cancel();
        }
    }
}
