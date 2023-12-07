using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Linq;  //XDocument
using System.Xml.XPath; //XPathDocument
using Azure.ImagingSystem;
using Azure.Image.Processing;

namespace Azure.Configuration
{
    namespace Settings
    {
        /// <summary>
        /// Class is responsible for loading and saving persistent application settings.
        /// Call OnStartup method from App.OnStartup.
        /// Call OnExit method from App.OnExit.
        /// 
        /// Class contains Settings instance. Settings contains all persistent
        /// application settings and can be accessed from every place in the program
        /// using SettingsManager.ApplicationSettings property.
        /// 
        /// OnStartup creates Settings instance in any case, even if file with application
        /// settings cannot be loaded.
        /// 
        /// Application settings are kept in XML file inside of Application Data directory.
        /// For every program where this class is used, change applicationDirectory
        /// and optionally settingsFileName values in this class.
        /// 
        /// For example, if applicationDirectory = "MyProgram", and 
        /// settingsFileName = "Settings.xml", settings file name is:
        /// (System Drive):\Documents and Settings\(User Name)\Local Settings\Application Data\MyProgram\Settings.xml
        /// 
        /// See also: Settings class.
        /// </summary>
        static public class SettingsManager
        {
            #region Class Members

            // Persistent application settings
            static ApplicationSettings appSettings = new ApplicationSettings(); // default application Settings instance
            // in the case settings cannot be loaded from file.

            static ConfigSettings configSettings = new ConfigSettings();        // default imaging system settings

            //static MasterLibrary masterLibrary = null;                          // Darkmaster library and flat field image

            // Subdirectory in Application Data where settings file is kept.
            // Change this value for every program where SettingsManager class is used.
            //static string applicationDataPath = string.Empty;

            static string applicationDataPath = string.Empty;
            const string appSettingsFileName = "Settings.xml";      // Windows application settings
            //static string applicationPath = string.Empty;
            const string configFilename = "Config.xml";             // Imaging system configuration
            const string custSettingsFileName = "CustSettings.xml"; // Custom settings
            const string sysSettingsFileName = "SysSettings.xml";   // System specific settings
            const string methodFilename = "Method.xml";             // default scanning methods

            #endregion Class Members

            #region Constructor

            static SettingsManager()
            {
                //EnsureDirectoryExists();
                IsEngrUI = false;
            }

            #endregion Constructor

            #region Properties

            // Differentiate between standard and Engineering UI
            public static bool IsEngrUI { get; set; }

            public static ApplicationSettings ApplicationSettings
            {
                get { return appSettings; }
            }

            public static ConfigSettings ConfigSettings
            {
                get { return configSettings; }
            }

            public static string ApplicationDataPath
            {
                get { return applicationDataPath; }
                set { applicationDataPath = value; }
            }

            //public static string ApplicationPath
            //{
            //    get { return applicationPath; }
            //    set { applicationPath = value; }
            //}

            //public static MasterLibrary MasterLibrary
            //{
            //    get { return masterLibrary; }
            //}

            #endregion Properties

            #region Startup, Exit

            /// <summary>
            /// Call this function from App.OnStartup function
            /// </summary>
            public static void OnStartup()
            {
                // Copy configuration files to common data directory (if doesn't exist)
                EnsureConfigFilesExist();

                //if (Directory.Exists(applicationDataPath))
                //{
                //    LoadSettings();
                //}
                LoadAppSettings();
                LoadConfigFiles();
                //LoadMasterLibraryInfo();
            }

            /// <summary>
            /// Call this function from App.OnExit function
            /// </summary>
            public static void OnExit()
            {
                //if (Directory.Exists(applicationPath))
                //{
                //    //SaveSettings();
                //}
                SaveAppSettings();
            }

            #endregion Overrides

            #region Other Functions

            /// <summary>
            /// Returns application settings file name
            /// </summary>
            static string AppSettingsFileName
            {
                get
                {
                    // File is kept in Application Data directory, program subdirectory.
                    // See also: EnsureDirectoryExists function.
                    return Path.Combine(applicationDataPath, appSettingsFileName);
                }
            }

            /// <summary>
            /// Load application settings from xml file
            /// </summary>
            static void LoadAppSettings()
            {
                ApplicationSettings tmp;

                try
                {
                    //Throws FileNotFoundException
                    //Could not load file or assembly 'WpfApplication.XmlSerializers
                    XmlSerializer xml = new XmlSerializer(typeof(ApplicationSettings));
                    // Avoid FileNotFoundException (the above statement throw an exception),
                    // hasitant to use because someone said it causes memory leaks.
                    //XmlSerializer xml = XmlSerializer.FromTypes(new[] { typeof(Settings) })[0];

                    using (Stream stream = new FileStream(AppSettingsFileName,
                        FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        tmp = (ApplicationSettings)xml.Deserialize(stream);
                    }
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e.Message);
                    return;
                }

                // If everything is OK, replace default Settings instance
                // with instance loaded from file
                appSettings = tmp;
            }

            /// <summary>
            /// Save application settings to xml file
            /// </summary>
            static void SaveAppSettings()
            {
                try
                {
                    //Throws FileNotFoundException
                    //Could not load file or assembly 'WpfApplication.XmlSerializers
                    XmlSerializer xml = new XmlSerializer(typeof(ApplicationSettings));
                    // Avoid FileNotFoundException (the above statement throw an exception),
                    // hasitant to use because someone said it causes memory leaks.
                    //XmlSerializer xml = XmlSerializer.FromTypes(new[] { typeof(Settings) })[0];

                    using (Stream stream = new FileStream(AppSettingsFileName,
                           FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        xml.Serialize(stream, appSettings);
                    }
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e.Message);
                }
            }

            // SuppressMessage doesn't work, to ask in MSDN forum
            /***[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
            static void EnsureDirectoryExists()
            {
                try
                {
                    DirectoryInfo info = new DirectoryInfo(
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        applicationDataPath));

                    if (!info.Exists)
                    {
                        info.Create();
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                }
            }***/

            /// <summary>
            /// Make sure the configuration files exist in ProgramData
            /// Copy configuration files from Program Files folder to ProgramData (if it doesn't exist)
            /// </summary>
            static void EnsureConfigFilesExist()
            {
                #region === Copy config.xml ===

                string sourceConfigFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, configFilename);
                string targetConfigFilePath = System.IO.Path.Combine(ApplicationDataPath, configFilename);

                if (!System.IO.File.Exists(sourceConfigFilePath) && !System.IO.File.Exists(targetConfigFilePath))
                {
                    throw new Exception("Missing system configuration file: " + configFilename);
                }

                if (!System.IO.File.Exists(targetConfigFilePath))
                {
                    if (System.IO.File.Exists(sourceConfigFilePath))
                    {
                        System.IO.File.Copy(sourceConfigFilePath, targetConfigFilePath);
                    }
                }

                #endregion

                if (IsEngrUI)
                {
                    #region === Copy secure.xml ===

                    string authenConfigFile = "EUIAuthen.xml";
                    string sourceAuthenConfigFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, authenConfigFile);
                    string strTargetAuthenFilePath = System.IO.Path.Combine(ApplicationDataPath, authenConfigFile);

                    if (!File.Exists(sourceAuthenConfigFilePath) && !File.Exists(strTargetAuthenFilePath))
                    {
                        throw new Exception("Missing system configuration file: " + authenConfigFile);
                    }

                    if (!File.Exists(strTargetAuthenFilePath))
                    {
                        File.Copy(sourceAuthenConfigFilePath, strTargetAuthenFilePath);
                    }

                    #endregion
                }
                else
                {
                    #region === Copy lens.xml ===
                    /*
                    string sourceLensConfigFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, lensSettingsFilename);
                    string targetLensFilePath = System.IO.Path.Combine(ApplicationDataPath, lensSettingsFilename);

                    if (!File.Exists(sourceLensConfigFilePath) && !File.Exists(targetLensFilePath))
                    {
                        throw new Exception("Missing system configuration file: " + lensSettingsFilename);
                    }

                    if (!File.Exists(targetLensFilePath))
                    {
                        if (File.Exists(sourceLensConfigFilePath))
                        {
                            File.Copy(sourceLensConfigFilePath, targetLensFilePath);
                        }
                    }
                    */
                    #endregion

                    #region === Copy SysSettings.xml ===

                    //string strSysSettingsFile = "settings.xml";
                    string sourceSysSettingsFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, sysSettingsFileName);
                    string targetSysSettingsFilePath = System.IO.Path.Combine(ApplicationDataPath, sysSettingsFileName);

                    if (!File.Exists(sourceSysSettingsFilePath) && !File.Exists(targetSysSettingsFilePath))
                    {
                        throw new Exception("Missing system configuration file: " + sysSettingsFileName);
                    }

                    if (!File.Exists(targetSysSettingsFilePath))
                    {
                        if (File.Exists(sourceSysSettingsFilePath))
                        {
                            File.Copy(sourceSysSettingsFilePath, targetSysSettingsFilePath);
                        }
                    }

                    #endregion

                    #region === Copy secure.xml ===

                    string secureConfigFile = "secure.xml";
                    string sourceSecureConfigFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, secureConfigFile);
                    string strTargetSecureFilePath = System.IO.Path.Combine(ApplicationDataPath, secureConfigFile);

                    if (!File.Exists(sourceSecureConfigFilePath) && !File.Exists(strTargetSecureFilePath))
                    {
                        throw new Exception("Missing system configuration file: " + secureConfigFile);
                    }

                    if (!File.Exists(strTargetSecureFilePath))
                    {
                        File.Copy(sourceSecureConfigFilePath, strTargetSecureFilePath);
                    }

                    #endregion

                    #region === Copy users.xml ===
                    /*
                    string sourceUsersAcctFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, usersAccountFilename);
                    string targetUsersAcctFilePath = System.IO.Path.Combine(ApplicationDataPath, usersAccountFilename);

                    if (!File.Exists(sourceUsersAcctFilePath) && !File.Exists(targetUsersAcctFilePath))
                    {
                        throw new Exception("Missing system configuration file: " + usersAccountFilename);
                    }

                    if (!File.Exists(targetUsersAcctFilePath))
                    {
                        if (File.Exists(sourceUsersAcctFilePath))
                        {
                            File.Copy(sourceUsersAcctFilePath, targetUsersAcctFilePath);
                        }
                    }
                    */
                    #endregion

                    #region === Copy Method.xml ===

                    string sourceMethodFilePath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, methodFilename);
                    string targetMethodFilePath = System.IO.Path.Combine(ApplicationDataPath, methodFilename);

                    if (!System.IO.File.Exists(sourceMethodFilePath) && !System.IO.File.Exists(targetMethodFilePath))
                    {
                        throw new Exception("Missing default methods configuration file: " + methodFilename);
                    }

                    if (!System.IO.File.Exists(targetMethodFilePath))
                    {
                        if (System.IO.File.Exists(sourceMethodFilePath))
                        {
                            System.IO.File.Copy(sourceMethodFilePath, targetMethodFilePath);
                        }
                    }

                    #endregion
                }
            }

            public static void LoadConfigFiles()
            {
                LoadConfigurationFile();    // default settings
                LoadCustomSettingsFile();   // custom settings
                LoadSystemSettings();       // system specific settings

                if (!IsEngrUI)
                {
                    LoadMethodSettings();   // pre-defined application methods/protocols
                }

                // Merge the dyes list (default + custom dyes).
                if (configSettings.DyeOptions != null && configSettings.CustDyeOptions != null)
                {
                    if (configSettings.CustDyeOptions.Count > 0)
                    {
                        foreach (var dye in configSettings.CustDyeOptions)
                        {
                            configSettings.DyeOptions.Add(dye);
                        }
                    }
                }
            }

            #region public static void LoadConfigurationFile()
            /// <summary>
            /// Load the config.xml file.
            /// </summary>
            /// <param name="configFile"></param>
            public static void LoadConfigurationFile()
            {
                string configFilePath = Path.Combine(applicationDataPath, configFilename);

                if (!File.Exists(configFilePath))
                {
                    throw new Exception("Configuration file does not exits: " + configFilename);
                }

                XPathDocument xpathDoc = new XPathDocument(configFilePath);
                XPathNavigator xpathNav = xpathDoc.CreateNavigator();

                XPathNodeIterator iter = xpathNav.Select("/Config/ComPort");
                if (iter.MoveNext())
                {
                    configSettings.ComPort = int.Parse(iter.Current.GetAttribute("Value", ""));
                }

                iter = xpathNav.Select("/Config/GalilAddress");
                if (iter.MoveNext())
                {
                    configSettings.GalilAddress = iter.Current.GetAttribute("Value", "");
                }

                iter = xpathNav.Select("/Config/IsSimulationMode");
                if (iter.MoveNext())
                {
                    bool simFlag;
                    string simValue = iter.Current.GetAttribute("Value", "");
                    if (!Boolean.TryParse(simValue, out simFlag))
                    {
                        simFlag = false;
                    }
                    configSettings.IsSimulationMode = simFlag;
                }

                #region Camera mode settings...

                iter = xpathNav.Select("/Config/IsDarkCorrection");
                if (iter.MoveNext())
                {
                    bool flag;
                    string darkCorrValue = iter.Current.GetAttribute("Value", "");
                    if (!Boolean.TryParse(darkCorrValue, out flag))
                    {
                        flag = false;
                    }
                    ConfigSettings.CameraModeSettings.IsDarkCorrection = flag;
                }
                iter = xpathNav.Select("/Config/IsFlatCorrection");
                if (iter.MoveNext())
                {
                    bool flag;
                    string flatCorrValue = iter.Current.GetAttribute("Value", "");
                    if (!Boolean.TryParse(flatCorrValue, out flag))
                    {
                        flag = false;
                    }
                    ConfigSettings.CameraModeSettings.IsFlatCorrection = flag;
                }

                #region Lens distortion correction parameters...

                iter = xpathNav.Select("/Config/BarrelParamA");
                if (iter.MoveNext())
                {
                    double barrelParamA;
                    if (!Double.TryParse(iter.Current.GetAttribute("Value", ""), out barrelParamA))
                    {
                        barrelParamA = 0.0d;
                    }
                    ConfigSettings.CameraModeSettings.BarrelParamA = barrelParamA;
                }

                iter = xpathNav.Select("/Config/BarrelParamB");
                if (iter.MoveNext())
                {
                    double barrelParamB;
                    if (!Double.TryParse(iter.Current.GetAttribute("Value", ""), out barrelParamB))
                    {
                        barrelParamB = 0.013d;
                    }
                    ConfigSettings.CameraModeSettings.BarrelParamB = barrelParamB;
                }

                iter = xpathNav.Select("/Config/BarrelParamC");
                if (iter.MoveNext())
                {
                    double barrelParamC;
                    if (!Double.TryParse(iter.Current.GetAttribute("Value", ""), out barrelParamC))
                    {
                        barrelParamC = 0.0d;
                    }
                    ConfigSettings.CameraModeSettings.BarrelParamC = barrelParamC;
                }

                #endregion

                iter = xpathNav.Select("/Config/CCDCoolerSetPoint");
                if (iter.MoveNext())
                {
                    int ccdCoolerSetPoint;
                    if (!int.TryParse(iter.Current.GetAttribute("Value", ""), out ccdCoolerSetPoint))
                    {
                        ccdCoolerSetPoint = -20;
                    }
                    ConfigSettings.CameraModeSettings.CCDCoolerSetPoint = ccdCoolerSetPoint;
                }

                #region PVCAM post processing features...

                iter = xpathNav.Select("/Config/IsDynamicDarkCorrection");
                if (iter.MoveNext())
                {
                    bool flag;
                    string ddfcValue = iter.Current.GetAttribute("Value", "");
                    if (!Boolean.TryParse(ddfcValue, out flag))
                    {
                        flag = false;
                    }
                    ConfigSettings.CameraModeSettings.IsDynamicDarkCorrection = flag;
                }
                // PVCAM post processing features
                iter = xpathNav.Select("/Config/IsEnhanceDynamicRange");
                if (iter.MoveNext())
                {
                    bool flag;
                    string edrValue = iter.Current.GetAttribute("Value", "");
                    if (!Boolean.TryParse(edrValue, out flag))
                    {
                        flag = false;
                    }
                    ConfigSettings.CameraModeSettings.IsEnhanceDynamicRange = flag;
                }
                iter = xpathNav.Select("/Config/IsDefectivePixelCorrection");
                if (iter.MoveNext())
                {
                    bool flag;
                    string dpcValue = iter.Current.GetAttribute("Value", "");
                    if (!Boolean.TryParse(dpcValue, out flag))
                    {
                        flag = false;
                    }
                    ConfigSettings.CameraModeSettings.IsDefectivePixelCorrection = flag;
                }

                #endregion

                #endregion

                iter = xpathNav.Select("/Config/IsChannelALightShadeFix");
                if (iter.MoveNext())
                {
                    bool lightShadeFixFlag;
                    string lightShadeFixValue = iter.Current.GetAttribute("Value", "");
                    if (!Boolean.TryParse(lightShadeFixValue, out lightShadeFixFlag))
                    {
                        lightShadeFixFlag = false;
                    }
                    configSettings.IsChannelALightShadeFix = lightShadeFixFlag;
                }

                iter = xpathNav.Select("/Config/IsChannelBLightShadeFix");
                if (iter.MoveNext())
                {
                    bool lightShadeFixFlag;
                    string lightShadeFixValue = iter.Current.GetAttribute("Value", "");
                    if (!Boolean.TryParse(lightShadeFixValue, out lightShadeFixFlag))
                    {
                        lightShadeFixFlag = false;
                    }
                    configSettings.IsChannelBLightShadeFix = lightShadeFixFlag;
                }

                iter = xpathNav.Select("/Config/IsChannelCLightShadeFix");
                if (iter.MoveNext())
                {
                    bool lightShadeFixFlag;
                    string lightShadeFixValue = iter.Current.GetAttribute("Value", "");
                    if (!Boolean.TryParse(lightShadeFixValue, out lightShadeFixFlag))
                    {
                        lightShadeFixFlag = false;
                    }
                    configSettings.IsChannelCLightShadeFix = lightShadeFixFlag;
                }

                iter = xpathNav.Select("/Config/IsChannelDLightShadeFix");
                if (iter.MoveNext())
                {
                    bool lightShadeFixFlag;
                    string lightShadeFixValue = iter.Current.GetAttribute("Value", "");
                    if (!Boolean.TryParse(lightShadeFixValue, out lightShadeFixFlag))
                    {
                        lightShadeFixFlag = false;
                    }
                    configSettings.IsChannelDLightShadeFix = lightShadeFixFlag;
                }

                // Replaced with "Fluorescence2LinesAvgScan" and "Phosphor2LinesAvgScan"
                // to be able to apply the correction only for Fluorescence or Phosphor scan or both.
                /*iter = xpathNav.Select("/Config/IsUnidirectionalScan");
                if (iter.MoveNext())
                {
                    bool unidirectionalScan;
                    string unidirectionalScanStr = iter.Current.GetAttribute("Value", "");
                    if (!Boolean.TryParse(unidirectionalScanStr, out unidirectionalScan))
                    {
                        unidirectionalScan = false;
                    }
                    configSettings.IsUnidirectionalScan = unidirectionalScan;
                }*/

                iter = xpathNav.Select("/Config/Fluorescence2LinesAvgScan");
                if (iter.MoveNext())
                {
                    bool bFl2LinesAvgScan = false;
                    string strFl2LinesAvgScan = iter.Current.GetAttribute("Value", "");
                    if (!Boolean.TryParse(strFl2LinesAvgScan, out bFl2LinesAvgScan))
                    {
                        bFl2LinesAvgScan = false;
                    }
                    configSettings.IsFluorescence2LinesAvgScan = bFl2LinesAvgScan;
                }

                iter = xpathNav.Select("/Config/Phosphor2LinesAvgScan");
                if (iter.MoveNext())
                {
                    bool bPh2LinesAvgScan = false;
                    string strPh2LinesAvgScan = iter.Current.GetAttribute("Value", "");
                    if (!Boolean.TryParse(strPh2LinesAvgScan, out bPh2LinesAvgScan))
                    {
                        bPh2LinesAvgScan = false;
                    }
                    configSettings.IsPhosphor2LinesAvgScan = bPh2LinesAvgScan;
                }

                iter = xpathNav.Select("/Config/BinFactors/BinFactor");
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;
                    BinningFactorType binningItem = new BinningFactorType();
                    binningItem.Position = int.Parse(nav.GetAttribute("Position", ""));
                    binningItem.HorizontalBins = int.Parse(nav.GetAttribute("HorizontalBins", ""));
                    binningItem.VerticalBins = int.Parse(nav.GetAttribute("VerticalBins", ""));
                    binningItem.DisplayName = nav.GetAttribute("DisplayName", "");
                    configSettings.BinningFactorOptions.Add(binningItem);
                }

                iter = xpathNav.Select("/Config/PvCamSensitivities/Sensitivity");
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;
                    PvCamSensitivity sensitivity = new PvCamSensitivity();
                    sensitivity.Position = int.Parse(nav.GetAttribute("Position", ""));
                    int binningMode = int.Parse(nav.GetAttribute("BinningMode", ""));
                    sensitivity.HorizontalBin = binningMode;
                    sensitivity.VerticalBin = binningMode;
                    sensitivity.DisplayName = nav.GetAttribute("DisplayName", "");
                    configSettings.PvCamSensitivityOptions.Add(sensitivity);
                }

                iter = xpathNav.Select("/Config/Gains/Gain");
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;
                    GainType gainItem = new GainType();
                    gainItem.Position = int.Parse(nav.GetAttribute("Position", ""));
                    gainItem.Value = int.Parse(nav.GetAttribute("Value", ""));
                    gainItem.DisplayName = nav.GetAttribute("DisplayName", "");
                    configSettings.GainOptions.Add(gainItem);
                }

                iter = xpathNav.Select("/Config/RgbLedIntensities/RgbLedIntensity");
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;
                    RgbLedIntensity rgbLedIntensity = new RgbLedIntensity();
                    rgbLedIntensity.Position = int.Parse(nav.GetAttribute("Position", ""));
                    rgbLedIntensity.Intensity = int.Parse(nav.GetAttribute("Intensity", ""));
                    rgbLedIntensity.DisplayName = nav.GetAttribute("DisplayName", "");
                    configSettings.RgbLedIntensities.Add(rgbLedIntensity);
                }

                #region Chemi settings...

                iter = xpathNav.Select("/Config/ChemiSettings");
                if (iter != null)
                {
                    while (iter.MoveNext())
                    {
                        XPathNavigator nav = iter.Current;
                        var selectedNode = nav.SelectSingleNode("NumFrames");
                        if (selectedNode != null)
                        {
                            ConfigSettings.CameraModeSettings.ChemiSettings.NumFrames = int.Parse(selectedNode.Value);
                        }
                        selectedNode = nav.SelectSingleNode("ReadoutSpeed");
                        if (selectedNode != null)
                        {
                            ConfigSettings.CameraModeSettings.ChemiSettings.ReadoutSpeed = int.Parse(selectedNode.Value);
                        }
                        selectedNode = nav.SelectSingleNode("Gain");
                        if (selectedNode != null)
                        {
                            ConfigSettings.CameraModeSettings.ChemiSettings.Gain = int.Parse(selectedNode.Value);
                        }
                        selectedNode = nav.SelectSingleNode("MinExposure");
                        if (selectedNode != null)
                        {
                            double minExposure;
                            if (!Double.TryParse(selectedNode.Value, out minExposure))
                            {
                                minExposure = 1.0d;
                            }
                            ConfigSettings.CameraModeSettings.ChemiSettings.MinExposure = minExposure;
                        }
                        selectedNode = nav.SelectSingleNode("MaxExposure");
                        if (selectedNode != null)
                        {
                            double maxExposure;
                            if (!Double.TryParse(selectedNode.Value, out maxExposure))
                            {
                                maxExposure = 5400.0d;
                            }
                            ConfigSettings.CameraModeSettings.ChemiSettings.MaxExposure = maxExposure;
                        }
                        selectedNode = nav.SelectSingleNode("MarkerRedChExposure");
                        if (selectedNode != null)
                        {
                            double markerExposure;
                            if (!Double.TryParse(selectedNode.Value, out markerExposure))
                            {
                                markerExposure = 0.110d;
                            }
                            ConfigSettings.CameraModeSettings.ChemiSettings.MarkerRedChExposure = markerExposure;
                        }
                        selectedNode = nav.SelectSingleNode("MarkerGreenChExposure");
                        if (selectedNode != null)
                        {
                            double markerExposure;
                            if (!Double.TryParse(selectedNode.Value, out markerExposure))
                            {
                                markerExposure = 0.090d;
                            }
                            ConfigSettings.CameraModeSettings.ChemiSettings.MarkerGreenChExposure = markerExposure;
                        }
                        selectedNode = nav.SelectSingleNode("MarkerBlueChExposure");
                        if (selectedNode != null)
                        {
                            double markerExposure;
                            if (!Double.TryParse(selectedNode.Value, out markerExposure))
                            {
                                markerExposure = 0.065d;
                            }
                            ConfigSettings.CameraModeSettings.ChemiSettings.MarkerBlueChExposure = markerExposure;
                        }
                        selectedNode = nav.SelectSingleNode("PvCamScalingThreshold");
                        if (selectedNode != null)
                        {
                            int scalingThreshold = 0;
                            if (!int.TryParse(selectedNode.Value, out scalingThreshold))
                            {
                                scalingThreshold = 45000;
                            }
                            ConfigSettings.CameraModeSettings.ChemiSettings.PvCamScalingThreshold = scalingThreshold;
                        }
                        selectedNode = nav.SelectSingleNode("AutoExposureUpperCeiling");
                        if (selectedNode != null)
                        {
                            int aeUppderCeiling;
                            if (!int.TryParse(selectedNode.Value, out aeUppderCeiling))
                            {
                                aeUppderCeiling = 35000;
                            }
                            ConfigSettings.CameraModeSettings.ChemiSettings.AutoExposureUpperCeiling = aeUppderCeiling;
                        }
                        selectedNode = nav.SelectSingleNode("AEUnderExposure");
                        if (selectedNode != null)
                        {
                            int underExposure;
                            if (!int.TryParse(selectedNode.Value, out underExposure))
                            {
                                underExposure = 25000;
                            }
                            ConfigSettings.CameraModeSettings.ChemiSettings.AEUnderExposure = underExposure;
                        }
                        selectedNode = nav.SelectSingleNode("AEOverExposure");
                        if (selectedNode != null)
                        {
                            int overExposure;
                            if (!int.TryParse(selectedNode.Value, out overExposure))
                            {
                                overExposure = 65535;
                            }
                            ConfigSettings.CameraModeSettings.ChemiSettings.AEOverExposure = overExposure;
                        }
                        selectedNode = nav.SelectSingleNode("AEWideDynamicRange");
                        if (selectedNode != null)
                        {
                            int wideDynamic;
                            if (!int.TryParse(selectedNode.Value, out wideDynamic))
                            {
                                wideDynamic = 62000;
                            }
                            ConfigSettings.CameraModeSettings.ChemiSettings.AEWideDynamicRange = wideDynamic;
                        }
                    }
                }

                #endregion

                #region === Live Mode Settings ===

                iter = xpathNav.Select("/Config/LiveModeSettings");
                if (iter != null)
                {
                    while (iter.MoveNext())
                    {
                        XPathNavigator nav = iter.Current;
                        var selectedNode = nav.SelectSingleNode("BinningMode");
                        if (selectedNode != null)
                        {
                            ConfigSettings.CameraModeSettings.LiveModeSettings.BinningMode = int.Parse(selectedNode.Value);
                        }
                        selectedNode = nav.SelectSingleNode("ExposureTime");
                        if (selectedNode != null)
                        {
                            double exposureTime;
                            if (!Double.TryParse(selectedNode.Value, out exposureTime))
                            {
                                exposureTime = 0.05d;
                            }
                            ConfigSettings.CameraModeSettings.LiveModeSettings.ExposureTime = exposureTime;
                        }
                        selectedNode = nav.SelectSingleNode("ReadoutSpeed");
                        if (selectedNode != null)
                        {
                            ConfigSettings.CameraModeSettings.LiveModeSettings.ReadoutSpeed = int.Parse(selectedNode.Value);
                        }
                    }
                }

                #endregion

                #region Visible imaging settings...

                iter = xpathNav.Select("/Config/VisibleImagingSettings");
                if (iter != null)
                {
                    while (iter.MoveNext())
                    {
                        XPathNavigator nav = iter.Current;
                        var selectedNode = nav.SelectSingleNode("Bin");
                        if (selectedNode != null)
                        {
                            ConfigSettings.CameraModeSettings.VisibleImagingSettings.Bin = int.Parse(selectedNode.Value);
                        }
                        selectedNode = nav.SelectSingleNode("ReadoutSpeed");
                        if (selectedNode != null)
                        {
                            ConfigSettings.CameraModeSettings.VisibleImagingSettings.ReadoutSpeed = int.Parse(selectedNode.Value);
                        }
                        selectedNode = nav.SelectSingleNode("Gain");
                        if (selectedNode != null)
                        {
                            ConfigSettings.CameraModeSettings.VisibleImagingSettings.Gain = int.Parse(selectedNode.Value);
                        }
                        selectedNode = nav.SelectSingleNode("MinExposure");
                        if (selectedNode != null)
                        {
                            double minExposure;
                            if (!Double.TryParse(selectedNode.Value, out minExposure))
                            {
                                minExposure = 0.001d;
                            }
                            ConfigSettings.CameraModeSettings.VisibleImagingSettings.MinExposure = minExposure;
                        }
                        selectedNode = nav.SelectSingleNode("MaxExposure");
                        if (selectedNode != null)
                        {
                            double maxExposure;
                            if (!Double.TryParse(selectedNode.Value, out maxExposure))
                            {
                                maxExposure = 60.0d;
                            }
                            ConfigSettings.CameraModeSettings.VisibleImagingSettings.MaxExposure = maxExposure;
                        }
                        selectedNode = nav.SelectSingleNode("AutoExposureUpperCeiling");
                        if (selectedNode != null)
                        {
                            int aeUppderCeiling;
                            if (!int.TryParse(selectedNode.Value, out aeUppderCeiling))
                            {
                                aeUppderCeiling = 55000;
                            }
                            ConfigSettings.CameraModeSettings.VisibleImagingSettings.AutoExposureUpperCeiling = aeUppderCeiling;
                        }
                        selectedNode = nav.SelectSingleNode("ScalingThreshold");
                        if (selectedNode != null)
                        {
                            int scalingThreshold;
                            if (!int.TryParse(selectedNode.Value, out scalingThreshold))
                            {
                                scalingThreshold = 55000;
                            }
                            ConfigSettings.CameraModeSettings.VisibleImagingSettings.ScalingThreshold = scalingThreshold;
                        }
                    }
                }

                #endregion

                iter = xpathNav.Select("/Config/Resolutions/Resolution");
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;
                    ResolutionType resItem = new ResolutionType();
                    resItem.Position = int.Parse(nav.GetAttribute("Position", ""));
                    resItem.Value = int.Parse(nav.GetAttribute("Value", ""));
                    resItem.DisplayName = nav.GetAttribute("DisplayName", "");
                    configSettings.ResolutionOptions.Add(resItem);
                }

                iter = xpathNav.Select("/Config/Qualities/Quality");
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;
                    QualityType qualityItem = new QualityType();
                    qualityItem.Position = int.Parse(nav.GetAttribute("Position", ""));
                    qualityItem.Value = int.Parse(nav.GetAttribute("Value", ""));
                    qualityItem.DisplayName = nav.GetAttribute("DisplayName", "");
                    configSettings.QualityOptions.Add(qualityItem);
                }

                iter = xpathNav.Select("/Config/ScanSpeeds/ScanSpeed");
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;
                    ScanSpeedType scanSpeedItem = new ScanSpeedType();
                    scanSpeedItem.Position = int.Parse(nav.GetAttribute("Position", ""));
                    scanSpeedItem.Value = int.Parse(nav.GetAttribute("Value", ""));
                    scanSpeedItem.DisplayName = nav.GetAttribute("DisplayName", "");
                    configSettings.ScanSpeedOptions.Add(scanSpeedItem);
                }

                iter = xpathNav.Select("/Config/MotorParameters/MotorParameter");
                string MotorParameterName = null;
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;
                    MotorParameterName = nav.GetAttribute("Name", "");
                    switch (MotorParameterName)
                    {
                        case "XMaxValue": configSettings.XMaxValue = int.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "YMaxValue": configSettings.YMaxValue = int.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "ZMaxValue": configSettings.ZMaxValue = int.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "WMaxValue": ConfigSettings.WMaxValue = int.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "WMinValue": ConfigSettings.WMinValue = int.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "WMediumValue": ConfigSettings.WMediumValue = int.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "XMotorSubdivision": configSettings.XMotorSubdivision = double.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "YMotorSubdivision": configSettings.YMotorSubdivision = double.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "ZMotorSubdivision": configSettings.ZMotorSubdivision = double.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "WMotorSubdivision": ConfigSettings.WMotorSubdivision = double.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "XEncoderSubdivision": ConfigSettings.XEncoderSubdivision = double.Parse(nav.GetAttribute("Value", ""));
                            break;
                    }
                }

                iter = xpathNav.Select("/Config/MotorPolarities/Polarities");
                string palarityName;
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;
                    palarityName = nav.GetAttribute("Name", "");
                    switch (palarityName)
                    {
                        case "DirX":
                            configSettings.MotionPolarityDirX = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "EnableX":
                            configSettings.MotionPolarityEnableX = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "ClockX":
                            configSettings.MotionPolarityClkX = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "HomeX":
                            configSettings.MotionPolarityHomeX = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "FwdLimitX":
                            configSettings.MotionPolarityFwdX = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "BwdLimitX":
                            configSettings.MotionPolarityBwdX = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "DirY":
                            configSettings.MotionPolarityDirY = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "EnableY":
                            configSettings.MotionPolarityEnableY = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "ClockY":
                            configSettings.MotionPolarityClkY = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "HomeY":
                            configSettings.MotionPolarityHomeY = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "FwdLimitY":
                            configSettings.MotionPolarityFwdY = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "BwdLimitY":
                            configSettings.MotionPolarityBwdY = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "DirZ":
                            configSettings.MotionPolarityDirZ = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "EnableZ":
                            configSettings.MotionPolarityEnableZ = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "ClockZ":
                            configSettings.MotionPolarityClkZ = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "HomeZ":
                            configSettings.MotionPolarityHomeZ = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "FwdLimitZ":
                            configSettings.MotionPolarityFwdZ = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "BwdLimitZ":
                            configSettings.MotionPolarityBwdZ = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "DirW":
                            configSettings.MotionPolarityDirW = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "EnableW":
                            configSettings.MotionPolarityEnableW = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "ClockW":
                            configSettings.MotionPolarityClkW = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "HomeW":
                            configSettings.MotionPolarityHomeW = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "FwdLimitW":
                            configSettings.MotionPolarityFwdW = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                        case "BwdLimitW":
                            configSettings.MotionPolarityBwdW = bool.Parse(nav.GetAttribute("Value", ""));
                            break;
                    }
                }

                iter = xpathNav.Select("/Config/APDGains/APDGain");
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;
                    APDGainType aPDGainItem = new APDGainType();
                    aPDGainItem.Position = int.Parse(nav.GetAttribute("Position", ""));
                    aPDGainItem.Value = int.Parse(nav.GetAttribute("Value", ""));
                    aPDGainItem.DisplayName = nav.GetAttribute("DisplayName", "");
                    configSettings.APDGains.Add(aPDGainItem);
                }

                //iter = xpathNav.Select("/Config/Focus/FocusOptions");
                //while (iter.MoveNext())
                //{
                //    XPathNavigator nav = iter.Current;
                //    FocusType FocusItem = new FocusType();
                //    FocusItem.Position = int.Parse(nav.GetAttribute("Position", ""));
                //    FocusItem.Value = double.Parse(nav.GetAttribute("Value", ""));
                //    FocusItem.DisplayName = nav.GetAttribute("DisplayName", "");
                //    configSettings.Focus.Add(FocusItem);
                //}

                iter = xpathNav.Select("/Config/APDPgas/APDPga");
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;
                    APDPgaType APDPgaItem = new APDPgaType();
                    APDPgaItem.Position = int.Parse(nav.GetAttribute("Position", ""));
                    APDPgaItem.Value = int.Parse(nav.GetAttribute("Value", ""));
                    APDPgaItem.DisplayName = nav.GetAttribute("DisplayName", "");
                    configSettings.APDPgas.Add(APDPgaItem);
                }

                iter = xpathNav.Select("/Config/MotorSettings/MotorSetting");
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;
                    MotorSettingsType motorSetting = new MotorSettingsType();
                    MotorType motorType;
                    Enum.TryParse(nav.GetAttribute("Motor", ""), out motorType); //string to enum
                    double position = 0;
                    double.TryParse(nav.GetAttribute("Position", ""), out position);
                    double speed = 0;
                    double.TryParse(nav.GetAttribute("Speed", ""), out speed);
                    double acc = 0;
                    double.TryParse(nav.GetAttribute("Accel", ""), out acc);
                    double dcc = 0;
                    double.TryParse(nav.GetAttribute("Dccel", ""), out dcc);
                    motorSetting.MotorType = motorType;
                    motorSetting.Position = position;
                    motorSetting.Speed = speed;
                    motorSetting.Accel = acc;
                    motorSetting.Dccel = dcc;
                    configSettings.MotorSettings.Add(motorSetting);
                }
                iter = xpathNav.Select("/Config/MotorSettings/ScanningSetting");
                while (iter.MoveNext())
                {
                    var nav = iter.Current;
                    int delay = 0;
                    string name = nav.GetAttribute("Name", "");
                    if(name== "XMotorTurnDelay")
                    {
                        int.TryParse(nav.GetAttribute("Value", ""), out delay);
                        configSettings.XMotionTurnDelay = delay;
                    }
                    else if(name == "XMotionExtraMoveLength")
                    {
                        configSettings.XMotionExtraMoveLength = int.Parse(nav.GetAttribute("Value", ""));
                    }
                }
                iter = xpathNav.Select("/Config/MotorSettings/LaunchSetting");
                while (iter.MoveNext())
                {
                    var nav = iter.Current;
                    bool homeMotion;
                    string name = nav.GetAttribute("Name", "");
                    if (name == "HomeMotionsAtLaunchTime")
                    {
                        bool.TryParse(nav.GetAttribute("Value", ""), out homeMotion);
                        configSettings.HomeMotionsAtLaunchTime = homeMotion;
                    }
                }
                
                iter = xpathNav.Select("/Config/LaserPowers/LaserPower");
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;
                    string strLaserType = nav.GetAttribute("Laser", "");
                    int laserIntensity = int.Parse(nav.GetAttribute("Intensity", ""));
                    int laserMaxIntensity = int.Parse(nav.GetAttribute("MaxIntensity", ""));
                    switch (strLaserType.ToUpper())
                    {
                        case "A":
                            configSettings.LaserAIntensity = laserIntensity;
                            configSettings.LaserAMaxIntensity = laserMaxIntensity;
                            break;
                        case "B":
                            configSettings.LaserBIntensity = laserIntensity;
                            configSettings.LaserBMaxIntensity = laserMaxIntensity;
                            break;
                        case "C":
                            configSettings.LaserCIntensity = laserIntensity;
                            configSettings.LaserCMaxIntensity = laserMaxIntensity;
                            break;
                        case "D":
                            configSettings.LaserDIntensity = laserIntensity;
                            configSettings.LaserDMaxIntensity = laserMaxIntensity;
                            break;
                    }
                }
                iter = xpathNav.Select("/Config/ScanSettings/ScannSetting");
                while (iter.MoveNext())
                {
                    var nav = iter.Current;
                    bool dynamicBitAt;
                    int delay = 0;
                    string name = nav.GetAttribute("Name", "");
                    if (name == "ScanDynamicBitAt")
                    {
                        bool.TryParse(nav.GetAttribute("Value", ""), out dynamicBitAt);
                        configSettings.ScanDynamicBitAt = dynamicBitAt;
                    }
                    bool ImageOffsetProcessing;
                    if (name == "ImageOffsetProcessing")
                    {
                        bool.TryParse(nav.GetAttribute("Value", ""), out ImageOffsetProcessing);
                        configSettings.ImageOffsetProcessing = ImageOffsetProcessing;
                    }
                    bool PixelOffsetProcessing;
                    if (name == "PixelOffsetProcessing")
                    {
                        bool.TryParse(nav.GetAttribute("Value", ""), out PixelOffsetProcessing);
                        configSettings.PixelOffsetProcessing = PixelOffsetProcessing;
                    }
                    bool ycompensationBitAt;
                    if (name == "YCompenSationBitAt")
                    {
                        bool.TryParse(nav.GetAttribute("Value", ""), out ycompensationBitAt);
                        configSettings.YCompenSationBitAt = ycompensationBitAt;
                    }
                    if (name == "YCompenOffset")
                    {
                        configSettings.YCompenOffset = Convert.ToDouble(nav.GetAttribute("Value", ""));
                    }
                    if (name == "XOddNumberedLine")
                    {
                        int.TryParse(nav.GetAttribute("Value", ""), out delay);
                        configSettings.XOddNumberedLine = delay;
                    }
                    if (name == "XEvenNumberedLine")
                    {
                        int.TryParse(nav.GetAttribute("Value", ""), out delay);
                        configSettings.XEvenNumberedLine = delay;
                    }
                    if (name == "YOddNumberedLine")
                    {
                        int.TryParse(nav.GetAttribute("Value", ""), out delay);
                        configSettings.YOddNumberedLine = delay;
                    }
                    if (name == "YEvenNumberedLine")
                    {
                        int.TryParse(nav.GetAttribute("Value", ""), out delay);
                        configSettings.YEvenNumberedLine = delay;
                    }
                    bool PhosphorModuleProcessing;
                    if (name == "PhosphorModuleProcessing")
                    {
                        bool.TryParse(nav.GetAttribute("Value", ""), out PhosphorModuleProcessing);
                        configSettings.PhosphorModuleProcessing = PhosphorModuleProcessing;
                    }
                    bool AllModuleProcessing;
                    if (name == "AllModuleProcessing")
                    {
                        bool.TryParse(nav.GetAttribute("Value", ""), out AllModuleProcessing);
                        configSettings.AllModuleProcessing = AllModuleProcessing;
                    }
                }
                iter = xpathNav.Select("/Config/RadiatorTemperatures/RadiatorTemperature");
                while (iter.MoveNext())
                {
                    var nav = iter.Current;
                    int delay = 0;
                    string name = nav.GetAttribute("Name", "");
                    if (name == "RadiatorL")
                    {
                        int.TryParse(nav.GetAttribute("Value", ""), out delay);
                        configSettings.RadiatorTemperatureL = delay;
                    }
                    if (name == "RadiatorR1")
                    {
                        int.TryParse(nav.GetAttribute("Value", ""), out delay);
                        configSettings.RadiatorTemperatureR1 = delay;
                    }
                    if (name == "RadiatorR2")
                    {
                        int.TryParse(nav.GetAttribute("Value", ""), out delay);
                        configSettings.RadiatorTemperatureR2 = delay;
                    }
                }
                iter = xpathNav.Select("/Config/PhosphorLaserModules/LaserModule");
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;
                    PhosphorLaserModules PhosphorItem = new PhosphorLaserModules();
                    PhosphorItem.DisplayName = nav.GetAttribute("LaserNumber", "");
                    configSettings.PhosphorLaserModules.Add(PhosphorItem);
                }

                iter = xpathNav.Select("/Config/ShellFanModules/ShellFanModule");
                while (iter.MoveNext())
                {
                    var nav = iter.Current;
                    double delay = 0;
                    string name = nav.GetAttribute("ShellFan", "");
                    if (name == "InternalLowTemperature")
                    {
                        double.TryParse(nav.GetAttribute("Value", ""), out delay);
                        configSettings.InternalLowTemperature = delay;
                    }
                    if (name == "InternalModerateTemperature")
                    {
                        double.TryParse(nav.GetAttribute("Value", ""), out delay);
                        configSettings.InternalModerateTemperature = delay;
                    }
                    if (name == "InternalHighTemperature")
                    {
                        double.TryParse(nav.GetAttribute("Value", ""), out delay);
                        configSettings.InternalHighTemperature = delay;
                    }
                    if (name == "ModuleLowTemperature")
                    {
                        double.TryParse(nav.GetAttribute("Value", ""), out delay);
                        configSettings.ModuleLowTemperature = delay;
                    }
                    if (name == "ModuleModerateTemperature")
                    {
                        double.TryParse(nav.GetAttribute("Value", ""), out delay);
                        configSettings.ModuleModerateTemperature = delay;
                    }
                    if (name == "ModuleHighTemperature")
                    {
                        double.TryParse(nav.GetAttribute("Value", ""), out delay);
                        configSettings.ModuleHighTemperature = delay;
                    }
                }

                iter = xpathNav.Select("/Config/LasersIntensities/LaserIntensity");
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;
                    LaserSettingsType laserSetting = new LaserSettingsType();
                    string strLaserType = nav.GetAttribute("Laser", "");
                    LaserType laserType;
                    Enum.TryParse(strLaserType, out laserType); //string to enum

                    laserSetting.LaserType = laserType;

                    string strLaserInt = nav.GetAttribute("Intensities", "");
                    string[] arrLasersInt = strLaserInt.Split(' ');
                    int laserInt = 0;
                    for (int i = 0; i < arrLasersInt.Length; i++)
                    {
                        int.TryParse(arrLasersInt[i], out laserInt);
                        laserSetting.Intensities.Add(laserInt);
                    }

                    laserSetting.MaxIntensity = int.Parse(nav.GetAttribute("MaxIntensity", ""));

                    configSettings.LasersIntensitySettings.Add(laserSetting);
                }

                iter = xpathNav.Select("/Config/Dyes/Dye");
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;
                    string strPosition = nav.GetAttribute("Position", "");
                    int position = 0;
                    int.TryParse(strPosition, out position);

                    string displayName = nav.GetAttribute("DisplayName", "");

                    string strLaserType = nav.GetAttribute("Laser", "");
                    LaserType laserType;
                    Enum.TryParse(strLaserType, out laserType); //string to enum

                    string strWaveLength = nav.GetAttribute("WaveLength", "");
                    //int waveLength = 0;
                    //int.TryParse(strWaveLength, out waveLength);

                    DyeType dyeType = new DyeType();
                    dyeType.Position = position;
                    dyeType.DisplayName = displayName;
                    dyeType.LaserType = laserType;
                    //dyeType.WaveLength = waveLength;
                    dyeType.WaveLength = strWaveLength;
                    configSettings.DyeOptions.Add(dyeType);
                }

                /*
                iter = xpathNav.Select("/Config/Methods/Method");
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;

                    string displayName = nav.GetAttribute("DisplayName", "");

                    string strSampleType = nav.GetAttribute("SampleType", "");
                    int sampleType;
                    int.TryParse(strSampleType, out sampleType);

                    string strPixelSize = nav.GetAttribute("PixelSize", "");
                    int pixelSize = 0;
                    int.TryParse(strPixelSize, out pixelSize);

                    string strScanspeed = nav.GetAttribute("ScanSpeed", "");
                    int scanSpeed = 0;
                    int.TryParse(strScanspeed, out scanSpeed);

                    AppMethod appMethod = new AppMethod();
                    appMethod.Name = displayName;
                    appMethod.Sampletype = sampleType;
                    appMethod.Pixelsize = pixelSize;
                    appMethod.Scanspeed = scanSpeed;

                    if (nav.HasChildren)
                    {
                        nav.MoveToFirstChild();
                        do
                        {
                            string strDyeType = nav.GetAttribute("DyeType", "");
                            int dyeType = 0;
                            int.TryParse(strDyeType, out dyeType);
                            string strSignalInt = nav.GetAttribute("SignalIntensity", "");
                            int signalInt = 0;
                            int.TryParse(strSignalInt, out signalInt);
                            string strColorChan = nav.GetAttribute("ColorChannel", "");
                            ImageChannelType colorChannel;
                            Enum.TryParse(strColorChan, out colorChannel); //string to enum

                            AppDyeData dyeData = new AppDyeData();
                            dyeData.DyeType = dyeType;
                            dyeData.SignalIntensity = signalInt;
                            dyeData.ColorChannel = colorChannel;

                            appMethod.Dyes.Add(dyeData);

                        } while (nav.MoveToNext());
                    }

                    configSettings.AppMethods.Add(appMethod);
                }*/

                // 20180221: Moved from SysSettings.xml
                // Read signal options
                iter = xpathNav.Select("/Config/Signals/Signal");
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;

                    string strLaserType = nav.GetAttribute("LaserType", "");
                    strLaserType = "Laser" + strLaserType;
                    LaserType laserType = (LaserType)Enum.Parse(typeof(LaserType), strLaserType);

                    if (nav.HasChildren)
                    {
                        if (nav.MoveToFirstChild())
                        {
                            do
                            {
                                Signal signal = new Signal();
                                signal.Position = int.Parse(nav.GetAttribute("Position", ""));
                                signal.LaserIntensity = int.Parse(nav.GetAttribute("LaserIntensity", ""));
                                //LaserIntensity will be overrided with the laser intensity read from the scanner.
                                //Laser intensity in mW is needed for SmartScan calculation so we're now saving the laser power in mW (previously not needed).
                                signal.LaserIntInmW = signal.LaserIntensity;
                                signal.ApdGain = int.Parse(nav.GetAttribute("ApdGain", ""));
                                signal.ApdPga = int.Parse(nav.GetAttribute("ApdPga", ""));
                                signal.LaserType = laserType;

                                switch (laserType)
                                {
                                    case LaserType.LaserA:
                                        configSettings.LaserASignalOptions.Add(signal);
                                        break;
                                    case LaserType.LaserB:
                                        configSettings.LaserBSignalOptions.Add(signal);
                                        break;
                                    case LaserType.LaserC:
                                        configSettings.LaserCSignalOptions.Add(signal);
                                        break;
                                    case LaserType.LaserD:
                                        configSettings.LaserDSignalOptions.Add(signal);
                                        break;
                                }
                            }
                            while (nav.MoveToNext());
                            nav.MoveToParent();
                        }
                    }
                }

                // 20180221: Moved from SysSettings.xml
                // Read signal options
                iter = xpathNav.Select("/Config/PhosphorSignals/PhosphorSignal");
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;

                    string strLaserType = nav.GetAttribute("LaserType", "");
                    strLaserType = "Laser" + strLaserType;
                    LaserType laserType = (LaserType)Enum.Parse(typeof(LaserType), strLaserType);

                    if (nav.HasChildren)
                    {
                        if (nav.MoveToFirstChild())
                        {
                            do
                            {
                                Signal signal = new Signal();
                                signal.Position = int.Parse(nav.GetAttribute("Position", ""));
                                signal.LaserIntensity = int.Parse(nav.GetAttribute("LaserIntensity", ""));
                                signal.ApdGain = int.Parse(nav.GetAttribute("ApdGain", ""));
                                signal.ApdPga = int.Parse(nav.GetAttribute("ApdPga", ""));
                                signal.LaserType = laserType;

                                switch (laserType)
                                {
                                    //case LaserType.LaserA:
                                    //    configSettings.LaserASignalOptions.Add(signal);
                                    //    break;
                                    //case LaserType.LaserB:
                                    //    configSettings.LaserBSignalOptions.Add(signal);
                                    //    break;
                                    case LaserType.LaserC:
                                        configSettings.PhosphorCSignalOptions.Add(signal);
                                        break;
                                    case LaserType.LaserD:
                                        configSettings.PhosphorDSignalOptions.Add(signal);
                                        break;
                                }
                            }
                            while (nav.MoveToNext());
                            nav.MoveToParent();
                        }
                    }
                }

                #region === Auto scan Settings ===

                iter = xpathNav.Select("/Config/AutoScan/Resolution");
                if (iter.MoveNext())
                {
                    ConfigSettings.AutoScanSettings.Resolution = int.Parse(iter.Current.GetAttribute("Value", ""));
                }
                iter = xpathNav.Select("/Config/AutoScan/OptimalVal");
                if (iter.MoveNext())
                {
                    ConfigSettings.AutoScanSettings.OptimalVal = int.Parse(iter.Current.GetAttribute("Value", ""));
                }
                iter = xpathNav.Select("/Config/AutoScan/OptimalDelta");
                if (iter.MoveNext())
                {
                    ConfigSettings.AutoScanSettings.OptimalDelta = double.Parse(iter.Current.GetAttribute("Value", ""));
                }
                iter = xpathNav.Select("/Config/AutoScan/Ceiling");
                if (iter.MoveNext())
                {
                    ConfigSettings.AutoScanSettings.Ceiling = int.Parse(iter.Current.GetAttribute("Value", ""));
                }
                iter = xpathNav.Select("/Config/AutoScan/Floor");
                if (iter.MoveNext())
                {
                    ConfigSettings.AutoScanSettings.Floor = int.Parse(iter.Current.GetAttribute("Value", ""));
                }
                iter = xpathNav.Select("/Config/AutoScan/Alpha488");
                if (iter.MoveNext())
                {
                    ConfigSettings.AutoScanSettings.Alpha488 = double.Parse(iter.Current.GetAttribute("Value", ""));
                }
                iter = xpathNav.Select("/Config/AutoScan/LaserASignalLevel");
                if (iter.MoveNext())
                {
                    int signalLevelPos = int.Parse(iter.Current.GetAttribute("Position", ""));
                    if (configSettings.LaserASignalOptions != null && signalLevelPos < configSettings.LaserASignalOptions.Count - 1)
                    {
                        ConfigSettings.AutoScanSettings.LaserASignalLevel = configSettings.LaserASignalOptions[signalLevelPos - 1];
                    }
                }
                iter = xpathNav.Select("/Config/AutoScan/LaserBSignalLevel");
                if (iter.MoveNext())
                {
                    int signalLevelPos = int.Parse(iter.Current.GetAttribute("Position", ""));
                    if (configSettings.LaserBSignalOptions != null && signalLevelPos < configSettings.LaserBSignalOptions.Count - 1)
                    {
                        ConfigSettings.AutoScanSettings.LaserBSignalLevel = configSettings.LaserBSignalOptions[signalLevelPos - 1];
                    }
                }
                iter = xpathNav.Select("/Config/AutoScan/LaserCSignalLevel");
                if (iter.MoveNext())
                {
                    int signalLevelPos = int.Parse(iter.Current.GetAttribute("Position", ""));
                    if (configSettings.LaserCSignalOptions != null && signalLevelPos < configSettings.LaserCSignalOptions.Count - 1)
                    {
                        ConfigSettings.AutoScanSettings.LaserCSignalLevel = configSettings.LaserCSignalOptions[signalLevelPos - 1];
                    }
                }
                iter = xpathNav.Select("/Config/AutoScan/LaserDSignalLevel");
                if (iter.MoveNext())
                {
                    int signalLevelPos = int.Parse(iter.Current.GetAttribute("Position", ""));
                    if (configSettings.LaserDSignalOptions != null && signalLevelPos < configSettings.LaserDSignalOptions.Count - 1)
                    {
                        ConfigSettings.AutoScanSettings.LaserDSignalLevel = configSettings.LaserDSignalOptions[signalLevelPos - 1];
                    }
                }

                #endregion

                xpathNav = null;
                xpathDoc = null;
            }
            #endregion

            /// <summary>
            /// Load system specific settings file
            /// </summary>
            public static void LoadSystemSettings()
            {
                // 2017/07/07: Currently Engineering UI does not use SysSettings.xml
                if (IsEngrUI) { return; }

                string sysSettingsFilePath = Path.Combine(applicationDataPath, sysSettingsFileName);

                if (!File.Exists(sysSettingsFilePath))
                {
                    throw new ArgumentException("Configuration file does not exits: " + sysSettingsFileName);
                }

                try
                {
                    XPathDocument xpathDoc = new XPathDocument(sysSettingsFilePath);
                    XPathNavigator xpathNav = xpathDoc.CreateNavigator();
                    XPathNodeIterator iter;

                    iter = xpathNav.Select("/SysSettings/ImagingTabs/ImagingTab");
                    while (iter.MoveNext())
                    {
                        XPathNavigator nav = iter.Current;
                        ImagingSettings imagingSettings = new ImagingSettings();
                        string strImagingType = nav.GetAttribute("ImagingType", "");
                        ImagingType imagingType;
                        Enum.TryParse(strImagingType, out imagingType); //string to enum

                        bool bIsVisible;
                        string strVisibility = iter.Current.GetAttribute("IsVisible", "");
                        if (!Boolean.TryParse(strVisibility, out bIsVisible))
                        {
                            bIsVisible = false;
                        }
                        imagingSettings.ImagingTabType = imagingType;
                        imagingSettings.IsVisible = bIsVisible;
                        configSettings.ImagingSettings.Add(imagingSettings);
                    }

                    iter = xpathNav.Select("/SysSettings/SampleTypes/SampleType");
                    while (iter.MoveNext())
                    {
                        XPathNavigator nav = iter.Current;
                        SampleTypeSetting sampleTypeSetting = new SampleTypeSetting();

                        string strPosition = nav.GetAttribute("Position", "");
                        int position = 0;
                        int.TryParse(strPosition, out position);

                        string strSampleType = nav.GetAttribute("DisplayName", "");
                        //SampleType sampleType;
                        //Enum.TryParse(strSampleType, out sampleType); //string to enum

                        string strFocusPos = nav.GetAttribute("FocusPosition", "");
                        double focusPosition = 0;
                        double.TryParse(strFocusPos, out focusPosition);

                        sampleTypeSetting.Position = position;
                        sampleTypeSetting.DisplayName = strSampleType;
                        //sampleTypeSetting.SampleType = sampleType;
                        sampleTypeSetting.FocusPosition = focusPosition;

                        configSettings.SampleTypeSettings.Add(sampleTypeSetting);
                    }

                    // 20180221: Moved to config.xml
                    // Read signal options
                    /*iter = xpathNav.Select("/SysSettings/Signals/Signal");
                    while (iter.MoveNext())
                    {
                        XPathNavigator nav = iter.Current;

                        string strLaserType = nav.GetAttribute("LaserType", "");
                        strLaserType = "Laser" + strLaserType;
                        LaserType laserType = (LaserType)Enum.Parse(typeof(LaserType), strLaserType);

                        if (nav.HasChildren)
                        {
                            if (nav.MoveToFirstChild())
                            {
                                do
                                {
                                    Signal signal = new Signal();
                                    signal.Position = int.Parse(nav.GetAttribute("Position", ""));
                                    signal.LaserIntensity = int.Parse(nav.GetAttribute("LaserIntensity", ""));
                                    signal.ApdGain = int.Parse(nav.GetAttribute("ApdGain", ""));
                                    signal.ApdPga = int.Parse(nav.GetAttribute("ApdPga", ""));
                                    signal.LaserType = laserType;

                                    switch (laserType)
                                    {
                                        case LaserType.LaserA:
                                            configSettings.LaserASignalOptions.Add(signal);
                                            break;
                                        case LaserType.LaserB:
                                            configSettings.LaserBSignalOptions.Add(signal);
                                            break;
                                        case LaserType.LaserC:
                                            configSettings.LaserCSignalOptions.Add(signal);
                                            break;
                                        case LaserType.LaserD:
                                            configSettings.LaserDSignalOptions.Add(signal);
                                            break;
                                    }
                                }
                                while (nav.MoveToNext());
                                nav.MoveToParent();
                            }
                        }
                    }*/

                    // 20180221: Moved to config.xml
                    // Read signal options
                    /*iter = xpathNav.Select("/SysSettings/PhosphorSignals/PhosphorSignal");
                    while (iter.MoveNext())
                    {
                        XPathNavigator nav = iter.Current;

                        string strLaserType = nav.GetAttribute("LaserType", "");
                        strLaserType = "Laser" + strLaserType;
                        LaserType laserType = (LaserType)Enum.Parse(typeof(LaserType), strLaserType);

                        if (nav.HasChildren)
                        {
                            if (nav.MoveToFirstChild())
                            {
                                do
                                {
                                    Signal signal = new Signal();
                                    signal.Position = int.Parse(nav.GetAttribute("Position", ""));
                                    signal.LaserIntensity = int.Parse(nav.GetAttribute("LaserIntensity", ""));
                                    signal.ApdGain = int.Parse(nav.GetAttribute("ApdGain", ""));
                                    signal.ApdPga = int.Parse(nav.GetAttribute("ApdPga", ""));
                                    signal.LaserType = laserType;

                                    switch (laserType)
                                    {
                                        //case LaserType.LaserA:
                                        //    configSettings.LaserASignalOptions.Add(signal);
                                        //    break;
                                        //case LaserType.LaserB:
                                        //    configSettings.LaserBSignalOptions.Add(signal);
                                        //    break;
                                        case LaserType.LaserC:
                                            configSettings.PhosphorCSignalOptions.Add(signal);
                                            break;
                                        case LaserType.LaserD:
                                            configSettings.PhosphorDSignalOptions.Add(signal);
                                            break;
                                    }
                                }
                                while (nav.MoveToNext());
                                nav.MoveToParent();
                            }
                        }
                    }*/

                    iter = xpathNav.Select("/SysSettings/Chemi/EnableCCDBCRemoval");
                    if (iter.MoveNext())
                    {
                        configSettings.CameraModeSettings.SysSettings.IsEnableCcdBcRemoval = bool.Parse(iter.Current.GetAttribute("Value", ""));
                    }
                    else
                    {
                        configSettings.CameraModeSettings.SysSettings.IsEnableCcdBcRemoval = false;
                    }

                    iter = xpathNav.Select("/Settings/Chemi/CCDColumns/CCDColumn");
                    while (iter.MoveNext())
                    {
                        XPathNavigator nav = iter.Current;
                        string strBinning = nav.GetAttribute("Binning", "");
                        string strBinningMode = System.Text.RegularExpressions.Regex.Match(strBinning, @"\d+").Value;
                        int nBinning = int.Parse(strBinningMode);
                        string strColumns = nav.GetAttribute("Column", "");
                        string[] arrColumns = strColumns.Split(',');
                        for (int i = 0; i < arrColumns.Length; i++)
                        {
                            CCDBadColumn ccdBadCol = new CCDBadColumn();
                            ccdBadCol.Binning = nBinning;
                            ccdBadCol.Column = int.Parse(arrColumns[i].Trim());
                            configSettings.CameraModeSettings.SysSettings.CcdBadColumns.Add(ccdBadCol);
                        }
                    }

                    /*
                    iter = xpathNav.Select("/SysSettings/MotorSettings/MotorSetting");
                    while (iter.MoveNext())
                    {
                        XPathNavigator nav = iter.Current;
                        MotorSettingsType motorSetting = new MotorSettingsType();
                        MotorType motorType;
                        Enum.TryParse(nav.GetAttribute("Motor", ""), out motorType); //string to enum
                        double speed = 0;
                        double.TryParse(nav.GetAttribute("Speed", ""), out speed);

                        motorSetting.MotorType = motorType;
                        motorSetting.Speed = speed;
                        configSettings.MotorSettings.Add(motorSetting);
                    }*/

                    xpathNav = null;
                    xpathDoc = null;
                }
                catch (Exception ex)
                {
                    string message = string.Format("Error reading system settings: \"{0}\"\nERROR: {1}", sysSettingsFileName, ex.Message);
                    throw new Exception(message, ex);
                }
            }

            /// <summary>
            /// Load application method/protocol
            /// </summary>
            public static void LoadMethodSettings()
            {
                string configFilePath = Path.Combine(applicationDataPath, methodFilename);

                if (!File.Exists(configFilePath))
                {
                    throw new Exception("Configuration file does not exits: " + configFilePath);
                }

                XPathDocument xpathDoc = new XPathDocument(configFilePath);
                XPathNavigator xpathNav = xpathDoc.CreateNavigator();

                XPathNodeIterator iter = xpathNav.Select("/Config/Dyes/Dye");
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;
                    string strPosition = nav.GetAttribute("Position", "");
                    int position = 0;
                    int.TryParse(strPosition, out position);

                    string displayName = nav.GetAttribute("DisplayName", "");

                    string strLaserType = nav.GetAttribute("Laser", "");
                    LaserType laserType;
                    Enum.TryParse(strLaserType, out laserType); //string to enum

                    string strWaveLength = nav.GetAttribute("WaveLength", "");
                    //int waveLength = 0;
                    //int.TryParse(strWaveLength, out waveLength);

                    DyeType dyeType = new DyeType();
                    dyeType.Position = position;
                    dyeType.DisplayName = displayName;
                    dyeType.LaserType = laserType;
                    //dyeType.WaveLength = waveLength;
                    dyeType.WaveLength = strWaveLength;
                    configSettings.DyeOptions.Add(dyeType);
                }

                iter = xpathNav.Select("/Config/Methods/Method");
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;

                    string displayName = nav.GetAttribute("DisplayName", "");

                    string strSampleType = nav.GetAttribute("SampleType", "");
                    int sampleType;
                    int.TryParse(strSampleType, out sampleType);

                    string strPixelSize = nav.GetAttribute("PixelSize", "");
                    int pixelSize = 0;
                    int.TryParse(strPixelSize, out pixelSize);

                    string strScanspeed = nav.GetAttribute("ScanSpeed", "");
                    int scanSpeed = 0;
                    int.TryParse(strScanspeed, out scanSpeed);

                    AppMethod appMethod = new AppMethod();
                    appMethod.Name = displayName;
                    appMethod.Sampletype = sampleType;
                    appMethod.Pixelsize = pixelSize;
                    appMethod.Scanspeed = scanSpeed;

                    if (nav.HasChildren)
                    {
                        nav.MoveToFirstChild();
                        do
                        {
                            string strDyeType = nav.GetAttribute("DyeType", "");
                            int dyeType = 0;
                            int.TryParse(strDyeType, out dyeType);
                            string strSignalInt = nav.GetAttribute("SignalIntensity", "");
                            int signalInt = 0;
                            int.TryParse(strSignalInt, out signalInt);
                            string strColorChan = nav.GetAttribute("ColorChannel", "");
                            ImageChannelType colorChannel;
                            Enum.TryParse(strColorChan, out colorChannel); //string to enum

                            AppDyeData dyeData = new AppDyeData();
                            dyeData.DyeType = dyeType;
                            dyeData.SignalIntensity = signalInt;
                            dyeData.ColorChannel = colorChannel;

                            appMethod.Dyes.Add(dyeData);

                        } while (nav.MoveToNext());
                    }

                    configSettings.AppMethods.Add(appMethod);
                }

                iter = xpathNav.Select("/Config/PhosphorMethods/PhosphorMethod");
                while (iter.MoveNext())
                {
                    XPathNavigator nav = iter.Current;

                    string displayName = nav.GetAttribute("DisplayName", "");

                    string strSampleType = nav.GetAttribute("SampleType", "");
                    int sampleType;
                    int.TryParse(strSampleType, out sampleType);

                    string strPixelSize = nav.GetAttribute("PixelSize", "");
                    int pixelSize = 0;
                    int.TryParse(strPixelSize, out pixelSize);

                    string strScanspeed = nav.GetAttribute("ScanSpeed", "");
                    int scanSpeed = 0;
                    int.TryParse(strScanspeed, out scanSpeed);

                    string strIntensityLevel = nav.GetAttribute("IntensityLevel", "");
                    int intensityLevel = 0;
                    int.TryParse(strIntensityLevel, out intensityLevel);

                    AppMethod appMethod = new AppMethod();
                    appMethod.Name = displayName;
                    appMethod.Sampletype = sampleType;
                    appMethod.Pixelsize = pixelSize;
                    appMethod.Scanspeed = scanSpeed;
                    appMethod.IntensityLevel = intensityLevel;

                    /*if (nav.HasChildren)
                    {
                        nav.MoveToFirstChild();
                        do
                        {
                            string strDyeType = nav.GetAttribute("DyeType", "");
                            int dyeType = 0;
                            int.TryParse(strDyeType, out dyeType);
                            string strSignalInt = nav.GetAttribute("SignalIntensity", "");
                            int signalInt = 0;
                            int.TryParse(strSignalInt, out signalInt);
                            string strColorChan = nav.GetAttribute("ColorChannel", "");
                            ImageChannelType colorChannel;
                            Enum.TryParse(strColorChan, out colorChannel); //string to enum

                            AppDyeData dyeData = new AppDyeData();
                            dyeData.DyeType = dyeType;
                            dyeData.SignalIntensity = signalInt;
                            dyeData.ColorChannel = colorChannel;

                            appMethod.Dyes.Add(dyeData);

                        } while (nav.MoveToNext());
                    }*/

                    configSettings.PhosphorMethods.Add(appMethod);
                }

                xpathNav = null;
                xpathDoc = null;
            }

            /// <summary>
            /// Load custom settings (CustSettings.xml).
            /// </summary>
            public static void LoadCustomSettingsFile()
            {
                try
                {
                    string configFilePath = Path.Combine(applicationDataPath, custSettingsFileName);

                    if (!File.Exists(configFilePath))
                    {
                        //throw new Exception("Configuration file does not exits: " + configFilePath);
                        return;
                    }

                    XPathDocument xpathDoc = new XPathDocument(configFilePath);
                    XPathNavigator xpathNav = xpathDoc.CreateNavigator();

                    XPathNodeIterator iter = xpathNav.Select("/CustSettings/Dyes/Dye");
                    while (iter.MoveNext())
                    {
                        XPathNavigator nav = iter.Current;
                        string strPosition = nav.GetAttribute("Position", "");
                        int position = 0;
                        int.TryParse(strPosition, out position);

                        string displayName = nav.GetAttribute("DisplayName", "");
                        string strLaserType = nav.GetAttribute("Laser", "");
                        LaserType laserType;
                        Enum.TryParse(strLaserType, out laserType); //string to enum
                        string strWaveLength = nav.GetAttribute("WaveLength", "");

                        DyeType dyeType = new DyeType();
                        dyeType.Position = position;
                        dyeType.DisplayName = displayName;
                        dyeType.LaserType = laserType;
                        dyeType.WaveLength = strWaveLength;
                        dyeType.IsCustomDye = true;
                        configSettings.CustDyeOptions.Add(dyeType);
                    }

                    xpathNav = null;
                    xpathDoc = null;
                }
                catch (Exception ex)
                {
                    string message = string.Format("Error reading custom settings: \"{0}\"\nERROR: {1}", custSettingsFileName, ex.Message);
                    throw new Exception(message, ex);
                }
            }

            //public static void LoadMasterLibraryInfo()
            //{
            //    if (!string.IsNullOrEmpty(applicationPath))
            //    {
            //        string mastersFolderPath = System.IO.Path.Combine(applicationPath, "Masters");
            //        if (Directory.Exists(mastersFolderPath))
            //        {
            //            masterLibrary = new MasterLibrary(applicationPath);
            //            masterLibrary.LoadLibraryInfo();
            //        }
            //    }
            //}

            #endregion Other Functions

        }

    }

}
