using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parrot.DroneControl.Protocol
{
    public enum VideoFrameType : byte
    {
        Unknown,
        I,
        P
    }

    public class VideoPacket
    {
        public long Timestamp;
        public uint FrameNumber;
        public ushort Height;
        public ushort Width;
        public VideoFrameType FrameType;
        public byte[] Data;
    }
}
