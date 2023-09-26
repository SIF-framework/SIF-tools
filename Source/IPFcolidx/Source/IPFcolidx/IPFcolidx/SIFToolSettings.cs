// IPFcolidx is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IPFcolidx.
// 
// IPFcolidx is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IPFcolidx is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IPFcolidx. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;

namespace Sweco.SIF.IPFcolidx
{
    /// <summary>
    /// Class for processing command-line arguments and storing settings for this tool
    /// </summary>
    public class SIFToolSettings : SIFToolSettingsBase
    {
        /// <summary>
        /// Name of specified IPF-file
        /// </summary>
        public string IPFFilename { get; set; }

        /// <summary>
        /// Name if specified columnname, or null if not defined 
        /// </summary>
        public string ColumnName { get; set; }

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettings(string[] args) : base(args)
        {
            // Set default values for settings
            IPFFilename = null;
            
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected override void DefineToolSyntax()
        {
            AddToolParameterDescription("ipffile",
                "Path to input IPF-file(s); if filename in path contains wildcards,\n" +
                "the first file found is used", "Test\\Input\\residual_L1.IPF");
            AddToolOptionDescription("c", 
                "retrieve index for column with specified 'colname' surround with double quotes (\")\n" + 
                "to include whitespace. The default is to return the number of the last column.",
                "/c:AbsRes", "Column index is retrieved for column: {0}", new string[] { "colname" });
        }

        /// <summary>
        /// Format specified option parameter value in logstring with a new (readable) string
        /// </summary>
        /// <param name="optionName"></param>
        /// <param name="parameter"></param>
        /// <param name="parameterValue"></param>
        /// <param name="parameterValues"></param>
        /// <returns>a readable form of specified parameter value</returns>
        protected override string FormatLogStringParameter(string optionName, string parameter, string parameterValue, List<string> parameterValues)
        {
            return parameterValue;

            //switch (optionName)
            //{
            //    case "x":
            //        switch (parameter)
            //        {
            //            case "x1":
            //                switch (parameterValue)
            //                {
            //                    case "Value1": return "Value1";
            //                    default: return parameterValue;
            //                }
            //            default: return parameterValue;
            //        }
            //    default: return parameterValue;
            //}
        }

        /// <summary>
        /// Parse and process obligatory tool parameter at (zero based) index parIndex
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex">returns the index for the argument group for these parameters, 0 if only a single group is defined</param>
        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length == 1)
            {
                // Parse syntax 1:
                IPFFilename = parameters[0];
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
                if (hasOptionParameters)
                {
                    ColumnName = optionParametersString;
                }
                else
                {
                    throw new ToolException("Parameter value expected for option '" + optionName + "'");
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
            if (IPFFilename != null)
            {
                string[] ipfFilenames = Directory.GetFiles(Path.GetDirectoryName(IPFFilename), Path.GetFileName(IPFFilename));
                if (ipfFilenames.Length > 0)
                {
                    IPFFilename = ipfFilenames[0];
                    if (ipfFilenames.Length > 1)
                    {
                        SIFTool.Log.AddWarning("Multiple files found, first file found used: " + IPFFilename);
                    }
                }
                else
                {
                    throw new ToolException("No IPF-file found for the specified path/filter: " + IPFFilename);
                }


                if (!File.Exists(IPFFilename))
                {
                    throw new ToolException("Input file does not exist: " + IPFFilename);
                }
            }
        }
    }
}
