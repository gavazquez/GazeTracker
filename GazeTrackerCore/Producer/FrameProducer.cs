using GazeTrackerCore.Structures;
using System;
using System.Threading;
using System.Threading.Tasks.Dataflow;
using UtilitiesOF;

namespace GazeTrackerCore.Producer
{
    public class FrameProducer : IDisposable
    {
        public volatile bool Paused;

        private float Fx;
        private float Fy;
        private float Cx;
        private float Cy;

        private SequenceReader _reader;

        public FrameProducer(int cameraId, int width, int height)
        {
            _reader = new SequenceReader(cameraId, width, height);
            if (!_reader.IsOpened())
            {
                throw new Exception("Could not start the web-cam");
            }
        }

        public void ReadFrames(BroadcastBlock<FrameData> broadcast, CancellationToken token)
        {
            while (true)
            {
                while (Paused)
                {
                    if (token.IsCancellationRequested) return;
                    Thread.Sleep(100);
                }

                if (Fx == 0 || Fy == 0 || Cx == 0 || Cy == 0)
                {
                    Fx = _reader.GetFx();
                    Fy = _reader.GetFy();
                    Cx = _reader.GetCx();
                    Cy = _reader.GetCy();
                }

                if (token.IsCancellationRequested) return;

                broadcast.Post(new FrameData(_reader.GetNextImage(), _reader.GetCurrentFrameGray(), Fx, Fy, Cx, Cy));
            }
        }

        public void Dispose()
        {
            _reader.Dispose();
        }

        public void Pause()
        {
            Paused = !Paused;
        }
    }
}
