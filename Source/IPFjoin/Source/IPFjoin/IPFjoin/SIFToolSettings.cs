// IPFjoin is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IPFjoin.
// 
// IPFjoin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IPFjoin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IPFjoin. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.IPFjoin
{
    public enum JoinType
    {
        Natural,
        Inner,
        FullOuter,
        LeftOuter,
        RightOuter,
    }

    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string InputPath { get; set; }
        public string InputFilter { get; set; }

        /// <summary>
        /// The second file, on the right side of the join, that is joined to the input files
        /// </summary>
        public string JoinFilename { get; set; }
        public string KeyString1 { get; set; }
        public string KeyString2 { get; set; }
        public string OutputPath { get; set; }
        public string OutputFilename { get; set; }
        public bool IsRecursive { get; set; }
        public JoinType JoinType { get; set; }

        // Column indices for XY-coordinates, or -1 if not defined
        public int XColIdx1 { get; set; }
        public int YColIdx1 { get; set; }
        public int XColIdx2 { get; set; }
        public int YColIdx2 { get; set; }

        public bool IsTSJoined { get; set; }
        public JoinType TSJoinType { get; set; }
        public bool IsTS2Interpolated { get; set; }
        public float TSMaxInterpolationDistance { get; set; }
        public DateTime? TSPeriodStartDate { get; set; }
        public DateTime? TSPeriodEndDate { get; set; }
        public int TSDecimalCount { get; set; }
        public bool IgnoreTSDateErrors { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            JoinFilename = null;
            KeyString1 = null;
            KeyString2 = null;
            OutputPath = null;
            OutputFilename = null;
            IsRecursive = false;
            JoinType = JoinType.Natural;

            XColIdx1 = 0;
            YColIdx1 = 1;
            XColIdx2 = 0;
            YColIdx2 = 1;

            IsTSJoined = true;
            TSJoinType = JoinType.FullOuter;
            TSPeriodStartDate = null;
            TSPeriodEndDate = null;
            IsTS2Interpolated = false;
            TSMaxInterpolationDistance = float.NaN;
            TSDecimalCount = -1;
            IgnoreTSDateErrors = false;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for input IPF-files", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input IPF-files (e.g. *.IPF)", "*.IPF");
            AddToolParameterDescription("file2", "Path to secondary IPF-file that is joined to all source files", "tmp\\File2.IPF");
            AddToolParameterDescription("outPath", "Path (or single filename) to write results", "C:\\Test\\Output");
            AddToolOptionDescription("r", "Process input path recursively", "/r", "Input path is processed recursively");
            AddToolOptionDescription("t", "Specify join type: 0: Natural (default), 1: Inner 2: Full Outer, 3: Left Outer, 4: Right Outer", "/t:3", "Specified join type: {0}", new string[] { "t1" });
            AddToolOptionDescription("k1", "Specify key(s) for source IPF-file as (comma-seperated) list of columns (name or number)", "/k1:3,4", "Key1 columnstring(s): {...}", new string[] { "s" }, new string[] { "..." });
            AddToolOptionDescription("k2", "Specify key(s) for joined IPF-file as (comma-seperated) list of columns (name or number)\n" + 
                                           "Note: When k1 and k2 are not defined, all columns with equal names are used and a Natural join is forced.\n" + 
                                           "      When k1 or k2 is not defined, the other keys are used if specified columns exist.", "/k2:ID1,ID2", "Key2 columnstring(s): {...}", new string[] { "s" }, new string[] { "..." });
            AddToolOptionDescription("x1", "Specify (one-based) column numbers for XY-coordinates for input IPF-file(s);\n" + 
                                           "if not specified 1,2 is used as a default", "/x1:4,5", "XY-column numbers file1: {...}", new string[] { "x", "y" });
            AddToolOptionDescription("x2", "Specify (one-based) column numbers for XY-coordinates for joined file;\n" +
                                           "if not specified 1,2 is used as a default", null, "XY-column numbers file2: {...}", new string[] { "x", "y" });

            AddToolOptionDescription("tss", "Skip joining of timeseries from ipf2", "/tss", "Timeseries of ipf2 are not joined");
            AddToolOptionDescription("tst", "Specify join type for timeseries: 1: Inner 2: Full Outer (default), 3: Left Outer, 4: Right Outer", "/tst:3", "Specified join type for timeseries: {0}", new string[] { "t1" });
            AddToolOptionDescription("tsp", "Define period between dates d1 and d2 (dd-mm-yyyy or ddmmyyyy) to select values for; d1 or d2 can be empty",
                                            "/p:01012000,31122020", "Timeseries period: {0} - {1}", new string[] { "d1", "d2" });
            AddToolOptionDescription("tsi", "Use lineair interpolation for timeseries to get value for missing dates in ipf2. Optionally specify maximum\n" +
                                            "interpolation distance i1 (default: NaN) for maximum number of days betweeen missing and existing date\n" + 
                                            "Note: Only missing dates in the timeseries of ipf2 are interpolated, existing NoData-values are copied.",
                                            "/i:7", "Missing dates in timeseries of ipf2 are interpolated with maximum distance: {0}", null, new string[] { "i1" }, new string[] { "NaN" });
            AddToolOptionDescription("tsd", "Specify number of decimals for non NoData-values in joined timeseries TXT-file (default: no rounding)", "/d:2", "Timeseries values are rounded to {0} decimals", new string[] { "d1" });
            AddToolOptionDescription("tserr", "Ignore date errors when reading timeseries of IPF-points; date is skipped", "/err", "Ignore date errors when reading timeseries of IPF-files");
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
                InputPath = parameters[0];
                InputFilter = parameters[1];
                JoinFilename = parameters[2];
                OutputPath = parameters[3];

                SplitPathArgument(OutputPath, out string outputPath, out string outputFilename);
                OutputPath = outputPath;
                OutputFilename = outputFilename;

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
                case "t":
                case "tst":
                    JoinType joinType = ParseJoinType(parameterValue);
                    return joinType.ToString();
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
            else if (optionName.ToLower().Equals("t"))
            {
                if (hasOptionParameters)
                {
                    JoinType = ParseJoinType(optionParametersString);
                }
                else
                {
                    throw new ToolException("Parameter value (1-" + Enum.GetValues(JoinType.GetType()).Length + " expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("k1"))
            {
                if (hasOptionParameters)
                {
                    KeyString1 = optionParametersString;
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("k2"))
            {
                if (hasOptionParameters)
                {
                    KeyString2 = optionParametersString;
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("x1"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if (optionParameters.Length >= 2)
                    {
                        if (!int.TryParse(optionParameters[0], out int xColNr))
                        {
                            throw new ToolException("Invalid column number for X-coordinate for option '" + optionName + "': " +optionParametersString);
                        }
                        if (!int.TryParse(optionParameters[1], out int yColNr))
                        {
                            throw new ToolException("Invalid column number for Y-coordinate for option '" + optionName + "': " + optionParametersString);
                        }
                        XColIdx1 = xColNr - 1;
                        YColIdx1 = yColNr - 1;
                    }
                    else
                    {
                        throw new ToolException("column numbers for X,Y-coordinates expected for option '" + optionName + "': " + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("x2"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if (optionParameters.Length >= 2)
                    {
                        if (!int.TryParse(optionParameters[0], out int xColNr))
                        {
                            throw new ToolException("Invalid column number for X-coordinate for option '" + optionName + "': " + optionParametersString);
                        }
                        if (!int.TryParse(optionParameters[1], out int yColNr))
                        {
                            throw new ToolException("Invalid column number for Y-coordinate for option '" + optionName + "': " + optionParametersString);
                        }
                        XColIdx2 = xColNr - 1;
                        YColIdx2 = yColNr - 1;
                    }
                    else
                    {
                        throw new ToolException("column numbers for X,Y-coordinates expected for option '" + optionName + "': " + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("tst"))
            {
                if (hasOptionParameters)
                {
                    TSJoinType = ParseJoinType(optionParametersString);

                    // Disallow the following JoinTypes: 
                    if (TSJoinType == JoinType.Natural)
                    {
                        throw new ToolException("JoinType Natural (0) is not valid for timeseries, choose another type");
                    }
                }
                else
                {
                    throw new ToolException("Parameter value (1-" + Enum.GetValues(JoinType.GetType()).Length + " expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("tsp"))
            {
                if (hasOptionParameters)
                {
                    string[] parStrings = GetOptionParameters(optionParametersString);
                    if (parStrings.Length < 1)
                    {
                        throw new ToolException("Please specify start- and enddate for option '" + optionName + "':" + optionParametersString);
                    }
                    try
                    {
                        if (parStrings.Length != 2)
                        {
                            throw new ToolException("Invalid number of parameters for option '" + optionName + "':" + optionParametersString);
                        }

                        string startdateString = parStrings[0];
                        string enddateString = parStrings[1];
                        if ((startdateString != null) && !startdateString.Equals(string.Empty))
                        {
                            if (!DateTime.TryParseExact(startdateString, "ddMMyyyy", EnglishCultureInfo, DateTimeStyles.AssumeLocal, out DateTime startdate))
                            {
                                if (!DateTime.TryParseExact(startdateString, "dd-MM-yyyy", EnglishCultureInfo, DateTimeStyles.AssumeLocal, out startdate))
                                {
                                    throw new ToolException("Invalid startdate for option '" + optionName + "':" + startdateString);
                                }
                            }
                            TSPeriodStartDate = startdate;
                        }
                        if ((enddateString != null) && !enddateString.Equals(string.Empty))
                        {
                            if (!DateTime.TryParseExact(enddateString, "ddMMyyyy", EnglishCultureInfo, DateTimeStyles.AssumeLocal, out DateTime enddate))
                            {
                                if (!DateTime.TryParseExact(enddateString, "dd-MM-yyyy", EnglishCultureInfo, DateTimeStyles.AssumeLocal, out enddate))
                                {
                                    throw new ToolException("Invalid enddate for option '" + optionName + "':" + enddateString);
                                }
                            }
                            TSPeriodEndDate = enddate;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new ToolException("Could not parse dates for option '" + optionName + "':" + optionParametersString, ex);
                    }
                }
            }
            else if (optionName.ToLower().Equals("tsi"))
            {
                IsTS2Interpolated = true;
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if (!float.TryParse(optionParametersString, NumberStyles.Float, EnglishCultureInfo, out float days))
                    {
                        throw new ToolException("Could not parse value for option '" + optionName + "':" + optionParametersString);
                    }
                    TSMaxInterpolationDistance = days;
                }
            }
            else if (optionName.ToLower().Equals("tsd"))
            {
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if (!int.TryParse(optionParametersString, out int count))
                    {
                        throw new ToolException("Could not parse value for option '" + optionName + "':" + optionParametersString);
                    }
                    TSDecimalCount = count;
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("tss"))
            {
                IsTSJoined = false;
            }
            else if (optionName.ToLower().Equals("tserr"))
            {
                IgnoreTSDateErrors = true;
            }
            else {
                // specified option could not be parsed
                return false;
            }

            return true;
        }

        protected JoinType ParseJoinType(string joinTypeString)
        {
            if (!int.TryParse(joinTypeString, out int typeValue))
            {
                throw new ToolException("Could not parse integer value for JoinType:" + joinTypeString);
            }
            return ParseJoinType(typeValue);
        }

        protected JoinType ParseJoinType(int typeValue)
        {
            JoinType joinType;
            switch (typeValue)
            {
                case 0:
                    joinType = JoinType.Natural;
                    break;
                case 1:
                    joinType = JoinType.Inner;
                    break;
                case 2:
                    joinType = JoinType.FullOuter;
                    break;
                case 3:
                    joinType = JoinType.LeftOuter;
                    break;
                case 4:
                    joinType = JoinType.RightOuter;
                    break;
                default:
                    throw new ToolException("Unexpected JoinType value: " + typeValue);
            }

            return joinType;
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
                InputFilter = "*.IPF";
            }

            // Check tool option values
            if ((KeyString1 == null) && (KeyString2 != null))
            {
                KeyString1 = KeyString2;
            }
            if ((KeyString2 == null) && (KeyString1 != null))
            {
                KeyString2 = KeyString1;
            }

            // Check tool option values
            if (TSMaxInterpolationDistance < 0)
            {
                throw new ToolException("Invalid maximum interpolation distance (should be positive): " + TSMaxInterpolationDistance);
            }

            if (IsTS2Interpolated)
            {
                if ((TSJoinType == JoinType.Inner) || (TSJoinType == JoinType.RightOuter))
                {
                    SIFTool.Log.AddWarning("Interpolation of missing dates in timeseries of ipf2 has no effect for join types LeftOuter and Inner\n");
                }
            }

            CheckJoinType();

            if ((XColIdx1 < -1) || (YColIdx1 < -1))
            {
                throw new ToolException("Invalid column numbers for XY-coordinates for file1: " + XColIdx1 + ", " + YColIdx1);
            }
            if ((XColIdx2 < -1) || (YColIdx2 < -1))
            {
                throw new ToolException("Invalid column numbers for XY-coordinates for file2: " + XColIdx2 + ", " + YColIdx2);
            }
        }

        protected virtual void CheckJoinType()
        {
            if (((KeyString1 == null) || (KeyString2 == null)) && (JoinType != JoinType.Natural))
            {
                SIFTool.Log.AddInfo("Because one or both keys are not defined, a natural join is enforced");
                JoinType = JoinType.Natural;
            }
        }
    }
}
