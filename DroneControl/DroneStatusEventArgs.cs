using System;
using System.Collections.Generic;

using Parrot.DroneControl.Protocol;


namespace Parrot.DroneControl
{
    /// <summary>
    /// This class represents the decoded status of the drone.
    /// </summary>
    internal class DroneStatusEventArgs : EventArgs
    {
        private uint _status;

        // properties
        public bool FlightEnabled
        { 
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_FLY_MASK); }
        }

        public bool VideoEnabled
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_VIDEO_MASK); }
        }

        public bool VisionEnabled
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_VISION_MASK); }
        }

        public bool AngularSpeedControlEnabled
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_CONTROL_MASK); }
        }

        public bool AltitudeControlEnabled
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_ALTITUDE_MASK); }
        }

        public bool StartButtonEnabled
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_USER_FEEDBACK_START); }
        }

        public bool HasCommandReceived 
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_COMMAND_MASK); }
        }

        public bool HasFlatTrimCommandReceived
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_TRIM_COMMAND_MASK); }
        }

        public bool FlatTrimCommandRunning
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_TRIM_RUNNING_MASK); }
        }

        public bool FlatTrimCommandSucceded
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_TRIM_RESULT_MASK); }
        }

        public bool BootstrapMode
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_NAVDATA_BOOTSTRAP); }
        }

        public bool BrushMotors
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_MOTORS_BRUSHED); }
        }

        public bool HasCommunicationLost
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_COM_LOST_MASK); }
        }

        public bool HasGyrosProblem
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_GYROS_ZERO); }
        }

        public bool BatteryChargeTooLow
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_VBAT_LOW); }
        }

        public bool BatteryChargeTooHigh
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_VBAT_HIGH); }
        }
        public bool TimerElapsed
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_TIMER_ELAPSED); }
        }
        public bool InsufficientPower
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_NOT_ENOUGH_POWER); }
        }

        public bool AnglesOutOfRange
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_ANGLES_OUT_OF_RANGE); }
        }

        public bool TooMuchWind
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_WIND_MASK); }
        }

        public bool SystemCutout
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_CUTOUT_MASK); }
        }

        public bool UltrasoundProblem
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_ULTRASOUND_MASK); }
        }

        public bool ActiveATCommandThread
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_ATCODEC_THREAD_ON); }
        }

        public bool ActiveNavigationDataThread
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_NAVDATA_THREAD_ON); }
        }

        public bool ActiveVideoStreamThread
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_VIDEO_THREAD_ON); }
        }

        public bool ComWatchdog
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_COM_WATCHDOG_MASK); }
        }

        public bool HasEmergency
        {
            get { return GetMask(_status, DroneStatusFlags.AR_DRONE_EMERGENCY_MASK); }
        }

        internal DroneStatusEventArgs(uint status)
        {
            _status = status;
        }

        private static bool GetMask(uint status, DroneStatusFlags droneStatusFlags)
        {
            return (((DroneStatusFlags)status & droneStatusFlags) == droneStatusFlags);
        }

        public static DroneStatusEventArgs operator ^(DroneStatusEventArgs a1, DroneStatusEventArgs a2)
        {
            return new DroneStatusEventArgs(a1._status ^ a2._status);
        }
    }
}
