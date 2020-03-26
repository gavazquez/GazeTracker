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
        public bool Paused => frameProducer.Paused;
        public double CameraFps => cameraFps.Fps;
        public double LandmarkFps => landmarkFps.Fps;
        public double DetectedFps => detectedFps.Fps;

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

        private readonly BitmapTransformer bitmapTransformer = new BitmapTransformer();
        private readonly FrameProducer frameProducer;
        private readonly SequenceReader sequenceReader;
        private readonly LandmarkExtractor landmarkExtractor;
        private readonly DataExtractor dataExtractor;
        private readonly UdpSender udpSender;
        private readonly FpsDetector cameraFps;
        private readonly FpsDetector landmarkFps;
        private readonly FpsDetector detectedFps;

        private static readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

        public ImageProcessDataflow(int cameraId, int width, int height, IPEndPoint udpEndpoint)
        {
            var faceModelParams = new FaceModelParameters(AppDomain.CurrentDomain.BaseDirectory, true, false, false);

            frameProducer = new FrameProducer(cameraId, width, height);

            cameraFps = new FpsDetector(tokenSource.Token);
            landmarkFps = new FpsDetector(tokenSource.Token);
            detectedFps = new FpsDetector(tokenSource.Token);

            udpSender = new UdpSender(udpEndpoint);
            sequenceReader = new SequenceReader(cameraId, width, height);
            landmarkExtractor = new LandmarkExtractor(faceModelParams);
            dataExtractor = new DataExtractor(faceModelParams);

            var bitmapBlock = new TransformBlock<FrameData, WriteableBitmap>(frm => bitmapTransformer.ConvertToWritableBitmap(frm.Frame), new ExecutionDataflowBlockOptions
            {
                CancellationToken = tokenSource.Token,
                TaskScheduler = TaskScheduler.FromCurrentSynchronizationContext()
            });

            var landmarkBlock = new TransformBlock<FrameData, LandmarkData>(x => landmarkExtractor.DetectLandmarks(x), new ExecutionDataflowBlockOptions
            {
                CancellationToken = tokenSource.Token,
                BoundedCapacity = 1,
            });

            var dataDetectorBlock = new TransformBlock<LandmarkData, DetectedData>(ld => dataExtractor.ExtractData(ld), new ExecutionDataflowBlockOptions
            {
                CancellationToken = tokenSource.Token,
                BoundedCapacity = 1
            });

            var udpBlock = new ActionBlock<DetectedData>(d => udpSender.SendPoseData(d.Pose), new ExecutionDataflowBlockOptions
            {
                CancellationToken = tokenSource.Token,
                BoundedCapacity = 1
            });

            var cameraFpsBlock = new ActionBlock<FrameData>(_ => cameraFps.AddFrame(), new ExecutionDataflowBlockOptions { CancellationToken = tokenSource.Token });
            var landmarkFpsBlock = new ActionBlock<LandmarkData>(_ => landmarkFps.AddFrame(), new ExecutionDataflowBlockOptions { CancellationToken = tokenSource.Token });
            var detectedFpsBlock = new ActionBlock<DetectedData>(_ => detectedFps.AddFrame(), new ExecutionDataflowBlockOptions { CancellationToken = tokenSource.Token });

            FrameDataBroadcast.LinkTo(bitmapBlock);
            FrameDataBroadcast.LinkTo(cameraFpsBlock);
            bitmapBlock.LinkTo(BitmapBroadcast);

            FrameDataBroadcast.LinkTo(landmarkBlock);
            landmarkBlock.LinkTo(LandmarkDataBroadcast);

            LandmarkDataBroadcast.LinkTo(landmarkFpsBlock);
            LandmarkDataBroadcast.LinkTo(dataDetectorBlock, l => l.DetectionSucceeded);
            dataDetectorBlock.LinkTo(DetectedDataBroadcast);

            DetectedDataBroadcast.LinkTo(detectedFpsBlock);
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

        public void ChangeUdpEndpoint(IPEndPoint newEndpoint)
        {
            udpSender?.Connect(newEndpoint);
        }

        public void Reset()
        {
            dataExtractor.Reset();
            landmarkExtractor.Reset();
            cameraFps.Reset();
            landmarkFps.Reset();
            detectedFps.Reset();
        }

        public void Dispose()
        {
            tokenSource.Cancel();

            frameProducer?.Dispose();
            dataExtractor?.Dispose();
            landmarkExtractor?.Dispose();
            sequenceReader?.Dispose();
            udpSender?.Dispose();
        }
    }
}
