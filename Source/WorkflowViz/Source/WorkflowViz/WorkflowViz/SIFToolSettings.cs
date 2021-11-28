// WorkflowViz is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of WorkflowViz.
// 
// WorkflowViz is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// WorkflowViz is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with WorkflowViz. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.WorkflowViz.Workflows;

namespace Sweco.SIF.WorkflowViz
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string InputPath { get; set; }
        public string OutputPath { get; set; }

        public string DotPath { get; set; }
        public string DotOptions { get; set; }

        public bool IsBatchfileShown { get; set; }
        public bool IsRunScriptsMode { get; set; }
        public int MaxWorkflowLevels { get; set; }
        public int MaxRecursionLevel { get; set; }

        public bool IsResultOpened { get; set; }

        public WorkflowSettings WorkflowSettings { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            OutputPath = null;

            DotPath = Properties.Settings.Default.DotPath;
            DotOptions = null;

            MaxWorkflowLevels = 2;
            MaxRecursionLevel = 1;

            IsBatchfileShown = false;
            IsRunScriptsMode = false;
            
            WorkflowSettings = new WorkflowSettings();
            WorkflowSettings.ExcludedStrings = null;
            WorkflowSettings.OrderStrings = null;
            WorkflowSettings.IsEdgeCheckSkipped = false;

            IsResultOpened = false;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to WORKIN (sub)directory", "C:\\Test\\Input\\Model\\WORKIN\\BASIS1");
            AddToolParameterDescription("outPath", "Path to write resulting WorkflowViz-files\n" +
                                                   "optionally specify output filename and one of following output types (default: HTML):\n" +
                                                   "PNG, GIF, BMP, PDF, SVG, PS, CMAPX, HTML", "C:\\Test\\Output");
            AddToolOptionDescription("vl", "Specify visualization level: maximum number of workflow levels to show in one diagram (default: 2)", null, "Maximum number of visualized workflow levels: {0}", new string[] { "m" });
            AddToolOptionDescription("rl", "Specify recursion level: subworkflow depth to generate diagrams for (default: 1)", "/rl:3", "Maximum number of subworkflows to generate output for: {0}", new string[] { "m" });
            AddToolOptionDescription("bt", "Show batchfiles at highest graph level (default: not shown)", null, "Batchfiles are shown at highest graph level");
            AddToolOptionDescription("ex", "Specify comma-seperated substrings in subworkflow names to exclude from visualization", "/ex:\"Archief,Plot\"", "Substrings in subworkflow names to exclude from visualization: {...}", new string[] { "s" }, new string[] { "..." });
            AddToolOptionDescription("wo", "Specify comma-seperated substrings in subworkflow names that define visualization order", "/ex:\"BASISDATA,BASIS0\"", "Substrings in subworkflow names to define visualization order: {...}", new string[] { "s" }, new string[] { "..." });
            AddToolOptionDescription("rm", "Activate RunscriptsMode: show Runscripts batchfiles above toplevel workflows", null, "RunscriptsMode is active");
            AddToolOptionDescription("se", "Skip inconsistency check for edges (i.e. date order of logfiles and/or subworkflows, as shown by edge color)", null, "Edge check is skipped");
            AddToolOptionDescription("do", "Specify dot options, use complete substring of options in dot command-line, e.g. /do:\"-Gdpi=300 -Tsvg\"", "/do:\"-Gdpi=300 -Tsvg\"", "Specfied dot option string: {0}", new string[] { "s" } );
            AddToolOptionDescription("or", "Open resulting file after succesful tool runs", "/oh", "Result file is opened after succesful tool runs");
            AddToolOptionDescription("dot", "Path to dot.exe from Graphviz package (see http://graphviz.org)", "/dot:%EXEPATH%\\GraphViz\\dot.exe", "Specified path for dot.exe: {0}", new string[] { "s" });
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
                InputPath = parameters[0];
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
            if (optionName.ToLower().Equals("bt"))
            {
                IsBatchfileShown = true;
            }
            else if (optionName.ToLower().Equals("rm"))
            {
                IsRunScriptsMode = true;
            }
            else if (optionName.ToLower().Equals("se"))
            {
                WorkflowSettings.IsEdgeCheckSkipped = true;
            }
            else if (optionName.ToLower().Equals("or"))
            {
                IsResultOpened = true;
            }
            else if (optionName.ToLower().Equals("dot"))
            {
                if (hasOptionParameters)
                {
                    DotPath = optionParametersString;
                }
                else
                {
                    throw new ToolException("Please specify path to dot.exe for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("do"))
            {
                if (hasOptionParameters)
                {
                    DotOptions = optionParametersString;
                    if (DotOptions.Equals(string.Empty))
                    {
                        throw new ToolException("No dot options specified");
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("vl"))
            {
                if (hasOptionParameters)
                {
                    int level;
                    if (!int.TryParse(optionParametersString, out level))
                    {
                        throw new ToolException("Could not parse value for option '" + optionName + "':" + optionParametersString);
                    }
                    MaxWorkflowLevels = level;
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("rl"))
            {
                if (hasOptionParameters)
                {
                    int level;
                    if (!int.TryParse(optionParametersString, out level))
                    {
                        throw new ToolException("Could not parse value for option '" + optionName + "':" + optionParametersString);
                    }
                    MaxRecursionLevel = level;
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("ex"))
            {
                if (hasOptionParameters)
                {
                    // split part after colon to strings seperated by a comma
                    string[] optionParameters = optionParametersString.Replace("\"", string.Empty).Split(',');
                    if (optionParameters.Length == 0)
                    {
                        throw new ToolException("Parameter value expected for option '" + optionName + "'");
                    }
                    else
                    {
                        WorkflowSettings.ExcludedStrings = new List<string>(optionParameters);
                    }
                }
            }
            else if (optionName.ToLower().Equals("wo"))
            {
                if (hasOptionParameters)
                {
                    // split part after colon to strings seperated by a comma
                    string[] optionParameters = optionParametersString.Replace("\"", string.Empty).Split(',');
                    if (optionParameters.Length == 0)
                    {
                        throw new ToolException("Parameter string(s) expected for option '" + optionName + "'");
                    }
                    else
                    {
                        WorkflowSettings.OrderStrings = new List<string>(optionParameters);
                    }
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

            if (OutputPath != null)
            {
                OutputPath = ExpandPathArgument(OutputPath);
            }

            // Check tool option values
            if (MaxWorkflowLevels < 0)
            {
                throw new ToolException("Value 0 or larger expected for option l");
            }

            if (!Path.IsPathRooted(DotPath))
            {
                string exePath = Path.GetDirectoryName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath);
                DotPath = Path.Combine(exePath, DotPath);
            }
            if (!File.Exists(DotPath))
            {
                throw new ToolException("dot.exe not found in specified path: " + DotPath);
            }
        }
    }
}
