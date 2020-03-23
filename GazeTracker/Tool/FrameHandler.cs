using CppInterop.LandmarkDetector;
using FaceDetectorInterop;
using GazeAnalyser_Interop;
using OpenCVWrappers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using UtilitiesOF;

namespace GazeTracker.Tool
{
    public sealed class FrameHandler : IDisposable
    {
        private readonly CLNF _faceModel;
        private readonly FaceModelParameters _modelParams;
        private readonly GazeAnalyserManaged _gazeAnalyzer;

        public double Scale;
        public double Confidence;
        public List<bool> Visibilities = new List<bool>();
        public List<Tuple<Point, Point>> Lines = new List<Tuple<Point, Point>>();
        public List<Tuple<float, float>> EyeLandmarks = new List<Tuple<float, float>>();
        public List<Point> Landmarks = new List<Point>();
        public List<Tuple<Point, Point>> GazeLines = new List<Tuple<Point, Point>>();
        public List<float> Pose = new List<float>();

        public bool CalculateEyes { get; set; } = true;
        public bool CalculateGazeLines { get; set; } = true;
        public bool CalculateLandmarks { get; set; } = true;
        public bool CalculateBox { get; set; } = true;

        public FrameHandler(FaceModelParameters faceModelParameters)
        {
            _modelParams = faceModelParameters;
            _gazeAnalyzer = new GazeAnalyserManaged();

            var face_detector = new FaceDetector(_modelParams.GetHaarLocation(), _modelParams.GetMTCNNLocation());
            if (!face_detector.IsMTCNNLoaded()) // If MTCNN model not available, use HOG
            {
                _modelParams.SetFaceDetector(false, true, false);
            }

            _faceModel = new CLNF(_modelParams);
        }

        public void Clear()
        {
            Confidence = 0;
            Lines.Clear();
            EyeLandmarks.Clear();
            Landmarks.Clear();
            GazeLines.Clear();
            Visibilities.Clear();
        }

        public void Reset()
        {
            _faceModel.Reset();
        }

        public bool DetectLandmarks(SequenceReader reader, RawImage frame)
        {
            var grayFrame = reader.GetCurrentFrameGray();

            var detectionSuccessful = _faceModel.DetectLandmarksInVideo(frame, _modelParams, grayFrame);
            _gazeAnalyzer.AddNextFrame(_faceModel, detectionSuccessful, reader.GetFx(), reader.GetFy(), reader.GetCx(), reader.GetCy());

            return detectionSuccessful;
        }

        public void DetectionParameters(SequenceReader reader)
        {
            Clear();
            Scale = _faceModel.GetRigidParams()[0];

            var fx = reader.GetFx();
            var fy = reader.GetFy();
            var cx = reader.GetCx();
            var cy = reader.GetCy();

            if (CalculateLandmarks)
            {
                Visibilities.AddRange(_faceModel.GetVisibilities());
                Landmarks.AddRange(_faceModel.CalculateAllLandmarks().Select(p => new Point(p.Item1, p.Item2)));
            }
            if (CalculateEyes)
                EyeLandmarks.AddRange(_faceModel.CalculateVisibleEyeLandmarks());
            if (CalculateGazeLines)
                GazeLines.AddRange(_gazeAnalyzer.CalculateGazeLines(fx, fy, cx, cy));
            if (CalculateBox)
                Lines.AddRange(_faceModel.CalculateBox(fx, fy, cx, cy));

            _faceModel.GetPose(Pose, fx, fy, cx, cy);
            Confidence = _faceModel.GetConfidence();
            Confidence = Confidence < 0 ? 0 : Confidence > 1 ? 1 : Confidence;
        }

        public void Dispose()
        {
            _faceModel?.Dispose();
            _modelParams?.Dispose();
            _gazeAnalyzer?.Dispose();
        }
    }
}
