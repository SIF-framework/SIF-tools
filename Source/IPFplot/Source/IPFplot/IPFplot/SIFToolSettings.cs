// IPFplot is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IPFplot.
// 
// IPFplot is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IPFplot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IPFplot. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using ZedGraph;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace Sweco.SIF.IPFplot
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public const float DefaultLineSize = 1.0F;
        public const DashStyle DefaultLineType = DashStyle.Solid;
        public const SymbolType DefaultMarkerType = SymbolType.None;
        public const string PlotXAxisFormatString = "yyyy-MM-dd";
        public const string PlotXAxisTitle = "Date";

        public string Plotrefs { get; set; }
        public string OutputPath { get; set; }

        public List<string> IdFormatStrings { get; set; }
        public List<string> SeriesLabels { get; set; }
        public bool IsMissingPointExcluded { get; set; }
        public Size GraphSize { get; set; }
        public List<Color> UserColors { get; set; }
        public List<int> ValueListNumbers { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            Plotrefs = null;
            OutputPath = null;

            IdFormatStrings = null;
            SeriesLabels = null;
            ValueListNumbers = null;
            UserColors = new List<Color>(new Color[] { Color.FromArgb(0, 0, 200), Color.FromArgb(150, 0, 0), Color.FromArgb(0, 0, 150) });
            IsMissingPointExcluded = false;
            GraphSize = new Size(800, 600);
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("plotrefs", "comma-seperated list of references to plotted objects, which can be defined by: \n" +
                                        "  IPF-file; by default first column is taken; \n" +
                                        "  columnnumber (one-based);  \n" +
                                        "  columnname in the first IPF-file; \n" +
                                        "  floating point value; \n" +
                                        "For IPF-files the timeseries of IPF-points in the first IPF-file are shown in seperate plots. \n" +
                                        "The first reference must always be an IPF-file. \n",
                                        "C:\\Test\\Input\\example.IPF");
            AddToolParameterDescription("outPath", "Path to write results", "C:\\Test\\Output");

            AddToolOptionDescription("i", "Comma-seperated list of ID-strings that define an ID. The first ID is used for plot title and filename.\n" +
                                          "ID's of points in other IPF-files are used to match with points in the first IPF-file.\n" +
                                          "Leave reference empty for non-IPF-files. ID-strings can be build up as follows:\n" +
                                          "  when the whole ID-string is a single integer i, the row value in (one-based) column i is used; \n" +
                                          "  '{i}'-substrings with i an integer are replaced with the row value in (one-based) column i; \n" +
                                          "  other non-value characters are simply copied; \n" +
                                          "If no ID's are specified, points are matched by sequence for equal pointcounts (files should be sorted), \n" +
                                          "or by XY-coordinates in combination with equality of all other column values.",
                                          "/i:2", "Specified optional ID column indices", new string[] { "..." });
            AddToolOptionDescription("n", "Specify comma-seperated list of series name per IPF-file to use",
                                          "/n:meting,simulatie", "Series names are defined for IPF-file: {0}", new string[] { "..." });
            AddToolOptionDescription("c", "Specify comma-seperated list of semicolon-seperated RGB-colors for defined series\n" +
                                          "If one series is defined a seperate color for series and average line can be defined",
                                          "/c:200;0;0,0;200;0,0;0;200", "RGB-colors are specified as: {0}", new string[] { "..." });
            AddToolOptionDescription("v", "Specify comma-seperated list with (one-based) valuelist number per IPF-file to use (default: 1)\n" +
                                          "Note: this column number refers to columns in the associated TXT-files",
                                          "/v:1", "Valuelist are defined per IPF-file: {0}", new string[] { "..." });
            AddToolOptionDescription("x", "Exclude plots if one or more of the IPF-points or all values in IPF-timeseries are missing", 
                                          "/x", "Missing points are excluded");
        }

        /// <summary>
        /// Parse and process obligatory tool parameter at (zero based) index parIndex
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex">returns the index for the argument group for these parameters, 0 if only a single group is defined</param>
        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length == 2)
            {
                // Parse syntax 1:
                Plotrefs = parameters[0];
                OutputPath = parameters[1];
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
            if (optionName.ToLower().Equals("x"))
            {
                IsMissingPointExcluded = true;
            }
            else if (optionName.ToLower().Equals("i"))
            {
                if (hasOptionParameters)
                {
                    //split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    IdFormatStrings = new List<string>();
                    try
                    {
                        for (int colNum = 0; colNum < optionParameters.Length; colNum++)
                        {
                            IdFormatStrings.Add(optionParameters[colNum]);
                        }
                    }
                    catch (Exception)
                    {
                        throw new ToolException("Could not parse values for option '" + optionName + "':" + optionParametersString );
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }         
            }
            else if (optionName.ToLower().Equals("n"))
            {
                if (hasOptionParameters)
                {
                    SeriesLabels = new List<string>(GetOptionParameters(optionParametersString));
                }
                else
                {
                    throw new ToolException("Please specify one or more series names after 'n': " + optionName);
                }
            }
            else if (optionName.ToLower().Equals("c"))
            {
                if (hasOptionParameters)
                {
                    //split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    UserColors = new List<Color>();
                    for (int colorIdx = 0; colorIdx < optionParameters.Length; colorIdx++)
                    {
                        string[] valueStrings = optionParameters[colorIdx].Split(new char[] { ';' });
                        if (valueStrings.Length != 3)
                        {
                            throw new ToolException("Invalid colorcount in RGB-string " + colorIdx + " for option c: " + optionParameters[colorIdx]);
                        }
                        try
                        {
                            // Parse substrings for this option
                            int r = int.Parse(valueStrings[0]);
                            int g = int.Parse(valueStrings[1]);
                            int b = int.Parse(valueStrings[2]);
                            UserColors.Add(Color.FromArgb(r, g, b));
                        }
                        catch (Exception)
                        {
                            throw new ToolException("Invalid RGB-string " + colorIdx + " for option c: " + optionParameters[colorIdx]);
                        }
                    }
                }
                else
                {
                    throw new ToolException("Please specify rgb-parameters after 'c:'");
                }
            }
            else if (optionName.ToLower().Equals("v"))
            {
                if (hasOptionParameters)
                {
                    //split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    ValueListNumbers = new List<int>();
                    for (int colNum = 0; colNum < optionParameters.Length; colNum++)
                    {
                        if (int.TryParse(optionParameters[colNum], out int valueColNum))
                        {
                            ValueListNumbers.Add(valueColNum);
                        }
                        else
                        {
                            throw new ToolException("Could not parse value number '" + optionParameters[colNum] + "' for option 'v'");
                        }
                    }
                }
                else
                {
                    throw new ToolException("Please specify valuelist numbers after 'v:': " + optionName);
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
        }
    }
}
