// Sweco.SIF.Common is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.Common.
// 
// Sweco.SIF.Common is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.Common is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.Common. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace Sweco.SIF.Common
{
    /// <summary>
    /// Abstract base class for processing command-line arguments and storing settings of SIF tools
    /// </summary>
    public abstract class SIFToolSettingsBase
    {
        /// <summary>
        /// Command-line arguments
        /// </summary>
        public string[] Args { get; }

        /// <summary>
        /// Argument group that was parsed based on specified arguments, or -1 if no group was or could be parsed (yet)
        /// </summary>
        public int ParsedGroupIndex { get; set; }

        /// <summary>
        /// String that is show in log strings when no defaultvalue is defined for an option parameter
        /// </summary>
        protected const string DefaultParameterValueString = "[default]";

        /// <summary>
        /// Language definition for english culture as used in SIFToolSettings
        /// </summary>
        protected static CultureInfo EnglishCultureInfo = new CultureInfo("en-GB", false);

        /// <summary>
        /// SIF-tool object that corresponds with these settings
        /// </summary>
        protected SIFToolBase SIFTool { get; private set; }

        /// <summary>
        /// Arguments that have been parsed, including specified values
        /// </summary>
        protected List<ToolArgumentDescription> ParsedArgumentDescriptions { get; set; }

        // List of strings, one of which can be passed on the command-line to requested tool information. E.g. 'help' and/or 'info'
        private List<string> toolHelpArgStrings;

        // Lists per parametergroup with the possible tool parameters and their syntax, as shown in the tool usage header
        private Dictionary<int, List<ToolParameterDescription>> toolUsageParameterDescriptions;
        private Dictionary<int, List<ToolOptionDescription>> toolUsageOptionDescriptions;

        // General remarks per parametergroup to add before/after descriptions of all options
        private Dictionary<int, List<string>> toolUsageOptionPreRemarks;
        private Dictionary<int, List<string>> toolUsageOptionPostRemarks;
        // General remarks to add after examples
        private List<string> toolUsageFinalRemarks;

        /// <summary>
        /// Create SIFToolSettings object for specified command-line arguments
        /// </summary>
        public SIFToolSettingsBase(string[] args)
        {
            this.Args = args;
            toolHelpArgStrings = new List<string>() { "info", "help" };
            InitializeSettings();

            // Allow the subclass to define the tool syntax
            DefineToolSyntax();
        }

        private void InitializeSettings()
        {
            toolUsageParameterDescriptions = new Dictionary<int, List<ToolParameterDescription>>();
            toolUsageOptionDescriptions = new Dictionary<int, List<ToolOptionDescription>>();
            toolUsageOptionPreRemarks = new Dictionary<int, List<string>>();
            toolUsageOptionPostRemarks = new Dictionary<int, List<string>>();
            toolUsageFinalRemarks = new List<string>();
            ParsedArgumentDescriptions = new List<ToolArgumentDescription>();
            ParsedGroupIndex = -1;
        }

        /// <summary>
        /// Register SIFTool object that this SIFToolSettings object is used for
        /// </summary>
        /// <param name="sifTool"></param>
        public void RegisterSIFTool(SIFToolBase sifTool)
        {
            this.SIFTool = sifTool;
        }

        /// <summary>
        /// Parse optional and obligatory parameters and option parameters from arguments
        /// </summary>
        public virtual void ParseArguments()
        {
            if (Args == null)
            {
                throw new Exception("Arguments cannot be null, please define Args property");
            }

            if (SIFTool == null)
            {
                throw new Exception("SIFTool is not defined, please define via RegisterSIFTool() method");
            }

            // First parse options
            int idx = 0;
            while (IsOption(idx))
            {
                // Read option string, part before colon (':')
                string optionName = GetOptionString(idx);

                bool hasOptionParameters = HasOptionParameters(idx);
                string optionParametersString = GetOptionParameterString(idx);
                if ((optionParametersString != null) && (optionParametersString.Length == 0))
                {
                    throw new ToolException("No parameters found after colon for option '" + optionName + "', use '/" + optionName + "' if no parameter are needed");
                }

                // Allow subclass to parse and process the current option and rest of option string (part after colon) if present
                bool isOptionParsed = ParseOption(optionName, hasOptionParameters, optionParametersString);
                if (!isOptionParsed)
                {
                    throw new ToolException("Unknown commandline option " + (idx + 1) + ": " + Args[idx] + ". Run without arguments to see description.");
                }
                ParsedArgumentDescriptions.Add(new ToolOptionDescription(optionName, optionParametersString));

                idx++;
            }

            // Check if enough arguments are left
            if (Args.Length < idx + GetMinParameterCount())
            {
                throw new ToolException("Found " + idx + " options and " + (Args.Length - idx)
                    + " (other) argument(s), but expected number of (obligatory) arguments is " + GetMinParameterCount() + ". Check tool usage!");
            }

            // Combine (leftover) parameters
            List<string> parList = new List<string>();
            for (; idx < Args.Length; idx++)
            {
                parList.Add(Args[idx]);
            }

            // Allow subclass to parse pararameters and retrieve used group index
            ParseParameters(parList.ToArray(), out int groupIndex);
            this.ParsedGroupIndex = groupIndex;

            // (Re)add double quotes around empty parameters or parameters with spaces
            for (int parIdx = 0; parIdx < parList.Count; parIdx++)
            {
                if (parList[parIdx].Equals(string.Empty) || parList[parIdx].Contains(" "))
                {
                    parList[parIdx] = CommonUtils.EnsureDoubleQuotes(parList[parIdx]);
                }
            }

            // Check group index with parameter count and save specified parameters
            if (toolUsageParameterDescriptions.ContainsKey(groupIndex))
            {
                List<ToolParameterDescription> parameterDescriptions = toolUsageParameterDescriptions[groupIndex];
                bool hasRepetitiveParameterDescriptions = HasRepetitiveParameterDescriptions(parameterDescriptions);
                if (hasRepetitiveParameterDescriptions)
                {
                    if (parList.Count < (parameterDescriptions.Count - 1))
                    {
                        throw new Exception("Invalid number of parameters specified: " + CommonUtils.ToString(parList)
                        + "\r\nFor group index " + groupIndex + " the minimal number of expected parameters is " + (parameterDescriptions.Count - 1) + ", while " + parList.Count + " parameters were specified.");
                    }
                }
                else if (parList.Count != parameterDescriptions.Count)
                {
                    throw new Exception("Invalid number of parameters specified: " + CommonUtils.ToString(parList)
                    + "\r\nFor group index " + groupIndex + " the number of expected parameters is " + parameterDescriptions.Count + ", while " + parList.Count + " parameters were specified.");
                }

                // Add parameter name and value to list of parsed arguments
                for (int parIdx = 0; parIdx < parameterDescriptions.Count; parIdx++)
                {
                    ToolParameterDescription parameterDescription = parameterDescriptions[parIdx];
                    string specifiedParameterValue = string.Empty;
                    if (parameterDescription.IsRepetitive)
                    {
                        for (int specifiedParListIdx = parIdx; specifiedParListIdx < parList.Count; specifiedParListIdx++)
                        {
                            specifiedParameterValue += " " + parList[specifiedParListIdx];
                        }
                    }
                    else
                    {
                        specifiedParameterValue = parList[parIdx];
                    }
                    ParsedArgumentDescriptions.Add(new ToolParameterDescription(parameterDescription.Name, specifiedParameterValue));
                }
            }
            else if (toolUsageOptionDescriptions.ContainsKey(groupIndex))
            {
                // Ignore group without parameters, e.g. to allow the tool to start in GUI mode with some specific settings
            }
            else
            {
                throw new Exception("Unexisting group returned (" + groupIndex + ") by method ParseParameters() for parameters: " + CommonUtils.ToString(parList));
            }
        }

        /// <summary>
        /// Check if one of the parameter descriptions in the specified list is repetitive
        /// </summary>
        /// <param name="parameterDescriptions"></param>
        /// <returns></returns>
        private bool HasRepetitiveParameterDescriptions(List<ToolParameterDescription> parameterDescriptions)
        {
            for (int idx = 0; idx < parameterDescriptions.Count; idx++)
            {
                if (parameterDescriptions[idx].IsRepetitive)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Write specified tool parameters to log and/or console
        /// </summary>
        /// <param name="log">if null, settings are written only to console</param>
        public virtual void LogSettings(Log log)
        {
            // First write tool parameters
            foreach (ToolArgumentDescription argDescription in ParsedArgumentDescriptions)
            {
                if (argDescription is ToolParameterDescription)
                {
                    log.AddInfo(argDescription.Name + ": " + argDescription.Value, 1);
                }
            }

            // Write tool options
            foreach (ToolArgumentDescription argDescription in ParsedArgumentDescriptions)
            {
                if (argDescription is ToolOptionDescription)
                {
                    ToolOptionDescription optionDescription = GetOptionDescription(argDescription.Name, ParsedGroupIndex);
                    if ((optionDescription != null) && (optionDescription.LogSettingsFormatString != null))
                    {
                        // Use defined format string for logging settings of this parameter
                        string logString = optionDescription.LogSettingsFormatString;

                        // Retrieve parametervalues as specified by user for this option
                        List<string> optionParameterValues = new List<string>();
                        if (argDescription.Value != null)
                        {
                            int maxOptionParameterCount = ((optionDescription.Parameters != null) ? optionDescription.Parameters.Length : 0)
                                + ((optionDescription.OptionalParameters != null) ? optionDescription.OptionalParameters.Length : 0);

                            if (maxOptionParameterCount > 1)
                            {
                                string[] stringValues = CommonUtils.SplitQuoted(argDescription.Value, ',', '\'', true);
                                optionParameterValues.AddRange(stringValues);  // argDescription.Value.Split(new char[] { ',' }))
                            }
                            else
                            {
                                // ensre that parameter string is not split if max expected parameters is one
                                optionParameterValues.Add(argDescription.Value);  // argDescription.Value.Split(new char[] { ',' }))
                            }
                        }

                        // Replace specified empty values for optional parameters by defaultvalues if defined
                        for (int parIdx = 0; parIdx < optionParameterValues.Count; parIdx++)
                        {
                            if (optionParameterValues[parIdx].Equals(string.Empty))
                            {
                                // An empty value was specified
                                int optionalParameterIdx = parIdx - ((optionDescription.Parameters != null) ? optionDescription.Parameters.Length : 0);
                                if ((optionDescription.OptionalParameterDefaults != null) && (optionalParameterIdx >= 0) && (optionalParameterIdx < optionDescription.OptionalParameterDefaults.Length))
                                {
                                    optionParameterValues[parIdx] = optionDescription.OptionalParameterDefaults[optionalParameterIdx];
                                }
                            }
                        }

                        // Add default strings for missing option parameters
                        if (optionDescription.OptionalParameters != null)
                        {
                            // Calculate maximum of (expected and optional) parameters. Note: a '...' value for an optional parameter is not replaced with int.MaxValue here.
                            int maxParameterCount = ((optionDescription.Parameters != null) ? optionDescription.Parameters.Length : 0) + optionDescription.OptionalParameters.Length;
                            for (int parIdx = optionParameterValues.Count; parIdx < maxParameterCount; parIdx++)
                            {
                                int optionalParameterIdx = parIdx - ((optionDescription.Parameters != null) ? optionDescription.Parameters.Length : 0);
                                if ((optionDescription.OptionalParameterDefaults != null) && (optionalParameterIdx >= 0) && (optionalParameterIdx < optionDescription.OptionalParameterDefaults.Length))
                                {
                                    // A default value has been defined for this option
                                    optionParameterValues.Add(optionDescription.OptionalParameterDefaults[optionalParameterIdx]);
                                }
                                else
                                {
                                    // No default value has been defined for this option, use string '[default]' when name of optional parameter is not equal to '...'
                                    if ((optionalParameterIdx >= 0) && ((optionalParameterIdx >= optionDescription.OptionalParameters.Length) 
                                        || !optionDescription.OptionalParameters[optionalParameterIdx].Equals("...")))
                                    {
                                        optionParameterValues.Add(DefaultParameterValueString);
                                    }
                                }
                            }
                        }

                        // Retrieve numbers of parameters for this option
                        int optionDescriptionParametersLength = (optionDescription.Parameters != null) ? optionDescription.Parameters.Length : 0;
                        int optionDescriptionOptionalParametersLength = (optionDescription.OptionalParameters != null) ? optionDescription.OptionalParameters.Length : 0;
                        List<string> parameterList = (optionDescription.OptionalParameters != null) ? new List<string>(optionDescription.OptionalParameters) : null;
                        bool hasRepetitiveOptionalParameter = (parameterList != null) ? parameterList.Contains("...") : false;

                        // Replace parameter values by optional user defined formatting in subclass
                        List<string> formattedOptionParameterValues = new List<string>();
                        for (int optionParameterIdx = 0; optionParameterIdx < optionParameterValues.Count; optionParameterIdx++)
                        {
                            if (optionParameterIdx < optionDescriptionParametersLength)
                            {
                                formattedOptionParameterValues.Add(FormatLogStringParameter(optionDescription.Name, optionDescription.Parameters[optionParameterIdx], optionParameterValues[optionParameterIdx], optionParameterValues));
                            }
                            else if ((optionParameterIdx - optionDescriptionParametersLength) < optionDescriptionOptionalParametersLength)
                            {
                                formattedOptionParameterValues.Add(FormatLogStringParameter(optionDescription.Name, optionDescription.OptionalParameters[optionParameterIdx - optionDescriptionParametersLength],
                                    optionParameterValues[optionParameterIdx], optionParameterValues));
                            }
                            else if (hasRepetitiveOptionalParameter)
                            {
                                formattedOptionParameterValues.Add(FormatLogStringParameter(optionDescription.Name, "...", optionParameterValues[optionParameterIdx], optionParameterValues));
                            }
                            else
                            {
                                throw new Exception("Invalid number of option parameters for log format string: " + logString + ". Use {...} instead of {0} for multiple values. Do not use {...} for obligatory parameters.");
                            }
                        }
                        optionParameterValues = formattedOptionParameterValues;

                        if (logString.Contains("{...}"))
                        {
                            // replace with all optional parameters

                            // first retrieve indices of parameter references that are used in log string: e.g. reference {2} has index 2
                            List<int> usedParameterIndices = GetFormatStringIndices(logString);
                            // Assume remaining references are after last used reference
                            int lastUsedParIndex = (usedParameterIndices.Count > 0) ? usedParameterIndices[usedParameterIndices.Count - 1] : -1;
                            usedParameterIndices.Clear();
                            for (int parIdx = 0; parIdx <= lastUsedParIndex; parIdx++)
                            {
                                usedParameterIndices.Add(parIdx);
                            }

                            List<string> remainingParameters = CommonUtils.SelectItems<string>(optionParameterValues, usedParameterIndices, true);
                            logString = logString.Replace("{...}", CommonUtils.ToString(remainingParameters));
                            // remove optional parameters from option parameter values
                            for (int parIdx = 0; parIdx < remainingParameters.Count; parIdx++)
                            {
                                if (optionParameterValues.Contains(remainingParameters[parIdx]))
                                {
                                    optionParameterValues.Remove(remainingParameters[parIdx]);
                                }
                            }
                        }

                        try
                        {
                            if (optionParameterValues.Count > 0)
                            {
                                logString = string.Format(logString, optionParameterValues.ToArray());
                            }
                            log.AddInfo(logString, 1);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Could not format log settings for option '" + optionDescription.Name + "', check defined LogSettingsFormatString: " + optionDescription.LogSettingsFormatString, ex);
                        }
                    }
                    else
                    {
                        // Use default log strings for logging settings of this parameter
                        if (argDescription.Value != null)
                        {
                            log.AddInfo("option '" + argDescription.Name + "' was specified with value(s): " + argDescription.Value, 1);
                        }
                        else
                        {
                            log.AddInfo("option '" + argDescription.Name + "' was specified", 1);
                        }
                    }
                }
            }

            log.AddInfo();
        }

        /// <summary>
        /// Retrieve indices of format items (e.g. {1}) that are present in string for String.Format() method
        /// </summary>
        /// <param name="logString"></param>
        /// <returns>list of found indices or empty list when no format items were found</returns>
        protected List<int> GetFormatStringIndices(string logString)
        {
            List<int> indices = new List<int>();

            int charIdx = 0;
            while (charIdx < logString.Length)
            {
                if (logString[charIdx].Equals('{'))
                {
                    charIdx++;
                    string indexString = string.Empty;
                    while ((charIdx < logString.Length) && !logString[charIdx].Equals('}') && !logString[charIdx].Equals(',') && !logString[charIdx].Equals(':'))
                    {
                        // Parse format item and index
                        indexString += logString[charIdx++];
                    }
                    if (int.TryParse(indexString, out int index))
                    {
                        indices.Add(index);
                    }
                }
                else
                {
                    charIdx++;
                }
            }

            return indices;
        }

        /// <summary>
        /// Format specified option parameter value in logstring with a new (readable) string
        /// </summary>
        /// <param name="optionName">name of option for which a formatted parameter value is required</param>
        /// <param name="parameter">name of option parameter for which a formatted parameter value is required</param>
        /// <param name="parameterValue">the parameter value that has to be formatted</param>
        /// <param name="parameterValues">for reference, all specified parameter values for this option</param>
        /// <returns>a readable form of specified parameter value</returns>
        protected virtual string FormatLogStringParameter(string optionName, string parameter, string parameterValue, List<string> parameterValues)
        {
            // As a default, do not use special formatting and simply return parameter value
            return parameterValue;
        }

        /// <summary>
        /// Check the number of parsed arguments against the number of expected arguments. Override to check actual values.
        /// </summary>
        public virtual void CheckSettings()
        {
            // First check tool parameters (if this group has parameters)
            if (toolUsageParameterDescriptions.ContainsKey(this.ParsedGroupIndex))
            {
                List<ToolParameterDescription> expectedParameterDescriptions = toolUsageParameterDescriptions[this.ParsedGroupIndex];
                int usedParameterCount = 0;
                foreach (ToolArgumentDescription parsedArgumentDescription in ParsedArgumentDescriptions)
                {
                    if (parsedArgumentDescription is ToolParameterDescription)
                    {
                        usedParameterCount++;
                    }
                }
                if (usedParameterCount != expectedParameterDescriptions.Count)
                {
                    throw new ToolException("Number of specified parameters (" + usedParameterCount + ") does not equal expected number (" + expectedParameterDescriptions.Count + "). Check tool usage.");
                }
            }

            // Check tool options
            foreach (ToolArgumentDescription parsedArgDescription in ParsedArgumentDescriptions)
            {
                if (parsedArgDescription is ToolOptionDescription)
                {
                    ToolOptionDescription expectedOptionDescription = GetOptionDescription(parsedArgDescription.Name, ParsedGroupIndex);
                    if (expectedOptionDescription == null)
                    {
                        throw new ToolException("Option '" + parsedArgDescription.Name + "' is not allowed for syntax " + GetGroupNumber(ParsedGroupIndex) + ". Check tool usage.");
                    }

                    string[] usedOptionParameters = null;
                    int minExpectedOptionParameterCount = (expectedOptionDescription.Parameters != null) ? expectedOptionDescription.Parameters.Length : 0;
                    int maxExpectedOptionParameterCount = minExpectedOptionParameterCount + ((expectedOptionDescription.OptionalParameters != null) ? expectedOptionDescription.OptionalParameters.Length : 0);
                    if ((maxExpectedOptionParameterCount == 1) && (parsedArgDescription.Value != null))
                    {
                        // Ensure parameter string is not split if the expected number of parameters is one.
                        usedOptionParameters = new string[1];
                        usedOptionParameters[0] = parsedArgDescription.Value;
                    }
                    else
                    {
                        usedOptionParameters = GetOptionParameters(parsedArgDescription.Value);
                    }

                    if (expectedOptionDescription.Parameters != null)
                    {
                        List<string> parameterList = new List<string>(expectedOptionDescription.Parameters);
                        if (parameterList.Contains("..."))
                        {
                            maxExpectedOptionParameterCount = int.MaxValue;
                        }
                    }
                    if (expectedOptionDescription.OptionalParameters != null)
                    {
                        List<string> parameterList = new List<string>(expectedOptionDescription.OptionalParameters);
                        if (parameterList.Contains("..."))
                        {
                            maxExpectedOptionParameterCount = int.MaxValue;
                        }
                    }
                    int usedOptionParameterCount = (usedOptionParameters != null) ? usedOptionParameters.Length : 0;
                    if (usedOptionParameterCount < minExpectedOptionParameterCount)
                    {
                        throw new ToolException("Check tool usage, expected parameter count (" + minExpectedOptionParameterCount + ") for option '"
                            + parsedArgDescription.Name + "' does not match number of specified parameters: " + (parsedArgDescription.Value ?? "<no parameters specified>"));
                    }
                    else if (usedOptionParameterCount > maxExpectedOptionParameterCount)
                    {
                        throw new ToolException("Check tool usage, maximum expected parameter count (" + maxExpectedOptionParameterCount + ") for option '"
                            + parsedArgDescription.Name + "' is less than number of specified parameters: " + parsedArgDescription.Value);
                    }
                }
            }
        }

        /// <summary>
        /// Throws exception for invalid number of tool parameters
        /// </summary>
        /// <param name="parameters"></param>
        /// <param name="groupIndex"></param>
        protected void HandleInvalidParameterCount(string[] parameters, out int groupIndex)
        {
            if (parameters.Length == 0)
            {
                throw new Exception("Zero parameters is not supported, check groupindex definitions in tool implementation.");
            }
            else
            {
                throw new ToolException("Invalid number of parameters (" + parameters.Length + "), check tool usage");
            }
        }

        /// <summary>
        /// Retrieve the added ToolOptionDescrioption object with the specified option name and optional groupIndex
        /// </summary>
        /// <param name="name"></param>
        /// <param name="groupIndex">number of requested groupindex, or negative number to retrieve first description found</param>
        /// <returns></returns>
        protected ToolOptionDescription GetOptionDescription(string name, int groupIndex = -1)
        {
            if (groupIndex >= 0)
            {
                foreach (ToolOptionDescription optionDescription in toolUsageOptionDescriptions[groupIndex])
                {
                    if (optionDescription.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return optionDescription;
                    }
                }
            }
            else
            {
                foreach (int idx in toolUsageOptionDescriptions.Keys)
                {
                    foreach (ToolOptionDescription optionDescription in toolUsageOptionDescriptions[idx])
                    {
                        if (optionDescription.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return optionDescription;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieve the added ToolParameterDescrioption object with the specified parameter name and optional groupIndex
        /// </summary>
        /// <param name="name"></param>
        /// <param name="groupIndex">number of requested groupindex, or negative number to retrieve first description found</param>
        /// <returns></returns>
        protected ToolParameterDescription GetParameterDescription(string name, int groupIndex = -1)
        {
            if (groupIndex >= 0)
            {
                foreach (ToolParameterDescription parameterDescription in toolUsageParameterDescriptions[groupIndex])
                {
                    if (parameterDescription.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return parameterDescription;
                    }
                }
            }
            else
            {
                foreach (int idx in toolUsageParameterDescriptions.Keys)
                {
                    foreach (ToolParameterDescription parameterDescription in toolUsageParameterDescriptions[idx])
                    {
                        if (parameterDescription.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return parameterDescription;
                        }
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Show lines with possible tool syntax and explanation of options and parameters. And optionally show some example command-lines.
        /// </summary>
        internal void ShowToolSyntax(Log log)
        {
            if (SIFTool == null)
            {
                throw new Exception("SIFTool is not defined, please use RegisterSIFTool() method to define SIFTool");
            }

            // Determine maximum argument or option name length
            int maxNameLength = 0;
            foreach (int groupIndex in toolUsageParameterDescriptions.Keys)
            {
                foreach (ToolParameterDescription toolArgDescription in toolUsageParameterDescriptions[groupIndex])
                {
                    if (toolArgDescription.Name.Length > maxNameLength)
                    {
                        maxNameLength = toolArgDescription.Name.Length;
                    }
                }
            }
            foreach (int groupIndex in toolUsageOptionDescriptions.Keys)
            {
                foreach (ToolOptionDescription toolOptionDescription in toolUsageOptionDescriptions[groupIndex])
                {
                    if (toolOptionDescription.Name.Length > maxNameLength)
                    {
                        maxNameLength = toolOptionDescription.Name.Length;
                    }
                }
            }

            // Show tool syntax
            foreach (int groupIndex in toolUsageParameterDescriptions.Keys)
            {
                string syntaxString = BuildSyntaxString(groupIndex);
                if (syntaxString != null)
                {
                    if (toolUsageParameterDescriptions.Keys.Count > 1)
                    {
                        log.AddInfo("Syntax " + GetGroupNumber(groupIndex) + ": ", 0, false);
                    }
                    else
                    {
                        log.AddInfo("Syntax: ", 0, false);
                    }
                    log.AddInfo(syntaxString);
                }
            }

            // Write tool parameters
            List<string> shownParametersStrings = new List<string>();
            foreach (int groupIndex in toolUsageParameterDescriptions.Keys)
            {
                foreach (ToolParameterDescription toolParameterDescription in toolUsageParameterDescriptions[groupIndex])
                {
                    if (!shownParametersStrings.Contains(toolParameterDescription.Name))
                    {
                        log.AddInfo(toolParameterDescription.Name.PadRight(maxNameLength) + " - " + toolParameterDescription.Description.Replace("\n", "\n" + string.Empty.PadRight(maxNameLength + 3)));
                        shownParametersStrings.Add(toolParameterDescription.Name);
                    }
                }
            }

            // Write tool options
            List<string> shownOptionStrings = new List<string>();
            foreach (int groupIndex in toolUsageOptionDescriptions.Keys)
            {
                // Write option post remarks
                if (toolUsageOptionPreRemarks.ContainsKey(groupIndex))
                {
                    foreach (string remark in toolUsageOptionPreRemarks[groupIndex])
                    {
                        log.AddInfo(remark);
                    }
                }

                foreach (ToolOptionDescription toolOptionDescription in toolUsageOptionDescriptions[groupIndex])
                {
                    if (!shownOptionStrings.Contains(toolOptionDescription.Name))
                    {
                        log.AddInfo("/" + toolOptionDescription.Name.PadRight(maxNameLength - 1) + " - " + toolOptionDescription.Description.Replace("\n","\n" + string.Empty.PadRight(maxNameLength + 3)));
                        shownOptionStrings.Add(toolOptionDescription.Name);
                    }
                }

                // Write option post remarks
                if (toolUsageOptionPostRemarks.ContainsKey(groupIndex))
                {
                    foreach (string remark in toolUsageOptionPostRemarks[groupIndex])
                    {
                        log.AddInfo(remark);
                    }
                }
            }

            log.AddInfo();

            // Write example line(s) for each syntax group, for calling tool with command-line arguments
            foreach (int groupIndex in toolUsageParameterDescriptions.Keys)
            {
                string exampleString = BuildExampleString(groupIndex);
                if (exampleString != null)
                {
                    if (toolUsageParameterDescriptions.Keys.Count > 1)
                    {
                        log.AddInfo("Example " + GetGroupNumber(groupIndex) + ": ", 0, false);
                    }
                    else
                    {
                        log.AddInfo("Example: ", 0, false);
                    }
                    log.AddInfo(exampleString);
                }
            }

            // Write final remarks
            if (toolUsageFinalRemarks.Count > 0)
            {
                log.AddInfo();
                foreach (string remark in toolUsageFinalRemarks)
                {
                    log.AddInfo(remark);
                }
            }
        }

        private int GetGroupNumber(int groupIndex)
        {
            int groupNumber = 1;
            foreach (int definedGroupIndex in toolUsageParameterDescriptions.Keys)
            {
                if (groupIndex == definedGroupIndex)
                {
                    return groupNumber;
                }
                groupNumber++;
            }
            throw new Exception("Groupindex not defined: " + groupIndex);
        }

        /// <summary>
        /// Returns a string that represents this Settings object
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "SIFToolSettings instance for arguments: " + CommonUtils.ToString(new List<string>(Args));
        }

        /// <summary>
        /// Check if enough arguments have been passed by comparing argument count with MinArgCount property
        /// </summary>
        /// <returns></returns>
        public virtual bool HasEnoughArguments()
        {
            if (GetMinParameterCount() < 0)
            {
                // Negative MinArgCount, probably MinArgCount is not yet defined/overriden
                throw new Exception("Invalid MinArgCount-value (" + GetMinParameterCount()
                    + "), ensure that MinArgCount-property returns a positive value indicating the minimum number of (obligatory) command-line arguments.");
            }

            return (Args.Length >= GetMinParameterCount()) && !((Args.Length == 1) && IsToolHelpArgString(Args[0]));
        }

        /// <summary>
        /// Adds a string to the list of command-line arguments that will show help/info-text for this tool (e.g. 'info' or 'help')
        /// </summary>
        /// <param name="helpArgString"></param>
        protected void AddToolHelpArgString(string helpArgString)
        {
            this.toolHelpArgStrings.Add(helpArgString);
        }

        /// <summary>
        /// Adds strings to the list of command-line arguments that will show help/info-text for this tool (e.g. 'info' or 'help')
        /// </summary>
        protected void AddToolHelpArgString(string[] helpArgStrings)
        {
            this.toolHelpArgStrings.AddRange(helpArgStrings);
        }

        /// <summary>
        /// Adds a tool parameter description for the ToolUsage block
        /// </summary>
        /// <param name="name">short parameter name</param>
        /// <param name="description">short description, use '\n' for end-of-lines</param>
        /// <param name="example">valid example substring for this parameter</param>
        /// <param name="groupIndex">index to distinguish different groups of valid arguments</param>
        protected void AddToolParameterDescription(string name, string description, string example = null, int groupIndex = 0)
        {
            AddToolParameterDescription(name, description, example, false, new int[] { groupIndex });
        }

        /// <summary>
        /// Adds a tool parameter description for the ToolUsage block
        /// </summary>
        /// <param name="name">short parameter name</param>
        /// <param name="description">short description, use '\n' for end-of-lines</param>
        /// <param name="example">valid example substring for this parameter</param>
        /// <param name="isRepetitive">if true parameter can be specified zero or more times</param>
        /// <param name="groupIndex">index to distinguish different groups of valid arguments</param>
        protected void AddToolParameterDescription(string name, string description, string example, bool isRepetitive, int groupIndex = 0)
        {
            AddToolParameterDescription(name, description, example, isRepetitive, new int[] { groupIndex });
        }

        /// <summary>
        /// Adds a tool parameter description for the ToolUsage block
        /// </summary>
        /// <param name="name">short parameter name</param>
        /// <param name="description">short description, use '\n' for end-of-lines</param>
        /// <param name="example">valid example substring for this parameter</param>
        /// <param name="groupIndices">indices to distinguish different groups of valid arguments, or null to only use index 0</param>
        protected void AddToolParameterDescription(string name, string description, string example, int[] groupIndices)
        {
            AddToolParameterDescription(name, description, example, false, groupIndices);
        }

        /// <summary>
        /// Adds a tool parameter description for the ToolUsage block
        /// </summary>
        /// <param name="name">short parameter name</param>
        /// <param name="description">short description, use '\n' for end-of-lines</param>
        /// <param name="example">valid example substring for this parameter</param>
        /// <param name="isRepetitive">if true parameter can be specified zero or more times</param>
        /// <param name="groupIndices">indices to distinguish different groups of valid arguments, or null to only use index 0</param>
        protected void AddToolParameterDescription(string name, string description, string example, bool isRepetitive, int[] groupIndices)
        {
            if (groupIndices == null)
            {
                groupIndices = new int[] { 0 };
            }

            foreach (int groupIndex in groupIndices)
            {
                if (!toolUsageParameterDescriptions.ContainsKey(groupIndex))
                {
                    // Add new (empty) group
                    toolUsageParameterDescriptions.Add(groupIndex, new List<ToolParameterDescription>());
                }
                List<ToolParameterDescription> parameterGroupDescriptions = toolUsageParameterDescriptions[groupIndex];

                // Check that previous parameter is not repetitive
                if ((parameterGroupDescriptions.Count > 0) && parameterGroupDescriptions[parameterGroupDescriptions.Count - 1].IsRepetitive)
                {
                    throw new Exception("Repetative parameter should be the last parameter of a parameter group, parameter '" + name + "' cannot be added");
                }

                toolUsageParameterDescriptions[groupIndex].Add(new ToolParameterDescription(name, description, example, isRepetitive));
            }
        }

        /// <summary>
        /// Adds an option description for the ToolUsage block
        /// </summary>
        /// <param name="name">option name, often a single character</param>
        /// <param name="description">use '\n' for end-of-lines</param>
        /// <param name="example">valid example substring for this option, including slash</param>
        /// <param name="logSettingsFormatString">format string (see string.Format() for more info) to use for logging settings for this option. Use {...} for remaining optionparameters.</param>
        /// <param name="parameters">short (syntax) names of parameters for this option</param>
        /// <param name="optionalParameters">short (syntax) names of optional parameters for this option. Use '...' for undefined number of optional parameters.</param>
        /// <param name="optionalParameterDefaults">default values for optional parameters for this option</param>
        /// <param name="groupIndex">index to distinguish different groups of valid arguments</param>
        protected void AddToolOptionDescription(string name, string description, string example = null, string logSettingsFormatString = null, string[] parameters = null, string[] optionalParameters = null, string[] optionalParameterDefaults = null, int groupIndex = 0)
        {
            AddToolOptionDescription(name, description, example, logSettingsFormatString, parameters, optionalParameters, optionalParameterDefaults, new int[] { groupIndex });
        }

        /// <summary>
        /// Adds an option description for the ToolUsage block
        /// </summary>
        /// <param name="name">option name, often a single character</param>
        /// <param name="description">short description, use '\n' for end-of-lines</param>
        /// <param name="example">valid example substring for this option, including slash</param>
        /// <param name="logSettingsFormatString">format string (see string.Format() for more info) to use for logging settings for this option. Use {...} for remaining optionparameters.</param>
        /// <param name="parameters">short (syntax) names of parameters for this option</param>
        /// <param name="optionalParameters">short (syntax) names of optional parameters for this option. Use '...' for undefined number of optional parameters.</param>
        /// <param name="optionalParameterDefaults">default values for optional parameters for this option</param>
        /// <param name="groupIndices">indices to distinguish different groups of valid arguments, or null to only use index 0</param>
        protected void AddToolOptionDescription(string name, string description, string example, string logSettingsFormatString, string[] parameters, string[] optionalParameters, string[] optionalParameterDefaults, int[] groupIndices)
        {
            if (groupIndices == null)
            {
                groupIndices = new int[] { 0 };
            }

            foreach (int groupIndex in groupIndices)
            {
                if (!toolUsageOptionDescriptions.ContainsKey(groupIndex))
                {
                    toolUsageOptionDescriptions.Add(groupIndex, new List<ToolOptionDescription>());
                }
                toolUsageOptionDescriptions[groupIndex].Add(new ToolOptionDescription(name, description, parameters, optionalParameters, optionalParameterDefaults, example, logSettingsFormatString));
            }
        }

        /// <summary>
        /// Reassign groupindices for the specified parameter description. The parameter description is removed (if present) from other groups.
        /// </summary>
        /// <param name="name">parameter name to replace</param>
        /// <param name="groupIndices">array of syntax group indices to reassign this parameter to</param>
        protected void ReplaceToolParameterDescription(string name, int[] groupIndices)
        {
            if (groupIndices == null)
            {
                groupIndices = new int[] { 0 };
            }

            // First remove current descriptions and store first description for replacement
            ToolParameterDescription parameterDescription = null;
            foreach (int groupIndex in groupIndices)
            {
                if (toolUsageParameterDescriptions.ContainsKey(groupIndex))
                {
                    int idx = FindToolParameterDescriptionListIndex(name, groupIndex);
                    if (idx >= 0)
                    {
                        parameterDescription = toolUsageParameterDescriptions[groupIndex][idx];
                        toolUsageParameterDescriptions[groupIndex].RemoveAt(idx);
                    }
                }
            }
            // Remove specified parameter from remaining other groups
            foreach (int groupIndex in toolUsageParameterDescriptions.Keys)
            {
                int idx = FindToolParameterDescriptionListIndex(name, groupIndex);
                if (idx >= 0)
                {
                    toolUsageParameterDescriptions[groupIndex].RemoveAt(idx);
                }
            }

            if (parameterDescription != null)
            {
                // Add description again
                AddToolParameterDescription(name, parameterDescription.Description, parameterDescription.Example, groupIndices);
            }
        }

        /// <summary>
        /// Replace the description of option name in the specified syntax group with the specified values
        /// </summary>
        /// <param name="name">parameter name to replace</param>
        /// <param name="description">short description, use '\n' for end-of-lines</param>
        /// <param name="example">valid example substring for this parameter</param>
        /// <param name="isRepetitive">if true parameter can be specified zero or more times</param>
        /// <param name="groupIndex">syntax group index to replace this option for</param>
        protected void ReplaceToolParameterDescription(string name, string description, string example, bool isRepetitive, int groupIndex = 0)
        {
            ReplaceToolParameterDescription(name, description, example, isRepetitive, new int[] { groupIndex });
        }

        /// <summary>
        /// Replace the description of option name in all syntax groups with the specified values
        /// </summary>
        /// <param name="name">parameter name to replace</param>
        /// <param name="description">short description, use '\n' for end-of-lines</param>
        /// <param name="example">valid example substring for this parameter</param>
        /// <param name="isRepetitive">if true parameter can be specified zero or more times</param>
        /// <param name="groupIndices">array of syntax group indices to replace this option for</param>
        protected void ReplaceToolParameterDescription(string name, string description, string example, bool isRepetitive, int[] groupIndices)
        {
            if (groupIndices == null)
            {
                groupIndices = new int[] { 0 };
            }

            // First remove current descriptions
            foreach (int groupIndex in groupIndices)
            {
                if (toolUsageParameterDescriptions.ContainsKey(groupIndex))
                {
                    int idx = FindToolParameterDescriptionListIndex(name, groupIndex);
                    if (idx >= 0)
                    {
                        toolUsageParameterDescriptions[groupIndex][idx] = new ToolParameterDescription(name, description, example, isRepetitive);
                    }
                }
            }
        }

        /// <summary>
        /// Insert the specified parameter description before an existing other parameter
        /// </summary>
        /// <param name="name">short parameter name of parameter to insert</param>
        /// <param name="description">short description, use '\n' for end-of-lines</param>
        /// <param name="example">valid example substring for this parameter</param>
        /// <param name="groupIndices">array of syntax group indices to insert this option for</param>
        /// <param name="otherName">name of other parameter before which this new parameter should be inserted</param>
        protected void InsertToolParameterDescription(string name, string description, string example, int[] groupIndices, string otherName)
        {
            if (groupIndices == null)
            {
                groupIndices = new int[] { 0 };
            }

            // First remove current descriptions
            foreach (int groupIndex in groupIndices)
            {
                if (!toolUsageParameterDescriptions.ContainsKey(groupIndex))
                {
                    toolUsageParameterDescriptions.Add(groupIndex, new List<ToolParameterDescription>());
                }

                int idx = FindToolParameterDescriptionListIndex(otherName, groupIndex);
                if (idx >= 0)
                {
                    // Insert parameter before other parameter
                    toolUsageParameterDescriptions[groupIndex].Insert(idx, new ToolParameterDescription(name, description, example));
                }
                else
                {
                    // Add parameter at end of list
                    toolUsageParameterDescriptions[groupIndex].Add(new ToolParameterDescription(name, description, example));
                }
            }
        }

        /// <summary>
        /// Reassign groupindices for the specified option description. The option description is removed (if present) from other groups.
        /// </summary>
        /// <param name="name">option name to replace</param>
        /// <param name="groupIndices">array of syntax group indices to replace option for</param>
        protected void ReplaceToolOptionDescription(string name, int[] groupIndices)
        {
            if (groupIndices == null)
            {
                groupIndices = new int[] { 0 };
            }

            // First remove current descriptions and store first description for replacement
            ToolOptionDescription optionDescription = null;
            foreach (int groupIndex in groupIndices)
            {
                if (toolUsageOptionDescriptions.ContainsKey(groupIndex))
                {
                    int idx = FindToolOptionDescriptionListIndex(name);
                    if (idx >= 0)
                    {
                        optionDescription = toolUsageOptionDescriptions[groupIndex][idx];
                        toolUsageOptionDescriptions[groupIndex].RemoveAt(idx);
                    }
                }
            }

            if (optionDescription != null)
            {
                // Add description again
                AddToolOptionDescription(name, optionDescription.Description, optionDescription.Example, optionDescription.LogSettingsFormatString, optionDescription.Parameters, optionDescription.OptionalParameters, optionDescription.OptionalParameterDefaults, groupIndices);
            }
        }

        /// <summary>
        /// Replace or add the description of option name in the specified syntax group with the specified values
        /// </summary>
        /// <param name="name">option name to replace</param>
        /// <param name="description">short description, use '\n' for end-of-lines</param>
        /// <param name="example">valid example substring for this option, including slash</param>
        /// <param name="logSettingsFormatString">format string (see string.Format() for more info) to use for logging settings for this option</param>
        /// <param name="parameters">short (syntax) names of parameters for this option</param>
        /// <param name="optionalParameters">short (syntax) names of optional parameters for this option. Use '...' for undefined number of optional parameters.</param>
        /// <param name="optionalParameterDefaults">default values for optional parameters for this option</param>
        /// <param name="groupIndex">syntax group index to replace this option for</param>
        protected void ReplaceToolOptionDescription(string name, string description, string example, string logSettingsFormatString, string[] parameters, string[] optionalParameters, string[] optionalParameterDefaults, int groupIndex = 0)
        {
            ReplaceToolOptionDescription(name, description, example, logSettingsFormatString, parameters, optionalParameters, optionalParameterDefaults, new int[] { groupIndex });
        }

        /// <summary>
        /// Replace or add the description of option name in all syntax groups with the specified values
        /// </summary>
        /// <param name="name">option name to replace</param>
        /// <param name="description">short description, use '\n' for end-of-lines</param>
        /// <param name="example">valid example substring for this option, including slash</param>
        /// <param name="logSettingsFormatString">format string (see string.Format() for more info) to use for logging settings for this option</param>
        /// <param name="parameters">short (syntax) names of parameters for this option</param>
        /// <param name="optionalParameters">short (syntax) names of optional parameters for this option. Use '...' for undefined number of optional parameters.</param>
        /// <param name="optionalParameterDefaults">default values for optional parameters for this option</param>
        /// <param name="groupIndices">array of syntax group indices to replace this option for</param>
        protected void ReplaceToolOptionDescription(string name, string description, string example, string logSettingsFormatString, string[] parameters, string[] optionalParameters, string[] optionalParameterDefaults, int[] groupIndices)
        {
            if (groupIndices == null)
            {
                groupIndices = new int[] { 0 };
            }

            // First remove current descriptions
            foreach (int groupIndex in groupIndices)
            {
                if (toolUsageOptionDescriptions.ContainsKey(groupIndex))
                {
                    int idx = FindToolOptionDescriptionListIndex(name, groupIndex);
                    if (idx >= 0)
                    {
                        // Replace option description
                        toolUsageOptionDescriptions[groupIndex][idx] = new ToolOptionDescription(name, description, parameters, optionalParameters, optionalParameterDefaults, example, logSettingsFormatString); 
                    }
                    else
                    {
                        toolUsageOptionDescriptions[groupIndex].Add(new ToolOptionDescription(name, description, parameters, optionalParameters, optionalParameterDefaults, example, logSettingsFormatString));
                    }
                }
                else
                {
                    toolUsageOptionDescriptions.Add(groupIndex, new List<ToolOptionDescription>());
                    toolUsageOptionDescriptions[groupIndex].Add(new ToolOptionDescription(name, description, parameters, optionalParameters, optionalParameterDefaults, example, logSettingsFormatString));
                }
            }
        }

        /// <summary>
        /// Search index of specified ToolOptionDescription item
        /// </summary>
        /// <param name="name">option name to find</param>
        /// <param name="groupIndex">syntax group index of option to find</param>
        /// <returns></returns>
        private int FindToolOptionDescriptionListIndex(string name, int groupIndex = 0)
        {
            if (toolUsageOptionDescriptions.ContainsKey(groupIndex))
            {
                List<ToolOptionDescription> optionDescriptionList = toolUsageOptionDescriptions[groupIndex];
                for (int idx = 0; idx < optionDescriptionList.Count; idx++)
                {
                    if (optionDescriptionList[idx].Name.Equals(name))
                    {
                        return idx;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Search index of specified ToolParameterDescription item
        /// </summary>
        /// <param name="name">parameter name to find</param>
        /// <param name="groupIndex">syntax group index of parameter to find</param>
        /// <returns></returns>
        private int FindToolParameterDescriptionListIndex(string name, int groupIndex = 0)
        {
            if (toolUsageParameterDescriptions.ContainsKey(groupIndex))
            {
                List<ToolParameterDescription> parameterDescriptionList = toolUsageParameterDescriptions[groupIndex];
                for (int idx = 0; idx < parameterDescriptionList.Count; idx++)
                {
                    if (parameterDescriptionList[idx].Name.Equals(name))
                    {
                        return idx;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Add remark string that will be shown directly before all individual option decription lines, but before tool examples
        /// </summary>
        /// <param name="remark"></param>
        /// <param name="groupIndex">syntax group index of parameter to find</param>
        protected void AddToolUsageOptionPreRemark(string remark, int groupIndex = 0)
        {
            if (!toolUsageOptionPreRemarks.ContainsKey(groupIndex))
            {
                toolUsageOptionPreRemarks.Add(groupIndex, new List<string>());
            }
            toolUsageOptionPreRemarks[groupIndex].Add(remark);
        }

        /// <summary>
        /// Replace (partial) pre option remark string in all ToolUsageOptionRemark strings with specified replaceString
        /// </summary>
        /// <param name="searchString"></param>
        /// <param name="replaceString"></param>
        /// <param name="groupIndex">syntax group index of parameter to find</param>
        protected void ReplaceToolUsageOptionPreRemark(string searchString, string replaceString, int groupIndex = 0)
        {
            for (int remarkIdx = 0; remarkIdx < toolUsageOptionPreRemarks[groupIndex].Count; remarkIdx++)
            {
                string remark = toolUsageOptionPreRemarks[groupIndex][remarkIdx];
                if (remark.Contains(searchString))
                {
                    toolUsageOptionPreRemarks[groupIndex][remarkIdx] = remark.Replace(searchString, replaceString);
                }
            }
        }

        /// <summary>
        /// Add remark string that will be shown directly after all individual option decription lines, but before tool examples
        /// </summary>
        /// <param name="remark"></param>
        /// <param name="groupIndex">syntax group index of parameter to find</param>
        protected void AddToolUsageOptionPostRemark(string remark, int groupIndex = 0)
        {
            if (!toolUsageOptionPostRemarks.ContainsKey(groupIndex))
            {
                toolUsageOptionPostRemarks.Add(groupIndex, new List<string>());
            }
            toolUsageOptionPostRemarks[groupIndex].Add(remark);
        }

        /// <summary>
        /// Replace (partial) post option remark string in all ToolUsageOptionRemark strings with specified replaceString
        /// </summary>
        /// <param name="searchString"></param>
        /// <param name="replaceString"></param>
        /// <param name="groupIndex">syntax group index of parameter to find</param>
        protected void ReplaceToolUsageOptionPostRemark(string searchString, string replaceString, int groupIndex = 0)
        {
            for (int remarkIdx = 0; remarkIdx < toolUsageOptionPostRemarks[groupIndex].Count; remarkIdx++)
            {
                string remark = toolUsageOptionPostRemarks[groupIndex][remarkIdx];
                if (remark.Contains(searchString))
                {
                    toolUsageOptionPostRemarks[groupIndex][remarkIdx] = remark.Replace(searchString, replaceString);
                }
            }
        }

        /// <summary>
        /// Remove all post option remarks that contain one of the specified substrings, or remove all post option remarks when null is specified
        /// </summary>
        /// <param name="remarkSubstrings">list of substrings or null to remove all post option remarks</param>
        /// <param name="groupIndices">array with indices of groups to check, or null to check group index 0</param>
        protected void RemoveToolUsageOptionPostRemarks(List<string> remarkSubstrings = null, int[] groupIndices = null)
        {
            if (groupIndices == null)
            {
                groupIndices = new int[] { 0 };
            }

            foreach (int groupIndex in groupIndices)
            {
                for (int remarkIdx = 0; remarkIdx < toolUsageOptionPostRemarks[groupIndex].Count; remarkIdx++)
                {
                    string remark = toolUsageOptionPostRemarks[groupIndex][remarkIdx];

                    // check substrings
                    bool isRemarkRemoved = false;
                    if (remarkSubstrings != null)
                    {
                        foreach (string substring in remarkSubstrings)
                        {
                            if (remark.Contains(substring))
                            {
                                isRemarkRemoved = true;
                            }
                        }
                    }
                    else
                    {
                        isRemarkRemoved = true;
                    }

                    if (isRemarkRemoved)
                    {
                        toolUsageOptionPostRemarks[groupIndex].RemoveAt(remarkIdx);
                    }
                }
            }
        }

        /// <summary>
        /// Add remark string that will be shown below tool exmaples
        /// </summary>
        /// <param name="remark"></param>
        protected void AddToolUsageFinalRemark(string remark)
        {
            toolUsageFinalRemarks.Add(remark);
        }

        /// <summary>
        /// Replace (partial) remark string in all ToolUsageFinalRemark strings with specified replaceString
        /// </summary>
        /// <param name="searchString"></param>
        /// <param name="replaceString"></param>
        protected void ReplaceToolUsageFinalRemark(string searchString, string replaceString)
        {
            for (int remarkIdx = 0; remarkIdx < toolUsageFinalRemarks.Count; remarkIdx++)
            {
                string remark = toolUsageFinalRemarks[remarkIdx];
                if (remark.Contains(searchString))
                {
                    toolUsageFinalRemarks[remarkIdx] = remark.Replace(searchString, replaceString);
                }
            }
        }

        /// <summary>
        /// Checks if the argument at specified index is an option, i.e. starts with a slash '/'
        /// </summary>
        /// <param name="idx"></param>
        /// <returns></returns>
        protected bool IsOption(int idx)
        {
            return IsOption(Args, idx);
        }

        /// <summary>
        /// Checks if the argument at specified index is an option, i.e. starts with a slash '/'
        /// </summary>
        /// <param name="args"></param>
        /// <param name="idx"></param>
        /// <returns></returns>
        protected static bool IsOption(string[] args, int idx)
        {
            if (idx < args.Length)
            {
                return args[idx].Trim().StartsWith("/");
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Retrieve option at specified index, i.e. the part before the (optional) colon
        /// </summary>
        /// <param name="idx">index for option in arguments array</param>
        /// <returns></returns>
        protected string GetOptionString(int idx)
        {
            return GetOptionString(Args, idx);
        }

        /// <summary>
        /// Retrieve option at specified index, i.e. the part before the (optional) colon
        /// </summary>
        /// <param name="args"></param>
        /// <param name="idx">index for option in arguments array</param>
        /// <returns></returns>
        protected static string GetOptionString(string[] args, int idx)
        {
            if ((args != null) && (idx < args.Length))
            {
                if (args[idx].Contains(":"))
                {
                    return args[idx].Substring(1, args[idx].IndexOf(":") - 1);
                }
                else
                {
                    return args[idx].Substring(1, args[idx].Length - 1);
                }
            }
            else
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Check if option at specified index has parameters after the (optional) colon symbol
        /// </summary>
        /// <param name="idx">index for option in arguments array</param>
        /// <returns></returns>
        protected bool HasOptionParameters(int idx)
        {
            return HasOptionParameters(Args, idx);
        }

        /// <summary>
        /// Check if option at specified index has parameters after the (optional) colon symbol
        /// </summary>
        /// <param name="args"></param>
        /// <param name="idx">index for option in arguments array</param>
        /// <returns></returns>
        protected static bool HasOptionParameters(string[] args, int idx)
        {
            return (args[idx].Length > 3) && (args[idx].IndexOf(':') >= 0);
        }

        /// <summary>
        /// Retrieve the full string with all parameters for the option at specified index, i.e. the string after (optional) colon symbol
        /// </summary>
        /// <param name="idx">index for option in arguments array</param>
        /// <returns>null if no parameters are present for the specified option</returns>
        protected string GetOptionParameterString(int idx)
        {
            return GetOptionParameterString(Args, idx);
        }

        /// <summary>
        /// Retrieve the full string with all parameters for the option at specified index, i.e. the string after (optional) colon symbol
        /// </summary>
        /// <param name="args"></param>
        /// <param name="idx">index for option in arguments array</param>
        /// <returns>null if no parameters are present for the specified option</returns>
        protected static string GetOptionParameterString(string[] args, int idx)
        {
            string argString = args[idx];
            int colonIdx = argString.IndexOf(':');
            if (colonIdx >= 2)  // as a minimum a slash and single character option string should be present before the colon symbol
            {
                return argString.Substring(colonIdx + 1, argString.Length - (colonIdx + 1));
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve a string array with all parameters (seperated by commas) for the option at specified index, i.e. the string after (optional) colon symbol
        /// </summary>
        /// <param name="idx">index for option in arguments array</param>
        /// <returns>null if no parameters are present for the specified option</returns>
        private string[] GetOptionParameters(int idx)
        {
            return GetOptionParameters(Args, idx);
        }

        /// <summary>
        /// Retrieve a string array with all parameters (seperated by commas) for the option at specified index, i.e. the string after (optional) colon symbol
        /// </summary>
        /// <param name="args"></param>
        /// <param name="idx">index for option in arguments array</param>
        /// <returns>null if no parameters are present for the specified option</returns>
        private static string[] GetOptionParameters(string[] args, int idx)
        {
            string parameterString = GetOptionParameterString(args, idx);
            if (parameterString != null)
            {
                return parameterString.Split(new char[] { ',' });
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve individual parameters from a string with comma separated parameters
        /// </summary>
        /// <param name="parameterString"></param>
        /// <param name="listSeperator">character that is used to seperate parameter strings</param>
        /// <param name="quote">character that is used to surround individual parameters that contain the list seperator; quotes are removed</param>
        /// <returns></returns>
        protected string[] GetOptionParameters(string parameterString, char listSeperator = ',', char quote = '\'')
        {
            if (parameterString != null)
            {
                return CommonUtils.SplitQuoted(parameterString, listSeperator, quote, true);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve full path for a valid (full or absolute) path, or throw ToolExpection for invalid paths
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        protected string ExpandPathArgument(string path)
        {
            if (path.Equals(string.Empty) || (path == null))
            {
                return path;
            }

            try
            {
                path = Path.GetFullPath(path);
            }
            catch (Exception ex)
            {
                throw new ToolException("Invalid path: " + path, ex);
            }

            return path;
        }

        /// <summary>
        /// Splits specified path to a directoryname and a filename. Input path is refers to either a directory or a filename (wildcards allowed)
        /// </summary>
        /// <param name="sourcePath">path to directory of specified source path</param>
        /// <param name="resultPath">directory specified by source path</param>
        /// <param name="resultFilename">filename if present in specified source path, or null otherwise</param>
        protected void SplitPathArgument(string sourcePath, out string resultPath, out string resultFilename)
        {
            if (Path.GetExtension(sourcePath).Length > 0)
            {
                resultFilename = Path.GetFileName(sourcePath);
                resultPath = Path.GetDirectoryName(sourcePath);
            }
            else
            {
                resultFilename = null;
                resultPath = sourcePath;
            }
        }

        /// <summary>
        /// Builds a syntax string from currently added arguments and options that shows how to call tool
        /// </summary>
        /// <returns>syntax string including toolname</returns>
        private string BuildSyntaxString(int groupIndex = 0)
        {
            if (toolUsageParameterDescriptions.ContainsKey(groupIndex))
            {
                string syntaxString = string.Empty;

                if (toolUsageOptionDescriptions.ContainsKey(groupIndex))
                {
                    // Add options
                    foreach (ToolOptionDescription toolOptionDescription in toolUsageOptionDescriptions[groupIndex])
                    {
                        syntaxString += " [/" + toolOptionDescription.Name;
                        if (((toolOptionDescription.Parameters != null) && (toolOptionDescription.Parameters.Length > 0)) 
                            || ((toolOptionDescription.OptionalParameters != null) && (toolOptionDescription.OptionalParameters.Length > 0)))
                        {
                            syntaxString += ":";

                            if (toolOptionDescription.Parameters != null)
                            {
                                for (int idx = 0; idx < toolOptionDescription.Parameters.Length; idx++)
                                {
                                    string optionParameterString = toolOptionDescription.Parameters[idx];
                                    if (idx > 0)
                                    {
                                        syntaxString += ",";
                                    }
                                    syntaxString += optionParameterString;
                                }
                            }

                            if (toolOptionDescription.OptionalParameters != null)
                            {
                                syntaxString += "[";
                                if ((toolOptionDescription.Parameters != null) && (toolOptionDescription.Parameters.Length > 0))
                                {
                                    syntaxString += ",";
                                }
                                for (int idx = 0; idx < toolOptionDescription.OptionalParameters.Length; idx++)
                                {
                                    string optionParameterString = toolOptionDescription.OptionalParameters[idx];
                                    if (idx > 0)
                                    {
                                        syntaxString += ",";
                                    }
                                    syntaxString += optionParameterString;
                                }
                                syntaxString += "]";
                            }
                        }
                        syntaxString += "]";
                    }
                }

                // Add parameters
                foreach (ToolParameterDescription toolParameterDescription in toolUsageParameterDescriptions[groupIndex])
                {
                    if (toolParameterDescription.IsRepetitive)
                    {
                        syntaxString += " [" + toolParameterDescription.Name + " ...]";
                    }
                    else
                    {
                        syntaxString += " " + toolParameterDescription.Name;
                    }
                }

                return ((SIFTool != null) ? (SIFTool.ToolName + " ") : string.Empty) + syntaxString.Trim();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Builds example string from currently added arguments and options that shows how to call tool
        /// </summary>
        /// <returns>syntax string including toolname</returns>
        private string BuildExampleString(int groupIndex)
        {
            if (toolUsageParameterDescriptions.ContainsKey(groupIndex))
            {
                string exampleString = string.Empty;

                if (toolUsageOptionDescriptions.ContainsKey(groupIndex))
                {
                    // Add options
                    foreach (ToolOptionDescription toolOptionDescription in toolUsageOptionDescriptions[groupIndex])
                    {
                        if (toolOptionDescription.Example != null)
                        {
                            // Check that for all obligatory option parameters a string is present
                            if (toolOptionDescription.Parameters != null)
                            {
                                string[] substrings = toolOptionDescription.Example.Split(new char[] { ',' });
                                if (toolOptionDescription.Parameters.Length > substrings.Length)
                                {
                                    throw new Exception("At least " + toolOptionDescription.Parameters.Length + " option parameters are expected in example for option '" + toolOptionDescription.Name + "': " + toolOptionDescription.Example);
                                }
                            }
                            exampleString += " " + toolOptionDescription.Example;
                        }
                    }
                }

                // Add parameters
                foreach (ToolParameterDescription toolParameterDescription in toolUsageParameterDescriptions[groupIndex])
                {
                    if (toolParameterDescription.Example != null)
                    {
                        exampleString += " " + toolParameterDescription.Example;
                    }
                    else
                    {
                        // if no example is present for a parameter skip example
                        if (exampleString.Equals(string.Empty))
                        {
                            return null;
                        }
                        else
                        {
                            throw new Exception("Add parameter example as well to show syntax example");
                        }
                    }
                }
                return ((SIFTool != null) ? (SIFTool.ToolName + " ") : string.Empty) + exampleString.Trim();
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve the minimum number of obligatory parameters at the command-line to run the tool
        /// </summary>
        /// <returns></returns>
        private int GetMinParameterCount()
        {
            int minParameterCount = int.MaxValue;
            foreach (int groupIndex in toolUsageParameterDescriptions.Keys)
            {
                List<ToolParameterDescription> parameterGroupDescriptions = toolUsageParameterDescriptions[groupIndex];
                int groupMinParameterCount = parameterGroupDescriptions.Count;
                if (parameterGroupDescriptions[groupMinParameterCount - 1].IsRepetitive)
                {
                    groupMinParameterCount--;
                }
                if (groupMinParameterCount < minParameterCount)
                {
                    minParameterCount = groupMinParameterCount;
                }
            }
            foreach (int groupIndex in toolUsageOptionDescriptions.Keys)
            {
                List<ToolOptionDescription> optionGroupDescriptions = toolUsageOptionDescriptions[groupIndex];
                int groupMinParameterCount = optionGroupDescriptions.Count;
                if (groupMinParameterCount < minParameterCount)
                {
                    // Check if option group is a group without parameters
                    if (!toolUsageParameterDescriptions.ContainsKey(groupIndex))
                    {
                        // This options is allowed for a group without parameters (i.e. to start GUI-tool with some specific setting)
                        minParameterCount = 0;
                    }
                }
            }
            if (minParameterCount == int.MaxValue)
            {
                throw new Exception("Please define tool parameter descriptions in SIFToolSettingsBase subclass with method AddToolParameterDescription()");
            }
            return minParameterCount;
        }

        /// <summary>
        /// Checks if the specified string is one of the argument strings that will show help/info-text for this tool
        /// </summary>
        private bool IsToolHelpArgString(string argString)
        {
            for (int idx = 0; idx < toolHelpArgStrings.Count; idx++)
            {
                if (toolHelpArgStrings[idx].Equals(argString, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Define the syntax of the tool as shown in the tool usage block. 
        /// Use one or more calls of the following methods: SetToolUsageHeader(), AddParameterDescription() and AddOptionDescription()
        /// </summary>
        protected abstract void DefineToolSyntax();

        /// <summary>
        /// Parse and process tool option
        /// </summary>
        /// <param name="optionName">the character(s) that identify this option</param>
        /// <param name="hasOptionParameters">true if this option has parameters</param>
        /// <param name="optionParametersString">a string with comma seperated parameters for this option when hasOptionParameters is true</param>
        /// <returns>true if recognized and processed</returns>
        protected abstract bool ParseOption(string optionName, bool hasOptionParameters, string optionParametersString = null);

        /// <summary>
        /// Parse and process obligatory tool parameter at (zero based) index parIndex
        /// </summary>
        /// <param name="parameters">array with parameter string from the command-line</param>
        /// <param name="groupIndex">returns the index for the argument group for these parameters, 0 if only a single group is defined</param>
        protected abstract void ParseParameters(string[] parameters, out int groupIndex);
    }
}
