// ReplaceLine is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of ReplaceLine.
// 
// ReplaceLine is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// ReplaceLine is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with ReplaceLine. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.ReplaceLine
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

                exitcode = tool.Run();
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
            Authors = new string[] { "Koen van der Hauw", "Iris Vreugdenhil" };
            ToolPurpose = "SIF-tool for replacing line at specified linenumber within a text file.";
        }

        /// <summary>
        /// Starts actual tool process after reading and checking settings
        /// </summary>
        /// <returns>resultcode: 0 for success, 1 for errors</returns>
        protected override int StartProcess()
        {
            int exitCode = 0;

            // Retrieve tool settings that have been parsed from the command-line arguments 
            SIFToolSettings settings = (SIFToolSettings)Settings;

            StringReader sr = null;
            StreamWriter sw = null;
            string outputText = string.Empty;
            bool isModified = false;
            StringBuilder outputTextBuilder = new StringBuilder();
            try
            {
                string fileString = File.ReadAllText(settings.Filename);
                // Read lines from inputfile and create resulting file in memory
                sr = new StringReader(fileString);
                int currentLineNumber = 0;
                string currentLine;
                while ((currentLine = sr.ReadLine()) != null)
                {
                    currentLineNumber++;
                    if (currentLineNumber != settings.Linenumber)
                    {
                        outputTextBuilder.AppendLine(currentLine);
                    }
                    else
                    {
                        outputTextBuilder.AppendLine(settings.NewLine);
                        if (settings.IsInserted)
                        {
                            // Add existing line after inserted line
                            outputTextBuilder.AppendLine(currentLine);
                            isModified = true;
                            ToolSuccessMessage = "Inserted line '" + settings.NewLine + "' above line number " + settings.Linenumber;
                        }
                        else
                        {
                            isModified = !currentLine.ToLower().Equals(settings.NewLine.ToLower());
                            if (isModified)
                            {
                                ToolSuccessMessage = "Replaced current string at line " + settings.Linenumber + ": '" + currentLine + "' with '" + settings.NewLine + "'";
                            }
                            else
                            {
                                ToolSuccessMessage = "No modification was necessary for line " + settings.Linenumber + ": '" + currentLine + "'";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not read file " + settings.Filename, ex);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                    sr = null;
                }
            }

            try
            {
                if (isModified)
                {
                    // test for backup option
                    if (settings.IsBackedUp)
                    {
                        string subdirName = SIFToolSettings.DefaultSubdirName;
                        if ((settings.BackupSubdirname != null) && (!settings.BackupSubdirname.Equals(string.Empty)))
                        {
                            subdirName = settings.BackupSubdirname;
                        }
                        string backupFilename = Path.Combine(Path.Combine(Path.GetDirectoryName(settings.Filename), subdirName), Path.GetFileNameWithoutExtension(settings.Filename) + "_" + DateTime.Now.ToString("ddMMyyyy_HHmmss") + Path.GetExtension(settings.Filename));
                        if (!Directory.Exists(Path.GetDirectoryName(backupFilename)))
                        {
                            Directory.CreateDirectory(Path.GetDirectoryName(backupFilename));
                        }
                        File.Copy(settings.Filename, backupFilename);
                    }

                    // Write result to file
                    sw = new StreamWriter(settings.Filename, false);
                    sw.Write(outputTextBuilder.ToString());
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not write file " + settings.Filename, ex);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                    sw = null;
                }
            }

            return exitCode;
        }
    }
}
