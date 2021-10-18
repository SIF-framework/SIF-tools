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
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.IDF;
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

        // Variables used during conversion
        protected IDFFile valueIDFFile;
        protected IDFFile lengthIDFFile;
        protected IDFFile angleIDFFile;
        protected Metadata metadata;
        protected float genValue;

        public GENIDFConverter(SIFToolSettings settings, Log log)
        {
            this.settings = settings;
            this.log = log;
            genValue = float.NaN;
        }

        public virtual bool Convert(string inputFilename, string outputPath, string outputFilename)
        {
            // Read input GEN-file
            GENFile genFile = GENFile.ReadFile(inputFilename);
            if (genFile.Count == 0)
            {
                log.AddWarning("GEN-file does not contain any features and is skipped: " + Path.GetFileName(inputFilename), 1);
                return false;
            }

            InitializeFiles(genFile);

            int genFeatureIdx = 0;
            foreach (GENFeature genFeature in genFile.Features)
            {
                RetrieveGENValue(genFeature, genFeatureIdx);
                if (genFeature is GENPolygon)
                {
                    log.AddInfo("Processing GEN-polygon " + genFeature.ID, 1);
                    GENPolygonToIDF((GENPolygon)genFeature, ref genFeatureIdx);
                }
                else if (genFeature is GENLine)
                {
                    log.AddInfo("Processing GEN-line " + genFeature.ID, 1);
                    GENLineToIDF((GENLine)genFeature, ref genFeatureIdx);
                    CorrectLineInconsistencies();
                }
                else
                {
                    log.AddWarning("Conversion of GEN-feature " + genFeature.GetType().Name + " is currently not supported.");
                }
                genFeatureIdx++;
            }

            WriteResults(outputPath, outputFilename);

            return true;
        }

        /// <summary>
        /// Create initial files for GEN-IDF conversion: valueIDFFile, lengthIDFFile, angleIDFFile and Metadata for valueIDFFile
        /// </summary>
        /// <param name="genFile"></param>
        protected virtual void InitializeFiles(GENFile genFile)
        {
            Extent idfExtent = GetIDFExtent(genFile);
            valueIDFFile = new IDFFile("valueIDFFile.IDF", idfExtent, settings.GridCellsize, -9999.0f);
            valueIDFFile.ResetValues();

            // Create length IDF-file to correct some possible inconsistencies
            lengthIDFFile = new IDFFile("lenghtIDFFile.IDF", idfExtent, settings.GridCellsize, -9999.0f);
            lengthIDFFile.ResetValues();

            angleIDFFile = null;
            if (settings.AddAngleIDFFile)
            {
                angleIDFFile = new IDFFile("areaIDFFile.IDF", idfExtent, settings.GridCellsize, -9999.0f);
                angleIDFFile.ResetValues();
            }

            metadata = new Metadata("Converted from GEN-file to IDF-file with " + SIFTool.Instance.ToolName + " " + SIFTool.Instance.ToolVersion);
            metadata.Source = genFile.Filename;
            if (settings.GENColIdx >= 0)
            {
                metadata.ProcessDescription = "Gridded features: value from (one-based) GEN-column " + settings.GENColIdx;
            }
            else
            {
                metadata.ProcessDescription = "Gridded features: value 1 for cells within polygon";
            }
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
        protected virtual void GENPolygonToIDF(GENPolygon genPolygon, ref int genFeatureIdx)
        {
            Extent polygonExtent = genPolygon.RetrieveExtent();

            double[] polygonXArray = new double[genPolygon.Points.Count];
            double[] polygonYArray = new double[genPolygon.Points.Count];
            for (int pointIdx = 0; pointIdx < genPolygon.Points.Count; pointIdx++)
            {
                polygonXArray[pointIdx] = genPolygon.Points[pointIdx].X;
                polygonYArray[pointIdx] = genPolygon.Points[pointIdx].Y;
            }

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

            for (int rowIdx = minRowIdx; (rowIdx <= maxRowIdx) && (rowIdx < valueIDFFile.NRows); rowIdx++)
            {
                for (int colIdx = minColIdx; (colIdx <= maxColIdx) && (colIdx < valueIDFFile.NCols); colIdx++)
                {
                    float x = valueIDFFile.GetX(colIdx);
                    float y = valueIDFFile.GetY(rowIdx);
                    if (GISUtils.IsPointInPolygon(x, y, polygonXArray, polygonYArray))
                    {
                        CalculateCellPolygonValue(rowIdx, colIdx);
                    }
                }
            }
        }

        /// <summary>
        /// Convert GEN-polygon to IDF-cell values in valueIDFFile
        /// </summary>
        /// <param name="genLine"></param>
        /// <param name="genFeatureIdx"></param>
        protected virtual void GENLineToIDF(GENLine genLine, ref int genFeatureIdx)
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
                    LineSegment cellSegment = GENClipper.ClipLine(segment, currCellExtent);
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
                        HandleMissingSegment(currCellX, currCellY, emptySegmentCount, segment.Length, processedSegmentLength);

                        currCellRowIdx = GetRowIdx(genExtent, yCellsize, nextLinePoint.Y);
                        currCellColIdx = GetColIdx(genExtent, xCellsize, nextLinePoint.X);
                        currCellX = GetX(genExtent, xCellsize, currCellColIdx);
                        currCellY = GetY(genExtent, yCellsize, currCellRowIdx);
                        currentCellEntrancePoint = null;
                        currentCellLeavingPoint = null;

                        break;
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
        /// Write specified result files after conversion
        /// </summary>
        /// <param name="outputPath"></param>
        /// <param name="outputFilename"></param>
        protected virtual void WriteResults(string outputPath, string outputFilename)
        {
            valueIDFFile.WriteFile(Path.Combine(outputPath, Path.GetFileNameWithoutExtension(outputFilename) + ".IDF"), metadata);
            if (settings.AddAngleIDFFile)
            {
                if (angleIDFFile.RetrieveElementCount() > 0)
                {
                    angleIDFFile.WriteFile(Path.Combine(outputPath, Path.GetFileNameWithoutExtension(outputFilename) + "_angle.IDF"));
                }
            }
        }

        /// <summary>
        /// Retrieve value for specified GEN-feature based on DAT-file and settings
        /// </summary>
        /// <param name="genFeature"></param>
        /// <param name="genFeatureIdx"></param>
        protected virtual void RetrieveGENValue(GENFeature genFeature, int genFeatureIdx)
        {
            int idVal;
            genValue = float.NaN;
            if (genFeature.GENFile.HasDATFile())
            {
                string id = genFeature.ID;
                DATRow datRow = genFeature.GENFile.DATFile.GetRow(id);
                if (datRow != null)
                {
                    if ((settings.GENColIdx > 0) && (settings.GENColIdx <= datRow.Count))
                    {
                        if (!float.TryParse(datRow[settings.GENColIdx - 1], NumberStyles.Float, SIFTool.EnglishCultureInfo, out genValue))
                        {
                            log.AddWarning("GEN-value not defined for feature " + genFeatureIdx + ": " + datRow[settings.GENColIdx - 1], 1);
                            //  val1 is not a floating point use default values: id if a value, or feature index
                            genValue = int.TryParse(id, out idVal) ? idVal : (genFeatureIdx + 1);
                        }
                    }
                    else
                    {
                        genValue = genFeatureIdx + 1;
                    }
                }
                else
                {
                    genValue = genFeatureIdx + 1;
                }
            }
            // No DAT-file is present
            else if (settings.GENColIdx >= 0)
            {
                genValue = settings.GENColIdx;
            }
            else
            {
                genValue = genFeatureIdx + 1;
            }

            if (genValue.Equals(float.NaN))
            {
                genValue = 1.0f;
            }

            if (settings.SkippedValues != null)
            {
                foreach (ValueRange range in settings.SkippedValues)
                {
                    if (range.Contains(genValue))
                    {
                        genValue = float.NaN;
                    }
                }
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

        /// <summary>
        /// Calculate area in m2 of specified GEN-polygon
        /// </summary>
        /// <param name="genPolygon"></param>
        /// <returns></returns>
        protected virtual float CalculateArea(GENPolygon genPolygon)
        {
            float polygonArea = (float)genPolygon.CalculateArea();

            if (polygonArea < 0)
            {
                log.AddWarning("Points of polygon are defined counterclockwise, which indicates an island. Use Plus-version of " + SIFTool.Instance.ToolName + " tool to process islands", 1);
            }

            return polygonArea;
        }

        protected virtual void CalculateCellPolygonValue(int rowIdx, int colIdx)
        {
            if (valueIDFFile.values[rowIdx][colIdx].Equals(valueIDFFile.NoDataValue))
            {
                valueIDFFile.values[rowIdx][colIdx] = genValue;
            }
            else
            {
                // Another (earlier processed) polygon also contains this cell, this is ignored in basic version
            }
        }

        /// <summary>
        /// Calculate line value for current cell
        /// </summary>
        /// <param name="currCellX"></param>
        /// <param name="currCellY"></param>
        /// <param name="cellIdx"></param>
        /// <param name="currCellStartDistance"></param>
        /// <param name="processedLineDistance"></param>
        /// <param name="lineLength"></param>
        protected virtual void CalculateCellLineValue(float currCellX, float currCellY, int cellIdx, double currCellStartDistance, double processedLineDistance, double lineLength)
        {
            // Simply set cell value to specified genValue in basic version
            valueIDFFile.SetValue(currCellX, currCellY, genValue);
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
            // Simply set cell value to specified genValue in basic version
            valueIDFFile.SetValue(currCellX, currCellY, genValue);
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
            // Currently this is ignored
        }
    }
}
