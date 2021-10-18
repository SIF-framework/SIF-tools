// IDFGENconvert is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFGENconvert.
// 
// IDFGENconvert is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFGENconvert is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFGENconvert. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.Values;

namespace Sweco.SIF.IDFGENconvert
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public const string DefaultMergedGENFilename = "IDFconversion.GEN";

        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public string OutputPath { get; set; }
        public string OutputFilename { get; set; } = null;

        public int HullType { get; set; }
        public double HullPar1 { get; set; }
        public bool IsMerged { get; set; }
        public string MergedGENFilename { get; set; }
        public List<ValueRange> SkippedValues { get; set; }
        public bool IsOptionGUsed { get; set; }
        public float GridCellsize { get; set; }
        public int GENColIdx { get; set; }
        public bool AddAngleIDFFile { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            OutputPath = null;

            IsMerged = false;
            MergedGENFilename = null;
            HullType = 1;
            HullPar1 = double.NaN;
            SkippedValues = null;

            IsOptionGUsed = false;
            GridCellsize = 25;
            GENColIdx = -1;  // column index for point, polygon or first line vertex

            AddAngleIDFFile = false;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            // Define parameter syntax
            AddToolParameterDescription("inPath", "Path to search for input files", "C:\\Test\\Input", false, new int[] { 1, 2 });
            AddToolParameterDescription("idfFilter", "Filter to select input IDF-files (e.g. *.IDF)", "*.IDF", false, 1);
            AddToolParameterDescription("genFilter", "Filter to select input GEN-files (e.g. *.IDF)", "*.GEN", false, 2);
            AddToolParameterDescription("outPath", "Path to write results", "C:\\Test\\Output", false, new int[] { 1, 2 });

            // Define IDF-GEN option syntax
            AddToolUsageOptionPreRemark("\nFor IDF-GEN conversion:", 1);
            AddToolOptionDescription("h", "Create a hull of type h1:\n" +
                                          "0) no hull, just write IPF-points for all non-NoData IDF-cells\n" +
                                          "1) convex hull based on cell centers (default)", "/h:1", "Hull of type {0} created for IDF-files", new string[] { "h1" }, null, null, 1);
            AddToolOptionDescription("m", "Merge all resulting GEN-features into one GEN-file with filename 'fname':\n" +
                                          "If no filename is given, the default is 'IDFconversion.GEN'", "/m", "Resulting GEN-files are merged to: {0}", null, new string[] { "f" }, new string[] { DefaultMergedGENFilename }, 1);
            AddToolOptionDescription("s", "Skip specified commaseperated values si, or ranges (s1-s2) in inputfiles", "/s:-9999,-999", "Skipped IDF-values: {...}", new string[] { "s1" }, new string[] { "..." }, null, 1);

            // Define GEN-IDF option syntax
            AddToolUsageOptionPreRemark("\nFor GEN-IDF conversion: ", 2);
            AddToolOptionDescription("g", "Create a grid with:" + 
                                          "cellsize 'sz' (default 25)\n" +
                                          "for polygons: value in column c1 (one based) or (integer) value c1 if no DAT-file is present\n" +
                                          "for lines: value in column c1 (one based) or (integer) value c1 if no DAT-file is present\n" +
                                          "when DAT-file misses and c1 is not defined, sequence numbers are used, starting with 1", "/g:100,5", "Grid is created with cellsize: {0}; and value c1: {1}", new string[] { "sz" }, new string[] { "c1" }, new string[] { "seq.nr" }, 2);
            AddToolOptionDescription("a", "Add IDF-file with angle (line) of first GEN-line(s) in cell", "/a", "IDF-file with angle is added", null, null, null, 2);
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

                if (Path.GetExtension(InputFilter).ToLower().Equals(".idf"))
                {
                    groupIndex = 1;
                }
                else if (Path.GetExtension(InputFilter).ToLower().Equals(".gen"))
                {
                    groupIndex = 2;
                }
                else
                {
                    throw new ToolException("Invalid filter extension, parameter group cannot be determied: " + Path.GetExtension(InputFilter));
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
            if (optionName.ToLower().Equals("m"))
            {
                IsMerged = true;
                if (hasOptionParameters)
                {
                    MergedGENFilename = optionParametersString;
                }
                else
                {
                    MergedGENFilename = DefaultMergedGENFilename;
                }
            }
            else if (optionName.ToLower().Equals("s"))
            {
                if (hasOptionParameters)
                {
                    // split part after colon to strings seperated by a comma
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if (optionParameters.Length == 0)
                    {
                        throw new ToolException("Please specify parameters after option 's'");
                    }
                    else
                    {
                        // Parse substrings for this option
                        SkippedValues = new List<ValueRange>();
                        for (int i = 0; i < optionParameters.Length; i++)
                        {
                            string valueString = optionParameters[i];
                            int dashIdx = valueString.IndexOf("-");
                            if (dashIdx > 0)
                            {
                                // form is v1-v2; add all values between v1 and v2
                                string value1String = valueString.Substring(0, dashIdx).Trim();
                                string value2String = valueString.Substring(dashIdx + 1, valueString.Length - dashIdx - 1).Trim();
                                try
                                {
                                    double value1 = double.Parse(value1String, NumberStyles.Float, EnglishCultureInfo);
                                    double value2 = double.Parse(value2String, NumberStyles.Float, EnglishCultureInfo);
                                    SkippedValues.Add(new ValueRange(value1, value2));
                                }
                                catch (Exception ex)
                                {
                                    throw new ToolException("Invalid value range for option 's': " + valueString, ex);
                                }
                            }
                            else
                            {
                                try
                                {
                                    double value = double.Parse(valueString, NumberStyles.Float, EnglishCultureInfo);
                                    SkippedValues.Add(new ValueRange(value, value));
                                }
                                catch (Exception ex)
                                {
                                    throw new ToolException("Invalid value for option 's': " + valueString, ex);
                                }
                            }
                        }
                    }
                }
                else
                {
                    throw new ToolException("Missing parameter(s) for option 's'");
                }
            }
            else if (optionName.ToLower().Equals("h"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParametersStrings = GetOptionParameters(optionParametersString);
                    if (optionParametersStrings.Length > 0)
                    {
                        int val;
                        if (int.TryParse(optionParametersStrings[0], out val))
                        {
                            if ((optionParametersStrings.Length == 1) && (val >= 0) && (val <= 1))
                            {
                                HullType = val;
                            }
                            else
                            {
                                throw new ToolException("Invalid Hull-method, only value 0 - 1 is allowed: " + optionParametersString);
                            }
                        }
                        else
                        {
                            throw new ToolException("Invalid Hull-method value: " + optionParametersString);
                        }
                    }
                    else
                    {
                        throw new ToolException("Please specify Hull-method: " + optionParametersString);
                    }
                }
            }
            else if (optionName.ToLower().Equals("g"))
            {
                IsOptionGUsed = true;
                if (hasOptionParameters)
                {
                    // split part after colon to strings seperated by a comma
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if (optionParameters.Length == 0)
                    {
                        throw new ToolException("Please specify parameters after 'g:':" + optionParametersString);
                    }
                    else
                    {
                        try
                        {
                            // Parse substrings for this option: cellsize and column indices c1 and c2
                            GridCellsize = float.Parse(optionParameters[0], NumberStyles.Float, EnglishCultureInfo);
                            if (optionParameters.Length >= 2)
                            {
                                GENColIdx = int.Parse(optionParameters[1]);
                            }
                        }
                        catch (Exception)
                        {
                            throw new ToolException("Could not parse values for option '" + optionName + "':" + optionParametersString);
                        }
                    }
                }
            }
            else if (optionName.ToLower().Equals("a"))
            {
                AddAngleIDFFile = true;
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
                // InputFilter = "*.XXX";
            }

            if ((HullType == 2) && !HullPar1.Equals(double.NaN) && (HullPar1 < 3))
            {
                throw new ToolException("For option h=2 (concave hull), the minimum k-value is 3");
            }

            // Check specified paths
            InputPath = Path.GetFullPath(InputPath);
            OutputPath = Path.GetFullPath(OutputPath);

            // Check that inputPath exists
            if (!Directory.Exists(InputPath))
            {
                throw new ToolException("inPath does not exist: " + InputPath);
            }

            OutputFilename = null;
            if (Path.GetExtension(OutputPath).Length > 0)
            {
                if (!Path.GetExtension(OutputPath).ToLower().Equals(".gen") && !Path.GetExtension(OutputPath).ToLower().Equals(".idf"))
                {
                    throw new ToolException("Output filename should have .GEN or .IDF-extension: " + OutputPath);
                }
                else
                {
                    OutputFilename = Path.GetFileName(OutputPath);
                    OutputPath = Path.GetDirectoryName(OutputPath);

                    // Create output path if not yet existing
                    if (!Directory.Exists(Path.GetDirectoryName(OutputPath)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
                    }
                }
            }
            else
            {
                // Create output path if not yet existing
                if (!Directory.Exists(OutputPath))
                {
                    Directory.CreateDirectory(OutputPath);
                }
            }
        }

        /// <summary>
        /// Retrieve short name for defined hull type
        /// </summary>
        /// <returns></returns>
        public virtual string GetHullTypeString()
        {
            switch (HullType)
            {
                case 0:
                    return "IPF-points";
                case 1:
                    return "convex hull";
                default:
                    throw new ToolException("Unknown hull type: " + HullType);
            }
        }
    }
}
