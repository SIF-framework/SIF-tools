// WorkflowViz is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of WorkflowViz.
// 
// WorkflowViz is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// WorkflowViz is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with WorkflowViz. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.WorkflowViz.Status;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.WorkflowViz.Workflows
{
    /// <summary>
    /// Class for representation of handling SIF-workflow batchfiles
    /// </summary>
    public class Batchfile
    {
        public const string RunscriptsName = "Runscripts";
        public const string SettingsName = "Settings";

        protected static int bfCount = 0;
        public static string LogErrorSubString = Properties.Settings.Default.LogErrorString;

        public string ID { get; set; }
        public string Name { get; set; }
        public string Filename { get; set; }
        public DateTime LastWriteTime { get; set; }
        public Logfile Logfile { get; set; }
        public RunStatus RunStatus { get; set; }

        public Batchfile(string filename)
        {
            this.ID = "BN" + ++bfCount;
            this.Filename = filename;
            this.Name = Path.GetFileNameWithoutExtension(filename);
            this.Logfile = null;
            this.RunStatus = RunStatus.Undefined;

            if (!File.Exists(filename))
            {
                throw new FileNotFoundException("Batchfile not found", filename);
            }

            LastWriteTime = File.GetLastWriteTime(filename);

            string logFilename = Path.ChangeExtension(filename, "log");
            if (File.Exists(logFilename))
            {
                Logfile = new Logfile(logFilename);
            }

            CheckRunStatus();
        }

        /// <summary>
        /// Check status of batchfiles based on existance and date of and errormessages in corresponding logfile
        /// </summary>
        protected virtual void CheckRunStatus()
        {
            if (HasLogfile())
            {
                // Check that datetime of logfile is after datetime of batchfile
                if (Logfile.LastWriteTime > LastWriteTime)
                {
                    // Parse file for errors/success
                    RunStatus = RetrieveRunStatus(Logfile);

                    if ((RunStatus == RunStatus.Completed) && HasINIfile())
                    {
                        // Check that INI-file also has datetime before datetime of logfile
                        DateTime iniLastWriteTime = File.GetLastWriteTime(Path.ChangeExtension(Filename, "INI"));
                        if (iniLastWriteTime > Logfile.LastWriteTime)
                        {
                            RunStatus = RunStatus.Outdated;
                        }
                    }
                }
                else
                {
                    RunStatus = RunStatus.Outdated;
                }
            }
            else if (Utils.IsSettingsName(Path.GetFileNameWithoutExtension(Filename)))
            {
                RunStatus = RunStatus.Ignored;
            }
            else
            {
                RunStatus = RunStatus.None;
            }
        }

        /// <summary>
        /// Retrieve status based on specified logifle
        /// </summary>
        /// <param name="logfile"></param>
        /// <returns></returns>
        protected virtual RunStatus RetrieveRunStatus(Logfile logfile)
        {
            string logString = null;
            try
            {
                logString = FileUtils.ReadFile(logfile.Filename);
                if (logString.Contains(LogErrorSubString))
                {
                    return RunStatus.Error;
                }
                else
                {
                    return RunStatus.Completed;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not read logfile: " + logfile.Filename, ex);
            }


        }

        /// <summary>
        /// Check if this batchfile object has a corresponding logfile (registered)
        /// </summary>
        /// <returns></returns>
        public bool HasLogfile()
        {
            return Logfile != null;
        }

        /// <summary>
        /// Check if batchfile has a corresponding INI-file, i.e. for IDFexp
        /// </summary>
        /// <returns></returns>
        public bool HasINIfile()
        {
            return File.Exists(Path.ChangeExtension(Filename, "ini"));
        }
    }
}
