// iMODWBalFormat is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODWBalFormat.
// 
// iMODWBalFormat is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODWBalFormat is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODWBalFormat. If not, see <https://www.gnu.org/licenses/>.
using iMODWBalFormat;
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODWBalFormat
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
            AddAuthor("Koen Jansen");
            ToolPurpose = "Tool for formatting CSV-output of iMOD-batchfunction WBALANCE";
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

            // An example for reading files from a path and creating a new file...
            string[] inputFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter);

            IMODWBal wbal = CreateIMODWBalObject(settings);

            Log.AddInfo("Processing input files ...");
            int fileCount = 0;
            foreach (string inputFilename in inputFilenames)
            {
                Log.AddInfo("Reading csv-file " + Path.GetFileName(inputFilename) + " ...", 1);
                wbal.ReadWaterbalanceFile(inputFilename, Log, settings.GENFilename);
                if (wbal != null)
                {
                    string excelFilename = Path.Combine(settings.OutputPath, Path.GetFileNameWithoutExtension(inputFilename) + ".xlsx");
                    try
                    {
                        wbal.Export(excelFilename, settings);
                    }
                    catch (ToolException ex)
                    {
                        throw ex;
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Error: could not export waterbalance file: " + Path.GetFileNameWithoutExtension(inputFilename) + ".xlsx", ex);
                    }
                    Log.AddInfo("Finished formatting waterbalance " + Path.GetFileName(excelFilename));
                }
                else
                {
                    throw new ToolException("Error: could not read waterbalance file: " + Path.GetFileName(inputFilename));
                }


                fileCount++;
            }

            ToolSuccessMessage = "Finished processing " + fileCount + " file(s)";

            return exitcode;
        }

        protected virtual IMODWBal CreateIMODWBalObject(SIFToolSettings settings)
        {
            return new IMODWBal();
        }
    }
}
