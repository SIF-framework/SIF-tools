// GENSHPconvert is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of GENSHPconvert.
// 
// GENSHPconvert is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// GENSHPconvert is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with GENSHPconvert. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;

namespace Sweco.SIF.GENSHPconvert
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        /// <summary>
        /// Searchprefix for NoData-value #1. Note EGIS-library seems to return this value instead of 0-values in input shapefile
        /// </summary>
        public const string ShpNoData1Prefix = "*****";
        public const string ShpNoData1ReplacementString = "0";

        /// <summary>
        /// Searchprefix for NoData-value #2. Note EGIS-library seems to return this value instead of Null-values in input shapefile
        /// </summary>
        public const string ShpNoData2Prefix = "00000000";
        public const string ShpNoData2ReplacementString = "NULL";

        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public string OutputPath { get; set; }
        public bool IsRecursive { get; set; }
        public int MaxFeatureCount { get; set; }
        public bool IgnoreDuplicateIDs { get; set; }
        public bool IsClockwiseOrderForced { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            OutputPath = null;
            IsRecursive = false;
            MaxFeatureCount = 0;
            IgnoreDuplicateIDs = false;
            IsClockwiseOrderForced = false;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for input files", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input files (e.g. *.shp or *.GEN)", "*.shp");
            AddToolParameterDescription("outPath", "Path to write results", "C:\\Test\\Output");
            AddToolOptionDescription("r", "Process input path recursively", "/r", "Subdirectories under input path are processed recursively ");
            AddToolOptionDescription("s", "Split result in files of maximum r features", "/s:1000000", "Split result in files of {0} features: {0}", new string[] { "r" });
            AddToolOptionDescription("d", "�gnore errors for duplicate IDs in features/rows of GEN or DAT-file, otherwise an exception is thrown", "/d", "Errors on duplicate IDs in GEN/DAT-files are ignored");
            AddToolOptionDescription("c", "Force clockwise order of points. This effectively removes islands (or ring order errors in shapefiles)", null, "Clockwise point order is enforced");

            AddToolUsageFinalRemark("When several feature types are present in a GEN-file, it is split in point, line and polygon and a postfix is added to the filename (resp. '" + Properties.Settings.Default.Point_postfix + "','" + Properties.Settings.Default.Line_postfix + "','" + Properties.Settings.Default.Polygon_postfix + "')");
        }

        /// <summary>
        /// Parse and process obligatory tool parameter at (zero based) index parIndex
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex">returns the index for the argument group for these parameters, 0 if only a single group is defined</param>
        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length == 3)
            {
                // Parse syntax 1:
                InputPath = parameters[0];
                InputFilter = parameters[1];
                OutputPath = parameters[2];
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
            if (optionName.ToLower().Equals("r"))
            {
                IsRecursive = true;
            }
            else if (optionName.ToLower().Equals("d"))
            {
                IgnoreDuplicateIDs = true;
            }
            else if (optionName.ToLower().Equals("c"))
            {
                IsClockwiseOrderForced = true;
            }
            else if (optionName.ToLower().Equals("s"))
            {
                if (hasOptionParameters)
                {
                    if (!int.TryParse(optionParametersString, out int featureCount))
                    {
                        throw new ToolException("Could not parse value for option '" + optionName + "':" + optionParametersString);
                    }
                    MaxFeatureCount = featureCount;
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
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

            // Retrieve full paths and check existance
            if (InputPath != null)
            {
                InputPath = ExpandPathArgument(InputPath);
                if (!Directory.Exists(InputPath))
                {
                    throw new ToolException("Input path does not exist: " + InputPath);
                }
            }

            // Check tool parameters
            if ((InputFilter != null) && (InputFilter.Equals(string.Empty)))
            {
                // Specify default
                InputFilter = "*.shp";
            }

            // Check tool option values
        }
    }
}
