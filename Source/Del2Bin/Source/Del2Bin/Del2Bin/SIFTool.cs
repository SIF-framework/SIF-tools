// Del2Bin is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Del2Bin.
// 
// Del2Bin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Del2Bin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Del2Bin. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.IO;
using Sweco.SIF.Common;

namespace Sweco.SIF.Del2Bin
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
            SIFToolSettings settings = null;
            try
            {
                // Use SwecoTool Framework to handle license check, write of toolname and version, parsing arguments, writing of logfile and if specified so handling exeptions
                settings = new SIFToolSettings(args);
                tool = new SIFTool(settings);

                exitcode = tool.Run();
            }
            catch (ToolException ex)
            {
                ExceptionHandler.HandleToolException(ex, tool?.Log);
                exitcode = (settings != null) ? (settings.IsErrorLevelReturned ? 1 : -1) : 1;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, tool?.Log);
                exitcode = (settings != null) ? (settings.IsErrorLevelReturned ? 1 : -1) : 1;
            }

            System.Environment.Exit(exitcode);
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected override void DefineToolProperties()
        {
            Authors = new string[] { "Koen van der Hauw" };
            ToolPurpose = "SIF-tool for deleting files or subdirectories to the recycle bin. Number of deletions is returned.";
        }

        /// <summary>
        /// Starts actual tool process after reading and checking settings
        /// </summary>
        /// <returns>resultcode: 0 for success, 1 for errors</returns>
        protected override int StartProcess()
        {
            // Retrieve tool settings that have been parsed from the command-line arguments 
            SIFToolSettings settings = (SIFToolSettings) Settings;

            Log.AddInfo("Processing input files ...");

            int fileCount = 0;

            // Define logging object
//            Log log = toolInstance.log;
//            log.Filename = null;                                                // Set filename to null to prevent writing a logfile, just write logmessages to console
            // log.Filename = Path.Combine(outputPath, ToolName + ".log");

            string inputPath = settings.InputPath;
            string filterString = settings.InputFilter;

            SearchOption searchOption = (settings.IsRecursive) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            bool isRootPath = inputPath.Substring(1, 1).Equals(":") && (inputPath.Length <= 3);

            if (filterString != null)
            {
                // A filter has been specified, delete files in input path that match filter
                Log.AddInfo("Deleting specified files ...", 0);
                string[] filenames = Directory.GetFiles(settings.InputPath, filterString, searchOption);

                // try to remove files
                for (int idx = 0; idx < filenames.Length; idx++)
                {
                    string filename = filenames[idx];
                    string relFilename = Path.Combine(Path.GetDirectoryName(FileUtils.GetRelativePath(filename, inputPath)), Path.GetFileName(filename));
                    try
                    {
                        // Check for read only files
                        bool isReadOnly = File.GetAttributes(filename).HasFlag(FileAttributes.ReadOnly);
                        if (isReadOnly && !settings.IsReadOnlyDeleted)
                        {
                            System.Console.WriteLine("Skipping readonly file: " + relFilename, 1);
                        }
                        else
                        {
                            Log.AddInfo("Deleting " + (isReadOnly ? "read-only " : string.Empty) + "file '" + relFilename + "' ...", 1);
                            try
                            {
                                DeleteFile(filename, settings);
                                fileCount++;
                            }
                            catch (Exception ex)
                            {
                                Log.AddError("Could not delete file '" + relFilename + "'. " + ex.GetBaseException().GetType().Name + ": " + ex.GetBaseException().Message, 2);

                                Log.AddError("Trying alternative delete ... ", 2);
                                int errorCode = DeleteUtils.DeleteFileOrFolder(filename);
                                if (errorCode != 0)
                                {
                                    throw new ToolException("Could not delete file '" + relFilename + "'. Errorcode of SHFileOperation: " + errorCode);
                                }
                                else
                                {
                                    fileCount++;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new ToolException("Could not delete file '" + relFilename + "': " + ex.GetBaseException().Message);
                    }
                }
            }
            else
            {
                // No filter has been specified, delete directory
                try
                {
                    // bool isReadOnly = Directory.GetAttributes(filename).HasFlag(FileAttributes.ReadOnly);
                    //if (isReadOnly && !settings.isReadOnlyDeleted)
                    //{
                    //    System.Console.WriteLine("Skipping readonly file: " + relFilename, 1);
                    //}
                    Log.AddInfo("Deleting directory '" + inputPath + "' ...", 1);

                    try
                    {
                        DeleteDirectory(inputPath, settings);
                        fileCount++;
                    }
                    catch (Exception ex)
                    {
                        Log.AddError("Could not delete directory '" + inputPath + "': " + ex.Message);
                        Log.AddInfo("Trying alternative delete ... ", 2);
                        int errorCode = DeleteUtils.DeleteFileOrFolder(inputPath);
                        if (errorCode != 0)
                        {
                            throw new ToolException("Could not delete directory '" + inputPath + "'. Errorcode of SHFileOperation: " + errorCode);
                        }
                        fileCount++;
                    }
                }
                catch (Exception ex)
                {
                    Log.AddError("Could not delete directory '" + inputPath + "': " + ex.Message);
                }

            }

            System.Console.WriteLine();
            ToolSuccessMessage = fileCount + " " + ((filterString == null) ? "directorie(s)" : "file(s)") + " deleted";
            int exitcode = settings.IsErrorLevelReturned ? 0 : fileCount;

            return exitcode;

        }

        protected virtual void DeleteDirectory(string inputPath, SIFToolSettings settings)
        {
            Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(inputPath,
                Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
        }

        protected virtual void DeleteFile(string filename, SIFToolSettings settings)
        {
            Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(filename,
                Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin);
        }
    }
}
