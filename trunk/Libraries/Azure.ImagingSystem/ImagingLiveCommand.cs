using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using APOGEELib;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows.Threading;
using Azure.CameraLib;
using Azure.CommandLib;
using Azure.Image.Processing;
using Azure.ImagingSystem;
using Hats.APDCom;  //APDTransfer

namespace Azure.ImagingSystem
{
    public class ImagingLiveCommand : ThreadBase
    {
        // Image capture status delegate
        public delegate void CommandStatusHandler(object sender, string status);
        // Image capture status event
        public event CommandStatusHandler CommandStatus;

        // Image received delegate
        public delegate void ImageReceivedHandler(BitmapSource displayBitmap);
        /// <summary>
        /// Live image received event handler. Triggered everytime the live image is received.
        /// </summary>
        public event ImageReceivedHandler LiveImageReceived;

        #region private field/data...

        private Dispatcher _CallingDispatcher = null;
        private CameraLibBase _ActiveCamera = null;
        private APDTransfer _ApdTransfer = null;
        private List<RgbLedIntensity> _RgbLedIntensities;
        private BitmapSource _DisplayBitmap;

        private ImageChannelSettings _ImageChannel = null;
        private string _StatusText = string.Empty;
        private Rect _RoiRect;
        private bool _IsCommandAborted = false;
        private bool _IsUserVersion = false;
        #endregion

        public ImagingLiveCommand(Dispatcher callingDispatcher,
                                  CameraLibBase camera,
                                  APDTransfer apdTransfer,
                                  ImageChannelSettings imageChannel,
                                  Rect cropRect,
                                  bool bIsUserVersion)
        {
            _CallingDispatcher = callingDispatcher;
            _ActiveCamera = camera;
            _ApdTransfer = apdTransfer;
            _ImageChannel = imageChannel;
            _RgbLedIntensities = imageChannel.RgbIntensities;
            _RoiRect = cropRect;
            _IsUserVersion = bIsUserVersion;
        }

        public override void Initialize()
        {
        }

        public override void Finish()
        {
            _StatusText = string.Empty;
            if (CommandStatus != null)
            {
                CommandStatus(this, _StatusText);
            }

            // Turn off white LED.
            if (_ApdTransfer != null && _ApdTransfer.APDTransferIsAlive)
            {
                _ApdTransfer.APDLaserTurnOffWhiteLED();
            }

            if (_ActiveCamera != null)
            {
                if (_ActiveCamera.IsAcqRunning)
                    _ActiveCamera.StopCapture();
            }

            _ActiveCamera.CameraNotif -= new CameraLibBase.CameraNotifHandler(MVCameraLive_CameraNotif);
            _DisplayBitmap = null;
        }

        public override void ThreadFunction()
        {
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

            _StatusText = "Preparing...";
            if (CommandStatus != null)
            {
                CommandStatus(this, _StatusText);
            }

            if (_ApdTransfer != null && _ApdTransfer.APDTransferIsAlive)
            {
                // Open camera housing cover
                _ApdTransfer.APDLaserOpenEX();
                System.Threading.Thread.Sleep(1000);    // NOTE: wait for camera cover to be opened

                // Turn on the white light for the general user version
                if (_IsUserVersion)
                {
                    _ApdTransfer.APDLaserTurnOnWhiteLED();
                }
            }

            _StatusText = "LIVE MODE";
            if (CommandStatus != null)
            {
                CommandStatus(this, _StatusText);
            }

            try
            {
                _ActiveCamera.CameraNotif += new CameraLibBase.CameraNotifHandler(MVCameraLive_CameraNotif);

                _ActiveCamera.StartContinuousMode(_ImageChannel.Exposure);

                while (_ActiveCamera.IsAcqRunning && !_IsCommandAborted)
                {
                    // See: link why Sleep(1) is better than Sleep(0)
                    // http://joeduffyblog.com/2006/08/22/priorityinduced-starvation-why-sleep1-is-better-than-sleep0-and-the-windows-balance-set-manager/
                    System.Threading.Thread.Sleep(1);
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
                if (!_IsCommandAborted)
                {
                    _ActiveCamera.StopCapture();
                }
                throw new Exception("Live mode error.", ex);
            }
            finally
            {
                // Forces a garbage collection
                //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                //GC.WaitForPendingFinalizers();
                //GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced);
                // Force garbage collection.
                //GC.Collect();
                // Wait for all finalizers to complete before continuing.
                //GC.WaitForPendingFinalizers();
            }
        }

        internal void MVCameraLive_CameraNotif(object sender)
        {
            _DisplayBitmap = (sender as WriteableBitmap);

            if (LiveImageReceived != null)
            {
                if (_DisplayBitmap != null)
                {
                    // Rotate the image 180 degree
                    TransformedBitmap tb = new TransformedBitmap();
                    tb.BeginInit();
                    tb.Source = _DisplayBitmap;
                    System.Windows.Media.RotateTransform transform = new System.Windows.Media.RotateTransform(180);
                    tb.Transform = transform;
                    tb.EndInit();
                    _DisplayBitmap = new WriteableBitmap((BitmapSource)tb);
                }
            }

            if (_DisplayBitmap != null)
            {
                //if (_RoiRect.Width > 0 && _RoiRect.Height > 0)
                //{
                //    _DisplayBitmap = ImageProcessing.Crop(_DisplayBitmap, _RoiRect);
                //}
                if (_DisplayBitmap.CanFreeze)
                {
                    _DisplayBitmap.Freeze();
                }

                if (LiveImageReceived != null)
                {
                    LiveImageReceived(_DisplayBitmap);
                }
            }
        }

        public override void AbortWork()
        {
            _IsCommandAborted = true;
            _ActiveCamera.CameraNotif -= new CameraLibBase.CameraNotifHandler(MVCameraLive_CameraNotif);

            //try
            //{
            //    _ActiveCamera.StopCapture();
            //    System.Threading.Thread.Sleep(200);
            //}
            //catch (Exception)
            //{
            //    //System.Diagnostics.Debug.WriteLine(ex.Message);
            //    throw;
            //}
        }
    }
}
