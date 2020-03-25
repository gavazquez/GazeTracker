using CppInterop.LandmarkDetector;

namespace GazeTrackerCore.Structures
{
    public class LandmarkData
    {
        public FrameData FrameData { get; }
        public CLNF FaceModel { get; }
        public bool DetectionSucceeded { get; }

        public LandmarkData(FrameData frame, CLNF faceModel, bool detectionSucceeded)
        {
            FrameData = frame;
            FaceModel = faceModel;
            DetectionSucceeded = detectionSucceeded;
        }
    }
}
