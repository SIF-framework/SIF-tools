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
        public const string NoDataSHPDateValue = "00000000";

        /// <summary>
        /// Array with recognized char values for first character of NULL-strings in values of numeric columns. 
        /// If not defined and array equals null, the correction is skipped completely, which speeds up the conversion.
        /// Note: EGIS-library returns a string value for each value regardless of type. Default '*'-symbols are returned for NULL-values
        /// </summary>
        public char[] ShpNullNumericChars { get; set; }

        /// <summary>
        /// Replacement value for NULL
        /// </summary>
        public string ShpNullIntReplacementString { get; set; }
        public string ShpNullDblReplacementString { get; set; }
        public string ShpNullDateReplacementString { get; set; }

        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public string OutputPath { get; set; }
        public string OutputFilename { get; set; }

        public bool IsRecursive { get; set; }
        public int MaxFeatureCount { get; set; }
        public bool IgnoreDuplicateIDs { get; set; }
        public bool IsClockwiseOrderForced { get; set; }

        public string DateFormat { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            OutputPath = null;
            OutputFilename = null;

            IsRecursive = false;
            MaxFeatureCount = 0;
            IgnoreDuplicateIDs = false;
            IsClockwiseOrderForced = false;

            ShpNullNumericChars = new char[] { '*', '\0' };
            ShpNullIntReplacementString = "NULL";
            ShpNullDblReplacementString = "NaN";
            ShpNullDateReplacementString = "NULL";

            DateFormat = "dd-MM-yyyy";
    }

    /// <summary>
    /// Define the syntax of the tool as shown in the tool usage block. 
    /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
    /// </summary>
    protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for input files", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input files (e.g. *.shp or *.GEN)", "*.shp");
            AddToolParameterDescription("outPath", "Path or filename (for single input) to write results", "C:\\Test\\Output");
            AddToolOptionDescription("r", "Process input path recursively", "/r", "Subdirectories under input path are processed recursively ");
            AddToolOptionDescription("s", "Split result in files of maximum r features", "/s:1000000", "Split result in files of {0} features: {0}", new string[] { "r" });
            AddToolOptionDescription("d", "Ignore errors for duplicate IDs in features/rows of GEN or DAT-file, otherwise an exception is thrown", "/d", "Errors on duplicate IDs in GEN/DAT-files are ignored");
            AddToolOptionDescription("c", "Force clockwise order of points. This effectively removes islands (or ring order errors in shapefiles)", null, "Clockwise point order is enforced");
            AddToolOptionDescription("null", "Replace NULL-values in shapefiles by string s when converting to GEN-file\n" +
                                             "As a default 'NULL' is used for integer and 'NaN' for decimal types\n" +
                                             "To use empty string for NULL-values, use /null without parameter values\n" +
                                             "To skip replacement, which may give one or more *-values (depending on field length), use s=*", null, "NULL-values are {0}", null, new string[] { "s" }, new string[] { "[empty string]" });
            AddToolUsageOptionPostRemark("Note for GEN/DAT-files:\n" +
                                    "- valid boolean values are 'True', 'False' (case-insensitive) or '?' (if undefined);\n" + 
                                    "- valid date values have format 'dd-MM-yyyy';\n" +
                                    "- valid numeric values can also have special values 'Infinity', 'Inf', '-Infinity', '-Inf' or 'NaN' (case-insensitive)\n" +
                                    "  and scientific notation is allowed, e.g. '10.01E-6'; use empty value or NULL to create NULL-value in shapefile");
            AddToolUsageOptionPostRemark("Note for shapefiles: When several feature types are present in a GEN-file, it is split in points, lines and polygons and a postfix is added to the filename (resp. '" + Properties.Settings.Default.Point_postfix + "','" + Properties.Settings.Default.Line_postfix + "','" + Properties.Settings.Default.Polygon_postfix + "')");
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
                if (Path.HasExtension(OutputPath))
                {
                    OutputFilename = Path.GetFileName(OutputPath);
                    OutputPath = Path.GetDirectoryName(OutputPath);
                }
                else
                {
                    // Leave null for now
                    OutputFilename = null;
                }
                groupIndex = 0;
            }
            else
            {
                throw new ToolException("Invalid number of parameters (" + parameters.Length + "), check tool usage");
            }
        }

        protected override string FormatLogStringParameter(string optionName, string parameter, string parameterValue, List<string> parameterValues)
        {
            switch (optionName)
            {
                case "null":
                    if (parameterValue.Equals("*"))
                    {
                        return "not checked; asterisks may be returned";
                    }
                    else
                    {
                        return "replaced by: " + parameterValue;
                    }
                default:
                    return base.FormatLogStringParameter(optionName, parameter, parameterValue, parameterValues);
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
            else if (optionName.ToLower().Equals("null"))
            {
                if (hasOptionParameters)
                {
                    if (optionParametersString.Equals("*"))
                    {
                        // Disable check/replacement of NULL-values completely
                        ShpNullNumericChars = null;
                        ShpNullDblReplacementString = null;
                        ShpNullIntReplacementString = null;
                        ShpNullDateReplacementString = null;
                    }
                    else
                    {
                        ShpNullDblReplacementString = optionParametersString;
                        ShpNullIntReplacementString = optionParametersString;
                        ShpNullDateReplacementString = optionParametersString;
                    }
                }
                else
                {
                    ShpNullDblReplacementString = string.Empty;
                    ShpNullIntReplacementString = string.Empty;
                    ShpNullDateReplacementString = string.Empty;
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
            if (OutputFilename != null)
            {
                if (Path.GetExtension(InputFilter).ToLower().Equals(".shp") && !Path.GetExtension(OutputFilename).ToLower().Equals(".gen"))
                {
                    throw new ToolException("For input SHP-files, the output filename should have extension GEN:" + OutputFilename);
                }
                else if (Path.GetExtension(InputFilter).ToLower().Equals(".gen") && !Path.GetExtension(OutputFilename).ToLower().Equals(".shp"))
                {
                    throw new ToolException("For input GEN-files, the output filename should have extension SHP:" + OutputFilename);
                }
            }
        }
    }
}
