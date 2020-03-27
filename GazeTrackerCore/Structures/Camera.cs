using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace GrazeTracker.Common
{
    public enum CameraType
    {
        Webcam,
        Ps3Eye
    }

    public class Camera
    {
        public string Id { get; set; }
        public int Index { get; set; }
        public string Name { get; set; }
        public Tuple<int, int> SelectedResolution { get; set; }
        public List<Tuple<int, int>> Resolutions { get; set; }
        public ImageSource Image { get; }
        public CameraType CameraType { get; }

        public Camera(string id, int index, string name, List<Tuple<int, int>> resolutions, ImageSource img, CameraType cameraType)
        {
            img.Freeze();

            Id = id;
            Index = index;
            Name = name;
            Resolutions = resolutions.Where(i => i.Item1 > 0 && i.Item2 > 0).ToList();
            Image = img;
            SelectedResolution = Resolutions.OrderBy(c => c.Item1).First();
            CameraType = cameraType;
        }
    }
}
