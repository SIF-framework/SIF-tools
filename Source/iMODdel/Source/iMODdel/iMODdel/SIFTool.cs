// iMODdel is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODdel.
// 
// iMODdel is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODdel is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODdel. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.IPF;

namespace Sweco.SIF.iMODdel
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
            Log log = null;
            try
            {
                // Call SwecoTool Framework to handle license check, write of toolname and version, parsing arguments, writing of logfile and if specified so handling exeptions
                SIFToolSettings settings = new SIFToolSettings(args);
                tool = new SIFTool(settings);
                log = tool.Log;

                exitcode = tool.Run();
            }
            catch (ToolException ex)
            {
                ExceptionHandler.HandleToolException(ex, log);
                exitcode = 1;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, log);
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
            ToolPurpose = "SIF-tool for selective deletion of iMOD-files: IDF, IPF, GEN. The default is to delete empty files with only NoData-cells or no objects to the recycle bin.";
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

            string[] filenames = Directory.GetFiles(settings.InputPath, settings.InputFilter, (settings.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly));
            List<string> selectedFiles = new List<string>();
            if (filenames.Length > 0)
            {
                Log.AddInfo("Starting selective delete " + (settings.IsRecursive ? "(recursively) " : string.Empty)  + "...");

                foreach (string filename in filenames)
                {
                    Log.AddInfo("Processing " + Path.GetFileName(filename) + " ...", 1, false);
                    if (SelectiveDeleteIMODFile(filename, settings, Log))
                    {
                        selectedFiles.Add(filename);
                    }
                }
                Log.AddInfo(string.Empty);

                if (selectedFiles.Count > 0)
                {
                    Log.AddInfo((settings.IsListMode ? "Selected" : "Deleted") + " empty files:");
                    foreach (string filename in selectedFiles)
                    {
                        Log.AddInfo(Path.GetFileName(filename));
                    }
                }
            }
            else
            {
                Log.AddWarning("No files found for filter: " + settings.InputFilter);
            }

            ToolSuccessMessage = "Finished processing, deleted " + selectedFiles.Count + "/" + filenames.Length + " file(s)";

            return exitcode;
        }

        private bool SelectiveDeleteIMODFile(string filename, SIFToolSettings settings, Log log)
        {
            bool isEmptyIMODFile = false;

            string extension = Path.GetExtension(filename);
            if (extension != null)
            {
                switch (extension.ToLower())
                {
                    case ".idf":
                        isEmptyIMODFile = IsEmptyIDFFile(filename, settings);
                        break;
                    case ".ipf":
                        isEmptyIMODFile = IsEmptyIPFFile(filename, settings);
                        break;
                    case ".gen":
                        isEmptyIMODFile = IsEmptyGENFile(filename, settings);
                        break;
                    default:
                        throw new ToolException("File with unknown extension is skipped: " + Path.GetFileName(filename));
                }
            }
            else
            {
                throw new ToolException("File without extension is skipped: " + Path.GetFileName(filename));
            }

            if (isEmptyIMODFile)
            {
                if (settings.IsListMode)
                {
                    log.AddInfo(" empty iMOD-file");
                }
                else
                {
                    // Delete empty IDF-file
                    log.AddInfo(" deleting empty iMOD-file");
                    try
                    {
                        DeleteFile(filename, settings);
                        isEmptyIMODFile = true;
                    }
                    catch (Exception ex)
                    {
                        log.AddError("Could not delete file '" + filename + "'. " + ex.GetBaseException().GetType().Name + ": " + ex.GetBaseException().Message, 2);
                    }
                }
            }
            else
            {
                log.AddInfo(" non-empty iMOD-file");
            }

            return isEmptyIMODFile;
        }

        private void DeleteFile(string filename, SIFToolSettings settings)
        {
            if (settings.IsRecycleBinUsed)
            {
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(filename, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
            }
            else
            {
                File.Delete(filename);
            }

            filename = Path.ChangeExtension(filename, "met");
            if (File.Exists(filename))
            {
                if (settings.IsRecycleBinUsed)
                {
                    Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(filename, Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs, Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
                }
                else
                {
                    File.Delete(filename);
                }
            }
        }

        private bool IsEmptyGENFile(string filename, SIFToolSettings settings)
        {
            GENFile genFile = GENFile.ReadFile(filename);
            return (genFile.Features.Count == 0);
        }

        private bool IsEmptyIPFFile(string filename, SIFToolSettings settings)
        {
            IPFFile ipfFile = IPFFile.ReadFile(filename);
            return (ipfFile.PointCount == 0);
        }

        private bool IsEmptyIDFFile(string filename, SIFToolSettings settings)
        {
            IDFFile idfFile = IDFFile.ReadFile(filename);
            if (settings.IsZeroDeleted)
            {
                float zeroValue = settings.ZeroValue;
                if (!settings.ZeroMargin.Equals(float.NaN) && !settings.ZeroMargin.Equals(0f))
                {
                    float rangeMinValue = settings.ZeroValue - settings.ZeroMargin;
                    float rangeMaxValue = settings.ZeroValue + settings.ZeroMargin;
                    // Set values outside range to value 1 and values inside range to value 0
                    idfFile = idfFile.IsNotBetween(rangeMinValue, rangeMaxValue);
                    zeroValue = 0;
                }

                return (idfFile.RetrieveValueCount(zeroValue, true) == 0);
            }
            else
            {
                return (idfFile.RetrieveValueCount(idfFile.NoDataValue, true) == 0);
            }
        }

    }
}
