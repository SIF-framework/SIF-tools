// IDFvoxel is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFvoxel.
// 
// IDFvoxel is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFvoxel is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFvoxel. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.IDF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IDFvoxel
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
            ToolPurpose = "SIF-tool for handling IDF-voxel files";
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

            long fileCount = 0;
            VoxelFileStatistics voxelFileStatistics = new VoxelFileStatistics();

            ProcessFiles(settings.InputPath, settings.InputPath, settings.OutputPath, settings, voxelFileStatistics, ref fileCount, Log);

            Log.AddInfo();
            Log.AddInfo(fileCount + " file(s) have been processed successfully");
            if (settings.IsITBChecked)
            {
                Log.AddInfo(voxelFileStatistics.MissingITBFileCount + " IDF-files are missing ITB-levels");
            }

            ToolSuccessMessage = null;

            return exitcode;
        }

        protected virtual void ProcessFiles(string inputPath, string baseInputPath, string outputPath, SIFToolSettings settings, VoxelFileStatistics voxelFileStatistics, ref long fileCount, Log log)
        {
            switch (Path.GetExtension(settings.InputFilter).ToLower())
            {
                case ".idf":
                    ProcessIDFFiles(inputPath, inputPath, outputPath, settings, voxelFileStatistics, ref fileCount, Log);
                    break;
                case ".csv":
                    ProcessCSVFiles(inputPath, inputPath, outputPath, settings, voxelFileStatistics, ref fileCount, Log);
                    break;
                case ".zip":
                    ProcessZIPFiles(inputPath, inputPath, outputPath, settings, voxelFileStatistics, ref fileCount, Log);
                    break;
                default:
                    throw new ToolException("Unexpected extension for input filter, specify either .IDF or .CSV: " + settings.InputFilter);
            }

            if (settings.IsRecursive)
            {
                string[] subDirectories = Directory.GetDirectories(inputPath);
                foreach (string subDirPath in subDirectories)
                {
                    string outputSubDirPath = Path.Combine(settings.OutputPath, FileUtils.GetRelativePath(subDirPath, baseInputPath));
                    ProcessFiles(subDirPath, baseInputPath, outputSubDirPath, settings, voxelFileStatistics, ref fileCount, log);
                }
            }
        }

        protected virtual void ProcessIDFFiles(string inputPath, string baseInputPath, string outputPath, SIFToolSettings settings, VoxelFileStatistics voxelFileStatistics, ref long fileCount, Log log)
        {
            string[] inputFilenames = Directory.GetFiles(inputPath, settings.InputFilter);

            // Sort input filenames
            SortedDictionary<string, string> sortedInputFilenameDictionary = SortGeoTOPFilenames(inputFilenames, out bool isTopDownSorted, log);

            IDFFile voxelIDFFile = null;
            int filenameIdx = 0;
            foreach (string shortSortableName in sortedInputFilenameDictionary.Keys)
            {
                filenameIdx++;
                string inputFilename = sortedInputFilenameDictionary[shortSortableName];
                string relInputFilename = FileUtils.GetRelativePath(inputFilename, baseInputPath);
                log.AddInfo("Processing " + filenameIdx + "/" + sortedInputFilenameDictionary.Keys.Count + ": " + relInputFilename + " ...");

                voxelIDFFile = IDFFile.ReadFile(inputFilename);

                if (settings.IsITBChecked)
                {
                    CheckITBLevels(voxelIDFFile, log, voxelFileStatistics);
                }
                else
                {
                    voxelIDFFile.Filename = Path.Combine(Path.Combine(settings.OutputPath, Path.GetDirectoryName(FileUtils.GetRelativePath(voxelIDFFile.Filename, settings.InputPath)), Path.GetFileName(voxelIDFFile.Filename)));
                    if (settings.IsITBUpdated)
                    {
                        UpdateITBLevels(voxelIDFFile, settings, log);
                    }
                    if (settings.IsRenamed)
                    {
                        RenameVoxelFile(voxelIDFFile, inputFilenames.Length, isTopDownSorted, settings, log);
                    }
                    // TODO option z
                    // TODO option d, o

                    WriteVoxelIDFFile(voxelIDFFile, settings, log);
                }
                fileCount++;
            }
        }

        private void RenameVoxelFile(IDFFile idfFile, int idfFileCount, bool isTopDownSorted, SIFToolSettings settings, Log log)
        {
            if (settings.InputPath.Equals(settings.OutputPath))
            {
                if (!settings.IsOverwrite)
                {
                    throw new ToolException("Input path is equal to output path, but overwrite option has not been set");
                }

                // Remove old file
                try
                {
                    File.Delete(idfFile.Filename);
                }
                catch (Exception ex)
                {
                    throw new ToolException("Existing output file cannot be deleted: " + idfFile.Filename, ex);
                }
            }

            float botLevel = ParseGeoTOPFilenameLevel(idfFile.Filename);
            string shortName = GetShortGeoTOPFilename(idfFile.Filename, botLevel, true, idfFileCount);
            idfFile.Filename = Path.Combine(Path.GetDirectoryName(idfFile.Filename), shortName + ".IDF");

        }

        protected void ProcessCSVFiles(string inputPath, string baseInputPath, string outputPath, SIFToolSettings settings, VoxelFileStatistics voxelFileStatistics, ref long fileCount, Log log)
        {
            string[] inputFilenames = Directory.GetFiles(inputPath, settings.InputFilter);
            for (int idx = 0; idx < inputFilenames.Length; idx++)
            {
                try
                {
                    ProcessCSVFile(inputFilenames[idx], outputPath, settings, voxelFileStatistics, log);
                    fileCount++;
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not process CSV-file: " + inputFilenames[idx], ex);
                }
            }
        }

        private void ProcessZIPFiles(string inputPath, string baseInputPath, string outputPath, SIFToolSettings settings, VoxelFileStatistics voxelFileStatistics, ref long fileCount, Log log)
        {
            string[] inputFilenames = Directory.GetFiles(inputPath, settings.InputFilter);

            int filenameIdx = 0;
            List<List<string>> voxelLineLists = null;

            // Create list of lists with all selected voxel lines per CSV-file
            voxelLineLists = new List<List<string>>();

            foreach (string inputFilename in inputFilenames)
            {
                filenameIdx++;
                string relInputFilename = FileUtils.GetRelativePath(inputFilename, baseInputPath);
                log.AddInfo("Processing " + filenameIdx + "/" + inputFilenames.Length + ": " + relInputFilename + " ...");

                // string csvString = null;
                ZipArchive zipArchive = ZipFile.OpenRead(inputFilename);
                foreach (ZipArchiveEntry zipArchiveEntry in zipArchive.Entries)
                {
                    if (zipArchiveEntry.Name.ToLower().EndsWith(".csv"))
                    {
                        log.AddInfo("Reading " + zipArchiveEntry.Name + "...", 1);

                        Stream stream = zipArchiveEntry.Open();
                        StreamReader sr = null;
                        try
                        {
                            sr = new StreamReader(stream);
                            List<string> voxelLines = null;
                            if (settings.zoneGENFile != null)
                            {
                                // Select voxels within zone and add to other memory stream with selected voxels
                                voxelLines = ProcessCSVStream(sr, zipArchiveEntry.Name, settings.zoneGENFile, log, 2);
                            }
                            else
                            {
                                // Process all voxels in CSV-file stream and create corresponding, seperate IDF voxel model
                                voxelLines = ProcessCSVStream(sr, zipArchiveEntry.Name, log, 2);
                            }

                            if (voxelLines != null)
                            {
                                voxelLineLists.Add(voxelLines);
                            }

                            fileCount++;
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Error when reading ZIP-file: " + zipArchiveEntry.Name, ex);
                        }
                        finally
                        {
                            if (sr != null)
                            {
                                sr.Close();
                            }
                            if (stream != null)
                            {
                                stream.Close();
                            }
                        }
                    }
                }
            }

            try
            {
                // Process selected voxels and create merged IDF voxel model
                ProcessCSVVoxelLines(voxelLineLists, null, outputPath, settings, voxelFileStatistics, log, 0);
            }
            catch (Exception ex)
            {
                throw new ToolException("Could not process merged CSV-stream", ex);
            }
        }

        /// <summary>
        /// Add contents of CSV-file stream to memory stream with selected voxel lines
        /// </summary>
        private List<string> ProcessCSVStream(StreamReader sr, string name, GENFile genFile, Log log, int logIndentLevel)
        {
            List<string> voxelLineList = new List<string>();

            List<GENPolygon> genPolygons = genFile.RetrieveGENPolygons();
            List<Extent> genPolygonExtents = new List<Extent>();
            foreach (GENPolygon genPolygon in genPolygons)
            {
                genPolygonExtents.Add(genPolygon.RetrieveExtent());
            }

            if ((genPolygons == null) || (genPolygons.Count == 0))
            {
                throw new ToolException("Specified zone GEN-file doesn't contain any polygons: " + genFile.Filename);
            }

            int lineCount = 0;
            string line = null;
            try
            {
                // Read column names. The first 3 columns should be: x,y,z. And normally columns 4 and 5 contain: lithostrat and lithoklasse
                line = sr.ReadLine().Trim();
                lineCount++;

                string[] lineValues = line.Split(new char[] { ',' });
                string[] columnNames = lineValues;
                if ((columnNames.Length < 5) || !columnNames[0].ToLower().StartsWith("x") || !columnNames[1].ToLower().StartsWith("y") || !columnNames[2].ToLower().StartsWith("z"))
                {
                    log.AddWarning("Contents of CSV-file are not recognized as GeoTOP-data (x,y,z header is missing) and is skipped: " + name, logIndentLevel);
                    return null;
                }

                voxelLineList.Add(line);

                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    lineCount++;

                    lineValues = line.Split(new char[] { ',' });

                    // Read x and y
                    float x = float.Parse(lineValues[0], NumberStyles.Float, EnglishCultureInfo);
                    float y = float.Parse(lineValues[1], NumberStyles.Float, EnglishCultureInfo);

                    Point voxelPoint = new FloatPoint(x, y);
                    for (int genPolygonIdx = 0; genPolygonIdx <  genPolygons.Count(); genPolygonIdx++)
                    {
                        // First check if point is within bounding box extent around polygon
                        Extent genPolygonExtent = genPolygonExtents[genPolygonIdx];
                        if (genPolygonExtent.Contains(x, y))
                        {
                            // Now check if point is within polygon
                            GENPolygon genPolygon = genPolygons[genPolygonIdx];
                            if (voxelPoint.IsInside(genPolygon.Points))
                            {
                                voxelLineList.Add(line);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error when reading line " + lineCount + ": " + line, ex);
            }

            return voxelLineList;
        }

        /// <summary>
        /// Add contents of CSV-file stream to memory stream with selected voxel lines
        /// </summary>
        private List<string> ProcessCSVStream(StreamReader sr, string name, Log log, int logIndentLevel)
        {
            List<string> voxelLineList = new List<string>();

            int lineCount = 0;
            string line = null;
            try
            {
                // Read column names. The first 3 columns should be: x,y,z. And normally columns 4 and 5 contain: lithostrat and lithoklasse
                line = sr.ReadLine().Trim();
                lineCount++;

                string[] lineValues = line.Split(new char[] { ',' });
                string[] columnNames = lineValues;
                if ((columnNames.Length < 5) || !columnNames[0].ToLower().StartsWith("x") || !columnNames[1].ToLower().StartsWith("y") || !columnNames[2].ToLower().StartsWith("z"))
                {
                    log.AddWarning("Contents of CSV-file are not recognized as GeoTOP-data (x,y,z header is missing) and is skipped: " + name, logIndentLevel);
                    return null;
                }
                voxelLineList.Add(line);

                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine();
                    lineCount++;
                    voxelLineList.Add(line);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error when reading line " + lineCount + ": " + line, ex);
            }

            return voxelLineList;
        }

        protected void ProcessCSVFile(string csvFilename, string outputPath, SIFToolSettings settings, VoxelFileStatistics voxelFileStatistics, Log log)
        {
            try
            {
                string[] voxelLines = File.ReadAllLines(csvFilename);
                List<List<string>> voxelLineLists = new List<List<string>>();
                voxelLineLists.Add(voxelLines.ToList());

                ProcessCSVVoxelLines(voxelLineLists, Path.GetFileName(csvFilename), outputPath, settings, voxelFileStatistics, log, 1);
            }
            catch (Exception ex)
            {
                throw new Exception("Error when reading CSV-file: " + csvFilename, ex);
            }
        }

        protected void ProcessCSVVoxelLines(List<List<string>> voxelLineLists, string csvname, string outputPath, SIFToolSettings settings, VoxelFileStatistics voxelFileStatistics, Log log, int logIndentLevel)
        {
            log.AddInfo("Parsing voxels ...", logIndentLevel);

            float voxelThickness = settings.VoxelThickness; //  float.NaN;
            float voxelWidth = settings.VoxelWidth; // float.NaN;
            string[] columnNames = null;
            Extent extent = null;
            SortedDictionary<float, List<CSVVoxel>> csvVoxelLayers = null;
            csvVoxelLayers = ReadVoxelLayers(voxelLineLists, out int linecount, ref voxelThickness, ref voxelWidth, out columnNames, out extent);
            log.AddInfo("processed " + linecount + " voxel lines", logIndentLevel + 1);

            log.AddInfo();
            log.AddInfo("Voxel dateset properties from CSV-file", logIndentLevel);
            log.AddInfo("--------------------------------------", logIndentLevel);
            if (extent != null)
            {
                log.AddInfo("Extent: " + extent.ToString(), logIndentLevel);
            }
            log.AddInfo("Resolution: " + voxelWidth.ToString(EnglishCultureInfo), logIndentLevel);
            log.AddInfo("Thickness: " + voxelThickness.ToString(EnglishCultureInfo), logIndentLevel);
            if (columnNames != null)
            {
                log.AddInfo("Columns: " + CommonUtils.ToString(columnNames.ToList()), logIndentLevel);
            }
            log.AddInfo();

            float width = (settings.VoxelWidth.Equals(float.NaN)) ? voxelWidth : settings.VoxelWidth;
            float thickness = (settings.VoxelThickness.Equals(float.NaN)) ? voxelThickness : settings.VoxelThickness;

            log.AddInfo("Converting CSV-file to IDF-file(s), using resolution " + width.ToString(EnglishCultureInfo) + " and thickness " + thickness.ToString(EnglishCultureInfo), logIndentLevel);
            if (csvname != null)
            {
                outputPath = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(csvname));
            }
            ConvertCSVVoxelsToIDF(csvVoxelLayers, extent, width, thickness, outputPath, settings, voxelFileStatistics);
        }

        private SortedDictionary<float, List<CSVVoxel>> ReadVoxelLayers(List<List<string>> voxelLineLists, out int lineCount, ref float voxelThickness, ref float voxelWidth, out string[] columnNames, out Extent extent)
        {
            string line = null;
            lineCount = 0;
            extent = null;
            SortedDictionary<float, List<CSVVoxel>> csvVoxelLayers = new SortedDictionary<float, List<CSVVoxel>>();
            float minX = float.MaxValue;
            float minY = float.MaxValue;
            float minZ = float.MaxValue;
            float maxX = float.MinValue;
            float maxY = float.MinValue;
            float maxZ = float.MinValue;

            float x0 = float.NaN;
            float z0 = float.NaN;
            columnNames = null;

            for (int listIdx = 0; listIdx < voxelLineLists.Count(); listIdx++)
            {
                try
                {
                    List<string> voxelLines = voxelLineLists[listIdx];

                    if (voxelLines.Count() <= 1)
                    {
                        // empty list, skip
                        continue;
                    }
                    // Read column names. The first 3 columns should be: x,y,z. And normally columns 4 and 5 contain: lithostrat and lithoklasse
                    line = voxelLines[0];
                    string[] lineValues = line.Trim().Split(new char[] { ',' });
                    columnNames = lineValues;
                    CSVVoxel.ColumnNames = columnNames;

                    Log.AddInfo("Parsing voxel dataset " + (listIdx + 1) + "/" + voxelLineLists.Count() + " ...", 1);

                    List<CSVVoxel> voxelLayer = null;
                    for (int lineIdx = 1; lineIdx < voxelLines.Count(); lineIdx++)
                    {
                        line = voxelLines[lineIdx];
                        lineValues = line.Split(new char[] { ',' });
                        CSVVoxel csvVoxel = new CSVVoxel(lineValues);

                        if (!csvVoxelLayers.ContainsKey(csvVoxel.Z))
                        {
                            csvVoxelLayers.Add(csvVoxel.Z, new List<CSVVoxel>());
                        }
                        voxelLayer = csvVoxelLayers[csvVoxel.Z];
                        voxelLayer.Add(csvVoxel);

                        // update gridextent
                        if (csvVoxel.X < minX)
                        {
                            minX = csvVoxel.X;
                        }
                        else if (csvVoxel.X > maxX)
                        {
                            maxX = csvVoxel.X;
                        }
                        if (csvVoxel.Y < minY)
                        {
                            minY = csvVoxel.Y;
                        }
                        else if (csvVoxel.Y > maxY)
                        {
                            maxY = csvVoxel.Y;
                        }
                        if (csvVoxel.Z < minZ)
                        {
                            minZ = csvVoxel.Z;
                        }
                        else if (csvVoxel.Z > maxZ)
                        {
                            maxZ = csvVoxel.Z;
                        }

                        // Calculate voxel resolution from CSV-file. Note this buggy (when first xy are seperated by more than actual width), todo: calculate properly
                        if (voxelWidth.Equals(float.NaN) && !csvVoxel.X.Equals(x0))
                        {
                            // Assume voxels are ordered by x and z
                            if (x0.Equals(float.NaN))
                            {
                                x0 = csvVoxel.X;
                            }
                            else
                            {
                                voxelWidth = Math.Abs(csvVoxel.X - x0);
                            }
                        }

                        // Calculate voxel thickness from CSV-file. 
                        if (voxelThickness.Equals(float.NaN) && !csvVoxel.Z.Equals(z0))
                        {
                            // Assume voxels are ordered by x and z
                            if (z0.Equals(float.NaN))
                            {
                                z0 = csvVoxel.Z;
                            }
                            else
                            {
                                voxelThickness = Math.Abs(csvVoxel.Z - z0);
                            }
                        }
                    }

                    lineCount += voxelLines.Count();
                    extent = new Extent(minX - voxelWidth / 2, minY - voxelWidth / 2, maxX + voxelWidth / 2, maxY + voxelWidth / 2);

                    // Log.AddMessage(LogLevel.Info, "Currently " + (GC.GetTotalMemory(true) / 1000000) + "Mb memory is in use.", 1);
                    // Remove current list from memory
                    voxelLineLists[listIdx] = null;
                    // GC.Collect();
                    // Log.AddMessage(LogLevel.Info, "Currently " + (GC.GetTotalMemory(true) / 1000000) + "Mb memory is in use.", 1);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error when reading line " + lineCount + " in dataset " + (listIdx + 1) + ": " + line, ex);
                }
            }

            return csvVoxelLayers;
        }

        private SortedDictionary<float, List<CSVVoxel>> ReadVoxelLayers(List<string> voxelLines, out int lineCount, out float voxelThickness, out float voxelWidth, out string[] columnNames, out Extent extent)
        {
            lineCount = 0;
            string line = null;
            SortedDictionary<float, List<CSVVoxel>> csvVoxelLayers = new SortedDictionary<float, List<CSVVoxel>>();
            try
            {
                float minX = float.MaxValue;
                float minY = float.MaxValue;
                float minZ = float.MaxValue;
                float maxX = float.MinValue;
                float maxY = float.MinValue;
                float maxZ = float.MinValue;

                float x0 = float.NaN;
                float z0 = float.NaN;
                voxelThickness = float.NaN;
                voxelWidth = float.NaN;
                columnNames = null;

                // Read column names. The first 3 columns should be: x,y,z. And normally columns 4 and 5 contain: lithostrat and lithoklasse
                if ((voxelLines == null) || (voxelLines.Count <= 1))
                {
                    throw new ToolException("No voxels found, voxel model cannot be written");
                }
                line = voxelLines[lineCount++];
                string[] lineValues = line.Trim().Split(new char[] { ',' });
                columnNames = lineValues;
                CSVVoxel.ColumnNames = columnNames;

                List<CSVVoxel> voxelLayer = null;
                while (lineCount < voxelLines.Count)
                {
                    line = voxelLines[lineCount++];
                    lineValues = line.Split(new char[] { ',' });
                    CSVVoxel csvVoxel = new CSVVoxel(lineValues);

                    if (!csvVoxelLayers.ContainsKey(csvVoxel.Z))
                    {
                        csvVoxelLayers.Add(csvVoxel.Z, new List<CSVVoxel>());
                    }
                    voxelLayer = csvVoxelLayers[csvVoxel.Z];
                    voxelLayer.Add(csvVoxel);

                    // update gridextent
                    if (csvVoxel.X < minX)
                    {
                        minX = csvVoxel.X;
                    }
                    else if (csvVoxel.X > maxX)
                    {
                        maxX = csvVoxel.X;
                    }
                    if (csvVoxel.Y < minY)
                    {
                        minY = csvVoxel.Y;
                    }
                    else if (csvVoxel.Y > maxY)
                    {
                        maxY = csvVoxel.Y;
                    }
                    if (csvVoxel.Z < minZ)
                    {
                        minZ = csvVoxel.Z;
                    }
                    else if (csvVoxel.Z > maxZ)
                    {
                        maxZ = csvVoxel.Z;
                    }

                    // Calculate voxel resolution from CSV-file. Note this buggy (when first xy are seperated by more than actual width), todo: calculate properly
                    if (voxelWidth.Equals(float.NaN) && !csvVoxel.X.Equals(x0))
                    {
                        // Assume voxels are ordered by x and z
                        if (x0.Equals(float.NaN))
                        {
                            x0 = csvVoxel.X;
                        }
                        else
                        {
                            voxelWidth = Math.Abs(csvVoxel.X - x0);
                        }
                    }

                    // Calculate voxel thickness from CSV-file. 
                    if (voxelThickness.Equals(float.NaN) && !csvVoxel.Z.Equals(z0))
                    {
                        // Assume voxels are ordered by x and z
                        if (z0.Equals(float.NaN))
                        {
                            z0 = csvVoxel.Z;
                        }
                        else
                        {
                            voxelThickness = Math.Abs(csvVoxel.Z - z0);
                        }
                    }
                }

                extent = new Extent(minX - voxelWidth / 2, minY - voxelWidth / 2, maxX + voxelWidth / 2, maxY + voxelWidth / 2);
            }
            catch (Exception ex)
            {
                throw new Exception("Error when reading line " + lineCount + ": " + line, ex);
            }

            return csvVoxelLayers;
        }

        protected void ConvertCSVVoxelsToIDF(SortedDictionary<float, List<CSVVoxel>> csvVoxelLayers, Extent extent, float voxelWidth, float voxelThickness, string outputPath, SIFToolSettings settings, VoxelFileStatistics voxelFileStatistics)
        {
            List<float> zValues = csvVoxelLayers.Keys.ToList();

            for (int layerIdx = 0; layerIdx < csvVoxelLayers.Count; layerIdx++)
            {
                List<CSVVoxel> csvVoxelLayer = csvVoxelLayers[zValues[layerIdx]];
                float z = csvVoxelLayer[0].Z;
                float absVoxelBotCM = Math.Abs((z - voxelThickness / 2) * 100);

                string levelString = absVoxelBotCM.ToString("F0").PadLeft(4, '0') + "_cm_" + ((z < 0) ? "onder" : "boven") + "_nap";
                Log.AddInfo("Processing voxel layer " + (layerIdx + 1) + "/" + csvVoxelLayers.Count + " " + levelString + " ...", 1);

                // Initialize IDF-file(s) for specified columnnumbers
                IDFFile[] idfFiles = new IDFFile[settings.CSVColumnNumbers.Count];
                for (int colNrIdx = 0; colNrIdx < settings.CSVColumnNumbers.Count; colNrIdx++)
                {
                    int csvColumnNumber = settings.CSVColumnNumbers[colNrIdx];
                    if (csvColumnNumber < 0)
                    {
                        csvColumnNumber *= -1;
                    }
                    if ((csvColumnNumber == 0) || (csvColumnNumber > CSVVoxel.ColumnCount))
                    {
                        throw new ToolException("Invalid CSV-columnnumber " + csvColumnNumber + ", please specify column numbers between 1 and " + CSVVoxel.ColumnCount);
                    }
                    string colName = CSVVoxel.ColumnNames[csvColumnNumber - 1];
                    string subdir = null;
                    string filePrefix = null;
                    switch (csvColumnNumber)
                    {
                        case 4:
                            subdir = "geologische eenheid";
                            filePrefix = "strat";
                            break;
                        case 5:
                            subdir = "lithoklasse";
                            filePrefix = "lith";
                            break;
                        default:
                            subdir = CorrectFilename(colName);
                            filePrefix = CorrectFilename(colName).Replace("_", string.Empty);
                            break;
                    }
                    string idfFilename = Path.Combine(Path.Combine(outputPath, subdir), filePrefix + "_" + (csvVoxelLayers.Count - layerIdx).ToString().PadLeft(3, '0') + "_" + levelString + ".IDF");
                    idfFiles[colNrIdx] = new IDFFile(idfFilename, extent, voxelWidth, -9999.0f);
                    idfFiles[colNrIdx].ResetValues();
                }

                // Write voxel-values to IDF-file(s)
                for (int idfIdx = 0; idfIdx < idfFiles.Length; idfIdx++)
                {
                    int csvColumnNumber = settings.CSVColumnNumbers[idfIdx];
                    bool isLog10Value = (csvColumnNumber < 0);
                    if (isLog10Value)
                    {
                        csvColumnNumber *= -1;
                    }

                    for (int voxelIdx = 0; voxelIdx < csvVoxelLayer.Count; voxelIdx++)
                    {
                        CSVVoxel voxel = csvVoxelLayer[voxelIdx];

                        float value = voxel.Values[csvColumnNumber - 1];
                        if (value.Equals(CSVVoxel.NoDataValue))
                        {
                            idfFiles[idfIdx].SetValue(voxel.X, voxel.Y, idfFiles[idfIdx].NoDataValue);
                        }
                        else
                        {
                            if (isLog10Value)
                            {
                                value = (float)Math.Exp(value);
                            }
                            idfFiles[idfIdx].SetValue(voxel.X, voxel.Y, value);
                        }
                    }
                }

                // Write IDF-file(s)
                for (int idfIdx = 0; idfIdx < idfFiles.Length; idfIdx++)
                {
                    idfFiles[idfIdx].SetITBLevels(z + voxelThickness / 2, z - voxelThickness / 2);

                    if (settings.IsRenamed)
                    {
                        RenameVoxelFile(idfFiles[idfIdx], idfFiles.Length, true, settings, Log);
                    }
                    // TODO option z
                    // TODO option d, o

                    WriteVoxelIDFFile(idfFiles[idfIdx], settings, Log);
                }
            }
        }

        /// <summary>
        /// Sorts GeoTOP filenames from high to low level. If log is defined a warning is given for sort inconsistencies.
        /// </summary>
        /// <param name="inputFilenames"></param>
        /// <param name="isTopDownSorted"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        protected SortedDictionary<string, string> SortGeoTOPFilenames(string[] inputFilenames, out bool isTopDownSorted, Log log = null)
        {
            isTopDownSorted = true;
            float prevBotLevel = float.NaN;
            bool hasSortInconsistency = false;
            SortedDictionary<string, string> sortedInputFilenameDictionary = new SortedDictionary<string, string>();
            for (int idx = 0; idx < inputFilenames.Length; idx++)
            {
                string inputFilename = inputFilenames[idx];
                float botLevel = ParseGeoTOPFilenameLevel(inputFilename);
                string sortableName = GetShortGeoTOPFilename(inputFilename, botLevel, isTopDownSorted, inputFilenames.Length);
                if (idx == 1)
                {
                    isTopDownSorted = (botLevel < prevBotLevel);
                    if (!isTopDownSorted)
                    {
                        // Order is retrieved from first two files. Initially topdown order is assumed, but correct if it was bottomup.
                        sortableName = GetShortGeoTOPFilename(inputFilename, botLevel, isTopDownSorted, inputFilenames.Length);
                    }
                }
                else
                {
                    hasSortInconsistency = (isTopDownSorted && (botLevel >= prevBotLevel)) || (!isTopDownSorted && (botLevel <= prevBotLevel));
                    if (hasSortInconsistency && (log != null))
                    {
                        log.AddWarning("Inconsistency in file order (BOT-level file 1: " + prevBotLevel.ToString(EnglishCultureInfo) + "; file2: " + botLevel.ToString(EnglishCultureInfo) + ") for file: " + Path.GetFileName(inputFilename));
                    }
                }
                prevBotLevel = botLevel;

                sortedInputFilenameDictionary.Add(sortableName, inputFilename);
            }

            return sortedInputFilenameDictionary;
        }

        /// <summary>
        /// Parse a standard or short GeoTOP filename and retrieve BOT-level
        /// </summary>
        /// <param name="inputFilename">a filename with GeoTOP format</param>
        protected float ParseGeoTOPFilenameLevel(string inputFilename)
        {
            if (IsDefaultGeoTOPFormatFilename(inputFilename))
            {
                return ParseDefaultGeoTOPFilenameLevel(inputFilename);
            }
            else if (IsShortGeoTOPFormatFilename(inputFilename))
            {
                return ParseShortGeoTOPFilenameLevel(inputFilename);
            }
            else
            {
                throw new ToolException("Invalid GeoTOP-filename, expected 4 or 6 parts seperated by '_' symbols: " + Path.GetFileName(inputFilename));
            }
        }

        ///// <summary>
        ///// Parse a standard or short GeoTOP filename and retrieve TOP- and BOT-level and creates a short, sortable name
        ///// </summary>
        ///// <param name="inputFilename">a filename with GeoTOP format</param>
        ///// <param name="inputFileCount">total (one-based) number of files to process</param>
        ///// <param name="isTopDownSorted">true if input files are sorted from high to low level</param>
        ///// <param name="botLevel">BOT-level in meters relative to NAP</param>
        ///// <param name="shortSortableName"></param>
        //protected void ParseGeoTOPFilenameLevels(string inputFilename, int inputFileCount, bool isTopDownSorted, out float botLevel, out string shortSortableName)
        //{
        //    if (IsDefaultGeoTOPFormatFilename(inputFilename))
        //    {
        //        ParseGeoTOPDefaultFilenameLevel(inputFilename, inputFileCount, isTopDownSorted, out botLevel, out shortSortableName);
        //    }
        //    else if (IsShortGeoTOPFormatFilename(inputFilename))
        //    {
        //        ParseGeoTOPShortFilenameLevels(inputFilename, out botLevel);
        //        shortSortableName = Path.GetFileNameWithoutExtension(inputFilename);
        //    }
        //    else
        //    {
        //        throw new ToolException("Invalid GeoTOP-filename, expected 4 or 6 parts seperated by '_' symbols: " + Path.GetFileName(inputFilename));
        //    }
        //}

        /// <summary>
        /// Check that specified filename has default the GeoTOP format. IF not a ToolException is thrown.
        /// </summary>
        /// <param name="inputFilename"></param>
        protected void CheckDefaultGeoTOPFilename(string inputFilename)
        {
            string name = Path.GetFileNameWithoutExtension(inputFilename);
            string[] parts = name.Split('_');
            if (parts.Length != 6)
            {
                throw new ToolException("Unknown filename format: 6 substrings seperated by underscore expected: " + name);
            }

            if (!parts[5].ToLower().Equals("nap"))
            {
                throw new ToolException("Currently only levels relative to NAP are supported: " + name);
            }

            if ((!parts[4].ToLower().Equals("onder") && !parts[4].ToLower().Equals("boven")) || !parts[3].ToLower().Equals("cm"))
            {
                throw new ToolException("Unknown filename format: expected format 'xxx_iii_llll_cm_[onder|boven]_nap':" + name);
            }

            string botLevelString = parts[2];
            if (!float.TryParse(botLevelString, NumberStyles.Float, EnglishCultureInfo, out float value))
            {
                throw new ToolException("Invalid level string:" + botLevelString);
            }

            if (!int.TryParse(parts[1], out int geotopIdx))
            {
                throw new ToolException("Unexpected GeoTOP index '" + parts[1] + "' + in filename:" + Path.GetFileName(inputFilename));
            }
        }

        protected void CheckShortGeoTOPFilename(string inputFilename)
        {
            string name = Path.GetFileNameWithoutExtension(inputFilename);
            string[] parts = name.Split('_');
            if (parts.Length != 4)
            {
                throw new ToolException("Unknown filename format: 4 substrings seperated by underscore expected: " + name);
            }

            if (!float.TryParse(parts[2], NumberStyles.Float, EnglishCultureInfo, out float value))
            {
                throw new ToolException("Invalid level string:" + parts[2]);
            }

            if (!parts[3].ToLower().Equals("nap"))
            {
                throw new ToolException("Currently only levels relative to NAP are supported: " + name);
            }

        }

        /// <summary>
        /// Parse a standard GeoTOP filename and retrieve BOT-level from it
        /// </summary>
        /// <param name="inputFilename">a filename with GeoTOP format</param>
        /// <returns>the bottom level in meters relative to NAP is returned</returns>
        protected float ParseDefaultGeoTOPFilenameLevel(string inputFilename)
        {
            CheckDefaultGeoTOPFilename(inputFilename);

            string name = Path.GetFileNameWithoutExtension(inputFilename);
            string[] parts = name.Split('_');

            float value = float.Parse(parts[2], NumberStyles.Float, EnglishCultureInfo);
            float botLevel = (parts[4].ToLower().Equals("onder")) ? -value / 100f : botLevel = value / 100f;

            return botLevel;
        }

        /// <summary>
        /// Parse a standard (or short) GeoTOP filename and retrieve a short, sortable GeoTOP name
        /// </summary>
        /// <param name="geoTOPFilename">a filename with default or short GeoTOP format</param>
        /// <param name="botLevel">BOT-level for this file</param>
        /// <param name="isTopDownSorted"></param>
        /// <param name="inputFileCount">when sorted top down, this should be the total number of voxel files</param>
        /// <returns>a filename with short format is returned that can be sorted alphabetically</returns>
        protected string GetShortGeoTOPFilename(string geoTOPFilename, float botLevel, bool isTopDownSorted, int inputFileCount)
        {
            string shortSortableName = null;

            if (IsDefaultGeoTOPFormatFilename(geoTOPFilename))
            {
                CheckDefaultGeoTOPFilename(geoTOPFilename);

                string name = Path.GetFileNameWithoutExtension(geoTOPFilename);
                string[] parts = name.Split('_');

                float value = float.Parse(parts[2], NumberStyles.Float, EnglishCultureInfo);
                botLevel = (parts[4].ToLower().Equals("onder")) ? -value / 100f : botLevel = value / 100f;

                int geotopIdx = int.Parse(parts[1]);
                if (isTopDownSorted)
                {
                    geotopIdx = inputFileCount - geotopIdx + 1;
                }

                shortSortableName = parts[0] + "_" + geotopIdx.ToString().PadLeft(3, '0') + "_" + ((botLevel < 0) ? "-" : "+") + parts[2].PadLeft(4, '0') + "_NAP";
            }
            else if (IsShortGeoTOPFormatFilename(geoTOPFilename))
            {
                shortSortableName = Path.GetFileNameWithoutExtension(geoTOPFilename);
            }
            else
            {
                throw new ToolException("Invalid GeoTOP-filename, expected 4 or 6 parts seperated by '_' symbols: " + Path.GetFileName(geoTOPFilename));
            }
            return shortSortableName;
        }

        /// <summary>
        /// Parse a standard GeoTOP filename and retrieves TOP- and BOT-level and creates a short, sortable name
        /// </summary>
        /// <param name="inputFilename">a filename with GeoTOP format</param>
        /// <returns>BOT-level in meters relative to NAP</returns>
        protected float ParseShortGeoTOPFilenameLevel(string inputFilename)
        {
            string name = Path.GetFileNameWithoutExtension(inputFilename);
            string[] parts = name.Split('_');
            if (parts.Length != 4)
            {
                throw new ToolException("Unknown filename format: 4 substrings seperated by underscore expected: " + name);
            }

            string botLevelString = parts[2];
            float value;
            if (!float.TryParse(botLevelString, NumberStyles.Float, EnglishCultureInfo, out value))
            {
                throw new ToolException("Invalid level string:" + botLevelString);
            }

            if (!parts[3].ToLower().Equals("nap"))
            {
                throw new ToolException("Currently only levels relative to NAP are supported: " + name);
            }

            return value / 100f;
        }

        /// <summary>
        /// Checks if filename has GeoTOP format 'xxx_iii_llll_cm_[onder|boven]_nap'
        /// </summary>
        /// <param name="inputFilename"></param>
        /// <returns></returns>
        protected bool IsDefaultGeoTOPFormatFilename(string inputFilename)
        {
            string name = Path.GetFileNameWithoutExtension(inputFilename);
            string[] parts = name.Split('_');
            if (parts.Length != 6)
            {
                return false;
            }

            if (!parts[5].ToLower().Equals("nap"))
            {
                return false;
            }

            if ((!parts[4].ToLower().Equals("onder") && !parts[4].ToLower().Equals("boven")) || !parts[3].ToLower().Equals("cm"))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if filename has short format xxx_iii_llll_nap, with xxx and iii any string, llll a level relative to nap.
        /// </summary>
        /// <param name="inputFilename"></param>
        /// <returns></returns>
        protected bool IsShortGeoTOPFormatFilename(string inputFilename)
        {
            string name = Path.GetFileNameWithoutExtension(inputFilename);
            string[] parts = name.Split('_');
            if (parts.Length != 4)
            {
                return false;
            }

            if ((parts[1].Length != 3) || (parts[2].Length != 5))
            {
                return false;
            }

            int someNumber;
            if (!int.TryParse(parts[1], out someNumber) || !int.TryParse(parts[2], out someNumber))
            {
                return false;
            }

            if (!parts[3].ToLower().Equals("nap"))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if ITB-level are defined (unequal to NoData) in specified IDF-file
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="log"></param>
        /// <param name="missingITBFileCount"></param>
        protected void CheckITBLevels(IDFFile idfFile, Log log, VoxelFileStatistics voxelFileStatistics)
        {
            if (idfFile.TOPLevel.Equals(float.NaN) || idfFile.BOTLevel.Equals(float.NaN))
            {
                log.AddInfo("IDF-file has no ITB-level set: " + Path.GetFileName(idfFile.Filename), 1);
                voxelFileStatistics.MissingITBFileCount++;
            }
        }

        protected void UpdateITBLevels(IDFFile idfFile, SIFToolSettings settings, Log log)
        {
            string inputFilename = idfFile.Filename;
            float botLevel = ParseGeoTOPFilenameLevel(inputFilename);
            float topLevel = botLevel + settings.VoxelThickness;
            idfFile.SetITBLevels(topLevel, botLevel);
        }

        protected void WriteVoxelIDFFile(IDFFile idfFile, SIFToolSettings settings, Log log)
        {
            if (File.Exists(idfFile.Filename))
            {
                if (!settings.IsOverwrite)
                {
                    throw new ToolException("Resultfile already exists and overwrite is not specified: " + idfFile.Filename);
                }
                else
                {
                    try
                    {
                        File.Delete(idfFile.Filename);
                    }
                    catch (Exception ex)
                    {
                        throw new ToolException("Existing output file cannot be deleted: " + idfFile.Filename, ex);
                    }
                }
            }

            try
            {
                if (!settings.IsDeleteEmpty)
                {
                    idfFile.WriteFile(idfFile.Filename);
                }
                else
                {
                    if (idfFile.RetrieveValueCount() > 0)
                    {
                        idfFile.WriteFile(idfFile.Filename);
                    }
                    else
                    {
                        log.AddInfo("Empty IDF-file is not written: " + Path.GetFileName(idfFile.Filename), 2);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ToolException("Result IDF-file has not been written successfully: " + idfFile.Filename, ex);
            }
        }

        protected void UpdateITBLevels2(IDFFile idfFile, string outputPath, int idfFileCount, bool isTopDownSorted, SIFToolSettings settings, Log log)
        {
            string inputFilename = idfFile.Filename;
            float botLevel;
            string shortName = null;
            string outputFilename = null;
            botLevel = ParseGeoTOPFilenameLevel(inputFilename);
            shortName = GetShortGeoTOPFilename(inputFilename, botLevel, isTopDownSorted, idfFileCount);
            if (settings.IsRenamed)
            {
                outputFilename = Path.Combine(outputPath, shortName + ".IDF");
            }
            else
            {
                outputFilename = Path.Combine(outputPath, Path.GetFileName(inputFilename));
            }

            float topLevel = botLevel + settings.VoxelThickness;
            idfFile.SetITBLevels(topLevel, botLevel);

            idfFile.EnsureLoadedValues();

            bool isSourceBackuped = false;
            if (settings.InputPath.Equals(settings.OutputPath))
            {
                if (!settings.IsOverwrite)
                {
                    throw new ToolException("Input path is equal to output path, but overwrite option has not been set");
                }
                else
                {
                    try
                    {
                        // temporarily rename input file as a backup until new file has been written
                        if (File.Exists(inputFilename + ".bak"))
                        {
                            File.Delete(inputFilename + ".bak");
                        }
                        File.Move(inputFilename, inputFilename + ".bak");
                        isSourceBackuped = true;
                    }
                    catch (Exception ex)
                    {
                        throw new ToolException("Input file could not be renamed as a backup: " + inputFilename, ex);
                    }
                }
            }

            if (File.Exists(outputFilename))
            {
                if (!settings.IsOverwrite)
                {
                    throw new ToolException("Resultfile already exists and overwrite is not specified: " + outputFilename);
                }
                else
                {
                    try
                    {
                        File.Delete(outputFilename);
                    }
                    catch (Exception ex)
                    {
                        throw new ToolException("Existing output file cannot be deleted: " + outputFilename, ex);
                    }
                }
            }

            try
            {
                if (!settings.IsDeleteEmpty)
                {
                    idfFile.WriteFile(outputFilename);
                }
                else
                {
                    if (idfFile.RetrieveValueCount() > 0)
                    {
                        idfFile.WriteFile(outputFilename);
                    }
                    else
                    {
                        log.AddInfo("Empty IDF-file is not written: " + Path.GetFileName(outputFilename), 2);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ToolException("Result IDF-file has not been written successfully (source file has .bak backup file): " + outputFilename, ex);
            }

            if (isSourceBackuped)
            {
                // Result has been successfully written, remove backup
                try
                {
                    File.Delete(inputFilename + ".bak");
                }
                catch (Exception ex)
                {
                    throw new ToolException("Backup of input file could not be deleted: " + inputFilename + ".bak", ex);
                }
            }
        }

        private string CorrectFilename(string colName)
        {
            char[] invalidChars = Path.GetInvalidPathChars();
            for (int charIdx = 0; charIdx < invalidChars.Length; charIdx++)
            {
                char c = invalidChars[charIdx];
                colName = colName.Replace(c.ToString(), string.Empty);
            }
            return colName;
        }
    }
}
