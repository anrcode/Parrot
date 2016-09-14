using System;


namespace Parrot.DroneControl.Protocol
{
    /// <summary>
    /// This class contains several helpers that either use unsafe or managed
    /// code to ensure compatibility with the target device(s).
    /// </summary>
    internal class Helpers
    {
        /// <summary>
        /// Converts a float vaue to its IEEE 754 int representation.
        /// </summary>
        /// <param name="value">The float value to convert.</param>
        /// <returns>The IEEE 754 representation of the float.</returns>
        public static int FloatToBits(float value)
        {
            int ieee754 = BitConverter.ToInt32(BitConverter.GetBytes(value), 0);
            return ieee754;
        }

        //public static int FloatToBits(float value)
        //{
        //    unsafe
        //    {
        //        return *(int*)&value;
        //    }
        //}

        public static T BytesToStruct<T>(byte[] bytes) where T: struct
        {   
            T stuff = StructureHelper.Read<T>(bytes);
            return stuff;
        }

        public static T BytesToStruct<T>(byte[] bytes, int offset) where T : struct
        {
            T stuff = StructureHelper.Read<T>(bytes, offset);
            return stuff;
        }
    }
}
