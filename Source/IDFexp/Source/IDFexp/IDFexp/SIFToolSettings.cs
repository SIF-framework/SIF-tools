// IDFexp is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFexp.
// 
// IDFexp is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFexp is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFexp. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.IDF;

namespace Sweco.SIF.IDFexp
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string InputFilename { get; set; }
        public string OutputPath { get; set; }

        public bool UseNodataAsValue { get; set; }
        public float NoDataValue { get; set; }
        public Extent Extent { get; set; }
        public bool IsMetadataAdded { get; set; }
        public bool IsDebugMode { get; set; }
        public bool IsQuietMode { get; set; }
        public bool IsResultRounded { get; set; }
        public int DecimalCount { get; set; }
        public bool IsIntermediateResultWritten { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputFilename = null;
            OutputPath = null;

            UseNodataAsValue = false;
            NoDataValue = float.NaN;
            Extent = null;
            IsMetadataAdded = false;
            IsQuietMode = false;
            IsResultRounded = false;
            DecimalCount = -1;
            IsDebugMode = false;
            IsIntermediateResultWritten = false;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("iniFile", "Text-file with one or more expressions as defined below", "C:\\Test\\Input\\Expressions.INI");
            AddToolParameterDescription("outPath", "Path for writing results; if empty (\"\") the path of the cs-script is used.", "C:\\Test\\Output");

            AddToolOptionDescription("e", "Define expression extent: xll,yll,xur,yur or IDF-file. IDF-files are enlarged (with NoData-values) or\n" 
                                        + "clipped to the specified extent. Without option e, extents are corrected in the following cases:\n"
                                        + "- if-expression: if(<cond>,<then>,<else>)\n"
                                        + "  extents are enlarged to union of input IDF-extents\n"
                                        + "- min/max/abs-functions: e.g. max(<exp1>,<exp2>) or abs(<exp>)\n"
                                        + "  extents are clipped/enlarged to extent of <exp1>\n"
                                        + "- clip(<exp1>,<exp2>): clip to extent of <exp2>\n"
                                        + "- enlarge(<exp1>,<exp2>): enlarge to at least extent of <exp2>\n"
                                        + "- other (+,-,/,*,^,==,!=,<,): no extent corrections"
                                        + "", null, "Expression extent: {0},{1},{2},{3}", new string[] { "xll", "yll", "xur", "yur" });
            AddToolOptionDescription("v", "Use NoData as value v1. Without option v, NoData in one of the input IDF-files results in NoData.\n"
                                        + "Without option v1, the NoData-value of each IDF-file is used as a value.", "/v:0", "NoData calculation value: {0}", null, new string[] { "x1" }, new string[] { "NoData-value" });
            AddToolOptionDescription("d", "Run in debug mode: write intermediate expressions and IDF-files", "/d", "Running in debug mode");
            AddToolOptionDescription("i", "Write intermediate results (all IDF-variables) to IDF-files", "/i", "Intermediate variables are written to IDF-files");
            AddToolOptionDescription("m", "Add metadatafiles with (part of) expression(s) and source path", "/m", "Metadata is added to result files");
            AddToolOptionDescription("q", "Quiet mode: end without raising an error for missing IDF-files", null, "Quiet mode is used");
            AddToolOptionDescription("r", "Round values in (intermediate) result IDF-files to d decimals", "/d:3", "Rounding cell values to {0} decimals", new string[] { "d" });

            AddToolUsageOptionPostRemark("\n"
                                   + "Syntax description for expressions in INI-file\n"
                                   + "-----------------------------------------------\n"
                                   + "Each line should be one of the following:\n"
                                   + "- A remark: REM <comment>, to define a commentline which will be ignored\n"
                                   + "- An empty line, which will be ignored\n"
                                   + "- An expression: \"<var>=<exp>\", to store the result of IDF-expression <exp> in a variable with name <var>\n"
                                   + "\n"
                                   + "Variable names follow the rules of filenames and should not contain operators, decimal seperators or other\n"
                                   + "language structures (see below). Variable names are case sensitive.\n"
                                   + "Variables are stored in memory and also written as IDF-file with filename: <var>.IDF\n"
                                   + "Variable names can be prefixed by a relative path to write the IDF-file to a subdirectory, e.g. KHV\\100\\KHV_L1=KHV+0.5\n"
                                   + "IDF-expressions are defined by one of the following:\n"
                                   + "- names of previously defined variables\n"
                                   + "- IDF-filename (absolute path or relative to the path of the ini-file\n"
                                   + "- floating point constant values\n"
                                   + "- NoData to specify NoData-value(s)\n"
                                   + "- <exp1> <op> <exp2>\n"
                                   + "  where <exp1> and <exp2> are (nested) IDF-expressions\n"
                                   + "  where <op> is an arithmetic operator: ^, *, /, +, -\n"
                                   + "- if-expression: if(<cond>,<then>,<else>)\n"
                                   + "  where <cond> is a condition build up from IDF-expressions and comparison/logical operators\n"
                                   + "    comparison operators: ==, !=, >, >=, <, <=\n"
                                   + "    logical operators: &&, ||\n"
                                   + "    order of evaluation : * or /, + or -, == to <=, && or ||\n"
                                   + "  where <then> and <else> are IDF-expressions\n"
                                   + "  extents are enlarged with NoData to union of input IDF-extents\n"
                                   + "- min/max-functions: e.g. max(<exp1>,<exp2>)\n"
                                   + "  to take the minimum/maximum of IDF-expressions <exp1> and <exp2>; extents are clipped/enlarged\n"
                                   + "  with NoData to extent of IDF-expression <exp1>; cellsize of <exp1> is used for result.\n"
                                   + "- round-function: e.g. round(<exp1>,<decimalcount>)\n"
                                   + "  where <decimalcount> is an integer for the number of decimals\n"
                                   + "- enlarge-function: e.g. enlarge(<exp1>,<exp2>)\n"
                                   + "  to enlarge IDF-expression <exp1> with NoData to at least the extent of IDF-expression <exp2>\n"
                                   + "- clip-function: e.g. clip(<exp1>,<exp2>)\n"
                                   + "  to clip IDF-expression <exp1> to the extent of IDF-expression <exp2>\n"
                                   + "Notes:\n"
                                   + "- Parenthesis ('(' and ')') can be used to group subexpressions\n"
                                   + "- In general the cellsize of lefmost expression/IDF-file will be used for result\n"
                                   + "- Environment variables enclosed by %-symbols will be evaluated");

            AddToolUsageOptionPostRemark("\n"
                                   + "Example INI-file:\n"
                                   + "-----------------\n"
                                   + "REM Read source files\n"
                                   + "Horst=HORST.IDF\n"
                                   + "dL1_ZUG_Horst=dL1_ZUG_Horst.IDF\n"
                                   + "dL1_ZUG_Slenk=dL1_ZUG_Slenk.IDF\n"
                                   + "dL1_ZZG_Horst=dL1_ZZG_Horst.IDF\n"
                                   + "dL1_ZZG_Slenk=dL1_ZZG_Slenk.IDF\n"
                                   + "\n"
                                   + "REM Define kh-values\n"
                                   + "kZUG=50\n"
                                   + "kZZG=35\n"
                                   + "\n"
                                   + "REM Calculate kD-value\n"
                                   + "dL1_ZUG=if(Horst==1,dL1_ZUG_Horst,dL1_ZUG_Slenk)\n"
                                   + "dL1_ZZG=if(Horst==1,dL1_ZZG_Horst,dL1_ZZG_Slenk)\n"
                                   + "kD_L1=dL1_ZUG*kZUG+dL1_ZZG*kZZG");
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
                InputFilename = parameters[0];
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
            if (optionName.ToLower().Equals("m"))
            {
                IsMetadataAdded = true;
            }
            else if (optionName.ToLower().Equals("q"))
            {
                IsQuietMode = true;
            }
            else if (optionName.ToLower().Equals("i"))
            {
                IsIntermediateResultWritten = true;
            }
            else if (optionName.ToLower().Equals("d"))
            {
                IsDebugMode = true;
            }
            else if (optionName.ToLower().Equals("e"))
            {
                if (hasOptionParameters)
                {
                    string[] extentStrings = optionParametersString .Split(',');
                    if (extentStrings.Length == 4)
                    {
                        float[] extentCoordinates = new float[4];
                        for (int i = 0; i < 4; i++)
                        {
                            string coordinateString = extentStrings[i];
                            float coordinate;
                            if (!float.TryParse(coordinateString, out coordinate))
                            {
                                throw new ToolException("Invalid extent coordinate: " + coordinateString);
                            }
                            extentCoordinates[i] = coordinate;
                        }
                        Extent = new Extent(extentCoordinates[0], extentCoordinates[1], extentCoordinates[2], extentCoordinates[3]);
                    }
                    else if (File.Exists(optionParametersString) && Path.GetExtension(optionParametersString).ToLower().Equals(".idf"))
                    {
                        IDFFile extentIDF = IDFFile.ReadFile(optionParametersString, true);
                        Extent = extentIDF.Extent;
                    }
                    else
                    {
                        throw new ToolException("Invalid extent for option 'e': IDF-filename or 4 coordinates expected: " + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Missing extent for option 'e': IDF-filename or 4 coordinates (xll,yll,xur,yur) expected");
                }
            }
            else if (optionName.ToLower().Equals("r"))
            {
                IsResultRounded = true;
                if (hasOptionParameters)
                {
                    // retrieve part after colon
                    if (!optionParametersString.Equals(string.Empty))
                    {
                        try
                        {
                            DecimalCount = int.Parse(optionParametersString, EnglishCultureInfo);
                        }
                        catch (Exception)
                        {
                            throw new ToolException("Could not parse value for option 'r':" + optionParametersString);
                        }
                    }
                    else
                    {
                        throw new ToolException("A number of decimals should be specified for option 'r'");
                    }
                }
            }
            else if (optionName.ToLower().Equals("v"))
            {
                UseNodataAsValue = true;
                if (hasOptionParameters)
                {
                    // retrieve part after colon
                    if (!optionParametersString.Equals(string.Empty))
                    {
                        try
                        {
                            NoDataValue = float.Parse(optionParametersString, EnglishCultureInfo);
                        }
                        catch (Exception)
                        {
                            throw new ToolException("Could not parse value for option 'v':" + optionParametersString);
                        }
                    }
                    else
                    {
                        throw new ToolException("A NoData value should be specified for option 'v'");
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
            if (InputFilename != null)
            {
                InputFilename = ExpandPathArgument(InputFilename);
                if (!File.Exists(InputFilename))
                {
                    throw new ToolException("Input file does not exist: " + InputFilename);
                }
            }

            if (OutputPath.Equals(string.Empty) || (OutputPath == null))
            {
                // Use path of input file
                OutputPath = Path.GetDirectoryName(InputFilename);
            }
            OutputPath = ExpandPathArgument(OutputPath);
            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }
        }
    }
}
