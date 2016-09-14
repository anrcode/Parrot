using System;
using System.Collections.Generic;


namespace Parrot.DroneControl
{
    /// <summary>
    /// This class represents a detected (enemy) tag.
    /// </summary>
    public class DroneTag
    {
        public uint TagType { get; internal set; }
        public uint CenterX { get; internal set; }
        public uint CenterY { get; internal set; }
        public uint BoxTop { get; internal set; }
        public uint BoxLeft { get; internal set; }
        public uint BoxWidth { get; internal set; }
        public uint BoxHeight { get; internal set; }
        public double Distance { get; internal set; }
        public float OrientationAngle { get; internal set; }
    }
}
