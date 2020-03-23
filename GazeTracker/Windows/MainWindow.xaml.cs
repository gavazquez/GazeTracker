using CppInterop.LandmarkDetector;
using GazeTracker.Tool;
using System;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using UtilitiesOF;

namespace GazeTracker.Windows
{
    public partial class MainWindow : Window
    {
        private readonly UdpSender _udpSender;
        private readonly VideoHandler _videoHandler;
        private readonly FrameHandler _frameHandler;

        private readonly CancellationTokenSource _tokenSource = new CancellationTokenSource();

        public MainWindow()
        {
            InitializeComponent();

            var cameraSelection = new CameraSelection();
            if (!cameraSelection.no_cameras_found)
            {
                cameraSelection.ShowDialog();
            }

            if (cameraSelection.camera_selected)
            {
                var cameraId = cameraSelection.selected_camera.Item1;
                var width = cameraSelection.selected_camera.Item2;
                var height = cameraSelection.selected_camera.Item3;

                var reader = new SequenceReader(cameraId, width, height);
                if (!reader.IsOpened())
                {
                    throw new Exception("Could not start the web-cam");
                }

                _frameHandler = new FrameHandler(new FaceModelParameters(AppDomain.CurrentDomain.BaseDirectory, true, false, false));
                _videoHandler = new VideoHandler(reader, _frameHandler);
                _videoHandler.OnDetectionSucceeded += OnDetectionSucceeded;

                _udpSender = new UdpSender(5000, _videoHandler);

                var webCamImg = new OverlayImage(_videoHandler);
                webCamImg.SetValue(Grid.RowProperty, 1);
                webCamImg.SetValue(Grid.ColumnProperty, 1);
                MainGrid.Children.Add(webCamImg);

                _videoHandler.Start(_tokenSource.Token);
            }
            else
            {
                cameraSelection.Close();
                Close();
            }
        }

        private void OnDetectionSucceeded(object source, FrameArgs e)
        {
            if (e.FrameHandler.Pose.Count == 0) return;

            var pitch = (e.FrameHandler.Pose[3] * 180 / Math.PI) + 0.5;
            var yaw = (e.FrameHandler.Pose[4] * 180 / Math.PI) + 0.5;
            var roll = (e.FrameHandler.Pose[5] * 180 / Math.PI) + 0.5;

            Dispatcher.Invoke(DispatcherPriority.Render, new TimeSpan(0, 0, 0, 0, 200), (Action)(() =>
            {
                YawLabel.Content = $"{Math.Abs(yaw):0}°";
                RollLabel.Content = $"{Math.Abs(roll):0}°";
                PitchLabel.Content = $"{Math.Abs(pitch):0}°";

                YawLabelDir.Content = yaw > 0 ? "Right" : yaw < 0 ? "Left" : "Straight";
                PitchLabelDir.Content = pitch > 0 ? "Down" : pitch < 0 ? "Up" : "Straight";
                RollLabelDir.Content = roll > 0 ? "Left" : roll < 0 ? "Right" : "Straight";

                XPoseLabel.Content = $"{e.FrameHandler.Pose[0]:0} mm";
                YPoseLabel.Content = $"{e.FrameHandler.Pose[1]:0} mm";
                ZPoseLabel.Content = $"{e.FrameHandler.Pose[2]:0} mm";
            }));
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _videoHandler.Reset();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _tokenSource.Cancel();
            _videoHandler.Dispose();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _videoHandler.TogglePause();
            PauseButton.Content = _videoHandler.Pause ? "Resume" : "Pause";
        }

        private void UdpPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            _udpSender?.ChangePort(ushort.Parse(UdpPort.Text));
        }

        private void UdpPort_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !ushort.TryParse(UdpPort.Text + e.Text, out _);
        }

        private void CbxShowBox_OnChecked(object sender, RoutedEventArgs e)
        {
            if (_frameHandler != null && CbxShowBox.IsChecked.HasValue)
                _frameHandler.CalculateBox = CbxShowBox.IsChecked.Value;
        }

        private void CbxShowLandmarks_OnChecked(object sender, RoutedEventArgs e)
        {
            if (_frameHandler != null && CbxShowLandmarks.IsChecked.HasValue)
                _frameHandler.CalculateLandmarks = CbxShowLandmarks.IsChecked.Value;
        }

        private void CbxShowEyes_OnChecked(object sender, RoutedEventArgs e)
        {
            if (_frameHandler != null && CbxShowEyes.IsChecked.HasValue)
                _frameHandler.CalculateEyes = CbxShowEyes.IsChecked.Value;
        }

        private void CbxShowGazeLines_OnChecked(object sender, RoutedEventArgs e)
        {
            if (_frameHandler != null && CbxShowGazeLines.IsChecked.HasValue)
                _frameHandler.CalculateGazeLines = CbxShowGazeLines.IsChecked.Value;
        }
    }
}