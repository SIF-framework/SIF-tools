// IPFsplit is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IPFsplit.
// 
// IPFsplit is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IPFsplit is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IPFsplit. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.iMOD.IPF;

namespace Sweco.SIF.IPFsplit
{
    public class SIFTool : SIFToolBase
    {
        #region Constructor

        /// <summary>
        /// Creates a SIFTool instance and initializes tool name and version and a Log object with the console as a default listener
        /// </summary>
        public SIFTool(SIFToolSettingsBase settings) : base(settings)
        {
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
            AddAuthor("Koen Jansen");
            AddAuthor("Koen van der Hauw");
            ToolPurpose = "Tool for splitting rows from IPF-file(s) into two or more new IPF-file(s)";
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
            if (!Directory.Exists(settings.OutputPath))
            {
                Directory.CreateDirectory(settings.OutputPath);
            }

            Log.AddInfo("Processing input files ...");
            int fileCount = 0;
            string[] inputFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter, settings.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
             
            foreach (string inputFilename in inputFilenames)
            {
                SplitIPFFile(inputFilename, settings);
                fileCount++;
            }

            ToolSuccessMessage = "Finished processing " + fileCount + " file(s)";

            return exitcode;
        }

        /// <summary>
        /// Actually do splitting of specified IPF-file with given settings
        /// </summary>
        /// <param name="inputFilename"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        private void SplitIPFFile(string inputFilename, SIFToolSettings settings)
        {

            if (settings.InputPath == settings.OutputPath && settings.SplitValuePrefix == string.Empty)
            {
                throw new ToolException("Inputfilename is the same as outputfilename and no postfix is defined");
            }

            Log.AddInfo("Processing IPF-file " + Path.GetFileName(inputFilename) + " ...", 1);

            Log.AddInfo("Reading " + Path.GetFileName(inputFilename) + " ...", 2);
            IPFFile sourceIPFFile = null;
            try
            {
                if (!File.Exists(inputFilename))
                {
                    throw new ToolException("IPF-file doesn't exist: " + inputFilename);
                }

                if (!Directory.Exists(settings.OutputPath))
                {
                    Log.AddInfo("Creating target directory: " + settings.OutputPath + " ...", 2);
                    Directory.CreateDirectory(settings.OutputPath);
                }

                Log.AddInfo("Splitting IPF-file " + inputFilename, 2);
                sourceIPFFile = IPFFile.ReadFile(inputFilename);

                int splitColumnIdx = RetrieveColumnIndex(sourceIPFFile, settings, Log);
                if (splitColumnIdx > sourceIPFFile.ColumnCount)
                {
                    throw new ToolException("Invalid splitcolumn number (" + (splitColumnIdx + 1) + "): higher than number of columns in IPF-file (" + sourceIPFFile.ColumnCount + ")");
                }

                List<IPFFile> targetIPFFiles = new List<IPFFile>();
                List<string> splitValues = new List<string>();

                for (int pointIdx = 0; pointIdx < sourceIPFFile.PointCount; pointIdx++)
                {
                    IPFPoint ipfPoint = sourceIPFFile.Points[pointIdx];
                    List<string> columnValues = ipfPoint.ColumnValues;
                    string splitValue = columnValues[splitColumnIdx];
                    IPFFile targetIPFFile = null;
                    if (splitValues.Contains(splitValue))
                    {
                        int targetIPFIdx = splitValues.IndexOf(splitValue);
                        targetIPFFile = targetIPFFiles[targetIPFIdx];
                    }
                    else
                    {
                        splitValues.Add(splitValue);
                        targetIPFFile = new IPFFile();
                        targetIPFFile.ColumnNames = sourceIPFFile.ColumnNames;
                        targetIPFFile.Filename = Path.Combine(settings.OutputPath,
                            Path.GetFileNameWithoutExtension(sourceIPFFile.Filename) + "_"
                            + settings.SplitValuePrefix + splitValue.Trim().Replace(" ", "").Replace("\"", "").Replace(";", "-")
                            + Path.GetExtension(sourceIPFFile.Filename));
                        targetIPFFile.AssociatedFileColIdx = sourceIPFFile.AssociatedFileColIdx;
                        targetIPFFiles.Add(targetIPFFile);
                    }
                    targetIPFFile.AddPoint(new IPFPoint(targetIPFFile, ipfPoint, columnValues));
                }

                targetIPFFiles = iMOD.Utils.IMODUtils.SortAlphanumericIMODFiles<IPFFile>(targetIPFFiles);
                foreach (IPFFile targetIPFFile in targetIPFFiles)
                {
                    string targetIPFFilename = targetIPFFile.Filename;
                    Log.AddInfo("Writing target IPF-file: " + Path.GetFileName(targetIPFFilename) + " ...", 2);
                    targetIPFFile.WriteFile(targetIPFFilename);
                }
            }
            catch (ToolException ex)
            {
                throw new ToolException("Error splitting " + Path.GetFileName(inputFilename), ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error splitting " + Path.GetFileName(inputFilename), ex);
            }
        }

        protected virtual int RetrieveColumnIndex(IPFFile sourceIPFFile, SIFToolSettings settings, Log log, int logIndentLevel = 0)
        {
            if (!int.TryParse(settings.SplitColString, out int splitColumnNr))
            {
                throw new ToolException("Could not parse split column number, specify an integer value: " + settings.SplitColString);
            }
            return splitColumnNr - 1;
        }
    }
}
