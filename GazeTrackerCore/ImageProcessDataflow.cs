using CppInterop.LandmarkDetector;
using GazeTrackerCore.Consumer;
using GazeTrackerCore.Consumer.Extractor;
using GazeTrackerCore.Producer;
using GazeTrackerCore.Producer.Base;
using GazeTrackerCore.Structures;
using GrazeTracker.Common;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using System.Windows.Media.Imaging;

namespace GazeTrackerCore
{
    public sealed class ImageProcessDataflow : IDisposable
    {
        public bool Paused => landmarkExtractor.Paused;
        public double CameraFps => cameraFps.Fps;
        public double LandmarkFps => landmarkFps.Fps;
        public double DetectedFps => detectedFps.Fps;

        public BroadcastBlock<DetectedData> DetectedDataBroadcast { get; }
        public BroadcastBlock<LandmarkData> LandmarkDataBroadcast { get; }
        public BroadcastBlock<WriteableBitmap> BitmapBroadcast { get; }
        public BroadcastBlock<FrameData> FrameDataBroadcast { get; }

        private readonly BitmapTransformer bitmapTransformer = new BitmapTransformer();
        private readonly FrameProducer frameProducer;
        private readonly LandmarkExtractor landmarkExtractor;
        private readonly DataExtractor dataExtractor;
        private readonly UdpSender udpSender;
        private readonly FpsDetector cameraFps;
        private readonly FpsDetector landmarkFps;
        private readonly FpsDetector detectedFps;

        public ImageProcessDataflow(Camera camera, int width, int height, IPEndPoint udpEndpoint, CancellationToken token)
        {
            var faceModelParams = new FaceModelParameters(AppDomain.CurrentDomain.BaseDirectory, true, false, false);

            frameProducer = camera.CameraType == CameraType.Webcam
                ? new WebcamFrameProducer(int.Parse(camera.Id), width, height) : null;
            //: (FrameProducer)new Ps3EyeFrameProducer(Guid.Parse(camera.Id));

            cameraFps = new FpsDetector(token);
            landmarkFps = new FpsDetector(token);
            detectedFps = new FpsDetector(token);

            udpSender = new UdpSender(udpEndpoint);
            landmarkExtractor = new LandmarkExtractor(faceModelParams);
            dataExtractor = new DataExtractor(faceModelParams);

            var cancellationDataFlowOptions = new DataflowBlockOptions { CancellationToken = token };
            DetectedDataBroadcast = new BroadcastBlock<DetectedData>(f => f, cancellationDataFlowOptions);
            LandmarkDataBroadcast = new BroadcastBlock<LandmarkData>(f => f, cancellationDataFlowOptions);
            BitmapBroadcast = new BroadcastBlock<WriteableBitmap>(f => f, cancellationDataFlowOptions);
            FrameDataBroadcast = new BroadcastBlock<FrameData>(f => f, cancellationDataFlowOptions);

            var bitmapBlock = new TransformBlock<FrameData, WriteableBitmap>(frm => bitmapTransformer.ConvertToWritableBitmap(frm.Frame), new ExecutionDataflowBlockOptions
            {
                CancellationToken = token,
                BoundedCapacity = 1,
                TaskScheduler = TaskScheduler.FromCurrentSynchronizationContext()
            });

            var landmarkBlock = new TransformBlock<FrameData, LandmarkData>(x => landmarkExtractor.DetectLandmarks(x), new ExecutionDataflowBlockOptions
            {
                CancellationToken = token,
                BoundedCapacity = 1,
            });

            var dataDetectorBlock = new TransformBlock<LandmarkData, DetectedData>(ld => dataExtractor.ExtractData(ld), new ExecutionDataflowBlockOptions
            {
                CancellationToken = token,
                BoundedCapacity = 1
            });

            var udpBlock = new ActionBlock<DetectedData>(d => udpSender.SendPoseData(d.Pose), new ExecutionDataflowBlockOptions
            {
                CancellationToken = token,
                BoundedCapacity = 1
            });

            var executionDataFlowOptions = new ExecutionDataflowBlockOptions { CancellationToken = token };
            var cameraFpsBlock = new ActionBlock<FrameData>(_ => cameraFps.AddFrame(), executionDataFlowOptions);
            var landmarkFpsBlock = new ActionBlock<LandmarkData>(_ => landmarkFps.AddFrame(), executionDataFlowOptions);
            var detectedFpsBlock = new ActionBlock<DetectedData>(_ => detectedFps.AddFrame(), executionDataFlowOptions);

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

            //Start producing frames
            Task.Factory.StartNew(() => frameProducer.ReadFrames(FrameDataBroadcast), token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void TogglePause()
        {
            landmarkExtractor.Pause();
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
            frameProducer?.Dispose();
            dataExtractor?.Dispose();
            landmarkExtractor?.Dispose();
            udpSender?.Dispose();
        }
    }
}
