using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Azure.Image.Processing;

namespace Azure.ImagingSystem
{
    /// <summary>
    /// Blue  = 488nm = LaserD
    /// Green = 520nm = LaserB
    /// IR700 = 658nm = LaserC
    /// IR800 = 784nm = LaserA
    /// </summary>
    public enum LaserType
    {
        None = 0,
        LaserA = 784,
        LaserB = 520,
        LaserC = 658,
        LaserD = 488
    }

    public enum ApdType
    {
        None,
        ApdA,
        ApdB,
        ApdC,
        ApdD
    }

    public enum MotorType
    {
        X = 1,
        Y = 2,
        Z = 3,
        W
    }

    public enum SampleType
    {
        Gel,
        Plate,
        Membrane,
        Custom,
    }

    public class MotorSettingsType
    {
        #region Public properties...

        public MotorType MotorType { get; set; }
        public double Position { get; set; }
        public double Speed { get; set; }
        public double Accel { get; set; }
        public double Dccel { get; set; }

        #endregion  //Public properties

        #region Constructors...

        public MotorSettingsType()
        {
        }

        public MotorSettingsType(MotorType motorType, int position, int speed)
        {
            this.MotorType = motorType;
            this.Position = position;
            this.Speed = speed;
        }

        #endregion  //Constructors
    }

    public class APDGainType
    {
        #region Public properties...

        public int Position { get; set; }

        public int Value { get; set; }

        public string DisplayName { get; set; }

        #endregion

        #region Constructors...

        public APDGainType()
        {
        }

        public APDGainType(int position, int value, string displayName)
        {
            this.Position = position;
            this.Value = value;
            this.DisplayName = displayName;
        }

        #endregion
    }

    public class FocusType
    {
        #region Public properties...

        public int Position { get; set; }

        public double Value { get; set; }

        public string DisplayName { get; set; }

        #endregion

        #region Constructors...

        public FocusType()
        {
        }

        public FocusType(int position, int value, string displayName)
        {
            this.Position = position;
            this.Value = value;
            this.DisplayName = displayName;
        }

        #endregion
    }

    public class APDPgaType
    {
        #region Public properties...

        public int Position { get; set; }

        public int Value { get; set; }

        public string DisplayName { get; set; }

        #endregion

        #region Constructors...

        public APDPgaType()
        {
        }

        public APDPgaType(int position, int value, string displayName)
        {
            this.Position = position;
            this.Value = value;
            this.DisplayName = displayName;
        }

        #endregion
    }
    public class PhosphorLaserModules
    {
        #region Public properties...

        public string DisplayName { get; set; }

        #endregion

        #region Constructors...

        public PhosphorLaserModules()
        {
        }

        public PhosphorLaserModules(string displayName)
        {
            this.DisplayName = displayName;
        }

        #endregion
    }

    public class ShellFanModules
    {
        #region Public properties...

        public string DisplayName { get; set; }

        #endregion

        #region Constructors...

        public ShellFanModules()
        {
        }

        public ShellFanModules(string displayName)
        {
            this.DisplayName = displayName;
        }

        #endregion
    }

    public class ResolutionType
    {
        #region Public properties...

        public int Position { get; set; }

        public int Value { get; set; }

        public string DisplayName { get; set; }

        #endregion

        #region Constructors...

        public ResolutionType()
        {
        }

        public ResolutionType(int position, int value, string displayName)
        {
            this.Position = position;
            this.Value = value;
            this.DisplayName = displayName;
        }

        #endregion
    }

    public class QualityType
    {
        #region Public properties...

        public int Position { get; set; }

        public int Value { get; set; }

        public string DisplayName { get; set; }

        #endregion

        #region Constructors...

        public QualityType()
        {
        }

        public QualityType(int position, int value, string displayName)
        {
            this.Position = position;
            this.Value = value;
            this.DisplayName = displayName;
        }

        #endregion

    }



    public class ScanSpeedType
    {
        #region Public properties...

        public int Position { get; set; }

        public int Value { get; set; }

        public string DisplayName { get; set; }

        #endregion

        #region Constructors...

        public ScanSpeedType()
        {
        }

        public ScanSpeedType(int position, int value, string displayName)
        {
            this.Position = position;
            this.Value = value;
            this.DisplayName = displayName;
        }

        #endregion

    }

    /// <summary>
    /// Laser intensities setting
    /// </summary>
    public class LaserSettingsType
    {
        #region Public properties...

        public int Position { get; set; }
        public LaserType LaserType { get; set; }
        public List<int> Intensities { get; set; }
        public int MaxIntensity { get; set; }

        #endregion

        #region Constructors...

        public LaserSettingsType()
        {
            this.Intensities = new List<int>();
        }

        public LaserSettingsType(int position, LaserType laserType, List<int> intensities, int maxIntensity)
        {
            this.Position = position;
            this.LaserType = laserType;
            this.Intensities = intensities;
            this.MaxIntensity = maxIntensity;
        }

        #endregion
    }

    public class SampleTypeSetting
    {
        #region Public properties...

        public int Position { get; set; }
        public string DisplayName { get; set; }
        //public SampleType SampleType { get; set; }
        public double FocusPosition { get; set; }
        public bool IsDefaultSampleType { get; set; }

        #endregion

        #region Constructors...

        public SampleTypeSetting()
        {
        }

        /*public SampleTypeSetting(string displayName, SampleType sampleType, double focusPosition)
        {
            this.DisplayName = displayName;
            this.SampleType = sampleType;
            this.FocusPosition = focusPosition;
        }*/

        public SampleTypeSetting(int position, string displayName, double focusPosition)
        {
            this.Position = position;
            this.DisplayName = displayName;
            this.FocusPosition = focusPosition;
        }

        #endregion

        /*public bool Equals(SampleTypeSetting otherSampleType)
        {
            bool bResult = false;

            if (otherSampleType == null) return bResult;

            if (this.DisplayName.Equals(otherSampleType.DisplayName, StringComparison.OrdinalIgnoreCase) &&
                this.FocusPosition == otherSampleType.FocusPosition)
            {
                bResult = true;
            }

            return bResult;
        }*/
    }

    public class Signal
    {
        public int Position { get; set; }
        public LaserType LaserType { get; set; }
        public int LaserIntensity { get; set; }
        public int ApdGain { get; set; }
        public int ApdPga { get; set; }
        public int ColorChannel { get; set; }
        public int LaserIntInmW { get; set; }

        public Signal()
        {
        }

        public Signal(int position, LaserType laserType, int laserIntInmW, int laserInt, int gain, int pga)
        {
            this.Position = position;
            this.LaserType = laserType;
            this.LaserIntInmW = laserIntInmW;
            this.LaserIntensity = laserInt;
            this.ApdGain = gain;
            this.ApdPga = pga;
        }
    }

    public class DyeType
    {
        #region Public properties...

        public int Position { get; set; }
        public string DisplayName { get; set; }
        public LaserType LaserType { get; set; }
        public string WaveLength { get; set; }
        public bool IsCustomDye { get; set; }

        #endregion

        #region Constructors...

        public DyeType()
        {
        }

        public DyeType(int position, string displayName, LaserType laserType, string waveLength, bool bIsCustomDye)
        {
            this.Position = position;
            this.DisplayName = displayName;
            this.LaserType = laserType;
            this.WaveLength = waveLength;
            this.IsCustomDye = bIsCustomDye;
        }

        #endregion
    }

    public class AppMethod
    {
        public string Name { get; set; }
        public int Sampletype { get; set; }
        public int Pixelsize { get; set; }
        public int Scanspeed { get; set; }
        public int IntensityLevel { get; set; } // currently only being used for Phosphor imaging.
        public List<AppDyeData> Dyes = new List<AppDyeData>();

        public AppMethod()
        {
        }
    }

    public class AppDyeData
    {
        public int DyeType { get; set; }
        public int SignalIntensity { get; set; }
        public ImageChannelType ColorChannel { get; set; }

        public AppDyeData()
        {
        }
    }

    public class AutoScan
    {
        public int Resolution;
        public int OptimalVal;
        public double OptimalDelta;
        public int Ceiling;
        public int Floor;
        public double Alpha488;

        public Signal LaserASignalLevel;
        public Signal LaserBSignalLevel;
        public Signal LaserCSignalLevel;
        public Signal LaserDSignalLevel;

        public AutoScan()
        {
        }
    }

    public class PreviewChannel

    {
        #region Public properties...

        public int Position { get; set; }

        public int Value { get; set; }

        public string DisplayName { get; set; }

        #endregion

        #region Constructors...

        public PreviewChannel()
        {
        }

        public PreviewChannel(int position, int value, string displayName)
        {
            this.Position = position;
            this.Value = value;
            this.DisplayName = displayName;
        }

        #endregion

    }


    /*public class LaserImageChannel
    {
        public System.Windows.Media.Imaging.WriteableBitmap ImageBuffer { get; set; }
        public LaserType LaserChannel { get; set; }
        public bool IsSelected { get; set; }

        public LaserImageChannel()
        {
        }
    }*/

}
