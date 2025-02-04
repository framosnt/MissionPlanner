﻿using log4net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;

namespace MissionPlanner.Utilities
{
    /// <summary>
    /// This class loads and saves some handy app level settings so UI state is preserved across sessions.
    /// </summary>
    public class Settings
    {
        static Settings _instance;

        public static string AppConfigName { get; set; } = "BonsVoosMP";

        public static Settings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new Settings();
                    try
                    {
                        _instance.Load();
                    } catch { }
                }
                return _instance;
            }
        }

        public Settings()
        {
        }

        /// <summary>
        /// use to store all internal config - use Instance
        /// </summary>
        public static Dictionary<string, string> config = new Dictionary<string, string>();

        public static string FileName { get; set; } = "config.xml";

        public string this[string key]
        {
            get
            {
                string value = null;
                config.TryGetValue(key, out value);
                return value;
            }

            set
            {
                config[key] = value;
            }
        }

        public string this[string key, string defaultvalue]
        {
            get
            {
                string value = this[key];
                if (value == null)
                    value = defaultvalue;
                return value;
            }
        }

        public IEnumerable<string> Keys
        {
            // the "ToArray" makes this safe for someone to add items while enumerating.
            get { return config.Keys.ToArray(); }
        }

        public bool ContainsKey(string key)
        {
            return config.ContainsKey(key);
        }

        public string UserAgent { get; set; } = "BonsVoosMP";
        
        public string ComPort
        {
            get { return this["comport"]; }
            set { this["comport"] = value; }
        }

        public string APMFirmware
        {
            get { return this["APMFirmware"]; }
            set { this["APMFirmware"] = value; }
        }

        public string GetString(string key, string @default = "")
        {
            string result = @default;
            string value;
            if (config.TryGetValue(key, out value))
            {
                result = value;
            }
            return result;
        }

        public string BaudRate
        {
            get
            {
                try
                {
                    return this[ComPort + "_BAUD"];
                }
                catch
                {
                    return "";
                }
            }
            set { this[ComPort + "_BAUD"] = value; }
        }

        public string LogDir
        {
            get
            {
                string dir = this["logdirectory"];
                if (string.IsNullOrEmpty(dir))
                {
                    dir = GetDefaultLogDir();
                }
                return dir;
            }
            set
            {
                this["logdirectory"] = value;
            }
        }

        public int Count { get { return config.Count; } }

        public static string GetDefaultLogDir()
        {
            string directory = GetUserDataDirectory() + @"logs";
            if (!Directory.Exists(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch
                {
                
                }
            }

            return directory;
        }

        public IEnumerable<string> GetList(string key)
        {
            if (config.ContainsKey(key))
                return config[key].Split(';').Select(a => System.Net.WebUtility.UrlDecode(a)).Distinct();
            return new string[0];
        }

        public void SetList(string key, IEnumerable<string> list)
        {
            if (list == null || list.Count() == 0)
                return;
            config[key] = list.Select(a => System.Net.WebUtility.UrlEncode(a)).Distinct().Aggregate((s, s1) => s + ';' + s1);
        }

        public void AppendList(string key, string item)
        {
            var list = GetList(key).ToList();
            list.Add(item);
            SetList(key, list);
        }

        public void RemoveList(string key, string item)
        {
            var list = GetList(key).ToList().Where(a => a != item);
            SetList(key, list);
        }

        public int GetInt32(string key, int defaulti = 0)
        {
            int result = defaulti;
            string value = null;
            if (config.TryGetValue(key, out value))
            {
                int.TryParse(value, out result);
            }
            return result;
        }

        public DisplayView GetDisplayView(string key)
        {
            DisplayView result = new DisplayView();
            string value = null;
            if (config.TryGetValue(key, out value))
            {
                DisplayViewExtensions.TryParse(value, out result);
            }
            return result;
        }

        public bool GetBoolean(string key, bool defaultb = false)
        {
            bool result = defaultb;
            string value = null;
            if (config.TryGetValue(key, out value))
            {
                bool.TryParse(value, out result);
            }
            return result;
        }

        public float GetFloat(string key, float defaultv = 0)
        {
            float result = defaultv;
            string value = null;
            if (config.TryGetValue(key, out value))
            {
                float.TryParse(value, out result);
            }
            return result;
        }

        public double GetDouble(string key, double defaultd = 0)
        {
            double result = defaultd;
            string value = null;
            if (config.TryGetValue(key, out value))
            {
                double.TryParse(value, out result);
            }
            return result;
        }

        public byte GetByte(string key, byte defaultb = 0)
        {
            byte result = defaultb;
            string value = null;
            if (config.TryGetValue(key, out value))
            {
                byte.TryParse(value, out result);
            }
            return result;
        }

        private static string _GetRunningDirectory = "";
        /// <summary>
        /// Install directory path
        /// </summary>
        /// <returns></returns>
        public static string GetRunningDirectory()
        {
            if(_GetRunningDirectory != "")
                return _GetRunningDirectory;

            var ass = Assembly.GetEntryAssembly();

            if (ass == null)
            {
                if (CustomUserDataDirectory != "")
                    return CustomUserDataDirectory + Path.DirectorySeparatorChar + AppConfigName +
                           Path.DirectorySeparatorChar;

                return "." + Path.DirectorySeparatorChar;
            }

            var location = ass.Location;

            var path = Path.GetDirectoryName(location);

            if (path == "")
            {
                path = Path.GetDirectoryName(GetDataDirectory());
            }

            _GetRunningDirectory = path + Path.DirectorySeparatorChar;

            return _GetRunningDirectory;
        }

        static bool isMono()
        {
            var t = Type.GetType("Mono.Runtime");
            return (t != null);
        }

        public static bool isUnix = Environment.OSVersion.Platform == PlatformID.Unix;

        /// <summary>
        /// Shared data directory
        /// </summary>
        /// <returns></returns>
        public static string GetDataDirectory()
        {
            if (isMono())
            {
                return GetUserDataDirectory();
            }

            var path = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + Path.DirectorySeparatorChar + AppConfigName +
                          Path.DirectorySeparatorChar;

            return path;
        }

        public static string CustomUserDataDirectory = "";

        /// <summary>
        /// User specific data
        /// </summary>
        /// <returns></returns>
        public static string GetUserDataDirectory()
        {
            if (CustomUserDataDirectory != "")
                return CustomUserDataDirectory + Path.DirectorySeparatorChar + AppConfigName +
                       Path.DirectorySeparatorChar;

            var oldApproachPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) +
                Path.DirectorySeparatorChar + AppConfigName + Path.DirectorySeparatorChar;
            var path = "";
            if (isUnix && !Directory.Exists(oldApproachPath)) // Do not use new AppData path if old path already exists
            {                                                 // E.g. do not migrate to new aproach if directory exists
                path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            }
            else
            {
                path = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            }

            path += Path.DirectorySeparatorChar + AppConfigName + Path.DirectorySeparatorChar;
            return path;
        }

        /// <summary>
        /// full path to the config file
        /// </summary>
        /// <returns></returns>
        static string GetConfigFullPath()
        {
            // old path details
            string directory = GetRunningDirectory();
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var path = Path.Combine(directory, FileName);

            // get new path details
            var newdir = GetUserDataDirectory();

            if (!Directory.Exists(newdir))
            {
                Directory.CreateDirectory(newdir);
            }

            var newpath = Path.Combine(newdir, FileName);

            // check if oldpath config exists
            if (File.Exists(path))
            {
                // is new path exists already, then dont do anything
                if (!File.Exists(newpath))
                {
                    // move to new path
                    File.Move(path, newpath);

                    // copy other xmls as this will be first run
                    var files = Directory.GetFiles(directory, "*.xml", SearchOption.TopDirectoryOnly);

                    foreach (var file in files)
                    {
                        File.Copy(file, newdir + Path.GetFileName(file));
                    }
                }
            }

            return newpath;
        }

        /// <summary>
        /// Returns the full path to the custom default config 
        /// </summary>
        /// <returns></returns>
        static string GetConfigDefaultsFullPath()
        {
            // get default path details
            var newdir = GetRunningDirectory();

            var newpath = Path.Combine(newdir, "custom.config.xml");

            return newpath;
        }

        public void Load()
        {
            // load the defaults
            try
            {
                if (File.Exists(GetConfigDefaultsFullPath()))
                    using (XmlTextReader xmlreader = new XmlTextReader(GetConfigDefaultsFullPath()))
                    {
                        while (xmlreader.Read())
                        {
                            if (xmlreader.NodeType == XmlNodeType.Element)
                            {
                                try
                                {
                                    switch (xmlreader.Name)
                                    {
                                        case "config":
                                            break;
                                        case "xml":
                                            break;
                                        default:
                                            var key = xmlreader.Name;
                                            if (key.Contains("____"))
                                                key = key.Replace("____", "/");
                                            config[key] = xmlreader.ReadString();
                                            break;
                                    }
                                }
                                // silent fail on bad entry
                                catch (Exception)
                                {
                                }
                            }
                        }
                    }
  
            }
            catch
            {

            }

            if (!File.Exists(GetConfigFullPath()))

                //carregar o default fernando 25-10-2022 - escrever código para carregar valor padrão
                // DistToHome

            //config["quickView1"] = "DistToHome";
            config["quickView1"] = "DistToHome";
            config["quickView2"] = "rangefinder1";
            config["quickView3"] = "battery_voltage";
            config["quickView4"] = "battery_voltage2";
            config["quickView5"] = "ch7out";
            config["quickView6"] = "ch6out";
            config["quickViewCols"] = "2";
            config["quickViewRows"] = "3";
            config["MainHeight"] = "548";
            config["MainLocX"] = "-4";
            config["MainLocY"] = "-4";
            config["MainMaximised"] = "Maximizedconfig";
            config["MainWidth"] = "991";
            config["maplast_lat"] = "-23,150198";
            config["maplast_lng"] = "-46,881563";
            config["maplast_zoom"] = "18";
            config["MapType"] = "GoogleSatelliteMap";
            config["menu_autohide"] = "False";
            config["language"] = "pt";
            config["speechaltenabled"] = "False";
            config["speecharm"] = "Motor Armado";
            config["speecharmenabled"] = "True";
            config["speechbattery"] = "Atenção, Bateria com {batv} Voltis e {batp} porcento";
            config["speechbatteryenabled"] = "True";
            config["speechbatterypercent"] = "20";
            config["speechbatteryvolt"] = "20";
            config["speechcustom"] = "Apontado para o  {wpn}, altitude {alt} metros e velocidade de solo {gsp} Quilômetros por hora";
            config["speechcustomenabled"] = "True";
            config["speechdisarm"] = "Motor desarmado";
            config["speechenable"] = "True";
            config["speechmode"] = "Modo de voo alterado para {mode}";
            config["speechmodeenabled"] = "True";
            config["speechwaypoint"] = "Apontado para o ponto {wpn}";
            config["speechwaypointenabled"] = "True";
            config["speedunits"] = "kph";


            //fernando 27-10-2022 - configuração default se nao existir config.xml
            config["displayview"] = "{ 'displayRTKInject': false,   'displayGPSOrder': false,   'displayHWIDs': false,  'displayADSB': false,   'displayName': 1,   'displaySimulation': true,		'displayTerminal': false,		'displayDonate': false,		'displayHelp': false,		'displayAnenometer': false,		'displayQuickTab': true,		'displayPreFlightTab': true,		'displayAdvActionsTab': true,		'displaySimpleActionsTab': false,		'displayGaugesTab': false,		'displayStatusTab': true,		'displayServoTab': false,		'displayScriptsTab': false,		'displayTelemetryTab': true,		'displayDataflashTab': true,		'displayMessagesTab': true,		'displayRallyPointsMenu': true,		'displayGeoFenceMenu': true,		'displaySplineCircleAutoWp': false,		'displayTextAutoWp': true,		'displayCircleSurveyAutoWp': false,		'displayPoiMenu': false,		'displayTrackerHomeMenu': false,		'displayCheckHeightBox': false,		'displayPluginAutoWp': false,		'displayInstallFirmware': false,		'displayWizard': false,		'displayFrameType': false,		'displayAccelCalibration': true,		'displayCompassConfiguration': true,		'displayRadioCalibration': true,		'displayEscCalibration': false,		'displayFlightModes': true,		'displayFailSafe': true,		'displaySikRadio': false,		'displayBattMonitor': false,		'displayCAN': false,		'displayCompassMotorCalib': false,		'displayRangeFinder': true,		'displayAirSpeed': false,		'displayPx4Flow': false,		'displayOpticalFlow': false,		'displayOsd': false,		'displayCameraGimbal': false,		'displayMotorTest': false,		'displayBluetooth': false,		'displayParachute': false,		'displayEsp': false,		'displayAntennaTracker': false,		'displayBasicTuning': false,		'displayExtendedTuning': false,		'displayStandardParams': false,		'displayAdvancedParams': false,		'displayFullParamList': true,		'displayFullParamTree': false,		'displayParamCommitButton': false,		'displayBaudCMB': true,		'displaySerialPortCMB': true,		'standardFlightModesOnly': false,		'autoHideMenuForce': false,		'displayInitialParams': false,		'isAdvancedMode': false,		'displayServoOutput': false,		'displayJoystick': false,		'displayOSD': false,		'displayUserParam': false,		'displayPlannerSettings': false,		'displayFFTSetup': false,		'displayPreFlightTabEdit': true,		'displayPlannerLayout': true,		'lockQuickView': false}";
            config["displayview"] = "{ 'displayRTKInject': false,   'displayGPSOrder': false,   'displayHWIDs': false,  'displayADSB': false,   'displayName': 1,   'displaySimulation': true,		'displayTerminal': false,		'displayDonate': false,		'displayHelp': true,		'displayAnenometer': false,		'displayQuickTab': true,		'displayPreFlightTab': true,		'displayAdvActionsTab': true,		'displaySimpleActionsTab': false,		'displayGaugesTab': false,		'displayStatusTab': true,		'displayServoTab': false,		'displayScriptsTab': false,		'displayTelemetryTab': true,		'displayDataflashTab': true,		'displayMessagesTab': true,		'displayRallyPointsMenu': true,		'displayGeoFenceMenu': true,		'displaySplineCircleAutoWp': true,		'displayTextAutoWp': true,		'displayCircleSurveyAutoWp': false,		'displayPoiMenu': true,		'displayTrackerHomeMenu': true,		'displayCheckHeightBox': true,		'displayPluginAutoWp': true,		'displayInstallFirmware': false,		'displayWizard': false,		'displayFrameType': false,		'displayAccelCalibration': true,		'displayCompassConfiguration': true,		'displayRadioCalibration': true,		'displayEscCalibration': false,		'displayFlightModes': true,		'displayFailSafe': true,		'displaySikRadio': false,		'displayBattMonitor': true,		'displayCAN': false,		'displayCompassMotorCalib': false,		'displayRangeFinder': true,		'displayAirSpeed': false,		'displayPx4Flow': false,		'displayOpticalFlow': false,		'displayOsd': false,		'displayCameraGimbal': true,		'displayMotorTest': false,		'displayBluetooth': false,		'displayParachute': false,		'displayEsp': false,		'displayAntennaTracker': false,		'displayBasicTuning': true,		'displayExtendedTuning': false,		'displayStandardParams': false,		'displayAdvancedParams': true,		'displayFullParamList': true,		'displayFullParamTree': true,		'displayParamCommitButton': true,		'displayBaudCMB': true,		'displaySerialPortCMB': true,		'standardFlightModesOnly': true,		'autoHideMenuForce': false,		'displayInitialParams': false,		'isAdvancedMode': true,		'displayServoOutput': false,		'displayJoystick': true,		'displayOSD': false,		'displayUserParam': false,		'displayPlannerSettings': true,		'displayFFTSetup': false,		'displayPreFlightTabEdit': true,		'displayPlannerLayout': true,		'lockQuickView': false}";


            //ver default:
            //MissionPlanner.Utilities.displa
            //DisplayConfiguration.displayQuickTab=treu 



            return;
            


            try
            {
                using (XmlTextReader xmlreader = new XmlTextReader(GetConfigFullPath()))
                {
                    while (xmlreader.Read())
                    {
                        if (xmlreader.NodeType == XmlNodeType.Element)
                        {
                            try
                            {
                                switch (xmlreader.Name)
                                {
                                    case "Config":
                                        break;
                                    case "xml":
                                        break;
                                    default:
                                        config[xmlreader.Name] = xmlreader.ReadString();
                                        break;
                                }
                            }
                            // silent fail on bad entry
                            catch (Exception)
                            {
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                File.Copy(GetConfigFullPath(), GetConfigFullPath() + DateTime.Now.toUnixTime() + ".failed", true);
                throw;
            }
        }

        public void Save()
        {
            string filename = GetConfigFullPath();

            using (XmlTextWriter xmlwriter = new XmlTextWriter(filename, Encoding.UTF8))
            {
                xmlwriter.Formatting = Formatting.Indented;

                xmlwriter.WriteStartDocument();

                xmlwriter.WriteStartElement("Config");

                foreach (string key2 in config.Keys.OrderBy(a=>a))
                {
                    var key = key2;
                    try
                    {
                        if (key.Contains("/"))
                            key = key.Replace("/", "____");

                        if (key == "" || key.Contains("/") || key.Contains(" ")
                            || key.Contains("-") || key.Contains(":")
                            || key.Contains(";") || key.Contains("@")
                            || key.Contains("!") || key.Contains("#")
                            || key.Contains("$") || key.Contains("%"))
                        {
                            Debugger.Break();
                            Console.WriteLine("Bad config key " + key);
                            continue;
                        }

                        xmlwriter.WriteElementString(key, ""+config[key]);
                    }
                    catch
                    {
                    }
                }

                xmlwriter.WriteEndElement();

                xmlwriter.WriteEndDocument();
                xmlwriter.Close();
            }
        }

        public void Remove(string key)
        {
            if (config.ContainsKey(key))
            {
                config.Remove(key);
            }
        }

    }
}
