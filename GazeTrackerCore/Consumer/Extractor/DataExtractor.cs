using CppInterop.LandmarkDetector;
using GazeTrackerCore.Settings;
using GazeTrackerCore.Structures;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace GazeTrackerCore.Consumer.Extractor
{
    public class DataExtractor : ExtractorBase
    {
        public DataExtractor(FaceModelParameters faceModelParameters) : base(faceModelParameters)
        {
        }

        public DetectedData ExtractData(LandmarkData landmarkData)
        {
            var confidence = FaceModel.GetConfidence();
            var pose = new List<float>();
            FaceModel.GetPose(pose, landmarkData.FrameData.Fx, landmarkData.FrameData.Fy, landmarkData.FrameData.Cx, landmarkData.FrameData.Cy);

            var data = new DetectedData
            {
                Scale = FaceModel.GetRigidParams()[0],
                Pose = pose,
                Confidence = confidence < 0 ? 0 : confidence > 1 ? 1 : confidence
            };

            if (DetectionSettings.CalculateLandmarks)
            {
                data.Visibilities = FaceModel.GetVisibilities();
                data.Landmarks = FaceModel.CalculateAllLandmarks().Select(p => new Point(p.Item1, p.Item2)).ToList();
            }

            if (DetectionSettings.CalculateEyes)
                data.EyeLandmarks = FaceModel.CalculateVisibleEyeLandmarks();

            if (DetectionSettings.CalculateGazeLines)
                data.GazeLines = GazeAnalyzer.CalculateGazeLines(landmarkData.FrameData.Fx, landmarkData.FrameData.Fy, landmarkData.FrameData.Cx, landmarkData.FrameData.Cy);

            if (DetectionSettings.CalculateBox)
                data.BoxLines = FaceModel.CalculateBox(landmarkData.FrameData.Fx, landmarkData.FrameData.Fy, landmarkData.FrameData.Cx, landmarkData.FrameData.Cy);

            return data;
        }
    }
}
