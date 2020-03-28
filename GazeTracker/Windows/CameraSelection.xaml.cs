using GazeTrackerCore.Lister;
using GrazeTracker.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace GazeTracker.Windows
{
    public partial class CameraSelection : Window
    {
        public Camera SelectedCamera { get; private set; }

        private List<Border> sample_images;
        private List<ComboBox> combo_boxes;

        public void PopulateCameraSelections()
        {
            KeyDown += CameraSelection_KeyDown;

            var cameraList = CameraLister.GetAvailableCameras();

            sample_images = new List<Border>();

            // Each cameras corresponding resolutions
            combo_boxes = new List<ComboBox>();

            foreach (var camera in cameraList.Select((value, i) => new { i, value }))
            {
                camera.value.Index = camera.i;
                var bitmap = camera.value.Image.CreateWriteableBitmap();
                camera.value.Image.UpdateWriteableBitmap(bitmap);
                bitmap.Freeze();

                Dispatcher.Invoke(() =>
                {
                    Image img = new Image();
                    img.Source = bitmap;
                    img.Margin = new Thickness(5);

                    ColumnDefinition col_def = new ColumnDefinition();
                    ThumbnailPanel.ColumnDefinitions.Add(col_def);

                    Border img_border = new Border();
                    img_border.SetValue(Grid.ColumnProperty, camera.value.Index);
                    img_border.SetValue(Grid.RowProperty, 0);
                    img_border.CornerRadius = new CornerRadius(5);

                    StackPanel img_panel = new StackPanel();

                    Label camera_name_label = new Label();
                    camera_name_label.Content = camera.value.Name;
                    camera_name_label.HorizontalAlignment = HorizontalAlignment.Center;
                    img_panel.Children.Add(camera_name_label);
                    img.Height = 200;
                    img_panel.Children.Add(img);
                    img_border.Child = img_panel;

                    sample_images.Add(img_border);

                    ThumbnailPanel.Children.Add(img_border);

                    ComboBox resolutions = new ComboBox();
                    resolutions.Width = 80;
                    combo_boxes.Add(resolutions);

                    foreach (var r in camera.value.Resolutions)
                    {
                        resolutions.Items.Add(r.Item1 + "x" + r.Item2);
                    }

                    resolutions.SelectedIndex = camera.value.Resolutions.IndexOf(camera.value.SelectedResolution);
                    resolutions.SetValue(Grid.ColumnProperty, camera.i);
                    resolutions.SetValue(Grid.RowProperty, 2);
                    ThumbnailPanel.Children.Add(resolutions);

                    img_panel.MouseDown += (sender, e) => HighlightCamera(camera.value);
                    resolutions.DropDownOpened += (sender, e) => HighlightCamera(camera.value);
                    resolutions.DropDownClosed += (sender, e) => ResolutionChanged(camera.value, resolutions);
                });
            }

            var firstCamera = cameraList.FirstOrDefault();
            if (firstCamera != null)
            {
                Dispatcher.Invoke(DispatcherPriority.Render, new TimeSpan(0, 0, 0, 0, 200), (Action)(() => HighlightCamera(firstCamera)));
            }
            else
            {
                MessageBox.Show("No cameras detected, please connect a webcam", "Camera error!", MessageBoxButton.OK, MessageBoxImage.Warning);
                Dispatcher.Invoke(DispatcherPriority.Render, new TimeSpan(0, 0, 0, 0, 200), (Action)Close);
            }
        }

        private void ResolutionChanged(Camera camera, ComboBox resolutions)
        {
            camera.SelectedResolution = camera.Resolutions[resolutions.SelectedIndex];
        }

        public CameraSelection()
        {
            InitializeComponent();

            // We want to display the loading screen first
            Thread load_cameras = new Thread(LoadCameras);
            load_cameras.Start();
        }

        public void LoadCameras()
        {
            Thread.CurrentThread.IsBackground = true;
            PopulateCameraSelections();

            Dispatcher.Invoke(DispatcherPriority.Render, new TimeSpan(0, 0, 0, 0, 200), (Action)(() =>
            {
                LoadingGrid.Visibility = Visibility.Hidden;
                camerasPanel.Visibility = Visibility.Visible;
            }));
        }

        private void HighlightCamera(Camera camera)
        {
            foreach (var img in sample_images)
            {
                img.BorderThickness = new Thickness(1);
                img.BorderBrush = Brushes.Gray;
            }
            sample_images[camera.Index].BorderThickness = new Thickness(4);
            sample_images[camera.Index].BorderBrush = Brushes.Green;
            SelectedCamera = camera;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CameraSelection_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Close();
            }
        }
    }
}
