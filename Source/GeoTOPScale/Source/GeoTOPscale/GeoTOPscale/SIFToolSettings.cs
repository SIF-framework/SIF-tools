// GeoTOPScale is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of GeoTOPScale.
// 
// GeoTOPScale is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// GeoTOPScale is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with GeoTOPScale. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;

namespace Sweco.SIF.GeoTOPscale
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public string StratVoxelPath { get; set; }
        public string LithoVoxelPath { get; set; }

        public string KTableFilename { get; set; }
        public string KTableSheetID { get; set; }
        public int KTableDataRowIdx { get; set; }
        public int KTableStratColIdx { get; set; }
        public int KTableLithoColIdx { get; set; }
        public int KTableKHColIdx { get; set; }
        public int KTableKVColIdx { get; set; }

        public string TOPFilename { get; set; }
        public string BOTFilename { get; set; }
        public int BoundaryLayerSelectionMethod { get; set; }

        public string MF6Exe { get; set; }
        public string iMODExe { get; set; }
        public string OutputPath { get; set; }
        public bool WriteKVbot { get; set; }
        public bool WriteKVstack { get; set; }

        public Extent ClipExtent { get; set; }

        /// <summary>
        /// Timeout length in milliseconds before cancelling MF6-modelrun
        /// </summary>
        public int MF6Timeout { get; set; }

        /// <summary>
        /// Timeout length in milliseconds before cancelling MF6TOIDF-run
        /// </summary>
        public int MF6TOIDFTimeout { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            //InputPath = null;
            StratVoxelPath = null;
            LithoVoxelPath = null;

            KTableFilename = null;
            KTableSheetID = null;
            KTableStratColIdx = -1;
            KTableLithoColIdx = -1;
            KTableKHColIdx = -1;
            KTableKVColIdx = -1;
            KTableDataRowIdx = 1;

            TOPFilename = null;
            BOTFilename = null;
            BoundaryLayerSelectionMethod = 0;

            MF6Exe = null;
            iMODExe = null;
            OutputPath = null;

            //options
            ClipExtent = null;
            MF6Timeout = 0;
            MF6TOIDFTimeout = 0;
            WriteKVbot = false;
            WriteKVstack = false;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            //AddToolParameterDescription("inPath", "Path to search for input files", "C:\\Test\\Input");
            AddToolParameterDescription("stratPath", "Path to directory in which stratigraphy voxel model is stored", "C:\\Test\\Input\\eenheid");
            AddToolParameterDescription("lithoPath", "Path to directory in which lithoclass voxel model is stored", "C:\\Test\\Input\\lithoklasse");

            AddToolParameterDescription("topFile", "IDFFile with top level for scaling area", "C:\\Test\\Input\\bxlm_tcc.idf");
            AddToolParameterDescription("botFile", "IDFFile with bot level for scaling area", "C:\\Test\\Input\\bxlm_bcc.idf");

            AddToolParameterDescription("MF6exe", "Path to modflow6 executable", "C:\\Test\\Input\\Model\\EXE\\iMODFlOW\\MODFLOW6_v6.3.0.exe");
            AddToolParameterDescription("iMODexe", "Path to iMOD executable", "C:\\Test\\Input\\Model\\EXE\\iMOD\\iMOD_V5_4.exe");

            AddToolParameterDescription("outPath", "Path to write results to", "C:\\Test\\Output");

            AddToolOptionDescription("kvb", "Also write kv_bot grid, based on bottom flux; otherwise only kv_top is written", "/kvb", "kv_bot is also written");
            AddToolOptionDescription("kvs", "Also Write kv-file as calculated by stack-method well", "/ks", "kv_stack is also written");
            AddToolOptionDescription("e", "Clip extent for processing input", "/e:158000,403000,163000,407000", "Clip extent (xll,yll,xur,yur): {0},{1},{2},{3}", new string[] { "xll", "yll", "xur", "yur" });
            AddToolOptionDescription("b", "Method for uppper/lower TOP/BOT-level from voxel model to TOP/BOT-IDFfile (default: 0)\n" +
                                          "  0 round to a thinner layer;\n" +
                                          "  1 round to a thicker layer;\n" +
                                          "  2 include TOP/BOT-layer if more than half of the voxel thickness layer is in TOP/BOT-layer",
                                          "/b:2", "Method for upper/lower BOT-level: {0}", new string[] { "a1" });
            AddToolOptionDescription("t", "Define timeout lengths t1,t2 (in milliseconds) before cancelling MF6 or MF6TOIDF-run.\n" +
                                          "Use 0 to wait indefenitely (default: 0)/ Note: when MF6TOIDF hangs, try MF6TOIDF timeout, e.g. 10000 ms", "/t:0,10000", "Timeout length for MF6 and MF6TOIDF: {0}, {1}", new string[] { "t1" }, new string[] { "t2" }, new string[] { "0" });
            AddToolOptionDescription("k", "Define datasource for k-values by Excelfile e, sheet s, rownr of header r, and\n" +
                "columnnumbers c1, c2, c3, c4 for stratcode, lithoclassnr, kh-values, kv-values\n" +
                "  Excel workbook should contain kh- and kv-values for each combination of stratigraphy and lithoclass\n" +
                "  Excel sheet can be specified with sheet name or number. Row and column numbers are one-based",
                "/khkv:Test\\GeoTOP khkv-values.xlsx,1,2,2,4,6,7", 
                "Definition for kh- and kv-values:\n" + 
                "\tExcel workbook: {0}\n" +
                "\tExcel sheet: {1}\n" +
                "\tdata row nr: {2}\n" +
                "\tstratcode column nr: {3}\n" +
                "\tlithoclass column nr: {4}\n" +
                "\tkh-value column nr: {5}\n" +
                "\tkv-value column nr: {6}", new string[] { "e", "s", "r", "c1", "c2", "c3", "c4" });
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
            // As a default, do not use special formatting and simply return parameter value
            return parameterValue;

            // An example for formatting a log string parameter value 
            // switch (optionName)
            // {
            //     case "x":
            //         switch (parameter)
            //         {
            //             case "x1":
            //                 switch (parameterValue)
            //                 {            //                     case "Value1": return "Value1";
            //                     default: return parameterValue;
            //                 }
            //             default: return parameterValue;
            //         }
            //     default: return parameterValue;
            // }
        }

        /// <summary>
        /// Parse and process obligatory tool parameter at (zero based) index parIndex
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex">returns the index for the argument group for these parameters, 0 if only a single group is defined</param>
        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length == 7)
            {
                // Parse syntax 1:
                //InputPath = parameters[1];
                StratVoxelPath = parameters[0];
                LithoVoxelPath = parameters[1];

                TOPFilename = parameters[2];
                BOTFilename = parameters[3];

                MF6Exe = parameters[4];
                iMODExe = parameters[5];
                OutputPath = parameters[6];
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
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    // Parse substrings for this option
                    if (optionParameters.Length == 4)
                    {
                        try
                        {
                            ClipExtent = Extent.ParseExtent(optionParametersString);
                        }
                        catch
                        {
                            throw new ToolException("Could not parse scale extent: " + optionParameters[0]);
                        }
                    }
                    else
                    {
                        throw new ToolException("Four extent coordinates are expected for option: '" + optionName + "':" + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("k"))
            {
                if (hasOptionParameters)
                {
                    string[] parameters = GetOptionParameters(optionParametersString);
                    if (parameters.Length != 7)
                    {
                        throw new ToolException("Invalid numnber of parameter for option '" + optionName + "', 7 expected: " + optionParametersString);
                    }

                    KTableFilename = parameters[0];
                    KTableSheetID = parameters[1];
                    if (int.TryParse(parameters[2], out int rowNr))
                    {
                        KTableDataRowIdx = rowNr - 1;
                    }
                    else
                    {
                        throw new ToolException("Could not parse rownumber for option '" + optionName + "': " + parameters[2]);
                    }
                    if (int.TryParse(parameters[3], out int stratColNr))
                    {
                        KTableStratColIdx = stratColNr - 1;
                    }
                    else
                    {
                        throw new ToolException("Could not parse stratography column number for option '" + optionName + "': " + parameters[3]);
                    }
                    if (int.TryParse(parameters[4], out int lithoColNr))
                    {
                        KTableLithoColIdx = lithoColNr - 1;
                    }
                    else
                    {
                        throw new ToolException("Could not parse stratography column number for option '" + optionName + "': " + parameters[4]);
                    }
                    if (int.TryParse(parameters[5], out int khCol))
                    {
                        KTableKHColIdx = khCol - 1;
                    }
                    else
                    {
                        throw new ToolException("Could not parse stratography column number for option '" + optionName + "': " + parameters[5]);
                    }
                    if (int.TryParse(parameters[6], out int kvCol))
                    {
                        KTableKVColIdx = kvCol - 1;
                    }
                    else
                    {
                        throw new ToolException("Could not parse stratography column number for option '" + optionName + "': " + parameters[6]);
                    }
                }
                else
                {
                    throw new ToolException("Missing parameters for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("t"))
            {
                if (hasOptionParameters)
                {
                    string[] optionParameterStrings = GetOptionParameters(optionParametersString);
                    if (int.TryParse(optionParameterStrings[0], out int timeout))
                    {
                        MF6Timeout = timeout;
                    }
                    else
                    {
                        throw new ToolException("Invalid MF6-timeout value for option '" + optionName + "': " + optionParameterStrings[0]);
                    }
                    if (optionParameterStrings.Length > 1)
                    {
                        if (int.TryParse(optionParameterStrings[1], out timeout))
                        {
                            MF6TOIDFTimeout = timeout;
                        }
                        else
                        {
                            throw new ToolException("Invalid MF6TOIDF-timeout value for option '" + optionName + "': " + optionParameterStrings[1]);
                        }
                    }
                }
                else
                {
                    throw new ToolException("Missing timeout value for option '" + optionName + "'");
                }
            }
            else if (optionName.ToLower().Equals("kvb"))
            {
                WriteKVbot = true;
            }
            else if (optionName.ToLower().Equals("kvs"))
            {
                WriteKVstack = true;
            }
            else if (optionName.ToLower().Equals("b"))
            {
                if (hasOptionParameters)
                {
                    if (int.TryParse(optionParametersString, out int boundaryLayerSelectionMethod))
                    {
                        if (boundaryLayerSelectionMethod > 2)
                        {
                            throw new ToolException("Boundary layers selection method must be 0; 1; or 2; : " + boundaryLayerSelectionMethod);
                        }
                        BoundaryLayerSelectionMethod = boundaryLayerSelectionMethod;
                    }
                    else
                    {
                        throw new ToolException("Boundary layer selection is it not an integer: '" + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Missing argument for option '" + optionName + "'");
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
            if (StratVoxelPath != null)
            {
                StratVoxelPath = ExpandPathArgument(StratVoxelPath);
                if (!Directory.Exists(StratVoxelPath))
                {
                    throw new ToolException("Input path does not exist: " + StratVoxelPath);
                }
            }

            if (LithoVoxelPath != null)
            {
                LithoVoxelPath = ExpandPathArgument(LithoVoxelPath);
                if (!Directory.Exists(LithoVoxelPath))
                {
                    throw new ToolException("Input path does not exist: " + LithoVoxelPath);
                }
            }

            if (KTableFilename != null)
            {
                KTableFilename = ExpandPathArgument(KTableFilename);
                if (!Directory.Exists(Path.GetDirectoryName(KTableFilename)))
                {
                    throw new ToolException("Input path does not exist: " + KTableFilename);
                }
            }

            if ((MF6Exe == null) || !File.Exists(MF6Exe))
            {
                throw new ToolException("Specified MF6 executable not found: " + ExpandPathArgument(MF6Exe));
            }
            if ((iMODExe == null) || !File.Exists(iMODExe))
            {
                throw new ToolException("Specified iMOD executable not found: " + ExpandPathArgument(iMODExe));
            }
            MF6Exe = ExpandPathArgument(MF6Exe);
            iMODExe = ExpandPathArgument(iMODExe);

            if(ClipExtent != null)
            {
                if (ClipExtent.llx > ClipExtent.urx || ClipExtent.lly > ClipExtent.ury)
                {
                    throw new ToolException("Extent is incorrectly defined because e1>e3 or e2>4, please check extent order (llx,lly,urx,ury)");
                }
            }

            //  Check that a source is defined for kv- and kv-values
            if (KTableFilename == null)
            {
                throw new ToolException("Please specify datasource for kh- and kv-values with option 'k'");
            }
        }
    }
}
