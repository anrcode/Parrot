using System;
using System.IO;
using System.Runtime.InteropServices;


namespace Parrot.DroneControl.Protocol
{
    /// <summary>
    /// This class contains the structures that is returned by the navigation
    /// communication channel.
    /// </summary>
    internal class NavigationData
    {
        private static readonly int _navDataHeaderSize = 16; //Marshal.SizeOf(typeof(NavDataHeader));
        private static readonly int _navDataStructSize = 4; //Marshal.SizeOf(typeof(NavDataStructure));

        public NavDataHeader Header { get; private set; }
        public NavDataDrone DroneData { get; private set; }
        public NavVisionDetect VisionDetect { get; private set; }
        public bool ContainsValidData { get; private set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationData"/> class.
        /// </summary>
        private NavigationData()
        {
            //this.Header = new NavDataHeader();
            //this.DroneData = new NavDataDrone();
            //this.VisionDetect = new NavVisionDetect();
        }

        /// <summary>
        /// Deserializes the navigation data.
        /// </summary>
        /// <param name="data">The navigation data.</param>
        /// <returns>The deserialized navigation data object.</returns>
        public static NavigationData Parse(byte[] data)
        {
            if ((data == null) || (data.Length < _navDataHeaderSize))
            {
                return new NavigationData();
            }

            return ParseBinary(data);
        }

        private static NavigationData ParseStruct(byte[] data)
        {
            NavigationData navdata = new NavigationData();

            int position = 0;
            navdata.Header = Helpers.BytesToStruct<NavDataHeader>(data);
            position += _navDataHeaderSize;

            // go through the appended tag structures
            while (position + _navDataStructSize <= data.Length)
            {
                NavDataStructure navDataStruct = Helpers.BytesToStruct<NavDataStructure>(data, position);

                // validate data, check size of tag and remaining buffer
                if ((navDataStruct.Size == 0) ||
                    (position + navDataStruct.Size > data.Length)) return navdata;

                switch ((NavigationDataTag)navDataStruct.Tag)
                {
                    case NavigationDataTag.NavDataDemo:
                        navdata.DroneData = Helpers.BytesToStruct<NavDataDrone>(data, position);
                        break;
                    case NavigationDataTag.NavDataCks:
                        NavDataCheckSum checkSumData = Helpers.BytesToStruct<NavDataCheckSum>(data, position);
                        UInt32 calcChkSum = CalculateCheckSum(data);
                        navdata.ContainsValidData = checkSumData.CheckSum == calcChkSum;
                        break;
                    case NavigationDataTag.NavDataVisionDetect:
                        navdata.VisionDetect = Helpers.BytesToStruct<NavVisionDetect>(data, position);
                        break;
                    default:
                        break;
                }
                // move to next tag position
                position += navDataStruct.Size;
            }
            // return the deserialized navdata
            return navdata;
        }

        private static NavigationData ParseBinary(byte[] data)
        {
            NavigationData navdata = new NavigationData();
            // initialize the streams and readers
            MemoryStream ms = new MemoryStream(data);
            BinaryReader br = new BinaryReader(ms);
            int position = 0;

            // read header
            NavDataHeader header = new NavDataHeader();
            header.Header = br.ReadUInt32();
            header.DroneStatus = br.ReadUInt32();
            header.Sequence = br.ReadUInt32();
            header.VisionDefined = br.ReadUInt32();
            navdata.Header = header;
            position += _navDataHeaderSize;

            // go through the appended tag structures
            while (position + _navDataStructSize <= data.Length)
            {
                // prepare stream for reading tag structure
                ms.Seek(position, SeekOrigin.Begin);
                br = new BinaryReader(ms);

                // read tag structure
                NavDataStructure navDataStruct = new NavDataStructure();
                navDataStruct.Tag = br.ReadUInt16();
                navDataStruct.Size = br.ReadUInt16();

                // validate data, check size of tag and remaining buffer
                if ((navDataStruct.Size == 0) ||
                    (position + navDataStruct.Size > data.Length)) return navdata;

                switch ((NavigationDataTag)navDataStruct.Tag)
                {
                    case NavigationDataTag.NavDataDemo:
                        NavDataDrone droneData = new NavDataDrone();
                        droneData.Tag = navDataStruct.Tag;
                        droneData.Size = navDataStruct.Size;
                        droneData.ControlStatus = br.ReadUInt32();
                        droneData.FlyingPercentage = br.ReadUInt32();
                        droneData.Theta = br.ReadSingle();
                        droneData.Phi = br.ReadSingle();
                        droneData.Psi = br.ReadSingle();
                        droneData.Altitude = br.ReadInt32();
                        droneData.VelocityX = br.ReadSingle();
                        droneData.VelocityY = br.ReadSingle();
                        droneData.VelocityZ = br.ReadSingle();
                        droneData.LastFrameIndex = br.ReadUInt32();
                        navdata.DroneData = droneData;
                        break;
                    case NavigationDataTag.NavDataCks:
                        NavDataCheckSum cs = new NavDataCheckSum();
                        cs.Tag = navDataStruct.Tag;
                        cs.Size = navDataStruct.Size;
                        cs.CheckSum = br.ReadUInt32();
                        UInt32 calcChkSum = CalculateCheckSum(data);
                        navdata.ContainsValidData = cs.CheckSum == calcChkSum;
                        break;
                    case NavigationDataTag.NavDataVisionDetect:
                        NavVisionDetect vd = new NavVisionDetect();
                        vd.Tag = navDataStruct.Tag;
                        vd.Size = navDataStruct.Size;
                        vd.TagCount = br.ReadUInt32();
                        vd.Tag1Type = br.ReadUInt32();
                        vd.Tag2Type = br.ReadUInt32();
                        vd.Tag3Type = br.ReadUInt32();
                        vd.Tag4Type = br.ReadUInt32();
                        vd.Tag1X = br.ReadUInt32();
                        vd.Tag2X = br.ReadUInt32();
                        vd.Tag3X = br.ReadUInt32();
                        vd.Tag4X = br.ReadUInt32();
                        vd.Tag1Y = br.ReadUInt32();
                        vd.Tag2Y = br.ReadUInt32();
                        vd.Tag3Y = br.ReadUInt32();
                        vd.Tag4Y = br.ReadUInt32();
                        vd.Tag1BoxWidth = br.ReadUInt32();
                        vd.Tag2BoxWidth = br.ReadUInt32();
                        vd.Tag3BoxWidth = br.ReadUInt32();
                        vd.Tag4BoxWidth = br.ReadUInt32();
                        vd.Tag1BoxHeight = br.ReadUInt32();
                        vd.Tag2BoxHeight = br.ReadUInt32();
                        vd.Tag3BoxHeight = br.ReadUInt32();
                        vd.Tag4BoxHeight = br.ReadUInt32();
                        vd.Tag1Distance = br.ReadUInt32();
                        vd.Tag2Distance = br.ReadUInt32();
                        vd.Tag3Distance = br.ReadUInt32();
                        vd.Tag4Distance = br.ReadUInt32();
                        vd.Tag1OrientationAngle = br.ReadSingle();
                        vd.Tag2OrientationAngle = br.ReadSingle();
                        vd.Tag3OrientationAngle = br.ReadSingle();
                        vd.Tag4OrientationAngle = br.ReadSingle();
                        navdata.VisionDetect = vd;
                        break;
                    default:
                        break;
                }
                // move to next tag position
                position += navDataStruct.Size;
            }
            // return the deserialized navdata
            return navdata;
        }

        /// <summary>
        /// Calculates the check sum.
        /// </summary>
        /// <param name="buffer">The raw byte sequence.</param>
        /// <returns>The calculated checksum of the byte sequence</returns>
        private static UInt32 CalculateCheckSum(byte[] buffer)
        {
            UInt32 checkSum = 0;
            UInt32 temp = 0;

            //Substract the size of the checksum struct
            int size = buffer.Length - 8;
            for (int index = 0; index < size; index++)
            {
                temp = buffer[index];
                checkSum += temp;
            }

            return checkSum;
        }

        private enum NavigationDataTag : uint
        {
            NavDataDemo = 0,
            NavDataTime,
            NavDataRawMeasures,
            NavDataPhysicalMeasures,
            NavDayrosOffsetsS,
            NavDataEulerAngles,
            NavDataReferences,
            NavDataTrims,
            NavDataRcReferences,
            NavDataPwm,
            NavDataAltitude,
            NavDataVisionRaw,
            NavDataVisionOf,
            NavDataVision,
            NavDataVisionPerf,
            NavDataTrackersSend,
            NavDataVisionDetect,
            NavDataWatchDog,
            NavDataAdcDataFrame,
            NavDataCks = 0xFFFF
        }

        // Remark: These types do not contain reference types in order to be able to use
        // unsafe code to map a memory location to the structure. The navigation data info
        // is received within timeframes of about 5 ms. It is clear that processing should
        // be as fast as possible

        [StructLayout(LayoutKind.Sequential)]
        public struct NavDataHeader
        {
            public UInt32 Header;
            public UInt32 DroneStatus;
            public UInt32 Sequence;
            public UInt32 VisionDefined;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NavDataStructure
        {
            public UInt16 Tag;
            public UInt16 Size;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NavDataDrone
        {
            public UInt16 Tag;
            public UInt16 Size;
            public UInt32 ControlStatus;
            public UInt32 FlyingPercentage;
            public Single Theta;
            public Single Phi;
            public Single Psi;
            public Int32 Altitude;
            public Single VelocityX;
            public Single VelocityY;
            public Single VelocityZ;
            public UInt32 LastFrameIndex;
/*
            public Single DetectionCameraRotationm11;
            public Single DetectionCameraRotationm12;
            public Single DetectionCameraRotationm13;
            public Single DetectionCameraRotationm21;
            public Single DetectionCameraRotationm22;
            public Single DetectionCameraRotationm23;
            public Single DetectionCameraRotationm31;
            public Single DetectionCameraRotationm32;
            public Single DetectionCameraRotationm33;

            public Single DetectionCameraTranslationX;
            public Single DetectionCameraTranslationY;
            public Single DetectionCameraTranslationZ;

            public UInt32 DetectionTagIndex;
            public UInt32 DetectionCameraType;

            public Single DroneCameraRotationm11;
            public Single DroneCameraRotationm12;
            public Single DroneCameraRotationm13;
            public Single DroneCameraRotationm21;
            public Single DroneCameraRotationm22;
            public Single DroneCameraRotationm23;
            public Single DroneCameraRotationm31;
            public Single DroneCameraRotationm32;
            public Single DroneCameraRotationm33;

            public Single DroneCameraTranslationX;
            public Single DroneCameraTranslationY;
            public Single DroneCameraTranslationZ;
 */
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NavDataCheckSum
        {
            public UInt16 Tag;
            public UInt16 Size;
            public UInt32 CheckSum;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct NavVisionDetect
        {
            public UInt16 Tag;
            public UInt16 Size;

            public UInt32 TagCount;

            public UInt32 Tag1Type;
            public UInt32 Tag2Type;
            public UInt32 Tag3Type;
            public UInt32 Tag4Type;

            public UInt32 Tag1X;
            public UInt32 Tag2X;
            public UInt32 Tag3X;
            public UInt32 Tag4X;

            public UInt32 Tag1Y;
            public UInt32 Tag2Y;
            public UInt32 Tag3Y;
            public UInt32 Tag4Y;

            public UInt32 Tag1BoxWidth;
            public UInt32 Tag2BoxWidth;
            public UInt32 Tag3BoxWidth;
            public UInt32 Tag4BoxWidth;

            public UInt32 Tag1BoxHeight;
            public UInt32 Tag2BoxHeight;
            public UInt32 Tag3BoxHeight;
            public UInt32 Tag4BoxHeight;

            public UInt32 Tag1Distance;
            public UInt32 Tag2Distance;
            public UInt32 Tag3Distance;
            public UInt32 Tag4Distance;

            public Single Tag1OrientationAngle;
            public Single Tag2OrientationAngle;
            public Single Tag3OrientationAngle;
            public Single Tag4OrientationAngle;
        }
    }
}
