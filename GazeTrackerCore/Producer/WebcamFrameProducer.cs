using GazeTrackerCore.Producer.Base;
using OpenCVWrappers;
using System;
using UtilitiesOF;

namespace GazeTrackerCore.Producer
{
    public class WebcamFrameProducer : FrameProducer
    {
        private SequenceReader _reader;

        public WebcamFrameProducer(int cameraId, int width, int height)
        {
            _reader = new SequenceReader(cameraId, width, height);
            if (!_reader.IsOpened())
            {
                throw new Exception("Could not start the web-cam");
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _reader.Dispose();
        }

        protected override RawImage GetNextFrame() => _reader.GetNextImage();
        protected override RawImage GetGrayFrame() => _reader.GetCurrentFrameGray();

        protected override float GetFx() => _reader.GetFx();
        protected override float GetFy() => _reader.GetFy();
        protected override float GetCx() => _reader.GetCx();
        protected override float GetCy() => _reader.GetCy();
    }
}
