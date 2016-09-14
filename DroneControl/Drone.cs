using System;
using System.Collections.Generic;
using System.Net;
using System.IO;


namespace Parrot.DroneControl
{
    public class Drone
    {
        private enum ConnectionState
        {
            Disconnected,
            Bootstrapping,
            Connected
        }

        private enum CommandState
        {
            Unknown,
            InitiateEmergency,
            Emergency,
            InitiateReset,
            Reset
        }

        private Protocol.Communicator _communicator = null;
        private ConnectionState _connectionState = ConnectionState.Disconnected;
        private CommandState _cmdState = CommandState.Unknown;
        private DroneStatusEventArgs _lastStatus;

        /// <summary>
        /// Sets the drone input. The drone input will be queried approximately
        /// 30 times per second.
        /// </summary>
        public IDroneInput DroneInput
        {
            set { _communicator.DroneInput = value; }
        }

        // events
        public event EventHandler<DroneDataEventArgs> OnDroneData;       
        public event EventHandler<VisionDetectEventArgs> OnVisionDetect;
        public event EventHandler<DroneImageCompleteEventArgs> OnDroneImage;
        public event EventHandler OnCommunicationFailure;
        public event EventHandler OnConfigComplete;
        // higher level events
        public event EventHandler OnTakeoff;
        public event EventHandler OnLand;
        public event EventHandler OnTooMuchWind;
        public event EventHandler OnAnglesOutOfRange;
        public event EventHandler OnBatteryLow;
        public event EventHandler OnSensorFailure;
        public event EventHandler OnSystemCutout;


        public Drone()
        {
            _communicator = new Protocol.Communicator(new Video.UvlcVideoDecoder());
            _communicator.OnCommFailure += new EventHandler(OnDroneDisconnectHandler);
            _communicator.OnDroneData += new EventHandler<DroneDataEventArgs>(OnDroneDataHandler);
            _communicator.OnDroneStatus += new EventHandler<DroneStatusEventArgs>(OnDroneStatusHandler);
            _communicator.OnDroneStatus += new EventHandler<DroneStatusEventArgs>(OnDroneStatusHandler2);
            _communicator.OnVisionDetect += new EventHandler<VisionDetectEventArgs>(OnVisionDetectHandler);
            _communicator.OnDroneImage += new EventHandler<DroneImageCompleteEventArgs>(OnDroneImageHandler);
            _communicator.OnConfigComplete += new EventHandler(OnConfigCompleteHandler);
        }

        /// <summary>
        /// Establishes a connection with the drone.
        /// </summary>
        /// <param name="droneIp">The IP address of the drone.</param>
        public void EstablishConnection(string droneIp)
        {
            _communicator.Connect(droneIp);
        }

        /// <summary>
        /// Disconnects from the drone.
        /// </summary>
        public void Disconnect()
        {
            _communicator.Disconnect();
        }

        /// <summary>
        /// Configures the drone for multi applications.
        /// </summary>
        /// <param name="appName">The application to store the config for.</param>
        /// <param name="userName">The user name to store the config for.</param>
        public void ConfigMulti(string appName, string userName)
        {
            _communicator.ConfigMulti(appName, userName);
        }

        /// <summary>
        /// Configures the name of the AR drone.
        /// </summary>
        /// <param name="name"></param>
        public void ConfigDroneName(string name)
        {
            _communicator.ConfigDroneName(name);
        }

        /// <summary>
        /// Configures the navdata demo mode.
        /// </summary>
        public void ConfigNavData()
        {
            _communicator.ConfigNavDataDemo();
        }

        /// <summary>
        /// Configures the drone for outdooer/indoor usage.
        /// </summary>
        /// <param name="value">True for outdoor, false for indoor.</param>
        public void ConfigOutdoor(bool value)
        {
            _communicator.ConfigOutdoor(value);
        }

        /// <summary>
        /// Configures the drone to fly with/without hull.
        /// </summary>
        /// <param name="value">True for flight without hull, false with hull.</param>
        public void ConfigFlightWithoutShell(bool value)
        {
            _communicator.ConfigFlightWithoutShell(value);
        }

        /// <summary>
        /// Enables the combined yaw mode. This mode is intended to be an easier
        /// control mode for racing games.
        /// </summary>
        public void ConfigCombinedYawMode()
        {
            _communicator.ConfigCombinedYawMode();
        }

        /// <summary>
        /// Configures the maximum euler angle.
        /// </summary>
        /// <param name="value">The maximum allowed euler angle in degrees.</param>
        public void ConfigEulerAngleMax(float value)
        {
            _communicator.ConfigEulerAngleMax(value);
        }

        /// <summary>
        /// Configures the max vertical speed (in m/sec) of the drone.
        /// </summary>
        /// <param name="value">The max vertical speed in m/s.</param>
        public void ConfigMaxVertSpeed(float value)
        {
            _communicator.ConfigMaxVertSpeed(value);
        }

        /// <summary>
        /// Configures the yaw in degrees per sec.
        /// </summary>
        /// <param name="value">The value.</param>
        public void ConfigYaw(float value)
        {
            _communicator.ConfigYaw(value);
        }

        /// <summary>
        /// Configures the max altitude (in meters) of the drone.
        /// </summary>
        /// <param name="value">The max altitude in meters.</param>
        public void ConfigAltMax(float value)
        {
            _communicator.ConfigAltMax(value);
        }

        /// <summary>
        /// Changes the ssid of the drone. Needs reboot.
        /// </summary>
        /// <param name="ssid"></param>
        public void ConfigSinglePlayerSsid(string ssid)
        {
            _communicator.ConfigSinglePlayerSsid(ssid);
        }

        /// <summary>
        /// Configures the owner mac address of the drone.
        /// </summary>
        /// <param name="mac">The network interface mac address of the controller.</param>
        public void ConfigOwnerMac(string mac)
        {
            _communicator.ConfigOwnerMac(mac);
        }        

        /// <summary>
        /// Switches the video channel.
        /// </summary>
        /// <param name="channel">The channel to switch to.</param>
        public void ConfigVideoChannel(VideoChannel channel)
        {
            _communicator.ConfigVideoChannel(channel);
        }

        /// <summary>
        /// Configures the video codec to be used.
        /// </summary>
        /// <param name="codec">The video codec.</param>
        public void ConfigVideoCodec(VideoCodec codec)
        {
            _communicator.ConfigVideoCodec(codec);
        }

        /// <summary>
        /// Configures the video bitrate control mode.
        /// </summary>
        /// <param name="mode">The bitrate control mode.</param>
        /// <param name="frameSize">For manual mode, specify framesize in bytes.</param>
        public void ConfigVideoBitrateControl(VideoBitrateCtlMode mode, int frameSize)
        {
            _communicator.ConfigVideoBitrateControl(mode, frameSize);
        }

        /// <summary>
        /// Configures the enemy color.
        /// </summary>
        /// <param name="color">The color of the enemy.</param>
        public void ConfigEnemyColor(EnemyColor color)
        {
            _communicator.ConfigEnemyColor(color);
        }

        /// <summary>
        /// Configures the drone to ne use with or without the hull.
        /// </summary>
        /// <param name="value">True, if no hull is fitted.</param>
        public void ConfigDetectionWithoutShell(bool value)
        {
            _communicator.ConfigDetectionWithoutShell(value);
        }

        /// <summary>
        /// Configures the detection type.
        /// </summary>
        /// <param name="type">The type of detection to use.</param>
        public void ConfigDetectionType(DetectionType type)
        {
            _communicator.ConfigDetectionType(type);
        }

        public void RequestConfiguredValues()
        {
            _communicator.RequestConfiguredValues();
        }

        /// <summary>
        /// Performs a flat trim (on a flat ground).
        /// </summary>
        public void FlatTrim()
        {
            _communicator.SetFlatTrim();
        }

        /// <summary>
        /// Makes the drone to takeoff.
        /// </summary>
        public void Takeoff()
        {
            _communicator.Takeoff();
        }

        /// <summary>
        /// Makes the drone hover.
        /// </summary>
        public void Hover()
        {
            _communicator.Hover();
        }

        /// <summary>
        /// Lands the drone.
        /// </summary>
        public void Land()
        {
            _communicator.Land();
        }

        /// <summary>
        /// Switches to emergency mode.
        /// </summary>
        public void SetEmergency()
        {
            if (_connectionState != ConnectionState.Connected)
            {
                throw new DroneNotConnectedException();
            }

            if (_cmdState != CommandState.Unknown)
            {
                throw new InvalidOperationException("Command in progress!");
            }

            _cmdState = CommandState.InitiateEmergency;
        }

        /// <summary>
        /// Clears the emergency mode.
        /// </summary>
        public void ResetEmergency()
        {
            if (_connectionState != ConnectionState.Connected)
            {
                throw new DroneNotConnectedException();
            }

            if (_cmdState != CommandState.Unknown)
            {
                throw new InvalidOperationException("Command in progress!");
            }

            _cmdState = CommandState.InitiateReset;
        }

        /// <summary>
        /// Starts the video transmission.
        /// </summary>
        public void StartVideo()
        {
            _communicator.StartVideo();
        }

        /// <summary>
        /// Stops the video transmission.
        /// </summary>
        public void StopVideo()
        {
            _communicator.StopVideo();
        }

        /// <summary>
        /// Plays a predefined animation for a given duration.
        /// </summary>
        /// <param name="anim">The animation to play.</param>
        /// <param name="duration">The duration of the animation.</param>
        public void PlayAnimation(FlightAnimation anim, TimeSpan duration)
        {
            _communicator.PlayAnimation(anim, duration);
        }

        /// <summary>
        /// Plays a led animation.
        /// </summary>
        /// <param name="anim">The led animatio to play.</param>
        /// <param name="frequency">The frequency (in Hz) of the animation.</param>
        /// <param name="duration">The duration of the animation.</param>
        public void PlayLedAnimation(LedAnimation anim, float frequency, TimeSpan duration)
        {
            _communicator.PlayLedAnimation(anim, frequency, duration);
        }

        /// <summary>
        /// Handles the disconnection fron the drone.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The event arguments.</param>
        protected void OnDroneDisconnectHandler(object sender, EventArgs e)
        {
            _connectionState = ConnectionState.Disconnected;
            _cmdState = CommandState.Unknown;

            // bubble up the disconnect event
            if (this.OnCommunicationFailure != null)
            {
                this.OnCommunicationFailure(this, e);
            }
        }

        /// <summary>
        /// Handles the data reception from the drone.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The event arguments.</param>
        protected void OnDroneDataHandler(object sender, DroneDataEventArgs e)
        {
            if (this.OnDroneData != null)
            {
                this.OnDroneData(this, e);
            }          
        }

        private void OnDroneStatusHandler(object sender, DroneStatusEventArgs e)
        {
            try
            {
                if (_connectionState == ConnectionState.Disconnected)
                {
                    // handle bootstrap mode               
                    _connectionState = ConnectionState.Connected;
                }
                else if ((_connectionState == ConnectionState.Connected) && e.HasCommunicationLost)
                {
                    _communicator.Disconnect();
                }

                // long running commands, commands that span multiple drone status
                if ((_cmdState == CommandState.InitiateEmergency) && e.HasEmergency)
                {
                    _cmdState = CommandState.Unknown;
                }
                else if ((_cmdState == CommandState.InitiateEmergency) && !e.HasEmergency)
                {
                    _communicator.SetEmergency();
                    _cmdState = CommandState.Emergency;
                }
                else if ((_cmdState == CommandState.Emergency) && e.HasEmergency)
                {
                    _communicator.ClearEmergency();
                    _cmdState = CommandState.Unknown;
                }
                else if ((_cmdState == CommandState.InitiateReset) && !e.HasEmergency)
                {
                    _cmdState = CommandState.Unknown;
                }
                else if ((_cmdState == CommandState.InitiateReset) && e.HasEmergency)
                {
                    _communicator.SetEmergency();
                    _cmdState = CommandState.Reset;
                }
                else if (_cmdState == CommandState.Reset && !e.HasEmergency)
                {
                    _communicator.ClearEmergency();
                    _cmdState = CommandState.Unknown;
                }
            }
            catch (DroneNotConnectedException)
            {
                Logger.Log(LogScope.Error, "Drone not connected anymore!");
            }
        }

        /// <summary>
        /// Handles the event bubbling.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e"></param>
        private void OnDroneStatusHandler2(object sender, DroneStatusEventArgs e)
        {
            if (_lastStatus == null)
            {
                _lastStatus = e;
            }

            DroneStatusEventArgs toggled = e ^ _lastStatus;

            // fire higher level events
            if ((this.OnTakeoff != null) && e.FlightEnabled && toggled.FlightEnabled)
            {
                Logger.Log(LogScope.Info, "Taking off!");
                this.OnTakeoff(this, EventArgs.Empty);
            }

            if ((this.OnLand != null) && !e.FlightEnabled && toggled.FlightEnabled)
            {
                Logger.Log(LogScope.Info, "Landing!");
                this.OnLand(this, EventArgs.Empty);
            }

            if ((this.OnTooMuchWind != null) && e.TooMuchWind && toggled.TooMuchWind)
            {
                Logger.Log(LogScope.Warning, "Too much wind!");
                this.OnTooMuchWind(this, EventArgs.Empty);
            }

            if ((this.OnAnglesOutOfRange != null) && e.AnglesOutOfRange && toggled.AnglesOutOfRange)
            {
                Logger.Log(LogScope.Warning, "Angles out of range!");
                this.OnAnglesOutOfRange(this, EventArgs.Empty);
            }

            if ((this.OnBatteryLow != null) && e.BatteryChargeTooLow && toggled.BatteryChargeTooLow)
            {
                Logger.Log(LogScope.Warning, "Battery low!");
                this.OnBatteryLow(this, EventArgs.Empty);
            }

            if ((this.OnSystemCutout != null) && e.SystemCutout && toggled.SystemCutout)
            {
                Logger.Log(LogScope.Warning, "System cutout!");
                this.OnSystemCutout(this, EventArgs.Empty);
            }

            if ((this.OnSensorFailure != null) &&
                    ((e.UltrasoundProblem && toggled.UltrasoundProblem) ||
                    (e.HasGyrosProblem && toggled.HasGyrosProblem)))
            {
                Logger.Log(LogScope.Warning, "Sensor failure!");
                this.OnSensorFailure(this, EventArgs.Empty);
            }

            // remember last status
            _lastStatus = e;
        }

        /// <summary>
        /// Handles the vision detect event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        protected void OnVisionDetectHandler(object sender, VisionDetectEventArgs e)
        {
            if (this.OnVisionDetect != null)
            {
                this.OnVisionDetect(this, e);
            }
        }


        /// <summary>
        /// Handles the reception of a drone image.
        /// </summary>
        /// <param name="sender">The sender object.</param>
        /// <param name="e">The event arguments.</param>
        protected void OnDroneImageHandler(object sender, DroneImageCompleteEventArgs e)
        {
            if (this.OnDroneImage != null)
            {
                this.OnDroneImage(this, e);
            }

            #if !WINDOWS_PHONE
            //BitmapEncoder encoder = new JpegBitmapEncoder();

            //using (Stream fileStream = new FileStream("c:\\temp\\dronepic.jpg", FileMode.Create))
            //{
            //    encoder.Frames.Add(BitmapFrame.Create((WriteableBitmap)e.ImageSource));
            //    encoder.Save(fileStream);
            //} 
            #endif
        }

        /// <summary>
        /// Handles the configuration complete event.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The event arguments.</param>
        protected void OnConfigCompleteHandler(object sender, EventArgs e)
        {
            if (this.OnConfigComplete != null)
            {
                this.OnConfigComplete(sender, e);
            }
        }
    }
}
