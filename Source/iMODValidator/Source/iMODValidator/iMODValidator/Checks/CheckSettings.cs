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
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.IDF;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Checks
{
    /// <summary>
    /// To be overridden with actual settings
    ///  
    /// The .NET System.ComponentModel.PropertyGrid class is used to display check-properties
    /// To display check-properties in iMODValidator use the following Attributes above class/properties in the code:
    /// Attribute                Description  
    /// Category        This attribute places your property in the appropriate category in a node on the property grid.  
    /// Description     This attribute places a description of your property at the bottom of the property grid  
    /// BrowsableAttribute       This is used to determine whether or not the property is shown or hidden in the property grid  
    /// ReadOnlyAttribute        Use this attribute to make your property read only inside the property grid 
    /// DefaultValueAttribute    Specifies the default value of the property shown in the property grid  
    /// DefaultPropertyAttribute If placed above a property, this property gets the focus when the property grid is first launched. Unlike the other attributes, this attribute goes above the class.  
    /// see: http://msdn.microsoft.com/en-us/library/aa302326.aspx
    /// e.g. [Category("Ranges"), Description("The minimum valid KHV-value for this region")]
    /// public float MinValidKHVValue { get { return minValidKHVValue; } set { minValidKHVValue = value; } }
    /// </summary>
    public abstract class CheckSettings
    {
        protected CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

        [Category("\tDescription"), Description("The name of this check")]
        public string CheckName { get; }

        [Category("\tDescription"), Description("Defines if the check is active by default")]
        public bool IsActiveDefault { get; set; }

        private List<IDFFile> idfFileList;

        protected CheckSettings(string checkName)
        {
            this.CheckName = checkName;
            this.IsActiveDefault = true;
            idfFileList = new List<IDFFile>();
        }

        /// <summary>
        /// Retrieves either an IDFFile oject when the settingsValue refers to an existing IDF-filename
        /// or a ConstantIDFFile object when the settingsValue refers to a valid float value. 
        /// Otherwise, null is returned.
        /// </summary>
        /// <param name="settingsValue"></param>
        /// <returns>an IDFFile- or ConstantIDFFile-object or null</returns>
        public IDFFile GetIDFFile(string settingsValue, Log log = null, int logIndentLevel = 0, string settingsName = null)
        {
            if ((settingsValue == null) || settingsValue.Equals(string.Empty))
            {
                return null;
            }

            float constantValue = float.NaN;
            IDFFile valueIDFFile = null;
            if (float.TryParse(settingsValue.Replace(",", "."), System.Globalization.NumberStyles.Float, englishCultureInfo, out constantValue))
            {
                valueIDFFile = new ConstantIDFFile(constantValue);
            }
            else
            {
                if (!File.Exists(settingsValue))
                {
                    string settingsDescription = "Settingsvalue";
                    if (settingsName != null)
                    {
                        settingsDescription = "Setting " + settingsName;
                    }
                    log.AddWarning(settingsDescription + " for class " + this.GetType().Name + " could not be read: it's neither numeric nor the filename of an existing file: " + settingsValue);
                    valueIDFFile = null;
                }
                else
                {
                    valueIDFFile = IDFFile.ReadFile(settingsValue, false, log, logIndentLevel);
                }
            }

            idfFileList.Add(valueIDFFile);
            return valueIDFFile;
        }

        /// <summary>
        /// Converts the given string to a float. For non-numeric string float.NaN is returned.
        /// </summary>
        /// <param name="settingsValue"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <param name="settingsName"></param>
        /// <returns>the corresponding float number or float.NaN if no number</returns>
        public float GetValue(string settingsValue, Log log = null, int logIndentLevel = 0, string settingsName = null)
        {
            float value = float.NaN;
            if (float.TryParse(settingsValue.Replace(",", "."), System.Globalization.NumberStyles.Float, englishCultureInfo, out value))
            {
                return value;
            }
            else
            {
                return float.NaN;
            }
        }

        /// <summary>
        /// Converts the given string values to a list of floats. For non-numeric strings an Exception is thrown.
        /// </summary>
        /// <param name="settingsValue"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <param name="settingsName"></param>
        /// <returns>list of corresponding float values</returns>
        public List<float> GetValues(string settingsValue, Log log = null, int logIndentLevel = 0, string settingsName = null)
        {
            List<float> values = new List<float>();
            if ((settingsValue != null) && !settingsValue.Equals(string.Empty))
            {
                string[] stringValues = settingsValue.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int idx = 0; idx < stringValues.Length; idx++)
                {
                    float floatValue = float.NaN;
                    if (float.TryParse(stringValues[idx], NumberStyles.Float, englishCultureInfo, out floatValue))
                    {
                        values.Add(floatValue);
                    }
                    else
                    {
                        throw new Exception("Invalid string for parameter ValidKHVValues: " + stringValues[idx]);
                    }
                }
            }
            return values;
        }

        /// <summary>
        /// Converts string with comma-separated integer strings to list of integers
        /// </summary>
        /// <param name="intListString">string with comma-separated integers</param>
        /// <returns>int-list, empty for empty strings</returns>
        public List<int> ParseIntArrayString(string intListString)
        {
            List<int> intList = new List<int>();
            if (!intListString.Trim().Equals(string.Empty))
            {
                string[] stringParts = intListString.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                for (int idx = 0; idx < stringParts.Length; idx++)
                {
                    if (int.TryParse(stringParts[idx], out int intValue))
                    {
                        if ((intValue >= 0))
                        {
                            intList.Add(intValue);
                        }
                    }
                }
            }

            return intList;
        }

        public void ReleaseMemory(bool isMemoryCollected = true)
        {
            foreach (IDFFile idfFile in idfFileList)
            {
                if (idfFile != null)
                {
                    idfFile.ReleaseMemory(isMemoryCollected);
                }
            }
        }

        public abstract void LogSettings(Log log, int logIndentLevel = 0);
    }
}
