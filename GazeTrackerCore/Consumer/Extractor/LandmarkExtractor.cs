using CppInterop.LandmarkDetector;
using GazeTrackerCore.Settings;
using GazeTrackerCore.Structures;

namespace GazeTrackerCore.Consumer.Extractor
{
    public sealed class LandmarkExtractor : ExtractorBase
    {
        public volatile bool Paused;

        public LandmarkExtractor(FaceModelParameters faceModelParameters) : base(faceModelParameters)
        {
        }

        public LandmarkData DetectLandmarks(FrameData frame)
        {
            if (Paused) return new LandmarkData(frame, FaceModel, false);

            var detectionSuccessful = FaceModel.DetectLandmarksInVideo(frame.Frame, ModelParams, frame.GrayFrame);

            if (DetectionSettings.CalculateGazeLines)
                GazeAnalyzer.AddNextFrame(FaceModel, detectionSuccessful, frame.Fx, frame.Fy, frame.Cx, frame.Cy);

            return new LandmarkData(frame, FaceModel, detectionSuccessful);
        }

        public void Pause()
        {
            Paused = !Paused;
        }
    }
}
