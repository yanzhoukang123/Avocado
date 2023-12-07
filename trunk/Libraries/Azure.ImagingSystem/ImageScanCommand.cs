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
//using Azure.GalilMotor;


namespace Azure.ImagingSystem
{
    public class ImageScanCommand : ThreadBase
    {
        // Image scanning status delegate
        public delegate void CommandStatusHandler(object sender, string status);
        // Image scanning status event
        public event CommandStatusHandler CommandStatus;
        public delegate void CommandCompletionEstHandler(ThreadBase sender, DateTime dateTime, double estTime);
        // Image capture completion time estimate event
        //public event CommandCompletionEstHandler CompletionEstimate;
        // update event while APD receives data
        public delegate void ScanReceiveDataHandler(ThreadBase sender, string scanType);
        public event ScanReceiveDataHandler ReceiveTransfer;

        private APDTransfer _ActiveAPDTransfer = null;
        private Dispatcher _CallingDispatcher = null;
        private ImageInfo _ImageInfo = null;
        private int _ImageWidth, _ImageHeight;
        private string _ScanType = null;
        private ScanParameterStruct _ScanParameter = null;
        private int _RemainingTime = 0;
        private bool _ImageIsCapture = true;
        public bool _IsScanAborted = false;
        private bool _ScannerIsReady = false;



        public class ScanParameterStruct
        {
            public int Width { get; set; }
            public int Height { get; set; }
            public int XMotorSpeed { get; set; }
            public int YMotorSpeed { get; set; }
            public int ZMotorSpeed { get; set; }
            public int ScanX0 { get; set; }
            public int ScanY0 { get; set; }
            public int ScanZ0 { get; set; }
            public int ScanDeltaX { get; set; }
            public int ScanDeltaY { get; set; }
            public int ScanDeltaZ { get; set; }
            public int Res { get; set; }
            public int Quality { get; set; }
            public int DataRate { get; set; }
            public int LineCounts { get; set; }
            public int Time { get; set; }
            public double XMotorSubdivision { get; set; }
            public double YMotorSubdivision { get; set; }
            public double ZMotorSubdivision { get; set; }
            public double XEncoderSubdivision { get; set; }
            /// <summary>
            /// new firmware is version after 2.0.1.1 (included 2.0.1.1);
            /// new firmware takes control of X,Y,Z motion;
            /// </summary>
            public bool IsNewFirmwire { get; set; }
            /// <summary>
            /// Unit of msec
            /// 
            /// </summary>
            public int XmotionTurnAroundDelay { get; set; }
            public int XMotionAccVal { get; set; }
            public int YMotionAccVal { get; set; }
            public int ZMotionAccVal { get; set; }
            public int XMotionDccVal { get; set; }
            public int YMotionDccVal { get; set; }
            public int ZMotionDccVal { get; set; }
            public int DynamicBits { get; set; }
        }

        public ImageScanCommand(Dispatcher callingDispatcher, APDTransfer aPDTransfer, ScanParameterStruct scanParameter)
        {
            _ImageIsCapture = true;
            _CallingDispatcher = callingDispatcher;
            _ActiveAPDTransfer = aPDTransfer;
            _ScanParameter = scanParameter;
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
        #region Dynamic Scan
        private ushort[] _APDChannelA;
        private ushort[] _APDChannelB;
        private ushort[] _APDChannelC;
        private ushort[] _APDChannelD;
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
        private string _DynamicScanProcess()
        {
            int tstststst = 0;

            long imageDataSize = (long)_ScanParameter.Width * (long)_ScanParameter.Height * 16L+512L*32L;
            int motorMode = 0;
            _ImageWidth = _ScanParameter.Width;
            _RemainingTime = _ScanParameter.Time;

            if (_ScanParameter.ScanDeltaY == 0 && _ScanParameter.ScanDeltaZ == 0)
            {
                motorMode = 1;
                _ImageHeight = 2000;
            }
            else if (_ScanParameter.ScanDeltaY == 0 && _ScanParameter.ScanDeltaZ > 0)
            {
                motorMode = 2;
                _ImageHeight = _ScanParameter.ScanDeltaZ * 1000 / _ScanParameter.Res;

            }
            else if (_ScanParameter.ScanDeltaY > 0 && _ScanParameter.ScanDeltaZ == 0)
            { 
                motorMode = 0;
                _ImageHeight = (int)(_ScanParameter.ScanDeltaY / _ScanParameter.YMotorSubdivision * 1000 / _ScanParameter.Res);
            }
            else
            {
                return "Dynamic Error";
            }
            //
            //MemoryMappedFile imageData = MemoryMappedFile.CreateFromFile(@"testmap", System.IO.FileMode.Create, null, imageDataSize);

            _DynamicScanPreparingWork(motorMode);

            int channelDataLength = _ImageWidth * _ImageHeight;
            _APDChannelA = new ushort[channelDataLength];
            _APDChannelB = new ushort[channelDataLength];
            _APDChannelC = new ushort[channelDataLength];
            _APDChannelD = new ushort[channelDataLength];

            int channelDataPrepareViewLength=(_ImageWidth / 10) * (_ImageHeight / 10);
            ushort[] aPDChannelAPrepareView = new ushort[channelDataPrepareViewLength];
            ushort[] aPDChannelBPrepareView = new ushort[channelDataPrepareViewLength];
            ushort[] aPDChannelCPrepareView = new ushort[channelDataPrepareViewLength];
            ushort[] aPDChannelDPrepareView = new ushort[channelDataPrepareViewLength];

            uint imageOffsetX = 0;
            uint imageOffsetY = 0;
            int iNum = 0;
            double yNum = 0;
            int dataIsGet = 0;
            int bufferOffset = 0;
            int channelDataAddress = 0;
            int channelPreviewDataOffsetAddress = 0;

            try
            {
                while (imageOffsetY < _ImageHeight && _ImageIsCapture == true)
                {
                    if (_ActiveAPDTransfer.IsReceived)
                    {
                        //using( MemoryMappedViewAccessor imageDataViewAccessor = imageData.CreateViewAccessor())
                        //{
                        //    imageDataViewAccessor.WriteArray<byte>(dataIsGet * _ActiveAPDTransfer.ReceiveBuffer.Length, _ActiveAPDTransfer.ReceiveBuffer, 0, _ActiveAPDTransfer.ReceiveBuffer.Length);
                        //    imageDataViewAccessor.Flush();
                        //}
                        dataIsGet++;
                        Console.WriteLine("图像数据取出：" + dataIsGet.ToString());
                        iNum = _ActiveAPDTransfer.BufSz / 16;
                        if (!_ScannerIsReady)
                        {
                            _ScannerIsReady = true;
                            if (ReceiveTransfer != null)
                            {
                                ReceiveTransfer(this, "ScannerIsReady");
                            }
                        }
                        if (ReceiveTransfer != null)
                        {
                            ReceiveTransfer(this, "ScannerIsReady");
                        }
                        for (int i = 0; i < iNum; ++i)
                        {
                            bufferOffset = i * 16;
                            tstststst = i;
                            if (_ActiveAPDTransfer.ReceiveBuffer[0 + bufferOffset] == 0x5B)
                            {
                                imageOffsetX = BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 2 + bufferOffset);
                                imageOffsetY = BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 4 + bufferOffset);

                                if (_ActiveAPDTransfer.ReceiveBuffer[14+bufferOffset] == 0x01)
                                {
                                    _ActiveAPDTransfer.LIDIsOpen = false;
                                    
                                }
                                else
                                {
                                    _ActiveAPDTransfer.LIDIsOpen = true;
                                }
                                imageOffsetX -= 1;
                                imageOffsetY -= 1;
                                if (_ActiveAPDTransfer.ReceiveBuffer[1 + bufferOffset] == 0xD5)
                                {
                                    uint temp = imageOffsetX;
                                    imageOffsetX = (uint)_ImageWidth - imageOffsetX - 1;
                                    if (imageOffsetX > 0xffffff00)
                                    {
                                        UInt16 x = BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 2 + bufferOffset);
                                    }
                                }

                                channelDataAddress = (int)(imageOffsetX + imageOffsetY * _ImageWidth);
                                if (channelDataAddress < channelDataLength)
                                {
                                    _APDChannelA[channelDataAddress] = BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 6 + bufferOffset);

                                    _APDChannelB[channelDataAddress] = BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 8 + bufferOffset);

                                    _APDChannelC[channelDataAddress] = BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 10 + bufferOffset);

                                    _APDChannelD[channelDataAddress] = BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 12 + bufferOffset);

                                    if (imageOffsetY % 10 == 0)
                                    {
                                        if (imageOffsetX % 10 == 0)
                                        {
                                            channelPreviewDataOffsetAddress = (int)(imageOffsetX / 10 + (imageOffsetY / 10) * (_ImageWidth / 10));
                                            if (channelPreviewDataOffsetAddress < channelDataPrepareViewLength)
                                            {
                                                aPDChannelAPrepareView[channelPreviewDataOffsetAddress] = BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 6 + bufferOffset);

                                                aPDChannelBPrepareView[channelPreviewDataOffsetAddress] = BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 8 + bufferOffset);

                                                aPDChannelCPrepareView[channelPreviewDataOffsetAddress] = BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 10 + bufferOffset);

                                                aPDChannelDPrepareView[channelPreviewDataOffsetAddress] = BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 12 + bufferOffset);
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                Console.WriteLine("数据分析出错！");
                            }
                        }
                        //Console.WriteLine("余数：" + (imageOffsetY % ((uint)_ImageHeight / 5)).ToString());
                        if (yNum < ((double)imageOffsetY / ((double)_ImageHeight / 20)))
                        {
                            yNum = (double)imageOffsetY / ((double)_ImageHeight / 20);
                            Console.WriteLine("dssfdsfdsfdsfdsfdsfdsfdsf" + yNum.ToString());
                            FrameToBitmap(out _ChannelAImage, aPDChannelAPrepareView, _ImageWidth / 10, _ImageHeight / 10);
                            FrameToBitmap(out _ChannelBImage, aPDChannelBPrepareView, _ImageWidth / 10, _ImageHeight / 10);
                            FrameToBitmap(out _ChannelCImage, aPDChannelCPrepareView, _ImageWidth / 10, _ImageHeight / 10);
                            FrameToBitmap(out _ChannelDImage, aPDChannelDPrepareView, _ImageWidth / 10, _ImageHeight / 10);
                            if (ReceiveTransfer != null)
                            {
                                ReceiveTransfer(this, "Dynamic");
                            }
                        }

                        if (_RemainingTime > (int)(_ScanParameter.Time - _ScanParameter.Time * imageOffsetY / _ImageHeight))
                        {
                            _RemainingTime = (int)(_ScanParameter.Time - _ScanParameter.Time * imageOffsetY / _ImageHeight);
                            if (ReceiveTransfer != null)
                            {
                                ReceiveTransfer(this, "RemainingTime");
                                ReceiveTransfer(this, "LIDStatus");
                            }
                        }
                    }
                    Thread.Sleep(1);
                }
                //imageData.Dispose();
            }
            catch (Exception e)
            {
                Xceed.Wpf.Toolkit.MessageBox.Show(e.Message, "Scanner error!");
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
        private bool _DynamicScanPreparingWork(int motorMode)
        {
            //first,make the motor goes to start position
            if (MoveToStartLocation() == false)
            {
                return false;
            }
            System.Threading.Thread.Sleep(1000);
            //int xMotorSpeed = XMotorSpeedCalibration.GetSpeed(_ScanParameter.ScanDeltaX / 100 + 30, _ScanParameter.Quality) / 10;
            int singleTrip = _ScanParameter.ScanDeltaX + (int)(10 * _ScanParameter.XMotorSubdivision);
            double singleTripTime = _ScanParameter.Quality / 2.0 - _ScanParameter.XmotionTurnAroundDelay / 1000.0;
            int xMotorSpeed = XMotorSpeedCalibration.GetSpeed(_ScanParameter.XMotionAccVal, 256, singleTrip, singleTripTime);
            //when the motor is ready,switch APD on
            if (_ScanParameter.IsNewFirmwire)
            {
                DynamicScanMotionConfig(motorMode, xMotorSpeed);
                _ActiveAPDTransfer.IsCommBusy = true;
                Thread.Sleep(100);      // wait for motion status query timer stop querying
            }
            _ActiveAPDTransfer.APDLaserStartScan();
            _ActiveAPDTransfer.IsParametersData = false;
            if (_ScanParameter.IsNewFirmwire)
            {
                Thread.Sleep(10);
                _ActiveAPDTransfer.MotionControl.StartMotion(MotionTypes.Y, true);      // start Y motion at first
                _ActiveAPDTransfer.MotionControl.StartMotion(MotionTypes.X, true);
                return true;
            }
            else
            {
                return DynamicScanMotionConfig(motorMode, xMotorSpeed);
            }
        }

        private bool MoveToStartLocation()
        {
            try
            {
                if (_ScanParameter.IsNewFirmwire)
                {
                    _ActiveAPDTransfer.MotionControl.EnableMotion(MotionTypes.X, true);
                    _ActiveAPDTransfer.MotionControl.StartMotion(MotionTypes.X, false);
                    _ActiveAPDTransfer.MotionControl.EnableMotion(MotionTypes.Y, true);
                    _ActiveAPDTransfer.MotionControl.StartMotion(MotionTypes.Y, false);
                    int startPosX = _ScanParameter.ScanX0 - (int)(1 * _ScanParameter.XMotorSubdivision);
                    _ActiveAPDTransfer.MotionControl.AbsoluteMove(MotionTypes.X, 256, _ScanParameter.XMotorSpeed,
                        _ScanParameter.XMotionAccVal, _ScanParameter.XMotionDccVal, startPosX, false, true);

                    _ActiveAPDTransfer.MotionControl.AbsoluteMove(MotionTypes.Y, 256, _ScanParameter.YMotorSpeed,
                        _ScanParameter.YMotionAccVal, _ScanParameter.YMotionDccVal, _ScanParameter.ScanY0, false, true);

                    while ((_ActiveAPDTransfer.MotionControl.MotionCrntPositions[MotionTypes.X] != startPosX) ||
                        (_ActiveAPDTransfer.MotionControl.MotionCrntPositions[MotionTypes.Y] != _ScanParameter.ScanY0))
                    {
                        Thread.Sleep(1);
                    }
                }
                else
                {
                }
                return true;
            }
            catch
            {
                return false;
            }
        }
        private bool DynamicScanMotionConfig( int motorMode, int xMotorSpeed)
        {
            try
            {
                if (_ScanParameter.IsNewFirmwire)
                {
                    if(motorMode == 0)
                    {
                        int speedY = (int)Math.Round(_ScanParameter.Res * 2 * _ScanParameter.YMotorSubdivision / _ScanParameter.Quality / 1000);
                        int tgtPosY = _ScanParameter.ScanY0 + _ScanParameter.ScanDeltaY + (int)Math.Round(3 * _ScanParameter.YMotorSubdivision);
                        int tgtPosX1 = _ScanParameter.ScanX0 + _ScanParameter.ScanDeltaX + (int)Math.Round(5 * _ScanParameter.XMotorSubdivision);
                        int tgtPosX2 = _ScanParameter.ScanX0 - (int)Math.Round(5 * _ScanParameter.XMotorSubdivision);
                        int repeats = _ScanParameter.Height + 10;
                        _ActiveAPDTransfer.MotionControl.AbsoluteMove(MotionTypes.Y, 256, speedY, _ScanParameter.YMotionAccVal, _ScanParameter.YMotionDccVal, tgtPosY, false, false);
                        _ActiveAPDTransfer.MotionControl.AbsoluteMove(MotionTypes.X, 256, xMotorSpeed, _ScanParameter.XMotionAccVal,
                            _ScanParameter.XMotionDccVal, tgtPosX1, tgtPosX2, repeats, _ScanParameter.XmotionTurnAroundDelay, false, false);
                    }
                }
                else
                {
                    string temp;
                    if (motorMode == 0)
                    {
                        temp = "#SCAN\r" +
                                           "SHXY\r" +
                                           "STXYZ\r" +
                                           "SPY=" + (_ScanParameter.Res * 2 * _ScanParameter.YMotorSubdivision / _ScanParameter.Quality / 1000).ToString() + "\r" +
                                           "SPX=" + ((int)xMotorSpeed).ToString() + "\r" +
                                           "PAB=" + (_ScanParameter.ScanY0 + _ScanParameter.ScanDeltaY + _ScanParameter.YMotorSubdivision * 3).ToString() + "\r" +
                                           "BGY\r" +
                                           "#LOOP\r" +
                                           "PAA=" + (_ScanParameter.ScanX0 + _ScanParameter.ScanDeltaX + 500).ToString() + "\r" +
                                           "BGX\r" +
                                           "AMX\r" +
                                           "WT 150" + "\r" +
                                           "PAA=" + (_ScanParameter.ScanX0 - 500).ToString() + "\r" +
                                           "BGX\r" +
                                           "AMX\r" +
                                           "WT 150" + "\r" +
                                           "IF(_RPB>=" + (_ScanParameter.ScanY0 + _ScanParameter.ScanDeltaY + _ScanParameter.YMotorSubdivision * 3).ToString() + ")" + "\r" +
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
                    }
                    else
                    {
                        #region motor program two
                        temp = "#SCAN\r" +
                                            "n=0\r" +
                                           "SHXY\r" +
                                           "STXYZ\r" +
                                           "SPX=" + (2 * _ScanParameter.ScanDeltaX / _ScanParameter.Quality * 10 / 9).ToString() + "\r" +
                                           "#LOOP\r" +
                                           "PAA=" + (_ScanParameter.ScanX0 + _ScanParameter.ScanDeltaX + 2000).ToString() + "\r" +
                                           "BGX\r" +
                                           "AMX\r" +
                                           "PAA=" + (_ScanParameter.ScanX0 - 2000).ToString() + "\r" +
                                           "BGX\r" +
                                           "AMX\r" +
                                            "n=n+1\r" +
                                            "IF(n=1000)" + "\r" +
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

                    }

                }
                return true;
            }
            catch
            {
                return false;
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

        Point _StaticChannelAMax;
        Point _StaticChannelBMax;
        Point _StaticChannelCMax;
        Point _StaticChannelDMax;

        public Point StaticChannelAMax
        {
            get 
            {
                return _StaticChannelAMax;
            }
        }

        public Point StaticChannelBMax
        {
            get
            {
                return _StaticChannelBMax;
            }
        }

        public Point StaticChannelCMax
        {
            get
            {
                return _StaticChannelCMax;
            }
        }

        public Point StaticChannelDMax
        {
            get
            {
                return _StaticChannelDMax;
            }
        }

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
            uint offsetPointNum = 0;
            int iNum = 0;
            int bufferOffset = 0;
            int counts = 0;
            double x = 0;

            Point staticChannelA = new Point();
            Point staticChannelB = new Point();
            Point staticChannelC = new Point();
            Point staticChannelD = new Point();

            _StaticChannelAMax = new Point(0,0);
            _StaticChannelBMax = new Point(0,0);
            _StaticChannelCMax = new Point(0,0);
            _StaticChannelDMax = new Point(0,0);
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

            double offsetX = 0;
            if(_ScanParameter.ScanDeltaZ == 0)
            {
                offsetX = 1;
            }
            else
            {
                offsetX = (double)_ScanParameter.ScanDeltaZ / (double)_ScanParameter.ZMotorSubdivision / (double)counts;
            }

            try
            {
                while (offsetPointNum <= (counts - 1) && _ImageIsCapture == true)
                {
                    if (_ActiveAPDTransfer.IsReceived)
                    {
                        if (!_ScannerIsReady)
                        {
                            if (ReceiveTransfer != null)
                            {
                                ReceiveTransfer(this, "ScannerIsReady");
                            }
                        }
                        iNum = _ActiveAPDTransfer.BufSz / 16;
                        for (int i = 0; i < iNum; ++i)
                        {
                            bufferOffset = i * 16;

                            if (_ActiveAPDTransfer.ReceiveBuffer[0 + bufferOffset] == 0x5B)
                            {
                                if (++offsetPointNum > (counts - 1))
                                    break;
                                x += offsetX;
                                staticChannelA.Y = (int)BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 6 + bufferOffset);
                                staticChannelA.X = x;

                                staticChannelB.Y = (int)BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 8 + bufferOffset);
                                staticChannelB.X = x;

                                staticChannelC.Y = (int)BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 10 + bufferOffset);
                                staticChannelC.X = x;

                                staticChannelD.Y = (int)BitConverter.ToUInt16(_ActiveAPDTransfer.ReceiveBuffer, 12 + bufferOffset);
                                staticChannelD.X = x;
                                _StaticArrayChannelA[offsetPointNum] = staticChannelA;
                                _StaticArrayChannelB[offsetPointNum] = staticChannelB;
                                _StaticArrayChannelC[offsetPointNum] = staticChannelC;
                                _StaticArrayChannelD[offsetPointNum] = staticChannelD;
                                if (_StaticChannelAMax.Y <= staticChannelA.Y)
                                    _StaticChannelAMax = staticChannelA;
                                if (_StaticChannelBMax.Y <= staticChannelB.Y)
                                    _StaticChannelBMax = staticChannelB;
                                if (_StaticChannelCMax.Y <= staticChannelC.Y)
                                    _StaticChannelCMax = staticChannelC;
                                if (_StaticChannelDMax.Y <= staticChannelD.Y)
                                    _StaticChannelDMax = staticChannelD;
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
            if (_ScanParameter.IsNewFirmwire)
            {
                _ActiveAPDTransfer.MotionControl.StartMotion(MotionTypes.X, false);
                _ActiveAPDTransfer.MotionControl.StartMotion(MotionTypes.Y, false);
                _ActiveAPDTransfer.MotionControl.StartMotion(MotionTypes.Z, false);
            }
            else
            {
            }

            return true;
        }
        #endregion Static Scan
        public override void ThreadFunction()
        {
            if (CommandStatus != null)
            {
                CommandStatus(this, "Scanning in progress....");
            }
            string tResult;
            if (_ScanParameter.ScanDeltaX == 0 && _ScanParameter.ScanDeltaY == 0)
            {
                tResult = _StaticScanProcess();
            }
            else
            {
                tResult = _DynamicScanProcess();
            }

            if (!_IsScanAborted)
            {
                if (tResult == "Dynamic")
                {
                    FrameToBitmap(out _ChannelAImage, _APDChannelA, _ImageWidth, _ImageHeight);
                    _APDChannelA = null;
                    FrameToBitmap(out _ChannelBImage, _APDChannelB, _ImageWidth, _ImageHeight);
                    _APDChannelB = null;
                    FrameToBitmap(out _ChannelCImage, _APDChannelC, _ImageWidth, _ImageHeight);
                    _APDChannelC = null;
                    FrameToBitmap(out _ChannelDImage, _APDChannelD, _ImageWidth, _ImageHeight);
                    _APDChannelD = null;
                    if (_ChannelAImage.CanFreeze) { _ChannelAImage.Freeze(); }
                    if (_ChannelBImage.CanFreeze) { _ChannelBImage.Freeze(); }
                    if (_ChannelCImage.CanFreeze) { _ChannelCImage.Freeze(); }
                    if (_ChannelDImage.CanFreeze) { _ChannelDImage.Freeze(); }
                }
            }
            else
            {
                ExitStat = ThreadExitStat.Abort;
            }

            DateTime dateTime = DateTime.Now;
            _ImageInfo = new Azure.Image.Processing.ImageInfo();
            _ImageInfo.DateTime = System.String.Format("{0:G}", dateTime.ToString());
            _ImageInfo.CaptureType = "Scanner";
            _ImageInfo.IsScannedImage = true;
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
                if (_ScanParameter.IsNewFirmwire)
                {
                    _ActiveAPDTransfer.MotionControl.StartMotion(MotionTypes.X, false);
                    _ActiveAPDTransfer.MotionControl.StartMotion(MotionTypes.Y, false);
                }
                else
                {
                    _ActiveAPDTransfer.APDLaserStopScan();
                }
                _ImageIsCapture = false;
            }

            if (CommandStatus != null)
            {
                CommandStatus(this, string.Empty);
            }
        }

        public override void AbortWork()
        {
            if (_ActiveAPDTransfer.APDTransferIsAlive)
            {
                if (_ScanParameter.IsNewFirmwire)
                {
                    _ActiveAPDTransfer.MotionControl.StartMotion(MotionTypes.X, false);
                    _ActiveAPDTransfer.MotionControl.StartMotion(MotionTypes.Y, false);
                }
                else
                {
                    _ActiveAPDTransfer.APDLaserStopScan();
                }
                _ImageIsCapture = false;
            }

            // Wait for the scanning thread to terminate
            int count = 0;
            while (!_IsScanAborted && count < 20)
            {
                System.Threading.Thread.Sleep(100);
                count++;
            }
        }

        public unsafe void FrameToBitmap(out WriteableBitmap lastBMP, ushort[] dataToConvert, Int32 width, Int32 height)
        {
            lastBMP = null;
            lastBMP = new WriteableBitmap(width, height, 96, 96, PixelFormats.Gray16, null);
            int iStride = (width * 16 + 7) / 8;
            lastBMP.WritePixels(new Int32Rect(0, 0, width, height), dataToConvert, iStride, 0);
        }

    }
}
