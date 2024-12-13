// URLdownload is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of URLdownload.
// 
// URLdownload is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// URLdownload is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with URLdownload. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.URLdownload
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

            Environment.Exit(exitcode);
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected override void DefineToolProperties()
        {
            AddAuthor("Koen van der Hauw");
            ToolPurpose = "SIF-tool for download via URL";
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

            // Create output path if not yet existing
            if (!settings.OutputPath.Equals(string.Empty) && !Directory.Exists(settings.OutputPath))
            {
                Directory.CreateDirectory(settings.OutputPath);
            }

            int fileCount = 0;

            WebClient webClient = new WebClient();
            if (settings.Accountname != null)
            {
                if (settings.Domain != null)
                {
                    webClient.Credentials = new NetworkCredential(settings.Accountname, settings.Password, settings.Domain);
                }
                else
                {
                    webClient.Credentials = new NetworkCredential(settings.Accountname, settings.Password);
                }
            }

            switch (settings.SecurityProtocol)
            {
                case SecurityProtocol.SSL:
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3;
                    break;
                case SecurityProtocol.TLS:
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    break;
                default:
                    // nothing to do
                    break;
            }

            // Start download
            StartDownload(webClient, settings, ref fileCount, Log, 0);

            Log.AddInfo();
            if (fileCount > 1)
            {
                ToolSuccessMessage = "Finished downloading " + fileCount + " files";
            }
            else if (fileCount == 1)
            {
                ToolSuccessMessage = "Finished downloading";
            }
            else if (fileCount == 0)
            {
                ToolSuccessMessage = "No files were downloaded";
            }

            return exitcode;
        }

        protected virtual void StartDownload(WebClient webClient, SIFToolSettings settings, ref int fileCount, Log log, int v)
        {
            string filename = Path.Combine(settings.OutputPath, settings.OutputFilename);
            DownloadFile(webClient, settings.URL, filename, settings, Log, 0);
            PerformPostprocessing(settings, filename, Log, 0);
        }

        protected virtual void PerformPostprocessing(SIFToolSettings settings, string filename, Log log, int logIndentLevel)
        {
            // Currently no postprocessing is done
        }

        protected void DownloadFile(WebClient webClient, string url, string filename, SIFToolSettings settings, Log log, int logIndentLevel = 0)
        {
            try
            {
                FileUtils.EnsureFolderExists(filename);

                Log.AddInfo("starting download for file '" + Path.GetFileName(filename) + "' ... ", logIndentLevel + 1, false);
                webClient.DownloadFile(url, filename);
                Log.AddInfo("download succesful");
            }
            catch (Exception ex)
            {
                log.AddInfo("FAILED!");
                throw ex;
            }

            if (!File.Exists(filename))
            {
                throw new ToolException("File was not downloaded: " + filename);
            }
        }
    }
}
