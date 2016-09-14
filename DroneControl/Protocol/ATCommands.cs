using System;


namespace Parrot.DroneControl.Protocol
{
    /// <summary>
    /// Static class containing the AT commands currently supported.
    /// </summary>
    internal static class ATCommands
    {
        /// <summary>
        /// This AT Command is used for take off/land and emergency reset.
        /// </summary>
        public static readonly string SetInputValue = "AT*REF={0},{1}\r";
        /// <summary>
        /// This AT command sets a reference of the horizontal plane for the drone internal control system.
        /// </summary>
        public static readonly string SetFlatTrim = "AT*FTRIM={0}\r";
        /// <summary>
        /// This AT Command sets an configurable option on the drone.
        /// </summary>
        public static readonly string SetConfigurationIds = "AT*CONFIG_IDS={0},\"{1}\",\"{2}\",\"{3}\"\r";
        /// <summary>
        /// This AT Command sets an configurable option on the drone.
        /// </summary>
        public static readonly string SetConfiguration = "AT*CONFIG={0},\"{1}\",\"{2}\"\r";
        /// <summary>
        /// This AT Command is used when communicating with the control communication channel.
        /// </summary>
        public static readonly string SetControlMode = "AT*CTRL={0},{1},{2}\r";
        /// <summary>
        /// This AT Command makes the ARDrone animate its LED's according to a selectable pattern.
        /// </summary>
        public static readonly string PlayLedAnimation = "AT*LED={0},{1},{2},{3}\r";
        /// <summary>
        /// This AT Command is used to provide the ARDrone with piloting instructions.
        /// </summary>
        public static readonly string SetProgressiveInputValues = "AT*PCMD={0},{1},{2},{3},{4},{5}\r";
        /// <summary>
        /// This AT Command resets the internal ARDrone communication system.
        /// </summary>
        public static readonly string ResetCommunicationHub = "AT*COMWDG={0}\r";
        /// <summary>
        /// Playt a LED animation.
        /// </summary>
        public static readonly string PlayAnimation = "AT*ANIM={0},{1},{2}\r";
    }
}
