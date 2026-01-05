// IFFselect is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IFFselect.
// 
// IFFselect is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IFFselect is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IFFselect. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.IFF;

namespace Sweco.SIF.IFFSelect
{
    public enum ReverseMethodEnum
    {
        None,
        Before,
        After
    }

    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public string OutputPath { get; set; }
        public string OutputFilename { get; set; }

        public SelectPointType SelectPointType { get; set; }
        public SelectPointMethod SelectPointMethod { get; set; }
        public SelectFlowLinesMethod SelectFlowLinesMethod { get; set; }
        public Extent Extent { get; set; }
        public string TopLevelString { get; set; }
        public string BotLevelString { get; set; }
        public float MinTravelTime { get; set; }
        public float MaxTravelTime { get; set; }
        public float MinVelocity { get; set; }
        public float MaxVelocity { get; set; }
        public string GENFilename { get; set; }
        public bool IsTravelTimeReversed { get; set; }
        public ReverseMethodEnum ReverseMethod { get; set; }
        public string OutputFilePostfix { get; set; }

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

            Extent = null;
            TopLevelString = null;
            BotLevelString = null;
            GENFilename = null;
            MinTravelTime = float.NaN;
            MaxTravelTime = float.NaN;
            MinVelocity = float.NaN;
            MaxVelocity = float.NaN;
            SelectPointType = SelectPointType.Undefined;
            SelectPointMethod = SelectPointMethod.Inside;
            SelectFlowLinesMethod = SelectFlowLinesMethod.Undefined;
            ReverseMethod = ReverseMethodEnum.None;
            OutputFilePostfix = null;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("InputPath", "Path to search for input files", "C:\\Test\\Input");
            AddToolParameterDescription("InputFilter", "Filter to select input files (e.g. *.IPF)", "*.IFF");
            AddToolParameterDescription("OutputPath", "Path or IPF-filename to write results", "C:\\Test\\Output");
            AddToolOptionDescription("e", "Selection volume within specified extent", "/e:184000,352500,200500,371000",
                                     "Analysis volume extent defined: {0},{1},{2},{3}", new string[] { "e1", "e2", "e3", "e4" });
            AddToolOptionDescription("l", "Selection volume below specified value/IDF-file l1 or \n" +
                                     "between TOP/BOT-levels by values/IDF-file l1 and l2",
                                     "/l:3,5", "Analysis volume TOP/BOT-levels defined by: {0},{1}", new string[] { "l1" }, new string[] { "l2" });
            AddToolOptionDescription("p", "Selection volume within polygon(s) in GEN-file p1", "/p:somecountour.GEN",
                                     "Analysis volume area defined by: {0}", new string[] { "p1" });
            AddToolOptionDescription("t", "Select flowlines that have travel time between t1 and t2 (years)", "/t:2010,2020",
                                      "Flowlines are selected with travel time between {0} and {1}", null, new string[] { "t1", "t2" }, new string[] { "0", "Inf" });
            AddToolOptionDescription("v", "Select flowlines that have velocity between v1 and v2 (m/d)",
                                    "/v:0.1,5", "Flowlines with velocity between {0} and {1} are selected", null, new string[] { "v1", "v2" }, new string[] { "0", "Inf" });
            AddToolOptionDescription("c", "Clip IFF-pathlines as defined by selection volume and c1 parameter: \n" +
                                     "  0) Select all pathlines; \n" +
                                     "  1) Select only pathlines inside specified volume (clip, the default); \n" +
                                     "  2) Select only pathlines outside specified volume (inverse clip); \n" +
                                     "  3) Select only pathlines before specified volume (start to just inside); \n" +
                                     "  4) Select only pathlines before and inside specified volume (start to inside);\n" + 
                                     "  note: flowline is clipped, but individual linesegments are not clipped",
                                     "/c:1", "Volume/area method is: {0}", new string[] { "c1" });
            AddToolOptionDescription("s", "Instead of clipping flowlines, select whole pathline as specified by \n" +
                                     "  s1=the IFF-point Selection method. \n"+
                                     "  If one IFF-point is inside the specified volume, the whole pathline is selected (!)\n" +
                                     "    1) evaluate only IFF-points that start inside the specified volume; \n" +
                                     "    2) evaluate only IFF-points, that pass through the specified volume (midpoints); \n" +
                                     "    3) evaluate only IFF-points that end inside the specified volume; \n" +
                                     "    4) evaluate all IFF-points, that start, pass or end inside volume; \n" +
                                     "  s2, the type of constraint: 1) inside (default) or 2) outside specified volume",
                                     "/s:1,1", "Pathlines selected with method '{0}' and constraint '{1}'", new string[] { "s1" }, new string[] { "s2" });
            AddToolOptionDescription("r", "Reverse traveltime per flowline, before (r1=1) or after (r1=2) selection",
                                     "/r:1", "Traveltime is reversed per flowline with method {0}", new string[] { "r1" });
            AddToolOptionDescription("pf", "Add postfix p1 to output file(s)", "/pf:_sel", "Added postfix to output file(s): {0}", new string[] { "p1" });
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
                if (Path.GetExtension(OutputPath).ToLower().Equals(".iff") )
                {
                    OutputFilename = Path.GetFileName(OutputPath);
                    OutputPath = Path.GetDirectoryName(OutputPath);
                }
                else
                {
                    // Leave null
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
                case "c":
                    switch (parameterValue)
                    {
                        case "0":
                            return "select all pathlines";
                        case "1":
                            return "select only pathlines inside specified volume (clip)";
                        case "2":
                            return "select only pathlines outside specified volume (inverse clip)";
                        case "3":
                            return "select only pathlines before specified volume (start to just inside)";
                        case "4":
                            return "select only pathlines before and inside specified volume (start to inside)";
                        default:
                            break;
                    }
                    break;

                case "s":
                    switch (parameter)
                    {
                        case "s1":
                            switch (parameterValue)
                            {
                                case "1":
                                    return "evaluate only IFF-points that start inside specified volume";
                                case "2":
                                    return "evaluate only IFF-points, that pass through specified volume";
                                case "3":
                                    return "evaluate only IFF-points that end inside specified volume";
                                case "4":
                                    return "evaluate IFF-points, that start, pass or end inside volume";
                                default:
                                    break;
                            }
                            break;

                        case "s2":
                            switch (parameterValue)
                            {
                                case "1":
                                    return "inside volume";
                                case "2":
                                    return "outside volume";
                                default:
                                    break;
                            }
                            break;

                        default:
                            break;
                    }
                    break;

                default:
                    break;
            }
            return base.FormatLogStringParameter(optionName, parameter, parameterValue, parameterValues);
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
                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    try
                    {
                        Extent = Extent.ParseExtent(optionParametersString);
                    }
                    catch
                    {
                        throw new ToolException("Could not parse extent: " + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("l"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    TopLevelString = optionParameters[0];
                    if (optionParameters.Length == 2)
                    {
                        BotLevelString = optionParameters[1];
                    }
                    else if (optionParameters.Length > 2)
                    {
                        throw new ToolException("For option 'l' not more than two parameters are allowed: " + optionParameters);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("p"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameters = GetOptionParameters(optionParametersString);

                    if (optionParameters.Length == 1)
                    {
                        GENFilename = optionParameters[0];
                    }
                    else
                    {
                        throw new ToolException("More than one parameter specified for option 'p': " + optionParameters);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("t"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if (optionParameters[0].Equals(string.Empty))
                    {
                        // leave float.NaN value
                    }
                    else if (float.TryParse(optionParameters[0], NumberStyles.Float, EnglishCultureInfo, out float minTravelTime))
                    {
                        MinTravelTime = minTravelTime;
                    }
                    else
                    {
                        throw new ToolException("Invalid Minimum travel time value: " + optionParameters[0]);
                    }

                    if (optionParameters.Length == 2)
                    {
                        if (optionParameters[1].Equals(string.Empty))
                        {
                            // leave float.NaN value
                        }
                        else if (float.TryParse(optionParameters[1], NumberStyles.Float, EnglishCultureInfo, out float maxTravelTime))
                        {
                            MaxTravelTime = maxTravelTime;
                        }
                        else
                        {
                            throw new ToolException("Invalid Maximum travel time value: " + optionParameters[1]);
                        }
                    }
                    if (optionParameters.Length > 2)
                    {
                        throw new ToolException("For option 't' not more than two parameters are allowed: " + optionParameters);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("v"))
            {
                if (hasOptionParameters)
                {

                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if (optionParameters[0].Equals(string.Empty))
                    {
                        // leave float.NaN value
                    }
                    else if (float.TryParse(optionParameters[0], NumberStyles.Float, EnglishCultureInfo, out float minVelocity))
                    {
                        MinVelocity = minVelocity;
                    }
                    else
                    {
                        throw new ToolException("Invalid Minimum velocity value: " + optionParameters[0]);
                    }

                    if (optionParameters.Length > 1)
                    {
                        if (optionParameters[1].Equals(string.Empty))
                        {
                            // leave float.NaN value
                        }
                        else if (float.TryParse(optionParameters[1], NumberStyles.Float, EnglishCultureInfo, out float maxVelocity))
                        {
                            MaxVelocity = maxVelocity;
                        }
                        else
                        {
                            throw new ToolException("Invalid Maximum velocity value: " + optionParameters[1]);
                        }
                    }
                    else if (optionParameters.Length > 2)
                    {
                        throw new ToolException("For option 'v' not more than two parameters are allowed: " + optionParameters);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("c"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if (int.TryParse(optionParameters[0], out int c1Value))
                    {
                        switch (c1Value)
                        {
                            case 0:
                                SelectFlowLinesMethod = SelectFlowLinesMethod.Undefined;
                                break;
                            case 1:
                                SelectFlowLinesMethod = SelectFlowLinesMethod.Inside;
                                break;
                            case 2:
                                SelectFlowLinesMethod = SelectFlowLinesMethod.Outside;
                                break;
                            case 3:
                                SelectFlowLinesMethod = SelectFlowLinesMethod.Before;
                                break;
                            case 4:
                                SelectFlowLinesMethod = SelectFlowLinesMethod.BeforeAndInside;
                                break;
                            default:
                                throw new ToolException("Invalid parameter value for option c1: " + optionParameters);
                        }
                    }
                    else
                    {
                        throw new ToolException("Parameter for option 'c' should be an integer value 1, 2 or 3: " + optionParameters);
                    }
                }
                //REMARK: in old tool, default option  inside (1) as possibility
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("s"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if (int.TryParse(optionParameters[0], out int s1Value))
                    {
                        switch (s1Value)
                        {
                            case 0:
                                SelectPointType = SelectPointType.Undefined;
                                break;
                            case 1:
                                SelectPointType = SelectPointType.Start;
                                break;
                            case 2:
                                SelectPointType = SelectPointType.Mid;
                                break;
                            case 3:
                                SelectPointType = SelectPointType.End;
                                break;
                            case 4:
                                SelectPointType = SelectPointType.All;
                                break;
                            default:
                                throw new ToolException("Invalid parameter value for option s1: " + optionParameters[0]);
                        }
                        if (optionParameters.Length > 1)
                        {
                            if (int.TryParse(optionParameters[1], out int s2Value))
                            {
                                switch (s2Value)
                                {
                                    case 1:
                                        SelectPointMethod = SelectPointMethod.Inside;
                                        break;
                                    case 2:
                                        SelectPointMethod = SelectPointMethod.Outside;
                                        break;
                                    default:
                                        throw new ToolException("Invalid parameter value for option s2: " + optionParameters[1]);
                                }
                            }
                        }
                        if (optionParameters.Length > 2)
                        {
                            throw new ToolException("Invalid number of parameters for option 's', two parameters expected: " + optionParameters);
                        }
                    }
                }
                else
                {
                    throw new ToolException("Parameter for option '" + optionName + "' should be an integer value 1, 2, 3 or 4");
                }
            }
            else if (optionName.ToLower().Equals("pf"))
            {
                if (hasOptionParameters)
                {
                    OutputFilePostfix = optionParametersString;
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("r"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if (optionParameters.Length == 1)
                    {
                        if (int.TryParse(optionParameters[0], out int r1Value))
                        {
                            switch (r1Value)
                            {
                                case 1:
                                    ReverseMethod = ReverseMethodEnum.Before;
                                    break;
                                case 2:
                                    ReverseMethod = ReverseMethodEnum.After;
                                    break;
                                default:
                                    throw new ToolException("Invalid parameter value for option r1: " + optionParameters);
                            }
                        }
                        else
                        {
                            throw new ToolException("Parameter for option 'r' should be an integer value 1 or 2: " + optionParameters);
                        }
                    }
                    else
                    {
                        throw new ToolException("For option 'r' only one parameter is allowed: " + optionParameters);
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

            // Create output path if not yet existing
            string outputFolder = OutputPath;
            if (!Path.GetExtension(OutputPath).Equals(string.Empty))
            {
                outputFolder = Path.GetDirectoryName(OutputPath);
            }
            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
            }

            if (GENFilename != null)
            {
                if (!File.Exists(GENFilename))
                {
                    throw new ToolException("Specified GEN-file does not exist: " + GENFilename);
                }
            }

            // Check specified input
            if (TopLevelString != null)
            {
                if (!IsValidLevelString(TopLevelString))
                {
                    throw new ToolException("Level1 is not a floating point or existing IDF-file: " + TopLevelString);
                }
            }
            if (BotLevelString != null)
            {
                if (!IsValidLevelString(BotLevelString))
                {
                    throw new ToolException("Level2 is not a floating point or existing IDF-file: " + BotLevelString);
                }
            }

            if (!MinTravelTime.Equals(float.NaN))
            {
                if (MinTravelTime < 0)
                {
                    throw new ToolException("Minimum travel time cannot be negative: " + MinTravelTime);
                }
                if (MinTravelTime >= MaxTravelTime)
                {
                    throw new ToolException("Minimum travel (" + MinTravelTime + ") should be less than Maximum travel time (" + MaxTravelTime + ")");
                }
            }
            if (!MinVelocity.Equals(float.NaN))
            {
                if (MinVelocity < 0)
                {
                    throw new ToolException("Minimum velocity cannot be negative: " + MinVelocity);
                }
                if (MinVelocity >= MaxVelocity)
                {
                    throw new ToolException("Minimum velocity (" + MinVelocity + ") should be less than Maximum velocity (" + MaxVelocity + ")");
                }
            }
            if ((SelectPointType != SelectPointType.Undefined) && (SelectFlowLinesMethod != SelectFlowLinesMethod.Undefined))
            {
                throw new ToolException("Options c and s cannot be specified together");
            }
        }

        private bool IsValidLevelString(string levelString)
        {
            if (!float.TryParse(levelString, NumberStyles.Float, EnglishCultureInfo, out float value))
            {
                if (!File.Exists(levelString))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
