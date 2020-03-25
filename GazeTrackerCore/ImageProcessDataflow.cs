using CppInterop.LandmarkDetector;
using GazeTrackerCore.Consumer;
using GazeTrackerCore.Consumer.Extractor;
using GazeTrackerCore.Producer;
using GazeTrackerCore.Structures;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Media.Imaging;
using UtilitiesOF;

namespace GazeTrackerCore
{
    public sealed class ImageProcessDataflow : IDisposable
    {
        private readonly BitmapTransformer bitmapTransformer = new BitmapTransformer();
        private readonly FrameProducer frameProducer;

        public bool Paused => frameProducer.Paused;
        public double Fps => fpsDetector.Fps;
        public double BitmapFps => bitmapFpsDetector.Fps;

        public BroadcastBlock<DetectedData> DetectedDataBroadcast { get; } = new BroadcastBlock<DetectedData>(f => f, new DataflowBlockOptions
        {
            CancellationToken = tokenSource.Token
        });
        public BroadcastBlock<LandmarkData> LandmarkDataBroadcast { get; } = new BroadcastBlock<LandmarkData>(f => f, new DataflowBlockOptions
        {
            CancellationToken = tokenSource.Token
        });
        public BroadcastBlock<WriteableBitmap> BitmapBroadcast { get; } = new BroadcastBlock<WriteableBitmap>(f => f, new DataflowBlockOptions
        {
            CancellationToken = tokenSource.Token
        });
        public BroadcastBlock<FrameData> FrameDataBroadcast { get; } = new BroadcastBlock<FrameData>(f => f, new DataflowBlockOptions
        {
            CancellationToken = tokenSource.Token
        });

        private readonly SequenceReader sequenceReader;
        private readonly LandmarkExtractor landmarkDetector;
        private readonly DataExtractor dataExtractor;
        private readonly UdpSender udpSender;
        private readonly FpsDetector fpsDetector;
        private readonly FpsDetector bitmapFpsDetector;

        private static readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

        public ImageProcessDataflow(int cameraId, int width, int height, IPEndPoint udpEndpoint)
        {

            var faceModelParams = new FaceModelParameters(AppDomain.CurrentDomain.BaseDirectory, true, false, false);

            frameProducer = new FrameProducer(cameraId, width, height);
            fpsDetector = new FpsDetector(tokenSource.Token);
            bitmapFpsDetector = new FpsDetector(tokenSource.Token);
            udpSender = new UdpSender(udpEndpoint);
            sequenceReader = new SequenceReader(cameraId, width, height);
            landmarkDetector = new LandmarkExtractor(faceModelParams);
            dataExtractor = new DataExtractor(faceModelParams);

            var bitmapBlock = new TransformBlock<FrameData, WriteableBitmap>(frm => bitmapTransformer.ConvertToWritableBitmap(frm.Frame), new ExecutionDataflowBlockOptions
            {
                CancellationToken = tokenSource.Token,
                TaskScheduler = TaskScheduler.FromCurrentSynchronizationContext()
            });

            var landmarkBlock = new TransformBlock<FrameData, LandmarkData>(x => landmarkDetector.DetectLandmarks(x), new ExecutionDataflowBlockOptions
            {
                CancellationToken = tokenSource.Token,
                BoundedCapacity = 1
            });

            var dataDetectorBlock = new TransformBlock<LandmarkData, DetectedData>(ld => dataExtractor.DetectionParameters(ld), new ExecutionDataflowBlockOptions
            {
                CancellationToken = tokenSource.Token,
                BoundedCapacity = 1
            });

            var udpBlock = new ActionBlock<DetectedData>(d => udpSender.SendPoseData(d.Pose), new ExecutionDataflowBlockOptions
            {
                CancellationToken = tokenSource.Token,
                BoundedCapacity = 1
            });

            var fpsBlock = new ActionBlock<DetectedData>(_ => fpsDetector.AddFrame(), new ExecutionDataflowBlockOptions
            {
                CancellationToken = tokenSource.Token
            });
            var bitmapFpsBlock = new ActionBlock<WriteableBitmap>(_ => bitmapFpsDetector.AddFrame(), new ExecutionDataflowBlockOptions
            {
                CancellationToken = tokenSource.Token
            });

            FrameDataBroadcast.LinkTo(bitmapBlock);
            bitmapBlock.LinkTo(BitmapBroadcast);
            BitmapBroadcast.LinkTo(bitmapFpsBlock);

            FrameDataBroadcast.LinkTo(landmarkBlock);
            landmarkBlock.LinkTo(LandmarkDataBroadcast);

            LandmarkDataBroadcast.LinkTo(dataDetectorBlock, l => l.DetectionSucceeded);
            dataDetectorBlock.LinkTo(DetectedDataBroadcast);

            DetectedDataBroadcast.LinkTo(fpsBlock);
            DetectedDataBroadcast.LinkTo(udpBlock);
        }

        public void TogglePause()
        {
            frameProducer.Pause();
        }

        public void Start()
        {
            Task.Factory.StartNew(() => frameProducer.ReadFrames(FrameDataBroadcast, tokenSource.Token), tokenSource.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Stop()
        {
            tokenSource.Cancel();
        }

        public void ChangeUdpEndpoint(IPEndPoint newEndpoint)
        {
            udpSender?.Connect(newEndpoint);
        }

        public void Reset()
        {
            dataExtractor.Reset();
            landmarkDetector.Reset();
            fpsDetector.Reset();
            bitmapFpsDetector.Reset();
        }

        public void Dispose()
        {
            Stop();
            frameProducer?.Dispose();
            dataExtractor?.Dispose();
            landmarkDetector?.Dispose();
            sequenceReader?.Dispose();
            udpSender?.Dispose();
        }
    }
}
