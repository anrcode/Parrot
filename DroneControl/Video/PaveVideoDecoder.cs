using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Parrot.DroneControl.Video
{
    internal class PaveVideoDecoder : IDroneVideoDecoder
    {
        public event EventHandler<DroneImageCompleteEventArgs> ImageComplete;

        public void AddImageStream(byte[] stream)
        {
            
        }
    }
}
