using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;     //Dispatcher
using System.Windows.Media.Imaging; //WriteableBitmap
using System.Windows.Media;
using System.Windows;   //Rect
using System.Threading;
using Azure.CommandLib;
using Azure.Image.Processing;
using Hats.APDCom;
using Microsoft.Research.DynamicDataDisplay.DataSources;
using Azure.GalilMotor;


namespace Azure.ImagingSystem
{
    public class LaserImageScanCommand : ThreadBase
    {
        // Image scanning status delegate
        public delegate void CommandStatusHandler(object sender, string status);
        // Image scanning status event
        public event CommandStatusHandler CommandStatus;
        public delegate void CommandCompletionEstHandler(ThreadBase sender, DateTime dateTime, double estTime, double percentCompleted);
        // Image capture completion time estimate event
        public event CommandCompletionEstHandler CompletionEstimate;
        // update event while APD receives data
        public delegate void ScanReceiveDataHandler(ThreadBase sender, string scanType);
        public event ScanReceiveDataHandler ReceiveTransfer;

        public delegate void DataReceivedHandler(ushort[] apdChannelA, ushort[] apdChannelB, ushort[] apdChannelC, ushort[] apdChannelD);
        public event DataReceivedHandler DataReceived;

        public delegate void SequentialChannelCompletedHandler(ThreadBase sender, LaserType channel);
        public event SequentialChannelCompletedHandler SequentialChannelCompleted; 

        private APDTransfer _ActiveAPDTransfer = null;
        private GalilMotor.MyGalil _ActiveGalil = null;
        private Dispatcher _CallingDispatcher = null;
        private ImageInfo _ImageInfo = new ImageInfo();
        private int _ImageWidth, _ImageHeight;
        private int _PixelOffset;
        private string _ScanType = string.Empty;    //Dynamic vs Static
        private ScanParameterStruct _ScanParameter = null;
        private int _RemainingTime = 0;
        private bool _ImageIsCapture = true;
        private bool _IsScanAborted = false;
        private bool _IsSaveScanDataOnAborted = false;
        private bool _ScannerIsReady = false;
        private List<Signal> _Signals = null;
        private bool _IsPhosphorImaging = false;
        private bool _IsChannelASelected = false;
        private bool _IsChannelBSelected = false;
        private bool _IsChannelCSelected = false;
        private bool _IsChannelDSelected = false;

        public class ScanParameterStruct
        {
            public int Width;
            public int Height;
            public double XMotorSpeed;
            public double YMotorSpeed;
            public double ZMotorSpeed;
            public int ScanX0;
            public int ScanY0;
            public int ScanZ0;
            public int ScanDeltaX;
            public int ScanDeltaY;
            public int ScanDeltaZ;
            public int Res;
            public int Quality;
            public int DataRate;
            public int LineCounts;
            public int Time;
            public int XMotorSubdivision;
            public int YMotorSubdivision;
            public int ZMotorSubdivision;

            public bool IsChannelALightShadeFix;
            public bool IsChannelBLightShadeFix;
            public bool IsChannelCLightShadeFix;
            public bool IsChannelDLightShadeFix;

            public bool IsUnidirectionalScan;

            public bool IsSequentialScanningOn;
        }

        public LaserImageScanCommand(Dispatcher callingDispatcher,
                                     APDTransfer aPDTransfer,
                                     GalilMotor.MyGalil galil,
                                     ScanParameterStruct scanParameter,
                                     List<Signal> signals,
                                     bool bIsPhosphorImaging)
        {
            _ImageIsCapture = true;
            _CallingDispatcher = callingDispatcher;
            _ActiveAPDTransfer = aPDTransfer;
            _ActiveGalil = galil;
            _ScanParameter = scanParameter;
            _Signals = signals;
            _IsPhosphorImaging = bIsPhosphorImaging;
        }

        public bool ImageIsCapture
        {
            get
            {
                return _ImageIsCapture;
            }
            set
            {
                _ImageIsCapture = value;
            }
        }
        public int RemainingTime
        {
            get
            {
                return _RemainingTime;
            }
        }
        public string ScanType
        {
            get
            {
                return _ScanType;
            }
        }
        public ImageInfo ImageInfo
        {
            get { return _ImageInfo; }
        }
        public bool IsSaveScanDataOnAborted
        {
            get { return _IsSaveScanDataOnAborted; }
            set { _IsSaveScanDataOnAborted = value; }
        }

        #region Dynamic Scan
        private int _ImageOffsetY;
        private ushort[] _APDChannelA;
        private ushort[] _APDChannelB;
        private ushort[] _APDChannelC;
        private ushort[] _APDChannelD;

        public int ImageOffsetY
        {
            get
            {
                return _ImageOffsetY;
            }
        }
        public ushort[] APDChannelA
        {
            get { return _APDChannelA; }
            set { _APDChannelA = value; }
        }
        public ushort[] APDChannelB
        {
            get { return _APDChannelB; }
            set { _APDChannelB = value; }
        }
        public ushort[] APDChannelC
        {
            get { return _APDChannelC; }
            set { _APDChannelC = value; }
        }
        public ushort[] APDChannelD
        {
            get { return _APDChannelD; }
            set { _APDChannelD = value; }
        }

        private WriteableBitmap _ChannelAImage = null;
        private WriteableBitmap _ChannelBImage = null;
        private WriteableBitmap _ChannelCImage = null;
        private WriteableBitmap _ChannelDImage = null;

        public WriteableBitmap ChannelAImage
        {
            get { return _ChannelAImage; }
            set { _ChannelAImage = value; }
        }
        public WriteableBitmap ChannelBImage
        {
            get { return _ChannelBImage; }
            set { _ChannelBImage = value; }
        }
        public WriteableBitmap ChannelCImage
        {
            get { return _ChannelCImage; }
            set { _ChannelCImage = value; }
        }
        public WriteableBitmap ChannelDImage
        {
            get { return _ChannelDImage; }
            set { _ChannelDImage = value; }
        }

        public int ImageWidth
        {
            get { return _ImageWidth; }
        }

        public int ImageHeight
        {
            get { return _ImageHeight; }
        }

        private string _DynamicScanProcess()
        {
            int tstststst = 0;

            long imageDataSize = (long)_ScanParameter.Width * (long)_ScanParameter.Height * 16L + 512L * 32L;
            _ImageWidth = _ScanParameter.ScanDeltaX / _ScanParameter.Res;
            _RemainingTime = _ScanParameter.Time;
            _ImageHeight = _ScanParameter.ScanDeltaY / _ScanParameter.YMotorSubdivision * 1000 / _ScanParameter.Res;
            //MemoryMappedFile imageData = MemoryMappedFile.CreateFromFile(@"testmap", System.IO.FileMode.Create, null, imageDataSize);

            _DynamicScanPreparingWork();
            _ActiveAPDTransfer.IsParametersData = false;//turn APD receive mode to data mode

            // Only allocate memory for the selected scan channel
            int channelDataLength = _ImageWidth * _ImageHeight;
            if (_IsChannelASelected)
                _APDChannelA = new ushort[channelDataLength];
            if (_IsChannelBSelected)
                _APDChannelB = new ushort[channelDataLength];
            if (_IsChannelCSelected)
                _APDChannelC = new ushort[channelDataLength];
            if (_IsChannelDSelected)
                _APDChannelD = new ushort[channelDataLength];

            int imageOffsetX = 0;
            _ImageOffsetY = 0;
            int moduleOneImageX = 0;
            int iNum = 0;
            //int yNum = 0;
            int dataIsGet = 0;
            int bufferOffset = 0;
            int channelDataOffsetAddress = 0;

            try
            {
                while (_ImageOffsetY < _ImageHeight && _ImageIsCapture == true)
                {
                    if (_ActiveAPDTransfer.IsReceived)
                    {
                        //using( MemoryMappedViewAccessor imageDataViewAccessor = imageData.CreateViewAccessor())
                        //{
                        //    imageDataViewAccessor.WriteArray<byte>(dataIsGet * _ActiveAPDTransfer.ReceiveBuffer.Length, _ActiveAPDTransfer.ReceiveBuffer, 0, _ActiveAPDTransfer.ReceiveBuffer.Length);
                        //    imageDataViewAccessor.Flush();
                        //}
                        if (!_ScannerIsReady)
                        {
                            _ScannerIsReady = true;
                            if (ReceiveTransfer != null)
                            {
                                ReceiveTransfer(this, "ScannerIsReady");
                            }
                        }
                        dataIsGet++;
                        //Console.WriteLine("Í¼ÏñÊý¾ÝÈ¡³ö£º" + dataIsGet.ToString());
                        iNum = _ActiveAPDTransfer.BufSz / 16;
                        for (int i = 0; i < iNum; ++i)
                        {
                            bufferOffset = i * 16;
                            tstststst = i;
                            if (_ActiveAPDTransfer.ReceiveBuffer[0 + bufferOffset] == 0x5B)
                            {
                                imageOffsetX = BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 2 + bufferOffset);
                                _ImageOffsetY = BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 4 + bufferOffset);

                                if (_ActiveAPDTransfer.ReceiveBuffer[14 + bufferOffset] == 0x01)
                                {
                                    _ActiveAPDTransfer.LIDIsOpen = false;
                                }
                                else
                                {
                                    _ActiveAPDTransfer.LIDIsOpen = true;
                                }

                                imageOffsetX -= 1;
                                _ImageOffsetY -= 1;
                                if (_ScanParameter.IsUnidirectionalScan)
                                {
                                    _ImageOffsetY /= 2;
                                }

                                bool isEvenLine = _ActiveAPDTransfer.ReceiveBuffer[1 + bufferOffset] == 0xD5;

                                if (isEvenLine)
                                {
                                    imageOffsetX = _ImageWidth + _PixelOffset - imageOffsetX - 1;
                                }

                                if (imageOffsetX + 1 > _PixelOffset)
                                {
                                    moduleOneImageX = imageOffsetX - _PixelOffset;
                                    channelDataOffsetAddress = moduleOneImageX + (_ImageHeight - 1 - _ImageOffsetY) * _ImageWidth;
                                    if (channelDataOffsetAddress < channelDataLength && channelDataOffsetAddress >= 0)
                                    {
                                        if (_IsChannelASelected)
                                        {
                                            if (isEvenLine && _ScanParameter.IsUnidirectionalScan)
                                            {
                                                // use int temperory variable for average to avoid overflow
                                                int averageTmp = _APDChannelA[channelDataOffsetAddress] + BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 6 + bufferOffset);
                                                _APDChannelA[channelDataOffsetAddress] = (ushort)(averageTmp / 2);
                                            }
                                            else
                                            {
                                                _APDChannelA[channelDataOffsetAddress] = BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 6 + bufferOffset);
                                            }
                                        }
                                        if (_IsChannelBSelected)
                                        {
                                            if (isEvenLine && _ScanParameter.IsUnidirectionalScan)
                                            {
                                                // use int temperory variable for average to avoid overflow
                                                int averageTmp = _APDChannelB[channelDataOffsetAddress] + BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 8 + bufferOffset);
                                                _APDChannelB[channelDataOffsetAddress] = (ushort)(averageTmp / 2);
                                            }
                                            else
                                            {
                                                _APDChannelB[channelDataOffsetAddress] = BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 8 + bufferOffset);
                                            }
                                        }
                                    }
                                }
                                if (imageOffsetX < _ImageWidth)
                                {
                                    channelDataOffsetAddress = imageOffsetX + (_ImageHeight - 1 - _ImageOffsetY) * _ImageWidth;
                                    if (channelDataOffsetAddress < channelDataLength && channelDataOffsetAddress >= 0)
                                    {
                                        if (_IsChannelCSelected)
                                        {
                                            if (isEvenLine && _ScanParameter.IsUnidirectionalScan)
                                            {
                                                // use int temperory variable for average to avoid overflow
                                                int averageTmp = _APDChannelC[channelDataOffsetAddress] + BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 10 + bufferOffset);
                                                _APDChannelC[channelDataOffsetAddress] = (ushort)(averageTmp / 2);
                                            }
                                            else
                                            {
                                                _APDChannelC[channelDataOffsetAddress] = BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 10 + bufferOffset);
                                            }
                                        }
                                        if (_IsChannelDSelected)
                                        {
                                            if (isEvenLine && _ScanParameter.IsUnidirectionalScan)
                                            {
                                                // use int temperory variable for average to avoid overflow
                                                int averageTmp = _APDChannelD[channelDataOffsetAddress] + BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 12 + bufferOffset);
                                                _APDChannelD[channelDataOffsetAddress] = (ushort)(averageTmp / 2);
                                            }
                                            else
                                            {
                                                _APDChannelD[channelDataOffsetAddress] = BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 12 + bufferOffset);
                                            }
                                        }
                                    }

                                }
                            }
                            else
                            {
                                //Console.WriteLine("Êý¾Ý·ÖÎö³ö´í£¡");
                            }
                        }

                        if (_RemainingTime > (int)(_ScanParameter.Time - _ScanParameter.Time * _ImageOffsetY / _ImageHeight))
                        {
                            _RemainingTime = (int)(_ScanParameter.Time - _ScanParameter.Time * _ImageOffsetY / _ImageHeight);
                            if (ReceiveTransfer != null)
                            {
                                ReceiveTransfer(this, "RemainingTime");
                                ReceiveTransfer(this, "LIDStatus");
                            }

                            // Notify any subscriber
                            if (DataReceived != null)
                            {
                                DataReceived(_APDChannelA, _APDChannelB, _APDChannelC, _APDChannelD);
                            }
                        }
                    }
                }
                //imageData.Dispose();
            }
            catch
            {
                throw;
            }
            _ActiveAPDTransfer.IsParametersData = true;//turn APD receive mode to parameters mode
            _ActiveAPDTransfer.APDLaserStopScan();
            _RemainingTime = 0;
            if (ReceiveTransfer != null)
            {
                ReceiveTransfer(this, "RemainingTime");
            }
            _ScanType = "Dynamic";

            if (!_ImageIsCapture)
            {
                _IsScanAborted = true;  // Scanning command aborted
            }

            return "Dynamic";
        }

        private bool _DynamicScanPreparingWork()
        {
            _PixelOffset = _ActiveAPDTransfer.DeviceProperties.Pixel_10_Offset * 10 / _ScanParameter.Res;

            _ActiveAPDTransfer.APDLaserScanWidthSet(_ScanParameter.Width + _PixelOffset);
            if (_ScanParameter.IsUnidirectionalScan)
            {
                _ActiveAPDTransfer.APDLaserScanDySet(_ScanParameter.ScanDeltaY / _ScanParameter.YMotorSubdivision * 2);
            }
            else
            {
                _ActiveAPDTransfer.APDLaserScanDySet(_ScanParameter.ScanDeltaY / _ScanParameter.YMotorSubdivision);
            }
            _ActiveAPDTransfer.APDLaserScanDzSet(_ScanParameter.ScanDeltaZ / _ScanParameter.ZMotorSubdivision);
            _ActiveAPDTransfer.APDLaserScanResSet(_ScanParameter.Res);
            _ActiveAPDTransfer.APDLaserScanQualitySet(_ScanParameter.Quality);

            //first,make the motor goes to start position
            _ActiveGalil.SendCommand("HX");
            _ActiveGalil.SendCommand("ST");
            #region motor program one
            string temp = "#SCAN\r" +
                               "SHXYZ\r" +
                               "STXYZ\r" +
                               "SPX=50000\r" +
                               "SPY=5000\r" +
                               "SPZ=2000\r" +
                               "PAB=" + (_ScanParameter.ScanY0).ToString() + "\r" +
                               "BGY\r" +
                               "PAA=" + (_ScanParameter.ScanX0).ToString() + "\r" +
                               "BGX\r" +
                               "PAC=" + (_ScanParameter.ScanZ0).ToString() + "\r" +
                               "BGZ\r" +
                               "AMX\r" +
                               "AMY\r" +
                               "AMZ\r" +
                               "EN";
            #endregion motor program one
            _ActiveGalil.ProgramDownload(temp);
            //waitting for the motor goes to the position
            while (!(_ActiveGalil.XCurrentP == _ScanParameter.ScanX0 &&
                    _ActiveGalil.YCurrentP == _ScanParameter.ScanY0 &&
                    _ActiveGalil.ZCurrentP == _ScanParameter.ScanZ0))
            {
                System.Threading.Thread.Sleep(1);
            }

            #region === Set laser's intensity, apd gain, and apd pga ===
            //
            // Set laser's intensity, apd gain and apd pga
            // after the motors are in the set position
            //
            if (_ActiveAPDTransfer.APDTransferIsAlive)
            {
                foreach (var signal in _Signals)
                {
                    if (_ActiveAPDTransfer.APDTransferIsAlive)
                    {
                        switch (signal.LaserType)
                        {
                            case LaserType.LaserA:
                                _ActiveAPDTransfer.LaserSetA(signal.LaserIntensity);
                                ImagingHelper.Delay(20);
                                _ActiveAPDTransfer.APDSetA(signal.ApdGain);
                                ImagingHelper.Delay(20);
                                _ActiveAPDTransfer.APDPgaSetA(signal.ApdPga);
                                ImagingHelper.Delay(20);
                                //LaserA ImageInfo
                                _ImageInfo.LaserAIntensity = signal.LaserIntensity;
                                _ImageInfo.ApdAGain = signal.ApdGain;
                                _ImageInfo.ApdAPga = signal.ApdPga;
                                break;
                            case LaserType.LaserB:
                                _ActiveAPDTransfer.LaserSetB(signal.LaserIntensity);
                                ImagingHelper.Delay(20);
                                _ActiveAPDTransfer.APDSetB(signal.ApdGain);
                                ImagingHelper.Delay(20);
                                _ActiveAPDTransfer.APDPgaSetB(signal.ApdPga);
                                ImagingHelper.Delay(20);
                                //LaserB ImageInfo
                                _ImageInfo.LaserBIntensity = signal.LaserIntensity;
                                _ImageInfo.ApdBGain = signal.ApdGain;
                                _ImageInfo.ApdBPga = signal.ApdPga;
                                break;
                            case LaserType.LaserC:
                                _ActiveAPDTransfer.LaserSetC(signal.LaserIntensity);
                                ImagingHelper.Delay(20);
                                _ActiveAPDTransfer.APDSetC(signal.ApdGain);
                                ImagingHelper.Delay(20);
                                _ActiveAPDTransfer.APDPgaSetC(signal.ApdPga);
                                ImagingHelper.Delay(20);
                                //LaserC ImageInfo
                                _ImageInfo.LaserCIntensity = signal.LaserIntensity;
                                _ImageInfo.ApdCGain = signal.ApdGain;
                                _ImageInfo.ApdCPga = signal.ApdPga;
                                break;
                            case LaserType.LaserD:
                                if (!_IsPhosphorImaging)
                                {
                                    _ActiveAPDTransfer.LaserSetD(signal.LaserIntensity);
                                    ImagingHelper.Delay(20);
                                    //LaserD ImageInfo
                                    _ImageInfo.LaserDIntensity = signal.LaserIntensity;
                                }
                                _ActiveAPDTransfer.APDSetD(signal.ApdGain);
                                ImagingHelper.Delay(20);
                                _ActiveAPDTransfer.APDPgaSetD(signal.ApdPga);
                                ImagingHelper.Delay(20);
                                _ImageInfo.ApdDGain = signal.ApdGain;
                                _ImageInfo.ApdDPga = signal.ApdPga;
                                break;
                        }
                    }
                }
            }
            #endregion

            System.Threading.Thread.Sleep(3000);    // wait 3 sec for the lasers to warm up before start scanning.
            //when the motor is ready,switch APD on
            _ActiveAPDTransfer.APDLaserStartScan();
            //when the APD is ready,the motor starts to move for scannning
            //double v = _XSpeedMatch[_ScanParameter.ScanDeltaX / 1000];
            //double xMotorSpeed = 2 * (_ScanParameter.ScanDeltaX + _ActiveAPDTransfer.DeviceProperties.ModuleDistance + 4000) * (1.0 + v * (_ScanParameter.ScanDeltaX + _ActiveAPDTransfer.DeviceProperties.ModuleDistance + 4000) / 284000) / _ScanParameter.Quality;
            double xMotorSpeed = XMotorSpeedCalibration.GetSpeed((_ScanParameter.ScanDeltaX + _ActiveAPDTransfer.DeviceProperties.ModuleDistance) / 1000 + 4, _ScanParameter.Quality);
            double ySpeed;
            if (_ScanParameter.IsUnidirectionalScan)
            {
                ySpeed = (_ScanParameter.Res * _ScanParameter.YMotorSubdivision / _ScanParameter.Quality / 1000);
            }
            else
            {
                ySpeed = (_ScanParameter.Res * 2 * _ScanParameter.YMotorSubdivision / _ScanParameter.Quality / 1000);
            }
            #region motor program two
            temp = "#SCAN\r" +
                               "SHXY\r" +
                               "STXYZ\r" +
                               "SPY=" + ((int)(Math.Round(ySpeed, 0))).ToString() + "\r" +
                               "SPX=" + ((int)(Math.Round(xMotorSpeed, 0))).ToString() + "\r" +
                               "PAB=" + (_ScanParameter.ScanY0 + _ScanParameter.ScanDeltaY + _ScanParameter.YMotorSubdivision).ToString() + "\r" +
                               "BGY\r" +
                               "#LOOP\r" +
                               "PAA=" + (_ScanParameter.ScanX0 + _ActiveAPDTransfer.DeviceProperties.ModuleDistance + _ScanParameter.ScanDeltaX + 2000).ToString() + "\r" +
                               "BGX\r" +
                               "AMX\r" +
                               "PAA=" + (_ScanParameter.ScanX0 - 2000).ToString() + "\r" +
                               "BGX\r" +
                               "AMX\r" +
                               "IF(_RPB>" + (_ScanParameter.ScanY0 + (int)(_ScanParameter.ScanDeltaY * 1.1)).ToString() + ")" + "\r" +
                               "JP#END\r" +
                               "ENDIF\r" +
                               "JP#LOOP\r" +
                               "#END\r" +
                               "SPX=10000\r" +
                               "SPY=5000\r" +
                               "HMXY\r" +
                               "BGXY\r" +
                               "AMXY\r" +
                               "EN\r"
                               ;
            #endregion motor program two

            _ActiveGalil.ProgramDownload(temp);
            return true;
        }
        /// <summary>
        /// PositionAdjustment
        /// </summary>
        /// <param name="referenceImage"></param> this image only can channel C(Red channel)
        /// <param name="adjustiveImage"></param> Channel A channel B channel D,the order of position can not be changed
        /// <param name="deltaX"></param> the x-axis offset of other channels to channel C
        /// <param name="deltaY"></param>the y-axis offset of other channels to channel C
        private void _PositionAdjustment(int imageWidth, int imageHeight, WriteableBitmap referenceImage, WriteableBitmap[] adjustiveImage, int[] deltaX, int[] deltaY)
        {
            int positiveDeltaXMax = 0;
            int negativeDeltaXMax = 0;
            int positiveDeltaYMax = 0;
            int negativeDeltaYMax = 0;


            positiveDeltaXMax = deltaX.Max();
            if (positiveDeltaXMax < 0)
            {
                positiveDeltaXMax = 0;
            }
            negativeDeltaXMax = deltaX.Min();
            if (negativeDeltaXMax > 0)
            {
                negativeDeltaXMax = 0;
            }

            positiveDeltaYMax = deltaY.Max();
            if (positiveDeltaYMax < 0)
            {
                positiveDeltaYMax = 0;
            }
            negativeDeltaYMax = deltaY.Min();
            if (negativeDeltaYMax > 0)
            {
                negativeDeltaYMax = 0;
            }
            int destImagePixelWidth = (int)imageWidth + negativeDeltaXMax - positiveDeltaXMax;
            int destImagePixelHeight = (int)imageHeight + negativeDeltaYMax - positiveDeltaYMax;

            if (destImagePixelWidth != imageWidth || destImagePixelHeight != imageHeight)
            {
                int fixSourceImageWidthNoDouble = 0;
                int fixDestImageWidthNoDouble = 0;
                if (imageWidth % 2 != 0)
                {
                    fixSourceImageWidthNoDouble = 1;
                }
                if (destImagePixelWidth % 2 != 0)
                {
                    fixDestImageWidthNoDouble = 1;
                }

                WriteableBitmap[] result = new WriteableBitmap[adjustiveImage.Length + 1];
                unsafe
                {
                    if (referenceImage != null)
                    {
                        result[0] = new WriteableBitmap(destImagePixelWidth, destImagePixelHeight, 300, 300, PixelFormats.Gray16, null);
                        var sourcePtr = (UInt16*)referenceImage.BackBuffer.ToPointer();
                        var destPtr = (UInt16*)result[0].BackBuffer.ToPointer();
                        referenceImage.Lock();
                        result[0].Lock();

                        for (int y = 0; y < destImagePixelHeight; y++)
                        {
                            for (int x = 0; x < destImagePixelWidth; x++)
                            {
                                destPtr[y * (destImagePixelWidth + fixDestImageWidthNoDouble) + x] = sourcePtr[(y + positiveDeltaYMax) * (imageWidth + fixSourceImageWidthNoDouble) + (x + positiveDeltaXMax)];
                            }
                        }
                        referenceImage.Unlock();
                        result[0].Unlock();
                        referenceImage = null;
                    }
                    for (int i = 0; i < result.Length - 1; i++)
                    {
                        if (adjustiveImage[i] != null)
                        {
                            result[i + 1] = new WriteableBitmap(destImagePixelWidth, destImagePixelHeight, 300, 300, PixelFormats.Gray16, null);
                            var sourcePtr = (UInt16*)adjustiveImage[i].BackBuffer.ToPointer();
                            var destPtr = (UInt16*)result[i + 1].BackBuffer.ToPointer();

                            adjustiveImage[i].Lock();
                            result[i + 1].Lock();

                            for (int y = 0; y < destImagePixelHeight; y++)
                            {
                                for (int x = 0; x < destImagePixelWidth; x++)
                                {
                                    destPtr[y * (destImagePixelWidth + fixDestImageWidthNoDouble) + x] =
                                        sourcePtr[(y + positiveDeltaYMax - deltaY[i]) * (adjustiveImage[i].PixelWidth + fixSourceImageWidthNoDouble) + (x + positiveDeltaXMax - deltaX[i])];
                                }
                            }
                            adjustiveImage[i].Unlock();
                            result[i + 1].Unlock();
                            adjustiveImage[i] = null;
                        }
                    }
                    _ChannelCImage = result[0];
                    _ChannelAImage = result[1];
                    _ChannelBImage = result[2];
                    _ChannelDImage = result[3];
                }
            }
            if (_ChannelCImage != null)
            {
                if (_ChannelCImage.CanFreeze)
                { _ChannelCImage.Freeze(); }
            }
            if (_ChannelAImage != null)
            {
                if (_ChannelAImage.CanFreeze)
                { _ChannelAImage.Freeze(); }
            }
            if (_ChannelBImage != null)
            {
                if (_ChannelBImage.CanFreeze)
                { _ChannelBImage.Freeze(); }
            }
            if (_ChannelDImage != null)
            {
                if (_ChannelDImage.CanFreeze)
                { _ChannelDImage.Freeze(); }
            }
        }

        private void _LightShadeFix(WriteableBitmap sample)
        {
            UInt16 meanValue = 0;
            int fixNoDouble = 0;
            if (sample.PixelWidth % 2 != 0)
            {
                fixNoDouble = 1;
            }
            unsafe
            {
                var imagePtr = (UInt16*)sample.BackBuffer.ToPointer();
                sample.Lock();

                for (int y = 0; y < sample.PixelHeight - 1; y++)
                {
                    for (int x = 0; x < sample.PixelWidth; x++)
                    {
                        meanValue = (UInt16)(((UInt32)imagePtr[y * (sample.PixelWidth + fixNoDouble) + x]
                                    + (UInt32)imagePtr[(y + 1) * (sample.PixelWidth + fixNoDouble) + x]) / 2);
                        imagePtr[y * (sample.PixelWidth + fixNoDouble) + x] = meanValue;
                    }
                }
                sample.Unlock();
            }
        }
        #endregion Dynamic Scan

        #region Static Scan

        EnumerableDataSource<Point> _StaticChannelA;
        EnumerableDataSource<Point> _StaticChannelB;
        EnumerableDataSource<Point> _StaticChannelC;
        EnumerableDataSource<Point> _StaticChannelD;

        Point[] _StaticArrayChannelA;
        Point[] _StaticArrayChannelB;
        Point[] _StaticArrayChannelC;
        Point[] _StaticArrayChannelD;
        public EnumerableDataSource<Point> StaticChannelA
        {
            get
            {
                return _StaticChannelA;
            }
        }
        public EnumerableDataSource<Point> StaticChannelB
        {
            get
            {
                return _StaticChannelB;
            }
        }
        public EnumerableDataSource<Point> StaticChannelC
        {
            get
            {
                return _StaticChannelC;
            }
        }
        public EnumerableDataSource<Point> StaticChannelD
        {
            get
            {
                return _StaticChannelD;
            }
        }
        private string _StaticScanProcess()
        {
            uint mOffsetX = 0;
            int iNum = 0;
            int bufferOffset = 0;
            int counts = 0;
            double x = 0;

            Point staticChannelA = new Point();
            Point staticChannelB = new Point();
            Point staticChannelC = new Point();
            Point staticChannelD = new Point();
            if (_ScanParameter.ScanDeltaZ > 0)
            {
                counts = _ScanParameter.ScanDeltaZ / 1000 * 100 / _ScanParameter.DataRate;
            }
            else
            {
                counts = _ScanParameter.LineCounts;
            }
            _StaticArrayChannelA = new Point[counts];
            _StaticArrayChannelB = new Point[counts];
            _StaticArrayChannelC = new Point[counts];
            _StaticArrayChannelD = new Point[counts];

            _StaticChannelA = new EnumerableDataSource<Point>(_StaticArrayChannelA);
            // Set identity mapping of point in collection to point on plot
            _StaticChannelA.SetXYMapping(p => p);

            _StaticChannelB = new EnumerableDataSource<Point>(_StaticArrayChannelB);
            // Set identity mapping of point in collection to point on plot
            _StaticChannelB.SetXYMapping(p => p);

            _StaticChannelC = new EnumerableDataSource<Point>(_StaticArrayChannelC);
            // Set identity mapping of point in collection to point on plot
            _StaticChannelC.SetXYMapping(p => p);

            _StaticChannelD = new EnumerableDataSource<Point>(_StaticArrayChannelD);
            // Set identity mapping of point in collection to point on plot
            _StaticChannelD.SetXYMapping(p => p);
            if (_ScanParameter.ScanDeltaZ > 0)
            {
                _StaticScanPreparingWork();
            }
            else
            {
                _ActiveAPDTransfer.APDLaserStartScan();
            }

            _ActiveAPDTransfer.IsParametersData = false;//turn APD receive mode to data mode

            try
            {
                while (mOffsetX <= (counts - 1) && _ImageIsCapture == true)
                {
                    if (_ActiveAPDTransfer.IsReceived)
                    {
                        iNum = _ActiveAPDTransfer.BufSz / 16;
                        for (int i = 0; i < iNum; ++i)
                        {
                            bufferOffset = i * 16;

                            if (_ActiveAPDTransfer.ReceiveBuffer[0 + bufferOffset] == 0x5B)
                            {
                                if (++mOffsetX > (counts - 1))
                                    break;
                                x = mOffsetX;
                                staticChannelA.Y = (int)BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 6 + bufferOffset);
                                staticChannelA.X = x;

                                staticChannelB.Y = (int)BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 8 + bufferOffset);
                                staticChannelB.X = x;

                                staticChannelC.Y = (int)BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 10 + bufferOffset);
                                staticChannelC.X = x;

                                staticChannelD.Y = (int)BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 12 + bufferOffset);
                                staticChannelD.X = x;
                                _StaticArrayChannelA[mOffsetX] = staticChannelA;
                                _StaticArrayChannelB[mOffsetX] = staticChannelB;
                                _StaticArrayChannelC[mOffsetX] = staticChannelC;
                                _StaticArrayChannelD[mOffsetX] = staticChannelD;
                            }
                        }
                    }
                }
            }
            catch (ThreadAbortException)
            {
                //Ignore ThreadAbortException
            }
            catch (Exception ex)
            {
                throw ex;
            }
            try
            {
                string[] testdata = new string[counts];
                for (int y = 0; y < counts - 1; y++)
                {
                    testdata[y] = _StaticArrayChannelA[y].Y.ToString() + " "
                        + _StaticArrayChannelB[y].Y.ToString() + " " +
                        _StaticArrayChannelC[y].Y.ToString() + " " +
                        _StaticArrayChannelD[y].Y.ToString();
                }
                File.WriteAllLines(@"static.txt", testdata, System.Text.Encoding.UTF8);

            }
            catch (System.Exception ex)
            {
                throw ex;
            }

            _ActiveAPDTransfer.IsParametersData = true;//turn APD receive mode to parameters mode
            _ActiveAPDTransfer.APDLaserStopScan();
            _ScanType = "Static";

            if (!_ImageIsCapture)
            {
                _IsScanAborted = true;  // Scan command aborted
            }

            return "Static";
        }

        private bool _StaticScanPreparingWork()
        {
            //first,make the motor goes to start position
            _ActiveGalil.SendCommand("HX");
            _ActiveGalil.SendCommand("ST");
            #region motor program one
            string temp = "#SCAN\r" +
                               "SHXY\r" +
                               "STXYZ\r" +
                               "SPX=10000\r" +
                               "SPY=5000\r" +
                               "SPZ=1000\r" +
                               "PAB=" + (_ScanParameter.ScanY0).ToString() + "\r" +
                               "BGY\r" +
                               "PAA=" + (_ScanParameter.ScanX0).ToString() + "\r" +
                               "BGX\r" +
                               "PAC=0\r" +
                               "BGZ\r" +
                               "AMX\r" +
                               "AMY\r" +
                               "AMZ\r" +
                               "EN";
            #endregion motor program one
            _ActiveGalil.ProgramDownload(temp);
            //waitting for the motor goes to the position
            while (!(_ActiveGalil.XCurrentP == _ScanParameter.ScanX0 &&
                    _ActiveGalil.YCurrentP == _ScanParameter.ScanY0 &&
                    _ActiveGalil.ZCurrentP == 0))
            {
                // See: link why Sleep(1) is better than Sleep(0)
                // http://joeduffyblog.com/2006/08/22/priorityinduced-starvation-why-sleep1-is-better-than-sleep0-and-the-windows-balance-set-manager/
                System.Threading.Thread.Sleep(1);
            }
            //when the motor is ready,switch APD on
            try
            {
                #region motor program two
                temp = "#SCAN\r" +
                        "SHZ\r" +
                        "STZ\r" +
                        "PAC=" + (_ScanParameter.ScanDeltaZ).ToString() + "\r" +
                        "BGZ\r" +
                        "AMZ\r" +
                        "EN";
                #endregion motor program two
                _ActiveGalil.ProgramDownload(temp);
                _ActiveAPDTransfer.APDLaserStartScan();

            }
            catch
            {
            }

            return true;
        }
        #endregion Static Scan

        #region Sequential Scan
        private bool _SequentialScanPreparingWork(LaserType channel)
        {
            return _DynamicScanPreparingWork();
        }
        private string _SequentialScanProcess()
        {
            _ImageInfo.CaptureType = "Fluorescence";
            _ImageInfo.IsScannedImage = true;
            _ImageInfo.ScanResolution = _ScanParameter.Res;
            _ImageInfo.ScanQuality = _ScanParameter.Quality;
            _ImageInfo.ScanX0 = _ScanParameter.ScanX0;
            _ImageInfo.ScanY0 = _ScanParameter.ScanY0;
            _ImageInfo.DeltaX = (int)((double)_ScanParameter.ScanDeltaX / (double)_ScanParameter.XMotorSubdivision);
            _ImageInfo.DeltaY = (int)((double)_ScanParameter.ScanDeltaY / (double)_ScanParameter.YMotorSubdivision);
            _ImageInfo.ScanZ0 = (double)_ScanParameter.ScanZ0 / (double)_ScanParameter.ZMotorSubdivision;

            List<Signal> signals = new List<Signal>();
            foreach (Signal signal in _Signals)
            {
                signals.Add(signal);
            }

            foreach (Signal signal in signals)
            {
                CommandStatus?.Invoke(this, "Preparing to scan....");
                _Signals.Clear();
                _Signals.Add(signal);

                _IsChannelASelected = signal.LaserType == LaserType.LaserA;
                _IsChannelBSelected = signal.LaserType == LaserType.LaserB;
                _IsChannelCSelected = signal.LaserType == LaserType.LaserC;
                _IsChannelDSelected = signal.LaserType == LaserType.LaserD;

                _DynamicScanProcess();

                DateTime dateTime = DateTime.Now;
                _ImageInfo.DateTime = System.String.Format("{0:G}", dateTime.ToString());

                if (_IsChannelASelected)
                {
                    if (APDChannelA != null)
                    {
                        _ChannelAImage = ImageProcessing.FrameToBitmap(APDChannelA, _ImageWidth, _ImageHeight);
                        _APDChannelA = null;

                        if (_ScanParameter.IsChannelALightShadeFix && !_ScanParameter.IsUnidirectionalScan)
                        {
                            _LightShadeFix(_ChannelAImage);
                        }
                    }
                }
                if (_IsChannelBSelected)
                {
                    if (APDChannelB != null)
                    {
                        _ChannelBImage = ImageProcessing.FrameToBitmap(_APDChannelB, _ImageWidth, _ImageHeight);
                        _APDChannelB = null;
                        if (_ScanParameter.IsChannelBLightShadeFix && !_ScanParameter.IsUnidirectionalScan)
                        {
                            _LightShadeFix(_ChannelBImage);
                        }
                    }
                }
                if (_IsChannelCSelected)
                {
                    if (APDChannelC != null)
                    {
                        _ChannelCImage = ImageProcessing.FrameToBitmap(_APDChannelC, _ImageWidth, _ImageHeight);
                        _APDChannelC = null;
                        if (_ScanParameter.IsChannelCLightShadeFix && !_ScanParameter.IsUnidirectionalScan)
                        {
                            _LightShadeFix(_ChannelCImage);
                        }
                    }
                }
                if (_IsChannelDSelected)
                {
                    if (APDChannelD != null)
                    {
                        _ChannelDImage = ImageProcessing.FrameToBitmap(_APDChannelD, _ImageWidth, _ImageHeight);
                        _APDChannelD = null;
                        if (_ScanParameter.IsChannelDLightShadeFix && !_ScanParameter.IsUnidirectionalScan)
                        {
                            _LightShadeFix(_ChannelDImage);
                        }
                    }
                }
                SequentialChannelCompleted?.Invoke(this, signal.LaserType);
            }
            return "Dynamic";
        }
        #endregion Sequential Scan

        public override void ThreadFunction()
        {
            //if (CommandStatus != null)
            //{
            //    CommandStatus(this, "Scanning in progress....");
            //}

            foreach (var signal in _Signals)
            {
                if (signal.LaserType == LaserType.LaserA)
                {
                    _IsChannelASelected = true;
                }
                else if (signal.LaserType == LaserType.LaserB)
                {
                    _IsChannelBSelected = true;
                }
                else if (signal.LaserType == LaserType.LaserC)
                {
                    //
                    //Phosphor scanning turns on the red laser (LaserC or ChannelC), but read the data from ChannelD.
                    //
                    if (_IsPhosphorImaging)
                    {
                        _IsChannelCSelected = false;
                    }
                    else
                    {
                        _IsChannelCSelected = true;
                    }
                }
                else if (signal.LaserType == LaserType.LaserD)
                {
                    _IsChannelDSelected = true;
                }
            }

            try
            {
                if (_ScanParameter.ScanDeltaX == 0 && _ScanParameter.ScanDeltaY == 0)
                {
                    _ScanType = _StaticScanProcess();
                }
                else
                {
                    if (_ScanParameter.IsSequentialScanningOn)
                    {
                        _ScanType = _SequentialScanProcess();
                    }
                    else
                    {
                        _ScanType = _DynamicScanProcess();
                    }
                }
            }
            catch (System.Threading.ThreadAbortException)
            {
                // don't throw exception if the user abort the process.
            }
            catch (System.Runtime.InteropServices.SEHException)
            {
                // The SEHException class handles SEH errors that are thrown from unmanaged code,
                // but have not been mapped to another .NET Framework exception.

                throw new OutOfMemoryException();
            }
            catch (System.Runtime.InteropServices.COMException cex)
            {
                if (cex.ErrorCode == unchecked((int)0x88980003))
                {
                    throw new OutOfMemoryException();
                }
                else
                {
                    throw cex;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                // Forces an immediate garbage collection.
                //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                //GC.WaitForPendingFinalizers();
                //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            }

            DateTime dateTime = DateTime.Now;
            _ImageInfo.DateTime = System.String.Format("{0:G}", dateTime.ToString());
            if (_IsPhosphorImaging)
                _ImageInfo.CaptureType = "Phosphor";
            else
                _ImageInfo.CaptureType = "Fluorescence";
            _ImageInfo.IsScannedImage = true;
            _ImageInfo.MixChannel.IsAutoChecked = true;
            _ImageInfo.ScanResolution = _ScanParameter.Res;
            _ImageInfo.ScanQuality = _ScanParameter.Quality;
            _ImageInfo.ScanX0 = _ScanParameter.ScanX0;
            _ImageInfo.ScanY0 = _ScanParameter.ScanY0;
            _ImageInfo.DeltaX = (int)((double)_ScanParameter.ScanDeltaX / (double)_ScanParameter.XMotorSubdivision);
            _ImageInfo.DeltaY = (int)((double)_ScanParameter.ScanDeltaY / (double)_ScanParameter.YMotorSubdivision);
            _ImageInfo.ScanZ0 = (double)_ScanParameter.ScanZ0 / (double)_ScanParameter.ZMotorSubdivision;

            if (CommandStatus != null)
            {
                CommandStatus(this, string.Empty);
            }
        }

        public override void Finish()
        {
            if (_ActiveAPDTransfer.APDTransferIsAlive)
            {
                _ActiveAPDTransfer.APDLaserStopScan();
                _ActiveGalil.SendCommand("HX");
                _ActiveGalil.SendCommand("ST");
                _ImageIsCapture = false;
            }

            if (!_IsScanAborted || (_ScanParameter.IsSequentialScanningOn && _IsScanAborted && _IsSaveScanDataOnAborted))
            {
                if (_ScanType == "Dynamic")
                {
                    if (!_ScanParameter.IsSequentialScanningOn || _IsSaveScanDataOnAborted)
                    {
                        // Using static method to convert ushort[] buffer to WriteableBitmap to try to avoid threading issue.
                        if (_IsChannelASelected)
                        {
                            if (APDChannelA != null)
                            {
                                _ChannelAImage = ImageProcessing.FrameToBitmap(APDChannelA, _ImageWidth, _ImageHeight);
                                _APDChannelA = null;
                                if (_ScanParameter.IsChannelALightShadeFix && !_ScanParameter.IsUnidirectionalScan)
                                {
                                    _LightShadeFix(_ChannelAImage);
                                }
                            }
                        }
                        if (_IsChannelBSelected)
                        {
                            if (_APDChannelB != null)
                            {
                                _ChannelBImage = ImageProcessing.FrameToBitmap(_APDChannelB, _ImageWidth, _ImageHeight);
                                _APDChannelB = null;
                                if (_ScanParameter.IsChannelBLightShadeFix && !_ScanParameter.IsUnidirectionalScan)
                                {
                                    _LightShadeFix(_ChannelBImage);
                                }
                            }
                        }
                        if (_IsChannelCSelected)
                        {
                            if (_APDChannelC != null)
                            {
                                _ChannelCImage = ImageProcessing.FrameToBitmap(_APDChannelC, _ImageWidth, _ImageHeight);
                                _APDChannelC = null;
                                if (_ScanParameter.IsChannelCLightShadeFix && !_ScanParameter.IsUnidirectionalScan)
                                {
                                    _LightShadeFix(_ChannelCImage);
                                }
                            }
                        }
                        if (_IsChannelDSelected)
                        {
                            if (_APDChannelD != null)
                            {
                                _ChannelDImage = ImageProcessing.FrameToBitmap(_APDChannelD, _ImageWidth, _ImageHeight);

                                _APDChannelD = null;
                                if (_ScanParameter.IsChannelDLightShadeFix && !_ScanParameter.IsUnidirectionalScan)
                                {
                                    _LightShadeFix(_ChannelDImage);
                                }
                            }
                        }
                        // User stop the sequential scan before completion, notify subscriber.
                        if (_ScanParameter.IsSequentialScanningOn &&_IsSaveScanDataOnAborted)
                        {
                            foreach (Signal signal in _Signals)
                            {
                                SequentialChannelCompleted?.Invoke(this, signal.LaserType);
                            }
                        }
                    }
                    //Crop images for Aligning
                    WriteableBitmap[] adjustiveImage = new WriteableBitmap[3];
                    adjustiveImage[0] = _ChannelAImage;
                    adjustiveImage[1] = _ChannelBImage;
                    adjustiveImage[2] = _ChannelDImage;

                    int[] deltaX = new int[3];
                    deltaX[0] = _ActiveAPDTransfer.DeviceProperties.Pixel_10_ChannelA_DX / (_ScanParameter.Res / 10);
                    deltaX[1] = _ActiveAPDTransfer.DeviceProperties.Pixel_10_ChannelB_DX / (_ScanParameter.Res / 10);
                    deltaX[2] = _ActiveAPDTransfer.DeviceProperties.Pixel_10_ChannelD_DX / (_ScanParameter.Res / 10);
                    int[] deltaY = new int[3];
                    deltaY[0] = _ActiveAPDTransfer.DeviceProperties.Pixel_10_ChannelA_DY / (_ScanParameter.Res / 10);
                    deltaY[1] = _ActiveAPDTransfer.DeviceProperties.Pixel_10_ChannelB_DY / (_ScanParameter.Res / 10);
                    deltaY[2] = _ActiveAPDTransfer.DeviceProperties.Pixel_10_ChannelD_DY / (_ScanParameter.Res / 10);
                    _PositionAdjustment(_ImageWidth, _ImageHeight, _ChannelCImage, adjustiveImage, deltaX, deltaY);
                }
            }

            if (CommandStatus != null)
            {
                CommandStatus(this, string.Empty);
            }

            // Forces an immediate garbage collection.
            //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
            //GC.WaitForPendingFinalizers();
            //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
        }

        public override void AbortWork()
        {
            if (_ActiveAPDTransfer.APDTransferIsAlive)
            {
                _ActiveAPDTransfer.APDLaserStopScan();
                _ActiveGalil.SendCommand("HX");
                _ActiveGalil.SendCommand("ST");
                _ImageIsCapture = false;

                // Wait for the scanning thread to terminate
                int count = 0;
                while (!_IsScanAborted && count < 1000)
                {
                    // See: link why Sleep(1) is better than Sleep(0)
                    // http://joeduffyblog.com/2006/08/22/priorityinduced-starvation-why-sleep1-is-better-than-sleep0-and-the-windows-balance-set-manager/
                    System.Threading.Thread.Sleep(1);
                    count++;
                }
            }
            else
            {
                _IsScanAborted = true;
            }
        }

        public unsafe void FrameToBitmap(out WriteableBitmap lastBMP, ushort[] dataToConvert, Int32 width, Int32 height)
        {
            lastBMP = null;
            lastBMP = new WriteableBitmap(width, height, 300, 300, PixelFormats.Gray16, null);
            int iStride = (width * 16 + 7) / 8;
            lastBMP.WritePixels(new Int32Rect(0, 0, width, height), dataToConvert, iStride, 0);
        }

        /// <summary>
        /// Simulation Mode: Simulate image scanning.
        /// </summary>
        protected override void SimulateThreadFunction()
        {
            // if anyone has subscribed, notify them
            if (CommandStatus != null)
            {
                CommandStatus(this, "Preparing....");
            }

            // Load sample simulation images.
            if (!System.IO.File.Exists(@"Simulation\490_Blue_450nm.tif") ||
                !System.IO.File.Exists(@"Simulation\550_Green_520nm.tif") ||
                !System.IO.File.Exists(@"Simulation\700_IR700_650nm.tif") ||
                !System.IO.File.Exists(@"Simulation\800_IR800_750nm.tif"))
            {
                throw new Exception("Could not find simulation image file.");
            }

            WriteableBitmap srcChannelD = LoadImage(@"Simulation\490_Blue_450nm.tif");
            WriteableBitmap srcChannelB = LoadImage(@"Simulation\550_Green_520nm.tif");
            WriteableBitmap srcChannelC = LoadImage(@"Simulation\700_IR700_650nm.tif");
            WriteableBitmap srcChannelA = LoadImage(@"Simulation\800_IR800_750nm.tif");

            _ImageWidth = srcChannelA.PixelWidth;
            _ImageHeight = srcChannelA.PixelHeight;
            int bufferWidth = srcChannelA.BackBufferStride;

            WriteableBitmap tempChanAImage = new WriteableBitmap(_ImageWidth, _ImageHeight, 300, 300, PixelFormats.Gray16, null);
            WriteableBitmap tempChanBImage = new WriteableBitmap(_ImageWidth, _ImageHeight, 300, 300, PixelFormats.Gray16, null);
            WriteableBitmap tempChanCImage = new WriteableBitmap(_ImageWidth, _ImageHeight, 300, 300, PixelFormats.Gray16, null);
            WriteableBitmap tempChanDImage = new WriteableBitmap(_ImageWidth, _ImageHeight, 300, 300, PixelFormats.Gray16, null);
            //_ImageInfo = new ImageInfo();
            _ScanType = "Dynamic";

            // if anyone has subscribed, notify them
            if (CommandStatus != null)
            {
                CommandStatus(this, "Calculating....");
            }

            System.Diagnostics.Stopwatch stopWatch = new System.Diagnostics.Stopwatch();
            DateTime dateTime = new DateTime();
            double elapsedTime = 0.0;
            double estTimeInSeconds = 0.0;
            double timePerLine = 0.0;

            // Reset IsPreparing flag
            // if anyone has subscribed, notify them
            if (ReceiveTransfer != null)
            {
                ReceiveTransfer(this, "ScannerIsReady");
            }

            unsafe
            {
                try
                {
                    // Get and set the scan line
                    byte* pSrcBufferA = (byte*)srcChannelA.BackBuffer.ToPointer();
                    byte* pSrcBufferB = (byte*)srcChannelB.BackBuffer.ToPointer();
                    byte* pSrcBufferC = (byte*)srcChannelC.BackBuffer.ToPointer();
                    byte* pSrcBufferD = (byte*)srcChannelD.BackBuffer.ToPointer();
                    byte* pDstBufferA = (byte*)tempChanAImage.BackBuffer.ToPointer();
                    byte* pDstBufferB = (byte*)tempChanBImage.BackBuffer.ToPointer();
                    byte* pDstBufferC = (byte*)tempChanCImage.BackBuffer.ToPointer();
                    byte* pDstBufferD = (byte*)tempChanDImage.BackBuffer.ToPointer();

                    for (int i = 0; i < _ImageHeight && !_IsScanAborted; i++)
                    {
                        // Use for calculating total time to completion
                        //stopWatch.Reset();
                        if (i == 0)
                        {
                            stopWatch.Start();
                        }

                        ushort* pSrcA = (ushort*)(pSrcBufferA + (i * bufferWidth));
                        ushort* pSrcB = (ushort*)(pSrcBufferB + (i * bufferWidth));
                        ushort* pSrcC = (ushort*)(pSrcBufferC + (i * bufferWidth));
                        ushort* pSrcD = (ushort*)(pSrcBufferD + (i * bufferWidth));

                        ushort* pDstA = (ushort*)(pDstBufferA + (i * bufferWidth));
                        ushort* pDstB = (ushort*)(pDstBufferB + (i * bufferWidth));
                        ushort* pDstC = (ushort*)(pDstBufferC + (i * bufferWidth));
                        ushort* pDstD = (ushort*)(pDstBufferD + (i * bufferWidth));

                        for (int j = 0; j < _ImageWidth && !_IsScanAborted; j++)
                        {
                            *pDstA++ = *pSrcA++;
                            *pDstB++ = *pSrcB++;
                            *pDstC++ = *pSrcC++;
                            *pDstD++ = *pSrcD++;
                        }

                        _ChannelAImage = tempChanAImage.Clone();
                        _ChannelBImage = tempChanBImage.Clone();
                        _ChannelCImage = tempChanCImage.Clone();
                        _ChannelDImage = tempChanDImage.Clone();
                        if (_ChannelAImage.CanFreeze) { _ChannelAImage.Freeze(); }
                        if (_ChannelBImage.CanFreeze) { _ChannelBImage.Freeze(); }
                        if (_ChannelCImage.CanFreeze) { _ChannelCImage.Freeze(); }
                        if (_ChannelDImage.CanFreeze) { _ChannelDImage.Freeze(); }

                        // if anyone has subscribed, notify them
                        if (ReceiveTransfer != null && !_IsScanAborted)
                        {
                            ReceiveTransfer(this, "Dynamic");
                        }

                        // Calculating estimate total to completion
                        if (i == 0)
                        {
                            stopWatch.Stop();
                            elapsedTime = stopWatch.ElapsedMilliseconds;
                            stopWatch.Reset();
                            timePerLine = elapsedTime / 1000.0;
                            estTimeInSeconds = (double)_ImageHeight * timePerLine;
                            _RemainingTime = (int)estTimeInSeconds;
                            dateTime = DateTime.Now;
                            if (CompletionEstimate != null)
                            {
                                double percentCompleted = 100.0 * ((double)(i + 1) / (double)_ImageHeight);
                                CompletionEstimate(this, dateTime, estTimeInSeconds, percentCompleted);
                            }
                        }

                        if (_RemainingTime > (int)(estTimeInSeconds - estTimeInSeconds * (double)i / (double)_ImageHeight))
                        {
                            _RemainingTime = (int)(estTimeInSeconds - estTimeInSeconds * (double)i / (double)_ImageHeight);
                            // if anyone has subscribed, notify them
                            if (ReceiveTransfer != null)
                            {
                                ReceiveTransfer(this, "RemainingTime");
                            }
                        }
                    }
                }
                catch
                {
                }
            }

            _ChannelAImage = srcChannelA.Clone();
            _ChannelBImage = srcChannelB.Clone();
            _ChannelCImage = srcChannelC.Clone();
            _ChannelDImage = srcChannelD.Clone();
            if (_ChannelAImage.CanFreeze) { _ChannelAImage.Freeze(); }
            if (_ChannelBImage.CanFreeze) { _ChannelBImage.Freeze(); }
            if (_ChannelCImage.CanFreeze) { _ChannelCImage.Freeze(); }
            if (_ChannelDImage.CanFreeze) { _ChannelDImage.Freeze(); }

            // if anyone has subscribed, notify them
            if (ReceiveTransfer != null && !_IsScanAborted)
            {
                ReceiveTransfer(this, "Dynamic");
            }
        }

        internal WriteableBitmap LoadImage(string filePath)
        {
            WriteableBitmap wbBitmap = null;
            ImageInfo imageInfo = null;

            try
            {
                wbBitmap = ImageProcessing.Load(filePath);

                if (wbBitmap != null)
                {
                    try
                    {
                        // Get image info from the comments section of the image metadata
                        imageInfo = ImageProcessing.ReadMetadata(filePath);
                    }
                    catch
                    {
                    }

                    if (imageInfo == null)
                    {
                        imageInfo = new ImageInfo();
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return wbBitmap;
        }

    }
}
