// HydroMonitorIPFconvert is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of HydroMonitorIPFconvert.
// 
// HydroMonitorIPFconvert is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HydroMonitorIPFconvert is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HydroMonitorIPFconvert. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;

namespace Sweco.SIF.HydroMonitorIPFconvert
{
    /// <summary>
    /// Method for interpreting first volume in timeseries when converting from volume to volume flow rate
    /// </summary>
    public enum VolumeFirstVolumeMethod
    {
        /// <summary>
        /// Ignore volume of first timestamp
        /// </summary>
        IgnoreFirstVolume,
        /// <summary>
        /// Add one timestamp using first defined interval and volume of first timestamp (or 0 for NaN-value)
        /// </summary>
        CopyInterval,
    }

    /// <summary>
    /// Method for interpreting last date in timeseries when converting from volume to volume flow rate
    /// </summary>
    public enum VolumeEndDateMethod
    {
        /// <summary>
        /// Use rate 0 for last timestamp in resulting IPF-timeseries
        /// </summary>
        UseZeroRate,
        /// <summary>
        /// Copy rate value of previous timestamp in resulting IPF-timeseries
        /// </summary>
        UsePrevRate,
        /// <summary>
        /// Use NaN-value for last timestamp in resulting IPF-timeseries
        /// </summary>
        UseNaNRate,
        /// <summary>
        /// Do not copy the last timestamp to resulting IPF-timeseries
        /// </summary>
        SkipEndDate,
    }

    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public const string TSFilename = "TSFilename";
        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public string OutputPath { get; set; }
        public bool IsRecursive { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public Extent Extent { get; set; }

        /// <summary>
        /// Either a sheetname or (one-based) sheet index
        /// </summary>
        public string ExcelSheetId { get; set; }

        public bool IsCleaned { get; set; }
        public List<string> ResultColumnNames { get; set; }

        public VolumeFirstVolumeMethod VolumeFirstVolumeMethod;
        public VolumeEndDateMethod VolumeEndDateMethod;

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            InputFilter = null;
            OutputPath = null;
            IsRecursive = false;
            ExcelSheetId = "1";
            IsCleaned = true;
            ResultColumnNames = null;
            StartDate = null;
            EndDate = null;
            VolumeFirstVolumeMethod = VolumeFirstVolumeMethod.IgnoreFirstVolume;
            VolumeEndDateMethod = VolumeEndDateMethod.UseZeroRate;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for input files", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input files (e.g. *.xlsx)", "*.xlsx");
            AddToolParameterDescription("outPath", "Path to write results", "C:\\Test\\Output");

            AddToolOptionDescription("e", "Clip HydroMonitor points within specified extent (xll,yll,xur,yur)", "/e:184000,352500,200500,371000",
                                          "Clip extent defined: {0},{1},{2},{3}", new string[] { "xll", "yll", "xur", "yur" });
            AddToolOptionDescription("r", "Process input path recursively", "/r", "Input path is searched recursively");
            AddToolOptionDescription("s", "Sheet in input Excel files to process, define with name or (one-based) sheet index i", "/s:2", "Processed Excelsheet(s): {0}", new string[] { "i" });
            AddToolOptionDescription("p", "Period to clip/extend (with 0-values) timeseries to, defined d1 and d2 (format: yyyymmdd)\n"
                + "Note: when clipping, date before/after specified period are kept when exact period dates not available", "/p:20100101,20201231", "Period to clip/extend timeseries: {0} - {1}", new string[] { "d1" }, new string[] { "d2" }, new string[] { "" });
            AddToolOptionDescription("v", "Specify volume to volume flow rate conversion method with parameters v1 and v2:\n"
                + "v1=0: ignore volume of first timestamp (default);\n" 
                + "v1=1: add one timestamp using first defined interval and volume of first timestamp (or 0 for NaN-value)\n"
                + "v2=0: for last timestamp use rate 0 (default)\n"
                + "v2=1: for last timestamp copy rate value of previous timestamp\n"
                + "v2=2: do not copy the last timestamp to resulting IPF-timeseries\n"
                + "Note: volume in HydroMonitor-timeseries is defined as withdrawn (positive) flux over timestep:\n"
                + "      volume v(t) it the total volume (m3) between timestmaps d(t-1) and d(t), and is converted to\n"
                + "      (negative) rate (m3/d) in IPF-timeseries", null, "Volume method: {0}", new string[] { "v1", "v2" });
            AddToolOptionDescription("c", "Clean when no option parameters are specified: remove columns without values, or\n"
                + "specify comma-seperated columnnames or -numbers (one-based) from input HydroMonitor-file to export\n"
                + "in this order. Note: XY-column will always be used for first two columns.", "/c", "Clean/columns option is specified: {...}", null, new string[] { "..." });
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
            else if (optionName.ToLower().Equals("r"))
            {
                IsRecursive = true;
            }
            else if (optionName.ToLower().Equals("s"))
            {
                if (hasOptionParameters)
                {
                    ExcelSheetId = optionParametersString;
                }
                else
                {
                    throw new ToolException("Missing sheet id string for option 's'");
                }
            }
            else if (optionName.ToLower().Equals("c"))
            {
                if (hasOptionParameters)
                {
                    ResultColumnNames = new List<string>(GetOptionParameters(optionParametersString));
                }
                else
                {
                    IsCleaned = true;
                }
            }
            else if (optionName.ToLower().Equals("v"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    switch (optionParameters[0])
                    {
                        case "0":
                            VolumeFirstVolumeMethod = VolumeFirstVolumeMethod.IgnoreFirstVolume;
                            break;
                        case "1":
                            VolumeFirstVolumeMethod = VolumeFirstVolumeMethod.CopyInterval;
                            break;
                        default:
                            throw new ToolException("Invalid method for first volume: " + optionParameters[0]);
                    }
                    if (optionParameters.Length > 1)
                    {
                        switch (optionParameters[1])
                        {
                            case "0":
                                VolumeEndDateMethod = VolumeEndDateMethod.UseZeroRate;
                                break;
                            case "1":
                                VolumeEndDateMethod = VolumeEndDateMethod.UsePrevRate;
                                break;
                            case "2":
                                VolumeEndDateMethod = VolumeEndDateMethod.SkipEndDate;
                                break;
                            default:
                                throw new ToolException("Invalid method for last date: " + optionParameters[1]);
                        }
                    }
                }
                else
                {
                    IsCleaned = true;
                }
            }
            else if (optionName.ToLower().Equals("p"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    StartDate = null;
                    EndDate = null;

                    string startDateString = optionParameters[0].Trim();
                    if (!startDateString.Equals(string.Empty))
                    {
                        if (IsDateTime(startDateString))
                        {
                            StartDate = (DateTime)ParseDateTime(optionParameters[0]);
                        }
                        else
                        {
                            throw new ToolException("Invalid startdate for option 'p': " + optionParameters[0]);
                        }
                    }
                    if (optionParameters.Length > 1)
                    {
                        string endDateString = optionParameters[1].Trim();
                        if (!endDateString.Equals(string.Empty))
                        {
                            if (IsDateTime(optionParameters[1]))
                            {
                                EndDate = (DateTime)ParseDateTime(optionParameters[1]);
                            }
                            else
                            {
                                throw new ToolException("Invalid enddate for option 'p': " + optionParameters[1]);
                            }
                        }
                    }
                }
                else
                {
                    throw new ToolException("Missing date(s) for option 'p': " + optionParametersString);
                }
            }
            else
            {
                // specified option could not be parsed
                return false;
            }

            return true;
        }

        public DateTime? ParseDateTime(string dateString, string format)
        {
            try
            {
                IFormatProvider dateProvider = DateTimeFormatInfo.InvariantInfo; 
                return DateTime.ParseExact(dateString, format, dateProvider, DateTimeStyles.None);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Parse specified string to date/time using format 'yyyymmdd', 'dd-mm-yyyy' or 'dd/mm/yyyy' in this order. 
        /// </summary>
        /// <param name="dateTimeString"></param>
        /// <returns>null if no valid date/time string</returns>
        public DateTime? ParseDateTime(string dateTimeString)
        {
            DateTime? datetime = ParseDateTime(dateTimeString, "yyyyMMdd");
            if (datetime == null)
            {
                datetime = ParseDateTime(dateTimeString, "dd-MM-yyyy");
            }
            if (datetime == null)
            {
                datetime = ParseDateTime(dateTimeString, "dd/MM/yyyy");
            }
            return datetime;
        }

        /// <summary>
        /// Checks if specified string contains a date with specified format. Use format as defined for c# DateTime class, e.g. 'ddmmyyyy'.
        /// </summary>
        /// <param name="dateString"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public bool IsDateTime(string dateString, string format)
        {
            return ParseDateTime(dateString, format) != null;
        }

        /// <summary>
        /// Checks if specified string contains a date/time in format 'yyyymmdd', 'dd-mm-yyyy' or 'dd/mm/yyyy' in this order.
        /// </summary>
        /// <param name="dateString"></param>
        /// <returns></returns>
        public bool IsDateTime(string dateString)
        {
            return ParseDateTime(dateString) != null;
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
                InputFilter = "*.xlsx";
            }
        }
    }
}
