// IDFresample is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFresample.
// 
// IDFresample is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFresample is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFresample. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.IDF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IDFresample
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
            Authors = new string[] { "Koen van der Hauw" ,"Sruthi Sathyadevan" };
            ToolPurpose = "SIF-tool for resampling values in IDF-file with nearest neighbor method";
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

            // Read zone IDF-file if specified
            IDFFile zoneIDFFile = null;
            List <IDFFile> zoneIDFFiles = new List<IDFFile>();
            if (settings.ZoneFilename != null)
            {
                Log.AddInfo("Reading zone IDF-file ...");
                zoneIDFFile = IDFFile.ReadFile(settings.ZoneFilename);
                Log.AddInfo("splitting zone IDF-file ...", 1);
                List<float> uniqueIDFValues = zoneIDFFile.RetrieveUniqueValues();
                if (uniqueIDFValues.Count > 0)
                {
                    foreach (float uniqueValue in uniqueIDFValues)
                    {
                        IDFFile newIDFFile = zoneIDFFile.CopyIDF(uniqueValue.ToString(EnglishCultureInfo));
                        newIDFFile.ResetValues();
                        newIDFFile.ReplaceValues(zoneIDFFile, uniqueValue, 1f);
                        zoneIDFFiles.Add(newIDFFile);
                    }
                }
                else
                {
                    zoneIDFFile = null;
                }
            }
            else
            {
                // Use full extent of each value IDF-file
                zoneIDFFile = null;
            }


            Log.AddInfo("Processing input value files ...");
            int fileCount = 0;
            string[] valueFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter);
            foreach (string valueFilename in valueFilenames)
            {
                Log.AddInfo("reading file " + Path.GetFileName(valueFilename) + " ...", 1);
                IDFFile valueIDFFile = IDFFile.ReadFile(valueFilename);

                if (zoneIDFFile == null)
                {
                    IDFFile tmpZoneIDFFile = valueIDFFile.CopyIDF("dummyZone.IDF");
                    tmpZoneIDFFile.SetValues(1);
                    zoneIDFFiles.Clear();
                    zoneIDFFiles.Add(tmpZoneIDFFile);
                }

                if (!valueIDFFile.XCellsize.Equals(zoneIDFFiles[0].XCellsize) && !valueIDFFile.YCellsize.Equals(zoneIDFFiles[0].YCellsize))
                {
                    throw new ToolException("Cellsizes are different for zone IDF-file (" + zoneIDFFiles[0].XCellsize.ToString(EnglishCultureInfo) + "x" + zoneIDFFiles[0].YCellsize.ToString(EnglishCultureInfo) 
                        + ") and value IDF-file (" + zoneIDFFiles[0].XCellsize.ToString(EnglishCultureInfo) + "x" + zoneIDFFiles[0].YCellsize.ToString(EnglishCultureInfo) + ")");
                }

                if (!valueIDFFile.Extent.Equals(zoneIDFFiles[0].Extent))
                {
                    if (!zoneIDFFiles[0].Extent.Intersects(valueIDFFile.Extent))
                    {
                        throw new ToolException("No overlap between extents of zone IDF-file " + zoneIDFFiles[0].Extent.ToString() + " and value IDF-file " + valueIDFFile.Extent.ToString());
                    }

                    // Ensure value IDF-file has same extent as zone IDF-file
                    valueIDFFile = valueIDFFile.ClipIDF(zoneIDFFiles[0].Extent);
                    valueIDFFile = valueIDFFile.EnlargeIDF(zoneIDFFiles[0].Extent);
                }
                ProcessValueFile(valueIDFFile, zoneIDFFiles, settings, Log);
                fileCount++;
            }

            ToolSuccessMessage = "Finished processing " + fileCount + " file(s)";

            return exitcode;
        }

        private void ProcessValueFile(IDFFile valueIDFFile, List<IDFFile> zoneIDFFiles, SIFToolSettings settings, Log log)
        {
            string resultFilename = Path.Combine(settings.OutputPath, (settings.OutputFilename != null) ? settings.OutputFilename : FileUtils.AddFilePostFix(Path.GetFileName(valueIDFFile.Filename), "_resampled"));
            IDFFile resultIDFFile = valueIDFFile.CopyIDF(resultFilename, false);
            resultIDFFile.ResetValues();

            for (int idfFileIdx = 0; idfFileIdx < zoneIDFFiles.Count; idfFileIdx++)
            {
                IDFFile zoneIDFFile = zoneIDFFiles[idfFileIdx];
                Log.AddInfo("processing zone value " + Path.GetFileName(zoneIDFFile.Filename) + " ...", 2);

                ResampleValueFile(valueIDFFile, zoneIDFFile, resultIDFFile, settings, Log);
            }

            Log.AddInfo("writing resampled value IDF-file " + Path.GetFileName(resultIDFFile.Filename), 1);
            resultIDFFile.WriteFile();
        }

        protected virtual void ResampleValueFile(IDFFile valueIDFFile, IDFFile zoneIDFFile, IDFFile resultIDFFile, SIFToolSettings settings, Log log)
        {
            // Use a grow approach to implment nearest neighbor resampling
            // - Copy known values from valuegrid inside current zone
            // - For neighboring cells of all currently known values, copy value from known cells. Use conflict method when surrounded by more than one known cell.
            IDFFile zoneResultIDFFile = resultIDFFile.CopyIDF("tmpresult" + Path.GetFileName(zoneIDFFile.Filename));
            zoneResultIDFFile.ResetValues();
            zoneResultIDFFile.ReplaceValues(zoneIDFFile, valueIDFFile);

            int maxRowIdxZoneIDFFile = zoneIDFFile.NRows - 1;
            int maxColIdxZoneIDFFile = zoneIDFFile.NCols - 1;
            float zoneNoDataValue = zoneIDFFile.NoDataValue;
            float resultNoDataValue = zoneResultIDFFile.NoDataValue;
            long zoneCellCount = zoneIDFFile.RetrieveElementCount();
            bool isFinished = false;
            long processedCellCount = 0;
            do
            {
                log.AddInfo("Assigned " + processedCellCount + " of " + zoneCellCount + " cells, " + (((float) processedCellCount / (float) zoneCellCount) * 100.0).ToString("0.00", EnglishCultureInfo) + "%", 3);

                isFinished = true;
                IDFFile newResultIDFFile = zoneResultIDFFile.CopyIDF(zoneResultIDFFile.Filename);
                processedCellCount = 0;
                for (int rowIdx = 0; rowIdx < zoneIDFFile.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < zoneIDFFile.NCols; colIdx++)
                    {
                        float zoneValue = zoneIDFFile.values[rowIdx][colIdx];
                        float resultValue = zoneResultIDFFile.values[rowIdx][colIdx];
                        if (!zoneValue.Equals(zoneNoDataValue))
                        {
                            // When cell in current resultIDFFile still has a NoData-value, try to resample.
                            if (resultValue.Equals(resultNoDataValue))
                            {
                                // Loop through neighboring cells
                                float valueSum = 0;
                                float maxValue = float.MinValue;
                                float minValue = float.MaxValue;
                                float valueCount = 0;
                                bool isUndefinedHarmonicAverage = false;
                                int minRowIdx = (rowIdx > 0) ? rowIdx - 1 : 0;
                                int maxRowIdx = (rowIdx < maxRowIdxZoneIDFFile) ? rowIdx + 1 : maxRowIdxZoneIDFFile;
                                int minColIdx = (colIdx > 0) ? colIdx - 1 : 0;
                                int maxColIdx = (colIdx < maxColIdxZoneIDFFile) ? colIdx + 1 : maxColIdxZoneIDFFile;
                                for (int subRowIdx = minRowIdx; subRowIdx <= maxRowIdx; subRowIdx++)
                                {
                                    for (int subColIdx = minColIdx; subColIdx <= maxColIdx; subColIdx++)
                                    {
                                        float neighborValue = zoneResultIDFFile.values[subRowIdx][subColIdx];
                                        if (!neighborValue.Equals(resultNoDataValue))
                                        {
                                            switch (settings.ConflictMethod)
                                            {
                                                case ConflictMethod.ArithmeticAverage:
                                                    valueSum += neighborValue;
                                                    valueCount++;
                                                    break;
                                                case ConflictMethod.HarmonicAverage:
                                                    if (!neighborValue.Equals(0))
                                                    {
                                                        valueSum += (1 / neighborValue);
                                                        valueCount++;
                                                    }
                                                    else
                                                    {
                                                        valueCount++;
                                                        isUndefinedHarmonicAverage = true;
                                                    }
                                                    break;
                                                case ConflictMethod.MinimumValue:
                                                    if (neighborValue < minValue)
                                                    {
                                                        minValue = neighborValue;
                                                        valueCount = 1;
                                                    }
                                                    break;
                                                case ConflictMethod.MaximumValue:
                                                    if (neighborValue > maxValue)
                                                    {
                                                        maxValue = neighborValue;
                                                        valueCount = 1;
                                                    }
                                                    break;
                                                default:
                                                    throw new Exception("Unknown conflict method: " + settings.ConflictMethod.ToString());
                                            }
                                        }
                                    }
                                }

                                if (valueCount > 0)
                                {
                                    switch (settings.ConflictMethod)
                                    {
                                        case ConflictMethod.ArithmeticAverage:
                                            newResultIDFFile.values[rowIdx][colIdx] = valueSum / (float)valueCount;
                                            break;
                                        case ConflictMethod.HarmonicAverage:
                                            if (isUndefinedHarmonicAverage)
                                            {
                                                newResultIDFFile.values[rowIdx][colIdx] = 0;
                                            }
                                            else
                                            {
                                                newResultIDFFile.values[rowIdx][colIdx] = ((float)valueCount) / valueSum;
                                            }
                                            break;
                                        case ConflictMethod.MinimumValue:
                                            newResultIDFFile.values[rowIdx][colIdx] = minValue;
                                            break;
                                        case ConflictMethod.MaximumValue:
                                            newResultIDFFile.values[rowIdx][colIdx] = maxValue;
                                            break;
                                        default:
                                            throw new Exception("Unknown conflict method: " + settings.ConflictMethod.ToString());
                                    }

                                    isFinished = false;
                                    processedCellCount++;
                                }
                            }
                            else
                            {
                                processedCellCount++;
                            }
                        }
                    }
                }

                zoneResultIDFFile = newResultIDFFile;
            } while (!isFinished);

            // Merge zone results with total results IDF-file
            resultIDFFile.ReplaceValues(zoneResultIDFFile, zoneResultIDFFile);

            if (settings.isSplitByZoneValues && (settings.ZoneFilename != null))
            {
                string zoneResultFilename = FileUtils.AddFilePostFix(resultIDFFile.Filename, "_zone" + Path.GetFileName(zoneIDFFile.Filename));
                Log.AddInfo("Writing result IDF-file for zone " + Path.GetFileName(zoneIDFFile.Filename) + ": " + Path.GetFileName(zoneResultFilename), 3);
                zoneResultIDFFile.WriteFile(zoneResultFilename);
            }
        }
    }
}
