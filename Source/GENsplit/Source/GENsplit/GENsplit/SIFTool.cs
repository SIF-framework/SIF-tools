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

            IPFFile ipfFile = null;
            if (settings.IPFFilename != null)
            {
                Log.AddInfo("Reading IPF-file " + Path.GetFileName(settings.IPFFilename) + " ...", logIndentLevel);
                ipfFile = IPFFile.ReadFile(settings.IPFFilename);

                if (settings.AddedIPFColNr > 0)
                {
                    if (settings.AddedIPFColNr > ipfFile.ColumnCount)
                    {
                        throw new ToolException("Specified IPF-column number (" + (settings.AddedIPFColNr + 1) + ") is larger than number of columns (" + ipfFile.ColumnCount + ") in IPF-file '" + Path.GetFileName(settings.IPFFilename));
                    }
                }
            }

            Log.AddInfo("Processing " + inputFilenames.Length + " GEN-file(s) ...");

            int fileCount = 0;
            foreach (string inputFilename in inputFilenames)
            {
                logIndentLevel = 1;
                string outputFileName = null;
                if (inputFilenames.Length == 1 && Path.GetExtension(settings.OutputPath).ToLower().Equals(".gen"))
                {
                    outputFileName = Path.GetFileName(settings.OutputPath);
                }
                else
                {
                    outputFileName = Path.GetFileNameWithoutExtension(inputFilename) + "_split" + ".GEN";
                }

                Log.AddInfo("Reading GEN-file " + Path.GetFileName(inputFilename) + " ...", logIndentLevel);
                GENFile genFile = GENFile.ReadFile(inputFilename, false);

                if (ipfFile != null)
                {
                    // Start splitting GEN-lines
                    GENFile splitGENFile = Split(genFile, ipfFile, settings.MaxSnapDistance, settings.MinSplitDistance, Log, 1, (settings.AddedIPFColNr > 0) ? (settings.AddedIPFColNr - 1) : -1, false);
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

        /// <summary>
        /// Split GEN-lines in specified GEN-file
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="ipfFile"></param>
        /// <param name="maxSnapDistance"></param>
        /// <param name="minSplitDistance"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <param name="addedIPFColIdx"></param>
        /// <param name="isNewDATFileCreated"></param>
        /// <returns></returns>
        private GENFile Split(GENFile genFile, IPFFile ipfFile, float maxSnapDistance, float minSplitDistance, Log log, int logIndentLevel = 0, int addedIPFColIdx = -1, bool isNewDATFileCreated = true)
        {
            bool hasDATFile = genFile.HasDATFile();

            // Select GENLine features from GEN-file and warn for other features
            List<GENLine> genLines = genFile.RetrieveGENLines();
            int genLineCount = genLines.Count;
            if (genLineCount != genFile.Features.Count())
            {
                Log.AddWarning("GEN-file contains " + (genFile.Features.Count() - genLineCount) + " non-line feature(s) which are skipped");
                DATFile datFile = genFile.DATFile;
                GENFile lineGENFile = new GENFile();
                hasDATFile = genFile.HasDATFile();
                if (hasDATFile)
                {
                    lineGENFile.AddDATFile();
                    lineGENFile.DATFile.AddColumns(genFile.DATFile.ColumnNames);
                }
                foreach (GENFeature genFeature in genLines)
                {
                    lineGENFile.AddFeature(genFeature);
                    if (hasDATFile)
                    {
                        DATRow datrow = datFile.GetRow(genFeature.ID);
                        if (datrow != null)
                        {
                            genFile.DATFile.AddRow(datrow);
                        }
                    }
                }
                genFile = lineGENFile;
            }

            // Calculate number of points between 5% logmessages, use multiple of 50
            int logSnapPointMessageFrequency = Log.GetLogMessageFrequency(ipfFile.Points.Count, 5);

            // Find maximum ID value
            int currentIDValue = GENFeatureExtension.GetMaxFeatureID(genFile.Features);
            currentIDValue++;

            GENFile splitGENFile = genFile.CopyGEN(); // Note: genFile contains only GENLine objects
            int lineVal1ColIdx = -1;
            int lineVal2ColIdx = -1;
            if (isNewDATFileCreated || ((addedIPFColIdx >= 0) && !hasDATFile))
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
                lineVal1ColIdx = splitGENFile.DATFile.AddColumn("LineVal1", SIFToolSettings.NoDataValueString);
                lineVal2ColIdx = splitGENFile.DATFile.AddColumn("LineVal2", SIFToolSettings.NoDataValueString);
            }

            // Loop through all IPF-points and:
            // 1. snap each IPF-point to closest GEN-line; if no line is found, ignore point
            // 2. check if (snapped) point is at endpoint of line and splitting is not necessary; just 
            for (int ipfPointIdx = 0; ipfPointIdx < ipfFile.Points.Count; ipfPointIdx++)
            {
                if (ipfPointIdx % logSnapPointMessageFrequency == 0)
                {
                    Log.AddInfo("Splitting GEN-file for IPF-points " + (ipfPointIdx + 1) + "-" + (int)Math.Min(ipfFile.Points.Count, (ipfPointIdx + logSnapPointMessageFrequency)) + " of " + ipfFile.Points.Count + " ...", logIndentLevel);
                }

                IPFPoint ipfPoint = ipfFile.Points[ipfPointIdx];

                List<GENFeature> nearestFeatures = ipfPoint.FindNearestSegmentFeatures(splitGENFile.Features, maxSnapDistance, out List<LineSegment> nearestSegments);
                for (int nearestFeatureIdx = 0; nearestFeatureIdx < nearestFeatures.Count; nearestFeatureIdx++)
                {
                    // One or more features were found that have a segment within specified distance (snapTolerance), now snap point to this segment (perdicular or to closest endpoint)
                    // Since only GEN-lines should have been selected, the feature can safely be cast to a GENLine object
                    GENLine nearestGENLine = (GENLine)nearestFeatures[nearestFeatureIdx];
                    LineSegment nearestSegment = nearestSegments[nearestFeatureIdx];

                    Point snappedPoint = ipfPoint.SnapToLineSegment(nearestSegment.P1, nearestSegment.P2);
                    if (snappedPoint != null)
                    {
                        double snapDistance = snappedPoint.GetDistance(ipfPoint);

                        // If the snapped point equals the start- or endpoint of the GEN-line, there is no need for splitting, but a value could be assigned 
                        if (nearestGENLine.Points[0].Equals(snappedPoint))
                        {
                            // no need for splitting, assign optional IPF-column value to lineValue1 column
                            if (addedIPFColIdx >= 0)
                            {
                                nearestGENLine.SetColumnValue("LineVal1", ipfPoint.ColumnValues[addedIPFColIdx]);
                            }
                        }
                        else if (nearestGENLine.Points[nearestGENLine.Points.Count - 1].Equals(snappedPoint))
                        {
                            // no need for splitting, assign optional IPF-column value to lineValue2 column
                            if (addedIPFColIdx >= 0)
                            {
                                nearestGENLine.SetColumnValue("LineVal2", ipfPoint.ColumnValues[addedIPFColIdx]);
                            }
                        }
                        else
                        {
                            Point genPoint1 = nearestSegment.P1;
                            int genPoint1Idx = nearestGENLine.IndexOf(genPoint1);
                            Point genPoint2 = nearestGENLine.Points[genPoint1Idx + 1];
                            double distance1 = genPoint1.GetDistance(snappedPoint);
                            double distance2 = genPoint2.GetDistance(snappedPoint);

                            if (distance1 <= minSplitDistance)
                            {
                                // Snapped point is close to existing genPoint1, no need for an extra point
                                snappedPoint = genPoint1;
                                distance1 = 0;
                            }
                            else if (distance2 <= minSplitDistance)
                            {
                                // Snapped point is close to existing genPoint2, no need for an extra point
                                snappedPoint = genPoint2;
                                distance2 = 0;
                            }

                            // Now, genPoint1 is first node of segment with snappedPoint in it and before or equal to snappedPoint; genPoint2 is second node of segment, equal to or after snappedPoint.
                            if (snapDistance <= maxSnapDistance)
                            {
                                // splitindices found and other points left at this line, 
                                GENLine genLinePart1 = new GENLine(splitGENFile, currentIDValue++.ToString()); // genLine.ID + "1");
                                GENLine genLinePart2 = new GENLine(splitGENFile, currentIDValue++.ToString()); // genLine.ID + "2");

                                if (addedIPFColIdx >= 0)
                                {
                                    DATRow datRow = splitGENFile.DATFile.GetRow(nearestGENLine.ID);
                                    string rowValue1 = null;
                                    string rowValue2 = null;
                                    if (datRow != null)
                                    {
                                        rowValue1 = datRow[lineVal1ColIdx];
                                        rowValue2 = datRow[lineVal2ColIdx];
                                    }

                                    string ipfColValue = ipfPoint.ColumnValues[addedIPFColIdx].Trim();
                                    if (ipfColValue.Equals(string.Empty))
                                    {
                                        ipfColValue = SIFToolSettings.NoDataValueString;
                                    }

                                    genLinePart1.SetColumnValue("LineVal1", rowValue1);
                                    genLinePart1.SetColumnValue("LineVal2", ipfColValue);
                                    genLinePart2.SetColumnValue("LineVal1", ipfColValue);
                                    genLinePart2.SetColumnValue("LineVal2", rowValue2);
                                }

                                // Split GEN-line and copy points for both parts seperately
                                int genPointIdx = 0;
                                while (genPointIdx <= genPoint1Idx)
                                {
                                    genLinePart1.AddPoint(nearestGENLine.Points[genPointIdx]);
                                    genPointIdx++;
                                }

                                if (distance1 > 0)
                                {
                                    if (distance2 > 0)
                                    {
                                        genLinePart1.AddPoint(snappedPoint);
                                        genLinePart2.AddPoint(snappedPoint);
                                    }
                                    else
                                    {
                                        genLinePart1.AddPoint(genPoint2);
                                    }
                                }
                                else
                                {
                                    genLinePart2.AddPoint(genPoint1);
                                }

                                while (genPointIdx < nearestGENLine.Points.Count)
                                {
                                    genLinePart2.AddPoint(nearestGENLine.Points[genPointIdx]);
                                    genPointIdx++;
                                }

                                splitGENFile.RemoveFeature(nearestGENLine.ID);
                                if (genLinePart1.Points.Count > 1)
                                {
                                    splitGENFile.AddFeature(genLinePart1);
                                }
                                else
                                {
                                    if (!distance1.Equals(0))
                                    {
                                        // todo
                                        log.AddWarning("Unexpected skipped linepart1 with distance1: " + distance1.ToString(EnglishCultureInfo) + "; check tool implementation and increase SIFToolSettings.DistanceTolerance (current value: " + SIFToolSettings.DistanceTolerance.ToString(EnglishCultureInfo) + "). Snapped point coordinates: " + snappedPoint.ToString());
                                    }
                                }
                                if (genLinePart2.Points.Count > 1)
                                {
                                    splitGENFile.AddFeature(genLinePart2);
                                }
                                else
                                {
                                    if (!distance2.Equals(0))
                                    {
                                        // todo
                                        log.AddWarning("Unexpected skipped linepart2 with distance2: " + distance2.ToString(EnglishCultureInfo) + "; check tool implementation and increase SIFToolSettings.DistanceTolerance (current value: " + SIFToolSettings.DistanceTolerance.ToString(EnglishCultureInfo) + "). Snapped point coordinates: " + snappedPoint.ToString());
                                    }
                                }
                            }
                            else
                            {
                                log.AddWarning("Snapped distance too large, which should never happen. Distance error: " + snapDistance.ToString(EnglishCultureInfo) + "; check tool implementation and increase SIFToolSettings.DistanceTolerance (current value: " + SIFToolSettings.DistanceTolerance.ToString(EnglishCultureInfo) + "). Snapped point coordinates: " + snappedPoint.ToString());
                            }
                        }
                    }
                    else
                    {
                        log.AddWarning("No snap point found, which should never happen; check tool implementation and increase SIFToolSettings.DistanceTolerance (current value: " + SIFToolSettings.DistanceTolerance.ToString(EnglishCultureInfo) + "). Source point coordinates: " + ipfPoint.ToString());
                    }
                }
            }

            return splitGENFile;
        }

        //private static List<GENFeature> SelectFeatures(GENFile genFile, Point point, bool checkEndPointsOnly = false)
        //{
        //    List<GENFeature> selFeatures = new List<GENFeature>();

        //    for (int featureIdx = 0; featureIdx < genFile.Features.Count; featureIdx++)
        //    {
        //        GENFeature feature = genFile.Features[featureIdx];
        //        Extent featureExtent = feature.RetrieveExtent();
        //        if (featureExtent.Contains((float)point.X, (float)point.Y))
        //        {
        //            if (checkEndPointsOnly)
        //            {
        //                if (point.Equals(feature.Points[0]) || point.Equals(feature.Points[feature.Points.Count - 1]))
        //                {
        //                    selFeatures.Add(feature);
        //                }
        //            }
        //            else
        //            {
        //                for (int pointIdx = 0; pointIdx < feature.Points.Count; pointIdx++)
        //                {
        //                    Point otherPoint = feature.Points[pointIdx];
        //                    if (point.Equals(otherPoint))
        //                    {
        //                        selFeatures.Add(feature);
        //                        continue;
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    return selFeatures;
        //}
    }
}
