using System;
using System.Collections.Generic;


namespace Parrot.DroneControl.Video
{
    public class UvlcVideoDecoder : IDroneVideoDecoder
    {
        public event EventHandler<DroneImageCompleteEventArgs> ImageComplete;

        /// <summary>
        /// Specifies the image format of the returned image.
        /// </summary>
        private enum PictureFormats
        {
            Cif = 1, // 176px x 144px
            Vga = 2 // 320px x 240px
        }

        private const int MCU_WIDTH = 8;
        private const int MCU_BLOCK_SIZE = MCU_WIDTH * MCU_WIDTH;

        private const int QQCIF_WIDTH = 88;
        private const int QQCIF_HEIGHT = 72;
        private const int QQVGA_WIDTH = 160;
        private const int QQVGA_HEIGHT = 120;

        private const int CONST_TableQuantization = 31;

        private const int FIX_0_298631336 = 2446;
        private const int FIX_0_390180644 = 3196;
        private const int FIX_0_541196100 = 4433;
        private const int FIX_0_765366865 = 6270;
        private const int FIX_0_899976223 = 7373;
        private const int FIX_1_175875602 = 9633;
        private const int FIX_1_501321110 = 12299;
        private const int FIX_1_847759065 = 15137;
        private const int FIX_1_961570560 = 16069;
        private const int FIX_2_053119869 = 16819;
        private const int FIX_2_562915447 = 20995;
        private const int FIX_3_072711026 = 25172;

        private const int CONST_BITS = 13;
        private const int PASS1_BITS = 1;
        private const int F1 = CONST_BITS - PASS1_BITS - 1;
        private const int F2 = CONST_BITS - PASS1_BITS;
        private const int F3 = CONST_BITS + PASS1_BITS + 3;

        private static short[] zztable_t81 = new short[64]  {
            0,  1,  8, 16,  9,  2,  3, 10,
            17, 24, 32, 25, 18, 11,  4,  5,
            12, 19, 26, 33, 40, 48, 41, 34,
            27, 20, 13,  6,  7, 14, 21, 28,
            35, 42, 49, 56, 57, 50, 43, 36,
            29, 22, 15, 23, 30, 37, 44, 51,
            58, 59, 52, 45, 38, 31, 39, 46,
            53, 60, 61, 54, 47, 55, 62, 63,
            };

        //private static short[] quantizerValues = new short[64] {  
        //    3,  5,  7,  9, 11, 13, 15, 17,
        //    5,  7,  9, 11, 13, 15, 17, 19,
        //    7,  9, 11, 13, 15, 17, 19, 21,
        //    9, 11, 13, 15, 17, 19, 21, 23,
        //    11, 13, 15, 17, 19, 21, 23, 25,
        //    13, 15, 17, 19, 21, 23, 25, 27,
        //    15, 17, 19, 21, 23, 25, 27, 29,
        //    17, 19, 21, 23, 25, 27, 29, 31
        //    };

        private static byte[] nlz_Table = new byte[]
                {32,31, 99,16, 99,30, 3, 99,  15, 99, 99, 99,29,10, 2, 99,
                99, 99,12,14,21, 99,19, 99,   99,28, 99,25, 99, 9, 1, 99,
                17, 99, 4, 99, 99, 99,11, 99,  13,22,20, 99,26, 99, 99,18,
                5, 99, 99,23, 99,27, 99, 6,   99,24, 7, 99, 8, 99, 0, 99};


        private short[] dataBlockBuffer = new short[MCU_BLOCK_SIZE];

        private uint _streamField;
        private int _streamFieldBitIndex;
        private int _streamIndex;

        private bool _pictureComplete;
        private int _quantizerMode;
        private int _pictureType;

        private int _sliceCount;
        private int _sliceIndex;
        private int _blockCount;

        private int _width;
        private int _height;

        private byte[] _imageStream;
        private ImageSlice _imageSlice;
        private int[] _pixelData;


        public UvlcVideoDecoder()
        {
        }

        public void AddImageStream(byte[] stream)
        {
            _imageStream = stream;
            ProcessStream();
        }

        private void ProcessStream()
        {
            //Set StreamFieldBitIndex to 32 to make sure that the first call to ReadStreamData 
            //actually consumes data from the stream
            _streamFieldBitIndex = 32;
            _streamField = 0;
            _streamIndex = 0;
            _sliceIndex = 0;
            _pictureComplete = false;

            while (!_pictureComplete && (_streamIndex < _imageStream.Length))
            {
                this.ReadHeader();

                if (_pictureComplete) break;

                for (int count = 0; count < _blockCount; count++)
                {
                    uint macroBlockEmpty = ReadStreamData(1);
                    if (macroBlockEmpty == 0)
                    {
                        uint acCoefficients = ReadStreamData(8);
                        if ((acCoefficients >> 6 & 1) == 1)
                        {
                            uint quantizerMode = ReadStreamData(2);
                            int dquant = (int)((quantizerMode < 2) ? ~quantizerMode : quantizerMode);
                            _quantizerMode += dquant;
                        }

                        MacroBlock mb = _imageSlice.MacroBlocks[count];

                        //Block Y0
                        UvlcReadBlock((acCoefficients >> 0 & 1) == 1);
                        InverseDCT(dataBlockBuffer, mb.DataBlocks[0]);
                        //Block Y1
                        UvlcReadBlock((acCoefficients >> 1 & 1) == 1);
                        InverseDCT(dataBlockBuffer, mb.DataBlocks[1]);
                        // Block Y2
                        UvlcReadBlock((acCoefficients >> 2 & 1) == 1);
                        InverseDCT(dataBlockBuffer, mb.DataBlocks[2]);
                        // Block Y3
                        UvlcReadBlock((acCoefficients >> 3 & 1) == 1);
                        InverseDCT(dataBlockBuffer, mb.DataBlocks[3]);
                        // Block Cb
                        UvlcReadBlock((acCoefficients >> 4 & 1) == 1);
                        InverseDCT(dataBlockBuffer, mb.DataBlocks[4]);
                        // Block Cr
                        UvlcReadBlock((acCoefficients >> 5 & 1) == 1);
                        InverseDCT(dataBlockBuffer, mb.DataBlocks[5]);
                    }
                }

                this.ComposeImageSlice();
            }        

            if (this.ImageComplete != null)
            {
                this.ImageComplete(this, new DroneImageCompleteEventArgs(_width, _height, _pixelData));

                // create new writeable bitmap
                //if (_bitmap == null)
                //{
#if WINDOWS_PHONE
                   // _bitmap = new WriteableBitmap(_width, _height);
#else
                   // _bitmap = new WriteableBitmap(_width, _height, 96, 96, PixelFormats.Bgr32, null);
#endif                  
                //}

                //_bitmap.LoadPixels(ref _pixelData);

                // fire event
                //this.ImageComplete(this, new DroneImageCompleteEventArgs((WriteableBitmap)_bitmap.GetAsFrozen()));
            }
        }

        private void ReadHeader()
        {
            this.AlignStreamData();

            uint code = this.ReadStreamData(22);
            uint startCode = (uint)(code & ~0x1F);

            if (startCode != 32) return;

            if ((code & 0x1F) == 0x1F)
            {
                _pictureComplete = true;
                return;
            }
            
            if (_sliceIndex++ == 0)
            {
                int pictureFormat = (int)this.ReadStreamData(2);
                int resolution = (int)this.ReadStreamData(3);
                _pictureType = (int)this.ReadStreamData(3);
                _quantizerMode = (int)this.ReadStreamData(5);
                int frameIndex = (int)this.ReadStreamData(32);

                switch (pictureFormat)
                {
                    case (int)PictureFormats.Cif:
                        _width = QQCIF_WIDTH << resolution - 1;
                        _height = QQCIF_HEIGHT << resolution - 1;
                        break;
                    case (int)PictureFormats.Vga:
                        _width = QQVGA_WIDTH << resolution - 1;
                        _height = QQVGA_HEIGHT << resolution - 1;
                        break;
                }

                _sliceCount = _height >> 4;
                _blockCount = _width >> 4;

                if (_imageSlice == null)
                {
                    _imageSlice = new ImageSlice(_blockCount);
                    _pixelData = new int[_width * _height];
                }
                else if (_imageSlice.MacroBlocks.Count != _blockCount)
                {
                    _imageSlice = new ImageSlice(_blockCount);
                    _pixelData = new int[_width * _height];
                }
            }
            else
            {
                _quantizerMode = (int)this.ReadStreamData(5);
            }
        }

        private void UvlcReadBlock(bool acCoefficientsAvailable)
        {
            int run = 0;
            int level = 0;
            int zigZagPosition = 0;
            int matrixPosition = 0;
            bool last = false;
            // default (table quantization is quant(int,2);
            int quant = _quantizerMode == CONST_TableQuantization ? 2 : _quantizerMode; 

            Array.Clear(dataBlockBuffer, 0, dataBlockBuffer.Length);

            dataBlockBuffer[0] = (short)this.ReadStreamData(10);

            if (acCoefficientsAvailable)
            {
                while (!last)
                {
                    this.UvlcDecode(ref run, ref level, ref last);

                    if (!last)
                    {
                        zigZagPosition += run + 1;
                        matrixPosition = zztable_t81[zigZagPosition];
                        dataBlockBuffer[matrixPosition] = (short)level;
                    }
                }
            }

            // unquantize datablock
            for (int i = 0; i < dataBlockBuffer.Length; i++)
            {
                //dataBlockBuffer[i] = (short)((int)dataBlockBuffer[i] * (int)quantizerValues[i]);
                //dataBlockBuffer[i] = (short)((int)dataBlockBuffer[i] * (1 + (1 + (i / 8) + (i % 8)) * quant));
                dataBlockBuffer[i] = (short)((int)dataBlockBuffer[i] * (1 + (1 + (i >> 3) + (i & 0x7)) * quant));
            }
        }

        private void UvlcDecode(ref int run, ref int level, ref bool last)
        {
            int streamLength = 0, temp = 0, sign = 0;

            //Use the RLE and Huffman dictionaries to understand this code fragment. You can find 
            //them in the developers guide on page 34.
            //The bits in the data are actually composed of two kinds of fields:
            // - run fields - this field contains information on the number of consecutive zeros.
            // - level fields - this field contains the actual non zero value which can be negative or positive.
            //First we extract the run field info and then the level field info.
            
            uint streamCode = this.PeekStreamData(32);

            // Determine number of consecutive zeros in zig zag. (a.k.a 'run' field info)
            int zeroCount = Nlz(streamCode); // - (1)
            
            streamCode <<= zeroCount + 1; // - (2) -> shift left to get rid of the coarse value
            streamLength += zeroCount + 1; // - position bit pointer to keep track off how many bits to consume later on the stream.

            if (zeroCount > 1)
            {
                temp = (int)(streamCode >> (32 - (zeroCount - 1))); // - (2) -> shift right to determine the addtional bits (number of additional bits is zerocount - 1)
                streamCode <<= zeroCount - 1; // - shift all of the run bits out of the way so the first bit is points to the first bit of the level field.
                streamLength += zeroCount - 1;// - position bit pointer to keep track off how many bits to consume later on the stream.
                run = temp + (1 << (zeroCount - 1)); // - (3) -> calculate run value
            }
            else
            {
                run = zeroCount;
            }

            // Determine non zero value. (a.k.a 'level' field info)
            zeroCount = Nlz(streamCode);
            
            streamCode <<= zeroCount + 1; // - (1)
            streamLength += zeroCount + 1; // - position bit pointer to keep track off how many bits to consume later on the stream.

            if (zeroCount == 1)
            {
                //If coarse value is 01 according to the Huffman dictionary this means EOB, so there is
                //no run and level and we indicate this by setting last to true;
                run = 0;
                last = true;
            }
            else
            {
                if (zeroCount == 0)
                {
                    zeroCount = 1;
                    temp = 1;
                }

                streamLength += zeroCount;// - position bit pointer to keep track off how many bits to consume later on the stream.
                streamCode >>= (32 - zeroCount);// - (2) -> shift right to determine the addtional bits (number of additional bits is zerocount)
                sign = (int)(streamCode & 1); // determine sign, last bit is sign 

                if (zeroCount != 0)
                {
                    temp = (int)(streamCode >> 1); // take into account that last bit is sign, so shift it out of the way
                    temp += (int)(1 << (zeroCount - 1)); // - (3) -> calculate run value without sign
                }

                level = (sign == 1) ? -temp : temp; // - (3) -> calculate run value with sign
                last = false;
            }

            this.ReadStreamData(streamLength);
        }

        private uint ReadStreamData(int count)
        {
            uint data = 0;

            if (count > (32 - _streamFieldBitIndex))
            {
                data = _streamField >> _streamFieldBitIndex;

                count -= 32 - _streamFieldBitIndex;

                //_streamField = BitConverter.ToUInt32(_imageStream, _streamIndex);
                // faster implementation of BitConverter.ToUInt32(_imageStream, _streamIndex)
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

        private uint PeekStreamData(int count)
        {
            uint data = 0;
            uint streamField = _streamField;
            int streamFieldBitIndex = _streamFieldBitIndex;

            if (count > (32 - streamFieldBitIndex) && (_streamIndex < _imageStream.Length))
            {
                data = streamField >> streamFieldBitIndex;

                count -= 32 - streamFieldBitIndex;

                //streamField = BitConverter.ToUInt32(_imageStream, _streamIndex);
                // faster implementation of BitConverter.ToUInt32(stream, _streamIndex)
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

        // Blockline:
        //  _______
        // | 1 | 2 |
        // |___|___|  Y
        // | 3 | 4 |
        // |___|___|
        //  ___
        // | 5 |
        // |___| Cb
        //  ___
        // | 6 |
        // |___| Cr
        //
        // Layout in memory
        //  _______________________
        // | 1 | 2 | 3 | 4 | 5 | 6 | ...
        // |___|___|___|___|___|___|
        //
        private void ComposeImageSlice()
        {
            int[] cromaQuadrantOffsets = new int[4] { 0, 4, 32, 36 };
            int[] pixelDataQuadrantOffsets = new int[4] { 0, MCU_WIDTH, _width * MCU_WIDTH, (_width * MCU_WIDTH) + MCU_WIDTH };

            int imageDataOffset = (_sliceIndex - 1) * _width * 16;

            for(int index = 0; index < _imageSlice.MacroBlocks.Count; index++)
            {
                MacroBlock macroBlock = _imageSlice.MacroBlocks[index];
                for (int verticalStep = 0; verticalStep < MCU_WIDTH / 2; verticalStep++)
                {
                    int chromaOffset = verticalStep * MCU_WIDTH;
                    int lumaElementIndex1 = verticalStep * MCU_WIDTH * 2;
                    int lumaElementIndex2 = lumaElementIndex1 + MCU_WIDTH;

                    int dataIndex1 = imageDataOffset + (2 * verticalStep * _width);
                    int dataIndex2 = dataIndex1 + _width;

                    for (int horizontalStep = 0; horizontalStep < MCU_WIDTH / 2; horizontalStep++)
                    {
                        for (int quadrant = 0; quadrant < 4; quadrant++)
                        {
                            short[] y_buf = macroBlock.DataBlocks[quadrant];

                            int chromaIndex = chromaOffset + cromaQuadrantOffsets[quadrant] + horizontalStep;
                            int chromaBlueValue = macroBlock.DataBlocks[4][chromaIndex];
                            int chromaRedValue = macroBlock.DataBlocks[5][chromaIndex];

                            int u = chromaBlueValue - 128;
                            int ug = 88 * u;
                            int ub = 454 * u;

                            int v = chromaRedValue - 128;
                            int vg = 183 * v;
                            int vr = 359 * v;


                            int deltaIndex = 2 * horizontalStep;
                            int y_up_read = y_buf[lumaElementIndex1 + deltaIndex] << 8;
                            int y_down_read = y_buf[lumaElementIndex2 + deltaIndex] << 8;

                            // rgb up read val
                            int r = y_up_read + vr;
                            int g = y_up_read - ug - vg;
                            int b = y_up_read + ub;

                            //int rgbVal = MakeRgb32(r, g, b);
                            // inline MakeRgb32(r, g, b)
                            int rsat = r < 0 ? 0 : r > 0xffff ? 0xff : r >> 8;
                            int gsat = g < 0 ? 0 : g > 0xffff ? 0xff : g >> 8;
                            int bsat = b < 0 ? 0 : b > 0xffff ? 0xff : b >> 8;
                            int rgbVal = (rsat << 16) | (gsat << 8) | bsat;
                            _pixelData[dataIndex1 + pixelDataQuadrantOffsets[quadrant] + deltaIndex] = rgbVal;

                            // rgb down read val
                            r = y_down_read + vr;
                            g = y_down_read - ug - vg;
                            b = y_down_read + ub;

                            //rgbVal = MakeRgb32(r, g, b);
                            // inline MakeRgb32(r, g, b)
                            rsat = r < 0 ? 0 : r > 0xffff ? 0xff : r >> 8;
                            gsat = g < 0 ? 0 : g > 0xffff ? 0xff : g >> 8;
                            bsat = b < 0 ? 0 : b > 0xffff ? 0xff : b >> 8;
                            rgbVal = (int)((rsat << 16) | (gsat << 8) | bsat);
                            _pixelData[dataIndex2 + pixelDataQuadrantOffsets[quadrant] + deltaIndex] = rgbVal;

                            // 2nd round
                            deltaIndex++;
                            y_up_read = y_buf[lumaElementIndex1 + deltaIndex] << 8;
                            y_down_read = y_buf[lumaElementIndex2 + deltaIndex] << 8;

                            // rgb up read val
                            r = y_up_read + vr;
                            g = y_up_read - ug - vg;
                            b = y_up_read + ub;

                            //int rgbVal = MakeRgb32(r, g, b);
                            // inline MakeRgb32(r, g, b)
                            rsat = r < 0 ? 0 : r > 0xffff ? 0xff : r >> 8;
                            gsat = g < 0 ? 0 : g > 0xffff ? 0xff : g >> 8;
                            bsat = b < 0 ? 0 : b > 0xffff ? 0xff : b >> 8;
                            rgbVal = (rsat << 16) | (gsat << 8) | bsat;
                            _pixelData[dataIndex1 + pixelDataQuadrantOffsets[quadrant] + deltaIndex] = rgbVal;

                            // rgb down read val
                            r = y_down_read + vr;
                            g = y_down_read - ug - vg;
                            b = y_down_read + ub;

                            //rgbVal = MakeRgb32(r, g, b);
                            // inline MakeRgb32(r, g, b)
                            rsat = r < 0 ? 0 : r > 0xffff ? 0xff : r >> 8;
                            gsat = g < 0 ? 0 : g > 0xffff ? 0xff : g >> 8;
                            bsat = b < 0 ? 0 : b > 0xffff ? 0xff : b >> 8;
                            rgbVal = (int)((rsat << 16) | (gsat << 8) | bsat);
                            _pixelData[dataIndex2 + pixelDataQuadrantOffsets[quadrant] + deltaIndex] = rgbVal;
                        }
                    }
                }

                imageDataOffset += 16;
            }
        }

        private static int Nlz(uint x)
        {
            x |= (x >> 1);    // Propagate leftmost
            x |= (x >> 2);    // 1-bit to the right.
            x |= (x >> 4);
            x |= (x >> 8);
            x |= (x >> 16);
            x *= 0x06EB14F9;    // Multiplier is 7*255**3.
            return nlz_Table[x >> 26];
        }
      
        private static void InverseDCT(short[] inbuf, short[] outbuf)
        {
            int[] workSpace = new int[MCU_BLOCK_SIZE];
            short[] data = new short[MCU_BLOCK_SIZE];

            int z1, z2, z3, z4, z5;
            int tmp0, tmp1, tmp2, tmp3;
            int tmp10, tmp11, tmp12, tmp13;

            int pointer = 0;

            for (int index = 0; index < MCU_WIDTH; index++)
            {
                if (inbuf[pointer + 8] == 0 &&
                    inbuf[pointer + 16] == 0 &&
                    inbuf[pointer + 24] == 0 &&
                    inbuf[pointer + 32] == 0 &&
                    inbuf[pointer + 40] == 0 &&
                    inbuf[pointer + 48] == 0 &&
                    inbuf[pointer + 56] == 0)
                {
                    int dcValue = inbuf[pointer] << PASS1_BITS;

                    workSpace[pointer + 0] = dcValue;
                    workSpace[pointer + 8] = dcValue;
                    workSpace[pointer + 16] = dcValue;
                    workSpace[pointer + 24] = dcValue;
                    workSpace[pointer + 32] = dcValue;
                    workSpace[pointer + 40] = dcValue;
                    workSpace[pointer + 48] = dcValue;
                    workSpace[pointer + 56] = dcValue;

                    pointer++;
                    continue;
                }

                z2 = inbuf[pointer + 16];
                z3 = inbuf[pointer + 48];

                z1 = (z2 + z3) * FIX_0_541196100;
                tmp2 = z1 + z3 * -FIX_1_847759065;
                tmp3 = z1 + z2 * FIX_0_765366865;

                z2 = inbuf[pointer];
                z3 = inbuf[pointer + 32];

                tmp0 = (z2 + z3) << CONST_BITS;
                tmp1 = (z2 - z3) << CONST_BITS;

                tmp10 = tmp0 + tmp3;
                tmp13 = tmp0 - tmp3;
                tmp11 = tmp1 + tmp2;
                tmp12 = tmp1 - tmp2;

                tmp0 = inbuf[pointer + 56];
                tmp1 = inbuf[pointer + 40];
                tmp2 = inbuf[pointer + 24];
                tmp3 = inbuf[pointer + 8];

                z1 = tmp0 + tmp3;
                z2 = tmp1 + tmp2;
                z3 = tmp0 + tmp2;
                z4 = tmp1 + tmp3;
                z5 = (z3 + z4) * FIX_1_175875602;

                tmp0 = tmp0 * FIX_0_298631336;
                tmp1 = tmp1 * FIX_2_053119869;
                tmp2 = tmp2 * FIX_3_072711026;
                tmp3 = tmp3 * FIX_1_501321110;
                z1 = z1 * -FIX_0_899976223;
                z2 = z2 * -FIX_2_562915447;
                z3 = z3 * -FIX_1_961570560;
                z4 = z4 * -FIX_0_390180644;

                z3 += z5;
                z4 += z5;

                tmp0 += z1 + z3;
                tmp1 += z2 + z4;
                tmp2 += z2 + z3;
                tmp3 += z1 + z4;

                workSpace[pointer + 0] = ((tmp10 + tmp3 + (1 << F1)) >> F2);
                workSpace[pointer + 56] = ((tmp10 - tmp3 + (1 << F1)) >> F2);
                workSpace[pointer + 8] = ((tmp11 + tmp2 + (1 << F1)) >> F2);
                workSpace[pointer + 48] = ((tmp11 - tmp2 + (1 << F1)) >> F2);
                workSpace[pointer + 16] = ((tmp12 + tmp1 + (1 << F1)) >> F2);
                workSpace[pointer + 40] = ((tmp12 - tmp1 + (1 << F1)) >> F2);
                workSpace[pointer + 24] = ((tmp13 + tmp0 + (1 << F1)) >> F2);
                workSpace[pointer + 32] = ((tmp13 - tmp0 + (1 << F1)) >> F2);

                pointer++;
            }

            pointer = 0;

            for (int index = 0; index < MCU_WIDTH; index++)
            {
                z2 = workSpace[pointer + 2];
                z3 = workSpace[pointer + 6];

                z1 = (z2 + z3) * FIX_0_541196100;
                tmp2 = z1 + z3 * -FIX_1_847759065;
                tmp3 = z1 + z2 * FIX_0_765366865;

                tmp0 = (workSpace[pointer + 0] + workSpace[pointer + 4]) << CONST_BITS;
                tmp1 = (workSpace[pointer + 0] - workSpace[pointer + 4]) << CONST_BITS;

                tmp10 = tmp0 + tmp3;
                tmp13 = tmp0 - tmp3;
                tmp11 = tmp1 + tmp2;
                tmp12 = tmp1 - tmp2;

                tmp0 = workSpace[pointer + 7];
                tmp1 = workSpace[pointer + 5];
                tmp2 = workSpace[pointer + 3];
                tmp3 = workSpace[pointer + 1];

                z1 = tmp0 + tmp3;
                z2 = tmp1 + tmp2;
                z3 = tmp0 + tmp2;
                z4 = tmp1 + tmp3;

                z5 = (z3 + z4) * FIX_1_175875602;

                tmp0 = tmp0 * FIX_0_298631336;
                tmp1 = tmp1 * FIX_2_053119869;
                tmp2 = tmp2 * FIX_3_072711026;
                tmp3 = tmp3 * FIX_1_501321110;
                z1 = z1 * -FIX_0_899976223;
                z2 = z2 * -FIX_2_562915447;
                z3 = z3 * -FIX_1_961570560;
                z4 = z4 * -FIX_0_390180644;

                z3 += z5;
                z4 += z5;

                tmp0 += z1 + z3;
                tmp1 += z2 + z4;
                tmp2 += z2 + z3;
                tmp3 += z1 + z4;

                data[pointer + 0] = (short)((tmp10 + tmp3) >> F3);
                data[pointer + 7] = (short)((tmp10 - tmp3) >> F3);
                data[pointer + 1] = (short)((tmp11 + tmp2) >> F3);
                data[pointer + 6] = (short)((tmp11 - tmp2) >> F3);
                data[pointer + 2] = (short)((tmp12 + tmp1) >> F3);
                data[pointer + 5] = (short)((tmp12 - tmp1) >> F3);
                data[pointer + 3] = (short)((tmp13 + tmp0) >> F3);
                data[pointer + 4] = (short)((tmp13 - tmp0) >> F3);

                pointer += 8;
            }

            Array.Copy(data, outbuf, data.Length);
        }
    }

    class ImageSlice
    {
        public List<MacroBlock> MacroBlocks { get; set; }
        //public short[, ,] MacroBlocks2 { get; set; }


        public ImageSlice(int macroBlockCount)
        {
            MacroBlocks = new List<MacroBlock>();

            for (int index = 0; index < macroBlockCount; index++)
            {
                MacroBlocks.Add(new MacroBlock());
            }

            ///
            //MacroBlocks2 = new short[macroBlockCount, 5][64];
            //short[] data = new short[64];
            //Array.Copy(data, MacroBlocks2[4,5], data.Length);
        }
    }

    class MacroBlock
    {
        internal List<short[]> DataBlocks { get; set; }

        public MacroBlock()
        {
            DataBlocks = new List<short[]>();

            for (int index = 0; index < 6; index++)
            {
                DataBlocks.Add(new short[64]);
            }
        }
    }
}
