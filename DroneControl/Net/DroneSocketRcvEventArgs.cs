using System;
using System.Net;


namespace Parrot.DroneControl.Net
{
    /// <summary>
    /// Contains data sent from a socket.
    /// </summary>
    internal class DroneSocketRcvEventArgs : EventArgs
    {
        /// <summary>
        /// The remote endpoint that send the data.
        /// </summary>
        public EndPoint EndPoint { get; private set; }
        /// <summary>
        /// The data bits received.
        /// </summary>
        public byte[] Data { get; private set; }


        public DroneSocketRcvEventArgs(EndPoint endPoint, byte[] dgram)
        {
            this.EndPoint = endPoint;
            this.Data = dgram;
        }
    }
}
