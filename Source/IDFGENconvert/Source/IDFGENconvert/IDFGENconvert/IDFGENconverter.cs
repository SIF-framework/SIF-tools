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
    /// Class for conversion of IDF-files to GEN-files
    /// </summary>
    public class IDFGENConverter
    {
        public const int StatisticsDecimalCount = 3;

        protected SIFToolSettings settings;
        protected Log log;

        public IDFGENConverter(SIFToolSettings settings, Log log)
        {
            this.settings = settings;
            this.log = log;
        }

        /// <summary>
        /// Initalize result GEN-file and metadata file based on specified settings
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="metadata"></param>
        /// <param name="settings"></param>
        /// <param name="inputFilename"></param>
        public virtual void InitializeGENFile(ref GENFile genFile, ref Metadata metadata, SIFToolSettings settings, string inputFilename)
        {
            if ((settings.IsMerged) && (genFile == null))
            {
                // Results are merged in a single GEN-file
                genFile = new GENFile();
                genFile.Filename = Path.Combine(settings.OutputPath, settings.MergedGENFilename);
                AddDATFile(genFile);

                metadata = new Metadata("Automatic conversion from IDF to GEN-file with convex hull around non-NoData and non-zero cells");
                metadata.Source = settings.InputPath + "; " + settings.InputFilter;
            }
            else
            {
                // A resulting GEN-file is created for each input IDF-file
                genFile = new GENFile();
                if ((settings.OutputFilename != null) && Path.GetExtension(settings.OutputFilename).ToLower().Equals(".gen"))
                {
                    genFile.Filename = Path.Combine(settings.OutputPath, Path.GetFileNameWithoutExtension(settings.OutputFilename + ".GEN"));
                }
                else
                {
                    genFile.Filename = Path.Combine(settings.OutputPath, Path.GetFileNameWithoutExtension(inputFilename) + ".GEN");
                }
                
                AddDATFile(genFile);

                metadata = new Metadata("Automatic conversion from IDF to GEN-file with convex hull around non-NoData and non-zero cells");
                metadata.Source = inputFilename;
            }
        }

        /// <summary>
        /// Convert IDF-file to specified GEN-file
        /// </summary>
        /// <param name="inputIDFFilename">input IDF-filename</param>
        /// <param name="genFile">GEN-file to which converted GEN-features will be added. It should have a DAT-file with columns: SourceFile, Source, Idx, SourceValue</param>
        /// <param name="outputPath"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        public virtual bool Convert(string inputIDFFilename, ref GENFile genFile, string outputPath, int logIndentLevel)
        {
            log.AddInfo("converting " + Path.GetFileName(inputIDFFilename) + " to '" + settings.GetHullTypeString() +  "' ...", logIndentLevel);

            // Read input IDF-file and remove optionally skipped values
            IDFFile inputIDFFile = IDFFile.ReadFile(inputIDFFilename);
            if (settings.SkippedValues != null)
            {
                log.AddInfo("skipping values ...", logIndentLevel);
                foreach (ValueRange range in settings.SkippedValues)
                {
                    if (range.V1.Equals(range.V2))
                    {
                        inputIDFFile.ReplaceValues((float)range.V1, inputIDFFile.NoDataValue);
                    }
                    else
                    {
                        inputIDFFile.ReplaceValues(range, inputIDFFile.NoDataValue);
                    }
                }
            }

            // Convert IDF-file to GEN-file
            GENFile idfGENFile = Convert(inputIDFFile, genFile.Features.Count + 1, outputPath, logIndentLevel);

            // Add feartures from resulting GEN-file to output GEN-file
            if ((idfGENFile != null) && (idfGENFile.Features.Count > 0))
            {
                int genFeatureIdx = genFile.Features.Count + 1;
                foreach (GENFeature genFeature in idfGENFile.Features)
                {
                    // string sourceID = genFeature.ID;

                    //if ((genFeature.ID == "12933") || (genFeature.ID == "12934"))
                    //{
                    //    int a = 0;
                    //}

                    DATRow datRow = idfGENFile.DATFile.GetRow(genFeature.ID);
//                    genFeature.GENFile.DATFile.RemoveRow(sourceID);
//                    genFeature.ID = genFeatureIdx.ToString();
                    if (datRow == null)
                    {
                    //    datRow[0] = genFeature.ID;
                    //    genFeature.GENFile.DATFile.AddRow(datRow);
                    //}
                    //else
                    //{
                        log.AddWarning("No DAT-row found for GEN-feature " + genFeature.ID);
                    }

                    // Add GEN-feature, but use renumbered id, to get sequential id's in resulting GEN-file 
                    genFile.AddFeature(genFeature, true, genFeatureIdx);

                    genFeatureIdx++;
                }
            }

            return true;
        }

        /// <summary>
        /// Convert specified IDF-file to GEN-file with specified settings.HullType 
        /// </summary>
        /// <param name="inputIDFFile">input IDF-file</param>
        /// <param name="featureIdx">integer value with ID for generated GEN-feature</param>
        /// <param name="outputPath">path to write IPF-file when Hull-type is 0</param>
        /// <param name="logIndentLevel"></param>
        /// <returns>GEN-file with (one or more) feature(s), null if no features to convert or </returns>
        protected virtual GENFile Convert(IDFFile inputIDFFile, int featureIdx, string outputPath, int logIndentLevel)
        {
            GENFile idfGENFile = new GENFile();
            AddDATFile(idfGENFile);

            if (settings.HullType == 0)
            {
                List<Point> points = null;
                List<float> values = null;
                RetrieveIDFPoints(inputIDFFile, out points, out values);

                // Just write IPF-file, no GEN-features are added for this option
                string ipfFilename = null;
                if (settings.IsMerged)
                {
                    ipfFilename = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(settings.MergedGENFilename + ".IPF"));
                }
                else
                {
                    ipfFilename = Path.Combine(outputPath, Path.GetFileNameWithoutExtension((settings.OutputFilename != null) ? settings.OutputFilename : inputIDFFile.Filename) + ".IPF");
                }
                WriteIPFFile(ipfFilename, points, values, settings.IsMerged ? Path.GetFileName(inputIDFFile.Filename) : null);
            }
            else if (settings.HullType == 1)
            {
                List<Point> idfPoints = null;
                List<float> values = null;
                RetrieveIDFPoints(inputIDFFile, out idfPoints, out values);
                List<Point> convexHullPoints = GIS.ConvexHull.RetrieveConvexHull(idfPoints);
                if (convexHullPoints != null)
                {
                    GENPolygon genPolygon = new GENPolygon(idfGENFile, 1);
                    genPolygon.Points.AddRange(convexHullPoints);
                    genPolygon.Points.Add(new DoublePoint(convexHullPoints[0].X, convexHullPoints[0].Y));

                    idfGENFile.AddFeature(genPolygon);
                    AddDATRow(idfGENFile, genPolygon.ID, inputIDFFile.Filename, 1, values);
                }
                else if (idfPoints.Count >= 1)
                {
                    // It was not possible to retrieve a convex hull, simply return bounding box of feature
                    Extent idfExtent = inputIDFFile.RetrieveExtent();
                    List<Point> extentPoints = idfExtent.ToPointList();
                    extentPoints.Add(extentPoints[0]);

                    idfGENFile.AddFeature(new GENPolygon(idfGENFile, featureIdx, extentPoints));
                    AddDATRow(idfGENFile, featureIdx.ToString(), inputIDFFile.Filename, 1, values);
                }
                else
                {
                    throw new ToolException("Could not create convex hull for " + idfPoints.Count + " selected cells in IDF-file: " + inputIDFFile.Filename);
                }
            }
            else if (settings.HullType == 2)
            {
                // Convert IDF-file to IPF-points for outer IDF-cells
                IPFFile edgeCellIPFFile = new IPFFile();
                edgeCellIPFFile.AddXYColumns();
                edgeCellIPFFile.AddColumn("value");
                RetrieveOuterIDFCellEdges(inputIDFFile, edgeCellIPFFile);

                int hullPar1 = 3;
                if (!settings.HullPar1.Equals(double.NaN))
                {
                    hullPar1 = (int)settings.HullPar1;
                }
                GENFile tmpGENFile = ConcaveHull.RetrieveConcaveHull(edgeCellIPFFile, hullPar1, 0, log, false);
                idfGENFile.AddFeatures(tmpGENFile.Features);

                List<Point> idfPoints = null;
                List<float> values = null;
                RetrieveIDFPoints(inputIDFFile, out idfPoints, out values);
                AddDATRow(idfGENFile, featureIdx.ToString(), inputIDFFile.Filename, 1, values);
            }
            else if ((settings.HullType == 3) || (settings.HullType == 4) || (settings.HullType == 5))
            {
                List<Point> points = new List<Point>();
                IPFFile cellIPFFile = null;
                if (settings.HullType == 4)
                {
                    cellIPFFile = new IPFFile();
                    cellIPFFile.AddXYColumns();
                    cellIPFFile.AddColumn("value");
                }
                GENFile tmpGENFile = RetrieveOuterIDFCellEdges(inputIDFFile, cellIPFFile);
                if (settings.HullType == 5)
                {
                    tmpGENFile = RemoveIslands(tmpGENFile);
                }
                idfGENFile.AddFeatures(tmpGENFile.Features);

                if (settings.HullType == 4)
                {
                    string ipfFilename = GetResultIPFFilename(outputPath, inputIDFFile.Filename, settings);
                    cellIPFFile.WriteFile(ipfFilename);
                }

                RetrieveValueStatistics(idfGENFile, inputIDFFile);
            }
            else
            {
                throw new ToolException("Unknown hullType: " + settings.HullType);
            }

            return idfGENFile;
        }

        protected virtual string GetResultIPFFilename(string outputPath, string filename, SIFToolSettings settings)
        {
            string ipfFilename = Path.ChangeExtension(Path.Combine(outputPath, (settings.OutputFilename != null) ? settings.OutputFilename : Path.GetFileName(filename)), "IPF");

            return ipfFilename;
        }

        /// <summary>
        /// Add DATFile object to specified GEN-file and add column names
        /// </summary>
        /// <param name="genFile"></param>
        protected virtual void AddDATFile(GENFile genFile)
        {
            genFile.AddDATFile();
            genFile.DATFile.AddColumns(new List<string>() { "SourceFile", "Idx", "Count", "Average", "SD", "Median", "IQR", "Min", "Max" });
        }

        /// <summary>
        /// Add DAT-row with specified id and value statistics to GEN-file
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id"></param>
        /// <param name="sourceFilename"></param>
        /// <param name="index"></param>
        /// <param name="values"></param>
        protected virtual void AddDATRow(GENFile genFile, string id, string sourceFilename, int index, List<float> values)
        {
            Statistics.Statistics stats = new Statistics.Statistics(values);
            stats.ComputeBasicStatistics(false, false, false);
            stats.ComputePercentiles();

            // Remove old row with default empty column values when it exists
            genFile.DATFile.RemoveRow(id);

            genFile.DATFile.AddRow(new DATRow(new string[] {
                id,
                Path.GetFileName(sourceFilename),
                index.ToString(),
                values.Count.ToString(),
                Math.Round(stats.Mean, StatisticsDecimalCount).ToString(SIFTool.EnglishCultureInfo),
                Math.Round(stats.SD, StatisticsDecimalCount).ToString(SIFTool.EnglishCultureInfo),
                Math.Round(stats.Median, StatisticsDecimalCount).ToString(SIFTool.EnglishCultureInfo),
                Math.Round(stats.IQR, StatisticsDecimalCount).ToString(SIFTool.EnglishCultureInfo),
                Math.Round(stats.Min, StatisticsDecimalCount).ToString(SIFTool.EnglishCultureInfo),
                Math.Round(stats.Max, StatisticsDecimalCount).ToString(SIFTool.EnglishCultureInfo) }));
        }

        /// <summary>
        /// Write IPF-file with specified points and values
        /// </summary>
        /// <param name="ipfFilename"></param>
        /// <param name="points"></param>
        /// <param name="values"></param>
        /// <param name="source"></param>
        protected void WriteIPFFile(string ipfFilename, List<Point> points, List<float> values = null, string source = null)
        {
            IPFFile ipfFile = new IPFFile();
            ipfFile.AddXYColumns();
            ipfFile.AddColumn("value");

            if (values == null)
            {
                values = new List<float>();
                for (int pointIdx = 0; pointIdx < points.Count(); pointIdx++)
                {
                    values.Add(pointIdx + 1);
                }
            }

            if (source != null)
            {
                // Add source to points
                ipfFile.AddColumn("Source");
                for (int idx = 0; idx < points.Count; idx++)
                {
                    Point point = points[idx];
                    ipfFile.Points.Add(new IPFPoint(ipfFile, point, new List<string>() { point.XString, point.YString, values[idx].ToString(SIFTool.EnglishCultureInfo), source }));
                }
            }
            else
            {
                for (int idx = 0; idx < points.Count; idx++)
                {
                    Point point = points[idx];
                    ipfFile.Points.Add(new IPFPoint(ipfFile, point, new List<string>() { point.XString, point.YString, values[idx].ToString(SIFTool.EnglishCultureInfo) }));
                }
            }
            ipfFile.WriteFile(ipfFilename);
        }

        /// <summary>
        /// Retrieve list of points for non-NoData cells in specified IDF-file
        /// </summary>
        /// <param name="currentIDFFile"></param>
        /// <param name="points"></param>
        protected void RetrieveIDFPoints(IDFFile currentIDFFile, out List<Point> points)
        {
            points = new List<Point>();
            IDFCellIterator cellIterator = new IDFCellIterator();
            cellIterator.AddIDFFile(currentIDFFile);
            cellIterator.Reset();
            float halfCellWidth = currentIDFFile.XCellsize / 2;
            while (cellIterator.IsInsideExtent())
            {
                float value = cellIterator.GetCellValue(currentIDFFile);
                if (!value.Equals(0) && !value.Equals(currentIDFFile.NoDataValue))
                {
                    float x = cellIterator.X; // +halfCellWidth;
                    float y = cellIterator.Y; // +halfCellWidth;
                    Point point = new DoublePoint(x, y);
                    points.Add(point);
                }
                cellIterator.MoveNext();
            }
        }

        /// <summary>
        /// Retrieve lists of points and values for non-NoData cells in specified IDF-file
        /// </summary>
        /// <param name="currentIDFFile"></param>
        /// <param name="points">list of points with XY-coordinates for each non-NoData cell</param>
        /// <param name="values">list of non-NoData values for all cell's in IDF-file</param>
        protected void RetrieveIDFPoints(IDFFile currentIDFFile, out List<Point> points, out List<float> values)
        {
            points = new List<Point>();
            values = new List<float>();

            IDFCellIterator cellIterator = new IDFCellIterator();
            cellIterator.AddIDFFile(currentIDFFile);
            cellIterator.Reset();
            while (cellIterator.IsInsideExtent())
            {
                float value = cellIterator.GetCellValue(currentIDFFile);
                if (!value.Equals(0) && !value.Equals(currentIDFFile.NoDataValue))
                {
                    float x = cellIterator.X; // +halfCellWidth;
                    float y = cellIterator.Y; // +halfCellWidth;
                    Point point = new DoublePoint(x, y);
                    points.Add(point);
                    values.Add(value);
                }
                cellIterator.MoveNext();
            }
        }

        /// <summary>
        /// Retrieve GEN-file with edges for all outer IDF-cells of specified IDF-file
        /// </summary>
        /// <param name="currentIDFFile">input IDF-file</param>
        /// <param name="resultIPFFile">IPF-file with x, y and value columns for storing IDF-cell values, or null if not needed</param>
        /// <returns></returns>
        protected GENFile RetrieveOuterIDFCellEdges(IDFFile currentIDFFile, IPFFile resultIPFFile = null)
        {
            GENFile genFile = new GENFile();

            Point p1 = null;
            Point p2 = null;
            float noDataValue = currentIDFFile.NoDataValue;
            float xHalfCellSize = currentIDFFile.XCellsize / 2;
            float yHalfCellSize = currentIDFFile.YCellsize / 2;

            // loop through all cells and retrieve all outer edges that do not have a non-NoData-values at the outside
            List<LineSegment> cellEdges = new List<LineSegment>();
            Dictionary<string, List<LineSegment>> pointEdgesDictionary = new Dictionary<string, List<LineSegment>>();
            for (int rowIdx = 0; rowIdx < currentIDFFile.NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < currentIDFFile.NCols; colIdx++)
                {
                    float idfCellValue = currentIDFFile.values[rowIdx][colIdx];
                    if (!idfCellValue.Equals(noDataValue))
                    {
                        float x = currentIDFFile.GetX(colIdx);
                        float y = currentIDFFile.GetY(rowIdx);
                        float[][] neighbourValues = currentIDFFile.GetCellValues(x, y, 1, 2);
                        bool isEnclosed = true;
                        if (neighbourValues[0][1].Equals(noDataValue) || neighbourValues[0][1].Equals(float.NaN))
                        {
                            // No neighbour at top
                            p1 = new DoublePoint(x - xHalfCellSize, y + yHalfCellSize);
                            p2 = new DoublePoint(x + xHalfCellSize, y + yHalfCellSize);
                            SaveSegment(cellEdges, pointEdgesDictionary, p1, p2);
                            isEnclosed = false;
                        }
                        if (neighbourValues[1][2].Equals(noDataValue) || neighbourValues[1][2].Equals(float.NaN))
                        {
                            // No neighbour at right
                            p1 = new DoublePoint(x + xHalfCellSize, y + yHalfCellSize);
                            p2 = new DoublePoint(x + xHalfCellSize, y - yHalfCellSize);
                            SaveSegment(cellEdges, pointEdgesDictionary, p1, p2);
                            isEnclosed = false;
                        }
                        if (neighbourValues[2][1].Equals(noDataValue) || neighbourValues[2][1].Equals(float.NaN))
                        {
                            // No neighbour at bottom
                            p1 = new DoublePoint(x + xHalfCellSize, y - yHalfCellSize);
                            p2 = new DoublePoint(x - xHalfCellSize, y - yHalfCellSize);
                            SaveSegment(cellEdges, pointEdgesDictionary, p1, p2);
                            isEnclosed = false;
                        }
                        if (neighbourValues[1][0].Equals(noDataValue) || neighbourValues[1][0].Equals(float.NaN))
                        {
                            // No neighbour at left
                            p1 = new DoublePoint(x - xHalfCellSize, y - yHalfCellSize);
                            p2 = new DoublePoint(x - xHalfCellSize, y + yHalfCellSize);
                            SaveSegment(cellEdges, pointEdgesDictionary, p1, p2);
                            isEnclosed = false;
                        }
                        if (neighbourValues[0][0].Equals(noDataValue) || neighbourValues[0][0].Equals(float.NaN))
                        {
                            // No neighbour at topleft
                            isEnclosed = false;
                        }
                        if (neighbourValues[0][2].Equals(noDataValue) || neighbourValues[0][2].Equals(float.NaN))
                        {
                            // No neighbour at topright
                            isEnclosed = false;
                        }
                        if (neighbourValues[2][0].Equals(noDataValue) || neighbourValues[2][0].Equals(float.NaN))
                        {
                            // No neighbour at lowerleft
                            isEnclosed = false;
                        }
                        if (neighbourValues[2][2].Equals(noDataValue) || neighbourValues[2][2].Equals(float.NaN))
                        {
                            // No neighbour at lowerright
                            isEnclosed = false;
                        }
                        // WriteGENLines(cellEdges, @"C:\Temp\GENLines.GEN");
                        if (!isEnclosed && (resultIPFFile != null))
                        {
                            resultIPFFile.AddPoint(new IPFPoint(resultIPFFile, new DoublePoint(x, y), new string[] { x.ToString(SIFTool.EnglishCultureInfo), y.ToString(SIFTool.EnglishCultureInfo), idfCellValue.ToString(SIFTool.EnglishCultureInfo) }));
                        }
                    }
                }
            }

            // Now loop trough all edges and create polygons from them
            int polygonIdx = 0;
            GENPolygon currentGENPolygon = new GENPolygon(genFile, polygonIdx.ToString());
            LineSegment firstEdge = cellEdges[0];
            currentGENPolygon.Points.Add(firstEdge.P1);
            currentGENPolygon.Points.Add(firstEdge.P2);
            cellEdges.RemoveAt(0);
            pointEdgesDictionary[firstEdge.P1.ToString()].RemoveAt(0);
            if (pointEdgesDictionary[firstEdge.P1.ToString()].Count == 0)
            {
                pointEdgesDictionary.Remove(firstEdge.P1.ToString());
            }

            Point currentPoint = firstEdge.P2;  // currentPoint is second point of current Edge
            string currentPointString = currentPoint.ToString();
            while (cellEdges.Count > 0)
            {
                if (pointEdgesDictionary.ContainsKey(currentPointString))
                {
                    // An edge was found that starts with the currentPoint
                    LineSegment nextEdge = pointEdgesDictionary[currentPointString][0];

                    // Add edge to polygon
                    currentGENPolygon.Points.Add(nextEdge.P2);

                    // Check if polygon is closed 
                    if (nextEdge.P2.Equals(firstEdge.P1))
                    {
                        cellEdges.Remove(nextEdge);
                        pointEdgesDictionary[currentPointString].RemoveAt(0);
                        if (pointEdgesDictionary[currentPointString].Count == 0)
                        {
                            pointEdgesDictionary.Remove(currentPointString);
                        }

                        // polygon is closed, save and start new one
                        genFile.AddFeature(currentGENPolygon);

                        polygonIdx++;
                        currentGENPolygon = new GENPolygon(genFile, polygonIdx.ToString());

                        if (cellEdges.Count > 0)
                        {
                            firstEdge = cellEdges[0];
                            currentGENPolygon.Points.Add(firstEdge.P1);
                            currentGENPolygon.Points.Add(firstEdge.P2);
                            cellEdges.RemoveAt(0);
                            currentPointString = firstEdge.P1.ToString();
                            pointEdgesDictionary[currentPointString].RemoveAt(0);
                            if (pointEdgesDictionary[currentPointString].Count == 0)
                            {
                                pointEdgesDictionary.Remove(currentPointString);
                            }
                            currentPoint = firstEdge.P2;
                            currentPointString = currentPoint.ToString();
                        }
                    }
                    else
                    {
                        currentPoint = nextEdge.P2;
                        cellEdges.Remove(nextEdge);
                        pointEdgesDictionary[currentPointString].RemoveAt(0);
                        if (pointEdgesDictionary[currentPointString].Count == 0)
                        {
                            pointEdgesDictionary.Remove(currentPointString);
                        }
                        currentPointString = currentPoint.ToString();
                    }
                }
                else
                {
                    genFile.AddFeature(new GENLine(genFile, polygonIdx++, currentGENPolygon.Points));

                    while (cellEdges.Count > 0)
                    {
                        List<Point> points = new List<Point>();
                        points.Add(cellEdges[0].P1);
                        points.Add(cellEdges[0].P2);
                        cellEdges.RemoveAt(0);
                        genFile.AddFeature(new GENLine(genFile, polygonIdx++, points));
                    }
                    // throw new Exception("Invalid geometry");
                }
            }

            if (currentGENPolygon.Points.Count > 0)
            {
                // Add last, current polygon (which should always be closed)
                genFile.AddFeature(currentGENPolygon);
            }

            // Ensure all polygons are in clockwise order. Islands are processed later.
            foreach (GENPolygon genPolygon in genFile.RetrieveGENPolygons())
            {
                if (!GISUtils.IsClockwise(genPolygon.Points))
                {
                    genPolygon.ReversePoints();
                }
            }

            return genFile;
        }

        /// <summary>
        /// Save specified edge segment to list with all edges and to dictionary with edges per p1-point
        /// </summary>
        /// <param name="cellEdges"></param>
        /// <param name="pointEdgesDictionary"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        protected void SaveSegment(List<LineSegment> cellEdges, Dictionary<string, List<LineSegment>> pointEdgesDictionary, Point p1, Point p2)
        {
            string p1String = p1.ToString();
            LineSegment segment = new LineSegment(p1, p2);
            cellEdges.Add(segment);
            if (!pointEdgesDictionary.ContainsKey(p1String))
            {
                pointEdgesDictionary.Add(p1String, new List<LineSegment>());
            }
            pointEdgesDictionary[p1String].Add(segment);
        }

        /// <summary>
        ///  Removes islands. Works only for convex polygons.
        /// </summary>
        /// <param name="srcGENFile"></param>
        /// <returns></returns>
        private GENFile RemoveIslands(GENFile srcGENFile)
        {
            GENFile newGENFile = new GENFile();
            List<GENPolygon> srcGENPolygons = srcGENFile.RetrieveGENPolygons();
            foreach (GENFeature genFeature in srcGENFile.Features)
            {
                if (genFeature is GENPolygon)
                {
                    GENPolygon genPolygon1 = (GENPolygon)genFeature;
                    if (!GISUtils.IsClockwise(genPolygon1.Points))
                    {
                        genPolygon1 = (GENPolygon)genPolygon1.Copy();
                        genPolygon1.ReversePoints();
                    }

                    bool isIsland = false;
                    for (int genPolygonIdx2 = 0; genPolygonIdx2 < srcGENPolygons.Count; genPolygonIdx2++)
                    {
                        GENPolygon genPolygon2 = srcGENPolygons[genPolygonIdx2];

                        if (!GISUtils.IsClockwise(genPolygon2.Points))
                        {
                            genPolygon2 = (GENPolygon)genPolygon2.Copy();
                            genPolygon2.ReversePoints();
                        }

                        if (!genPolygon1.Equals(genPolygon2))
                        {
                            if (genPolygon2.RetrieveExtent().Contains(genPolygon1.RetrieveExtent()))
                            {
                                // extent of current polygon is inside extent of some other polygon, find overlap

                                // first check possible outer polygon (genPolygon2), which may be concave, against extent of polygon1
                                List<GENPolygon> overlapGENPolygons = genPolygon2.ClipPolygon(genPolygon1.RetrieveExtent());
                                if ((overlapGENPolygons != null) && (overlapGENPolygons.Count > 0))
                                {
                                    // Now check against concave hull of genPolygon1. When the clip polygon is concave, the clip will not be correct.
                                    List<Point> convexHull1 = ConvexHull.RetrieveConvexHull(genPolygon1.Points);
                                    List<Point> pointList2 = genPolygon2.Points.ToList();
                                    pointList2.RemoveAt(pointList2.Count - 1);
                                    List<Point> clippedPointList = CSHFClipper.ClipPolygon(pointList2, convexHull1);
                                    if ((clippedPointList != null) && (clippedPointList.Count > 0))
                                    {
                                        isIsland = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    if (!isIsland)
                    {
                        newGENFile.AddFeature(genFeature);
                    }
                }
                else
                {
                    newGENFile.AddFeature(genFeature);
                }
            }

            return newGENFile;
        }

        /// <summary>
        /// retrieve value statistics in each polygon of specified GEN-file and add DAT-file with corresponding statistics
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="currentIDFFile"></param>
        protected void RetrieveValueStatistics(GENFile genFile, IDFFile currentIDFFile)
        {
            float noDataValue = currentIDFFile.NoDataValue;

            // Retrieve values in each polygon for more efficiency: loop through all cells and find polygon
            Dictionary<string, List<float>> polygonDictionary = new Dictionary<string, List<float>>();
            List<Extent> extents = new List<Extent>();
            List<double> areas = new List<double>();
            List<GENPolygon> genPolygons = genFile.RetrieveGENPolygons();
            foreach (GENPolygon genPolygon in genPolygons)
            {
                polygonDictionary.Add(genPolygon.ID, new List<float>());
                areas.Add(genPolygon.CalculateArea());
                extents.Add(genPolygon.RetrieveExtent());
            }

            // loop through all cells and retrieve all outer edges that do not have a non-NoData-values at the outside
            for (int rowIdx = 0; rowIdx < currentIDFFile.NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < currentIDFFile.NCols; colIdx++)
                {
                    float idfCellValue = currentIDFFile.values[rowIdx][colIdx];
                    if (!idfCellValue.Equals(noDataValue))
                    {
                        float x = currentIDFFile.GetX(colIdx);
                        float y = currentIDFFile.GetY(rowIdx);

                        Point point = new FloatPoint(x, y);

                        // Find enclosing polygon in all GEN-polygpons
                        string enclosingPolygonID = null;
                        double currentArea = double.MaxValue;
                        for (int polygonIdx2 = 0; polygonIdx2 < genPolygons.Count; polygonIdx2++)
                        {
                            // First check if point is inside extent
                            if (extents[polygonIdx2].Contains(x, y))
                            {
                                // Now check if point is actually within polygon
                                GENPolygon genPolygon = genPolygons[polygonIdx2];
                                if (point.IsInside(genPolygon.Points))
                                {
                                    double area = areas[polygonIdx2];
                                    if (area < currentArea)
                                    {
                                        enclosingPolygonID = genPolygon.ID;
                                        currentArea = area;
                                    }
                                }
                            }
                        }

                        if (enclosingPolygonID != null)
                        {
                            polygonDictionary[enclosingPolygonID].Add(idfCellValue);
                        }
                        else
                        {
                            throw new ToolException("No enclosing polygon found for point, something went wrong: " + point.ToString());
                        }
                    }
                }
            }

            AddDATFile(genFile);
            for (int genPolygonIdx = 0; genPolygonIdx < genPolygons.Count; genPolygonIdx++)
            {
                GENPolygon genPolygon = genPolygons[genPolygonIdx];

                List<float> values = polygonDictionary[genPolygon.ID];
                // Check order of points: clockwise for normal (outer) polygons, anticlockwise for islands.
                if (values.Count == 0)
                {
                    // Polygon does not contain any values, probably it is an island, check point order
                    if (GISUtils.IsClockwise(genPolygon.Points))
                    {
                        genPolygon.ReversePoints();
                    }
                }
                else if (!GISUtils.IsClockwise(genPolygon.Points))
                {
                    genPolygon.ReversePoints();
                }
                AddDATRow(genFile, genPolygon.ID, currentIDFFile.Filename, genPolygonIdx, values);
            }
        }
    }
}
