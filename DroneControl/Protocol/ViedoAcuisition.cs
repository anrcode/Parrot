using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;


namespace Parrot.DroneControl.Protocol
{
    public class VideoAcquisition
    {
        public const int VideoPort = 5555;
        public const int FrameBufferSize = 0x100000;
        public const int NetworkStreamReadSize = 0x1000;
        private readonly NetworkConfiguration _configuration;
        private readonly Action<VideoPacket> _videoPacketAcquired;

        public VideoAcquisition(NetworkConfiguration configuration, Action<VideoPacket> videoPacketAcquired)
        {
            _configuration = configuration;
            _videoPacketAcquired = videoPacketAcquired;
        }

        protected override unsafe void Loop(CancellationToken token)
        {
            using (var tcpClient = new TcpClient(_configuration.DroneHostname, VideoPort))
            using (NetworkStream stream = tcpClient.GetStream())
            {
                var packet = new VideoPacket();
                byte[] packetData = null;
                int offset = 0;
                int frameStart = 0;
                int frameEnd = 0;
                var buffer = new byte[FrameBufferSize];

                while (token.IsCancellationRequested == false)
                {
                    int read = stream.Read(buffer, offset, NetworkStreamReadSize);

                    if (read == 0)
                    {
                        Thread.Sleep(1);
                        continue;
                    }

                    offset += read;
                    if (packetData == null)
                    {
                        // lookup for a frame start
                        int maxSearchIndex = offset - 64; // 64 = sizeof(parrot_video_encapsulation_t)
                        for (int i = 0; i < maxSearchIndex; i++)
                        {
                            if (buffer[i] == 'P' && buffer[i + 1] == 'a' && buffer[i + 2] == 'V' && buffer[i + 3] == 'E')
                            {
                                MemoryStream ms = new MemoryStream(buffer);
                                BinaryReader br = new BinaryReader(ms);

                                uint signature = br.ReadUInt32(); // "PaVE"
                                byte version = br.ReadByte();
                                byte codec = br.ReadByte();
                                ushort headerSize = br.ReadUInt16();
                                uint payloadSize = br.ReadUInt32();

                                //parrot_video_encapsulation_t pve = *(parrot_video_encapsulation_t*)(pBuffer + i);
                                packet.Data = new byte[payloadSize];

                                frameStart = i + headerSize;
                                frameEnd = frameStart + packet.Data.Length;
                                break;
                            }
                        }
                        if (packetData == null)
                        {
                            // frame is not detected
                            offset -= maxSearchIndex;
                            Array.Copy(buffer, maxSearchIndex, buffer, 0, offset);
                        }
                    }

                    if (packetData != null && offset >= frameEnd)
                    {
                        // frame acquired
                        Array.Copy(buffer, frameStart, packetData, 0, packetData.Length);
                        _videoPacketAcquired(packet);

                        // clean up acquired frame
                        packetData = null;
                        offset -= frameEnd;
                        Array.Copy(buffer, frameEnd, buffer, 0, offset);
                    }
                }
            }
        }
    }
}
