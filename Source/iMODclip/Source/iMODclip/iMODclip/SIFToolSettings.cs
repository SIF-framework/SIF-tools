// iMODclip is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODclip.
// 
// iMODclip is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODclip is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODclip. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;

namespace Sweco.SIF.iMODclip
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string InputPath { get; set; }
        public string OutputPath { get; set; }
        public Extent Extent { get; set; }

        public bool IsRecursive { get; set; }
        public bool IsOverwrite { get; set; }
        public bool IsKeepFileDateTime { get; set; }
        public bool IsNoDataGridSkipped { get; set; }
        public int EmptyFileMethod { get; set; }
        public List<string> SkippedClipSubstrings { get; set; }
        public List<string> SkippedCopySubstrings { get; set; }
        public List<string> SkippedExtensions { get; set; }
        public bool IsContinueOnErrors { get; set; }
        public bool IsDebugMode { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            OutputPath = null;
            Extent = null;

            IsRecursive = false;
            IsOverwrite = false;
            IsKeepFileDateTime = false;
            IsNoDataGridSkipped = false;
            EmptyFileMethod = -1;
            SkippedClipSubstrings = new List<string>();
            SkippedCopySubstrings = new List<string>();
            SkippedExtensions = new List<string>();
            IsContinueOnErrors = true;
            IsDebugMode = false;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to directory to search for input files or path to a single filename", "C:\\Test\\Input", new int[] { 0, 1 });
            AddToolParameterDescription("outPath", "Path to write results", "C:\\Test\\Output", new int[] { 0, 1 });
            AddToolParameterDescription("xll", "lower left x-coordinate of clipextent", "181000.0", 0);
            AddToolParameterDescription("yll", "lower left y-coordinate of clipextent", "360000.0", 0);
            AddToolParameterDescription("xur", "upper right x-coordinate of clipextent", "222500.0", 0);
            AddToolParameterDescription("yur", "upper right y-coordinate of clipextent", "401000.0", 0);
            AddToolOptionDescription("k", "keep original date and time", "/k", "Original date/time is kept", null, null, null, new int[] { 0, 1 });
            AddToolOptionDescription("o", "overwrite existing output files", "/o", "Existing files are overwritten", null, null, null, new int[] { 0, 1 });
            AddToolOptionDescription("r", "process input path recursively", "/r", "Subdirectories are processed recursively", null, null, null, new int[] { 0, 1 });
            AddToolOptionDescription("s",  "skip clipping for files with any of specified (comma-seperated) list of substrings in path or filename\n" + 
                                           "note: files will be copied instead", "/s:SHAPES", "Skip clipping files with any of the following substrings in path (files are copied): {...}", new string[] { "s1" }, new string[] { "..." }, null, new int[] { 0, 1 });
            AddToolOptionDescription("sc", "skip copying for files with any of specified (comma-seperated) list of substrings in path or filename\n" +
                                           "if no substrings are specified all copying is skipped"
#if DEBUG
                                            + ". Note: skipped files are not logged/shown."
#endif
                                           , "/sc", "Skip copying files with any of the following substrings in path: {...}", null, new string[] { "..." }, null, new int[] { 0, 1 });
            AddToolOptionDescription("sx", "skip files with specified extensions (these are not clipped/copied, case insensitive)", "/x:IDF,ASC", "Exclude files with following extensions: {0}", new string[] { "x1" }, new string[] { "x2", "..." }, null, new int[] { 0, 1 });
            AddToolOptionDescription("e", "define with value i how to handle empty (clippped) folders/iMOD-files (default: 0):\n"
                                        + "0: skip empty folder; write empty (clipped) iMOD-file (for ASC/IDF: NoData within clipextent)\n"
                                        + "1: as 0, but write empty folder(s)\n"
                                        + "2: skip empty folders/iMOD-files\n"
                                        + "3: copy complete source file when extent is completey outside clipextent", "/e:3", "Method for handling empty files/folders: {0}", new string[] { "i" }, null, null, new int[] { 0, 1 });
            AddToolOptionDescription("n", "skip IDF- or ASC-files with only NoData in specified extent\nthis option overrules method 0 of option e", null, "IDF/ASC-files with only NoData in extent are skipped");
            AddToolOptionDescription("err", "stop when an error occurs, instead of continuing and copying file(s)", "/err", "Clipping is stopped when an error occurs", null, null, null, new int[] { 0, 1 });
            AddToolOptionDescription("debug", "Log debug info while processing", "/debug", "Tool is run in debug mode", null, null, null, new int[] { 0, 1 });

            AddToolUsageOptionPostRemark("Note: if xll,yll,xur,yur are not specified, extent of input file is used and specified files are copied (including related files like MET- or TXT-files)");
            AddToolUsageOptionPostRemark("Note: ISG-files are currently skipped (if extent has no overlap) or copied completely (if extent has some overlap)");
        }

        /// <summary>
        /// Parse and process obligatory tool parameters
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex">returns the index for the argument group for these parameters, 0 if only a single group is defined</param>
        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length == 6)
            {
                groupIndex = 0;

                // Parse syntax 1:
                InputPath = parameters[0];
                OutputPath = parameters[1];
                string llxString = parameters[2];
                string llyString = parameters[3];
                string urxString = parameters[4];
                string uryString = parameters[5];

                Extent = new Extent();
                try
                {
                    Extent.llx = float.Parse(llxString, EnglishCultureInfo);
                    Extent.lly = float.Parse(llyString, EnglishCultureInfo);
                    Extent.urx = float.Parse(urxString, EnglishCultureInfo);
                    Extent.ury = float.Parse(uryString, EnglishCultureInfo);
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not parse input clip extent:" + llxString + " " + llyString + " " + urxString + " " + uryString, ex);
                }
            }
            else if (parameters.Length == 2)
            {
                groupIndex = 1;

                // Parse syntax 2:
                InputPath = parameters[0];
                OutputPath = parameters[1];
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
                case "e":
                    switch (parameter)
                    {
                        case "i":
                            switch (parameterValue)
                            {
                                case "0":
                                    return "skip empty folder; write empty (clipped) iMOD-file";
                                case "1":
                                    return "write empty folder; write empty (clipped) iMOD-file";
                                case "2":
                                    return "skip empty folder; skip empty (clipped) iMOD-file";
                                case "3":
                                    return "copy source file if completely outside extent";
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
            if (optionName.ToLower().Equals("r"))
            {
                IsRecursive = true;
            }
            else if (optionName.ToLower().Equals("o"))
            {
                IsOverwrite = true;
            }
            else if (optionName.ToLower().Equals("e"))
            {
                if (hasOptionParameters)
                {
                    if (int.TryParse(optionParametersString, out int value))
                    {
                        EmptyFileMethod = value;
                    }
                }
                else
                {
                    throw new ToolException("Missing method for option '" + optionName + "' after ':");
                }
            }
            else if (optionName.ToLower().Equals("n"))
            {
                IsNoDataGridSkipped = true;
            }
            else if (optionName.ToLower().Equals("k"))
            {
                IsKeepFileDateTime = true;
            }
            else if (optionName.ToLower().Equals("s"))
            {
                if (hasOptionParameters)
                {
                    SkippedClipSubstrings = new List<string>(GetOptionParameters(optionParametersString));
                }
                else
                {
                    throw new ToolException("Missing comma seperated list of extensions for option '" + optionName + "' after ':");
                }
            }
            else if (optionName.ToLower().Equals("sc"))
            {
                if (hasOptionParameters)
                {
                    SkippedCopySubstrings = new List<string>(GetOptionParameters(optionParametersString));
                }
                else
                {
                    throw new ToolException("Missing comma seperated list of extensions for option '" + optionName + "' after ':");
                }
            }
            else if (optionName.ToLower().Equals("sx"))
            {
                if (hasOptionParameters)
                {
                    SkippedExtensions = new List<string>(GetOptionParameters(optionParametersString));
                }
                else
                {
                    throw new ToolException("Missing comma seperated list of extensions for option '" + optionName + "' after ':");
                }
            }
            else if (optionName.ToLower().Equals("err"))
            {
                IsContinueOnErrors = false;
            }
            else if (optionName.ToLower().Equals("debug"))
            {
                IsDebugMode = true;
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

                // Check that path refers to an existing directory or file
                if (!Directory.Exists(InputPath))
                {
                    if (!File.Exists(InputPath))
                    {
                        throw new ToolException("Input path does not exist: " + InputPath);
                    }
                }
            }

            // Check tool parameters
            if (Extent != null)
            {
                if (Extent.llx > Extent.urx)
                {
                    throw new Exception("Clip rectangle's lower left x-coordinate (" + Extent.llx + ") is greater than upper right x-coordinate (" + Extent.urx + "). Specifiy a valid extent.");
                }
                if (Extent.lly > Extent.ury)
                {
                    throw new Exception("Clip rectangle's lower left y-coordinate (" + Extent.lly + ") is greater than upper right y-coordinate (" + Extent.ury + "). Specifiy a valid extent.");
                }
            }

            // Check tool option values
            if (EmptyFileMethod == -1)
            {
                // Set default method
                EmptyFileMethod = 0;
            }
            if ((EmptyFileMethod < 0) || (EmptyFileMethod > 3))
            {
                throw new ToolException("Invalid method value for option 'e', check tool info: " + EmptyFileMethod);
            }
        }
    }
}
