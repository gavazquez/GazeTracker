using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace GazeTrackerCore.Consumer
{
    public sealed class UdpSender : IDisposable
    {
        private IPEndPoint _endpoint;
        private readonly UdpClient _client;

        public UdpSender(IPEndPoint endPoint)
        {
            _client = new UdpClient();
            _client.Connect(endPoint);
        }

        public void Connect(IPEndPoint endPoint)
        {
            _endpoint = endPoint;
            lock (_client)
            {
                _client.Connect(_endpoint);
            }
        }

        public void SendPoseData(List<float> pose)
        {
            if (pose.Count == 0) return;

            //XYZ values are in millimeters but open-track needs them in centimeters
            var udpX = pose[0] * 0.1;
            var udpY = pose[1] * 0.1;
            var udpZ = pose[2] * 0.1;

            var udpPitch = pose[3] * 180 / Math.PI * -1;
            var udpYaw = pose[4] * 180 / Math.PI;
            var udpRoll = pose[5] * 180 / Math.PI * -1;

            double[] udp_pose = { udpX, udpY, udpZ, udpYaw, udpPitch, udpRoll };
            var bytes = udp_pose.SelectMany(BitConverter.GetBytes).ToArray();
            lock (_client)
            {
                _client.Send(bytes, bytes.Length);
            }
        }

        public void Dispose()
        {
            _client.Dispose();
        }
    }
}
