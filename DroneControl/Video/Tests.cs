using System;
using System.IO;


namespace Parrot.DroneControl.Video
{
    public class Tests
    {
        public static void TestImageDecodingPerformance(string filename)
        {
            Logger.Log(LogScope.Debug, "Starting image decoding performance test ...");
            byte[] imgBytes = File.ReadAllBytes(filename);
            IDroneVideoDecoder vi = new UvlcVideoDecoder();
            //vi.ImageComplete += new EventHandler<DroneImageCompleteEventArgs>(vi_ImageComplete);
            DateTime now = DateTime.Now;
            for (int i = 0; i < 1000; i++)
            {
                vi.AddImageStream(imgBytes);
            }
            TimeSpan diff = DateTime.Now - now;
            Logger.Log(LogScope.Debug, "Test finished  ... Time: " + diff.TotalMilliseconds + "ms");
        }
    }
}
