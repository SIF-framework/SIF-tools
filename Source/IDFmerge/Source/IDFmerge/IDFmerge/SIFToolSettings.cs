// IDFmerge is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFmerge.
// 
// IDFmerge is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFmerge is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFmerge. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.IDFmerge
{
    public enum StatFunction
    {
        Undefined,
        Min,
        Max,
        Mean,
        Sum
    }

    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        public const string DefaultOutputFilename = "IDFmerge.IDF";

        public string InputPath { get; set; }
        public string InputFilter { get; set; }
        public string OutputPath { get; set; }
        public string OutputFilename { get; set; }

        public bool UseNodataCalculationValue { get; set; }
        public float NoDataCalculationValue { get; set; }
		public bool IgnoreNoDataValue { get; set; }
        public StatFunction StatFunction { get; set; }
        public string Selectionstring { get; set; }

        /// <summary>
        /// Pairs of one-based substring indices for grouping
        /// </summary>
		public List<int[]> GroupIndices { get; set; }
        public bool WriteCountIDFFile { get; set; }
		public bool WritePostfix { get; set; }

        public bool WriteMetadata { get; set; }

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

            UseNodataCalculationValue = false;
            NoDataCalculationValue = float.NaN;
			IgnoreNoDataValue = false;
			StatFunction = StatFunction.Mean;
            Selectionstring = null;
            GroupIndices = null;
            WriteCountIDFFile = false;
			WritePostfix = false;
            WriteMetadata = true;
		}

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("inPath", "Path to search for input files", "C:\\Test\\Input");
            AddToolParameterDescription("filter", "Filter to select input files (e.g. *.IDF)", "*.IDF");
            AddToolParameterDescription("outPath", "Path to write results", "C:\\Test\\Output");

            AddToolOptionDescription("c", "Write IDF-file with count of non-NoData cells in specified IDF-files", "/c", "IDF-file with non-NoData cell count is written");
            AddToolOptionDescription("s", "Define statistical function to merge IDF-files. Use one of: min, max, mean (default), sum", "/s:min", 
                                          "Statistic method for aggregation: {0}", new string[] { "s1" });
            AddToolOptionDescription("v", "Use NoData as a value. If a floating point value v1 is specified, this value is used, otherwise the\n" +
                                          "NoData-value of the IDF-file is used as a value. For this NoData is set to float.MaxValue.\n" + 
                                          "Without option v, NoData in one of the input IDF-files results in NoData output.", "/v:0", 
                                          "NoData calculation value: {0}", null, new string[] { "v1" }, new string[] { "NoData-value" });
			AddToolOptionDescription("i", "Ignore NoDataValues for statistical method mean and sum, this cannot be combined with option v.", "/i",
										  "NoData values are ignored", null);
			AddToolOptionDescription("g", "Define group-substring for filename, as substring between (one-based) character indices i1 and i2 of filename\n" +
									      "  Valid values for i1 and i2:\n" +
									      "    > 0:  normal index, 1 refers to first character of filename\n" +
                                          "    <= 0: backward index, 0 refers to last character in filename (without extension)\n" +
									      "  As a default the whole filename string is used (i1=1; i2=0)", "/i:0,-18", "Group substring(s) defined, with pairs of indices in filename: {...}",
									 new string[] {"i1", "i2" }, new string[] { "..." });
			AddToolOptionDescription("p", "Add postfix with statistical method to the resultfile", "/c", "Statistical method is added as postfix");
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
                if (Path.HasExtension(OutputPath))
                {
                    OutputFilename = Path.GetFileName(OutputPath);
                    OutputPath = Path.GetDirectoryName(OutputPath);
                }
                else
                {
                    OutputFilename = null;
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
			if (optionName.ToLower().Equals("c"))
            {
                WriteCountIDFFile = true;
            }
            else if (optionName.ToLower().Equals("s"))
			{
				if (hasOptionParameters)
				{
					// split option parameter string into comma seperated substrings
					string[] optionParameters = GetOptionParameters(optionParametersString);
					// Parse substrings for this option
					if (optionParameters.Length == 1)
					{
						switch (optionParametersString.ToLower())
						{
							case "min":
								StatFunction = StatFunction.Min;
								break;
							case "max":
								StatFunction = StatFunction.Max;
								break;
							case "mean":
								StatFunction = StatFunction.Mean;
								break;
							case "sum":
								StatFunction = StatFunction.Sum;
								break;
							default:
								throw new ToolException("Invalid function specified for option 's': " + optionParametersString);
						}
					}
					else
					{
						throw new ToolException("Only one paramter should be given for option: " + optionName + ", you provided: " + optionParametersString);
					}
				}
				else
				{
					throw new ToolException("Parameter value expected for option '" + optionName + "'");
				}
			}
            else if (optionName.ToLower().Equals("v"))
            {
                UseNodataCalculationValue = true;

                if (hasOptionParameters)
                {
                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
                    // Parse substrings for this option
                    if (optionParameters.Length == 1)
                    {
                        if (!float.TryParse(optionParametersString, NumberStyles.Float, EnglishCultureInfo, out float noDataCalculationValue))
                        {
                            throw new ToolException("Could not parse NoData-value for option 'v':" + optionParametersString);
                        }
                        NoDataCalculationValue = noDataCalculationValue;
                    }
                    else
                    {
                        throw new ToolException("Only one paramter should be given for option: " + optionName + ": " + optionParametersString);
                    }
                }
                else
                {
                    // Use NoData-value as actual NoDataCalculationValue. Leave float.NaN for now.
                }
            }
			else if (optionName.ToLower().Equals("i"))
			{
				IgnoreNoDataValue = true;
			}
			else if (optionName.ToLower().Equals("g"))
            {
				if (hasOptionParameters)
				{
                    GroupIndices = new List<int[]>();

                    // split option parameter string into comma seperated substrings
                    string[] optionParameters = GetOptionParameters(optionParametersString);
					if (optionParameters.Length < 2)
					{
						throw new ToolException("Missing parameters for option '" + optionName + "': " + optionParametersString);
					}

					if(optionParameters.Length % 2 != 0)
					{
						throw new ToolException("Number of parameters should be even for '" + optionName + "': " + optionParametersString);
					}

					int subStringIdx1 = 0;
					int subStringIdx2 = 0;
					int i = 0;
					while (i < optionParameters.Length)
					{
						if (!int.TryParse(optionParameters[i], out subStringIdx1))
						{
							throw new ToolException("Invalid substring index1 for option '" + optionName + "': " + optionParametersString);
						}

						if (!int.TryParse(optionParameters[i+1], out subStringIdx2))
						{
							throw new ToolException("Invalid substring index2 for option '" + optionName + "': " + optionParametersString);
						}
						int[] subStringPair = { subStringIdx1, subStringIdx2 };
						GroupIndices.Add(subStringPair);
						i = i + 2;
					}
				}
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
                }
            }
			else if (optionName.ToLower().Equals("p"))
			{
				WritePostfix = true;
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
                // Specify default
                InputFilter = "*.IDF";
            }

			if(IgnoreNoDataValue && UseNodataCalculationValue)
			{
				throw new ToolException("Option v Nodata Value cannot be combined with option i ignore Nodata, remove one of these options");
			}
        }
    }
}
