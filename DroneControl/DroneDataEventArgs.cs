using System;

using Parrot.DroneControl.Protocol;


namespace Parrot.DroneControl
{
    /// <summary>
    /// This class contains the basic data sent fron the drone.
    /// </summary>
    public class DroneDataEventArgs : EventArgs
    {
        /// <summary>
        /// The remaining power in percent.
        /// </summary>
        public uint FlyingPercentage { get; private set; }
        /// <summary>
        /// The altitude of the drone in meters.
        /// </summary>
        public float Altitude { get; private set; }
        public float Theta { get; private set; }
        public float Phi { get; private set; }
        public float Psi { get; private set; }

        internal DroneDataEventArgs(NavigationData.NavDataDrone data)
        {
            this.FlyingPercentage = data.FlyingPercentage;
            this.Altitude = (float)data.Altitude / 1000.0f;
            this.Theta = data.Theta;
            this.Phi = data.Phi;
            this.Psi = data.Psi;
        }
    }
}
