using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;   //ObservableCollection
using Azure.ImagingSystem;  //LightingType

namespace Azure.Configuration
{
    namespace Settings
    {
        public class ConfigSettings
        {
            #region Private data...

            private ObservableCollection<BinningFactorType> _BinningFactorOptions = new ObservableCollection<BinningFactorType>();
            private List<PvCamSensitivity> _PvCamSensitivityOptions = new List<PvCamSensitivity>();
            private ObservableCollection<GainType> _GainOptions = new ObservableCollection<GainType>();
            private List<ResolutionType> _ResolutionOptions = new List<ResolutionType>();
            private List<QualityType> _QualityOptions = new List<QualityType>();
            private List<ScanSpeedType> _ScanSpeedOptions = new List<ScanSpeedType>();
            private ObservableCollection<APDGainType> _APDGains = new ObservableCollection<APDGainType>();
            private ObservableCollection<FocusType> _Focus = new ObservableCollection<FocusType>();
            private ObservableCollection<APDPgaType> _APDPgas = new ObservableCollection<APDPgaType>();
            private ObservableCollection<PhosphorLaserModules> _PhosphorLaserModules = new ObservableCollection<PhosphorLaserModules>();
            private int _XMaxValue = 0;
            private int _YMaxValue = 0;
            private int _ZMaxValue = 0;
            private double _XMotorSubdivision = 0;
            private double _YMotorSubdivision = 0;
            private double _ZMotorSubdivision = 0;
            private ObservableCollection<MotorSettingsType> _MotorSettings = new ObservableCollection<MotorSettingsType>();
            private int[] _LasersIntensities = new int[4];
            private List<LaserSettingsType> _LaserIntensitySettings = new List<LaserSettingsType>();
            private List<SampleTypeSetting> _SampleTypeSettings = new List<SampleTypeSetting>();
            private List<RgbLedIntensity> _RgbLedIntensities = new List<RgbLedIntensity>();

            private CameraModeSetting _CameraModeSettings = new CameraModeSetting();

            // Signal options
            private List<Signal> _LaserASignalOptions = new List<Signal>();
            private List<Signal> _LaserBSignalOptions = new List<Signal>();
            private List<Signal> _LaserCSignalOptions = new List<Signal>();
            private List<Signal> _LaserDSignalOptions = new List<Signal>();
            // Phosphor signal options
            private List<Signal> _PhosphorCSignalOptions = new List<Signal>();
            private List<Signal> _PhosphorDSignalOptions = new List<Signal>();

            private List<DyeType> _DyeOptions = new List<DyeType>();        // Default dye types
            private List<DyeType> _CustDyeOptions = new List<DyeType>();    // Custom dye types
            private List<AppMethod> _AppMethods = new List<AppMethod>();
            private List<AppMethod> _PhosphorMethods = new List<AppMethod>();

            private List<ImagingSettings> _ImagingSettings = new List<ImagingSettings>();

            private AutoScan _AutoScanSettings = new AutoScan();

            #endregion

            #region Constructors...

            public ConfigSettings()
            {
                ComPort = 3;
                GalilAddress = "COM1 115200";
            }

            #endregion

            #region Public properties...

            public int XMaxValue
            {
                get
                {
                    return _XMaxValue;
                }
                set
                {
                    _XMaxValue = value;
                }
            }
            public int YMaxValue
            {
                get
                {
                    return _YMaxValue;
                }
                set
                {
                    _YMaxValue = value;
                }
            }
            public int ZMaxValue
            {
                get
                {
                    return _ZMaxValue;
                }
                set
                {
                    _ZMaxValue = value;
                }
            }
            public int WMaxValue { get; set; }
            public int WMinValue { get; set; }
            public int WMediumValue { get; set; }
            public double XMotorSubdivision
            {
                get
                {
                    return _XMotorSubdivision;
                }

                set
                {
                    _XMotorSubdivision = value;
                }

            }
            public double YMotorSubdivision
            {
                get
                {
                    return _YMotorSubdivision;
                }

                set
                {
                    _YMotorSubdivision = value;
                }

            }
            public double ZMotorSubdivision
            {
                get
                {
                    return _ZMotorSubdivision;
                }

                set
                {
                    _ZMotorSubdivision = value;
                }

            }
            public double WMotorSubdivision { get; set; }
            public double XEncoderSubdivision { get; set; }
            // Non-EUI: Lasers default intensity
            public int LaserAIntensity { get; set; }
            public int LaserBIntensity { get; set; }
            public int LaserCIntensity { get; set; }
            public int LaserDIntensity { get; set; }

            // Non-EUI: Lasers maximum intensity
            public int LaserAMaxIntensity { get; set; }
            public int LaserBMaxIntensity { get; set; }
            public int LaserCMaxIntensity { get; set; }
            public int LaserDMaxIntensity { get; set; }

            public int ComPort { get; set; }
            public String GalilAddress { get; set; }
            public bool IsSimulationMode { get; set; }

            public bool IsChannelALightShadeFix { get; set; }
            public bool IsChannelBLightShadeFix { get; set; }
            public bool IsChannelCLightShadeFix { get; set; }
            public bool IsChannelDLightShadeFix { get; set; }

            // Replaced with 'IsFluorescence2LinesAvgScan' and 'IsPhosphor2LinesAvgScan'
            // to be able to apply the correction only for Fluorescence or Phosphor scan or both.
            //public bool IsUnidirectionalScan { get; set; }

            public bool IsFluorescence2LinesAvgScan { get; set; }

            public bool IsPhosphor2LinesAvgScan { get; set; }

            public int XMotionTurnDelay { get; set; }

            public int XMotionExtraMoveLength { get; set; }

            public bool MotionPolarityClkX { get; set; }
            public bool MotionPolarityDirX { get; set; }
            public bool MotionPolarityEnableX { get; set; }
            public bool MotionPolarityHomeX { get; set; }
            public bool MotionPolarityFwdX { get; set; }
            public bool MotionPolarityBwdX { get; set; }
            public bool MotionPolarityClkY { get; set; }
            public bool MotionPolarityDirY { get; set; }
            public bool MotionPolarityEnableY { get; set; }
            public bool MotionPolarityHomeY { get; set; }
            public bool MotionPolarityFwdY { get; set; }
            public bool MotionPolarityBwdY { get; set; }
            public bool MotionPolarityClkZ { get; set; }
            public bool MotionPolarityDirZ { get; set; }
            public bool MotionPolarityEnableZ { get; set; }
            public bool MotionPolarityHomeZ { get; set; }
            public bool MotionPolarityFwdZ { get; set; }
            public bool MotionPolarityBwdZ { get; set; }
            public bool MotionPolarityClkW { get; set; }
            public bool MotionPolarityDirW { get; set; }
            public bool MotionPolarityEnableW { get; set; }
            public bool MotionPolarityHomeW { get; set; }
            public bool MotionPolarityFwdW { get; set; }
            public bool MotionPolarityBwdW { get; set; }
            public bool HomeMotionsAtLaunchTime { get; set; }
            public double YCompenOffset { get; set; }
            public bool YCompenSationBitAt { get; set; }
            public bool ScanDynamicBitAt { get; set; }
            public int RadiatorTemperatureL { get; set; }
            public int RadiatorTemperatureR1 { get; set; }
            public int RadiatorTemperatureR2 { get; set; }
            public bool ImageOffsetProcessing { get; set; }
            public bool PixelOffsetProcessing { get; set; }
            public int XOddNumberedLine { get; set; }
            public int XEvenNumberedLine { get; set; }
            public int YOddNumberedLine { get; set; }
            public int YEvenNumberedLine { get; set; }
            //比如将100微米的图像取平均新图为50微米
            public bool PhosphorModuleProcessing { get; set; }
            public bool AllModuleProcessing { get; set; }
            public double InternalLowTemperature { get; set; }
            public double InternalModerateTemperature { get; set; }
            public double InternalHighTemperature { get; set; }
            public double ModuleLowTemperature { get; set; }
            public double ModuleModerateTemperature { get; set; }
            public double ModuleHighTemperature { get; set; }
            public ObservableCollection<BinningFactorType> BinningFactorOptions
            {
                get { return _BinningFactorOptions; }
            }

            public List<PvCamSensitivity> PvCamSensitivityOptions
            {
                get { return _PvCamSensitivityOptions; }
            }

            public ObservableCollection<GainType> GainOptions
            {
                get { return _GainOptions; }
            }

            public List<ResolutionType> ResolutionOptions
            {
                get { return _ResolutionOptions; }
            }

            public List<QualityType> QualityOptions
            {
                get { return _QualityOptions; }
            }

            public List<ScanSpeedType> ScanSpeedOptions
            {
                get { return _ScanSpeedOptions; }
            }

            public ObservableCollection<APDGainType> APDGains
            {
                get { return _APDGains; }
            }
            public ObservableCollection<FocusType> Focus
            {
                get { return _Focus; }
            }

            public ObservableCollection<APDPgaType> APDPgas
            {
                get { return _APDPgas; }
            }
            public ObservableCollection<PhosphorLaserModules> PhosphorLaserModules
            {
                get { return _PhosphorLaserModules; }
            }
            public ObservableCollection<MotorSettingsType> MotorSettings
            {
                get { return _MotorSettings; }
            }

            public int[] LasersIntensities
            {
                get { return _LasersIntensities; }
            }

            public List<LaserSettingsType> LasersIntensitySettings
            {
                get { return _LaserIntensitySettings; }
            }

            public List<SampleTypeSetting> SampleTypeSettings
            {
                get { return _SampleTypeSettings; }
            }

            public List<RgbLedIntensity> RgbLedIntensities
            {
                get { return _RgbLedIntensities; }
            }

            public List<Signal> LaserASignalOptions
            {
                get { return _LaserASignalOptions; }
            }
            public List<Signal> LaserBSignalOptions
            {
                get { return _LaserBSignalOptions; }
            }
            public List<Signal> LaserCSignalOptions
            {
                get { return _LaserCSignalOptions; }
            }
            public List<Signal> LaserDSignalOptions
            {
                get { return _LaserDSignalOptions; }
            }
            // Phosphor signals
            public List<Signal> PhosphorCSignalOptions
            {
                get { return _PhosphorCSignalOptions; }
            }
            public List<Signal> PhosphorDSignalOptions
            {
                get { return _PhosphorDSignalOptions; }
            }

            public List<DyeType> DyeOptions
            {
                get { return _DyeOptions; }
            }

            public List<DyeType> CustDyeOptions
            {
                get { return _CustDyeOptions; }
            }

            public List<AppMethod> AppMethods
            {
                get { return _AppMethods; }
            }

            public List<AppMethod> PhosphorMethods
            {
                get { return _PhosphorMethods; }
            }

            public CameraModeSetting CameraModeSettings
            {
                get { return _CameraModeSettings; }
            }

            public List<ImagingSettings> ImagingSettings
            {
                get { return _ImagingSettings; }
            }

            public AutoScan AutoScanSettings
            {
                get { return _AutoScanSettings; }
            }

            #endregion
        }

    }

}
