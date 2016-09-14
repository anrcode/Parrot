using System;
using System.Collections.Generic;

using Parrot.DroneControl.Protocol;


namespace Parrot.DroneControl
{
    public class VisionDetectEventArgs : EventArgs
    {
        public static double FACTOR_X = 320.0d / 1000.0d;
        public static double FACTOR_Y = 240.0d / 1000.0d;

        public IList<DroneTag> Tags { get; private set; }

        internal VisionDetectEventArgs(NavigationData.NavVisionDetect data)
        {
            this.Tags = new List<DroneTag>();
            if (data.TagCount > 0)
            {
                DroneTag tag = new DroneTag();
                tag.TagType = data.Tag1Type;
                tag.CenterX = (uint)((double)data.Tag1X * FACTOR_X);
                tag.CenterY = (uint)((double)data.Tag1Y * FACTOR_Y);
                tag.BoxLeft = (uint)(((double)data.Tag1X - (double)data.Tag1BoxWidth / 2.0) * FACTOR_X);
                tag.BoxTop = (uint)(((double)data.Tag1Y - (double)data.Tag1BoxHeight / 2.0) * FACTOR_Y);
                tag.BoxWidth = (uint)((double)data.Tag1BoxWidth * FACTOR_X);
                tag.BoxHeight = (uint)((double)data.Tag1BoxHeight * FACTOR_Y);      
                tag.Distance = (double)data.Tag1Distance / 1000.0;
                tag.OrientationAngle = data.Tag1OrientationAngle;
                this.Tags.Add(tag);
            }

            if (data.TagCount > 1)
            {
                DroneTag tag = new DroneTag();
                tag.TagType = data.Tag2Type;
                tag.CenterX = (uint)((double)data.Tag2X * FACTOR_X);
                tag.CenterY = (uint)((double)data.Tag2Y * FACTOR_Y);
                tag.BoxLeft = (uint)(((double)data.Tag2X - (double)data.Tag2BoxWidth / 2.0) * FACTOR_X);
                tag.BoxTop = (uint)(((double)data.Tag2Y - (double)data.Tag2BoxHeight / 2.0) * FACTOR_Y);
                tag.BoxWidth = (uint)((double)data.Tag2BoxWidth * FACTOR_X);
                tag.BoxHeight = (uint)((double)data.Tag2BoxHeight * FACTOR_Y);               
                tag.Distance = (double)data.Tag2Distance / 1000.0;
                tag.OrientationAngle = data.Tag2OrientationAngle;
                this.Tags.Add(tag);
            }

            if (data.TagCount > 2)
            {
                DroneTag tag = new DroneTag();
                tag.TagType = data.Tag3Type;
                tag.CenterX = (uint)((double)data.Tag3X * FACTOR_X);
                tag.CenterY = (uint)((double)data.Tag3Y * FACTOR_Y);
                tag.BoxLeft = (uint)(((double)data.Tag3X - (double)data.Tag3BoxWidth / 2.0) * FACTOR_X);
                tag.BoxTop = (uint)(((double)data.Tag3Y - (double)data.Tag3BoxHeight / 2.0) * FACTOR_Y);
                tag.BoxWidth = (uint)((double)data.Tag3BoxWidth * FACTOR_X);
                tag.BoxHeight = (uint)((double)data.Tag3BoxHeight * FACTOR_Y);               
                tag.Distance = (double)data.Tag3Distance / 1000.0;
                tag.OrientationAngle = data.Tag3OrientationAngle;
                this.Tags.Add(tag);
            }

            if (data.TagCount > 3)
            {
                DroneTag tag = new DroneTag();
                tag.TagType = data.Tag4Type;
                tag.CenterX = (uint)((double)data.Tag4X * FACTOR_X);
                tag.CenterY = (uint)((double)data.Tag4Y * FACTOR_Y);
                tag.BoxLeft = (uint)(((double)data.Tag4X - (double)data.Tag4BoxWidth / 2.0) * FACTOR_X);
                tag.BoxTop = (uint)(((double)data.Tag4Y - (double)data.Tag4BoxHeight / 2.0) * FACTOR_Y);
                tag.BoxWidth = (uint)((double)data.Tag4BoxWidth * FACTOR_X);
                tag.BoxHeight = (uint)((double)data.Tag4BoxHeight * FACTOR_Y);               
                tag.Distance = (double)data.Tag4Distance / 1000.0;
                tag.OrientationAngle = data.Tag4OrientationAngle;
                this.Tags.Add(tag);
            }
        }
    }
}
