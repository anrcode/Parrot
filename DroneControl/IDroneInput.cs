using System;


namespace Parrot.DroneControl
{
    public interface IDroneInput
    {
        /// <summary>
        /// A percentage value between -1.0 and 1.0.
        /// </summary>
        float Roll { get; }
        /// <summary>
        /// A percentage value between -1.0 and 1.0.
        /// </summary>
        float Pitch { get; }
        /// <summary>
        /// A percentage value between -1.0 and 1.0.
        /// </summary>
        float Yaw { get; }
        /// <summary>
        /// A percentage value between -1.0 and 1.0.
        /// </summary>
        float Gaz { get; }
    }
}
