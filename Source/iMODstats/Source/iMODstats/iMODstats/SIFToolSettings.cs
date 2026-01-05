// iMODstats is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODstats.
// 
// iMODstats is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODstats is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODstats. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.Values;

namespace Sweco.SIF.iMODstats
{
    public enum ResidualMethod
    {
        None,
        TSValueResidual,
        TSStatDifference
    }

    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public const int DefaultDecimalCount = 2;
        public const int DefaultIPFTSValueColNr = 1;

        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public string OutputFile { get; set; }

        public bool IsRecursive { get; set; }
        public bool IsOverwrite { get; set; }
        public Extent Extent { get; set; }

        /// <summary>
        /// Percentile percentages defined as a list of integer values between 1 en 100; e.g. { 10, 90 } represents 10% and 90%-percentiles; or null if not used.
        /// </summary>
        public List<int> PercentilePercentages { get; set; }

        /// <summary>
        /// Number of decimals in resulting statistics 
        /// </summary>
        public int DecimalCount { get; set; }

        public string IPFXColRef { get; set; }
        public string IPFYColRef { get; set; }
        public string IPFIDColRef { get; set; }
        public string IPFValueColRef { get; set; }
        public List<string> IPFSelColRefs { get; set; }
        public int IPFTSValueColIdx1 { get; set; }
        public int IPFTSValueColIdx2 { get; set; }
        public ResidualMethod IPFResidualMethod { get; set; }
        public DateTime? IPFTSPeriodStartDate { get; set; }
        public DateTime? IPFTSPeriodEndDate { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            OutputFile = null;
            IsOverwrite = false;
            Extent = null;
            DecimalCount = DefaultDecimalCount;
            PercentilePercentages = null;

            IPFXColRef = "1";
            IPFYColRef = "2";
            IPFIDColRef = null;
            IPFValueColRef = null;
            IPFSelColRefs = new List<string>();
            IPFTSValueColIdx1 = -1;
            IPFTSValueColIdx2 = -1;
            IPFResidualMethod = ResidualMethod.None;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for input IDF-, ASC- or IPF-files", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input IDF- or ASC-files (e.g. *.IDF)", "*.IDF");
            AddToolParameterDescription("outFile", "Path and filename of Excel-file(s) to write results to", "C:\\Test\\Output\\Statistics.xlsx");

            AddToolOptionDescription("d", "Define decimal count (default:" + DefaultDecimalCount + ") for results or just use /d to show all decimals", 
                                          "/d:1", "Number of decimals: {0}", null, new string[] { "d1" }, new string[] { DefaultDecimalCount.ToString() });
            AddToolOptionDescription("e", "extent (xll,yll,xur,yur) for processing", "/e:181500,407500,200500,426500", "Extent used for statistics: {0},{1},{2},{3}", new string[] { "xll", "yll", "xur", "yur" } );
            AddToolOptionDescription("o", "overwrite existing outputfile; otherwise an existing outputfile results in an error", "/o", "Existing outputfile is overwritten");
            AddToolOptionDescription("r", "Process input path recursively", "/r", "IDF-files are searched recursively");
            AddToolOptionDescription("t", "Define number of percentile classes via one of the following methods:\n" + 
                                          "- single integer value c (e.g. 4 gives four classes of 25%, default: 0). Use /t:0 to skip percentiles.\n" +
                                          "  0% and 100%-percentiles are shown via min/max-values. To get just the median, use /t:2\n" +
                                          "- comma-seperated list of integer values. E.g. /t:10,90 to get just the 10% and 90%-percentiles\n" +
                                          "Note: the Median Absolute Deviation (MAD) is added when c > 0; check literature for relation with SD", "/t:4", "Number of percentile classes: {0}", new string[] { "c" });
            AddToolOptionDescription("ipf", "Specify column names or numbers (one-based) x, y and v that define columns in the source IPF-file(s) with\n" +
                                            "XY-coordinates (default: 1, 2) and values to calculate statistics for\n" +
                                            "Note: when no value column is defined timeseries statistics are calculated",
                                            "/ipf:1,2,8", "IPF XY-coordinate columns {0}, {1} and value column {2}", new string[] { "x", "y" }, new string[] { "v" }, new string[] { "NA" });
            AddToolOptionDescription("tsp", "Define period between d1 and d2 (dd-mm-yyyy or ddmmyyyy) to select IPF TS-values for; d1 or d2 can be empty",
                                            "/p:01012000,31122020", "Selection period: {0} - {1}", new string[] { "d1" }, new string[] { "d2" }, new string[] { "" });
            AddToolOptionDescription("tsc", "Define (one-based) number v1 of column in associated file(s) to create TS-statistics for (default: 1)\n" +
                                            "optionally define column name or number in IPF-file for ID-column i and numbers ci for other selected columns\n" +
                                            "that should be copied to the resulting IPF-file, with statistics about the timeseries of each IPF-point\n" +
                                            "note: the name of the associated file, excluding path and extension is always added to a TSref column", 
                                            "/tsc:2,3", "Statistics are calculated for TS-column number: {0}, ID-column and other columns: {...}", new string[] { "v1" }, new string[] { "i", "c1", "..." }, new string[] { "NA" });
            AddToolOptionDescription("tsr", "Create residual statistics between timeseries column(s) v2 - v1:\n" +
                                            "  v2 is defined by the (one-based) value column number in the associated file(s) for the specified IPF-file\n" +
                                            "  v1 is defined via the tsc-option\n" + 
                                            "  The following options are available for method m to create residual statistics (default: 1):\n" +
                                            "  1: calculate residual between each timestamp of v2-v1 and calculate statistics over resulting residual\n" +
                                            "  2: calculate average (and defined percentiles) over timestamps of v1 and v2 and calculate difference\n" +
                                            "  when an IPF-point has no associated timeseries, the point is skipped\n" +
                                            "  when an IPF-file has no associated files, the whole IPF-file is skipped",
                                            "/tsr:2,2", "Residual statistics are calculated with: {...}", new string[] { "v2" }, new string[] { "m" }, new string[] { "1" });
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
                OutputFile = parameters[2];
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
                case "tsr":
                    switch (parameter)
                    {
                        case "v2":
                            return "TS-colnr: " + parameterValue;
                        case "m":
                            switch (parameterValue)
                            {
                                case "1":
                                    return " residual method: " + ResidualMethod.TSValueResidual;
                                case "2":
                                    return " residual method: " + ResidualMethod.TSStatDifference;
                                default:
                                    return base.FormatLogStringParameter(optionName, parameter, parameterValue, parameterValues);
                            }
                        default:
                            return base.FormatLogStringParameter(optionName, parameter, parameterValue, parameterValues);
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
            if (optionName.ToLower().Equals("o"))
            {
                IsOverwrite = true;
            }
            else if (optionName.ToLower().Equals("r"))
            {
                IsRecursive = true;
            }
            else if (optionName.ToLower().Equals("d"))
            {
                if (hasOptionParameters)
                {
                    if (!int.TryParse(optionParametersString, out int intValue))
                    {
                        throw new ToolException("Invalid parameter for option 'd', integer expected: " + optionParametersString);
                    }
                    DecimalCount = intValue;
                }
                else
                {
                    DecimalCount = int.MaxValue;
                }
            }
            else if (optionName.ToLower().Equals("e"))
            {
                if (hasOptionParameters)
                {
                    Extent = Extent.ParseExtent(optionParametersString);
                    if (Extent == null)
                    {
                        throw new ToolException("Could not parse extent:" + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Missing extent coordinates for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("t"))
            {
                if (hasOptionParameters)
                {
                    PercentilePercentages = null;
                    if (int.TryParse(optionParametersString, out int count))
                    {
                        if (count > 0)
                        {
                            // A single integer value; this is interpreted as the number of percentile classes including the maximum value
                            PercentilePercentages = new List<int>();
                            for (int pctIdx = 1; pctIdx < count; pctIdx++)
                            {
                                int percentage = (int)((100.0 / ((float)count)) * pctIdx);
                                PercentilePercentages.Add(percentage);
                            }
                        }
                        else
                        {
                            // for n=0, leave PercentilePercentages null, to prevent showing any percentiles or MAD
                        }
                    }
                    else if (optionParametersString.Contains(","))
                    {
                        // A Comma-seperarated string, check for integer values
                        string[] optionParameterStrings = GetOptionParameters(optionParametersString);
                        PercentilePercentages = new List<int>();
                        foreach (string optionParameterString in optionParameterStrings)
                        {
                            if (int.TryParse(optionParameterString, out int percentage))
                            {
                                if ((percentage > 0) && (percentage < 100))
                                {
                                    // Skip 0th and 100th percentile since minimum/maximum is also reported
                                    PercentilePercentages.Add(percentage);
                                }
                            }
                            else
                            {
                                throw new ToolException("Could not parse percentile value: " + optionParameterString);
                            }
                        }
                    }
                    else
                    {
                        throw new ToolException("Could not parse percentiles or percentile class count: " + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("ipf"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameters = GetOptionParameters(optionParametersString);

                    if (optionParameters.Length >= 2)
                    {
                        IPFXColRef = optionParameters[0];
                        IPFYColRef = optionParameters[1];

                        if (optionParameters.Length >= 3)
                        {
                            IPFValueColRef = optionParameters[2];
                        }
                    }
                    else
                    {
                        throw new ToolException("Missing XY-parameters for option '" + optionName + "'");
                    }
                }
                else
                {
                    throw new ToolException("Missing parameters for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("tsc"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if (!int.TryParse(optionParameters[0], out int colnr))
                    {
                        throw new ToolException("Could not parse v1 column number for option '" + optionName + "':" + optionParameters[0]);
                    }
                    IPFTSValueColIdx1 = colnr - 1;

                    if (optionParameters.Length >= 2)
                    {
                        IPFIDColRef = optionParameters[1];
                    }

                    if (optionParameters.Length >= 3)
                    {
                        for (int i = 2; i < optionParameters.Length; i++)
                        {
                            IPFSelColRefs.Add(optionParameters[i]);
                        }
                    }
                }
                else
                {
                    throw new ToolException("Missing parameters for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("tsp"))
            {
                if (hasOptionParameters)
                {
                    string[] parStrings = GetOptionParameters(optionParametersString);
                    if (parStrings.Length == 0)
                    {
                        throw new ToolException("Please specify start- and enddate after '" + optionName + ":': " + optionParametersString);
                    }
                    try
                    {
                        IPFTSPeriodStartDate = parStrings[0].Trim().Equals(string.Empty) ? null : (DateTime?)DateTime.Parse(parStrings[0], EnglishCultureInfo);
                        if (parStrings.Length == 2)
                        {
                            IPFTSPeriodEndDate = parStrings[1].Trim().Equals(string.Empty) ? null : (DateTime?)DateTime.Parse(parStrings[1], EnglishCultureInfo);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new ToolException("Could not parse dates for option '" + optionName + "':" + optionParametersString, ex);
                    }
                    if (parStrings.Length > 2)
                    {
                        throw new ToolException("Too much parameters for option '" + optionName + "'");
                    }
                }
                else
                {
                    throw new ToolException("Missing parameters for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("tsr"))
            {
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    try
                    {
                        // Parse substrings for this option
                        if (optionParameters.Length >= 1)
                        {
                            IPFTSValueColIdx2 = int.Parse(optionParameters[0]) - 1;
                        }
                        if (optionParameters.Length >= 2)
                        {
                            switch (optionParameters[1])
                            {
                                case "1":
                                    IPFResidualMethod = ResidualMethod.TSValueResidual;
                                    break;
                                case "2":
                                    IPFResidualMethod = ResidualMethod.TSStatDifference;
                                    break;
                                default:
                                    throw new ToolException("Undefined residual method: " + optionParametersString);
                            }
                        }
                        else
                        {
                            IPFResidualMethod = ResidualMethod.TSValueResidual;
                        }
                    }
                    catch (Exception)
                    {
                        throw new ToolException("Could not parse parameters for option '" + optionName + "':" + optionParametersString);
                    }
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
                InputFilter = "*.IDF";
            }

            if (!Path.GetExtension(OutputFile).ToLower().Equals(".xlsx"))
            {
                OutputFile = Path.Combine(OutputFile, SIFTool.ToolName + ".xlsx");
            }

            if (!Directory.Exists(Path.GetDirectoryName(OutputFile)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(OutputFile));
            }

            if (IPFValueColRef != null)
            {
                if (IPFTSValueColIdx1 >= 0)
                {
                    throw new ToolException("IPF value column (via /ipf) and IPF timeseries value column (via /tsc) cannot both be specified");
                }
                if ((IPFTSPeriodStartDate != null) || (IPFTSPeriodStartDate != null))
                {
                    throw new ToolException("When IPF value column is specified (via /ipf), period for IPF timeseries (via /tsp) cannot be specified");
                }
                if (IPFTSValueColIdx2 >= 0)
                {
                    throw new ToolException("When IPF value column is specified (via /ipf), residual statistics for IPF timeseries (via /tsr) cannot be specified");
                }
            }
        }
    }
}
