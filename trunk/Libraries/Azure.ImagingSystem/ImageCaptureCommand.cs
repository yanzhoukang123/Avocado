using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Threading;     //Dispatcher
using System.Windows.Media.Imaging; //WriteableBitmap
using System.Windows;   //Rect
using Azure.CameraLib;
using Azure.CommandLib;
using Azure.Image.Processing;
using Photometrics;

namespace Azure.ImagingSystem
{
    public class ImageCaptureCommand : ThreadBase
    {
        // Image capture status delegate
        public delegate void CommandStatusHandler(object sender, string status);
        // Image capture status event
        public event CommandStatusHandler CommandStatus;
        // Image capture completion time estimate delegate
        public delegate void CommandCompletionEstHandler(ThreadBase sender, DateTime dateTime, double estTime);
        // Image capture completion time estimate event
        public event CommandCompletionEstHandler CompletionEstimate;

        private Dispatcher _CallingDispatcher = null;
        //private PVCamCamera _ActiveCamera = null;
        //private PhotometricsCamera _ActiveCamera = null;
        private CameraLibBase _ActiveCamera = null;
        private ImageChannelSettings _ImageChannel = null;
        private WriteableBitmap _CapturedImage = null;
        private ImageInfo _ImageInfo = null;
        private Rect _RoiRect;
        private bool _IsCommandAborted = false;

        public ImageCaptureCommand(Dispatcher callingDispatcher,
                                   CameraLibBase camera,
                                   ImageChannelSettings imageChannel,
                                   Rect roiRect)
        {
            _CallingDispatcher = callingDispatcher;
            _ActiveCamera = camera;
            _ImageChannel = imageChannel;
            _RoiRect = roiRect;
        }

        public WriteableBitmap CapturedImage
        {
            get { return _CapturedImage; }
        }

        public ImageInfo ImageInfo
        {
            get { return _ImageInfo; }
        }


        public override void ThreadFunction()
        {
            if (CommandStatus != null)
            {
                CommandStatus(this, "Preparing to capture....");
            }

            #region === Camera setup ===

            //Set binning mode
            _ActiveCamera.HBin = _ImageChannel.BinningMode;
            _ActiveCamera.VBin = _ImageChannel.BinningMode;
            //Set CCD readout speed (0: Normal, 1: Fast)
            _ActiveCamera.ReadoutSpeed = _ImageChannel.ReadoutSpeed;
            //Set gain
            _ActiveCamera.Gain = _ImageChannel.AdGain;
            //Set region of interest
            if (_RoiRect.Width > 0 && _RoiRect.Height > 0)
            {
                _ActiveCamera.RoiStartX = (ushort)_RoiRect.X;
                _ActiveCamera.RoiWidth = (ushort)(_RoiRect.Width + _RoiRect.X);
                _ActiveCamera.RoiStartY = (ushort)_RoiRect.Y;
                _ActiveCamera.RoiHeight = (ushort)(_RoiRect.Height + _RoiRect.Y);
            }
            else
            {
                _ActiveCamera.RoiStartX = 0;
                _ActiveCamera.RoiStartY = 0;
                _ActiveCamera.RoiWidth = _ActiveCamera.ImagingColumns / _ActiveCamera.HBin;
                _ActiveCamera.RoiHeight = _ActiveCamera.ImagingRows / _ActiveCamera.VBin;
            }

            #endregion

            // Setup estimate time remaining countdown
            double cameraDownloadTime = (_ImageChannel.ReadoutSpeed == 0) ? 10.0 : 1.5;
            int iBinningFactor = _ImageChannel.BinningMode;
            double estCaptureTime = _ImageChannel.Exposure + (cameraDownloadTime / (iBinningFactor * iBinningFactor));
            DateTime dateTime = DateTime.Now;
            _ImageInfo = new Azure.Image.Processing.ImageInfo();
            _ImageInfo.DateTime = System.String.Format("{0:G}", dateTime.ToString());
            _ImageInfo.CaptureType = "Chemi";
            _ImageInfo.IsScannedImage = false;
            _ImageInfo.BinFactor = _ImageChannel.BinningMode;
            _ImageInfo.RedChannel.Exposure = _ImageChannel.Exposure;
            _ImageInfo.ReadoutSpeed = (_ImageChannel.ReadoutSpeed == 0) ? "Normal" : "Fast";
            _ImageInfo.GainValue = _ImageChannel.AdGain;

            if (CompletionEstimate != null)
            {
                CompletionEstimate(this, dateTime, estCaptureTime);
            }

            if (CommandStatus != null)
            {
                CommandStatus(this, "Capturing image....");
            }

            try
            {
                _ActiveCamera.GrabImage(_ImageChannel.Exposure,
                                        CaptureFrameType.Normal,
                                        ref _CapturedImage);

                if (_CapturedImage != null)
                {
                    if (_CapturedImage.CanFreeze) { _CapturedImage.Freeze(); }
                }

                if (CommandStatus != null)
                {
                    CommandStatus(this, string.Empty);
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
                    throw new Exception("Image capture error.", cex);
                }
            }
            catch (Exception ex)
            {
                if (!_IsCommandAborted)
                {
                    _ActiveCamera.StopCapture();
                }
                throw new Exception("Image capture error.", ex);
            }
            finally
            {
                //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                //GC.WaitForPendingFinalizers();
                //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                // Force garbage collection.
                //GC.Collect();
                // Wait for all finalizers to complete before continuing.
                //GC.WaitForPendingFinalizers();
            }

        }

        public override void Finish()
        {
            if (CommandStatus != null)
            {
                CommandStatus(this, string.Empty);
            }
        }

        public override void AbortWork()
        {
            _IsCommandAborted = true;

            try
            {
                _ActiveCamera.StopCapture();
                System.Threading.Thread.Sleep(100);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

    }
}
