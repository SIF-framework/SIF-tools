// ISGinfo is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of ISGinfo.
// 
// ISGinfo is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// ISGinfo is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with ISGinfo. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.ISGinfo
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public string OutputPath { get; set; }
        public bool IsSegmentCountRequested { get; set; }
        public bool IsExtentRequested { get; set; }
        public float SnapCellSize { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            OutputPath = null;
            IsSegmentCountRequested = false;
            IsExtentRequested = false;
            SnapCellSize = float.NaN;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to directory or ISG-file, or a path with a filter,\n"
                                                + "in which case the first file (in some order) that matches the specified filter (default *.ISG) is used", "C:\\Test\\Input\\SurfaceWater.ISG");
            AddToolOptionDescription("e", "Retrieve extent (with format: xll,yll,xur,yur). Optionally specify cellsize cs to snap to.", "/e:25", "Retrieving extent (snapping to cellsize {0})", null, new string[] { "cs" }, new string[] { "NaN"});
            AddToolOptionDescription("s", "Retrieve number of segments", "/s", "Retrieve number of segments");
        }

        /// <summary>
        /// Parse and process obligatory tool parameter at (zero based) index parIndex
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex">returns the index for the argument group for these parameters, 0 if only a single group is defined</param>
        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length == 1)
            {
                // Parse syntax 1:
                InputPath = parameters[0];
                groupIndex = 0;

                // Check InputPath for filename or filter
                if (!Path.GetExtension(InputPath).Equals(""))
                {
                    // A filename or filter is specified as well
                    InputFilter = Path.GetFileName(InputPath);
                    InputFilter = Path.ChangeExtension(InputFilter, "ISG");
                    InputPath = Path.GetDirectoryName(InputPath);
                }
                else
                {
                    InputFilter = "*.ISG";
                }
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
            if (optionName.ToLower().Equals("e"))
            {
                IsExtentRequested = true;
                if (hasOptionParameters)
                {
                    if (!float.TryParse(optionParametersString, NumberStyles.Float, EnglishCultureInfo, out float cellsize))
                    {
                        throw new ToolException("Invalid snapping cellsize: " + optionParametersString);
                    }
                    SnapCellSize = cellsize;
                }
            }
            else if (optionName.ToLower().Equals("s"))
            {
                IsSegmentCountRequested = true;
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

            // Check tool option values
        }
    }
}
