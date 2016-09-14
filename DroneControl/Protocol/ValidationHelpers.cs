using System;
using System.Collections.Generic;


namespace Parrot.DroneControl.Protocol
{
    /// <summary>
    /// This class contains some basic validation helper methods.
    /// </summary>
    internal class ValidationHelpers
    {
        /// <summary>
        /// Ensures that an integer is between a given range.
        /// </summary>
        /// <param name="val">The value to be checked.</param>
        /// <param name="min">The min value allowed.</param>
        /// <param name="max">The max value allowed.</param>
        /// <returns>The value between min and max value.</returns>
        public static int EnsureRange(int val, int min, int max)
        {
            val = val < min ? min : val;
            val = val > max ? max : val;
            return val;
        }

        /// <summary>
        /// Ensures that an integer is between a given range.
        /// </summary>
        /// <param name="val">The value to be checked.</param>
        /// <param name="min">The min value allowed.</param>
        /// <param name="max">The max value allowed.</param>
        /// <returns>The value between min and max value.</returns>
        public static float EnsureRange(float val, float min, float max)
        {
            val = val < min ? min : val;
            val = val > max ? max : val;
            return val;
        }
    }
}
