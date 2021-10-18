// IDFmath is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFmath.
// 
// IDFmath is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFmath is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFmath. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.IDFmath
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string IDF1Filename { get; set; }
        public string IDF2Filename { get; set; }
        public string IDF3Filename { get; set; }
        public string OperatorString { get; set; }

        public bool IsOverwrite { get; set; }
        public bool UseNodataAsValue { get; set; }
        public float[] NoDataValues { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            IDF1Filename = null;
            OperatorString = null;
            IDF2Filename = null;
            IDF3Filename = null;

            IsOverwrite = false;
            UseNodataAsValue = false;
            NoDataValues = new float[] { float.NaN, float.NaN };
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("idf1", "Path and filename for input IDF-file #1", "Test\\Input\\file1.IDF");
            AddToolParameterDescription("op", "operator to apply on IDF-files #1 and #2, one of: +,-,/,*,avg", "+");
            AddToolParameterDescription("idf2", "path and filename for input IDF-file #2 or floating point value", "Test\\Input\\file2.IDF");
            AddToolParameterDescription("idf3", "path and filename for output IDF-file", "Test\\Output\\file3.IDF");
            AddToolOptionDescription("o", "overwrite existing output", "/o", "Existing output file is overwritten");
            AddToolOptionDescription("v", "use NoData as value, defined in idf1/idf2 or in v1/v2.\n" 
                + "Leave v1 empty to use NoData for v1 and some value for v2.\n"
                + "Without option v, NoData in idf1 or idf2 results in NoData for idf3", "/v:,0", "Using value(s) instead of NoData for idf1 and idf2: {0}, {1}", null, new string[] { "v1", "v2" }, new string[] { "NoData(IDF1)", "NoData(IDF2)" });
        }

        /// <summary>
        /// Parse and process obligatory tool parameter at (zero based) index parIndex
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex">returns the index for the argument group for these parameters, 0 if only a single group is defined</param>
        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length == 4)
            {
                // Parse syntax 1:
                IDF1Filename = parameters[0];
                OperatorString = parameters[1];
                IDF2Filename = parameters[2];
                IDF3Filename = parameters[3];
                groupIndex = 0;
            }
            else
            {
                throw new ToolException("Invalid number of parameters (" + parameters.Length + "), check tool usage");
            }
        }

        /// <summary>
        /// Parse and process tool option
        /// </summary>
        /// <param name="optionName">the character(s) that identify this option</param>
        /// <param name="hasOptionParameters">true if this option has parameters</param>
        /// <param name="optionParametersString">a string with optional comma seperated parameters for this option</param>
        /// <returns>true if recognized and processed</returns>
        protected override bool ParseOption(string optionName, bool hasOptionParameters, string optionParametersString = null)
        {
            if (optionName.ToLower().Equals("o"))
            {
                IsOverwrite = true;
            }
            else if (optionName.ToLower().Equals("v"))
            {
                UseNodataAsValue = true;
                if (hasOptionParameters)
                {
                    if (!optionParametersString.Equals(string.Empty))
                    {
                        // split part after colon to strings seperated by a comma
                        string[] optionParameters = optionParametersString.Split(',');
                        if (optionParameters.Length == 0)
                        {
                            throw new ToolException("Please skip colon or specify parameters after option 'v'");
                        }
                        else
                        {
                            try
                            {
                                // Parse substrings for this option
                                if (optionParameters[0].Length > 0)
                                {
                                    NoDataValues[0] = float.Parse(optionParameters[0], EnglishCultureInfo);
                                }
                                if ((optionParameters.Length > 1) && (optionParameters[1].Length > 0))
                                {
                                    NoDataValues[1] = float.Parse(optionParameters[1], EnglishCultureInfo);
                                }
                            }
                            catch (Exception)
                            {
                                throw new ToolException("Could not parse values for option 'v':" + optionParametersString);
                            }
                        }
                    }
                }
            }
            else
            {
                // specified option could not be parsed
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check the number of parsed arguments against the number of expected arguments. Override to check actual values.
        /// </summary>
        public override void CheckSettings()
        {
            // Perform syntax checks 
            base.CheckSettings();

            IDF1Filename = Path.GetFullPath(IDF1Filename);
            if (!File.Exists(IDF1Filename))
            {
                throw new ToolException("IDF-file #1 does not exist: " + IDF1Filename);
            }

            if (!float.TryParse(IDF2Filename, NumberStyles.Float, EnglishCultureInfo, out float par2Value))
            {
                if (!File.Exists(IDF2Filename))
                {
                    throw new ToolException("IDF file #2 does not exist: " + IDF2Filename);
                }
                IDF2Filename = Path.GetFullPath(IDF2Filename);
            }

            // Create output path if not yet existing
            IDF3Filename = Path.GetFullPath(IDF3Filename);
            string outputPath = Path.GetDirectoryName(IDF3Filename);
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }
        }
    }
}
