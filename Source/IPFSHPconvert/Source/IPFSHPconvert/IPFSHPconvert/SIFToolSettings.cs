// IPFSHPconvert is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IPFSHPconvert.
// 
// IPFSHPconvert is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IPFSHPconvert is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IPFSHPconvert. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;

namespace Sweco.SIF.IPFSHPconvert
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public const string DefaultFIdxColName = "FeatureIdx";
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
        public bool IsOverwrite { get; set; }
        public bool IsFeatureIdxAdded { get; set; }
        public string FeatureIdxColumnName { get; set; }

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
            IsOverwrite = false;
            IsFeatureIdxAdded = false;
            FeatureIdxColumnName = DefaultFIdxColName;

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
            AddToolParameterDescription("inPath", "Path to search for input IPF-files or shapefiles", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input files (e.g. *.IPF)", "*.IPF");
            AddToolParameterDescription("outPath", "Path or IPF/SHP-filename to write results to", "C:\\Test\\Output");
            AddToolOptionDescription("r", "Process input path recursively", "/r", "Input path is processed recursively");
            AddToolOptionDescription("o", "Overwrite existing target output files; if not specified, the tool aborts for existing files", "/o", "Existing output files are overwritten", null, null, null);
            AddToolOptionDescription("f", "Add shape feature index column with name c for SHP-IPF conversion", "/f", "FID-column is added to output IPF-file", null, new string[] { "c" }, new string[] { DefaultFIdxColName });
            AddToolOptionDescription("null", "Replace NULL-values in shapefiles by string s when converting to IPF-file\n" +
                                     "As a default 'NULL' is used for integer and 'NaN' for decimal types\n" +
                                     "To use empty string for NULL-values, use /null without parameter values\n" +
                                     "To skip replacement, which may give one or more *-values (depending on field length), use s=*", null, "NULL-values are {0}", null, new string[] { "s" }, new string[] { "[empty string]" });
            AddToolUsageOptionPostRemark("Note for IPF-files:\n" +
                                    "- valid boolean values are 'True', 'False' (case-insensitive) or '?' (if undefined);\n" +
                                    "- valid date values have format 'dd-MM-yyyy' or leave empty to create NULL-value in shapefile;\n" +
                                    "- valid numeric values can also have special values 'Infinity', 'Inf', '-Infinity', '-Inf' or 'NaN' (case-insensitive)\n" +
                                    "  and scientific notation is allowed, e.g. '10.01E-6'; use empty value or NULL to create NULL-value in shapefile");
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
                if (Path.GetExtension(OutputPath).ToLower().Equals(".ipf") || Path.GetExtension(OutputPath).ToLower().Equals(".shp"))
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
            if (optionName.ToLower().Equals("f"))
            {
                IsFeatureIdxAdded = true;
                if (hasOptionParameters)
                {
                    FeatureIdxColumnName = optionParametersString;
                }
            }
            else if (optionName.ToLower().Equals("o"))
            {
                IsOverwrite = true;
            }
            else if (optionName.ToLower().Equals("r"))
            {
                IsRecursive = true;
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
        }
    }
}
