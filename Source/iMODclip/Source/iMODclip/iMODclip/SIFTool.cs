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
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.GIS;
using System.Security.Principal;
using System.Security.AccessControl;
using System.Security.Permissions;

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
        protected static List<string> clippedExtensions = new List<string>() { ".idf", ".ipf", ".asc", ".gen" };

        // File counts for summary of statistics when tool is finished
        protected static int inputFileCount;
        protected static int clippedFileCount;
        protected static int skippedExistingFileCount;
        protected static int skippedEmptyFileCount;
        protected static int skippedNoDataFileCount;
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
            ToolPurpose = "SIF-tool for clipping iMOD-files (IDF/ASC/IPF/GEN) in given directory to specified extent";
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
            inputFileCount = 0;
            clippedFileCount = 0;
            skippedExistingFileCount = 0;
            skippedEmptyFileCount = 0;
            skippedNoDataFileCount = 0;
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

            Log.AddInfo("Found " + inputFileCount + " source files");
            Log.AddInfo("Clipped " + clippedFileCount + " files");

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
                Log.AddInfo("Copied " + copiedOtherFileCount + " other files (non IDF, IPF, GEN or ASC)");
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
                if (!ContainsSubstring(inputPath, settings.SkippedClipSubstrings) && !HasExtension(inputPath, settings.SkippedExtensions))
                {
                    inputFilenames = new string[1];
                    inputFilenames[0] = inputPath;
                }
            }
            else
            {
                inputFilenames = Directory.GetFiles(inputPath);
                Log.AddInfo("Clipping input path '" + (inputPath.Equals(settings.InputPath) ? inputPath : inputPath.Replace(FileUtils.EnsureTrailingSlash(settings.InputPath), string.Empty)) + "' ...");
            }
            inputFileCount += inputFilenames.Length;

            // Keep track of skipped associated TXT-files for IPF-files
            List<string> skippedIPFTXTFilenames = new List<string>();

            try
            {
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
                    if (HasExtension(currentFilename, settings.SkippedExtensions))
                    {
                        Log.AddInfo("Skipping file with extension '" + Path.GetExtension(currentFilename) + "': " + FileUtils.GetRelativePath(currentFilename, settings.InputPath));
                        continue;
                    }
                    if (ContainsSubstring(currentFilename, settings.SkippedClipSubstrings, false, out string foundClipSubstring))
                    {
                        if (ContainsSubstring(currentFilename, settings.SkippedCopySubstrings, false, out string foundCopySubstring))
                        {
#if DEBUG
                            if (!foundClipSubstring.Equals(foundCopySubstring))
                            {
                                    Log.AddInfo("Skipping file with substrings '" + foundClipSubstring + " and " + foundCopySubstring + "': " + FileUtils.GetRelativePath(currentFilename, settings.InputPath));
                            }
                            else
                            {
                                Log.AddInfo("Skipping file with substring '" + foundClipSubstring + "': " + FileUtils.GetRelativePath(currentFilename, settings.InputPath));
                            }
#endif
                            continue;
                        }
                        else
                        {
                            Log.AddInfo("Copying file with substring '" + foundClipSubstring + "': " + FileUtils.GetRelativePath(currentFilename, settings.InputPath));
                            copiedOtherFilenames.Add(currentFilename, outputFilename);
                            continue;
                        }
                    }

                    if (settings.IsOverwrite || !(File.Exists(outputFilename)))
                    {
                        if (!File.Exists(outputFilename) || FileUtils.HasWriteAccess(outputFilename))
                        {
                            try
                            {
                                if (IDFFile.HasIDFExtension(currentFilename))
                                {
                                    Log.AddInfo("Processing IDF-file '" + FileUtils.GetRelativePath(currentFilename, settings.InputPath) + "' ...");
                                    ClipIDFFile(currentFilename, outputFilename, settings, log);
                                }
                                else if (ASCFile.HasASCExtension(currentFilename))
                                {
                                    Log.AddInfo("Processing ASC-file '" + FileUtils.GetRelativePath(currentFilename, settings.InputPath) + "' ...");
                                    ClipASCFile(currentFilename, outputFilename, settings, log);
                                }
                                else if (Path.GetExtension(currentFilename).ToLower().Equals(".ipf"))
                                {
                                    Log.AddInfo("Processing IPF-file '" + FileUtils.GetRelativePath(currentFilename, settings.InputPath) + "' ...");
                                    ClipIPFFile(currentFilename, outputFilename, settings, skippedIPFTXTFilenames, log);
                                }
                                else if (Path.GetExtension(currentFilename).ToLower().Equals(".gen"))
                                {
                                    Log.AddInfo("Processing GEN-file '" + FileUtils.GetRelativePath(currentFilename, settings.InputPath) + "' ...");
                                    ClipGENFile(currentFilename, outputFilename, settings, log);
                                }
                                //else if (Path.GetExtension(currentFilename).ToLower().Equals(".dat"))
                                //{
                                //    // ignore, assume DAT-file is part of some GEN-file
                                //}
                                else
                                {
                                    if (ContainsSubstring(currentFilename, settings.SkippedCopySubstrings, false, out string foundCopySubstring))
                                    {
#if DEBUG
                                        Log.AddInfo("Skipping file with substring '" + foundCopySubstring + "': " + FileUtils.GetRelativePath(currentFilename, settings.InputPath));
#endif
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
                            }
                            catch (Exception ex)
                            {
                                if (settings.IsContinueOnErrors)
                                {
                                    // Unexpected error: continue, but give warning in logfile and via statistics
                                    log.AddError("Unexpected error while clipping file: " + Path.GetFileName(currentFilename) + ":");
                                    Log.AddInfo(ExceptionHandler.GetExceptionChainString(ex), 1);
                                    if (!(ex is ToolException))
                                    {
                                        Log.AddInfo("Stacktrace:");
                                        Log.AddInfo(ex.StackTrace);
                                    }
                                    log.AddWarning("File will be copied: " + Path.GetFileName(currentFilename));
                                    copiedOtherFilenames.Add(currentFilename, outputFilename);
                                    if (Path.GetExtension(currentFilename).ToLower().Equals(".gen"))
                                    {
                                        // For GEN-files, also copy DAT-file
                                        if (File.Exists(Path.ChangeExtension(currentFilename, ".DAT")))
                                        {
                                            copiedOtherFilenames.Add(Path.ChangeExtension(currentFilename, "DAT"), Path.ChangeExtension(outputFilename, ".DAT"));
                                        }
                                    }
                                    errorFileCount++;
                                }
                                else
                                {
                                    throw new Exception("Unexpected error while clipping file: " + Path.GetFileName(currentFilename) + ":" + ex.GetBaseException().Message, ex);
                                }
                            }
                        }
                        else
                        {
                            log.AddError("Skipping existing readonly output file, reset file access to allow overwrite: " + FileUtils.GetRelativePath(currentFilename, settings.InputPath));
                            skippedExistingFileCount++;
                        }
                    }
                    else
                    {
                        Log.AddInfo("Skipped because of existing outputfile: " + Path.GetFileName(currentFilename));
                        skippedExistingFileCount++;
                    }
                }
            }
            catch (Exception ex)
            {
                // Fatal error, throw further up
                throw new Exception("Error while clipping file '" + currentFilename + "'", ex);
            }

            // Filter IPF TXT-files that were clipped from other files
            if (copiedOtherFilenames.Count > 0)
            {
                Log.AddInfo("Copying other files (non IDF, IPF, GEN or ASC) ...");
                foreach (string copiedFilename in copiedOtherFilenames.Keys)
                {
                    if (ContainsSubstring(currentFilename, settings.SkippedCopySubstrings, false, out string foundCopySubstring))
                    {
#if DEBUG
                        Log.AddInfo("Skipping file with substring '" + foundCopySubstring + "': " + FileUtils.GetRelativePath(currentFilename, settings.InputPath) + "...");
#endif
                    }
                    else if (skippedIPFTXTFilenames.Contains(copiedFilename.ToLower()))
                    {
                        Log.AddInfo("Skipping TXT-file outside extent: " + FileUtils.GetRelativePath(copiedFilename, settings.InputPath), 1);
                        skippedEmptyFileCount++;
                        skippedIPFTXTFilenames.Remove(copiedFilename);
                    }
                    else
                    {
                        Log.AddInfo("Copying file: " + FileUtils.GetRelativePath(copiedFilename, settings.InputPath), 1);
                        string targetFilename = copiedOtherFilenames[copiedFilename];
                        if (!File.Exists(targetFilename) || FileUtils.HasWriteAccess(targetFilename))
                        {
                            File.Copy(copiedFilename, targetFilename, settings.IsOverwrite);
                            copiedOtherFileCount++;
                        }
                        else
                        {
                            log.AddError("Skipping ReadOnly file, reset File access to allow overwrite: " + FileUtils.GetRelativePath(targetFilename, settings.InputPath), 1);
                            skippedExistingFileCount++;
                        }
                    }
                }
            }

            // Clip files in subdirectories recursively is specified
            if (settings.IsRecursive)
            {
                if (File.Exists(inputPath))
                {
                    // If inputPath is a single file recursion is not possible
                    Log.AddInfo("\nRecursive clip option is ignored since the specified inputPath is a single file");
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
                                    Log.AddInfo("Empty folder removed: " + (currentSubdir.Equals(settings.InputPath) ? currentSubdir : currentSubdir.Replace(FileUtils.EnsureTrailingSlash(settings.InputPath), string.Empty)));
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
        /// Check if specified filename has an extension that is equal to any of the specified extensions (with or without an initial dot)
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="extensions"></param>
        /// <returns></returns>
        public static bool HasExtension(string filename, List<string> extensions)
        {
            string fileExtension = Path.GetExtension(filename).ToLower().Replace(".", string.Empty);
            foreach (string extension in extensions)
            {
                if (fileExtension.Equals(extension.ToLower().Replace(".", string.Empty)))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Checks if specified string contains any of the specified substrings
        /// </summary>
        /// <param name="someString"></param>
        /// <param name="substrings"></param>
        /// <param name="isCaseSensitive"></param>
        /// <returns></returns>
        public static bool ContainsSubstring(string someString, List<string> substrings, bool isCaseSensitive = false)
        {
            return ContainsSubstring(someString, substrings, isCaseSensitive, out string foundSubstring);
        }

        /// <summary>
        /// Checks if specified string contains any of the specified substrings
        /// </summary>
        /// <param name="someString"></param>
        /// <param name="substrings"></param>
        /// <param name="isCaseSensitive"></param>
        /// <param name="foundSubstring"< 
        /// <returns></returns>
        public static bool ContainsSubstring(string someString, List<string> substrings, bool isCaseSensitive, out string foundSubstring)
        {
            if (isCaseSensitive)
            {
                foreach (string subString in substrings)
                {
                    if (someString.Contains(subString))
                    {
                        foundSubstring = subString;
                        return true;
                    }
                }
            }
            else
            {
                string lowerCaseString = someString.ToLower();
                foreach (string subString in substrings)
                {
                    if (lowerCaseString.Contains(subString.ToLower()))
                    {
                        foundSubstring = subString;
                        return true;
                    }
                }
            }

            foundSubstring = null;
            return false;
        }

        protected virtual void ClipGENFile(string currentFilename, string outputFilename, SIFToolSettings settings, Log log)
        {
            GENFile genFile = GENFile.ReadFile(currentFilename);

            GENFile clippedGENFile = ClipGENFile(genFile, settings);

            // Check if clipped file has any points, and extent had overlap with extent of input file
            if ((clippedGENFile == null) || (clippedGENFile.Features.Count == 0))
            {
                // Handle files without points with specified method
                switch (settings.EmptyFileMethod)
                {
                    case 0:
                        // 0: skip empty folder; write empty (clipped) iMOD-file (no features within clipextent). Note: process like option 1, empty folder is removed later.
                    case 1:
                        // 1: as 0, but write empty folder(s)
                        WriteGENFile(clippedGENFile, outputFilename, settings.IsKeepFileDateTime, genFile.HasDATFile());
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
                WriteGENFile(clippedGENFile, outputFilename, settings.IsKeepFileDateTime, (genFile.DATFile != null));
                CopyMetadataFile(currentFilename, Path.GetDirectoryName(outputFilename), settings.IsOverwrite);
                clippedFileCount++;
            }

            clippedGENFile = null;
            GC.Collect();
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
            IPFFile currentIPFFile = IPFFile.ReadFile(currentFilename, false);

            // Actuallly clip IPF-file  

            IPFFile outputIPFFile = null;
            try
            {
                outputIPFFile = ClipIPFFile(currentIPFFile, settings, skippedIPFTXTFilenames);
                // outputIPFFile.Filename = outputFilename;
            }
            catch (Exception ex)
            {
                throw new ToolException("dd", ex);
            }

            // Check if clipped file has any points, and extent had overlap with extent of input file
            if ((outputIPFFile == null) || (outputIPFFile.PointCount == 0))
            {
                // Handle files without points with specified method
                switch (settings.EmptyFileMethod)
                {
                    case 0:
                        // 0: skip empty folder; write empty (clipped) iMOD-file (no points within clipextent). Note: process like option 1, empty folder is removed later.
                    case 1:
                        // 1: as 0, but write empty folder(s)
                        WriteIPFFile(outputIPFFile, outputFilename, settings.IsKeepFileDateTime);
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
                        // 0: skip empty folder; write empty (clipped) iMOD-file (ASC/IDF: NoData within clipextent). Note: process like option 1, empty folder is removed later.
                    case 1:
                        // 1: as 0, but write empty folder(s)
                        if (settings.IsNoDataGridSkipped)
                        {
                            Log.AddInfo("Skipped " + Path.GetFileName(currentFilename) + " with only NoData values");
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
                    Log.AddInfo("Skipped " + Path.GetFileName(outputASCFile.Filename) + " with only NoData values");
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
                        // 0: skip empty folder; write empty (clipped) iMOD-file (NoData within clipextent). Note: process like option 1, empty folder is removed later.
                    case 1:
                        // 1: as 0, but write empty folder(s)
                        if (settings.IsNoDataGridSkipped)
                        {
                            Log.AddInfo("Skipped " + Path.GetFileName(currentFilename) + " with only NoData values");
                            skippedNoDataFileCount++;
                        }
                        else
                        {
                            Log.AddInfo("Input file has no overlap with clipboundary. NoData-file is written.", 1);
                            outputIDFFile = new IDFFile(outputFilename, settings.Extent, currentIDFFile.XCellsize, currentIDFFile.YCellsize, currentIDFFile.NoDataValue);
                            outputIDFFile.ResetValues();

                            WriteIDFFile(outputIDFFile, outputFilename, settings.IsKeepFileDateTime);
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
                    Log.AddInfo("Skipped " + Path.GetFileName(outputIDFFile.Filename) + " with only NoData values");
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
        /// Clip specified IPF-file with specified settings
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual GENFile ClipGENFile(GENFile genFile, SIFToolSettings settings)
        {
            return genFile.ClipGEN(settings.Extent);
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
                    //if (log != null)
                    //{
                    //    Log.AddInfo("Copying isolated MET-file: " + currentFilename);
                    //}
                }
            }
            if (Path.GetExtension(currentFilename).ToLower().Equals(".dat"))
            {
                // For DAT-files, check if a corresponding GEN-file is present. In that case the DAT-file is handled during the clip
                string[] filenames = Directory.GetFiles(Path.GetDirectoryName(currentFilename), Path.GetFileNameWithoutExtension(currentFilename) + ".GEN");
                if (filenames.Length >= 1)
                {
                    // A corresponding GEN-file is present, no need to copy DAT-file
                    isCopied = false;
                }
                else
                {
                    // No corresponding GEN-file is present, assume this DAT-file is something else
                    isCopied = true;
                    //if (log != null)
                    //{
                    //    Log.AddInfo("Copying isolated DAT-file: " + currentFilename);
                    //}
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
        /// Write specified IDF-file and optionally keep existing date and time of source IDF-file
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="newFilename"></param>
        /// <param name="isKeepFileDateTime"></param>
        protected void WriteIDFFile(IDFFile idfFile, string newFilename, bool isKeepFileDateTime = false)
        {
            if (isKeepFileDateTime)
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
        /// Write specified IPF-file and optionally keep existing date and time of source IDF-file
        /// </summary>
        /// <param name="ipfFile"></param>
        /// <param name="newFilename"></param>
        /// <param name="isKeepFileDateTime"></param>
        public void WriteIPFFile(IPFFile ipfFile, string newFilename, bool isKeepFileDateTime = false)
        {
            if (ipfFile == null)
            {
                // Create empty IPF-file
                ipfFile = new IPFFile();
                ipfFile.AddXYColumns();
            }

            if (isKeepFileDateTime)
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

        /// <summary>
        /// Write specified IPF-file and optionally keep existing date and time of source IDF-file
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="newFilename"></param>
        /// <param name="isKeepFileDateTime"></param>
        /// <param name="isDATFileWritten"></param>
        public void WriteGENFile(GENFile genFile, string newFilename, bool isKeepFileDateTime = false, bool isDATFileWritten = true)
        {
            if (genFile == null)
            {
                // Create empty IPF-file
                genFile = new GENFile();
            }

            if (!isDATFileWritten && (genFile.DATFile != null))
            {
                // Remove DAT-file
                genFile.DATFile = null;
            }

            if (isKeepFileDateTime)
            {
                if (File.Exists(genFile.Filename))
                {
                    DateTime lastWriteTime = File.GetLastWriteTime(genFile.Filename);
                    genFile.WriteFile(newFilename, null);
                    File.SetLastWriteTime(newFilename, lastWriteTime);
                }
                else
                {
                    genFile.WriteFile(newFilename, null);
                }
            }
            else
            {
                genFile.WriteFile(newFilename, null);
            }
        }
    }
}
