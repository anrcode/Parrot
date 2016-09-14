using System;


namespace Parrot.DroneControl
{
    /// <summary>
    /// Contains an image frame sent from the drone.
    /// </summary>
    public class DroneImageCompleteEventArgs : EventArgs
    {
        /// <summary>
        /// The width of the image.
        /// </summary>
        public int ImageWidth { get; internal set; }
        /// <summary>
        /// The height of the image.
        /// </summary>
        public int ImageHeight { get; internal set; }
        /// <summary>
        /// The raw image bits. Each int represents a pixel in the rgba format.
        /// </summary>
        public int[] ImagePixels { get; internal set; }


        internal DroneImageCompleteEventArgs(int width, int height, int[] pixels)
        {
            this.ImageWidth = width;
            this.ImageHeight = height;
            this.ImagePixels = pixels;
        }
    }
}
