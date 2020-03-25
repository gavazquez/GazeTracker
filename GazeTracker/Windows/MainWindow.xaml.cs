using GazeTrackerCore;
using GazeTrackerCore.Settings;
using GazeTrackerCore.Structures;
using System;
using System.Net;
using System.Threading.Tasks.Dataflow;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace GazeTracker.Windows
{
    public partial class MainWindow : Window
    {
        private readonly ImageProcessDataflow _processDataFlow;

        private ushort Port => ushort.Parse(UdpPort.Text);

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

                _processDataFlow = new ImageProcessDataflow(cameraId, width, height, new IPEndPoint(IPAddress.Loopback, Port));

                var webCamImg = new OverlayImage(_processDataFlow);
                webCamImg.SetValue(Grid.RowProperty, 1);
                webCamImg.SetValue(Grid.ColumnProperty, 1);
                MainGrid.Children.Add(webCamImg);

                var updateLabelsBlock = new ActionBlock<DetectedData>(UpdateLabelsWithData);
                _processDataFlow.DetectedDataBroadcast.LinkTo(updateLabelsBlock);

                _processDataFlow.Start();
            }
            else
            {
                cameraSelection.Close();
                Close();
            }
        }

        private void UpdateLabelsWithData(DetectedData data)
        {
            if (data.Pose.Count == 0) return;

            var pitch = (data.Pose[3] * 180 / Math.PI) + 0.5;
            var yaw = (data.Pose[4] * 180 / Math.PI) + 0.5;
            var roll = (data.Pose[5] * 180 / Math.PI) + 0.5;

            Dispatcher.Invoke(DispatcherPriority.Render, new TimeSpan(0, 0, 0, 0, 200), (Action)(() =>
            {
                YawLabel.Content = $"{Math.Abs(yaw):0}°";
                RollLabel.Content = $"{Math.Abs(roll):0}°";
                PitchLabel.Content = $"{Math.Abs(pitch):0}°";

                YawLabelDir.Content = yaw > 0 ? "Right" : yaw < 0 ? "Left" : "Straight";
                PitchLabelDir.Content = pitch > 0 ? "Down" : pitch < 0 ? "Up" : "Straight";
                RollLabelDir.Content = roll > 0 ? "Left" : roll < 0 ? "Right" : "Straight";

                XPoseLabel.Content = $"{data.Pose[0]:0} mm";
                YPoseLabel.Content = $"{data.Pose[1]:0} mm";
                ZPoseLabel.Content = $"{data.Pose[2]:0} mm";
            }));
        }

        private void ResetButton_Click(object sender, RoutedEventArgs e)
        {
            _processDataFlow.Reset();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _processDataFlow.Dispose();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            _processDataFlow.TogglePause();
            PauseButton.Content = _processDataFlow.Paused ? "Resume" : "Pause";
        }

        private void UdpPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            _processDataFlow?.ChangeUdpEndpoint(new IPEndPoint(IPAddress.Loopback, Port));
        }

        private void UdpPort_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !ushort.TryParse(UdpPort.Text + e.Text, out _);
        }

        private void CbxShowBox_OnChecked(object sender, RoutedEventArgs e)
        {
            if (CbxShowBox.IsChecked.HasValue)
                DetectionSettings.CalculateBox = CbxShowBox.IsChecked.Value;
        }

        private void CbxShowLandmarks_OnChecked(object sender, RoutedEventArgs e)
        {
            if (CbxShowLandmarks.IsChecked.HasValue)
                DetectionSettings.CalculateLandmarks = CbxShowLandmarks.IsChecked.Value;
        }

        private void CbxShowEyes_OnChecked(object sender, RoutedEventArgs e)
        {
            if (CbxShowEyes.IsChecked.HasValue)
                DetectionSettings.CalculateEyes = CbxShowEyes.IsChecked.Value;
        }

        private void CbxShowGazeLines_OnChecked(object sender, RoutedEventArgs e)
        {
            if (CbxShowGazeLines.IsChecked.HasValue)
                DetectionSettings.CalculateGazeLines = CbxShowGazeLines.IsChecked.Value;
        }
    }
}