// Tee is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Tee.
// 
// Tee is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Tee is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Tee. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Tee
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

        Queue<string> questionLineQueue;

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

                exitcode = tool.Run(false, false);
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

            System.Environment.Exit(exitcode);
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected override void DefineToolProperties()
        {
            Authors = new string[] { "Koen van der Hauw" };
            ToolPurpose = "SIF-tool for teeing standard output of some command to both standard output and a file";
        }

        /// <summary>
        /// Starts actual tool process after reading and checking settings
        /// </summary>
        /// <returns>resultcode: 0 for success, 1 for errors</returns>
        protected override int StartProcess()
        {
            int exitcode = 0;

            // Retrieve tool settings that have been parsed from the command-line arguments 
            SIFToolSettings settings = (SIFToolSettings) Settings;

            questionLineQueue = new Queue<string>();
            StreamWriter sw = null;
            string line;
            StringBuilder sb;
            int intval;
            char ch;
            try
            {
                // Create output file
                sw = new StreamWriter(settings.OutputFilename, settings.IsOutputAppended);

                if (settings.IsCharacterMode)
                {
                    // Read characters from console
                    sb = new StringBuilder();
                    while ((intval = Console.Read()) >= 0)
                    {
                        ch = Convert.ToChar(intval);
                        if ((ch == '\n') || (ch == '?'))
                        {
                            if (ch == '?')
                            {
                                sb.Append(ch);
                            }

                            int errorLevel = ProcessLine(sb.ToString(), sw, settings);
                            if (errorLevel > exitcode)
                            {
                                exitcode = errorLevel;
                            }
                            sb.Clear();
                        }
                        else
                        {
                            sb.Append(ch);
                        }
                    }
                }
                else
                {
                    // Read lines from console
                    while ((line = Console.ReadLine()) != null)
                    {
                        int errorLevel = ProcessLine(line, sw, settings);
                        if (errorLevel > exitcode)
                        {
                            exitcode = errorLevel;
                        }
                    }

                }
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }

            ToolSuccessMessage = null;

            return exitcode;
        }

        /// <summary>
        /// Process a line that was read from console
        /// </summary>
        /// <param name="line"></param>
        /// <param name="sw"></param>
        /// <param name="settings"></param>
        protected virtual int ProcessLine(string line, StreamWriter sw, SIFToolSettings settings)
        {
            if (!settings.IsQuestionEchoed)
            {
                Console.WriteLine(line);
            }
            else
            {
                HandleQuestionLines(line, sw, settings);
            }

            sw.WriteLine(line);
            sw.Flush();

            int errorlevel = 0;
            if (line.StartsWith(SIFToolSettings.ErrorMessagePrefix))
            {
                errorlevel = 1;
            }

            return errorlevel;
        }

        /// <summary>
        /// Handle lines when settings.IsQuestionEchoed is true. Line are cached until a line that ends with a question mark
        /// </summary>
        /// <param name="line"></param>
        /// <param name="sw"></param>
        /// <param name="settings"></param>
        protected void HandleQuestionLines(string line, StreamWriter sw, SIFToolSettings settings)
        {
            if (line.Trim().Replace("\r", string.Empty).Replace("\n", string.Empty).EndsWith("?"))
            {
                // Write queued lines
                StringBuilder sb = new StringBuilder();
                while (questionLineQueue.Count > 0)
                {
                    string queuedLine = questionLineQueue.Dequeue();
                    if (queuedLine.Trim().Equals(string.Empty))
                    {
                        sb.Clear();
                    }
                    else
                    {
                        sb.AppendLine(queuedLine);
                    }
                }
                Console.Write(sb.ToString());
                // Write question mark line
                Console.WriteLine(line);

                questionLineQueue.Clear();
            }
            else
            {
                // Check for empty lines
                if (line.Trim().Equals(string.Empty))
                {
                    questionLineQueue.Clear();
                }
                else
                {
                    // Queue line, but limit to defined maximum (including new line)
                    questionLineQueue.Enqueue(line);
                    if ((settings.MaxQuestionLines > 0) && (questionLineQueue.Count >= settings.MaxQuestionLines))
                    {
                        questionLineQueue.Dequeue();
                    }
                }
            }
        }
    }
}
