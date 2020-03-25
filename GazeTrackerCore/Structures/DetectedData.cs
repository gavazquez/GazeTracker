using System;
using System.Collections.Generic;
using System.Windows;

namespace GazeTrackerCore.Structures
{
    public class DetectedData
    {
        public double Scale;
        public double Confidence;
        public List<bool> Visibilities = new List<bool>();
        public List<Tuple<Point, Point>> BoxLines = new List<Tuple<Point, Point>>();
        public List<Tuple<float, float>> EyeLandmarks = new List<Tuple<float, float>>();
        public List<Point> Landmarks = new List<Point>();
        public List<Tuple<Point, Point>> GazeLines = new List<Tuple<Point, Point>>();
        public List<float> Pose = new List<float>();
    }
}
