using System.IO;


namespace Parrot.DroneControl.IO
{
    /// <summary>
    /// Summary description for BitStream.
    /// </summary>
    public class BitStream : MemoryStream
    {
        private byte[] _imageStream;
        private uint _streamField;
        private int _streamFieldBitIndex;
        private int _streamIndex;

        public BitStream(byte[] input)
        {
            _imageStream = input;
            _streamFieldBitIndex = 32;
            _streamField = 0;
            _streamIndex = 0;
        }

        public bool HasLeftBits()
        {
            return _streamIndex < _imageStream.Length;
        }

        public uint ReadBits(int count)
        {
            uint data = 0;

            if (count > (32 - _streamFieldBitIndex))
            {
                data = _streamField >> _streamFieldBitIndex;

                count -= 32 - _streamFieldBitIndex;

                _streamField = (uint)_imageStream[_streamIndex] | (uint)_imageStream[_streamIndex + 1] << 8;
                _streamField |= (uint)_imageStream[_streamIndex + 2] << 16 | (uint)_imageStream[_streamIndex + 3] << 24;
                _streamFieldBitIndex = 0;
                _streamIndex += 4;
            }

            if (count > 0)
            {
                data = (data << count) | (_streamField >> (32 - count));

                _streamField <<= count;
                _streamFieldBitIndex += count;
            }

            return data;
        }

        public uint PeekBits(int count)
        {
            uint data = 0;
            uint streamField = _streamField;
            int streamFieldBitIndex = _streamFieldBitIndex;

            if (count > (32 - streamFieldBitIndex) && (_streamIndex < _imageStream.Length))
            {
                data = streamField >> streamFieldBitIndex;

                count -= 32 - streamFieldBitIndex;

                streamField = (uint)_imageStream[_streamIndex] | (uint)_imageStream[_streamIndex + 1] << 8;
                streamField |= (uint)_imageStream[_streamIndex + 2] << 16 | (uint)_imageStream[_streamIndex + 3] << 24;
                streamFieldBitIndex = 0;
            }

            if (count > 0)
            {
                data = (data << count) | (streamField >> (32 - count));
            }

            return data;
        }

        private void AlignStreamData()
        {
            if (_streamFieldBitIndex > 0)
            {
                int alignedLength = (_streamFieldBitIndex & ~7);
                if (alignedLength != _streamFieldBitIndex)
                {
                    alignedLength += 0x08;
                    _streamField <<= (alignedLength - _streamFieldBitIndex);
                    _streamFieldBitIndex = alignedLength;
                }
            }
        }
    }
}
