// IDFbnd is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFbnd.
// 
// IDFbnd is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFbnd is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFbnd. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;

namespace Sweco.SIF.IDFbnd
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string InputPath { get; set; }
        public string BndFilterString { get; set; }
        public float ActiveValue { get; set; }
        public float BoundaryValue { get; set; }
        public float InactiveValue { get; set; }
        public string OutputPath { get; set; }
        public bool IsOverwrite { get; set; }
        public bool IsDiagonallyChecked { get; set; }
        public bool IsOuterCorrection { get; set; }
        public bool KeepInactiveCells { get; set; }
        public Extent Extent { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            BndFilterString = null;
            ActiveValue = float.NaN;
            BoundaryValue = float.NaN;
            InactiveValue = float.NaN;
            OutputPath = null;
            IsOverwrite = false;
            Extent = null;
            KeepInactiveCells = false;
            IsOuterCorrection = true;
            IsDiagonallyChecked = false;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for input files", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input files (e.g. BND*.IDF)", "BND*.IDF");
            AddToolParameterDescription("act", "Around cells with this value boundary cells will be created", "1");
            AddToolParameterDescription("bnd", "Value to use at the boundary", "-1");
            AddToolParameterDescription("inact", "Value to use for inactive cells outside boundary", "0");
            AddToolParameterDescription("outPath", "Path to write results", "C:\\Test\\Output");
            AddToolOptionDescription("o", "Overwrite existing outputfiles", "/o", "Existing output files are overwritten");
            AddToolOptionDescription("d", "Check neighbours also diagonally during boundary search", "/d", "Neighbours cells are also checked diagonally during boundary search");
            AddToolOptionDescription("e", "Extent for boundary (xll,yll,xur,yur). Boundary is searched inwards from this extent.\n" + 
                                          "Cells outside extent will be inactivated.", "/e:184000,352500,200500,371000", "Extent used for boundary cells: {0},{1},{2},{2}", new string[] { "xll", "yll", "xur", "yur" });
            AddToolOptionDescription("i", "Prevent inactive (or NoData-)cells to be converted to boundary cells, leave inactive cells.", "/i", "Inactive cells are not changed into boundary cells");
            AddToolOptionDescription("p", "Prevent inactivation of cells outside specified extent", "/p", "Cells outside specified extent will not be inactivated");
        }

        /// <summary>
        /// Parse and process obligatory tool parameter at (zero based) index parIndex
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex">returns the index for the argument group for these parameters, 0 if only a single group is defined</param>
        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length == 6)
            {
                // Parse syntax 1:
                InputPath = parameters[0];
                BndFilterString = parameters[1];
                ActiveValue = TryParseFloatString(parameters[2], "act value");
                BoundaryValue = TryParseFloatString(parameters[3], "bnd value");
                InactiveValue = TryParseFloatString(parameters[4], "inact value");
                OutputPath = parameters[5];
                groupIndex = 0;
            }
            else
            {
                throw new ToolException("Invalid number of parameters (" + parameters.Length + "), check tool usage");
            }
        }

        private float TryParseFloatString(string valueString, string parameterName)
        {
            try
            {
                return float.Parse(valueString, EnglishCultureInfo);
            }
            catch (Exception ex)
            {
                throw new ToolException("Could not parse '" + parameterName + "': " + valueString, ex);
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
            else if (optionName.ToLower().Equals("d"))
            {
                IsDiagonallyChecked = true;
            }
            else if (optionName.ToLower().Equals("e"))
            {
                if (hasOptionParameters)
                {

                    Extent = Extent.ParseExtent(optionParametersString);
                    if (Extent == null)
                    {
                        throw new ToolException("Could not parse coordinates for option '" + optionName + "':" + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Coordinate values expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("i"))
            {
                KeepInactiveCells = true;
            }
            else if (optionName.ToLower().Equals("p"))
            {
                IsOuterCorrection = false;
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
            if ((BndFilterString == null) || (BndFilterString.Equals(string.Empty)))
            {
                // Specify default
                BndFilterString = "*IDF";
            }

            if (!(BndFilterString.ToLower().EndsWith(".idf")))
            {
                if (!(BndFilterString.ToLower().EndsWith("idf")))
                {
                    BndFilterString = BndFilterString + ".idf";
                }
            }

            // Check that extent actually contains some area
            if ((Extent != null) && !Extent.IsValidExtent())
            {
                throw new ToolException("Extent has no area and is invalid: " + Extent);
            }

            // Create output directory if not existing yet
            OutputPath = ExpandPathArgument(OutputPath);
            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }

        }
    }
}
