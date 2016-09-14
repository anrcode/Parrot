/*
using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media.Imaging;


namespace Parrot.DroneControl
{
    internal static class WriteableBitmapEx
    {
#if WINDOWS_PHONE
        public static void LoadPixels(this WriteableBitmap bmp, ref int[] pixels)
        {
            if(pixels.Length != bmp.Pixels.Length)
            {
                throw new InvalidOperationException("Unequal pixel array length!");
            }

            //Array.Copy(pixels, bitmapImage.Pixels, pixels.Length);
            Buffer.BlockCopy(pixels, 0, bmp.Pixels, 0, pixels.Length * 4);
        }

        public static WriteableBitmap GetAsFrozen(this WriteableBitmap bmp)
        {
            WriteableBitmap result = new WriteableBitmap(bmp.PixelWidth, bmp.PixelHeight);
            Buffer.BlockCopy(bmp.Pixels, 0, result.Pixels, 0, bmp.Pixels.Length * 4);
            return result;
        }
#else
        public static void LoadPixels(this WriteableBitmap bmp, ref int[] pixels)
        {
            GCHandle pinnedPixData = GCHandle.Alloc(pixels, GCHandleType.Pinned);
            // write pixels to bitmap
            IntPtr bufferPtr = pinnedPixData.AddrOfPinnedObject();

            //bmp.Lock();
            bmp.WritePixels(new Int32Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight), bufferPtr, pixels.Length * 4, bmp.BackBufferStride);
            //bmp.Unlock();
            pinnedPixData.Free();
        }
#endif
    }
}

*/