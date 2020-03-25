using OpenCVWrappers;
using System;

namespace GazeTrackerCore.Structures
{
    public class FrameData
    {
        public long Time { get; }
        public RawImage Frame { get; }
        public RawImage GrayFrame { get; }
        public float Fx { get; }
        public float Fy { get; }
        public float Cx { get; }
        public float Cy { get; }

        public FrameData(RawImage frame, RawImage grayFrame, float fx, float fy, float cx, float cy)
        {
            Time = DateTimeOffset.Now.Ticks;
            Frame = frame;
            GrayFrame = grayFrame;
            Fx = fx;
            Fy = fy;
            Cx = cx;
            Cy = cy;
        }
    }
}
