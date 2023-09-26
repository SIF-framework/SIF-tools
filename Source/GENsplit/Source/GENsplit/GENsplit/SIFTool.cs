// GENsplit is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of GENsplit.
// 
// GENsplit is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// GENsplit is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with GENsplit. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.IPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.GENsplit
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
            AddAuthor("Koen Jansen");
            ToolPurpose = "SIF-tool for splitting GEN-files with IPF-files";
        }

        /// <summary>
        /// Starts actual tool process after reading and checking settings
        /// </summary>
        /// <returns>resultcode: 0 for success, 1 for errors</returns>
        protected override int StartProcess()
        {
            int exitcode = 0;
            int logIndentLevel = 0;
            // Retrieve tool settings that have been parsed from the command-line arguments 
            SIFToolSettings settings = (SIFToolSettings) Settings;

            string[] inputFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter, SearchOption.TopDirectoryOnly);
            string outputPath = GetOutputPath(inputFilenames, settings);
            
            // Create output path if not yet existing
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            int fileCount = 0;
            foreach (string inputFilename in inputFilenames)
            {
                logIndentLevel = 1;
                Log.AddInfo("Reading GEN-file " + Path.GetFileName(inputFilename) + " ...", logIndentLevel);
                string outputFileName = null;
                if (inputFilenames.Length == 1 && Path.GetExtension(settings.OutputPath).ToLower().Equals(".GEN"))
                {
                    outputFileName = Path.GetFileName(settings.OutputPath);
                }
                else
                {
                    outputFileName = Path.GetFileNameWithoutExtension(inputFilename) + "_split" + ".GEN";
                }

                GENFile genFile = GENFile.ReadFile(inputFilename, false);
                IPFFile ipfFile = null;
                if (settings.IPFFilename != null)
                {
                    ipfFile = IPFFile.ReadFile(settings.IPFFilename);

                    if (settings.AddedIPFColIdx > 0)
                    {
                        if (settings.AddedIPFColIdx > ipfFile.ColumnCount)
                        {
                            throw new ToolException("Specified IPF-columnindex (" + settings.AddedIPFColIdx + ") is larger than number of columns (" + ipfFile.ColumnCount + ") in IPF-file '" + Path.GetFileName(settings.IPFFilename));
                        }
                    }

                    List<GENLine> genLines = genFile.RetrieveGENLines();
                    int genLineCount = genLines.Count;
                    if (genLineCount != genFile.Features.Count())
                    {
                        Log.AddWarning("GEN-file contains " + (genFile.Features.Count() - genLineCount) + " non-line feature(s) which are skipped");
                        genFile.ClearFeatures();
                        foreach (GENFeature genFeature in genLines)
                        {
                            genFile.AddFeature(genFeature);
                        }
                    }
                    
                    GENFile splitGENFile = Split(genFile, ipfFile, Log, 1, settings.SnapTolerance, (settings.AddedIPFColIdx > 0) ? (settings.AddedIPFColIdx - 1) : -1, false);
                    Log.AddInfo(genFile.Features.Count + " GEN-line(s) have been split into " + splitGENFile.Features.Count + " lines", 2);

                    string fullResultGENFilename = Path.Combine(outputPath, outputFileName);
                    Log.AddInfo("Writing result GEN-file: " + fullResultGENFilename, 1);
                    splitGENFile.WriteFile(fullResultGENFilename);
                }

                if (settings.DatSplitColumnString != null)
                {
                    // Split GEN-file on value(s) in specified DAT-column
                    if (!genFile.HasDATFile())
                    {
                        throw new ToolException("DAT split column number specified, but no DAT-file found for GEN-file '" + Path.GetFileName(genFile.Filename) + "'");
                    }

                    DATFile datFile = genFile.DATFile;
                    int splitColumnIdx = -1;
                    int splitColumnNr;
                    if (!int.TryParse(settings.DatSplitColumnString, out splitColumnNr))
                    {
                        splitColumnIdx = datFile.FindColumnName(settings.DatSplitColumnString, true, false);
                        if (splitColumnIdx < 0)
                        {
                            throw new ToolException("Could not parse split columnstring, specify an integer value or column name: " + settings.DatSplitColumnString);
                        }
                        splitColumnNr = splitColumnIdx + 1;
                    }
                    else
                    {
                        splitColumnIdx = splitColumnNr - 1;
                    }

                    if (splitColumnIdx >= datFile.ColumnNames.Count)
                    {
                        throw new ToolException("DAT-file does not contain specified column number (" + (splitColumnIdx + 1) + "): " + Path.GetFileName(genFile.Filename));
                    }

                    List<GENFile> targetGENFiles = new List<GENFile>();
                    List<string> splitValues = new List<string>();

                    for (int featureIdx = 0; featureIdx < genFile.Features.Count; featureIdx++)
                    {
                        GENFeature genFeature = genFile.Features[featureIdx];
                        DATRow datRow = datFile.GetRow(genFeature.ID);
                        if (datRow != null)
                        {
                            string splitValue = datRow[splitColumnIdx];
                            GENFile targetGENFile = null;
                            if (splitValues.Contains(splitValue))
                            {
                                int targetIPFIdx = splitValues.IndexOf(splitValue);
                                targetGENFile = targetGENFiles[targetIPFIdx];
                            }
                            else
                            {
                                splitValues.Add(splitValue);
                                targetGENFile = new GENFile();
                                targetGENFile.Filename = Path.Combine(outputPath,
                                    Path.GetFileNameWithoutExtension(genFile.Filename) + "_" + settings.SplitValuePrefix + splitValue.Trim().Replace(" ", "").Replace("\"", "").Replace(";", "-")
                                    + Path.GetExtension(genFile.Filename));

                                targetGENFile.DATFile = new DATFile(targetGENFile);
                                targetGENFile.DATFile.ColumnNames = datFile.ColumnNames.ToList();

                                targetGENFiles.Add(targetGENFile);
                            }
                            GENFeature copiedGENFeature = genFeature.Copy();
                            copiedGENFeature.GENFile = genFile;
                            targetGENFile.AddFeature(copiedGENFeature);
                        }
                        else
                        {
                            Log.AddWarning("DAT-entry not found for GEN feature with ID '" + genFeature.ID + "'", 2);
                        }
                    }

                    foreach (GENFile targetGENFile in targetGENFiles)
                    {
                        string targetGENFilename = targetGENFile.Filename;
                        if (File.Exists(targetGENFilename) && settings.SkipOverwrite)
                        {
                            Log.AddWarning("Targetfile is existing, splitting is skipped for " + Path.GetFileName(targetGENFilename), 1);
                        }
                        else
                        {
                            Log.AddInfo("Writing resulting GEN-file: " + Path.GetFileName(targetGENFilename) + " ...", 1);
                            targetGENFile.WriteFile(targetGENFilename);
                        }
                    }
                }

                fileCount++;
            }

            ToolSuccessMessage = "Finished processing " + fileCount + " file(s)";

            return exitcode;
        }

        protected virtual string GetOutputPath(string[] inputFilenames, SIFToolSettings settings)
        {
            string outputPath = settings.OutputPath;

            if (Path.GetExtension(settings.OutputPath).ToLower().Equals(".gen"))
            {
                if (inputFilenames.Length == 1)
                {
                    outputPath = Path.GetDirectoryName(outputPath);
                }
                else
                {
                    throw new ToolException("Output GEN-filename cannot be specified since input filter (" + settings.InputFilter + ") results in more than one filenames");
                }
            }
            return outputPath;
        }

        private GENFile Split(GENFile genFile, IPFFile ipfFile, Log log, int logIndentLevel = 0, float snapTolerance = 100, int addedIPFColIdx = -1, bool isNewDATFileCreated = true)
        {
            // Calculate number of points between 5% logmessages, use multiple of 50
            int logSnapPointMessageFrequency = Log.GetLogMessageFrequency(ipfFile.Points.Count, 5);

            // Find maximum ID value
            
            int currentIDValue = GENFeatureExtension.GetMaxFeatureID(genFile.Features);
            currentIDValue++;

            GENFile splitGENFile = genFile.CopyGEN(); // Note: genFile contains only GENLine objects
            bool hasDATFile = genFile.HasDATFile();
            int lineVal1ColIdx = -1;
            int lineVal2ColIdx = -1;
            if (isNewDATFileCreated || ((addedIPFColIdx >= 0) && !splitGENFile.HasDATFile()))
            {
                splitGENFile.DATFile = new DATFile(splitGENFile);
                splitGENFile.DATFile.AddColumn(DATFile.IDColumnName);
                for (int genFeatureIdx = 0; genFeatureIdx < genFile.Features.Count; genFeatureIdx++)
                {
                    splitGENFile.DATFile.AddRow(new DATRow(new List<string>() { genFile.Features[genFeatureIdx].ID }));
                }
            }
            if (addedIPFColIdx >= 0)
            {
                lineVal1ColIdx = splitGENFile.DATFile.AddColumn("LineVal1", "-9999");
                lineVal2ColIdx = splitGENFile.DATFile.AddColumn("LineVal2", "-9999");
            }
            SnapSettings snapSettings = new SnapSettings(snapTolerance, false);

            for (int ipfPointIdx = 0; ipfPointIdx < ipfFile.Points.Count; ipfPointIdx++)
            {
                if (ipfPointIdx % logSnapPointMessageFrequency == 0)
                {
                    Log.AddInfo("Splitting GEN-file for IPF-points " + (ipfPointIdx + 1) + "-" + (int)Math.Min(ipfFile.Points.Count, (ipfPointIdx + logSnapPointMessageFrequency)) + " of " + ipfFile.Points.Count + " ...", logIndentLevel);
                }

                IPFPoint ipfPoint = ipfFile.Points[ipfPointIdx];
                GENPoint genIPFPoint = new GENPoint(null, "999", ipfPoint);

                GENFeature snappedGENIPFPoint = genIPFPoint.Snap(splitGENFile, snapSettings);
                Point snappedPoint = (snappedGENIPFPoint != null) ? ((GENPoint)snappedGENIPFPoint).Point : null;
                if (snappedPoint != null)
                {
                    GENLine genLine = (GENLine)splitGENFile.FindNearestFeature(new GENPoint(null, ipfPointIdx.ToString(), snappedPoint), snapTolerance);

                    if (genLine.Points[0].Equals(snappedPoint))
                    {
                        // no need for splitting, assign optional IPF-column value to lineValue1 column
                        if (addedIPFColIdx >= 0)
                        {
                            genLine.SetColumnValue("LineVal1", ipfPoint.ColumnValues[addedIPFColIdx]);
                            List<GENFeature> selFeatures = SelectFeatures(splitGENFile, genLine.Points[0], true); // genFile.SelectFeatures(genLine.Points[0]);
                            if (selFeatures.Count > 1)
                            {
                                for (int selFeatureIdx = 0; selFeatureIdx < selFeatures.Count; selFeatureIdx++)
                                {
                                    GENFeature selFeature = selFeatures[selFeatureIdx];
                                    if (!selFeature.ID.Equals(genLine.ID))
                                    {
                                        if (selFeature.Points[0].Equals(snappedPoint))
                                        {
                                            selFeature.SetColumnValue("LineVal2", ipfPoint.ColumnValues[addedIPFColIdx]);
                                        }
                                        else
                                        {
                                            selFeature.SetColumnValue("LineVal2", ipfPoint.ColumnValues[addedIPFColIdx]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else if (genLine.Points[genLine.Points.Count - 1].Equals(snappedPoint))
                    {
                        // no need for splitting, assign optional IPF-column value to lineValue1 column
                        if (addedIPFColIdx >= 0)
                        {
                            genLine.SetColumnValue("LineVal2", ipfPoint.ColumnValues[addedIPFColIdx]);
                            List<GENFeature> selFeatures = SelectFeatures(splitGENFile, genLine.Points[genLine.Points.Count - 1], true); // genFile.SelectFeatures(genLine.Points[0]);
                            if (selFeatures.Count > 1)
                            {
                                for (int selFeatureIdx = 0; selFeatureIdx < selFeatures.Count; selFeatureIdx++)
                                {
                                    GENFeature selFeature = selFeatures[selFeatureIdx];
                                    if (!selFeature.ID.Equals(genLine.ID))
                                    {
                                        if (selFeature.Points[0].Equals(snappedPoint))
                                        {
                                            selFeature.SetColumnValue("LineVal1", ipfPoint.ColumnValues[addedIPFColIdx]);
                                        }
                                        else
                                        {
                                            selFeature.SetColumnValue("LineVal2", ipfPoint.ColumnValues[addedIPFColIdx]);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        // Find indices of the two points that are closest to snappedPoint;
                        int genPoint1Idx = 0;
                        Point genPoint1 = null;
                        Point genPoint2 = genLine.Points[genPoint1Idx];
                        double distance0 = 0;                                       // distance from current GEN-point (genPoint1) to next GEN-point (genPoint2)
                        double distance1 = double.MaxValue;                         // distance from snapped point to current GEN-point (genPoint1)
                        double distance2 = genPoint2.GetDistance(snappedPoint);     // distance from snapped point to next GEN-point (genPoint2)
                        while (((genPoint1Idx + 1) < genLine.Points.Count) && ((distance1 + distance2) > (distance0 + Point.Tolerance)))
                        {
                            genPoint1 = genPoint2;
                            genPoint2 = genLine.Points[genPoint1Idx + 1];

                            distance0 = genPoint2.GetDistance(genPoint1);
                            distance1 = distance2;
                            distance2 = genPoint2.GetDistance(snappedPoint);

                            genPoint1Idx++;
                        }

                        // Now, genPoint1 is first nodle of segment with snappedPoint in it and before snappedPoint; genPoint2 is second node of segment, after snappedPoint.
                        // Check if snappedPoint is inside the segment (unequal to second segmentnode), or that other segments follow within this feature
                        if (((genPoint1Idx + 1) < genLine.Points.Count) || !snappedPoint.Equals(genPoint2))
                        {
                            // splitindices found and other points left at this line, 
                            GENLine genLinePart1 = new GENLine(splitGENFile, currentIDValue++.ToString()); // genLine.ID + "1");
                            GENLine genLinePart2 = new GENLine(splitGENFile, currentIDValue++.ToString()); // genLine.ID + "2");

                            if (addedIPFColIdx >= 0)
                            {
                                DATRow datRow = genLine.GENFile.DATFile.GetRow(genLine.ID);
                                string rowValue1 = null;
                                string rowValue2 = null;
                                if (datRow != null)
                                {
                                    rowValue1 = datRow[lineVal1ColIdx];
                                    rowValue2 = datRow[lineVal2ColIdx];
                                }

                                // genLinePart1.SetColumnValue("LineVal1", ipfColValue);
                                genLinePart1.SetColumnValue("LineVal1", rowValue1);
                                genLinePart1.SetColumnValue("LineVal2", "-9999");
                                genLinePart2.SetColumnValue("LineVal1", "-9999");
                                genLinePart2.SetColumnValue("LineVal2", rowValue2);
                                //genLinePart1.CopyDATRow(genLine);
                                //genLinePart2.CopyDATRow(genLine);
                            }

                            // Split GEN-line and copy points for both parts seperately
                            int genPointIdx = 0;
                            while (genPointIdx < genPoint1Idx)
                            {
                                genLinePart1.AddPoint(genLine.Points[genPointIdx]);
                                genPointIdx++;
                            }
                            if (snappedPoint.Equals(genLine.Points[genPointIdx]))
                            {
                                genLinePart1.AddPoint(genLine.Points[genPointIdx]);
                            }
                            else
                            {
                                genLinePart1.AddPoint(snappedPoint);
                                genLinePart2.AddPoint(snappedPoint);
                            }

                            while (genPointIdx < genLine.Points.Count)
                            {
                                genLinePart2.AddPoint(genLine.Points[genPointIdx]);
                                genPointIdx++;
                            }

                            splitGENFile.RemoveFeature(genLine.ID);
                            if (addedIPFColIdx >= 0)
                            {
                                string ipfColValue = ipfPoint.ColumnValues[addedIPFColIdx].Trim();
                                if (ipfColValue.Equals(string.Empty))
                                {
                                    ipfColValue = "-9999";
                                }
                                genLinePart1.SetColumnValue("LineVal2", ipfColValue);
                                genLinePart2.SetColumnValue("LineVal1", ipfColValue);
                            }
                            splitGENFile.AddFeature(genLinePart1);
                            splitGENFile.AddFeature(genLinePart2);
                        }
                    }
                }
            }

            return splitGENFile;
        }

        private static List<GENFeature> SelectFeatures(GENFile genFile, Point point, bool checkEndPointsOnly = false)
        {
            List<GENFeature> selFeatures = new List<GENFeature>();

            for (int featureIdx = 0; featureIdx < genFile.Features.Count; featureIdx++)
            {
                GENFeature feature = genFile.Features[featureIdx];
                Extent featureExtent = feature.RetrieveExtent();
                if (featureExtent.Contains((float)point.X, (float)point.Y))
                {
                    if (checkEndPointsOnly)
                    {
                        if (point.Equals(feature.Points[0]) || point.Equals(feature.Points[feature.Points.Count - 1]))
                        {
                            selFeatures.Add(feature);
                        }
                    }
                    else
                    {
                        for (int pointIdx = 0; pointIdx < feature.Points.Count; pointIdx++)
                        {
                            Point otherPoint = feature.Points[pointIdx];
                            if (point.Equals(otherPoint))
                            {
                                selFeatures.Add(feature);
                                continue;
                            }
                        }
                    }
                }
            }

            return selFeatures;
        }
    }
}
