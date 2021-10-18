// iMODValidator is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODValidator.
// 
// iMODValidator is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODValidator is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODValidator. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.iMODValidator.Checks;
using Sweco.SIF.iMODValidator.Exceptions;
using Sweco.SIF.iMODValidator.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator
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
            AddAuthor("Koen van der Hauw");
            ToolPurpose = "SIF-tool for checking iMOD-models for a number of possible modelissues";
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
            Validator validator = null;
            try
            {
                base.Log.AddInfo("Starting iMODValidator from command-line: " + CommonUtils.ToString(settings.Args.ToList(), " "));

                validator = new Validator();
                validator.ToolName = ToolName;
                validator.ToolVersion = ToolVersion + ", " + CopyrightNotice;
                validator.LoadSettings(settings.SettingsFilename);
                validator.NoDataValue = iMODValidatorSettingsManager.Settings.DefaultNoDataValue;
                validator.Runfilename = settings.RunFilename;
                validator.OutputPath = settings.OutputPath;
                validator.IsModelValidated = true;

                Log.Filename = Path.Combine(validator.OutputPath, ToolName + "_" + DateTime.Now.ToString("dd-MM-yyyy HH:mm").Replace(":", ".") + ".log");

                foreach (Check check in CheckManager.Instance.Checks)
                {
                    // First try to load settings from file
                    try
                    {
                        iMODValidatorSettingsManager.LoadCheckSettings(check, check.Name, settings.SettingsFilename);
                        check.IsActive = ((check.Settings != null) && check.Settings.IsActiveDefault);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Could not load Check-settings for check " + check.Name, ex);
                    }
                }

                validator.Run(Log);
            }
            catch (AbortException)
            {
                HandleAbortException(Log);
            }
            catch (OutOfMemoryException ex)
            {
                Log.AddMessage(LogLevel.Trace, "Currently " + (GC.GetTotalMemory(true) / 1000000) + "Mb memory is in use.");
                throw ex;
            }
            finally
            {
                // try to log results
                if ((validator != null) && (validator.OutputPath != null))
                {
                    Log.WriteLogFile();
                }
                validator = null;
            }

            ToolSuccessMessage = null;

            return exitcode;
        }

        public void HandleAbortException(Log log, int logIndentLevel = 0, bool showMessageBox = false)
        {
            string msg = "Abort was requested. Checks are cancelled.";
            log.AddInfo("\r\n" + msg);
        }
    }
}
