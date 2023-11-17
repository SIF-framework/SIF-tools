// ISDcreate is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of ISDcreate.
// 
// ISDcreate is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// ISDcreate is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with ISDcreate. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD;

namespace Sweco.SIF.ISDcreate
{
    public enum ShapeFileType
    {
        Unknown,
        IPF,
        GEN
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

        public int IdColIdx { get; set; }
        public bool IsRecursive { get; set; }

        public float N1 { get; set; }
        public float N2 { get; set; }
        public string TopDefinition { get; set; }
        public string BotDefinition { get; set; }
        public int VIN { get; set; }

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
            N1 = float.NaN;
            N2 = float.NaN;
            TopDefinition = null;
            BotDefinition = null;
            VIN = 0;

            IsRecursive = false;
            IdColIdx = -1;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for input files", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input IPF/GEN-file(s) that define location(s)/feature(s) for startpoints (e.g. *.IPF)", "*.IPF");
            AddToolParameterDescription("n1", "shape number 1, integer or float:\n" +
                                        "  for points: radius of circle around point\n" +
                                        "  for polygons: distance X between points in the polygon\n" +
                                        "  for lines: distance between points along the line", "5");
            AddToolParameterDescription("n2", "shape number 2, integer or float:\n" +
                                        "  for points: dinstance between points on the circle\n" +
                                        "  for polygons: distance Y between points in the polygon\n" +
                                        "  for lines: not used, use any number", "5");
            AddToolParameterDescription("top", "IDF-file, numeric value or columnname in shpFile for TOP-level", "Input\\top.IDF");
            AddToolParameterDescription("bot", "IDF-file, numeric value or columnname in shpFile for BOT-level", "Input\\bot.IDF");
            AddToolParameterDescription("vin", "Vertical interval number, number of points between TOP and BOT-level", "10");
            AddToolParameterDescription("outPath", "Path or ISD-filename to write results", "C:\\Test\\Output");

            AddToolOptionDescription("r", "Process input path recursively", null, "Input files are searched recursively");
            AddToolOptionDescription("i", "ID-columnnumber (one based) in IPF- or GEN-file", "/i:5", "ID-columnnumber: {0}", new string[] { "c" });
        }

        /// <summary>
        /// Parse and process obligatory tool parameter at (zero based) index parIndex
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex">returns the index for the argument group for these parameters, 0 if only a single group is defined</param>
        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length == 8)
            {
                // Parse syntax 1:
                InputPath = parameters[0];
                InputFilter = parameters[1];
                string n1String = parameters[2];
                string n2String = parameters[3];
                TopDefinition = parameters[4];
                BotDefinition = parameters[5];
                string VINString = parameters[6];
                OutputPath = parameters[7];

                // Parse and check parameters
                if (Path.GetExtension(OutputPath).ToLower().Equals(".isd"))
                {
                    OutputFilename = Path.GetFileName(OutputPath);
                    OutputPath = Path.GetDirectoryName(OutputPath);
                }
                else
                {
                    // Leave null for now
                    OutputFilename = null;
                }

                if (float.TryParse(n1String, NumberStyles.Float, EnglishCultureInfo, out float n1))
                {
                    this.N1 = n1;
                }
                else
                {
                    throw new ToolException("Invalid value for n1: " + n1String);
                }

                if (float.TryParse(n2String, NumberStyles.Float, EnglishCultureInfo, out float n2))
                {
                    this.N2 = n2;
                }
                else
                {
                    throw new ToolException("Invalid value for n2: " + n2String);
                }

                if (int.TryParse(VINString, out int vin))
                {
                    VIN = vin;
                }
                else
                {
                    throw new ToolException("Invalid vertical interval number: " + VINString);
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
            if (optionName.ToLower().Equals("r"))
            {
                IsRecursive = true;
            }
            else if (optionName.ToLower().Equals("i"))
            {
                if (hasOptionParameters)
                {
                    // retrieve part after colon
                    if (!int.TryParse(optionParametersString, out int colNr))
                    {
                        throw new ToolException("Could not parse ID-columnnumber for option '" + optionName + "':" + optionParametersString);
                    }
                    IdColIdx = colNr - 1;

                    if (IdColIdx < 0)
                    {
                        throw new ToolException("ID-columnnumber should be larger than zero: " + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Please specify parameter after '" + optionName + ":'");
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
                throw new ToolException("An input filter should be specified");
            }

            if (InputPath.Equals(OutputPath))
            {
                throw new ToolException("Input path cannot be equal to output path");
            }
        }
    }
}
