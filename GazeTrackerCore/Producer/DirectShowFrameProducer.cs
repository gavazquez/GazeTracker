using AForge.Video;
using AForge.Video.DirectShow;
using GazeTrackerCore.Producer.Base;
using GazeTrackerCore.Structures;
using OpenCVWrappers;
using System.Linq;
using System.Threading.Tasks.Dataflow;

namespace GazeTrackerCore.Producer
{
	public class DirectShowFrameProducer : FrameProducer
	{
		private int _width = 640;
		private int _height = 480;
		private BroadcastBlock<FrameData> _broadcast;
		private VideoCaptureDevice _videoSource;

		public DirectShowFrameProducer(string monikerString, int width, int height)
		{
			_videoSource = new VideoCaptureDevice(monikerString);

			_width = width;
			_height = height;
			var capability = _videoSource.VideoCapabilities.Where(c => c.FrameSize.Width == width && c.FrameSize.Height == height);
			_videoSource.VideoResolution = capability.First();

			_videoSource.NewFrame += NewFrameEvent;
			_videoSource.Start();
		}

		private void NewFrameEvent(object sender, NewFrameEventArgs eventArgs)
		{
			_broadcast?.Post(new FrameData(new RawImage(eventArgs.Frame), null, GetFx(), GetFy(), GetCx(), GetCy()));
		}

		public override void Dispose()
		{
			base.Dispose();
			_videoSource.SignalToStop();
		}

		public override void ReadFrames(BroadcastBlock<FrameData> broadcast)
		{
			_broadcast = broadcast;
		}

		protected override RawImage GetNextFrame() => null;
		protected override RawImage GetGrayFrame() => null;

		protected override float GetFx() => 500.0f * (_width / 640.0f);
		protected override float GetFy() => 500.0f * (_height / 480.0f);
		protected override float GetCx() => _width / 2.0f;
		protected override float GetCy() => _height / 2.0f;
	}
}
