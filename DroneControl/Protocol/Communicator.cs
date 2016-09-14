using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;

using Parrot.DroneControl.IO;
using Parrot.DroneControl.Net;
using Parrot.DroneControl.Video;


namespace Parrot.DroneControl.Protocol
{
    internal class Communicator
    {
        private string _droneIpHost = null;
        private IPAddress _droneIp = null;
        private DroneUdpSocket _cmdSocket = null;
        private DroneUdpSocket _dataSocket = null;
        private DroneUdpSocket _vidSocket = null;
        private DroneTcpSocket _ctlSocket = null;
        private Thread _cmdSendThread = null;
        // holds the state of the drone land/takeoff/emergency
        private uint _inputValue = 0;
        // holds the commands
        private uint _cmdSeqNo = 1;
        private Queue<AtCommand> _cmdQueue = new Queue<AtCommand>();        
        private Queue<ConfigCommand> _cfgQueue = new Queue<ConfigCommand>();
        // holds the navigation data
        private DateTime _lastDataReceived = DateTime.MinValue;
        private uint _dataSeqNo = 0;
        // holds the captured video image
        private IDroneVideoDecoder _vidImage = null;
        private bool _isCmdConnected;
        // multiconfig
        private uint _appId = 0;
        private uint _userId = 0;
        private uint _sessionId = 0;

        // low level events
        public event EventHandler<DroneStatusEventArgs> OnDroneStatus;
        public event EventHandler<DroneDataEventArgs> OnDroneData;
        public event EventHandler<VisionDetectEventArgs> OnVisionDetect;
        public event EventHandler<DroneImageCompleteEventArgs> OnDroneImage;
        public event EventHandler OnCommFailure;
        public event EventHandler OnConfigComplete;

        // properties
        public IDroneInput DroneInput { get; internal set; }


        internal Communicator(IDroneVideoDecoder decoder)
        {
            // bits 28,24,22,20 and 18 should always be set to 1
            _inputValue = 0x11540000;
            // wire the basic drone status handler
            this.OnDroneStatus += new EventHandler<DroneStatusEventArgs>(OnDroneStatusHandler);
            // create new image decoder
            _vidImage = decoder;
            _vidImage.ImageComplete += new EventHandler<DroneImageCompleteEventArgs>(ImageCompleteHandler); 
        }

        /// <summary>
        /// Connects to the drone.
        /// </summary>
        /// <param name="droneIp">The IP address of the drone.</param>
        public void Connect(string droneIp)
        {
            lock (this)
            {
                _droneIpHost = droneIp;
                // get address of the drone
                _droneIp = IPAddress.Parse(droneIp);
                // ensure disconnected state
                this.Disconnect();
                // setup command socket
                _cmdSocket = new DroneUdpSocket();
                _cmdSocket.OnError += new EventHandler(OnSocketErrorHandler);
                _cmdSocket.SetTimeouts(-1, 500);
                _cmdSocket.Connect(droneIp, 5556);              
                // setup data socket
                _dataSocket = new DroneUdpSocket();
                _dataSocket.OnDataReceived += new EventHandler<DroneSocketRcvEventArgs>(OnDataReceivedHandler);
                _dataSocket.OnTimeout += new EventHandler(OnTimeoutHandler);
                _dataSocket.OnError += new EventHandler(OnSocketErrorHandler);
                _dataSocket.SetTimeouts(2500, 500);
                _dataSocket.Connect(droneIp, 5554);
                _dataSocket.Send(new byte[] { 0x1, 0x0, 0x0, 0x0 });
                //_dataSocket.Connect(5554, new IPEndPoint(IPAddress.Parse("224.1.1.1"), 5554), true);
                //_dataSocket.Send(UTF8.GetBytes("Init"));

                //experimental
                try
                {
                    _ctlSocket = new DroneTcpSocket();
                    _ctlSocket.OnDataReceived += new EventHandler<DroneSocketRcvEventArgs>(OnConfigReceivedHandler);
                    _ctlSocket.OnError += new EventHandler(OnSocketErrorHandler);
                    _ctlSocket.Connect(droneIp, 5559);
                }
                catch (Exception ex)
                {
                    Logger.Log(LogScope.Error, ex.Message);
                }
                
                // we're now in the connected state
                _isCmdConnected = true;
                // setup and start command sender thread
                _cmdSendThread = new Thread(SendCommand);
                _cmdSendThread.Start();
            }
        }       

        protected void OnTimeoutHandler(object sender, EventArgs e)
        {
            // log message
            Logger.Log(LogScope.Warning, "Network timeout detected!");
            // close all connections
            this.Disconnect();
            // notify listeners
            if (this.OnCommFailure != null)
            {
                this.OnCommFailure(this, EventArgs.Empty);
            }
        }

        protected void OnSocketErrorHandler(object sender, EventArgs e)
        {
            // log message
            Logger.Log(LogScope.Error, "Socket error in communication with drone!");
            // close all connections
            this.Disconnect();
            // notify listeners
            if (this.OnCommFailure != null)
            {
                this.OnCommFailure(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Handles the reception of navdata messages.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        protected void OnDataReceivedHandler(object sender, DroneSocketRcvEventArgs e)
        {         
            // try to decode the data bytes
            NavigationData navData = NavigationData.Parse(e.Data);
            if (!navData.ContainsValidData)
            {
                // log message
                Logger.Log(LogScope.Error, "Received invalid navdata from drone!");
                // ignore data
                return;
            }
            
            // special case, drone resetted the sequence number, check for burst data
            if (DateTime.Now.Subtract(_lastDataReceived).TotalMilliseconds < 10)
            {
                // log message
                Logger.Log(LogScope.Warning, "Drone burst status sends detected!");
                // ignore data
                return;
            }

            if (navData.Header.Sequence == 1)
            {
                // log message
                Logger.Log(LogScope.Warning, "Drone reset the sequence number!");
            }
            else if (navData.Header.Sequence <= _dataSeqNo)
            {
                // log message
                Logger.Log(LogScope.Warning, "Ignoring duplicate packet from drone (" + navData.Header.Sequence + ")!");
                // ignore data
                return;
            }

            // remember time of last received data
            _lastDataReceived = DateTime.Now;
            // remember last sequence
            _dataSeqNo = navData.Header.Sequence;        
            // log message
            Logger.Log(LogScope.Debug, "Got drone status (" + navData.Header.DroneStatus + ")...");

            // fire drone status
            if (this.OnDroneStatus != null)
            {
                this.OnDroneStatus(this, new DroneStatusEventArgs(navData.Header.DroneStatus));
            }

            // fire navdata
            if (this.OnDroneData != null)
            {
                this.OnDroneData(this, new DroneDataEventArgs(navData.DroneData));
            }

            // fire vision detect
            if (this.OnVisionDetect != null)
            {
                this.OnVisionDetect(this, new VisionDetectEventArgs(navData.VisionDetect));
            }
        }

        /// <summary>
        /// Implements the basic drone status handling.
        /// </summary>
        /// <param name="e">The drone status to handle</param>
        protected void OnDroneStatusHandler(object sender, DroneStatusEventArgs e)
        {
            try
            {
                // reset commwatchdog
                if (e.ComWatchdog)
                {
                    // com watchdog timed out, reset hub
                    this.ResetCommHub();
                }

                // if there are any configs, send the next one
                if (_cfgQueue.Count > 0)
                {
                    if (!e.HasCommandReceived)
                    {
                        // peek the next configured value
                        ConfigCommand cfg = _cfgQueue.Peek();
                        // send the config to the drone
                        this.SetConfiguration(cfg);
                    }
                    else
                    {
                        // dequeue config just after we go the ack from the drone!
                        _cfgQueue.Dequeue();
                        // notify listeners
                        if ((this.OnConfigComplete != null) && (_cfgQueue.Count == 0))
                        {
                            this.OnConfigComplete(this, EventArgs.Empty);
                        }
                    }
                }

                if (e.HasCommandReceived)
                {
                    // reset ack
                    this.AckControlMode();
                }
            }
            catch (DroneNotConnectedException)
            {
                Logger.Log(LogScope.Error, "Drone not connected anymore!");
            }
        }

        protected void OnConfigReceivedHandler(object sender, DroneSocketRcvEventArgs e)
        {
            if (e.Data == null) return;

            string configData = Encoding.UTF8.GetString(e.Data, 0, e.Data.Length);
            string[] configLines = configData.Split('\n');
            foreach (string line in configLines)
            {
                Logger.Log(LogScope.Info, "len:" + e.Data.Length + "   " + line);
            }
        }

        /// <summary>
        /// Disconnects from the drone.
        /// </summary>
        public void Disconnect()
        {
            // control disconnect
            _isCmdConnected = false;

            lock (this)
            {
                // stop video
                this.StopVideo();

                // close and dispose sockets
                if (_cmdSocket != null)
                {
                    _cmdSocket.Close();
                    _cmdSocket = null;
                }

                if (_dataSocket != null)
                {
                    _dataSocket.Close();
                    _dataSocket = null;
                }

                if (_ctlSocket != null)
                {
                    _ctlSocket.Close();
                    _ctlSocket = null;
                }

                // clear queue
                this.ClearQueues();

                // reset sequences
                _cmdSeqNo = 1;
                _dataSeqNo = 0;
            }
        }

        /// <summary>
        /// Starts the video transmission fron the drone.
        /// </summary>
        public void StartVideo()
        {
            lock (this)
            {
                // ensure stopped video first
                this.StopVideo();               
                // create new receiver socket
                _vidSocket = new DroneUdpSocket();
                _vidSocket.OnDataReceived += new EventHandler<DroneSocketRcvEventArgs>(OnVideoReceivedHandler);
                _vidSocket.SetTimeouts(2000, 250);
                _vidSocket.Connect(_droneIpHost, 5555);
                _vidSocket.Send(new byte[] { 0x1, 0x0, 0x0, 0x0 });
                //_vidSocket.Connect(5555, new IPEndPoint(IPAddress.Parse("224.1.1.1"), 5555), true);
                //_vidSocket.Send(Encoding.UTF8.GetBytes("Init"));
            }
        }

        /// <summary>
        /// Handles the reception of an image.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The event args.</param>
        private void ImageCompleteHandler(object sender, DroneImageCompleteEventArgs e)
        {
            if (OnDroneImage != null)
            {
                OnDroneImage(this, e);
            }
        }

        /// <summary>
        /// Handles the reception of image stream data from the drone.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        protected void OnVideoReceivedHandler(object sender, DroneSocketRcvEventArgs e)
        {
            try
            {
                // log message
                Logger.Log(LogScope.Debug, "Got drone image");
                // add chunk to image decoder
                _vidImage.AddImageStream(e.Data);
            }
            catch (Exception ex)
            {
                // log message
                Logger.Log(LogScope.Error, ex.StackTrace);
            }
        }

        /// <summary>
        /// Stops the video transmittion. Note there is no way to tell the drone to
        /// stop the video transmission. We just shut down the socket.
        /// </summary>
        public void StopVideo()
        {
            if (_vidSocket != null)
            {
                _vidSocket.Close();
                _vidSocket = null;
            }
        }

        /// <summary>
        /// Configures the drone for multi applications.
        /// </summary>
        /// <param name="appName">The application to store the config for.</param>
        /// <param name="userName">The user name to store the config for.</param>
        public void ConfigMulti(string appName, string userName)
        {
            _appId = string.IsNullOrEmpty(appName) ? 0 : Crc32.Compute(Encoding.UTF8.GetBytes(appName));
            _userId = string.IsNullOrEmpty(userName) ? 0 : Crc32.Compute(Encoding.UTF8.GetBytes(userName));
            //_sessionId = DateTime.Now.Millisecond;
            _sessionId = 0xabcde;
            this.EnqueueCfgCmd(new ConfigCommand(false, "custom:session_id", _sessionId.ToString("x8")));
            this.EnqueueCfgCmd(new ConfigCommand(false, "custom:profile_id", _userId.ToString("x8")));                     
            this.EnqueueCfgCmd(new ConfigCommand(false, "custom:application_id", _appId.ToString("x8")));          
        }

        /// <summary>
        /// Configures the name of the AR drone.
        /// </summary>
        /// <param name="name"></param>
        public void ConfigDroneName(string name)
        {
            this.EnqueueCfgCmd(new ConfigCommand("general:ardrone_name", name));
        }

        /// <summary>
        /// Configures the navdata demo mode.
        /// </summary>
        public void ConfigNavDataDemo()
        {
            this.EnqueueCfgCmd(new ConfigCommand("general:navdata_demo", true));
        }

        /// <summary>
        /// Configures the drone for outdooer/indoor usage.
        /// </summary>
        /// <param name="value">True for outdoor, false for indoor.</param>
        public void ConfigOutdoor(bool value)
        {
            this.EnqueueCfgCmd(new ConfigCommand("control:outdoor", value));
        }

        /// <summary>
        /// Configures the drone to fly with/without hull.
        /// </summary>
        /// <param name="value">True for flight without hull, false with hull.</param>
        public void ConfigFlightWithoutShell(bool value)
        {
            this.EnqueueCfgCmd(new ConfigCommand("control:flight_without_shell", value));
        }

        /// <summary>
        /// Enables the combined yaw mode. This mode is intended to be an easier
        /// control mode for racing games.
        /// </summary>
        public void ConfigCombinedYawMode()
        {
            this.EnqueueCfgCmd(new ConfigCommand("control:control_level", 3));
        }

        /// <summary>
        /// Configures the maximum euler angle.
        /// </summary>
        /// <param name="value">The maximum allowed euler angle in degrees (0..30.0).</param>
        public void ConfigEulerAngleMax(float value)
        {
            value = value * (float)Math.PI / 180.0f;
            value = ValidationHelpers.EnsureRange(value, 0, 0.52f);
            this.EnqueueCfgCmd(new ConfigCommand("control:euler_angle_max", value));
        }

        /// <summary>
        /// Configures the max vertical speed (in m/sec) of the drone.
        /// </summary>
        /// <param name="value">The max vertical speed in m/s.</param>
        public void ConfigMaxVertSpeed(float value)
        {
            value = value * 1000.0f;
            value = ValidationHelpers.EnsureRange(value, 200.0f, 2000.0f);
            this.EnqueueCfgCmd(new ConfigCommand("control:control_vz_max", Convert.ToInt32(value)));
        }

        /// <summary>
        /// Configures the yaw in degrees per sec.
        /// </summary>
        /// <param name="value">The value.</param>
        public void ConfigYaw(float value)
        {
            value = value * (float)Math.PI / 180.0f;
            value = ValidationHelpers.EnsureRange(value, 0.7f, 6.1f);
            this.EnqueueCfgCmd(new ConfigCommand("control:control_yaw", value));
        }

        /// <summary>
        /// Configures the max altitude (in meters) of the drone.
        /// </summary>
        /// <param name="value">The max altitude in meters.</param>
        public void ConfigAltMax(float value)
        {
            value = value * 1000.0f;
            value = ValidationHelpers.EnsureRange(value, 500.0f, 10000.0f);
            this.EnqueueCfgCmd(new ConfigCommand("control:altitude_max", Convert.ToInt32(value)));
        }

        /// <summary>
        /// Changes the ssid of the drone. Needs reboot.
        /// </summary>
        /// <param name="ssid"></param>
        public void ConfigSinglePlayerSsid(string ssid)
        {
            this.EnqueueCfgCmd(new ConfigCommand("network:ssid_single_player", ssid));
        }

        /// <summary>
        /// Configures the owner mac address of the drone.
        /// </summary>
        /// <param name="mac">The network interface mac address of the controller.</param>
        public void ConfigOwnerMac(string mac)
        {
            if (string.IsNullOrEmpty(mac))
            {
                this.EnqueueCfgCmd(new ConfigCommand("network:owner_mac", "00:00:00:00:00:00"));
            }
            else
            {
                this.EnqueueCfgCmd(new ConfigCommand("network:owner_mac", mac));
            }
        }

        /// <summary>
        /// Switches the video channel.
        /// </summary>
        /// <param name="channel">The channel to switch to.</param>
        public void ConfigVideoChannel(VideoChannel channel)
        {
            this.EnqueueCfgCmd(new ConfigCommand("video:video_channel", (int)channel));
        }

        /// <summary>
        /// Configures the video codec to be used.
        /// </summary>
        /// <param name="codec">The video codec.</param>
        public void ConfigVideoCodec(VideoCodec codec)
        {
            this.EnqueueCfgCmd(new ConfigCommand("video:video_codec", (int)codec));
        }

        /// <summary>
        /// Configures the video bitrate control mode.
        /// </summary>
        /// <param name="mode">The bitrate control mode.</param>
        /// <param name="frameSize">For manual mode, specify framesize in bytes.</param>
        public void ConfigVideoBitrateControl(VideoBitrateCtlMode mode, int frameSize)
        {
            this.EnqueueCfgCmd(new ConfigCommand("video:bitrate_ctrl_mode", (int)mode));
            if (mode == VideoBitrateCtlMode.Manual)
            {
                this.EnqueueCfgCmd(new ConfigCommand("video:bitrate", frameSize));
            }
        }

        /// <summary>
        /// Configures the enemy color.
        /// </summary>
        /// <param name="color">The color of the enemy.</param>
        public void ConfigEnemyColor(EnemyColor color)
        {
            this.EnqueueCfgCmd(new ConfigCommand("detect:enemy_colors", (int)color));
        }

        /// <summary>
        /// Configures the detection to shell/hull.
        /// </summary>
        /// <param name="value">True for without shell, flase with shell.</param>
        public void ConfigDetectionWithoutShell(bool value)
        {
            this.EnqueueCfgCmd(new ConfigCommand("detect:enemy_without_shell", value ? "1" : "0"));
        }

        /// <summary>
        /// Configures the detection type.
        /// </summary>
        /// <param name="type">The type of detection to use.</param>
        public void ConfigDetectionType(DetectionType type)
        {
            this.EnqueueCfgCmd(new ConfigCommand("detect:detect_type", (int)type));
        }

        /// <summary>
        /// Sets drone configuration values.
        /// </summary>
        protected void EnqueueCfgCmd(ConfigCommand cmd)
        {
            _cfgQueue.Enqueue(cmd);
        }

        protected void SetConfiguration(ConfigCommand cmd) //string parameterName, string parameterValue)
        {
            if(cmd.IsMultiConfig && ((_appId != 0) || (_userId != 0)))
            {
                this.EnqueueCmd(ATCommands.SetConfigurationIds, _sessionId.ToString("x8"), _userId.ToString("x8"), _appId.ToString("x8"));
            }
            this.EnqueueCmd(ATCommands.SetConfiguration, cmd.ParamName, cmd.ParamValue);
        }

        /// <summary>
        /// Acknowledges the control mode after bootstrap.
        /// </summary>
        protected void AckControlMode()
        {
            // should wait for message from drone
            this.EnqueueCmd(ATCommands.SetControlMode, (int)ControlMode.ACK_CONTROL_MODE, 0);
        }

        /// <summary>
        /// Requests the configured values.
        /// </summary>
        public void RequestConfiguredValues()
        {
            this.EnqueueCmd(ATCommands.SetControlMode, (int)ControlMode.CFG_GET_CONTROL_MODE, 0);
        }

        /// <summary>
        /// Performs a flat trim (on a flat ground).
        /// </summary>
        public void SetFlatTrim()
        {
            this.EnqueueCmd(ATCommands.SetFlatTrim);
        }

        /// <summary>
        /// Resets the communication hub watchdog of the drone.
        /// </summary>
        protected void ResetCommHub()
        {
            this.EnqueueCmd(ATCommands.ResetCommunicationHub);
        }

        /// <summary>
        /// Makes the drone to takeoff.
        /// </summary>
        public void Takeoff()
        {
            if (!_isCmdConnected) throw new DroneNotConnectedException();

            _inputValue |= (uint)DroneInputFlags.TAKEOFF_MASK;
        }

        /// <summary>
        /// Makes the drone hover.
        /// </summary>
        public void Hover()
        {
            this.EnqueueCmd(ATCommands.SetProgressiveInputValues, 0, 0, 0, 0, 0);
        }

        /// <summary>
        /// Lands the drone.
        /// </summary>
        public void Land()
        {
            if (!_isCmdConnected) throw new DroneNotConnectedException();

            _inputValue &= ~(uint)DroneInputFlags.TAKEOFF_MASK;
        }

        /// <summary>
        /// Switches to emergency mode.
        /// </summary>
        public void SetEmergency()
        {
            // always land in case of emergency to prevent takeoff when emergency finishes
            this.Land();

            _inputValue |= (uint)DroneInputFlags.EMERGENCY_MASK;          
        }

        /// <summary>
        /// Clears the emergency mode.
        /// </summary>
        public void ClearEmergency()
        {
            // always land in case of emergency to prevent takeoff when emergency finishes
            this.Land();

            _inputValue &= ~(uint)DroneInputFlags.EMERGENCY_MASK;
        }

        /// <summary>
        /// Sets the input value of the drone.
        /// </summary>
        /// <param name="input">The input value to set.</param>
        protected void SetInputValue(uint input)
        {
            this.EnqueueCmd(ATCommands.SetInputValue, input);
        }

        /// <summary>
        /// Sets the progressive input.
        /// </summary>
        /// <param name="input">The input.</param>
        protected void SetProgressiveInputValues(IDroneInput input)
        {
            if (input == null)
            {
                throw new InvalidOperationException("DroneInput is not set. Equals null!");
            }

            this.SetProgressiveInputValues(input.Roll, input.Pitch, input.Gaz, input.Yaw);
        }

        /// <summary>
        /// Sets the progressive input.
        /// </summary>
        /// <param name="roll">The roll percentage between -1.0 and 1.0.</param>
        /// <param name="pitch">The pitch percentage between -1.0 and 1.0.</param>
        /// <param name="gaz">The gaz percentage between -1.0 and 1.0.</param>
        /// <param name="yaw">The yaw percentage between -1.0 and 1.0.</param>
        protected void SetProgressiveInputValues(float roll, float pitch, float gaz, float yaw)
        {
            // limit ranges
            roll = ValidationHelpers.EnsureRange(roll, -1.0f, 1.0f);
            pitch = ValidationHelpers.EnsureRange(pitch, -1.0f, 1.0f);
            gaz = ValidationHelpers.EnsureRange(gaz, -1.0f, 1.0f);
            yaw = ValidationHelpers.EnsureRange(yaw, -1.0f, 1.0f);
            // convert IEEE 754 to int
            int newPitch = Helpers.FloatToBits(pitch);
            int newRoll = Helpers.FloatToBits(roll);
            int newHeight = Helpers.FloatToBits(gaz);
            int newYaw = Helpers.FloatToBits(yaw);
            // add the command to the queue
            this.EnqueueCmd(ATCommands.SetProgressiveInputValues, 1, newRoll, newPitch, newHeight, newYaw);
        }

        /// <summary>
        /// Plays a predefined animation for a given duration.
        /// </summary>
        /// <param name="anim">The animation to play.</param>
        /// <param name="duration">The duration of the animation.</param>
        public void PlayAnimation(FlightAnimation anim, TimeSpan duration)
        {
            int seconds = ValidationHelpers.EnsureRange(Convert.ToInt32(duration.TotalSeconds), 1, 10);
            this.EnqueueCmd(ATCommands.PlayAnimation, (int)anim, seconds);
        }

        /// <summary>
        /// Plays a led animation.
        /// </summary>
        /// <param name="anim">The led animatio to play.</param>
        /// <param name="frequency">The frequency (in Hz) of the animation.</param>
        /// <param name="duration">The duration of the animation.</param>
        public void PlayLedAnimation(LedAnimation anim, float frequency, TimeSpan duration)
        {
            int seconds = ValidationHelpers.EnsureRange(Convert.ToInt32(duration.TotalSeconds), 1, 10);
            int newFrequency = Helpers.FloatToBits(frequency);
            this.EnqueueCmd(ATCommands.PlayLedAnimation, (int)anim, newFrequency, seconds);
        }

        /// <summary>
        /// Enqueues a new command. The command will automatically get the next sequence
        /// number.
        /// </summary>
        /// <param name="command">The command to be queued.</param>
        /// <param name="parameter">The command parameters (without the seq number.</param>
        protected void EnqueueCmd(string command, params object[] parameter)
        {
            if (!_isCmdConnected) throw new DroneNotConnectedException();

            object[] paramsWithSeq = new object[parameter.Length + 1];
            paramsWithSeq[0] = _cmdSeqNo++;
            Array.Copy(parameter, 0, paramsWithSeq, 1, parameter.Length);
            string cmdText = string.Format(command, paramsWithSeq);
            _cmdQueue.Enqueue(new AtCommand() { CmdText = cmdText });
        }

        protected string GetNextCommandBatch()
        {
            // append cmd queue
            string batchCommands = null;
            while (_cmdQueue.Count > 0)
            {
                // todo length check not > 1024
                batchCommands += _cmdQueue.Dequeue().CmdText;
            }
            return batchCommands;
        }

        /// <summary>
        /// Clears the command and the config queue.
        /// </summary>
        private void ClearQueues()
        {
            _cmdQueue.Clear();
            _cfgQueue.Clear();
        }

        /// <summary>
        /// Sends the queued commands to the drone. This is done 40 times a second.
        /// It also sends periodically the input value to keep the drone in an
        /// active state.
        /// </summary>
        private void SendCommand()
        {
            while (_isCmdConnected)
            {
                try
                {
                    // always append the current input state of the drone takeoff/land/emergency
                    this.SetInputValue(_inputValue);
                    // append drone input, if available and drone (should) fly
                    IDroneInput input = this.DroneInput;
                    if ((input != null) && ((_inputValue & (uint)DroneInputFlags.TAKEOFF_MASK) != 0))
                    {
                        this.SetProgressiveInputValues(input);
                    }
                }
                catch (DroneNotConnectedException)
                {
                    // drone not connected anymore -> exit
                    _isCmdConnected = false;
                    return;
                }

                // gets the next commnd batch, note: there will always be at least the
                // input state
                string nextBatch = this.GetNextCommandBatch();
                if (!string.IsNullOrEmpty(nextBatch))
                {
                    // log message
                    Logger.Log(LogScope.Debug, "Sending command(s) to drone: " + nextBatch);
                    // get bytes and transfer them over the (wire/air)
                    byte[] dataBytes = Encoding.UTF8.GetBytes(nextBatch);
                    _cmdSocket.Send(dataBytes);
                }

                // sleep for 30msec to set the rate to approximately 30 frames/sec
                Thread.Sleep(30);
            }
        }
    }
}
