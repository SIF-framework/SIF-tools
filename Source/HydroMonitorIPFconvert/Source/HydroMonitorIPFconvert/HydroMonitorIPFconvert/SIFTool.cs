// HydroMonitorIPFconvert is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of HydroMonitorIPFconvert.
// 
// HydroMonitorIPFconvert is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HydroMonitorIPFconvert is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HydroMonitorIPFconvert. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.iMOD;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.HydroMonitorIPFconvert
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
            Authors = new string[] { "Koen van der Hauw" };
            ToolPurpose = "SIF-tool for converting HydroMonitor Excel-files to IPF-files";
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

            // Place worker code here
            string outputPath = settings.OutputPath;

            // Create output path if not yet existing
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            HydroMonitorSettings.Initialize();
            if (settings.ExcelSheetId != null)
            {
                HydroMonitorFile.ExcelSheetId = settings.ExcelSheetId;
            }

            // An example for reading files from a path and creating a new file...
            List<string> inputFilenames = new List<string> (Directory.GetFiles(settings.InputPath, settings.InputFilter, settings.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
            inputFilenames = RemoveInvalidFilenames(inputFilenames);

            Log.AddInfo("Processing input files ...");
            int fileCount = 0;
            foreach (string inputFilename in inputFilenames)
            {
                Log.AddInfo("Reading file " + Path.GetFileName(inputFilename) + " ...", 1);
                HydroMonitorFile hydroMonitorFile = null;
                try
                {
                    hydroMonitorFile = HydroMonitorFile.ReadFile(inputFilename, settings.ExcelSheetId, Log);
                }
                catch (Exception ex)
                {
                    Log.AddError(ex.GetBaseException().Message, 2);
                    Log.AddWarning("File has not been read correctly and is skipped: " + Path.GetFileName(inputFilename), 2);
                }

                if (hydroMonitorFile != null)
                {
                    string outputFilename = Path.Combine(settings.OutputPath, Path.GetFileNameWithoutExtension(inputFilename).Trim() + ".IPF");
                    Log.AddInfo("Exporting file " + Path.GetFileName(outputFilename) + " ...", 1);
                    Metadata metadata = new Metadata(outputFilename, null, "Converted HydroMonitor v" + hydroMonitorFile.FormatVersion + " file of type " + hydroMonitorFile.FileType);
                    metadata.Source = inputFilename;
                    metadata.ProcessDescription = "Converted HydroMonitor-file with SIF-tool " + ToolName + ", " + ToolVersion;
                    hydroMonitorFile.Check();
                    if (settings.IsCleaned)
                    {
                        hydroMonitorFile.Clean();
                        hydroMonitorFile.Check();
                    }
                    hydroMonitorFile.Export(outputFilename, settings, metadata);

                    fileCount++;
                }
            }

            ToolSuccessMessage = "Converted " + fileCount + " file(s)";
            if (fileCount < inputFilenames.Count)
            {
                ToolSuccessMessage += "\nSkipped " + (inputFilenames.Count - fileCount) + " file(s)";
            }
            return exitcode;
        }

        private List<string> RemoveInvalidFilenames(List<string> inputFilenames)
        {
            int idx = 0;
            while (idx < inputFilenames.Count)
            {
                string filename = Path.GetFileName(inputFilenames[idx]);
                if (filename.StartsWith("~"))
                {
                    inputFilenames.RemoveAt(idx);
                }
                else
                {
                    idx++;
                }
            }
            return inputFilenames;
        }
    }
}
