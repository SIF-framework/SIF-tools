// IDFGENconvert is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFGENconvert.
// 
// IDFGENconvert is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFGENconvert is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFGENconvert. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.GIS.Clipping;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMOD.Values;

namespace Sweco.SIF.IDFGENconvert
{
    /// <summary>
    /// Class for conversion of GEN-files to IDF-files
    /// </summary>
    public class GENIDFConverter
    {
        protected const double ERRORMARGIN = 0.05;

        protected SIFToolSettings settings;
        protected Log log;

//        IDFFile windowIDFFile;

        // Variables used during conversion
        protected IDFFile valueIDFFile;
        protected IDFFile lengthIDFFile;
        protected IDFFile angleIDFFile;
        protected IDFFile areaIDFFile;
        protected IDFFile polygonAreaIDFFile;
        protected Metadata metadata;
        protected GENPolygon genPolygon;
        protected string genPolygonID;
        protected int genFeatureIdx;
        protected int logIndentLevel;
        protected GENPolygon clockwiseGENPolygon;
        protected double[] polygonXArray;
        protected double[] polygonYArray;
        protected float genValue;
        protected float genValue2;
        protected float halfCellsizeX;
        protected float halfCellsizeY;
        protected float idfCellArea;
        protected float polygonArea;
        protected bool isIsland = false;
        private IPFFile warningIPFFile;
        bool isNegativeAreaWarningShown;
        bool isInvalidGridPar2WarningShown;
        bool isCellCoverageWarningShown;

        public GENIDFConverter(SIFToolSettings settings, Log log)
        {
            this.settings = settings;
            this.log = log;
            genPolygon = null;
            clockwiseGENPolygon = null;
            polygonXArray = null;
            polygonYArray = null;
            genValue = float.NaN;
            genValue2 = float.NaN;
            idfCellArea = float.NaN;

            isNegativeAreaWarningShown = false;
            isInvalidGridPar2WarningShown = false;
            isCellCoverageWarningShown = false;
            warningIPFFile = new IPFFile();
            warningIPFFile.Filename = Path.Combine(settings.OutputPath, "GENIDFConverterWarnings.IPF");
            warningIPFFile.AddXYColumns();
            warningIPFFile.AddColumn("ErrorValue");
            warningIPFFile.AddColumn("Remark");
        }

        public virtual bool Convert(string inputFilename, string outputPath, string outputFilename, int logIndentLevel)
        {
            // Read input GEN-file
            log.AddInfo("Reading GEN-file '" + Path.GetFileName(inputFilename) + "' ...", logIndentLevel);
            GENFile genFile = GENFile.ReadFile(inputFilename);
            if (genFile.Count == 0)
            {
                log.AddWarning("GEN-file does not contain any features and is skipped: " + Path.GetFileName(inputFilename), 1);
                return false;
            }

            // Calculate number of points between 5% logmessages, use multiple of 50
            int logSnapPointMessageFrequency = Log.GetLogMessageFrequency(genFile.Count, 5);

            DoFileInitialization(genFile);

            if (settings.IsGENOrdered)
            {
                // For proper conversion of islands it is essential that polygons are sorted from larger to small area, keeping islands (with negative area) directly after corresponding ring
                log.AddInfo("Sorting GEN-features ...", 1);
                SortFeatures(genFile);
            }

            for (int genFeatureIdx = 0; genFeatureIdx < genFile.Features.Count; genFeatureIdx++)
            {
                if (genFeatureIdx % logSnapPointMessageFrequency == 0)
                {
                    log.AddInfo("Processing GEN-features " + (genFeatureIdx + 1) + "-" + (int)Math.Min(genFile.Count, (genFeatureIdx + logSnapPointMessageFrequency)) + " of " + genFile.Count + " ...", logIndentLevel);
                }

                GENFeature genFeature = genFile.Features[genFeatureIdx];

                // Retrieve value to use for cells inside this GEN-feature
                RetrieveGENValue(genFeature, genFeatureIdx, logIndentLevel);

                if (genFeature is GENPolygon)
                {
                    GENPolygonToIDF((GENPolygon)genFeature, genFeatureIdx, logIndentLevel + 1);
                }
                else if (genFeature is GENLine)
                {
                    GENLineToIDF((GENLine)genFeature, ref genFeatureIdx, logIndentLevel + 1);
                    CorrectLineInconsistencies();
                }
                else
                {
                    log.AddWarning("Conversion of GEN-feature " + genFeature.GetType().Name + " is currently not supported.", logIndentLevel + 1);
                }

                DoFeaturePostProcessing();
            }

            DoFilePostProcessing();

            WriteResults(outputPath, outputFilename, logIndentLevel);

            return true;
        }

        private void SortFeatures(GENFile genFile)
        {
            List<GENFeature> sortedPolygons = new List<GENFeature>();
            List<GENFeature> otherFeatures = new List<GENFeature>();
            List<double> sortedAreas = new List<double>();
            double prevPositiveMeasure = double.NaN;

            SortedDictionary<double, List<GENFeature>> measureDictionary = new SortedDictionary<double, List<GENFeature>>();

            for (int genFeatureIdx = 0; genFeatureIdx < genFile.Features.Count; genFeatureIdx++)
            {
                GENFeature genFeature = genFile.Features[genFeatureIdx];

                if ((genFeature is GENPolygon) || (genFeature is GENLine))
                {
                    double measure = genFeature.CalculateMeasure();
                    if (measure > 0)
                    {
                        // For GEN-polygons area can be negative for islands, keep order or islands, directly after current previous polygon
                        prevPositiveMeasure = measure;
                        if (!measureDictionary.ContainsKey(measure))
                        {
                            measureDictionary.Add(measure, new List<GENFeature>());
                        }
                        measureDictionary[measure].Add(genFeature);
                    } 
                    else
                    {
                        // Feature is an island of previous feature, add it to list of that feature
                        measureDictionary[prevPositiveMeasure].Add(genFeature);
                    }
                }
                else
                {
                    otherFeatures.Add(genFeature);
                }
            }

            genFile.Features.Clear();
            foreach (double measure in measureDictionary.Keys.Reverse())
            {
                genFile.Features.AddRange(measureDictionary[measure]);
            }
            genFile.Features.AddRange(otherFeatures);
        }

        /// <summary>
        /// Performs initializiation at file level for GEN-IDF conversion (checks and creation of valueIDFFile, lengthIDFFile, angleIDFFile and metadata for valueIDFFile)
        /// </summary>
        /// <param name="genFile"></param>
        protected virtual void DoFileInitialization(GENFile genFile)
        {
            CheckSettings(genFile);

            Extent idfExtent = GetIDFExtent(genFile);
            valueIDFFile = new IDFFile("valueIDFFile.IDF", idfExtent, settings.GridCellsize, -9999.0f);
            valueIDFFile.ResetValues();

            halfCellsizeX = valueIDFFile.XCellsize / 2.0f;
            halfCellsizeY = valueIDFFile.YCellsize / 2.0f;

            // Create length IDF-file to correct some possible inconsistencies
            lengthIDFFile = new IDFFile("lenghtIDFFile.IDF", idfExtent, settings.GridCellsize, -9999.0f);
            lengthIDFFile.ResetValues();

            angleIDFFile = null;
            if (settings.AddAngleIDFFile)
            {
                angleIDFFile = new IDFFile("areaIDFFile.IDF", idfExtent, settings.GridCellsize, -9999.0f);
                angleIDFFile.ResetValues();
            }

            // Calculate default area of polygon in cell (use complete area if not specified otherwise)
            idfCellArea = valueIDFFile.XCellsize * valueIDFFile.YCellsize;

            // Create area IDF-file (even if not requested by user) as it is needed to correct some possible inconsistencies            
            areaIDFFile = new IDFFile("areaIDFFile.IDF", valueIDFFile.Extent, settings.GridCellsize, -9999.0f);
            areaIDFFile.ResetValues();

            // For polygons use a temporary polygon area IDF-file, to be able to handle enclosed polygons correctly
            polygonAreaIDFFile = new IDFFile("polygonAreaIDFFile.IDF", valueIDFFile.Extent, settings.GridCellsize, -9999.0f);
            polygonAreaIDFFile.ResetValues();

            metadata = new Metadata("Converted from GEN-file to IDF-file with " + SIFTool.Instance.ToolName + " " + SIFTool.Instance.ToolVersion);
            metadata.Source = genFile.Filename;
            if (settings.GridPar1String != null)
            {
                if (settings.GridPar2String != null)
                {
                    metadata.ProcessDescription = "Gridded features, polygons: value from (one-based) GEN-column " + settings.GridPar1String + "; lines: lineair interpolation between values in (one-based) GEN-columns " + settings.GridPar1String + " and " + settings.GridPar2String;
                }
                else
                {
                    metadata.ProcessDescription = "Gridded features, polygons/lines: value from (one-based) GEN-column " + settings.GridPar1String;
                }
            }
            else
            {
                metadata.ProcessDescription = "Gridded features, value 1 for cells within polygon or at line";
            }
        }

        /// <summary>
        /// Perform postprocessing when conversion is finished, before writing files
        /// </summary>
        protected virtual void DoFilePostProcessing()
        {
            if (settings.GridPar3 == 6)
            {
                // Option 6 specifies area weighted value for polygons: divide areavalue-sum by areasum
                if (areaIDFFile != null)
                {
                    string filename = valueIDFFile.Filename;
                    valueIDFFile = valueIDFFile / areaIDFFile;
                    valueIDFFile.Filename = filename;
                }
            }
        }

        /// <summary>
        /// Performs initializiation for GEN-IDF conversion of polygons
        /// </summary>
        /// <param name="genPolygon"></param>
        /// <param name="genFeatureIdx"></param>
        /// <param name="logIndentLevel"></param>
        protected virtual void DoPolygonInitialization(GENPolygon genPolygon, int genFeatureIdx, int logIndentLevel)
        {
            this.genPolygon = genPolygon;
            this.genPolygonID = genPolygon.ID;
            this.genFeatureIdx = genFeatureIdx;

            this.logIndentLevel = logIndentLevel;

            // Check if polygon is an island (with negative area)
            isIsland = false;
            clockwiseGENPolygon = (GENPolygon) genPolygon.Copy();
            polygonArea = (float)genPolygon.CalculateArea();
            if (polygonArea < 0)
            {
                if (settings.IsIslandConverted || settings.IsPointOrderIgnored)
                {
                    polygonArea = (float)Math.Abs(polygonArea);
                    if (settings.IsIslandConverted)
                    {
                        isIsland = true;
                    }

                    clockwiseGENPolygon.ReversePoints();
                }
                else
                {
                    log.AddWarning("Points of polygon " + genPolygon.ID + " are defined counterclockwise, indicating an island, and may give unexpected results. Use option /i or /n.", logIndentLevel);
                    return;
                }
            }

            // Convert polygon point list to XY-arrays for point-inside-polygon check
            polygonXArray = new double[clockwiseGENPolygon.Points.Count];
            polygonYArray = new double[clockwiseGENPolygon.Points.Count];
            for (int pointIdx = 0; pointIdx < clockwiseGENPolygon.Points.Count; pointIdx++)
            {
                polygonXArray[pointIdx] = clockwiseGENPolygon.Points[pointIdx].X;
                polygonYArray[pointIdx] = clockwiseGENPolygon.Points[pointIdx].Y;
            }

            //List<Point> convexHullPoints = ConvexHull.RetrieveConvexHull(genPolygon.Points);
            //convexHullPoints.Add(convexHullPoints[0]);
            //GENFile convexHullGENFile = new GENFile();
            //convexHullPolygon = new GENPolygon(convexHullGENFile, 0, convexHullPoints);
            //convexHullGENFile.AddFeature(convexHullPolygon);
        }

        /// <summary>
        /// Perform postprocessing when conversion of feature is finished
        /// </summary>
        protected virtual void DoFeaturePostProcessing()
        {
            // Nothing to do currently
        }

        /// <summary>
        /// Retrieve extent of result IDF-file based of specified GEN-file and settings
        /// </summary>
        /// <param name="genFile"></param>
        /// <returns></returns>
        protected virtual Extent GetIDFExtent(GENFile genFile)
        {
            // Use extent of GEN-file aligned to specified cellsize
            return genFile.Extent.Snap(settings.GridCellsize, true);
        }

        /// <summary>
        /// Convert GEN-polygon to IDF-cell values in valueIDFFile
        /// </summary>
        /// <param name="genPolygon"></param>
        /// <param name="genFeatureIdx"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        protected virtual void GENPolygonToIDF(GENPolygon genPolygon, int genFeatureIdx, int logIndentLevel)
        {
            DoPolygonInitialization(genPolygon, genFeatureIdx, logIndentLevel);

            Extent polygonExtent = genPolygon.RetrieveExtent();
            polygonExtent = polygonExtent.Clip(valueIDFFile.Extent);

            // Retrieve extent indices for cells that contain this polygon
            int minRowIdx = valueIDFFile.GetRowIdx(polygonExtent.ury);
            int minColIdx = valueIDFFile.GetColIdx(polygonExtent.llx);
            int maxRowIdx = valueIDFFile.GetRowIdx(polygonExtent.lly);
            int maxColIdx = valueIDFFile.GetColIdx(polygonExtent.urx);
            if (minRowIdx < 0)
            {
                minRowIdx = 0;
            }
            if (minColIdx < 0)
            {
                minColIdx = 0;
            }

            //            Extent testExtent = new Extent(107000, 416000, 118000, 422000);
            //            if (testExtent.Contains(polygonExtent))
            ////            if ((genPolygon.Points[0].X > 112553f) && (genPolygon.Points[0].X < 112555f))
            //            {
            //                string fname = Path.Combine(settings.OutputPath, "ExtentFeatures.GEN");
            //                GENFile tmpGENFile = null;
            //                if (!File.Exists(fname))
            //                {
            //                    tmpGENFile = new GENFile();
            //                }
            //                else
            //                {
            //                    tmpGENFile = GENFile.ReadFile(fname);
            //                }

            //                tmpGENFile.AddFeature(genPolygon.Copy());
            //                tmpGENFile.WriteFile(fname);
            //            }

            //// For debugging
            //windowIDFFile = valueIDFFile.CopyIDF(string.Empty, false);
            //windowIDFFile.DeclareValuesMemory();
            //windowIDFFile.SetValues(0);

            GENPolygonToIDF(minRowIdx, minColIdx, maxRowIdx, maxColIdx);
        }

        /// <summary>
        /// Convert polygon to IDF-file for specified IDF window
        /// </summary>
        /// <param name="minRowIdx"></param>
        /// <param name="minColIdx"></param>
        /// <param name="maxRowIdx"></param>
        /// <param name="maxColIdx"></param>
        private void GENPolygonToIDF(int minRowIdx, int minColIdx, int maxRowIdx, int maxColIdx)
        {
//            SetZoneValues(windowIDFFile, minRowIdx, minColIdx, maxRowIdx, maxColIdx, 1);

            int minRowColCount = 10;

            // IDFWindow is processed using a divide and conquer algorithm:
            // - If window is small enough, process directly
            // - Divide large window in four smaller windows
            // - Check if smaller windows have overlap with polygon
            // - Process smaller windows that have overlap with polygon

            int rowCount = maxRowIdx - minRowIdx + 1;
            int colCount = maxColIdx - minColIdx + 1;
            if ((rowCount < minRowColCount) || (colCount < minRowColCount))
            {
//                SetZoneValues(windowIDFFile, minRowIdx, minColIdx, maxRowIdx, maxColIdx, 2);

                // Process all cells inside specified window
                for (int rowIdx = minRowIdx; (rowIdx <= maxRowIdx) && (rowIdx < valueIDFFile.NRows); rowIdx++)
                {
                    for (int colIdx = minColIdx; (colIdx <= maxColIdx) && (colIdx < valueIDFFile.NCols); colIdx++)
                    {
                        float x = valueIDFFile.GetX(colIdx);
                        float y = valueIDFFile.GetY(rowIdx);

                        try
                        {
                            if (IsCellCovered(x, y))
                            {
                                CalculateCellPolygonValue(rowIdx, colIdx);
                            }
                        }
                        catch (Exception ex)
                        {
                            if (!isCellCoverageWarningShown || settings.ShowWarnings)
                            {
                                log.AddWarning("Cell coverage could not be evaluated for cell with (x,y) = (" + x.ToString(SIFTool.EnglishCultureInfo) + "," + y.ToString(SIFTool.EnglishCultureInfo) + "): " + ex.GetBaseException().Message, 2);
                                isCellCoverageWarningShown = true;
                            }
                        }
                    }
                }

//                SetZoneValues(windowIDFFile, minRowIdx, minColIdx, maxRowIdx, maxColIdx, 3);
            }
            else
            {
                // Divide in four smaller windows and check overlap for each window
                int halfRowIdx = minRowIdx + rowCount / 2;
                int halfColIdx = minColIdx + colCount / 2;

                // Process lower left quadrant 
                if (IsWindowCovered(halfRowIdx + 1, minColIdx, maxRowIdx, halfColIdx))
                {
                    GENPolygonToIDF(halfRowIdx + 1, minColIdx, maxRowIdx, halfColIdx);
                }
                else
                {
//                    SetZoneValues(windowIDFFile, halfRowIdx, minColIdx, maxRowIdx, halfColIdx, -1);
                }

                // Process upper left quadrant 
                if (IsWindowCovered(minRowIdx, minColIdx, halfRowIdx, halfColIdx))
                {
                    GENPolygonToIDF(minRowIdx, minColIdx, halfRowIdx, halfColIdx);
                }
                else
                {
//                    SetZoneValues(windowIDFFile, minRowIdx, minColIdx, halfRowIdx, halfColIdx, -1);
                }

                // Process lower right quadrant 
                if (IsWindowCovered(halfRowIdx + 1, halfColIdx + 1, maxRowIdx, maxColIdx))
                {
                    GENPolygonToIDF(halfRowIdx + 1, halfColIdx + 1, maxRowIdx, maxColIdx);
                }
                else
                {
//                    SetZoneValues(windowIDFFile, halfRowIdx, halfColIdx, maxRowIdx, maxColIdx, -1);
                }

                // Process upper right quadrant 
                if (IsWindowCovered(minRowIdx, halfColIdx + 1, halfRowIdx, maxColIdx))
                {
                    GENPolygonToIDF(minRowIdx, halfColIdx + 1, halfRowIdx, maxColIdx);
                }
                else
                {
//                    SetZoneValues(windowIDFFile, minRowIdx, halfColIdx, halfRowIdx, maxColIdx, -1);
                }
            }
        }

        //private void SetZoneValues(IDFFile idfFile, int minRowIdx, int minColIdx, int maxRowIdx, int maxColIdx, int value)
        //{
        //    for (int rowIdx = minRowIdx; (rowIdx <= maxRowIdx) && (rowIdx < valueIDFFile.NRows); rowIdx++)
        //    {
        //        for (int colIdx = minColIdx; (colIdx <= maxColIdx) && (colIdx < valueIDFFile.NCols); colIdx++)
        //        {
        //            idfFile.values[rowIdx][colIdx] = value;
        //        }
        //    }
        //    try
        //    {
        //        windowIDFFile.WriteFile(Path.Combine(settings.OutputPath, "Windows.IDF"));
        //    }
        //    catch (Exception)
        //    {
        //        // ignore
        //    }
        //}

        private bool IsWindowCovered(int minRowIdx, int minColIdx, int maxRowIdx, int maxColIdx)
        {
            // Check that polygon is present in current window
            Extent currWindowExtent = new Extent(valueIDFFile.GetX(minColIdx) - halfCellsizeX, valueIDFFile.GetY(maxRowIdx) - halfCellsizeY, 
                valueIDFFile.GetX(maxColIdx) + halfCellsizeX, valueIDFFile.GetY(minRowIdx) + halfCellsizeY);

            List<GENPolygon> clippedGENPolygons = clockwiseGENPolygon.ClipPolygonWithoutDATRow(currWindowExtent);
            return (clippedGENPolygons.Count > 0);
        }

        /// <summary>
        /// Check if a cell (as defined by x and y-coordinates) is (partly) covered by the specified polygon
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="polygonXArray"></param>
        /// <param name="polygonYArray"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual bool IsCellCovered(float x, float y)
        {
            // Check if current cell is covered by this polygon
            bool isCellCovered = false;
            switch (settings.GridPar2String)
            {
                case "2":
                    // Calculate actual overlap between cell and polygon: clip polygon by cellextent and sum areas of clipped polygon(s)
                    idfCellArea = 0;
                    Extent currCellExtent = new Extent(x - halfCellsizeX, y - halfCellsizeY, x + halfCellsizeX, y + halfCellsizeY);

                    List<GENPolygon> clippedGENPolygons = clockwiseGENPolygon.ClipPolygonWithoutDATRow(currCellExtent);
                    foreach (GENPolygon clippedGENPolygon in clippedGENPolygons)
                    {
                        idfCellArea += (float)Math.Abs(clippedGENPolygon.CalculateArea());
                    }
                    isCellCovered = idfCellArea > Point.Tolerance;
                    break;
                case null:    // undefined, initial value, apply method 1 as a default
                case "1":
                    // Check if cell center is inside the polygon
                    isCellCovered = GISUtils.IsPointInPolygon(x, y, polygonXArray, polygonYArray);
                    if ((settings.GridPar2String == null) && !isInvalidGridPar2WarningShown)
                    {
                        log.AddInfo("GridPar2 (c2) was not defined, using default method 1 for GEN-polygon(s)", 2);
                        isInvalidGridPar2WarningShown = true;
                    }
                    break;
                default:
                    throw new ToolException("Undefined GridPar2-value for GEN-polygon(s): " + settings.GridPar2String);
            }

            return isCellCovered;
        }

        /// <summary>
        /// Convert GEN-polygon to IDF-cell values in valueIDFFile
        /// </summary>
        /// <param name="genLine"></param>
        /// <param name="genFeatureIdx"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        protected virtual void GENLineToIDF(GENLine genLine, ref int genFeatureIdx, int logIndentLevel)
        {
            // Algorithm is as follows:
            // 0. Assume value to set along line has been defined and stored in class variable genValue
            // 1. Loop through line segments
            //   2. Loop through cells that line segment passes through
            //     2a. determine cell that line segment starts in
            //     2b. determine part of line segment in cell
            //     3. if line segment continues outside cell:
            //       3a. update statistics for line length inside cell
            //       3b. continue at 2b, with part of line segment outside cell
            //     4. if line segment is completely inside cell: 
            //       4a. update statisics for length of line inside cell
            //       4b. continue at 1 with next line segment
            // Note: valueIDFFile, angleIDFFile, etc. are class variables.

            // Work with GEN-extent for looping through all lines
            float xCellsize = valueIDFFile.XCellsize;
            float yCellsize = valueIDFFile.YCellsize;
            Extent genExtent = genLine.GENFile.Extent;
            genExtent = genExtent.Snap(valueIDFFile.XCellsize, valueIDFFile.YCellsize, true);

            float cellsize = settings.GridCellsize;
            float halfCellsize = cellsize / 2.0f;
            double lineLength = genLine.CalculateLength();

            // Determine cell that line starts in
            int currCellRowIdx = GetRowIdx(genExtent, yCellsize, genLine.Points[0].Y);
            int currCellColIdx = GetColIdx(genExtent, xCellsize, genLine.Points[0].X);
            float currCellX = GetX(genExtent, xCellsize, currCellColIdx);
            float currCellY = GetY(genExtent, yCellsize, currCellRowIdx);
            Point currLinePoint = genLine.Points[0];
            Point currEndPoint = currLinePoint;
            Point currentCellEntrancePoint = currLinePoint.Copy(); // new FloatPoint(currCellX, currCellY);
            Point currentCellLeavingPoint = null;

            // 1. Loop through line segments
            int cellIdx = 0;
            double processedLineDistance = 0;
            double currCellStartDistance = 0;
            for (int pointIdx = 1; pointIdx < genLine.Points.Count; pointIdx++)
            {
                // Retrieve next line segment
                Point nextLinePoint = genLine.Points[pointIdx];
                LineSegment segment = new LineSegment(currLinePoint, nextLinePoint);

                // 2. Loop through cells that line segment passes through
                double processedSegmentLength = 0;
                int emptySegmentCount = 0;
                do
                {
                    // determine part of line segment in cell
                    Extent currCellExtent = new Extent(currCellX - halfCellsize, currCellY - halfCellsize, currCellX + halfCellsize, currCellY + halfCellsize);
                    LineSegment cellSegment = CSHFClipper.ClipLine(segment, currCellExtent);
                    if ((cellSegment != null) && (cellSegment.Length > 0))
                    {
                        // 3a/4a update statistics for length/angle of line segment inside cell
                        if (lengthIDFFile.Extent.Contains(currCellX, currCellY))
                        {
                            lengthIDFFile.AddValue(currCellX, currCellY, (float)cellSegment.Length);
                        }

                        processedSegmentLength += cellSegment.Length;
                        processedLineDistance += cellSegment.Length;

                        currEndPoint = cellSegment.P2;
                        emptySegmentCount = 0;
                    }
                    else
                    {
                        emptySegmentCount++;

                        if (emptySegmentCount > 2)
                        {
                            HandleMissingSegment(currCellX, currCellY, emptySegmentCount, segment.Length, processedSegmentLength);

                            currCellX = -1;
                            currCellY = -1;
                            currentCellEntrancePoint = null;
                            currentCellLeavingPoint = null;
                            //currCellRowIdx = GetRowIdx(genExtent, yCellsize, nextLinePoint.Y);
                            //currCellColIdx = GetColIdx(genExtent, xCellsize, nextLinePoint.X);
                            //currCellX = GetX(genExtent, xCellsize, currCellColIdx);
                            //currCellY = GetY(genExtent, yCellsize, currCellRowIdx);
                            //currentCellEntrancePoint = null;
                            //currentCellLeavingPoint = null;
                            break;
                        }

                    }

                    // 3. Check if line segment continues outside cell:
                    if ((segment.Length - processedSegmentLength) > ERRORMARGIN)
                    {
                        //  3a. determine interpolated value for current cell (which may be based on line length and distance along line halfway through cell)
                        CalculateCellLineValue(currCellX, currCellY, cellIdx, currCellStartDistance, processedLineDistance, lineLength);

                        currentCellLeavingPoint = currEndPoint;
                        if (currentCellEntrancePoint != null)
                        {
                            if ((angleIDFFile != null) && angleIDFFile.GetValue(currCellX, currCellY).Equals(angleIDFFile.NoDataValue))
                            {
                                // For first group of linesegments in cell angle is added
                                float angle = (float)new LineSegment(currentCellEntrancePoint, currentCellLeavingPoint).CalculateAngle();
                                if (!angle.Equals(float.NaN))
                                {
                                    angleIDFFile.SetValue(currCellX, currCellY, angle);
                                }
                            }
                        }
                        currentCellEntrancePoint = currentCellLeavingPoint;
                        currentCellLeavingPoint = null;

                        // Find next cell with steps of 10 cm after endpoint of cell segment in the direction of the line segment
                        int nextCellRowIdx;
                        int nextCellColIdx;
                        Point nextCellPoint = currEndPoint;
                        do
                        {
                            nextCellPoint = MovePoint(nextCellPoint, new Vector(nextLinePoint.X - currLinePoint.X, nextLinePoint.Y - currLinePoint.Y), 0.1);
                            nextCellRowIdx = GetRowIdx(genExtent, yCellsize, nextCellPoint.Y);
                            nextCellColIdx = GetColIdx(genExtent, xCellsize, nextCellPoint.X);
                        } while ((nextCellRowIdx == currCellRowIdx) && (nextCellColIdx == currCellColIdx));
                        currCellRowIdx = nextCellRowIdx;
                        currCellColIdx = nextCellColIdx;
                        currCellX = GetX(genExtent, xCellsize, currCellColIdx);
                        currCellY = GetY(genExtent, yCellsize, currCellRowIdx);

                        currCellStartDistance = processedLineDistance;

                        // 3c. continue at 2b, with part of line segment outside cell
                        cellIdx++;
                    }
                }
                while ((segment.Length - processedSegmentLength) > ERRORMARGIN);

                //  4. last part of line segment is completely inside current cell, continue with step 1 for next line segment
                currLinePoint = nextLinePoint;
            }

            CalculateLastCellLineValue(currCellX, currCellY, currCellStartDistance, processedLineDistance, lineLength);
        }

        /// <summary>
        /// Perform corrections on converted IDF-file based on calculated length of GEN-fils per IDF-cell
        /// </summary>
        protected virtual void CorrectLineInconsistencies()
        {
            // Correct for possible inconsistencies in value and length IDF-files
            valueIDFFile.ReplaceValues(lengthIDFFile, lengthIDFFile.NoDataValue, valueIDFFile.NoDataValue);
            valueIDFFile.ReplaceValues(lengthIDFFile, 0, valueIDFFile.NoDataValue);
            if (lengthIDFFile != null)
            {
                lengthIDFFile.ReplaceValues(valueIDFFile, valueIDFFile.NoDataValue, lengthIDFFile.NoDataValue);
            }
        }

        /// <summary>
        /// Retrieves the columnindex into the values-array for the given x-value
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int GetColIdx(IDFFile idfFile, double x)
        {
            return (int)((x - idfFile.Extent.llx) / idfFile.XCellsize);
        }

        public void CheckSettings(GENFile genFile)
        {
            if (settings.GridPar1String != null)
            {
                if (genFile.HasDATFile())
                {
                    int gridPar1 = genFile.DATFile.FindColumnNumber(settings.GridPar1String, true);
                    if (gridPar1 < 0)
                    {
                        throw new ToolException("Column(number) " + settings.GridPar1String + " is not found for GEN-file " + Path.GetFileName(genFile.Filename));
                    }
                    else if (gridPar1 > genFile.DATFile.ColumnNames.Count)
                    {
                        throw new ToolException("Columnnumber " + gridPar1 + " is larger than column count (" + genFile.DATFile.ColumnNames.Count + ") and is invalid for GEN-file " + Path.GetFileName(genFile.Filename));
                    }
                }
                else
                {

                    // When no DAT-file is present GridPar1 should be a floating point value
                    if (!float.TryParse(settings.GridPar1String, NumberStyles.Float, SIFTool.EnglishCultureInfo, out float par1Value))
                    {
                        throw new ToolException("Invalid constant float value for GridPar1-value: (which is used when no DAT-file is present): " + settings.GridPar1String);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieves the rowindex into the values-array for the given y-value
        /// </summary>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int GetRowIdx(IDFFile idfFile, double y)
        {
            return (int)(((idfFile.Extent.ury - 0.00001) - y) / idfFile.YCellsize);
        }

        /// <summary>
        /// Retrieves the rowindex into the extent for the given y
        /// </summary>
        /// <param name="extent"></param>
        /// <param name="yCellsize"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static int GetRowIdx(Extent extent, float yCellsize, double y)
        {
            return (int)(((extent.ury - 0.00001) - y) / yCellsize);
        }

        /// <summary>
        /// Retrieves the columnindex into the extent for the given x
        /// </summary>
        /// <param name="extent"></param>
        /// <param name="xCellsize"></param>
        /// <param name="x"></param>
        /// <returns></returns>
        public static int GetColIdx(Extent extent, float xCellsize, double x)
        {
            return (int)((x - extent.llx) / xCellsize);
        }

        /// <summary>
        /// Retrieves the x-value for the given columnindex into the extent
        /// </summary>
        /// <param name="colIdx"></param>
        /// <returns></returns>
        public static float GetX(Extent extent, float xCellsize, int colIdx)
        {
            return extent.llx + (colIdx + 0.5f) * xCellsize;
        }

        /// <summary>
        /// Retrieves the y-value for the given rowindex into the extent
        /// </summary>
        /// <param name="rowIdx"></param>
        /// <returns></returns>
        public static float GetY(Extent extent, float yCellsize, int rowIdx)
        {
            return extent.ury - (rowIdx + 0.5f) * yCellsize;
        }

        /// <summary>
        /// Move point over specfied distance along specified vector
        /// </summary>
        /// <param name="point"></param>
        /// <param name="v"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        protected Point MovePoint(Point point, Vector v, double distance)
        {
            double dx2 = double.NaN;
            double dy2 = double.NaN;
            if (!v.dX.Equals(0))
            {
                double angle = Math.Atan2(v.dY, v.dX);
                dx2 = distance * Math.Cos(angle);
                dy2 = distance * Math.Sin(angle);
            }
            else
            {
                dx2 = 0;
                dy2 = distance * Math.Sign(v.dY);
            }

            return new DoublePoint(point.X + dx2, point.Y + dy2);
        }

        ///// <summary>
        ///// Calculate area of specified GEN-polygon and give warning for negative area
        ///// </summary>
        ///// <param name="genPolygon"></param>
        ///// <returns></returns>
        //protected virtual float CalculateArea(GENPolygon genPolygon)
        //{
        //    float polygonArea = (float)genPolygon.CalculateArea();

        //    if (polygonArea < 0)
        //    {
        //        log.AddWarning("Points of polygon " + genPolygon.ID + " are defined counterclockwise, which indicates an island, and may give unexpected results. Use option /i or /n.", 1);
        //    }

        //    return polygonArea;
        //}

        /// <summary>
        /// Actually calculate the current cell value inside the current polygon
        /// </summary>
        /// <param name="rowIdx"></param>
        /// <param name="colIdx"></param>
        /// <param name="logIndentLevel"></param>
        protected virtual void CalculateCellPolygonValue(int rowIdx, int colIdx)
        {
            // Current polygon is (partly) inside cell
            if (valueIDFFile.values[rowIdx][colIdx].Equals(valueIDFFile.NoDataValue))
            {
                // Cell does not contain any of the polygons that were processed earlier

                if (isIsland)
                {
                    // For islands it is not expected to find an inner ring (with negative area) before the outer ring. Continue with negative area, but give warning
                    areaIDFFile.values[rowIdx][colIdx] = -idfCellArea;
                    valueIDFFile.values[rowIdx][colIdx] = (settings.GridPar3 == 6) ? genValue * -idfCellArea : genValue;    // use area-weighted value for option 6 (weighted average)

                    if (!isNegativeAreaWarningShown || settings.ShowWarnings)
                    {
                        log.AddWarning("Area<0 for feature idx " + genFeatureIdx + ", id " + genPolygonID + ", cell (" + (rowIdx + 1) + "," + (colIdx + 1) + "), indicating polygon/point order issue", logIndentLevel + 1);
                        log.AddInfo("Note1: calcalated area and value grid might be incorrect. Check polygon and/or point order.", logIndentLevel + 1);
                        log.AddInfo("Note2: topologically correct polygon rings should be ordered according to their containment relationship.", logIndentLevel + 1);
                        if (!settings.ShowWarnings)
                        {
                            log.AddInfo("Note3: similar warnings will not be shown anymore for this conversion.", logIndentLevel + 1);
                        }
                        isNegativeAreaWarningShown = true;
                    }

                    polygonAreaIDFFile.values[rowIdx][colIdx] = 0;
                }
                else
                {
                    areaIDFFile.values[rowIdx][colIdx] = idfCellArea;
                    valueIDFFile.values[rowIdx][colIdx] = (settings.GridPar3 == 6) ? genValue * idfCellArea : genValue;    // use area-weighted value for option 6 (weighted average)
                    polygonAreaIDFFile.values[rowIdx][colIdx] = polygonArea;
                }
            }
            else
            {
                // Another (earlier processed) polygon also contains this cell

                if (isIsland)
                {
                    // For islands substract area of polygon in cell from (total) polygon area in cell
                    if (settings.GridPar3 == 6)
                    {
                        // Keep same value, and reduce cellarea
                        valueIDFFile.values[rowIdx][colIdx] = valueIDFFile.values[rowIdx][colIdx] / areaIDFFile.values[rowIdx][colIdx];
                        areaIDFFile.values[rowIdx][colIdx] -= idfCellArea;
                        valueIDFFile.values[rowIdx][colIdx] = valueIDFFile.values[rowIdx][colIdx] * areaIDFFile.values[rowIdx][colIdx];
                    }
                    else
                    {
                        // leave current value, but reduce cell area
                        areaIDFFile.values[rowIdx][colIdx] -= idfCellArea;
                    }

                    if (areaIDFFile.values[rowIdx][colIdx] <= 0)
                    {
                        // If resulting area is 0, this cell is a donut hole, remove value and area
                        valueIDFFile.values[rowIdx][colIdx] = valueIDFFile.NoDataValue;
                        areaIDFFile.values[rowIdx][colIdx] = areaIDFFile.NoDataValue;

                        //  Remove current polygonarea when no area is left in cell
                        polygonAreaIDFFile.values[rowIdx][colIdx] = 0;
                    }
                }
                else
                {
                    switch (settings.GridPar3)
                    {
                        case 1:
                            // use first polygon, ignore later polygons
                            break;
                        case 2:
                            // use smallest value (min)
                            if (genValue < valueIDFFile.values[rowIdx][colIdx])
                            {
                                valueIDFFile.values[rowIdx][colIdx] = genValue;
                                areaIDFFile.values[rowIdx][colIdx] = idfCellArea;
                            }
                            break;
                        case 3:
                            // use largest value (max)
                            if (genValue > valueIDFFile.values[rowIdx][colIdx])
                            {
                                valueIDFFile.values[rowIdx][colIdx] = genValue;
                                areaIDFFile.values[rowIdx][colIdx] = idfCellArea;
                            }
                            break;
                        case 4:
                            // Add value to current cellvalue (sum)
                            valueIDFFile.values[rowIdx][colIdx] += genValue;
                            areaIDFFile.values[rowIdx][colIdx] += idfCellArea;
                            break;
                        case 5:
                            // use polygon with largest area in cell (area)
                            if (idfCellArea > areaIDFFile.values[rowIdx][colIdx])
                            {
                                valueIDFFile.values[rowIdx][colIdx] = genValue;
                                areaIDFFile.values[rowIdx][colIdx] = idfCellArea;
                            }
                            break;
                        case 6:
                            // use area-weighted value
                            valueIDFFile.values[rowIdx][colIdx] += (idfCellArea * genValue);
                            areaIDFFile.values[rowIdx][colIdx] += idfCellArea;
                            break;
                        case 7:
                            // use polygon with smallest area in cell (area)
                            if (idfCellArea < areaIDFFile.values[rowIdx][colIdx])
                            {
                                valueIDFFile.values[rowIdx][colIdx] = genValue;
                                areaIDFFile.values[rowIdx][colIdx] = idfCellArea;
                            }
                            break;
                        case 8:
                            // use largest polygon
                            if (polygonArea > polygonAreaIDFFile.values[rowIdx][colIdx])
                            {
                                valueIDFFile.values[rowIdx][colIdx] = genValue;
                                areaIDFFile.values[rowIdx][colIdx] = idfCellArea;
                                polygonAreaIDFFile.values[rowIdx][colIdx] = polygonArea;
                            }
                            break;
                        case 9:
                            // use smallest polygon
                            if (polygonArea < polygonAreaIDFFile.values[rowIdx][colIdx])
                            {
                                valueIDFFile.values[rowIdx][colIdx] = genValue;
                                areaIDFFile.values[rowIdx][colIdx] = idfCellArea;
                                polygonAreaIDFFile.values[rowIdx][colIdx] = polygonArea;
                            }
                            break;
                        case 10:
                            // use last polygon
                            valueIDFFile.values[rowIdx][colIdx] = genValue;
                            areaIDFFile.values[rowIdx][colIdx] = idfCellArea;
                            polygonAreaIDFFile.values[rowIdx][colIdx] = polygonArea;
                            break;
                        default:
                            throw new Exception("Unknown value for GridPar3: " + settings.GridPar3);
                    }
                }
            }
        }

        protected void CalculateFirstCellLineValue(float currCellX, float currCellY, double currCellStartDistance, double processedLineDistance, double lineLength)
        {
            InterpolateCurrentCellValue(currCellX, currCellY, genValue, genValue2, currCellStartDistance, processedLineDistance, lineLength);
        }

        protected void CalculateIntermediateCellLineValue(float currCellX, float currCellY, double currCellStartDistance, double processedLineDistance, double lineLength)
        {
            InterpolateCurrentCellValue(currCellX, currCellY, genValue, genValue2, currCellStartDistance, processedLineDistance, lineLength);
        }

        /// <summary>
        /// Interpolate value for specific cell along GEN-line based on val1 and val2 at both ends of the line
        /// </summary>
        /// <param name="currCellX"></param>
        /// <param name="currCellY"></param>
        /// <param name="val1"></param>
        /// <param name="val2"></param>
        /// <param name="currCellStartDistance"></param>
        /// <param name="currCellEndDistance"></param>
        /// <param name="lineLength"></param>
        private void InterpolateCurrentCellValue(float currCellX, float currCellY, float val1, float val2, double currCellStartDistance, double currCellEndDistance, double lineLength)
        {
            // Calculate current cell value 
            if ((currCellX >= 0) && (currCellY >= 0) && valueIDFFile.Extent.Contains(currCellX, currCellY))
            {
                float currCellValue = valueIDFFile.GetValue(currCellX, currCellY);
                double distance = (currCellStartDistance + currCellEndDistance) / 2;
                float newCellValue = float.NaN;
                if (val1.Equals(float.NaN) || val1.Equals(valueIDFFile.NoDataValue))
                {
                    if (!val2.Equals(float.NaN))
                    {
                        if (settings.GridPar3.Equals(float.NaN) || ((lineLength - distance) < settings.GridPar3))
                        {
                            newCellValue = val2;
                        }
                        else
                        {
                            newCellValue = valueIDFFile.NoDataValue;
                        }
                    }
                    else
                    {
                        newCellValue = valueIDFFile.NoDataValue;
                    }
                }
                else if (val2.Equals(float.NaN))
                {
                    if (settings.GridPar3.Equals(float.NaN) || (distance < settings.GridPar3))
                    {
                        newCellValue = val1;
                    }
                    else
                    {
                        newCellValue = valueIDFFile.NoDataValue;
                    }
                }
                else
                {
                    newCellValue = (float)(val1 + (distance / lineLength) * (val2 - val1));
                }
                if (currCellValue.Equals(valueIDFFile.NoDataValue))
                {
                    // no other value has been set yet for this cell, so simply set cell value
                    valueIDFFile.SetValue(currCellX, currCellY, newCellValue);
                }
                else if (!newCellValue.Equals(valueIDFFile.NoDataValue))
                {
                    // another value is already present in this cell from another line segment
                    double currLength = lengthIDFFile.GetValue(currCellX, currCellY);
                    double newLength = currCellEndDistance - currCellStartDistance;
                    // correct currLength, since newLength is already added during line segment processing
                    currLength = currLength - newLength;

                    // set new cell value to weighted average of both values;
                    newCellValue = (float)((newLength / (newLength + currLength)) * newCellValue + (currLength / (newLength + currLength)) * currCellValue);
                    valueIDFFile.SetValue(currCellX, currCellY, newCellValue);
                }
                else
                {
                    // Currrent line segment doesn't have values, keep current cellvalue
                }
            }
        }

        /// <summary>
        /// Retrieve value(s) for specified GEN-feature based on DAT-file and settings`. Result will be stored in class properties <see cref="genValue"/> and <see cref="genValue2"/>.
        /// </summary>
        /// <param name="genFeature"></param>
        /// <param name="genFeatureIdx"></param>
        /// <param name="logIndentLevel"></param>
        protected virtual void RetrieveGENValue(GENFeature genFeature, int genFeatureIdx, int logIndentLevel)
        {
            genValue = float.NaN;
            if (genFeature.GENFile.HasDATFile())
            {
                string id = genFeature.ID;
                DATRow datRow = genFeature.GENFile.DATFile.GetRow(id);
                if (datRow != null)
                {
                    // Check if a valid GEN column index was specified
                    if (settings.GridPar1String != null)
                    {
                        int gridPar1 = genFeature.GENFile.DATFile.FindColumnNumber(settings.GridPar1String);
                        if ((gridPar1 > 0) && (gridPar1 <= datRow.Count))
                        {
                            if (float.TryParse(datRow[gridPar1 - 1], NumberStyles.Float, SIFTool.EnglishCultureInfo, out genValue))
                            {
                                // val1 is a floating point value, parse val2
                                if ((genFeature is GENLine) && (settings.GridPar2String != null))
                                {
                                    int gridPar2 = genFeature.GENFile.DATFile.FindColumnNumber(settings.GridPar2String);
                                    if ((gridPar2 > 0) && (gridPar2 <= datRow.Count))
                                    {
                                        if (!float.TryParse(datRow[gridPar2 - 1], NumberStyles.Float, SIFTool.EnglishCultureInfo, out genValue2))
                                        {
                                            log.AddWarning("GEN-value 2 (" + datRow[gridPar2 - 1] + ") not defined for feature " + genFeatureIdx + ", using GEN-value 1 (" + genValue + ")", logIndentLevel);
                                            genValue2 = genValue;
                                        }
                                    }
                                    else
                                    {
                                        // column index 2 is not defined, always use val1;
                                        genValue2 = genValue;
                                    }
                                }
                                else
                                {
                                    // column index 2 is not defined, always use val1;
                                    genValue2 = genValue;
                                }
                            }
                            else
                            {
                                log.AddWarning("GEN-value 1 not defined for feature " + genFeatureIdx + ": " + datRow[gridPar1 - 1], logIndentLevel);
                                //  val1 is not a floating point use default values: id if a value, or feature index
                                genValue = int.TryParse(id, out int idVal) ? idVal : (genFeatureIdx + 1);
                                genValue2 = genValue;
                            }
                        }
                        else
                        {
                            // No valid GEN-column was specified
                            genValue = genFeatureIdx + 1;
                            genValue2 = genValue;
                        }
                    }
                    else
                    {
                        // No valid GEN-column was specified
                        genValue = genFeatureIdx + 1;
                        genValue2 = genValue;
                    }
                }
                else
                {
                    if (settings.GridPar1String != null)
                    {
                        // DAT-row is missing for this GEN-feature, use NoData-value
                        genValue = float.NaN;
                        genValue2 = float.NaN;
                    }
                    else
                    {
                        // DAT-row is missing, but columnindices have not been defined, use index-value
                        genValue = genFeatureIdx + 1;
                        genValue2 = genValue;
                    }
                }
            }
            else
            {
                // No DAT-file is present: use sequence number if no value1 was defined, otherwise use that value as a constant value
                if (settings.GridPar1String != null)
                {
                    if (!float.TryParse(settings.GridPar1String, NumberStyles.Float, SIFTool.EnglishCultureInfo, out float par1Value))
                    {
                        throw new ToolException("Invalid constant float value for GridPar1-value: (which is used when no DAT-file is present): " + settings.GridPar1String);
                    }
                    genValue = par1Value;
                    genValue2 = genValue;
                }
                else
                {
                    genValue = genFeatureIdx + 1;
                    genValue2 = genValue;
                }
            }

            if (settings.SkippedValues != null)
            {
                foreach (ValueRange range in settings.SkippedValues)
                {
                    if (range.Contains(genValue))
                    {
                        genValue = float.NaN;
                    }
                    if (range.Contains(genValue2))
                    {
                        genValue2 = float.NaN;
                    }
                }
            }
        }

        /// <summary>
        /// Calculate line value for last cell
        /// </summary>
        /// <param name="currCellX"></param>
        /// <param name="currCellY"></param>
        /// <param name="currCellStartDistance"></param>
        /// <param name="processedLineDistance"></param>
        /// <param name="lineLength"></param>
        protected virtual void CalculateLastCellLineValue(float currCellX, float currCellY, double currCellStartDistance, double processedLineDistance, double lineLength)
        {
            InterpolateCurrentCellValue(currCellX, currCellY, genValue, genValue2, currCellStartDistance, processedLineDistance, lineLength);
        }

        /// <summary>
        /// Calculate value for current cell (based on line length and distance along line halfway through cell)
        /// </summary>
        /// <param name="currCellX"></param>
        /// <param name="currCellY"></param>
        /// <param name="cellIdx"></param>
        /// <param name="currCellStartDistance"></param>
        /// <param name="processedLineDistance"></param>
        /// <param name="lineLength"></param>
        protected virtual void CalculateCellLineValue(float currCellX, float currCellY, int cellIdx, double currCellStartDistance, double processedLineDistance, double lineLength)
        {
            if (cellIdx == 0)
            {
                // Handle first cell
                CalculateFirstCellLineValue(currCellX, currCellY, currCellStartDistance, processedLineDistance, lineLength);
            }
            else
            {
                // Handle intermediate cell
                CalculateIntermediateCellLineValue(currCellX, currCellY, currCellStartDistance, processedLineDistance, lineLength);
            }

        }

        /// <summary>
        /// Handle missed segment length's during processing which might occur because of rounding, etc.
        /// </summary>
        /// <param name="currCellX"></param>
        /// <param name="currCellY"></param>
        /// <param name="length"></param>
        /// <param name="processedSegmentLength"></param>
        protected virtual void HandleMissingSegment(float currCellX, float currCellY, int emptySegmentCount, double segmentLength, double processedSegmentLength)
        {
            if (emptySegmentCount > 2)
            {
                log.AddWarning("Unexpected missing segment, set to zero-value: (" + currCellX.ToString("F3", SIFTool.EnglishCultureInfo) + "," + currCellY.ToString("F3", SIFTool.EnglishCultureInfo) + ")");
                warningIPFFile.AddPoint(new IPFPoint(warningIPFFile, new FloatPoint(currCellX, currCellY), new List<string>() { currCellX.ToString(), (segmentLength - processedSegmentLength).ToString(SIFTool.EnglishCultureInfo), currCellY.ToString(), "Unexpected missing segment: segment.Length (" + segmentLength + ") - processedSegmentLength (" + processedSegmentLength + ") > ERRORMARGIN (" + ERRORMARGIN + ")" }));
                warningIPFFile.WriteFile();
            }
        }

        /// <summary>
        /// Write specified result files after conversion
        /// </summary>
        /// <param name="outputPath"></param>
        /// <param name="outputFilename"></param>
        /// <param name="logIndentLevel"></param>
        protected virtual void WriteResults(string outputPath, string outputFilename, int logIndentLevel)
        {
            log.AddInfo("Writing IDF-file '" + Path.GetFileName(valueIDFFile.Filename) + "' ...", logIndentLevel);
            valueIDFFile.WriteFile(Path.Combine(outputPath, Path.GetFileNameWithoutExtension(outputFilename) + ".IDF"), metadata);
            if (settings.AddAngleIDFFile)
            {
                if (angleIDFFile.RetrieveElementCount() > 0)
                {
                    angleIDFFile.WriteFile(Path.Combine(outputPath, Path.GetFileNameWithoutExtension(outputFilename) + "_angle.IDF"));
                }
            }

            if (settings.AddLengthAreaIDFFile)
            {
                if ((lengthIDFFile != null) && (lengthIDFFile.RetrieveElementCount() > 0))
                {
                    lengthIDFFile.WriteFile(Path.Combine(outputPath, Path.GetFileNameWithoutExtension(outputFilename) + "_length.IDF"));
                }
                if ((areaIDFFile != null) && (areaIDFFile.RetrieveElementCount() > 0))
                {
                    areaIDFFile.WriteFile(Path.Combine(outputPath, Path.GetFileNameWithoutExtension(outputFilename) + "_area.IDF"));
                }
            }
        }

        protected class IDFWindow
        {
            public int MinRowIdx;
            public int MaxRowIdx;
            public int MinColIdx;
            public int MaxColIdx;

            public IDFWindow(int minRowIdx, int minColIdx, int maxRowIdx, int maxColIdx)
            {
                this.MinRowIdx = minRowIdx;
                this.MinColIdx = minColIdx;
                this.MaxRowIdx = maxRowIdx;
                this.MaxColIdx = maxColIdx;
            }
        }
    }
}
