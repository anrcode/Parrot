using System;


namespace Parrot.DroneControl.Video
{
    public interface IDroneVideoDecoder
    {
        event EventHandler<DroneImageCompleteEventArgs> ImageComplete;
        void AddImageStream(byte[] stream);
    }
}
