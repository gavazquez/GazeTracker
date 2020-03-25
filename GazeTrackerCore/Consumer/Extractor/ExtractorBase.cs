using CppInterop.LandmarkDetector;
using FaceDetectorInterop;
using GazeAnalyser_Interop;
using System;

namespace GazeTrackerCore.Consumer.Extractor
{
    public abstract class ExtractorBase : IDisposable
    {
        private static volatile bool _initialized;

        protected static CLNF FaceModel { get; set; }
        protected static FaceModelParameters ModelParams { get; set; }
        protected static GazeAnalyserManaged GazeAnalyzer { get; set; }

        protected ExtractorBase(FaceModelParameters faceModelParameters)
        {
            if (_initialized) return;

            ModelParams = faceModelParameters;
            GazeAnalyzer = new GazeAnalyserManaged();

            var face_detector = new FaceDetector(ModelParams.GetHaarLocation(), ModelParams.GetMTCNNLocation());
            if (!face_detector.IsMTCNNLoaded()) // If MTCNN model not available, use HOG
            {
                ModelParams.SetFaceDetector(false, true, false);
            }

            FaceModel = new CLNF(ModelParams);
            _initialized = true;
        }

        public void Dispose()
        {
            FaceModel?.Dispose();
            ModelParams?.Dispose();
            GazeAnalyzer?.Dispose();
        }

        public void Reset()
        {
            FaceModel.Reset();
        }
    }
}
