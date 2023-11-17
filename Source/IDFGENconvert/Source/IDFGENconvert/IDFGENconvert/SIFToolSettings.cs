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
        public const int GridPar2DefaultValue = 1;

        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public string OutputPath { get; set; }
        public string OutputFilename { get; set; } = null;

        public bool ShowWarnings { get; set; }
        public bool IsMerged { get; set; }
        public string MergedGENFilename { get; set; }
        public int HullType { get; set; }
        public double HullPar1 { get; set; }
        public List<ValueRange> SkippedValues { get; set; }

        public bool IsOptionGUsed { get; set; }
        public float GridCellsize { get; set; }
        public string GridPar1String { get; set; }
        public string GridPar2String { get; set; }
        public float GridPar3 { get; set; }
        public int GridPar4 { get; set; }
        public bool AddAngleIDFFile { get; set; }
        public bool AddLengthAreaIDFFile { get; set; }
        public bool IsIslandConverted { get; set; }
        public bool IsPointOrderIgnored { get; set; }
        public bool IsGENOrdered { get; set; }
        public int CellOverlapMethod { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            OutputPath = null;

            ShowWarnings = false;
            IsMerged = false;
            MergedGENFilename = null;
            HullType = 1;
            HullPar1 = double.NaN;
            SkippedValues = null;

            IsOptionGUsed = false;
            GridCellsize = 25;
            GridPar1String = null;  // column name or index for point, polygon or first line vertex
            GridPar2String = null;  // for polygons: method for overlap; for lines: column index for value at last/end vertex on line
            GridPar3 = float.NaN;   // method for cell overlap (for polygons), or max extrapolation distance (for lines)
            GridPar4 = 1;           // method for aligning extent and cellsize

            AddAngleIDFFile = false;
            AddLengthAreaIDFFile = false;
            IsIslandConverted = false;
            IsGENOrdered = false;
            IsPointOrderIgnored = false;
            CellOverlapMethod = -1;
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

            // Define general option syntax
            AddToolOptionDescription("s", "Skip specified commaseperated values si, or ranges (s1-s2) in inputfiles", "/s:-9999,-999", "Skipped values: {...}", new string[] { "s1" }, new string[] { "..." }, null, new int[] { 1, 2 });

            // Define IDF-GEN option syntax
            AddToolUsageOptionPreRemark("\nFor IDF-GEN conversion:", 1);
            AddToolOptionDescription("h", "Create a hull of type h1:\n" +
                                          "0) no hull, just write IPF-points for all non-NoData IDF-cells\n" +
                                          "1) convex hull based on cell centers (default)\n" +
                                          "2) concave hull based on cell centers\n" +
                                          "3) outer edges of cells (GEN-lines)\n" +
                                          "4) edges, as 3, but also write cell centers of outer cells (GEN-lines/IPF-points)\n" +
                                          "5) as 4 but only write outer edges of outer cells, so islands in (convex) polygons are removed\n" +
                                          "Add hull parameter h2 depending on h1:\n" +
                                          "2) for concave hull, specify initial number of neighbours (k-value)", "/h:2,100", "Hull of type {0} created for IDF-files", new string[] { "h1" }, new string[] { "h2" }, new string[] { "3" }, new int[] { 1 });
            AddToolOptionDescription("m", "Merge all resulting GEN-features into one GEN-file with filename 'fname':\n" +
                                          "If no filename is given, the default is 'IDFconversion.GEN'", "/m", "Resulting GEN-files are merged to: {0}", null, new string[] { "f" }, new string[] { DefaultMergedGENFilename }, 1);

            // Define GEN-IDF option syntax
            AddToolUsageOptionPreRemark("Statistics (N, average, SD, median, IQR, min, max) about IDF-values per GEN-polygon are written in the GEN-files", 2);
            AddToolUsageOptionPreRemark("\nFor GEN-IDF conversion: ", 2);
            AddToolOptionDescription("g", "Create a grid with:" +
                                          "cellsize 'sz' (default 25)\n" +
                                          "for polygons: cells that overlap with a polygon are assigned a value\n" +
                                          "- c1: columnname or number (one based) or (integer) value c1 if no DAT-file is present\n" +
                                          "- c2: method for checking polygon-cell overlap: 1) cell center inside polygon (default); 2) actual overlap\n" +
                                          "- c3: method for cellvalue/area when multiple polygons intersect cell:\n" +
                                          "      1)  first: value/area of first processed polygon of GEN-file (default);\n" +
                                          "      2)  min: value/area of polygon with minimum cell-value;\n" +
                                          "      3)  max: value/area of polygon with maximum cell-value;\n" +
                                          "      4)  sum: sum of value/area of polygon(s);\n" +
                                          "      5)  largest cellarea: value/area of polygon with largest area in cell;\n" +
                                          "      6)  weighted average (with weight defined by polygon area in cell);\n" +
                                          "      7)  smallest cellarea: value/area of polygon with smallest area in cell;\n" +
                                          "      8)  largest area: value/area of polygon with largest (total) area;\n" +
                                          "      9)  smallest area: value/area of polygon with smallest (total) area;\n" +
                                          "      10) last: value/area of last processed polygon of GEN-file.\n" +
                                          "      For methods 5-9, the value/area of the first polygon is used for equal areas.\n" +
                                          "- c4: method for aligning grid extent and cellsize:\n" +
                                          "      0: do not snap extent (but a warning is given for mismatch with cellsize)\n" +
                                          "      1: snap (enlarged) extent to (multiple) of cellsize (default)\n" +
                                          "      2: snap extent to (multiple) of cellsize (extent can be corrected for cellsize)\n" +
                                          "for lines: linear interpolation from value in column c1 (one based) for first vertex to value in column c2\n" +
                                          "           (if defined) for last vertex, or (integer) values c1/c2 if no DAT-file is present\n" +
                                          "           optionally, specify max. distance c3 for extrapolation along vector with only one column value\n" +
                                          "           columns c1 and/or c2 can be specified as a column name or (one-based) number.\n" + 
                                          "when DAT-file misses and c1/c2 is not defined, sequence numbers are used, starting with 1", "/g:100,5,6", "Grid is created with cellsize: {0} and values c1/c2/c3/c4: {...}", new string[] { "sz" }, new string[] { "c1", "c2", "c3", "c4" }, new string[] { "seq.nr", "N/A", "N/A", "1" }, 2);
            AddToolOptionDescription("a", "Add IDF-file with angle (line) of first GEN-line(s) in cell", "/a", "IDF-file with angle is added", null, null, null, 2);
            AddToolOptionDescription("l", "Add IDF-file with length (line) or area (polygon) of features in cell", "/l", "IDF-file with length/area is added", null, null, null, 2);
            AddToolOptionDescription("n", "Ignore point order, process counterclockwise like clockwise (otherwise counterclockwise is ignored)", null, "Point order is ignored (counterclockwise is processed like clockwise order)", null, null, null, 2);
            AddToolOptionDescription("i", "Convert also island polygons (donut holes, i.e. inner polygons with points in counterclockwise order)\n" +
                                          "for islands the value of the island (the smaller polygon) is always used (par c3 of option g is ignored).\n" +
                                          "without option i or n only polygons with points in clockwise order are converted.\n" + 
                                          "note: for islands, option o (ordered GEN-features) is enforced.", "/i", "Islands (donut holes) are also converted", null, null, null, 2);
            AddToolOptionDescription("o", "Order GEN-polygons/-lines from large to small area/length before processing;\n" + 
                                          "islands (with negative area) are kept directly after previous polygon in source GEN-file", "/o", "GEN-polygons are ordered from large to small area", null, null, null, 2);
            AddToolOptionDescription("w", "Show all warnings (and not only first occurance)", "/w", "All warnings are shown", null, null, null, new int[] { 1, 2 });
        }

        /// <summary>
        /// Format specified option parameter value in logstring with a new (readable) string
        /// </summary>
        /// <param name="optionName">name of option for which a formatted parameter value is required</param>
        /// <param name="parameter">name of option parameter for which a formatted parameter value is required</param>
        /// <param name="parameterValue">the parameter value that has to be formatted</param>
        /// <param name="parameterValues">for reference, all specified parameter values for this options</param>
        /// <returns>a readable form of specified parameter value</returns>
        protected override string FormatLogStringParameter(string optionName, string parameter, string parameterValue, List<string> parameterValues)
        {
            // Format options h and g
            switch (optionName)
            {
                case "h":
                    switch (parameter)
                    {
                        case "0":
                            return "no hull, just IPF-points";
                        case "1":
                            return "convex hull";
                        case "2":
                            return "concave hull";
                        case "3":
                            return "outer edges";
                        case "4":
                            return "outer edges + IPF-points";
                        case "5":
                            return "outer edges (no islands)";

                        default: return parameterValue;
                    }
                case "g":
                    switch (parameter)
                    {
                        case "c1":
                            return "col " + parameterValue;
                        case "c2":
                            // Distinguish between lines and polygons if less than 2 optional parameters are specified
                            string logSubString = (parameterValues.Count <= 3) ? "col " + parameterValue + " (lines)/" : string.Empty;
                            switch (parameterValue)
                            {
                                case "1": logSubString += "cell centre";
                                    break;
                                case "2":
                                    logSubString += "cell overlap";
                                    break;

                                default: return parameterValue;
                            }
                            return logSubString + ((parameterValues.Count <= 3) ? " (polygons)" : string.Empty);
                        case "c3":
                            switch (parameterValue)
                            {
                                case "1": return "first";
                                case "2": return "min";
                                case "3": return "max";
                                case "4": return "sum";
                                case "5": return "largest cellarea";
                                case "6": return "weighted cellarea";
                                case "7": return "smallest cellarea";
                                case "8": return "largest area";
                                case "9": return "smallest area";

                                default: return parameterValue;
                            }
                        case "c4":
                            switch (parameterValue)
                            {
                                case "0": return "don't snap extent";
                                case "1": return "snap (enlarged) extent";
                                case "2": return "snap extent";

                                default: return parameterValue;
                            }
                        default: return parameterValue;
                    }

                // As a default, do not use special formatting and simply return parameter value
                default: return parameterValue;
            }
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
                    throw new ToolException("Invalid filter extension, parameter group cannot be determined: " + Path.GetExtension(InputFilter));
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
            else if (optionName.ToLower().Equals("l"))
            {
                AddLengthAreaIDFFile = true;
            }
            else if (optionName.ToLower().Equals("w"))
            {
                ShowWarnings = true;
            }
            else if (optionName.ToLower().Equals("n"))
            {
                if (!IsIslandConverted)
                {
                    IsPointOrderIgnored = true;
                }
            }
            else if (optionName.ToLower().Equals("i"))
            {
                IsIslandConverted = true;
                IsGENOrdered = true;
                IsPointOrderIgnored = false;
            }
            else if (optionName.ToLower().Equals("o"))
            {
                IsGENOrdered = true;
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
                                GridPar1String = optionParameters[1];
                                if (GridPar1String.Equals(string.Empty))
                                {
                                    GridPar1String = null;
                                }
                            }
                            if (optionParameters.Length >= 3)
                            {
                                GridPar2String = optionParameters[2];
                            }
                            if (optionParameters.Length >= 4)
                            {
                                GridPar3 = float.Parse(optionParameters[3], NumberStyles.Float, EnglishCultureInfo);
                            }
                            if (optionParameters.Length >= 5)
                            {
                                GridPar4 = int.Parse(optionParameters[4], EnglishCultureInfo);
                            }
                        }
                        catch (Exception)
                        {
                            throw new ToolException("Could not parse values for option '" + optionName + "':" + optionParametersString);
                        }
                    }
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
                        if (int.TryParse(optionParametersStrings[0], out int val))
                        {
                            if ((val >= 0) && (val <= 5))
                            {
                                HullType = val;
                                if (optionParametersStrings.Length > 1)
                                {
                                    if (double.TryParse(optionParametersStrings[1], NumberStyles.Float, EnglishCultureInfo, out double dblVal))
                                    {
                                        HullPar1 = dblVal;
                                    }
                                }
                            }
                            else
                            {
                                throw new ToolException("Invalid Hull-method, only value 0 - 5 is allowed: " + optionParametersString);
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
                if (HullType.Equals(0))
                {
                    if (!Path.GetExtension(OutputPath).ToLower().Equals(".ipf"))
                    {
                        throw new ToolException("Output filename should have .IPF-extension for hull-method 0: " + OutputPath);
                    }
                }
                else
                {
                    if (!Path.GetExtension(OutputPath).ToLower().Equals(".gen") && !Path.GetExtension(OutputPath).ToLower().Equals(".idf"))
                    {
                        throw new ToolException("Output filename should have .GEN or .IDF-extension: " + OutputPath);
                    }
                }

                OutputFilename = Path.GetFileName(OutputPath);
                OutputPath = Path.GetDirectoryName(OutputPath);

                // Create output path if not yet existing
                if (!Directory.Exists(Path.GetDirectoryName(OutputPath)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(OutputPath));
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
                case 2:
                    return "concave hull";
                case 3:
                    return "cell edges";
                case 4:
                    return "cell edges/points";
                case 5:
                    return "outer cell edges";
                default:
                    throw new ToolException("Unknown hull type: " + HullType);
            }
        }
    }
}
