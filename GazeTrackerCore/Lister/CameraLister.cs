using AForge.Video;
using AForge.Video.DirectShow;
using GrazeTracker.Common;
using OpenCVWrappers;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using UtilitiesOF;

namespace GazeTrackerCore.Lister
{
    public static class CameraLister
    {
        private static Bitmap bitmap;

        public static IEnumerable<Camera> GetAvailableCameras()
        {
            //Only list PS3Eye cameras
            foreach (var device in new FilterInfoCollection(FilterCategory.VideoInputDevice).Cast<FilterInfo>().Where(d => d.Name.StartsWith("PS3")))
            {
                VideoCaptureDevice videoSource = new VideoCaptureDevice(device.MonikerString);
                videoSource.NewFrame += NewFrame;
                videoSource.Start();

                var resolutions = new List<Tuple<int, int>>();
                foreach (var cap in videoSource.VideoCapabilities)
                {
                    resolutions.Add(new Tuple<int, int>(cap.FrameSize.Width, cap.FrameSize.Height));
                }

                while (bitmap == null) Thread.Sleep(50);

                videoSource.SignalToStop();
                videoSource.WaitForStop();

                var camera = new Camera(device.MonikerString, device.Name, resolutions, new RawImage(new Bitmap(bitmap)), CameraType.Ps3Eye);
                bitmap = null;

                yield return camera;
            }

            //Do not list PS3Eye camera with OpenFace as it's limited to 30FPS
            foreach (var camera in SequenceReader.GetCameras(AppDomain.CurrentDomain.BaseDirectory).Where(d => !d.Item2.StartsWith("PS3"))
                .Select(c => new Camera(c.Item1.ToString(), c.Item2, c.Item3, c.Item4, CameraType.Webcam)))
            {
                yield return camera;
            }
        }

        private static void NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            bitmap = (Bitmap)eventArgs.Frame.Clone();
        }
    }
}
