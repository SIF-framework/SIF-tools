// RetrieveText is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of RetrieveText.
// 
// RetrieveText is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// RetrieveText is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with RetrieveText. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.RetrieveText
{
    public class SIFTool : SIFToolBase
    {
        #region Constructor

        /// <summary>
        /// Creates a SIFTool instance and initializes tool name and version and a Log object with the console as a default listener
        /// </summary>
        public SIFTool(SIFToolSettingsBase settings) : base(settings)
        {
            SetLicense(new SIFGPLLicense(this));
            settings.RegisterSIFTool(this);
        }

        #endregion

        /// <summary>
        /// Entry point of tool
        /// </summary>
        /// <param name="args">command-line arguments</param>
        static void Main(string[] args)
        {
            int exitcode = -1;
            SIFTool tool = null;
            try
            {
                // Use SwecoTool Framework to handle license check, write of toolname and version, parsing arguments, writing of logfile and if specified so handling exeptions
                SIFToolSettings settings = new SIFToolSettings(args);
                tool = new SIFTool(settings);

                if (settings.HasEnoughArguments())
                {
                    settings.ParseArguments();
                }

                exitcode = tool.Run(false, false, false);
            }
            catch (ToolException ex)
            {
                ExceptionHandler.HandleToolException(ex, tool?.Log);
                exitcode = 1;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, tool?.Log);
                exitcode = 1;
            }

            Environment.Exit(exitcode);
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected override void DefineToolProperties()
        {
            AddAuthor("Koen van der Hauw");
            ToolPurpose = "SIF-tool for reading the first line from specified file(s)";
        }

        /// <summary>
        /// Starts actual tool process after reading and checking settings
        /// </summary>
        /// <returns>resultcode: 0 for success, 1 for errors</returns>
        protected override int StartProcess()
        {
            int exitcode = 0;

            // Retrieve tool settings that have been parsed from the command-line arguments 
            SIFToolSettings settings = (SIFToolSettings)Settings;
            if (settings.IsDebugMode)
            {
                Log.ListenerLogLevels[0] = LogLevel.Debug;
            }

            string[] inputFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter);

            Log.AddMessage(LogLevel.Debug, "Processing input files ...");
            int fileCount = 0;
            List<string> lines = new List<string>();
            foreach (string inputFilename in inputFilenames)
            {
                Log.AddMessage(LogLevel.Debug, "Reading file " + Path.GetFileName(inputFilename) + " ...", 1);
                List<string> fileLines = File.ReadAllLines(inputFilename).ToList();

                fileLines = ProcessFileLines(fileLines, inputFilename, settings);

                AddLines(ref lines, fileLines);

                fileCount++;
            }

            PostprocessLines(lines, settings);

            ToolSuccessMessage = settings.IsDebugMode ? "Finished processing " + fileCount + " file(s)" : null;

            return exitcode;
        }

        protected virtual void PostprocessLines(List<string> lines, SIFToolSettings settings)
        {
            WriteResults(lines, settings);
        }

        protected virtual void WriteResults(List<string> lines, SIFToolSettings settings)
        {
            foreach (string line in lines)
            {
                Log.AddInfo(line);
            }
        }

        protected virtual List<string> ProcessFileLines(List<string> fileLines, string inputFilename, SIFToolSettings settings)
        {
            // Select line with specified linenumber from input file(s)
            if (settings.IsLineNumberSelected)
            {
                fileLines = SelectLineNumber(fileLines, inputFilename, settings);
            }

            return fileLines;
        }

        protected List<string> SelectLineNumber(List<string> inputLines, string inputFilename, SIFToolSettings settings)
        {
            int lineNumber = settings.LineNumber;
            if (lineNumber <= 0)
            {
                lineNumber = inputLines.Count() + lineNumber;
                if (lineNumber < 0)
                {
                    throw new ToolException("Specified line number (" + settings.LineNumber + ") results in line number (" + lineNumber + ") outside number of lines (" + inputLines.Count() + ")");
                }
            }
            List<string> selectedLines = new List<string>();
            if (lineNumber <= inputLines.Count())
            {
                selectedLines.Add(inputLines[lineNumber - 1]);
            }
            else
            {
                throw new ToolException("Invalid line number (" + lineNumber + ") for file '" + Path.GetFileName(inputFilename) + "' with " + inputLines.Count() + " lines");
            }

            return selectedLines;
        }

        protected virtual void AddLines(ref List<string> outputLines, List<string> inputLines)
        {
            foreach (string line in inputLines)
            {
                outputLines.Add(line);
            }
        }
    }
}
