using CppInterop.LandmarkDetector;
using GazeTrackerCore.Settings;
using GazeTrackerCore.Structures;

namespace GazeTrackerCore.Consumer.Extractor
{
    public sealed class LandmarkExtractor : ExtractorBase
    {
        public LandmarkExtractor(FaceModelParameters faceModelParameters) : base(faceModelParameters)
        {
        }

        public LandmarkData DetectLandmarks(FrameData frame)
        {
            var detectionSuccessful = FaceModel.DetectLandmarksInVideo(frame.Frame, ModelParams, frame.GrayFrame);

            if (DetectionSettings.CalculateGazeLines)
                GazeAnalyzer.AddNextFrame(FaceModel, detectionSuccessful, frame.Fx, frame.Fy, frame.Cx, frame.Cy);

            return new LandmarkData(frame, FaceModel, detectionSuccessful);
        }
    }
}
