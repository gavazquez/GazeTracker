using CppInterop.LandmarkDetector;
using FaceDetectorInterop;
using GazeAnalyser_Interop;
using OpenCVWrappers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using UtilitiesOF;

namespace GazeTracker
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region High-Resolution Timing

        private static DateTime startTime;
        private static readonly Stopwatch sw = new Stopwatch();

        public static DateTime CurrentTime
        {
            get { return startTime + sw.Elapsed; }
        }

        #endregion

        private readonly FpsTracker processing_fps = new FpsTracker();

        // Controls if the view should be mirrored or not
        private volatile bool mirror_image;

        // Capturing and displaying the images
        private readonly OverlayImage webcam_img;

        // Some members for displaying the results
        private WriteableBitmap latest_img;

        // For tracking
        private bool reset = false;

        // For recording
        private readonly int img_width;
        private readonly int img_height;

        private readonly CancellationTokenSource tokenSource = new CancellationTokenSource();

        private volatile bool running = true;
        private volatile bool pause;

        UdpClient client = new UdpClient();

        static MainWindow() { }

        public MainWindow()
        {
            InitializeComponent();

            startTime = DateTime.Now;
            sw.Start();
            CameraSelection cam_select = new CameraSelection();

            if (!cam_select.no_cameras_found)
            {
                cam_select.ShowDialog();
            }

            if (cam_select.camera_selected)
            {
                // Create the capture device
                int cam_id = cam_select.selected_camera.Item1;
                img_width = cam_select.selected_camera.Item2;
                img_height = cam_select.selected_camera.Item3;

                SequenceReader reader = new SequenceReader(cam_id, img_width, img_height);

                if (reader.IsOpened())
                {
                    var ep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
                    client.Connect(ep);

                    Task.Factory.StartNew(() => VideoLoop(reader, tokenSource.Token), TaskCreationOptions.LongRunning);
                }
                else
                {
                    MessageBox.Show("Failed to open a webcam", "Webcam failure", MessageBoxButton.OK, MessageBoxImage.Warning);
                    Close();
                }

                // Create an overlay image for display purposes
                webcam_img = new OverlayImage();

                webcam_img.SetValue(Grid.RowProperty, 1);
                webcam_img.SetValue(Grid.ColumnProperty, 1);
                MainGrid.Children.Add(webcam_img);
            }
            else
            {
                cam_select.Close();
                Close();
            }
        }

        private bool ProcessFrame(CLNF landmark_detector, GazeAnalyserManaged gaze_analyser, FaceModelParameters model_params, RawImage frame, RawImage grayscale_frame, float fx, float fy, float cx, float cy)
        {
            bool detection_succeeding = landmark_detector.DetectLandmarksInVideo(frame, model_params, grayscale_frame);
            gaze_analyser.AddNextFrame(landmark_detector, detection_succeeding, fx, fy, cx, cy);
            return detection_succeeding;
        }

        // Capturing and processing the video frame by frame
        private void VideoLoop(SequenceReader reader, CancellationToken token)
        {
            Thread.CurrentThread.IsBackground = true;

            string root = AppDomain.CurrentDomain.BaseDirectory;
            FaceModelParameters model_params = new FaceModelParameters(root, true, false, false);

            // Initialize the face detector
            FaceDetector face_detector = new FaceDetector(model_params.GetHaarLocation(), model_params.GetMTCNNLocation());

            // If MTCNN model not available, use HOG
            if (!face_detector.IsMTCNNLoaded())
            {
                model_params.SetFaceDetector(false, true, false);
            }

            CLNF face_model = new CLNF(model_params);
            GazeAnalyserManaged gaze_analyser = new GazeAnalyserManaged();

            DateTime? startTime = CurrentTime;

            var lastFrameTime = CurrentTime;

            while (running)
            {
                token.ThrowIfCancellationRequested();
                //////////////////////////////////////////////
                // CAPTURE FRAME AND DETECT LANDMARKS FOLLOWED BY THE REQUIRED IMAGE PROCESSING
                //////////////////////////////////////////////

                RawImage frame = reader.GetNextImage();

                lastFrameTime = CurrentTime;
                processing_fps.AddFrame();

                var grayFrame = reader.GetCurrentFrameGray();

                if (mirror_image)
                {
                    frame.Mirror();
                    grayFrame.Mirror();
                }

                bool detectionSucceeding = ProcessFrame(face_model, gaze_analyser, model_params, frame, grayFrame, reader.GetFx(), reader.GetFy(), reader.GetCx(), reader.GetCy());

                List<Tuple<Point, Point>> lines = new List<Tuple<Point, Point>>();
                List<Tuple<float, float>> eye_landmarks = new List<Tuple<float, float>>();
                List<Point> landmarks = new List<Point>();
                List<Tuple<Point, Point>> gaze_lines = new List<Tuple<Point, Point>>();
                var visibilities = face_model.GetVisibilities();
                double scale = face_model.GetRigidParams()[0];

                if (detectionSucceeding)
                {
                    foreach (var p in face_model.CalculateAllLandmarks())
                        landmarks.Add(new Point(p.Item1, p.Item2));

                    eye_landmarks = face_model.CalculateVisibleEyeLandmarks();

                    gaze_lines = gaze_analyser.CalculateGazeLines(reader.GetFx(), reader.GetFy(), reader.GetCx(), reader.GetCy());

                    lines = face_model.CalculateBox(reader.GetFx(), reader.GetFy(), reader.GetCx(), reader.GetCy());
                }

                if (reset)
                {
                    face_model.Reset();
                    reset = false;
                }

                // Visualisation updating
                try
                {
                    Dispatcher.Invoke(DispatcherPriority.Render, new TimeSpan(0, 0, 0, 0, 200), (Action)(() =>
                    {
                        if (latest_img == null)
                            latest_img = frame.CreateWriteableBitmap();

                        List<float> pose = new List<float>();
                        face_model.GetPose(pose, reader.GetFx(), reader.GetFy(), reader.GetCx(), reader.GetCy());

                        int yaw = (int)((pose[4] * 180 / Math.PI) + 0.5);
                        int yaw_abs = Math.Abs(yaw);

                        int roll = (int)((pose[5] * 180 / Math.PI) + 0.5);
                        int roll_abs = Math.Abs(roll);

                        int pitch = (int)((pose[3] * 180 / Math.PI) + 0.5);
                        int pitch_abs = Math.Abs(pitch);

                        YawLabel.Content = yaw_abs + "°";
                        RollLabel.Content = roll_abs + "°";
                        PitchLabel.Content = pitch_abs + "°";

                        YawLabelDir.Content = yaw > 0 ? "Right" : yaw < 0 ? "Left" : "Straight";
                        PitchLabelDir.Content = pitch > 0 ? "Down" : pitch < 0 ? "Up" : "Straight";
                        RollLabelDir.Content = roll > 0 ? "Left" : roll < 0 ? "Right" : "Straight";

                        XPoseLabel.Content = (int)pose[0] + " mm";
                        YPoseLabel.Content = (int)pose[1] + " mm";
                        ZPoseLabel.Content = (int)pose[2] + " mm";


                        double confidence = face_model.GetConfidence();

                        if (confidence < 0)
                            confidence = 0;
                        else if (confidence > 1)
                            confidence = 1;

                        frame.UpdateWriteableBitmap(latest_img);
                        webcam_img.Clear();

                        webcam_img.Source = latest_img;
                        webcam_img.Confidence.Add(confidence);
                        webcam_img.FPS = processing_fps.GetFPS();
                        if (detectionSucceeding)
                        {
                            webcam_img.OverlayLines.Add(lines);
                            webcam_img.OverlayPoints.Add(landmarks);
                            webcam_img.OverlayPointsVisibility.Add(visibilities);
                            webcam_img.FaceScale.Add(scale);

                            List<Point> eye_landmark_points = new List<Point>();
                            foreach (var p in eye_landmarks)
                            {
                                eye_landmark_points.Add(new Point(p.Item1, p.Item2));
                            }

                            webcam_img.OverlayEyePoints.Add(eye_landmark_points);
                            webcam_img.GazeLines.Add(gaze_lines);

                            //Pose[0] = X
                            //Pose[1] = Y
                            //Pose[2] = Z
                            //Pose[3] = pitch
                            //Pose[4] = yaw
                            //Pose[5] = roll

                            double[] udp_pose = { pose[0], pose[1], pose[2], pose[3], pose[4], pose[5] };
                            var bytes = udp_pose.SelectMany(BitConverter.GetBytes).ToArray();
                            lock (client)
                            {
                                client.Send(bytes, bytes.Length);
                            }
                        }
                    }));

                    while (running && pause)
                    {
                        Thread.Sleep(10);
                    }
                }
                catch (TaskCanceledException)
                {
                    // Quitting
                    break;
                }
            }

            reader.Close();
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            reset = true;
        }

        private void MirrorButton_Click(object sender, RoutedEventArgs e)
        {
            mirror_image = !mirror_image;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Stop capture and tracking
            running = false;
            tokenSource.Cancel();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            pause = !pause;
            PauseButton.Content = pause ? "Resume" : "Pause";
        }

        private void UdpPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            var ep = new IPEndPoint(IPAddress.Loopback, ushort.Parse(UdpPort.Text));
            lock (client)
            {
                client.Connect(ep);
            }
        }

        private void UdpPort_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !ushort.TryParse(UdpPort.Text + e.Text, out _);
        }
    }
}