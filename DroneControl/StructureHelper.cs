using System;
using System.Runtime.InteropServices;


namespace Parrot.DroneControl
{
    public abstract class StructureHelper
    {
        /// <summary>
        /// Marshals an array of bytes to the corresponding type.
        /// </summary>
        /// <typeparam name="T">The type to be unmarshalled.</typeparam>
        /// <param name="data">The serialized data.</param>
        /// <returns>The marshalled object.</returns>
        public static T Read<T>(byte[] data) where T : struct
        {
            GCHandle gch = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                return (T)Marshal.PtrToStructure(gch.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                gch.Free();
            }
        }

        /// <summary>
        /// Marshals an array of bytes to the corresponding type.
        /// </summary>
        /// <typeparam name="T">The type to be unmarshalled.</typeparam>
        /// <param name="data">The serialized data.</param>
        /// <param name="position">The start position in the array.</param>
        /// <returns>The marshalled object.</returns>
        public static T Read<T>(byte[] data, int position) where T : struct
        {
            if (position == 0)
            {
                return Read<T>(data);
            }

            // allocate temporary buffer
            byte[] tmp = new byte[data.Length - position];
            Array.Copy(data, position, tmp, 0, tmp.Length);
            // unmarshal struct data
            return Read<T>(tmp);
        }
    }
}
