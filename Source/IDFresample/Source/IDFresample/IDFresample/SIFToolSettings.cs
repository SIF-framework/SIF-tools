// IDFresample is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFresample.
// 
// IDFresample is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFresample is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFresample. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.IDFresample
{
    public enum ResampleMethod
    {
        NearestNeighbor,
        IDW,
        MinimumValue,
        MaximumValue,
        MeanValue,
        PercentileValue,
    }

    public enum ConflictMethod
    {
        MinimumValue,
        MaximumValue,
        ArithmeticAverage,
        HarmonicAverage
    }

    public enum DebugMode
    {
        None,
        Local,
        Global,
        All
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

        public ResampleMethod ResampleMethod { get; set; }
        public ConflictMethod ResampleNNConflictMethod { get; set; }
        public float ResampleIDWPower { get; set; }
        public float ResampleIDWSmoothingFactor { get; set; }
        public float ResampleIDWDistance { get; set; }
        public int ResamplePercentile { get; set; }
        public int ResampleStatDistance { get; set; }

        public string ZoneFilename { get; set; }
        public bool SkipDiagonalProcessing { get; set; }
        public DebugMode DebugMode { get; set; }

        /// <summary>
        /// Number of cells around visited cell to calculate statistic for; use 0 for all cells in zone
        /// </summary>
        public string DebugSubZoneId { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            OutputPath = null;
            ResampleMethod = ResampleMethod.NearestNeighbor;
            ResampleNNConflictMethod = ConflictMethod.ArithmeticAverage;
            ResampleIDWPower = 2;
            ResampleIDWSmoothingFactor = 0;
            ResampleIDWDistance = float.NaN;
            ResamplePercentile = -1;
            SkipDiagonalProcessing = false;
            DebugMode = DebugMode.None;
            DebugSubZoneId = null;
            ResampleStatDistance = 0;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for input IDF-files", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input value IDF-file(s) (e.g. *.IDF)", "*.IDF");
            AddToolParameterDescription("outPath", "Path to write results or  filename of resultfile when filter refers to a single value file", "C:\\Test\\Output");
            AddToolOptionDescription("m", "Define method m1 for resampling:\n" +
                                          "1: nearest neighbor (default): resamples NoData-values per region with value from nearest neighbor in input\n" +
                                          "   IDF-file; optionally define method m2 to handle multiple cells at same distance:\n" +
                                          "   1: arithmetic average (default); 2: harmonic average; 3: minimum value; 4: maximum value\n" +
                                          "2: Inverse Distance Weighted (IDW) interpolation with power m2, smoothing factor m3 and max. distance m4 (m)\n" +
                                          "   without a max. distance all Data-points in/around region are used to interpolate\n" +
                                          "   IDW resamples NoData-values per region with IDW-interpolated value from non-NoData-cells in region.\n" +
                                          "   If m2 is high and m3 is 0, interpolation changes a lot around points to give them their exact value.\n" +
                                          "   If m2 is 1 and m3 is high, results are much smoother, but pointvalues are not maintained.\n" +
                                          "   Default values for m2, m3 and m4: are 2, 0 and Infinite (no maximum).\n" +
                                          "   Note: IDFresample gets slow when zones contain large numbers of cells with Data-values.\n" + 
                                          "3: minimum zone value; 4: maximum zone value; 5: average zone value;\n" +
                                          "   for method 3, 4 and 5 a local window (with width/height 2xm2+1) can be defined for local statistics\n" +
                                          "6: percentile in zone values, with m2 an integer (value 0-100) to define the percentile;\n" +
                                          "   a local window (with width/height 2xm3+1) can be defined for local statistics\n" +
                                          "with methods 3-6 all cells in each zone are overwritten with the calculated statistic.\n" + 
                                          "Notes:\n" +
                                          " - a zone is group of one or more cells with the zame zone value\n" +
                                          " - a region is a group of connected cells in a specific zone\n" +
                                          " - it is advised to specify a zone IDF-file via option z to define resampled cells; when no zone is defined,\n" +
                                          "   a single zone is created with NoData-cells for method 1-2 and with Data-cells for methods 3-6",
                                          "/m:2", "Specified method for resampling: {0} ({1}{2}{3})", new string[] { "m1" }, new string[] { "m2", "m3", "m4" }, new string[] { string.Empty, string.Empty, string.Empty });
            AddToolOptionDescription("z", "Define zonefile to define resampled zone and clip result to. Each zone value is resampled individually.", "/z:zonefile.IDF", "Zonefile is defined for resampling zone: {0}", new string[] { "z1" });
            AddToolOptionDescription("d", "Prevent diagonal connections (resample, connectivity) and connect cells only horizontally/vertically)", "/d", "Cells are NOT checked diagonally");
            AddToolOptionDescription("debug", "Run in debug mode: 1 (All), 2 (Global, default), 3 (Local); optionally specify subzone id to debug", null, "Running in debug mode {0}", null, new string[] { "m", "sz" }, new string[] { "Global", "all" } );

            AddToolHelpArgString("Resulting IDF-file will get extent of zone IDF-file and cellsize and NoData-value of value IDF-file.");
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
            switch (optionName)
            {
                case "m":
                    switch (parameter)
                    {
                        case "m1":
                            switch (parameterValue)
                            {
                                case "1": return "Nearest Neighbour";
                                case "2": return "Inverse Distance Weighted";
                                case "3": return "minimum value";
                                case "4": return "maximum value";
                                case "5": return "average value";
                                case "6": return "percentile value";
                                default: return parameterValue;
                            }
                        case "m2":
                            if (parameterValues[0].Equals("1"))
                            {
                                switch (parameterValue)
                                {
                                    case "1": return "conflict method: arithmetic average";
                                    case "2": return "conflict method: harmonic average";
                                    case "3": return "conflict method: minimum value";
                                    case "4": return "conflict method: maximmum value";
                                    default: return parameterValue;
                                }
                            }
                            else if (parameterValues[0].Equals("2"))
                            {
                                return "power: " + parameterValue;
                            }
                            else if (parameterValues[0].Equals("3") || parameterValues[0].Equals("4") || parameterValues[0].Equals("5"))
                            {
                                if (parameterValue.Equals(string.Empty))
                                {
                                    return "no max cell distance";
                                }
                                else
                                {
                                    return "max cell distance: " + parameterValue;
                                }
                            }
                            else if (parameterValues[0].Equals("6"))
                            {
                                return parameterValue + "%";
                            }
                            else
                            {
                                return parameterValue;
                            }
                        case "m3":
                            if (parameterValues[0].Equals("2"))
                            {
                                return ", smoothing: " + parameterValue;
                            }
                            else if (parameterValues[0].Equals("6"))
                            {
                                if (parameterValue.Equals(string.Empty))
                                {
                                    return "no max cell distance";
                                }
                                else
                                {
                                    return "max cell distance: " + parameterValue;
                                }
                            }
                            return parameterValue;
                        case "m4":
                            if (parameterValues[0].Equals("2"))
                            {
                                return ", distance: " + parameterValue;
                            }
                            return parameterValue;
                        default: return parameterValue;
                    }
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
                if (OutputPath.ToLower().EndsWith(".idf"))
                {
                    OutputFilename = Path.GetFileName(OutputPath);
                    OutputPath = Path.GetDirectoryName(OutputPath);
                }
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
                SkipDiagonalProcessing = true;
            }
            else if (optionName.ToLower().Equals("m"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameterStrings = GetOptionParameters(optionParametersString);
                    if (!int.TryParse(optionParameterStrings[0], out int optionValue))
                    {
                        throw new ToolException("Could not parse values for option '" + optionName + "':" + optionParameterStrings[0]);
                    }
                    switch (optionValue)
                    {
                        case 1:
                            ResampleMethod = ResampleMethod.NearestNeighbor;
                            break;
                        case 2:
                            ResampleMethod = ResampleMethod.IDW;
                            break;
                        case 3:
                            ResampleMethod = ResampleMethod.MinimumValue;
                            break;
                        case 4:
                            ResampleMethod = ResampleMethod.MaximumValue;
                            break;
                        case 5:
                            ResampleMethod = ResampleMethod.MeanValue;
                            break;
                        case 6:
                            ResampleMethod = ResampleMethod.PercentileValue;
                            break;
                        default:
                            throw new Exception("Undefined method: " + ResampleMethod.ToString());
                    }

                    if (optionParameterStrings.Length > 1)
                    {
                        // Check value of option parameter m2 depending on value for m1
                        switch (optionValue)
                        {
                            case 1: // nearest neighbor
                                if (int.TryParse(optionParameterStrings[1], out int methodNumber))
                                {
                                    switch (methodNumber)
                                    {
                                        case 1:
                                            ResampleNNConflictMethod = ConflictMethod.ArithmeticAverage;
                                            break;
                                        case 2:
                                            ResampleNNConflictMethod = ConflictMethod.HarmonicAverage;
                                            break;
                                        case 3:
                                            ResampleNNConflictMethod = ConflictMethod.MinimumValue;
                                            break;
                                        case 4:
                                            ResampleNNConflictMethod = ConflictMethod.MaximumValue;
                                            break;
                                        default:
                                            throw new ToolException("Please specify a valid conflict method m2 (number 1-4) for option '" + optionName + "' and nearest neighbor method (m1): " + optionParameterStrings[1]);
                                    }
                                }
                                else
                                {
                                    throw new ToolException("Please specify a valid conflict method number (1-4) for option '" + optionName + "' and nearest neighbor method (m1): " + optionParameterStrings[1]);
                                }
                                break;
                            case 2: // IDW
                                if (!float.TryParse(optionParameterStrings[1], NumberStyles.Float, EnglishCultureInfo, out float power))
                                {
                                    throw new ToolException("Please specify a valid power value for option '" + optionName + "' and IDW method (m1): " + optionParameterStrings[1]);
                                }
                                ResampleIDWPower = power;
                                if (optionParameterStrings.Length > 2)
                                {
                                    if (!float.TryParse(optionParameterStrings[2], NumberStyles.Float, EnglishCultureInfo, out float smoothing))
                                    {
                                        throw new ToolException("Please specify a valid smoothing value for option '" + optionName + "' and IDW method (m1): " + optionParameterStrings[2]);
                                    }
                                    ResampleIDWSmoothingFactor = smoothing;
                                }
                                if (optionParameterStrings.Length > 3)
                                {
                                    if (!float.TryParse(optionParameterStrings[3], NumberStyles.Float, EnglishCultureInfo, out float distance))
                                    {
                                        throw new ToolException("Please specify a valid distance for option '" + optionName + "' and IDW method (m1): " + optionParameterStrings[3]);
                                    }
                                    ResampleIDWDistance = distance;
                                }
                                break;
                            case 3:
                            case 4:
                            case 5:
                                if (!int.TryParse(optionParameterStrings[1], out int distance1) || (distance1 < 0))
                                {
                                    throw new ToolException("Please specify a valid cell distance for option '" + optionName + "': " + optionParameterStrings[1]);
                                }
                                ResampleStatDistance = distance1;
                                break;
                            case 6: // Percentile value
                                if (!int.TryParse(optionParameterStrings[1], out int percentileValue) || (percentileValue < 0) || (percentileValue > 100))
                                {
                                    throw new ToolException("Please specify a valid conflict method number (0-100) for option '" + optionName + "': " + optionParameterStrings[1]);
                                }
                                ResamplePercentile = percentileValue;
                                if (optionParameterStrings.Length > 2)
                                {
                                    if (!int.TryParse(optionParameterStrings[2], out int distance2) || (distance2 < 0))
                                    {
                                        throw new ToolException("Please specify a valid cell distance for option '" + optionName + "': " + optionParameterStrings[2]);
                                    }
                                    ResampleStatDistance = distance2;
                                }
                                break;
                            default:
                                throw new Exception("Unexpected method m1 for value m2: " + optionValue);
                        }
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("z"))
            {
                if (hasOptionParameters)
                {
                    ZoneFilename = optionParametersString;
                }
                else
                {
                    throw new ToolException("Please specify an zone IDF-file for option 'z'");
                }
            }
            else if (optionName.ToLower().Equals("debug"))
            {
                int methodNumber;
                if (hasOptionParameters)
                {
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    if (int.TryParse(optionParameters[0], out methodNumber))
                    {
                        switch (methodNumber)
                        {
                            case 0:
                                DebugMode = DebugMode.None;
                                break;
                            case 1:
                                DebugMode = DebugMode.All;
                                break;
                            case 2:
                                DebugMode = DebugMode.Global;
                                break;
                            case 3:
                                DebugMode = DebugMode.Local;
                                break;
                            default:
                                throw new ToolException("Please specify a valid debug mode (1-3) for option 'debug':" + optionParameters[0]);
                        }
                    }
                    else
                    {
                        throw new ToolException("Please specify a valid debug mode number (1-3) for option 'debug':" + optionParameters[0]);
                    }

                    if (optionParameters.Length > 1)
                    {
                        DebugSubZoneId = optionParameters[1];
                    }
                }
                else
                {
                    DebugMode = DebugMode.Global;
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
            if ((InputFilter != null) && InputFilter.Equals(string.Empty))
            {
                // Specify default
                InputFilter = "*.IDF";
            }
            if ((InputFilter != null) && !Path.GetExtension(InputFilter).ToLower().Equals(".idf"))
            {
                throw new ToolException("Input filter for value file(s) should have IDF-extension: " + InputFilter);
            }

            // Check tool option values
            if ((ZoneFilename != null) && !File.Exists(ZoneFilename))
            {
                throw new ToolException("Zone IDF-file does not exist: " + ZoneFilename);
            }
            if ((ZoneFilename != null) && !Path.GetExtension(ZoneFilename).ToLower().Equals(".idf"))
            {
                throw new ToolException("Zone file should be an IDF-file: " + ZoneFilename);
            }

            if (ResampleMethod == ResampleMethod.PercentileValue)
            {
                if (ResamplePercentile < 0)
                {
                    throw new ToolException("Missing percentile value m2 (0-100) for percentile resample method");
                }
            }
        }
    }
}
