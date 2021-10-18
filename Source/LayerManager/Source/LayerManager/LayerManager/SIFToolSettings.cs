// LayerManager is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of LayerManager.
// 
// LayerManager is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// LayerManager is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with LayerManager. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.LayerManager.LayerModels;

namespace Sweco.SIF.LayerManager
{
    /// <summary>
    /// Available layermodel types
    /// </summary>
    public enum InputType
    {
        REGISII2,
        iMOD
    }

    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public InputType inputType;
        public string InputPath { get; set; }
        public string OutputPath { get; set; }

        public string OutputKDSubdirname { get; set; }
        public string OutputCSubdirname { get; set; }
        public string OutputThicknessSubdirname { get; set; }
        public string LayerOrderFilename { get; set; }
        public string TOPParString { get; set; }
        public string BOTParString { get; set; }
        public string KHVParString { get; set; }
        public string KVVParString { get; set; }
        public string KVAParString { get; set; }
        public string KDWParString { get; set; }
        public string VCWParString { get; set; }
        public string TOPPath { get; set; }
        public string BOTPath { get; set; }
        public string KDWPath { get; set; }
        public string KVVPath { get; set; }
        public string KVAPath { get; set; }
        public string VCWPath { get; set; }
        public string KHVPath { get; set; }
        public string TOPFilenamesPattern { get; set; }
        public string BOTFilenamesPattern { get; set; }
        public string KDWFilenamesPattern { get; set; }
        public string VCWFilenamesPattern { get; set; }
        public string KHVFilenamesPattern { get; set; }
        public string KVVFilenamesPattern { get; set; }
        public string KVAFilenamesPattern { get; set; }
        public float DefaultKDWValue { get; set; }
        public float DefaultVCWValue { get; set; }
        public float DefaultKHVValue { get; set; }
        public float DefaultKVVValue { get; set; }
        public float DefaultKVAValue { get; set; }

        public bool IsDeleteFiles { get; set; }
        public bool IsModelChecked { get; set; }
        public bool IsKDCCalculated { get; set; }
        public bool IsLayerGroupLevelMerged { get; set; }
        public bool IsDummyDefaultKDCSkipped { get; set; }
        public Extent Extent { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            InputPath = null;
            OutputPath = null;

            inputType = InputType.REGISII2;
            LayerOrderFilename = null;

            TOPParString = null;
            BOTParString = null;
            KDWParString = null;
            VCWParString = null;
            KHVParString = null;
            KVVParString = null;

            TOPFilenamesPattern = Properties.Settings.Default.iMODTOPFilePatternString;
            BOTFilenamesPattern = Properties.Settings.Default.iMODBOTFilePatternString;
            KDWFilenamesPattern = Properties.Settings.Default.iMODkDFilePatternString;
            VCWFilenamesPattern = Properties.Settings.Default.iMODCFilePatternString;
            KHVFilenamesPattern = Properties.Settings.Default.iMODKHVFilePatternString;
            KVVFilenamesPattern = Properties.Settings.Default.iMODKVVFilePatternString;
            KVAFilenamesPattern = Properties.Settings.Default.iMODKVAFilePatternString;

            OutputKDSubdirname = Properties.Settings.Default.kDSubdirname;
            OutputCSubdirname = Properties.Settings.Default.CSubdirName;
            OutputThicknessSubdirname = Properties.Settings.Default.ThicknessSubdirname;

            DefaultKDWValue = float.NaN;
            DefaultVCWValue = float.NaN;
            DefaultKHVValue = float.NaN;
            DefaultKVVValue = float.NaN;
            DefaultKVAValue = float.NaN;

            IsLayerGroupLevelMerged = true;
            IsDummyDefaultKDCSkipped = false;
            IsDeleteFiles = false;
            IsModelChecked = false;
            IsKDCCalculated = false;
            Extent = null;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Base input path (absolute or relative to current directory)", "C:\\Test\\Input");
            AddToolParameterDescription("outPath", "Output path (absolute or relative to current directory)", "C:\\Test\\Output");

            AddToolUsageOptionPreRemark("\nNote: paths are absolute or relative to InputPath; files may be IDF or ASC");
            AddToolOptionDescription("top", "Specify path p to TOP-files", null, "TOP-path: {0}", new string[] { "p" });
            AddToolOptionDescription("bot", "Specify path p to BOT-files", null, "BOT-path: {0}", new string[] { "p" });
            AddToolOptionDescription("khv", "Specify path p to KHV-files and optional default KHV-value v", null, "KHV-path: {0}; default value: {1}", new string[] { "p" }, new string[] { "v" }, new string[] { Properties.Settings.Default.DefaultKHValue.ToString(EnglishCultureInfo) });
            AddToolOptionDescription("kvv", "Specify path p to KVV-files and optional default KVV-value v", null, "KVV-path: {0}; default value: {1}", new string[] { "p" }, new string[] { "v" }, new string[] { Properties.Settings.Default.DefaultKVValue.ToString(EnglishCultureInfo) });
            AddToolOptionDescription("kva", "Specify path p to KVA-files and optional default KVA-value v", null, "KVA-path: {0}; default value: {1}", new string[] { "p" }, new string[] { "v" }, new string[] { Properties.Settings.Default.DefaultKVAValue.ToString(EnglishCultureInfo) });
            AddToolOptionDescription("kdw", "Specify path p to KDW-files and optional default KDW-value v", null, "KDW-path: {0}; default value: {1}", new string[] { "p" }, new string[] { "v" }, new string[] { Properties.Settings.Default.DefaultKDValue.ToString(EnglishCultureInfo) });
            AddToolOptionDescription("vcw", "Specify path p to VCW-files and optional default VCW-value v", null, "VCW-path: {0}; default value: {1}", new string[] { "p" }, new string[] { "v" }, new string[] { Properties.Settings.Default.DefaultCValue.ToString(EnglishCultureInfo) });

            AddToolOptionDescription("c", "Check consistency of input layerfiles", "/c", "Consistency of input layers file is checked");
            AddToolOptionDescription("kdc", "Calculate kD-, c- and thickness-grids for input layerfiles. A seperate or single subdirname for all three\n" +
                                            "types (kD, c, thickness) can be specified, defaults are: '" + Properties.Settings.Default.kDSubdirname + "', '" + Properties.Settings.Default.CSubdirName + "', " + Properties.Settings.Default.ThicknessSubdirname + "'", null,
                                            "kD-, c- and thickness-grids are calculated. Output paths are:\n" +
                                            "  - kD: {0}\n" + 
                                            "  - c: {1}\n" + 
                                            "  - thickness: {2}", null, new string[] { "p1", "p2", "p3" }, 
                                            new string[] { Properties.Settings.Default.kDSubdirname, Properties.Settings.Default.CSubdirName, Properties.Settings.Default.ThicknessSubdirname });
            AddToolOptionDescription("d", "Delete all existing files in output folder(s)", "/d", "Existing files in output path are deleted");
            AddToolOptionDescription("e", "specify extent (xll,yll,xur,yur) to clip input IDF-files to", "/e:220000,550000,242000,570000", "Exent specified for clipping IDF-files: {0},{1},{2},{3}",
                                           new string[] { "xll", "yll", "xur", "yur" });
            AddToolOptionDescription("o", "Specify layer order textfile with comma seperated layerIdx,layerPrefix pairs at each line.\n" +
                                          "At the first lines comments can be specified by prefixing them with an '#'-symbol.\n" +
                                          "If an order textfile is defined only layers specified in this file are processed.", "/o:test\\order.txt", "Order-file is specified: {0}", new string[] { "f" });
            AddToolOptionDescription("i", "Specify that input model has iMOD-format", null, "Input model has iMOD-format");
            AddToolOptionDescription("s", "skip warnings/errors for default kD/C-values in dummy layers)" +
                                          "(default kD-value = " + Properties.Settings.Default.DefaultKDValue.ToString(EnglishCultureInfo) + ", default C-value = " + Properties.Settings.Default.DefaultCValue.ToString(EnglishCultureInfo) + ")",
                                          "/s", "Default KD (" + Properties.Settings.Default.DefaultKDValue + ") or C (" + Properties.Settings.Default.DefaultCValue + ") values in dummy layers are not reported as error/warning");
            AddToolOptionDescription("t", "specify tolerance for errors/warnings about level equality (t1, default: " + Layer.LevelTolerance + ")", "/t:0.05", "Tolerance for level equality checks: {0}", new string[] { "t1" });
            AddToolUsageOptionPostRemark("Specify either option c (check model) or kdc (calculate kDc-values)");
            AddToolUsageFinalRemark("Exitcodes are as follows:\n" +
                                    "  -1 is returned in case of tool errors\n" +
                                    "   1 is returned in case of model inconsistencies\n" +
                                    "   0 is returned in case of no errors or inconsistencies\n");
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
            if (optionName.ToLower().Equals("d"))
            {
                IsDeleteFiles = true;
            }
            else if (optionName.ToLower().Equals("o"))
            {
                if (hasOptionParameters)
                {
                    LayerOrderFilename = optionParametersString;
                }
                else
                {
                    throw new ToolException("Filename missing for option 'o'");
                }
            }
            else if (optionName.ToLower().Equals("e"))
            {
                if (hasOptionParameters)
                {
                    Extent = Extent.ParseExtent(optionParametersString);
                    if (Extent == null)
                    {
                        throw new ToolException("Could not parse extent coordinates for option 'e': " + optionParametersString);
                    }
                }
                else
                {
                    throw new ToolException("Extent coordinates missing for option 'e'");
                }
            }
            else if (optionName.ToLower().Equals("i"))
            {
                inputType = InputType.iMOD;
            }
            else if (optionName.ToLower().Equals("top"))
            {
                if (hasOptionParameters)
                {
                    TOPParString = optionParametersString;
                }
                else
                {
                    throw new ToolException("parameter missing for option 'top'");
                }
            }
            else if (optionName.ToLower().Equals("bot"))
            {
                if (hasOptionParameters)
                {
                    BOTParString = optionParametersString;
                }
                else
                {
                    throw new ToolException("parameter missing for option 'bot'");
                }
            }
            else if (optionName.ToLower().Equals("khv"))
            {
                if (hasOptionParameters)
                {
                    string[] parameters = GetOptionParameters(optionParametersString);
                    if (parameters.Length >= 1)
                    {
                        KHVParString = parameters[0];
                    }
                    if (parameters.Length > 1)
                    {
                        if (float.TryParse(parameters[1], NumberStyles.Float, EnglishCultureInfo, out float value))
                        {
                            DefaultKHVValue = value;
                        }
                        else
                        {
                            throw new ToolException("Invalid default value for option 'khv':" + parameters[1]);

                        }
                    }
                }
                else
                {
                    throw new ToolException("parameter(s) missing for option 'khv'");
                }
            }
            else if (optionName.ToLower().Equals("kvv"))
            {
                if (hasOptionParameters)
                {
                    string[] parameters = GetOptionParameters(optionParametersString);
                    if (parameters.Length >= 1)
                    {
                        KVVParString = parameters[0];
                    }
                    if (parameters.Length > 1)
                    {
                        if (float.TryParse(parameters[1], NumberStyles.Float, EnglishCultureInfo, out float value))
                        {
                            DefaultKVVValue = value;
                        }
                        else
                        {
                            throw new ToolException("Invalid default value for option 'kvv':" + parameters[1]);

                        }
                    }
                }
                else
                {
                    throw new ToolException("parameter missing for option 'kvv'");
                }
            }
            else if (optionName.ToLower().Equals("kva"))
            {
                if (hasOptionParameters)
                {
                    string[] parameters = GetOptionParameters(optionParametersString);
                    if (parameters.Length >= 1)
                    {
                        KVAParString = parameters[0];
                    }
                    if (parameters.Length > 1)
                    {
                        if (float.TryParse(parameters[1], NumberStyles.Float, EnglishCultureInfo, out float value))
                        {
                            DefaultKVAValue = value;
                        }
                        else
                        {
                            throw new ToolException("Invalid default value for option 'kva':" + parameters[1]);

                        }
                    }
                }
                else
                {
                    throw new ToolException("parameter missing for option 'kva'");
                }
            }
            else if (optionName.ToLower().Equals("kdw"))
            {
                if (hasOptionParameters)
                {
                    string[] parameters = GetOptionParameters(optionParametersString);
                    if (parameters.Length >= 1)
                    {
                        KDWParString = parameters[0];
                    }
                    if (parameters.Length > 1)
                    {
                        if (float.TryParse(parameters[1], NumberStyles.Float, EnglishCultureInfo, out float value))
                        {
                            DefaultKDWValue = value;
                        }
                        else
                        {
                            throw new ToolException("Invalid default value for option 'kdw':" + parameters[1]);

                        }
                    }
                }
                else
                {
                    throw new ToolException("parameter missing for option 'kdw'");
                }
            }
            else if (optionName.ToLower().Equals("vcw"))
            {
                if (hasOptionParameters)
                {
                    string[] parameters = GetOptionParameters(optionParametersString);
                    if (parameters.Length >= 1)
                    {
                        VCWParString = parameters[0];
                    }
                    if (parameters.Length > 1)
                    {
                        if (float.TryParse(parameters[1], NumberStyles.Float, EnglishCultureInfo, out float value))
                        {
                            DefaultVCWValue = value;
                        }
                        else
                        {
                            throw new ToolException("Invalid default value for option 'vcw':" + parameters[1]);

                        }
                    }
                }
                else
                {
                    throw new ToolException("parameter missing for option 'vcw'");
                }
            }
            else if (optionName.ToLower().Equals("c"))
            {
                IsModelChecked = true;
            }
            else if (optionName.ToLower().Equals("kdc"))
            {
                IsKDCCalculated = true;
                if (hasOptionParameters)
                {
                    string[] parameters = GetOptionParameters(optionParametersString);
                    if (parameters.Length >= 1)
                    {
                        OutputKDSubdirname = parameters[0];
                        OutputCSubdirname = parameters[0];
                        OutputThicknessSubdirname = parameters[0];
                    }
                    if (parameters.Length >= 2)
                    {
                        OutputCSubdirname = parameters[1];
                        OutputThicknessSubdirname = parameters[1];
                    }
                    if (parameters.Length == 3)
                    {
                        OutputThicknessSubdirname = parameters[2];
                    }
                    if (parameters.Length > 3)
                    {
                        OutputKDSubdirname = parameters[0];
                        OutputCSubdirname = parameters[1];
                        OutputThicknessSubdirname = parameters[2];
                    }
                }
            }
            else if (optionName.ToLower().Equals("s"))
            {
                IsDummyDefaultKDCSkipped = true;
            }
            else if (optionName.ToLower().Equals("t"))
            {
                if (hasOptionParameters)
                {
                    string parameterString = optionParametersString;
                    if (!float.TryParse(parameterString, NumberStyles.Float, EnglishCultureInfo, out float t1))
                    {
                        throw new ToolException("Invalid value for parameter of option 't': " + parameterString);
                    }
                    Layer.LevelTolerance = t1;
                }
                else
                {
                    throw new ToolException("tolerance parameters missing for option 't'");
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
            InputPath = (InputPath != null) ? Path.GetFullPath(InputPath) : null;
            OutputPath = Path.GetFullPath(OutputPath);

            if (InputPath != null)
            {
                InputPath = ExpandPathArgument(InputPath);
                if (!Directory.Exists(InputPath))
                {
                    throw new ToolException("Input path does not exist: " + InputPath);
                }
            }

            // Create output path if not yet existing
            if (!Directory.Exists(OutputPath))
            {
                Directory.CreateDirectory(OutputPath);
            }

            // Check tool option values
            if (LayerOrderFilename != null)
            {
                // Check specified path
                if (!File.Exists(LayerOrderFilename))
                {
                    // Check specified path relative to input path
                    if (!File.Exists(Path.Combine(InputPath, LayerOrderFilename)))
                    {
                        throw new ToolException("Specified layer order file does not exist: " + LayerOrderFilename);
                    }
                    else
                    {
                        LayerOrderFilename = Path.Combine(InputPath, LayerOrderFilename);
                    }
                }
                LayerOrderFilename = (LayerOrderFilename != null) ? Path.GetFullPath(LayerOrderFilename) : null;
            }

            ProcessLayerSourceSettings();
            ProcessLayerDefaultSettings();
        }

        /// <summary>
        /// Handle and check settings 
        /// </summary>
        protected virtual void ProcessLayerSourceSettings()
        {
            // Correct for absolute or relative paths
            TOPPath = GetCheckedParStringPath(InputPath, TOPParString, "TOP");
            BOTPath = GetCheckedParStringPath(InputPath, BOTParString, "BOT");
            KHVPath = GetCheckedParStringPath(InputPath, KHVParString, "kh");
            KVVPath = GetCheckedParStringPath(InputPath, KVVParString, "kv");
            KVAPath = GetCheckedParStringPath(InputPath, KVAParString, "kv");
            KDWPath = GetCheckedParStringPath(InputPath, KDWParString, "kD");
            VCWPath = GetCheckedParStringPath(InputPath, VCWParString, "C");

            if (inputType == InputType.REGISII2)
            {
                TOPFilenamesPattern = Properties.Settings.Default.REGISTopFilePatternString;
                BOTFilenamesPattern = Properties.Settings.Default.REGISBotFilePatternString;
                KDWFilenamesPattern = Properties.Settings.Default.REGISkDFilePatternString;
                VCWFilenamesPattern = Properties.Settings.Default.REGISCFilePatternString;
                KHVFilenamesPattern = Properties.Settings.Default.REGISKHFilePatternString;
                KVVFilenamesPattern = Properties.Settings.Default.REGISKVFilePatternString;
                KVAFilenamesPattern = null;
            }
            else
            {
                TOPFilenamesPattern = Properties.Settings.Default.iMODTOPFilePatternString;
                BOTFilenamesPattern = Properties.Settings.Default.iMODBOTFilePatternString;
                KDWFilenamesPattern = Properties.Settings.Default.iMODkDFilePatternString;
                VCWFilenamesPattern = Properties.Settings.Default.iMODCFilePatternString;
                KHVFilenamesPattern = Properties.Settings.Default.iMODKHVFilePatternString;
                KVVFilenamesPattern = Properties.Settings.Default.iMODKVVFilePatternString;
                KVAFilenamesPattern = Properties.Settings.Default.iMODKVAFilePatternString;
            }
        }

        protected virtual void ProcessLayerDefaultSettings()
        {
            if (DefaultKHVValue.Equals(float.NaN))
            {
                DefaultKHVValue = Properties.Settings.Default.DefaultKHValue;
            }
            if (DefaultKVVValue.Equals(float.NaN))
            {
                DefaultKVVValue = Properties.Settings.Default.DefaultKVValue;
            }
            if (DefaultKVAValue.Equals(float.NaN))
            {
                DefaultKVAValue = Properties.Settings.Default.DefaultKVAValue;
            }
            if (DefaultKDWValue.Equals(float.NaN))
            {
                DefaultKDWValue = Properties.Settings.Default.DefaultKDValue;
            }
            if (DefaultVCWValue.Equals(float.NaN))
            {
                DefaultVCWValue = Properties.Settings.Default.DefaultCValue;
            }
        }

        /// <summary>
        /// Retrieve path of layermodel parameter and handle absolute/relative paths
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="parString">string that is used as a subdirectory for parameter</param>
        /// <param name="parName">name for parameter to use in errormessages</param>
        /// <returns></returns>
        protected string GetCheckedParStringPath(string basePath, string parString, string parName)
        {
            if (parString != null)
            {
                if (!Path.IsPathRooted(parString))
                {
                    parString = Path.Combine(basePath, parString);
                }
                parString = Path.GetFullPath(parString);

                if (!Directory.Exists(parString))
                {
                    throw new ToolException(parName + "-path does not exist: " + parString);
                }
            }
            else
            {
                return basePath;
            }
            return parString;
        }
    }
}
