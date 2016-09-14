using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace Parrot.DroneControl.Net
{
    /// <summary>
    /// This class implements the base communication channel (socket) with
    /// the drone.
    /// </summary>
    internal class DroneTcpSocket : Gbi.Core.Net.TcpClient
    {
        private byte[] _rcvBuffer = new byte[64000];
        private bool _isConnected = false;
        private Thread _rcvThread = null;

        // public events
        public event EventHandler<DroneSocketRcvEventArgs> OnDataReceived;


        /// <summary>
        /// Default constructor.
        /// </summary>
        public DroneTcpSocket()
        {
            this.OnConnect += (sender, args) =>
                {
                    // set is connected
                    _isConnected = true;
                    // create and start thread
                    _rcvThread = new Thread(ReceiveData);
                    _rcvThread.Start();
                };
        }

        /// <summary>
        /// Closes the socket and shuts down the receiver thread.
        /// </summary>
        public override void Close()
        {
            // allow receive thread to exit
            _isConnected = false;

            base.Close();
        }

        /// <summary>
        /// Receives data from the socket synchronousely with timeout.
        /// </summary>
        protected void ReceiveData()
        {
            while (_isConnected)
            {
                int read = this.Receive(_rcvBuffer, 0, _rcvBuffer.Length);
                byte[] dgram = new byte[read];
                Array.Copy(_rcvBuffer, dgram, dgram.Length);
                this.OnDataReceived(this, new DroneSocketRcvEventArgs(null, dgram));
            }
        }
    }
}
