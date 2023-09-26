// IDFvoxel is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFvoxel.
// 
// IDFvoxel is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFvoxel is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFvoxel. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.GEN;

namespace Sweco.SIF.IDFvoxel
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public string OutputPath { get; set; }
        public bool IsRecursive { get; set; }

        public bool IsITBChecked { get; set; }
        public bool IsITBUpdated { get; set; }
        public bool IsDeleteEmpty { get; set; }
        public bool IsOverwrite { get; set; }
        public bool IsRenamed { get; set; }

        public float VoxelWidth { get; set; }
        public float VoxelThickness { get; set; }
        public bool IsCSVtoIDFConversion { get; set; }
        public List<int> CSVColumnNumbers { get; set; }

        public GENFile zoneGENFile { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            OutputPath = null;
            IsITBUpdated = false;
            IsITBChecked = false;
            IsDeleteEmpty = false;
            IsOverwrite = false;
            IsRenamed = false;
            IsRecursive = false;

            VoxelThickness = float.NaN;
            VoxelWidth = float.NaN;
            IsCSVtoIDFConversion = false;
            CSVColumnNumbers = new List<int>() { 4, 5 };

            zoneGENFile = null;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for input IDF-files", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input IDF, CSV or zip-files (e.g. *.IDF)", "*.IDF");
            AddToolParameterDescription("outPath", "Path to write results", "C:\\Test\\Output");
            AddToolOptionDescription("r", "Recursively process subdirectories", "/r");
            AddToolOptionDescription("c", "Check if TOP/BOT-values (ITB) are defined in input IDF-file header(s)", "/c");
            AddToolOptionDescription("u", "Update voxel TOP/BOT-values (ITB) in IDF-header to levels defined by filenames and optional voxel thickness t\n" +
                                          "Use BOT-level as defined by llll in filenames with GeoTOP-format: xxxx_iii_llll_cm_[onder|boven]_nap.IDF", null, "IDF-files are updated for TOP/BOT-values (ITB) based on GeoTOP-filenames and voxel thickness {0}", null, new string[] { "t" }, new string[] { "0.5" });
            AddToolOptionDescription("i", "Convert voxel CSV-file(s) to IDF-grids. Default, both lithoklasse and stratigraphy values are converted.\n" +
                                          "Optionally define voxel resolution i1, thickness i2 and CSV-columnnumbers i3, etc. for exported voxel value.\n" +
                                          "When i1 and i2 are not defined, they're retrieved from the CSV-file (which should be sorted on x-coordinate).\n" + 
                                          "Use negative columnnumbers for Exp(val).\n" +
                                          "CSV files may be contained in zip-files (.zip format)", "/i:25,0.25", "CSV-file(s) are converted to IDF-files, using resolution: {0}, thickness {1} and columnnrs: {...}", null, new string[] { "i1", "i2", "i3", "..." }, new string[] { "from CSV-file", "from CSV-file", "4,5" });
            AddToolOptionDescription("z", "Specify processing zone with either an extent (xll,yll,xur,yur) or a GEN-file.\n" +
                                          "This will result in a single voxelmodel dataset when multiple csv/zip-files have been specified.", "/z:input\\zone.GEN", "Zone has been specified: {0}", new string[] { "z1" });
            AddToolOptionDescription("n", "Rename to shorter filenames: xxxx_iii_[+|-]llll_NAP.IDF, with iii reordered (low index for high level)\n" +
                                          "This option only works in combination with option u, see description for that option.", null, "Output files are renamed to short filenames");
            AddToolOptionDescription("o", "Overwrite existing outputfiles or input path. This will delete source files if inPath is outPath,\n" +
                                          "or if option n is specified. Note: existing files are deleted permanently.", "/o", "Existing outputfiles are overwritten");
            AddToolOptionDescription("d", "Delete empty IDF-files (with only NoData-values)", "/d", "Empty output IDF-files are deleted");

            AddToolUsageOptionPostRemark("Note: when no options are specified, option c (check) is selected as a default");
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
            if (optionName.ToLower().Equals("d"))
            {
                IsDeleteEmpty = true;
            }
            else if (optionName.ToLower().Equals("n"))
            {
                IsRenamed = true;
            }
            else if (optionName.ToLower().Equals("o"))
            {
                IsOverwrite = true;
            }
            else if (optionName.ToLower().Equals("r"))
            {
                IsRecursive = true;
            }
            else if (optionName.ToLower().Equals("c"))
            {
                IsITBChecked = true;
            }
            else if (optionName.ToLower().Equals("u"))
            {
                IsITBUpdated = true;
                if (hasOptionParameters)
                {
                    if (!float.TryParse(optionParametersString, NumberStyles.Float, EnglishCultureInfo, out float voxelThickness))
                    {
                        throw new ToolException("Could not parse thickness parameter t for option '" + optionName + "': " + optionParametersString);
                    }
                    VoxelThickness = voxelThickness;
                }
                else
                {
                    // Use default value for this option;
                    VoxelThickness = 0.5f;
                }
            }
            else if (optionName.ToLower().Equals("i"))
            {
                if (IsITBUpdated)
                {
                    throw new ToolException("Option '" + optionName + "' cannot be used together with option 'u'");
                }

                IsCSVtoIDFConversion = true;
                if (hasOptionParameters)
                {
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if (optionParameters.Length > 0)
                    {
                        if (!float.TryParse(optionParameters[0], NumberStyles.Float, EnglishCultureInfo, out float voxelWidth))
                        {
                            throw new ToolException("Could not parse resolution parameter i1 for option i: " + optionParametersString);
                        }
                        VoxelWidth = voxelWidth;
                    }
                    if (optionParameters.Length > 1)
                    {
                        if (!float.TryParse(optionParameters[1], NumberStyles.Float, EnglishCultureInfo, out float voxelThickness))
                        {
                            throw new ToolException("Could not parse thickness parameter i2 for option i: " + optionParametersString);
                        }
                        VoxelThickness = voxelThickness;
                    }
                    if (optionParameters.Length > 2)
                    {
                        CSVColumnNumbers = new List<int>();
                        for (int idx = 2; idx < optionParameters.Length; idx++)
                        {
                            if (!int.TryParse(optionParameters[idx], out int colNr))
                            {
                                throw new ToolException("Could not parse column number parameter i" + (idx + 1) + " for option '" + optionName + "': " + optionParametersString);
                            }
                            CSVColumnNumbers.Add(colNr);
                        }
                    }
                }
            }
            else if (optionName.ToLower().Equals("z"))
            {
                if (hasOptionParameters)
                {
                    if (optionParametersString.ToLower().EndsWith(".gen"))
                    {
                        zoneGENFile = GENFile.ReadFile(optionParametersString);
                    }
                    else
                    {
                        Extent extent = Extent.ParseExtent(optionParametersString);
                        if (extent != null)
                        {
                            zoneGENFile = new GENFile();
                            zoneGENFile.AddFeature(new GENPolygon(zoneGENFile, 1, extent.ToPointList()));
                        }
                    }
                }
                else
                {
                    throw new ToolException("Missing zone parameter z1, extent or GEN-file, for option '" + optionName + "'");
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

            InputPath = Path.GetFullPath(InputPath);
            OutputPath = Path.GetFullPath(OutputPath);

            // Check tool parameters
            if ((InputFilter != null) && (InputFilter.Equals(string.Empty)))
            {
                // Specify default
                InputFilter = "*.IDF";
            }

            if (VoxelWidth <= 0)
            {
                throw new ToolException("Voxel width cannot be zero or smaller: " + VoxelWidth);
            }

            if (VoxelThickness <= 0)
            {
                throw new ToolException("Voxel thickness cannot be zero or smaller: " + VoxelThickness);
            }

            // Check tool option values
        }
    }
}
