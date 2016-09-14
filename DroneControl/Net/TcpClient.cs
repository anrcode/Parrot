using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;


namespace Gbi.Core.Net
{
    public class TcpClient
    {
        private Socket _socket = null;
        private EndPoint _endPoint = null;
        private int _rcvTimeout = -1;
        private int _sendTimeout = -1;
        // event to sync the async data read
        private ManualResetEvent _rcvComplete = new ManualResetEvent(false);

        public event EventHandler OnConnect;
        public event EventHandler OnError;
        public event EventHandler OnTimeout;

        /// <summary>
        /// Sets the timeout values to be used.
        /// </summary>
        /// <param name="rcvTimeout">The receive timeout in milliseconds.</param>
        /// <param name="sendTimeout">The send timeout in milliseconds.</param>
        public void SetTimeouts(int rcvTimeout, int sendTimeout)
        {
            if ((rcvTimeout == 0) || (sendTimeout == 0))
            {
                throw new InvalidOperationException("Receive and sent timeout cannot be zero!");
            }

            // remember timeouts
            _sendTimeout = sendTimeout;
            _rcvTimeout = rcvTimeout;
        }

        public TcpClient()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// Sets up a local udp socket and connects it to the server.
        /// </summary>
        /// <param name="endPoint">The end point of the remote system.</param>
        public virtual void Connect(string hostname, int port)
        {
            // ensure disconnected state
            this.Close();

            lock (this)
            {
                // remember endpoint
                _endPoint = new DnsEndPoint(hostname, port);
                // create udp client and connect it to the drone ip
                _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                _socket.Bind(new IPEndPoint(IPAddress.Any, 0));
              
                SocketAsyncEventArgs e = new SocketAsyncEventArgs();
                e.RemoteEndPoint = _endPoint;
                e.Completed += (sender, args) =>
                    {
                        if (e.SocketError == SocketError.Success)
                        {
                            // notify listeners
                            if (this.OnConnect != null)
                            {
                                this.OnConnect(this, EventArgs.Empty);
                            }
                        }
                        else
                        {
                            // notify listeners
                            this.RaiseError();
                        }
                    };

                if(!_socket.ConnectAsync(e))
                {
                    if (e.SocketError == SocketError.Success)
                    {
                        // notify listeners
                        if (this.OnConnect != null)
                        {
                            this.OnConnect(this, EventArgs.Empty);
                        }
                    }
                    else
                    {
                        // notify listeners
                        this.RaiseError();
                    }
                }
            }
        }
 
        /// <summary>
        /// Closes the socket and shuts down the receiver thread.
        /// </summary>
        public virtual void Close()
        {
            lock (this)
            {
                // close socket
                if (_socket != null)
                {
                    _socket.Close();
                    _socket = null;
                }
            }
        }

        /// <summary>
        /// Sends data over the local socket to the remote system.
        /// </summary>
        /// <param name="dgram">The data to be sent.</param>
        /// <returns>The number of bytes transferred.</returns>
        public virtual void Send(byte[] data)
        {
            if (data == null)
            {
                throw new ArgumentNullException("data");
            }

            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            e.SetBuffer(data, 0, data.Length);
            e.RemoteEndPoint = _endPoint;
            e.Completed += (sender, args) =>
                {
                    if (e.SocketError != SocketError.Success)
                    {
                        // notify listeners
                        this.RaiseError();
                    }
                };

            bool res = false;

            try
            {
                res = _socket.SendToAsync(e);
            }
            catch (Exception)
            {
                // notify listeners
                this.RaiseError();
            }

            if(!res)
            {
                if (e.SocketError != SocketError.Success)
                {
                    // notify listeners
                    this.RaiseError();
                }
            }
        }

        /// <summary>
        /// Receives data. This method is called fron within a separate thread.
        /// </summary>
        public virtual int Receive(byte[] buffer, int offset, int length)
        {
            int read = -1;
            
            // build the object for the async operation
            SocketAsyncEventArgs e = new SocketAsyncEventArgs();
            e.RemoteEndPoint = _endPoint;
            e.SetBuffer(buffer, offset, length);
            e.Completed += (sender, args) =>
                {
                    // signal the event
                    _rcvComplete.Set();

                    if (e.SocketError == SocketError.Success)
                    {
                        byte[] dgram = new byte[e.BytesTransferred];
                        Array.Copy(e.Buffer, buffer, buffer.Length);
                        read = dgram.Length;
                    }
                    else
                    {
                        // notify listeners
                        this.RaiseError();
                    }
                };

            // reset signal
            _rcvComplete.Reset();

            bool res = false;

            try
            {
                res = _socket.ReceiveFromAsync(e);
            }
            catch (Exception)
            {
                // notify listeners
                this.RaiseError();
            }

            if (!res)
            {
                if (e.SocketError == SocketError.Success)
                {
                    byte[] dgram = new byte[e.BytesTransferred];
                    Array.Copy(e.Buffer, buffer, buffer.Length);
                    read = dgram.Length;
                }
                else
                {
                    // notify listeners
                    this.RaiseError();
                }
            }
            else
            {
                // wait until event is signaled
                if (!_rcvComplete.WaitOne(_rcvTimeout))
                {
                    // notify listeners
                    if (this.OnTimeout != null)
                    {
                        this.OnTimeout(this, EventArgs.Empty);
                    }
                }
            }

            return read;
        }

        private void RaiseError()
        {
            if (this.OnError != null)
            {
                this.OnError(this, EventArgs.Empty);
            }
        }
    }
}
