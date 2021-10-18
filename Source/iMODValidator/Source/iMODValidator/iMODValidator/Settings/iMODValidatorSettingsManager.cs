// iMODValidator is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODValidator.
// 
// iMODValidator is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODValidator is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODValidator. If not, see <https://www.gnu.org/licenses/>.
using Newtonsoft.Json;
using Sweco.SIF.iMODValidator.Checks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static System.Net.Mime.MediaTypeNames;

namespace Sweco.SIF.iMODValidator.Settings
{
    /// <summary>
    /// Class for managing iMODValidator settings, i.e. serializing to/from XML-file
    /// </summary>
    public static class iMODValidatorSettingsManager
    {
        private static iMODValidatorSettings settings;
        public static iMODValidatorSettings Settings
        {
            get
            {
                if (settings == null)
                {
                    LoadMainSettings();
                }
                return settings;
            }
        }

        /// <summary>
        /// Retrieve name of iMODValidator executable
        /// </summary>
        public static string ApplicationName { get; set; } = "iMODValidator";
        //{
        //    get
        //    {
        //        AssemblyProductAttribute myProduct = (AssemblyProductAttribute)AssemblyProductAttribute.GetCustomAttribute(Assembly.GetExecutingAssembly(), typeof(AssemblyProductAttribute));
        //        // (Assembly.GetEntryAssembly() != null) ? Assembly.GetEntryAssembly().GetName().Name : Assembly.GetExecutingAssembly().GetName().Name; }
        //        return myProduct.Product;
        //    }
        //}

        /// <summary>
        /// Retrieve path of iMODValidator executable
        /// </summary>
        public static string StartupPath
        {
            get { return Path.GetDirectoryName((Assembly.GetEntryAssembly() != null) ? Assembly.GetEntryAssembly().Location : Assembly.GetExecutingAssembly().Location); }
        }

        /// <summary>
        /// Retrieve path of used iMODValidator settingsfile
        /// </summary>
        public static string GetSettingsFilename()
        {
            return Path.Combine(StartupPath, ApplicationName + ".xml");
        }

        /// <summary>
        /// Load settings from settingsfile
        /// </summary>
        /// <param name="settingsFilename"></param>
        public static void LoadMainSettings(string settingsFilename = null)
        {
            settings = null;
            try
            {
                iMODValidatorSettings mainSettings = (iMODValidatorSettings)LoadSettings(typeof(iMODValidatorSettings), typeof(iMODValidatorSettings).Name, settingsFilename);

                if (mainSettings != null)
                {
                    settings = mainSettings;
                }
                else
                {
                    // If settings are available but not yet present in the settingsfile, save current settings
                    if (settings == null)
                    {
                        // Use hardcoded default settings
                        settings = new iMODValidatorSettings();
                    }
                    SaveMainSettings(settingsFilename);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not read " + ApplicationName + "-settings", ex);
            }
        }

        /// <summary>
        /// Save settings to settingsfile
        /// </summary>
        /// <param name="settingsFilename"></param>
        public static void SaveMainSettings(string settingsFilename = null)
        {
            try
            {
                if (settings != null)
                {
                    SaveSettings(settings, typeof(iMODValidatorSettings).Name, settingsFilename);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not save " + ApplicationName + "-settings in " + GetSettingsFilename(), ex);
            }
        }

        /// <summary>
        /// Load settings for a specific check
        /// </summary>
        /// <param name="check"></param>
        /// <param name="settingsName"></param>
        /// <param name="settingsFilename"></param>
        public static void LoadCheckSettings(Check check, string settingsName = null, string settingsFilename = null)
        {
            if ((check != null) && (check.Settings != null))
            {
                if (settingsName == null)
                {
                    settingsName = check.Name;
                }

                CheckSettings settings = (CheckSettings)LoadSettings(check.Settings.GetType(), settingsName, settingsFilename);

                if (settings != null)
                {
                    check.Settings = settings;
                }
                else
                {
                    // If settings are available but not yet present in the settingsfile, save them
                    if (check.Settings != null)
                    {
                        SaveCheckSettings(check, settingsName, settingsFilename);
                    }
                }
            }
        }

        /// <summary>
        /// Save settings for a specific check
        /// </summary>
        /// <param name="check"></param>
        /// <param name="settingsName"></param>
        /// <param name="settingsFilename"></param>
        public static void SaveCheckSettings(Check check, string settingsName = null, string settingsFilename = null)
        {
            if ((check != null) && (check.Settings != null))
            {
                if (settingsName == null)
                {
                    settingsName = check.Name;
                }

                SaveSettings(check.Settings, settingsName, settingsFilename);
            }
        }

        private static object LoadSettings(Type settingsType, string settingsName = null, string settingsFilename = null)
        {
            object settings = null;

            if (settingsName == null)
            {
                settingsName = settingsType.Name;
            }

            XmlDocument xmlDoc = LoadSettingsXmlDocument(settingsFilename);

            // Replace environments variables
            string innerXml = xmlDoc.InnerXml;
            xmlDoc.InnerXml = System.Environment.ExpandEnvironmentVariables(innerXml);

            // Retrieve settings for this check
            XmlNodeList nodeList = xmlDoc.GetElementsByTagName(settingsName);
            if (nodeList.Count > 0)
            {
                string jsonString = JsonConvert.SerializeXmlNode(nodeList[0], Newtonsoft.Json.Formatting.None, true);
                settings = JsonConvert.DeserializeObject(jsonString, settingsType);
            }

            return settings;
        }

        private static void SaveSettings(object settings, string settingsName = null, string settingsFilename = null)
        {
            if (settingsName == null)
            {
                settingsName = settings.GetType().Name;
            }

            try
            {
                // First load existing settingsFile
                XmlDocument validatorSettingsDoc = LoadSettingsXmlDocument(settingsFilename);

                // Convert settings of this check to JSON-string
                string jsonString = JsonConvert.SerializeObject(settings);

                // Convert CheckSettings to temporary XML-document using JSON.NET
                XmlDocument jsonDoc = JsonConvert.DeserializeXmlNode(jsonString, settingsName);
                XmlNode jsonNode = jsonDoc.FirstChild;

                // Import settingsnode from JSON-converted CheckSettings to iMODValidator settings document
                XmlNode settingsNode = validatorSettingsDoc.ImportNode(jsonNode, true);

                // Retrieve settings for this check
                XmlNodeList settingsNodeList = validatorSettingsDoc.GetElementsByTagName(settingsName);
                if (settingsNodeList.Count > 0)
                {
                    XmlNode parentNode = settingsNodeList[0].ParentNode;
                    parentNode.RemoveChild(settingsNodeList[0]);
                    parentNode.AppendChild(settingsNode);
                }
                else
                {
                    validatorSettingsDoc.FirstChild.AppendChild(settingsNode);
                }

                // Write iMODValidator settings xml
                StreamWriter sw = new StreamWriter((settingsFilename != null) ? settingsFilename : GetSettingsFilename());
                XmlTextWriter xw = new XmlTextWriter(sw);
                xw.Formatting = System.Xml.Formatting.Indented;
                validatorSettingsDoc.WriteTo(xw);
                xw.Close();
            }
            catch (Exception ex)
            {
                throw new Exception("Could not save " + settingsName + "-settings in " + GetSettingsFilename(), ex);
            }
        }

        private static XmlDocument LoadSettingsXmlDocument(string settingsFilename = null)
        {
            XmlDocument xmlDoc = new XmlDocument();
            if (settingsFilename != null)
            {
                if (File.Exists(settingsFilename))
                {
                    xmlDoc.Load(settingsFilename);
                }
            }
            else if (File.Exists(GetSettingsFilename()))
            {
                xmlDoc.Load(GetSettingsFilename());
            }
            else
            {
                XmlElement rootElement = xmlDoc.CreateElement(ApplicationName.Replace(" ", "-"));
                xmlDoc.AppendChild(rootElement);
            }

            return xmlDoc;
        }
    }
}
