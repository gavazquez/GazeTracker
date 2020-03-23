using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using UtilitiesOF;

namespace GazeTracker.Tool
{
    public class FrameArgs : EventArgs
    {
        public FrameHandler FrameHandler { get; set; }

        public FrameArgs(FrameHandler frameHandler)
        {
            FrameHandler = frameHandler;
        }
    }

    public class BitmapArgs : EventArgs
    {
        public WriteableBitmap Bitmap { get; set; }

        public BitmapArgs(WriteableBitmap bitmap)
        {
            Bitmap = bitmap;
        }
    }

    public sealed class VideoHandler : IDisposable
    {
        public event EventHandler<FrameArgs> OnDetectionSucceeded;
        public event EventHandler<EventArgs> OnDetectionFailed;
        public event EventHandler<BitmapArgs> OnBitmapUpdate;

        public volatile bool TriggerReset;
        public volatile bool Pause;

        private WriteableBitmap _bitmap;
        private readonly SequenceReader _reader;
        private readonly FrameHandler _frameHandler;

        public VideoHandler(SequenceReader reader, FrameHandler frameHandler)
        {
            _reader = reader;
            _frameHandler = frameHandler;
        }

        public void TogglePause()
        {
            Pause = !Pause;
        }

        public void Reset()
        {
            TriggerReset = true;
        }

        public void Start(CancellationToken token)
        {
            Task.Factory.StartNew(() => VideoLoop(_reader, token), token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void VideoLoop(SequenceReader reader, CancellationToken token)
        {
            var bitmapArgs = new BitmapArgs(null);
            var frameArgs = new FrameArgs(null);

            while (true)
            {
                var frame = reader.GetNextImage();

                if (_bitmap == null)
                    _bitmap = frame.CreateWriteableBitmap();

                frame.UpdateWriteableBitmap(_bitmap);
                bitmapArgs.Bitmap = _bitmap;

                OnBitmapUpdate?.Invoke(this, bitmapArgs);

                if (token.IsCancellationRequested) break;

                if (_frameHandler.DetectLandmarks(reader, frame))
                {
                    _frameHandler.DetectionParameters(reader);

                    frameArgs.FrameHandler = _frameHandler;
                    OnDetectionSucceeded?.Invoke(this, frameArgs);
                }
                else
                {
                    OnDetectionFailed?.Invoke(this, null);
                }

                if (TriggerReset)
                {
                    _frameHandler.Reset();
                    TriggerReset = false;
                }

                while (Pause)
                {
                    token.ThrowIfCancellationRequested();
                    Thread.Sleep(10);
                }
            }
        }

        public void Dispose()
        {
            _reader?.Dispose();
        }
    }
}
