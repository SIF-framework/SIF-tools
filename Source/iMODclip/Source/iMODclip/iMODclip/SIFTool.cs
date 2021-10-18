// iMODclip is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODclip.
// 
// iMODclip is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODclip is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODclip. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.IO;
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMOD.ASC;
using Sweco.SIF.iMOD.IDF;

namespace Sweco.SIF.iMODclip
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
        /// List of extensions of files that can currently be clipped by the tool
        /// </summary>
        protected static List<string> clippedExtensions = new List<string>() { ".idf", ".IPF", ".asc" };

        // File counts for summary of statistics when tool is finished
        protected static int skippedExistingFileCount;
        protected static int skippedEmptyFileCount;
        protected static int skippedNoDataFileCount;
        protected static int clippedFileCount;
        protected static int errorFileCount;
        protected static int copiedEmptyFileCount;
        protected static int copiedOtherFileCount;
        protected static int createdNoDataFileCount;

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
                // Use SIF-tool Framework to handle license check, write of toolname and version, parsing arguments, writing of logfile and if specified so handling exeptions
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
            ToolPurpose = "SIF-tool for clipping iMOD-files (IDF/ASC/IPF) in given directory to specified extent";
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
            string outputPath = FileUtils.GetFolderPath(settings.OutputPath);
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            InitializeStatistics();

            ClipFiles(settings.InputPath, settings.OutputPath, settings, Log);

            if ((settings.EmptyFileMethod == 0) || (settings.EmptyFileMethod == 2))
            {
                RemoveEmptySubdirectories(outputPath);
            }

            WriteStatistics();

            return exitcode;
        }

        /// <summary>
        /// Initialize clip statistics
        /// </summary>
        protected virtual void InitializeStatistics()
        {
            skippedExistingFileCount = 0;
            skippedEmptyFileCount = 0;
            skippedNoDataFileCount = 0;
            clippedFileCount = 0;
            errorFileCount = 0;
            copiedEmptyFileCount = 0;
            copiedOtherFileCount = 0;
            createdNoDataFileCount = 0;
        }

        /// <summary>
        /// Add clip statistics to log
        /// </summary>
        protected virtual void WriteStatistics()
        {
            // Write statistics
            Log.AddInfo(string.Empty);
            ToolSuccessMessage = null;
            if (clippedFileCount > 0)
            {
                Log.AddInfo("Clipped " + clippedFileCount + " files");
            }
            if (skippedEmptyFileCount > 0)
            {
                Log.AddInfo("Skipped " + skippedEmptyFileCount + " files outside clip extent");
            }
            if (skippedExistingFileCount > 0)
            {
                Log.AddInfo("Skipped " + skippedExistingFileCount + " existing files (overwrite is off)");
            }
            if (skippedNoDataFileCount > 0)
            {
                Log.AddInfo("Skipped " + skippedNoDataFileCount + " grid files with only NoData values");
            }
            if (createdNoDataFileCount > 0)
            {
                Log.AddInfo("Created " + createdNoDataFileCount + " empty grid-files (source outside clip extent)");
            }
            if (copiedEmptyFileCount > 0)
            {
                Log.AddInfo("Copied " + copiedEmptyFileCount + " empty clipped files (source file is copied)");
            }
            if (copiedOtherFileCount > 0)
            {
                Log.AddInfo("Copied " + copiedOtherFileCount + " other (non IDF, ASC or IPF) files");
            }
            if (errorFileCount > 0)
            {
                Log.AddInfo("Copied " + errorFileCount + " files that gave an error (contents were not clipped)");
            }
        }

        /// <summary>
        /// Start clipping files in specified input path
        /// </summary>
        /// <param name="inputPath"></param>
        /// <param name="outputPath"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        protected void ClipFiles(string inputPath, string outputPath, SIFToolSettings settings, Log log)
        {
            // Dictionary with files to be copied (and not clipped). Dictionary has the form <source, target> <Key, Value> pairs
            Dictionary<string, string> copiedOtherFilenames = new Dictionary<string, string>();
            string currentFilename = null;
            string outputFilename = null;

            // Retrieve input filenames, either use specified filename or find files in specified path
            string[] inputFilenames = null;
            if (File.Exists(inputPath))
            {
                inputFilenames = new string[1];
                inputFilenames[0] = inputPath;
            }
            else
            {
                inputFilenames = Directory.GetFiles(inputPath);
            }

            // Keep track of skipped associated TXT-files for IPF-files
            List<string> skippedIPFTXTFilenames = new List<string>();

            try
            {
                Log.AddInfo("Clipping inputPath " + inputPath);

                for (int i = 0; i < inputFilenames.Length; i++)
                {
                    currentFilename = inputFilenames[i];
                    if (FileUtils.GetFilename(outputPath) != null)
                    {
                        outputFilename = outputPath;
                    }
                    else
                    {
                        outputFilename = Path.Combine(FileUtils.GetFolderPath(outputPath), Path.GetFileName(currentFilename));
                    }
                    if (IsExcluded(Path.GetExtension(currentFilename).ToLower(), settings.ExcludedExtensions))
                    {
                        Log.AddInfo("Skipped file with extension '" + Path.GetExtension(currentFilename) + "': " + currentFilename);
                        continue;
                    }

                    if (settings.IsOverwrite || !(File.Exists(outputFilename)))
                    {
                        try
                        {
                            if (IDFFile.HasIDFExtension(currentFilename))
                            {
                                Log.AddInfo("Processing IDF file " + Path.GetFileName(currentFilename) + "...");
                                ClipIDFFile(currentFilename, outputFilename, settings, log);
                            }
                            else if (ASCFile.HasASCExtension(currentFilename))
                            {
                                ClipASCFile(currentFilename, outputFilename, settings, log);
                            }
                            else if (Path.GetExtension(currentFilename).ToLower().Equals(".ipf"))
                            {
                                ClipIPFFile(currentFilename, outputFilename, settings, skippedIPFTXTFilenames, log);
                            }
                            else
                            {
                                // Copy all other files, except MET-files since these are copied together with the clipped files
                                if (IsCopied(currentFilename, log))
                                {
                                    copiedOtherFilenames.Add(currentFilename, outputFilename);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            // Unexpected error: continue, but give warning in logfile and via statistics
                            log.AddError("Unexpected error while clipping file: " + Path.GetFileName(currentFilename) + ":");
                            Log.AddInfo(ex.GetBaseException().Message, 1);
                            Log.AddInfo("Stacktrace:");
                            Log.AddInfo(ex.StackTrace);
                            log.AddWarning("File will be copied: " + Path.GetFileName(currentFilename));
                            copiedOtherFilenames.Add(currentFilename, outputFilename);
                            errorFileCount++;
                        }
                    }
                    else
                    {
                        Log.AddInfo("Skipped existing outputfile " + currentFilename);
                        skippedExistingFileCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                // Fatal error, throw further up
                throw new Exception("Error while clipping file " + currentFilename, ex);
            }

            // Filter IPF TXT-files that were clipped from other files
            if (copiedOtherFilenames.Count > 0)
            {
                Log.AddInfo("Copying other (non IDF, IPF or ASC) files ...");
                foreach (string copiedFilename in copiedOtherFilenames.Keys)
                {
                    if (skippedIPFTXTFilenames.Contains(copiedFilename.ToLower()))
                    {
                        Log.AddInfo("Skipping TXT-file outside extent: " + copiedFilename, 1);
                        skippedEmptyFileCount++;
                        skippedIPFTXTFilenames.Remove(copiedFilename);
                    }
                    else
                    {
                        Log.AddInfo("Copying file: " + copiedFilename, 1);
                        string targetFilename = copiedOtherFilenames[copiedFilename];
                        File.Copy(copiedFilename, targetFilename, true);
                        copiedOtherFileCount++;
                    }
                }
            }

            // Clip files in subdirectories recursively is specified
            if (settings.IsRecursive)
            {
                if (File.Exists(inputPath))
                {
                    // If inputPath is a single file recursion is not possible
                    Log.AddInfo("Recursive clip option is ignored since the specified inputPath is a single file");
                }
                else
                {
                    string[] inputSubdirs = Directory.GetDirectories(inputPath);
                    string currentSubdir = null;
                    string outputSubdir = null;

                    for (int i = 0; i < inputSubdirs.Length; i++)
                    {
                        currentSubdir = inputSubdirs[i];
                        outputSubdir = Path.Combine(outputPath, Path.GetFileName(currentSubdir));
                        if (!(Directory.Exists(outputSubdir)))
                        {
                            Directory.CreateDirectory(outputSubdir);
                        }
                        ClipFiles(currentSubdir, outputSubdir, settings, log);
                        if ((settings.EmptyFileMethod == 0) || (settings.EmptyFileMethod == 2))
                        {
                            if (Directory.Exists(outputSubdir))
                            {
                                if ((Directory.GetFiles(outputSubdir).Length == 0) && (Directory.GetDirectories(outputSubdir).Length == 0))
                                {
                                    Log.AddInfo("Empty folder removed: " + currentSubdir);
                                    Log.AddInfo(string.Empty);
                                    Directory.Delete(outputSubdir);
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Clip IPF-file with specified filename and specified settings
        /// </summary>
        /// <param name="currentFilename"></param>
        /// <param name="outputFilename"></param>
        /// <param name="settings"></param>
        /// <param name="skippedIPFTXTFilenames"></param>
        /// <param name="log"></param>
        protected virtual void ClipIPFFile(string currentFilename, string outputFilename, SIFToolSettings settings, List<string> skippedIPFTXTFilenames, Log log)
        {
            // Log.AddInfo("Processing IPF file " + Path.GetFileName(currentFilename) + "...");
            IPFFile currentIPFFile = IPFFile.ReadFile(currentFilename, false, true);

            // Actuallly clip IPF-file
            IPFFile outputIPFFile = ClipIPFFile(currentIPFFile, settings, skippedIPFTXTFilenames);

            // Check if clipped file has any cells, and extent had overlap with extent of input file
            if ((outputIPFFile == null) || (outputIPFFile.PointCount == 0))
            {
                // Handle files without points with specified method
                switch (settings.EmptyFileMethod)
                {
                    case 0:
                        // 0: skip empty folder; write empty (clipped) iMOD-file (ASC/IDF: NoData within clipextent)
                    case 1:
                        // 1: as 0, but write empty folder(s)
                        outputIPFFile.WriteFile(settings.IsKeepFileDateTime);
                        CopyMetadataFile(currentFilename, Path.GetDirectoryName(outputFilename), settings.IsOverwrite);
                        clippedFileCount++;
                        break;
                    case 2:
                        // 2: skip empty folders/iMOD-files
                        Log.AddInfo("Result file is empty. File is skipped.", 1);
                        skippedEmptyFileCount++;
                        break;
                    case 3:
                        // 3: copy complete source file when extent is completey outside clipextent
                        Log.AddInfo("Input file has no overlap with clipboundary. File will be copied.", 1);
                        File.Copy(currentFilename, outputFilename, true);
                        CopyMetadataFile(currentFilename, Path.GetDirectoryName(outputFilename), settings.IsOverwrite);
                        copiedEmptyFileCount++;
                        break;
                    default:
                        throw new Exception("Unexpected emptyFileMethod: " + settings.EmptyFileMethod);
                }
            }
            else
            {
                // Handle files with one or more points
                WriteIPFFile(outputIPFFile, outputFilename, settings.IsKeepFileDateTime);
                CopyMetadataFile(currentFilename, Path.GetDirectoryName(outputFilename), settings.IsOverwrite);
                clippedFileCount++;
            }

            outputIPFFile = null;
            GC.Collect();
        }

        /// <summary>
        /// Clip ASC-file with specified filename and specified settings
        /// </summary>
        /// <param name="currentFilename"></param>
        /// <param name="outputFilename"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        protected virtual void ClipASCFile(string currentFilename, string outputFilename, SIFToolSettings settings, Log log)
        {
            // Log.AddInfo("Processing ASC file " + Path.GetFileName(currentFilename) + "...");
            ASCFile currentASCFile = ASCFile.ReadFile(currentFilename, EnglishCultureInfo);

            // Actuallly clip ASC-file
            ASCFile outputASCFile = ClipASCFile(currentASCFile, settings);
            outputASCFile.Filename = outputFilename;

            // Check if clipped file has any cells, and extent had overlap with extent of input file
            if ((outputASCFile == null) || (outputASCFile.NCols == 0) || (outputASCFile.NRows == 0))
            {
                // Handle files without cells with specified method
                switch (settings.EmptyFileMethod)
                {
                    case 0:
                        // 0: skip empty folder; write empty (clipped) iMOD-file (ASC/IDF: NoData within clipextent)
                    case 1:
                        // 1: as 0, but write empty folder(s)
                        if (settings.IsNoDataGridSkipped)
                        {
                            Log.AddInfo("Skipped" + Path.GetFileName(currentFilename) + " with only NoData values");
                            skippedNoDataFileCount++;
                        }
                        else
                        {
                            Log.AddInfo("Input file has no overlap with clipboundary. NoData-file is written.", 1);
                            IDFFile emptyIDFFile = new IDFFile(outputFilename, settings.Extent, currentASCFile.Cellsize, currentASCFile.NoDataValue);
                            emptyIDFFile.ResetValues();
                            outputASCFile = new ASCFile(emptyIDFFile);
                            outputASCFile.WriteFile(EnglishCultureInfo, settings.IsKeepFileDateTime);
                            CopyMetadataFile(currentFilename, Path.GetDirectoryName(outputFilename), settings.IsOverwrite);
                            createdNoDataFileCount++;
                        }
                        break;
                    case 2:
                        // 2: skip empty folders/iMOD-files
                        Log.AddInfo("Input file has no overlap with clipboundary. File is skipped.", 1);
                        skippedEmptyFileCount++;
                        break;
                    case 3:
                        // 3: copy complete source file when extent is completey outside clipextent
                        Log.AddInfo("Input file has no overlap with clipboundary. File will be copied.", 1);
                        File.Copy(currentFilename, outputFilename, true);
                        CopyMetadataFile(currentFilename, Path.GetDirectoryName(outputFilename), settings.IsOverwrite);
                        copiedEmptyFileCount++;
                        break;
                    default:
                        throw new Exception("Unexpected emptyFileMethod: " + settings.EmptyFileMethod);
                }
            }
            else
            {
                // Handle files with one or more cells
                if (!settings.IsNoDataGridSkipped || outputASCFile.HasDataValues())
                {
                    outputASCFile.WriteFile(EnglishCultureInfo, settings.IsKeepFileDateTime);
                    CopyMetadataFile(currentFilename, Path.GetDirectoryName(outputFilename), settings.IsOverwrite);
                    clippedFileCount++;
                }
                else
                {
                    Log.AddInfo("Skipped" + Path.GetFileName(outputASCFile.Filename) + " with only NoData values");
                    skippedNoDataFileCount++;
                }
            }

            outputASCFile = null;
            GC.Collect();
        }

        /// <summary>
        /// Clip IDF-file with specified filename and specified settings
        /// </summary>
        /// <param name="currentFilename"></param>
        /// <param name="outputFilename"></param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        protected virtual void ClipIDFFile(string currentFilename, string outputFilename, SIFToolSettings settings, Log log)
        {
            // Log.AddInfo("Processing IDF file " + Path.GetFileName(currentFilename) + "...");
            #if DEBUG 
              var watch = System.Diagnostics.Stopwatch.StartNew();
            #endif
            IDFFile currentIDFFile = IDFFile.ReadFile(currentFilename);
            #if DEBUG
                watch.Stop();
                Log.AddInfo("ReadIDFFile call: " + watch.ElapsedMilliseconds + " milliseconds", 1);
            #endif

            // Actuallly clip IDF-file
            #if DEBUG
                watch = System.Diagnostics.Stopwatch.StartNew();
            #endif
            IDFFile outputIDFFile = ClipIDFFile(currentIDFFile, settings);
            #if DEBUG
                watch.Stop();
                Log.AddInfo("ClipIDF call: " + watch.ElapsedMilliseconds + " milliseconds", 1);
            #endif
            
            // Check if clipped file has any cells, and extent had overlap with extent of input file
            if ((outputIDFFile == null) || (outputIDFFile.NCols == 0) || (outputIDFFile.NRows == 0))
            {
                // Handle files without cells with specified method
                switch (settings.EmptyFileMethod)
                {
                    case 0:
                        // 0: skip empty folder; write empty (clipped) iMOD-file (ASC/IDF: NoData within clipextent)
                    case 1:
                        // 1: as 0, but write empty folder(s)
                        if (settings.IsNoDataGridSkipped)
                        {
                            Log.AddInfo("Skipped" + Path.GetFileName(currentFilename) + " with only NoData values");
                            skippedNoDataFileCount++;
                        }
                        else
                        {
                            Log.AddInfo("Input file has no overlap with clipboundary. NoData-file is written.", 1);
                            outputIDFFile = new IDFFile(outputFilename, settings.Extent, currentIDFFile.XCellsize, currentIDFFile.YCellsize, currentIDFFile.NoDataValue);
                            outputIDFFile.ResetValues();
                            outputIDFFile.WriteFile();
                            CopyMetadataFile(currentFilename, Path.GetDirectoryName(outputFilename), settings.IsOverwrite);
                            createdNoDataFileCount++;
                        }
                        break;
                    case 2:
                        // 2: skip empty folders/iMOD-files
                        Log.AddInfo("Input file has no overlap with clipboundary. File is skipped.", 1);
                        skippedEmptyFileCount++;
                        break;
                    case 3:
                        // 3: copy complete source file when extent is completey outside clipextent
                        Log.AddInfo("Input file has no overlap with clipboundary. File will be copied.", 1);
                        File.Copy(currentFilename, outputFilename, true);
                        CopyMetadataFile(currentFilename, Path.GetDirectoryName(outputFilename), settings.IsOverwrite);
                        copiedEmptyFileCount++;
                        break;
                    default:
                        throw new Exception("Unexpected emptyFileMethod: " + settings.EmptyFileMethod);
                }
            }
            else
            {
                // Handle files with one or more cells
                outputIDFFile.Filename = outputFilename;
                if (!settings.IsNoDataGridSkipped || outputIDFFile.HasDataValues())
                {
                    #if DEBUG
                        watch = System.Diagnostics.Stopwatch.StartNew();
                    #endif
                    WriteIDFFile(outputIDFFile, outputFilename, settings.IsKeepFileDateTime);
                    #if DEBUG
                        watch.Stop();
                        Log.AddInfo("WriteIDFFile call: " + watch.ElapsedMilliseconds + " milliseconds", 1);
                    #endif
                        
                    CopyMetadataFile(currentFilename, Path.GetDirectoryName(outputFilename), settings.IsOverwrite);
                    clippedFileCount++;
                }
                else
                {
                    Log.AddInfo("Skipped" + Path.GetFileName(outputIDFFile.Filename) + " with only NoData values");
                }
            }

            outputIDFFile = null;
            GC.Collect();
        }

        /// <summary>
        /// Clip specified IPF-file with specified settings
        /// </summary>
        /// <param name="currentIPFFile"></param>
        /// <param name="settings"></param>
        /// <param name="skippedIPFTXTFilenames"></param>
        /// <returns></returns>
        protected virtual IPFFile ClipIPFFile(IPFFile currentIPFFile, SIFToolSettings settings, List<string> skippedIPFTXTFilenames)
        {
            return currentIPFFile.ClipIPF(settings.Extent, false, skippedIPFTXTFilenames);
        }

        /// <summary>
        /// Clip specified ASC-file with specified settings
        /// </summary>
        /// <param name="currentASCFile"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual ASCFile ClipASCFile(ASCFile currentASCFile, SIFToolSettings settings)
        {
            return currentASCFile.Clip(settings.Extent);
        }

        /// <summary>
        /// Clip specified IDF-file with specified settings
        /// </summary>
        /// <param name="currentIDFFile"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual IDFFile ClipIDFFile(IDFFile currentIDFFile, SIFToolSettings settings)
        {
            return currentIDFFile.ClipIDF(settings.Extent);
        }

        /// <summary>
        /// Check if files with specified extension should be skipped
        /// </summary>
        /// <param name="extension"></param>
        /// <param name="excludedExtensions"></param>
        /// <returns></returns>
        protected bool IsExcluded(string extension, List<string> excludedExtensions)
        {
            extension = extension.ToLower().Replace(".", string.Empty);
            foreach (string excludedExtension in excludedExtensions)
            {
                if (extension.Equals(excludedExtension.ToLower().Replace(".", string.Empty)))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if file with specified filename should be copied
        /// </summary>
        /// <param name="currentFilename"></param>
        /// <returns></returns>
        protected bool IsCopied(string currentFilename, Log log)
        {
            bool isCopied = !clippedExtensions.Contains(Path.GetExtension(currentFilename).ToLower());

            if (Path.GetExtension(currentFilename).ToLower().Equals(".met"))
            {
                // For MET-files, check if a corresponding IDF, ASC or IPF-file is present. In that case the MET-file is handled during the clip
                string[] filenames = Directory.GetFiles(Path.GetDirectoryName(currentFilename), Path.GetFileNameWithoutExtension(currentFilename) + ".*");
                if (filenames.Length > 1)
                {
                    for (int fileIdx = 0; fileIdx < filenames.Length; fileIdx++)
                    {
                        if (clippedExtensions.Contains(Path.GetExtension(filenames[fileIdx]).ToLower()))
                        {
                            isCopied = false;
                        }
                    }
                }
                else
                {
                    isCopied = false;

                    if (log != null)
                    {
                        Log.AddInfo("Skipped isolated MET-file: " + currentFilename);
                    }

                }
            }

            return isCopied;
        }

        /// <summary>
        /// Copy metadata for specified filename if existing
        /// </summary>
        /// <param name="currentFilename"></param>
        /// <param name="outputPath"></param>
        /// <param name="isOverwrite"></param>
        protected void CopyMetadataFile(string currentFilename, string outputPath, bool isOverwrite = false)
        {
            string metFilename = Path.ChangeExtension(currentFilename, "MET");
            if (File.Exists(metFilename))
            {
                File.Copy(metFilename, Path.Combine(outputPath, Path.GetFileName(metFilename)), isOverwrite);
            }
        }

        /// <summary>
        /// Write specified IDF-file and optionally keep existing date and time of source IDF-file
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="newFilename"></param>
        /// <param name="copyLastWriteTime"></param>
        protected void WriteIDFFile(IDFFile idfFile, string newFilename, bool copyLastWriteTime = false)
        {
            if (copyLastWriteTime)
            {
                if (File.Exists(idfFile.Filename))
                {
                    DateTime lastWriteTime = File.GetLastWriteTime(idfFile.Filename);
                    idfFile.WriteFile(newFilename);
                    File.SetLastWriteTime(newFilename, lastWriteTime);
                }
                else
                {
                    idfFile.WriteFile(newFilename);
                }
            }
            else
            {
                idfFile.WriteFile(newFilename);
            }
        }

        /// <summary>
        /// Remove all empty subdirectories recursively from specified basepath
        /// </summary>
        /// <param name="basePath"></param>
        /// <param name="log"></param>
        public void RemoveEmptySubdirectories(string basePath, Log log = null)
        {
            if (Directory.Exists(basePath))
            {
                string[] subdirectories = Directory.GetDirectories(basePath);
                if (subdirectories != null)
                {
                    foreach (string subdirectoryPath in subdirectories)
                    {
                        RemoveEmptySubdirectories(subdirectoryPath, log);

                        if ((Directory.GetFiles(subdirectoryPath).Length == 0) && (Directory.GetDirectories(subdirectoryPath).Length == 0))
                        {
                            if (log != null)
                            {
                                log.AddInfo("Empty folder removed: " + subdirectoryPath);
                            }
                            Directory.Delete(subdirectoryPath, false);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Write specified IPF-file and optionally keep existing date and time of source IDF-file
        /// </summary>
        /// <param name="ipfFile"></param>
        /// <param name="newFilename"></param>
        /// <param name="copyLastWriteTime"></param>
        public void WriteIPFFile(IPFFile ipfFile, string newFilename, bool copyLastWriteTime = false)
        {
            if (copyLastWriteTime)
            {
                if (File.Exists(ipfFile.Filename))
                {
                    DateTime lastWriteTime = File.GetLastWriteTime(ipfFile.Filename);
                    ipfFile.WriteFile(newFilename, null, true);
                    File.SetLastWriteTime(newFilename, lastWriteTime);
                }
                else
                {
                    ipfFile.WriteFile(newFilename, null, true);
                }
            }
            else
            {
                ipfFile.WriteFile(newFilename, null, true);
            }
        }
    }
}
