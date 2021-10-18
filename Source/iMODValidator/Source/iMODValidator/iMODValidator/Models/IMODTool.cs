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
using Sweco.SIF.iMODValidator.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Models
{
    public class IMODTool
    {
        public static void Start(string imfFilename, Log log, int indentLevel = 0)
        {
            string outputString;
            string iMODExecutablePath = iMODValidatorSettingsManager.Settings.iMODExecutablePath;
            if (!Path.IsPathRooted(iMODExecutablePath))
            {
                iMODExecutablePath = Path.Combine(Directory.GetCurrentDirectory(), iMODExecutablePath);
            }

            if (File.Exists(iMODExecutablePath))
            {
                if (log != null)
                {
                    log.AddInfo("Starting iMOD...", indentLevel);
                }

                try
                {
                    iMODExecutablePath = Path.GetFullPath(iMODExecutablePath);

                    string command = "\"" + Path.GetFileName(iMODExecutablePath) + " \"" + imfFilename + " \"";
                    int exitCode = CommonUtils.ExecuteCommand(command, -1, out outputString, Path.GetDirectoryName(iMODExecutablePath));
                    if ((log != null) && (outputString.Length > 0))
                    {
                        log.AddInfo(outputString);
                    }
                    else if (exitCode != 0)
                    {
                        log.AddInfo("Could not start iMOD, exitcode: " + exitCode);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while starting iMOD", ex);
                }
            }
            else
            {
                if (log != null)
                {
                    log.AddWarning("Specified iMOD-executable does not exist: " + iMODValidatorSettingsManager.Settings.iMODExecutablePath);
                    log.AddInfo("Current directory is: " + Directory.GetCurrentDirectory(), 1);
                }
            }
        }

        /// <summary>
        /// Starts the specified batch file
        /// </summary>
        /// <param name="batchFilename"></param>
        /// <param name="log"></param>
        /// <param name="indentLevel"></param>
        /// <param name="timeout">Timeout in milliseconds, 0 to wait indefinitely, or negative value for no timeout</param>
        /// <returns>iMOD-exitcode</returns>
        public static int StartBatchFunction(string batchFilename, Log log, int indentLevel = 0, int timeout = 0)
        {
            string outputString;
            string iMODExecutablePath = iMODValidatorSettingsManager.Settings.iMODExecutablePath;
            if (!Path.IsPathRooted(iMODExecutablePath))
            {
                iMODExecutablePath = Path.Combine(Directory.GetCurrentDirectory(), iMODExecutablePath);
            }

            if (File.Exists(iMODValidatorSettingsManager.Settings.iMODExecutablePath))
            {
                if (log != null)
                {
                    log.AddInfo("Executing iMOD batchfunction" + Path.GetFileName(batchFilename) + "...", indentLevel);
                }

                try
                {
                    string command = Path.GetFileName(iMODValidatorSettingsManager.Settings.iMODExecutablePath) + " \"" + batchFilename + "\"";
                    int exitCode = CommonUtils.ExecuteCommand(command, timeout, out outputString, Path.GetDirectoryName(iMODValidatorSettingsManager.Settings.iMODExecutablePath));
                    LogLevel logLevel = LogLevel.Trace;
                    if (exitCode != 0)
                    {
                        logLevel = LogLevel.Info;
                    }
                    if ((log != null) && (outputString.Length > 0))
                    {
                        log.AddMessage(logLevel, outputString);
                    }
                    return exitCode;
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while starting iMOD", ex);
                }
            }
            else
            {
                if (log != null)
                {
                    log.AddWarning("Specified iMOD-executable does not exist: " + iMODValidatorSettingsManager.Settings.iMODExecutablePath);
                }
            }
            return -1;
        }
    }
}
