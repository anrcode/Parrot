using System;


namespace Parrot.DroneControl.Protocol
{
    [Flags]
    internal enum DroneInputFlags : uint
    {
        EMERGENCY_MASK = 0x00000100,
        TAKEOFF_MASK = 0x00000200
    }


    internal enum ControlMode
    {
        NO_CONTROL_MODE = 0,          // Doing nothing
        ARDRONE_UPDATE_CONTROL_MODE,  // Ardrone software update reception (update is done next run)
        // After event completion, card should power off
        PIC_UPDATE_CONTROL_MODE,      // Ardrone pic software update reception (update is done next run)
        // After event completion, card should power off
        LOGS_GET_CONTROL_MODE,        // Send previous run's logs
        CFG_GET_CONTROL_MODE,         // Send active configuration
        ACK_CONTROL_MODE              // Reset command mask in navdata
    }

    /// <summary>
    /// Used to pass the status of the ARDrone. See ARDrone developer's guide for more information.
    /// </summary>
    [Flags]
    internal enum DroneStatusFlags : uint
    {
        AR_DRONE_FLY_MASK = 1,
        AR_DRONE_VIDEO_MASK = 2,
        AR_DRONE_VISION_MASK = 4,
        AR_DRONE_CONTROL_MASK = 8,
        AR_DRONE_ALTITUDE_MASK = 16,
        AR_DRONE_USER_FEEDBACK_START = 32,
        AR_DRONE_COMMAND_MASK = 64,
        AR_DRONE_TRIM_COMMAND_MASK = 128,
        AR_DRONE_TRIM_RUNNING_MASK = 256,
        AR_DRONE_TRIM_RESULT_MASK = 512,
        AR_DRONE_NAVDATA_DEMO_MASK = 1024,
        AR_DRONE_NAVDATA_BOOTSTRAP = 2048,
        AR_DRONE_MOTORS_BRUSHED = 4096,
        AR_DRONE_COM_LOST_MASK = 8192,
        AR_DRONE_GYROS_ZERO = 16384,
        AR_DRONE_VBAT_LOW = 32768,
        AR_DRONE_VBAT_HIGH = 65536,
        AR_DRONE_TIMER_ELAPSED = 131072,
        AR_DRONE_NOT_ENOUGH_POWER = 262144,
        AR_DRONE_ANGLES_OUT_OF_RANGE = 524288,
        AR_DRONE_WIND_MASK = 1048576,
        AR_DRONE_ULTRASOUND_MASK = 2097152,
        AR_DRONE_CUTOUT_MASK = 4194304,
        AR_DRONE_PIC_VERSION_MASK = 8388608,
        AR_DRONE_ATCODEC_THREAD_ON = 16777216,
        AR_DRONE_NAVDATA_THREAD_ON = 33554432,
        AR_DRONE_VIDEO_THREAD_ON = 67108864,
        AR_DRONE_ACQ_THREAD_ON = 134217728,
        AR_DRONE_CTRL_WATCHDOG_MASK = 268435456,
        AR_DRONE_ADC_WATCHDOG_MASK = 536870912,
        AR_DRONE_COM_WATCHDOG_MASK = 1073741824,
        AR_DRONE_EMERGENCY_MASK = 2147483648
    }
}
