// Sweco.SIF.iMOD is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.iMOD.
// 
// Sweco.SIF.iMOD is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.iMOD is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.iMOD. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.IDF;

namespace Sweco.SIF.iMOD.IPF
{
    /// <summary>
    /// Common utilities for processing IPF-files
    /// </summary>
    public class IPFUtils
    {
        /// <summary>
        /// Removed specified IPF-points from the specified IPF-point list
        /// </summary>
        /// <param name="points"></param>
        /// <param name="removedPoints"></param>
        public static void RemovePoints(List<IPFPoint> points, List<IPFPoint> removedPoints)
        {
            foreach (IPFPoint point in removedPoints)
            {
                points.Remove(point);
            }
        }

        /// <summary>
        /// Select IPFPoints from list that are within specified extent
        /// </summary>
        /// <param name="points"></param>
        /// <param name="selectionExtent"></param>
        /// <returns></returns>
        public static List<IPFPoint> SelectPoints(List<IPFPoint> points, Extent selectionExtent)
        {
            List<IPFPoint> selection = new List<IPFPoint>();
            foreach (IPFPoint point in points)
            {
                if (point.IsContainedBy(selectionExtent))
                {
                    selection.Add(point);
                }
            }
            return selection;
        }

        /// <summary>
        /// Selects all IPF-points with non-NoData values in specified period for specified valuecolumn(s).
        /// </summary>
        /// <param name="sourceIPFFile"></param>
        /// <param name="timeseriesStartDate">start date/time of selection period</param>
        /// <param name="timeseriesEndDate">end date/time of selection period</param>
        /// <param name="valueColIdx">column index in associated file to check, or -1 to check all columns</param>
        /// <param name="isRemoved">if false, points are not removed (all points are selected), but timeseries may be clipped</param>
        /// <param name="isTimeseriesClipped">if true, all date/value columns are clipped to specified period</param>
        /// <param name="srcPointIndices">optional (empty, non-null) list to store indices to selected points in source IPF-file</param>
        /// <returns></returns>
        public static IPFFile SelectPoints(IPFFile sourceIPFFile, DateTime? timeseriesStartDate, DateTime? timeseriesEndDate, int valueColIdx, bool isRemoved, bool isTimeseriesClipped, List<int> srcPointIndices = null)
        {
            IPFFile newIPFFile = new IPFFile();
            newIPFFile.CopyProperties(sourceIPFFile);

            if (srcPointIndices == null)
            {
                // When no list is specified, Create dummy list to speed up inner loop, actual list contents will not be returned as list is a value parameter
                srcPointIndices = new List<int>();
            }

            if (timeseriesStartDate != null)
            {
                DateTime startDate = (DateTime)timeseriesStartDate;

                List<IPFPoint> ipfPoints = sourceIPFFile.Points;
                for (int pointIdx = 0; pointIdx < sourceIPFFile.PointCount; pointIdx++)
                {
                    IPFPoint ipfPoint = ipfPoints[pointIdx];

                    if (ipfPoint.HasTimeseries())
                    {
                        // Select values in specified period. Here, always select/clip all value columns for this period
                        IPFTimeseries clippedIPFTimeseries = ipfPoint.Timeseries.Select(timeseriesStartDate, timeseriesEndDate, -1);

                        // Now check for non-NoData-values only for specified value column(s)
                        List<int> selValueColIndices = new List<int>();
                        if (valueColIdx == -1)
                        {
                            for (int colIdx = 0; colIdx < clippedIPFTimeseries.ValueColumns.Count; colIdx++)
                            {
                                selValueColIndices.Add(colIdx);
                            }
                        }
                        else
                        {
                            selValueColIndices.Add(valueColIdx);
                        }

                        // For all of the specified value columns check if number of NoData-values is equal to number of values in specified period
                        bool isPointSelected = true;
                        if (isRemoved)
                        {
                            // Remove points without dates in specified period
                            foreach (int selValueColIdx in selValueColIndices)
                            {
                                List<float> valueList = clippedIPFTimeseries.ValueColumns[selValueColIdx];

                                int noDataCount = clippedIPFTimeseries.RetrieveNoDataCount(selValueColIdx);
                                if (valueList.Count == noDataCount)
                                {
                                    isPointSelected = false;
                                }
                            }
                        }

                        if (isPointSelected)
                        {
                            if (!srcPointIndices.Contains(pointIdx))
                            {
                                if (isTimeseriesClipped)
                                {
                                    ipfPoint.Timeseries.Timestamps = clippedIPFTimeseries.Timestamps;
                                    ipfPoint.Timeseries.ValueColumns = clippedIPFTimeseries.ValueColumns;
                                }

                                newIPFFile.AddPoint(ipfPoint);
                                srcPointIndices.Add(pointIdx);
                            }
                        }
                    }
                }
            }

            return newIPFFile;
        }

        /// <summary>
        /// Selects all IPF-points that are inside or outside the specified levels (depending on selectionmethod). 
        /// </summary>
        /// <param name="sourceIPFFile"></param>
        /// <param name="topLevelIDFFile"></param>
        /// <param name="botLevelIDFFile"></param>
        /// <param name="selectPointMethod"></param>
        /// <param name="srcPointIndices">optional (empty, non-null) list to store indices to selected points in source IPF-file</param>
        /// <returns></returns>
        public static IPFFile SelectPoints(IPFFile sourceIPFFile, IDFFile topLevelIDFFile, IDFFile botLevelIDFFile = null, SelectPointMethod selectPointMethod = SelectPointMethod.Inside, List<int> srcPointIndices = null)
        {
            float maxTopLevelValue = (topLevelIDFFile != null) ? topLevelIDFFile.MaxValue : float.NaN;
            float minBotLevelValue = (botLevelIDFFile != null) ? botLevelIDFFile.MinValue : float.NaN;

            if (srcPointIndices == null)
            {
                // When no list is specified, Create dummy list to speed up inner loop, actual list contents will not be returned as list is a value parameter
                srcPointIndices = new List<int>();
            }

            IPFFile newIPFFile = new IPFFile();
            newIPFFile.CopyProperties(sourceIPFFile);

            List<IPFPoint> ipfPoints = sourceIPFFile.Points;
            for (int pointIdx = 0; pointIdx < sourceIPFFile.PointCount; pointIdx++)
            {
                IPFPoint ipfPoint = ipfPoints[pointIdx];

                float ipfPointZ = (float)ipfPoint.Z;
                if (ipfPointZ.Equals(float.NaN))
                {
                    throw new Exception("Z-coordinate is not defined, SelectPoints with levels is not possible");
                }

                if (!(ipfPointZ > maxTopLevelValue) && !(ipfPointZ < minBotLevelValue))
                {
                    float topValue = (topLevelIDFFile != null) ? topLevelIDFFile.GetValue((float)ipfPoint.X, (float)ipfPoint.Y) : float.NaN;
                    float botValue = (botLevelIDFFile != null) ? botLevelIDFFile.GetValue((float)ipfPoint.X, (float)ipfPoint.Y) : float.NaN;
                    // Select points between specified levels
                    if (!(ipfPointZ > topValue) && !(ipfPointZ < botValue)) // Use inverse expressions for top and bot to cope with possible NaN-value
                    {
                        if (selectPointMethod == SelectPointMethod.Inside)
                        {
                            if (!srcPointIndices.Contains(pointIdx))
                            {
                                newIPFFile.AddPoint(ipfPoint);
                                srcPointIndices.Add(pointIdx);
                            }
                        }
                    }
                    else
                    {
                        if (selectPointMethod == SelectPointMethod.Outside)
                        {
                            newIPFFile.AddPoint(ipfPoint);
                            srcPointIndices.Add(pointIdx);
                        }
                    }
                }
                else
                {
                    if (selectPointMethod == SelectPointMethod.Outside)
                    {
                        if (!srcPointIndices.Contains(pointIdx))
                        {
                            newIPFFile.AddPoint(ipfPoint);
                            srcPointIndices.Add(pointIdx);
                        }
                    }
                }
            }

            return newIPFFile;
        }

        /// <summary>
        /// Selects all IPF-points that are inside/outside the specified extent (depending on selectionmethod).
        /// </summary>
        /// <param name="sourceIPFFile"></param>
        /// <param name="extent"></param>
        /// <param name="selectMethod"></param>
        /// <param name="srcPointIndices">optional (empty, non-null) list to store indices to selected points in source IPF-file</param>
        /// <returns></returns>
        public static IPFFile SelectPoints(IPFFile sourceIPFFile, Extent extent, SelectPointMethod selectMethod = SelectPointMethod.Inside, List<int> srcPointIndices = null)
        {
            IPFFile newIPFFile = new IPFFile();
            newIPFFile.CopyProperties(sourceIPFFile);

            if (srcPointIndices == null)
            {
                // When no list is specified, Create dummy list to speed up inner loop, actual list contents will not be returned as list is a value parameter
                srcPointIndices = new List<int>();
            }

            List<IPFPoint> ipfPoints = sourceIPFFile.Points;
            for (int pointIdx = 0; pointIdx < sourceIPFFile.PointCount; pointIdx++)
            {
                IPFPoint ipfPoint = ipfPoints[pointIdx];

                bool isContained = ipfPoint.IsContainedBy(extent);
                if ((isContained && (selectMethod == SelectPointMethod.Inside)) || (!isContained && (selectMethod == SelectPointMethod.Outside)))
                {
                    if (!srcPointIndices.Contains(pointIdx))
                    {
                        newIPFFile.AddPoint(ipfPoint);
                        srcPointIndices.Add(pointIdx);
                    }
                }
            }

            return newIPFFile;
        }

        /// <summary>
        /// Selects all points that are inside/outside the specified polygons
        /// </summary>
        /// <param name="sourceIPFFile"></param>
        /// <param name="genFile"></param>
        /// <param name="selectMethod">specify if specified points should be inside/outside the specified extent</param>
        /// <param name="srcPointIndices">optional (empty, non-null) list to store indices to selected points in source IPF-file</param>
        /// <param name="isSourceColAdded">if true, two columns with a reference to the source are added: index and ID</param>
        /// <returns>List of selected particle numbers</returns>
        public static IPFFile SelectPoints(IPFFile sourceIPFFile, GENFile genFile, SelectPointMethod selectMethod = SelectPointMethod.Inside, List<int> srcPointIndices = null, bool isSourceColAdded = true)
        {
            IPFFile newIPFFile = new IPFFile();
            newIPFFile.CopyProperties(sourceIPFFile);

            if (isSourceColAdded)
            {
                newIPFFile.AddColumn(sourceIPFFile.FindUniqueColumnName("SourcePolygonIndex"), "0");
                newIPFFile.AddColumn(sourceIPFFile.FindUniqueColumnName("SourcePolygonId"));
            }

            List<GENPolygon> genPolygons = genFile.RetrieveGENPolygons();

            if (srcPointIndices == null)
            {
                // When no list is specified, Create dummy list to speed up inner loop, actual list contents will not be returned as list is a value parameter
                srcPointIndices = new List<int>();
            }

            List<IPFPoint> ipfPoints = sourceIPFFile.Points;
            for (int genPolygonIdx = 0; genPolygonIdx < genPolygons.Count; genPolygonIdx++)
            {
                GENPolygon genPolygon = genPolygons[genPolygonIdx];
                string genPolygonId = IPFUtils.RemoveListSeperators(genPolygon.ID);
                Extent boundingBox = new Extent(genPolygon.Points);

                for (int pointIdx = 0; pointIdx < sourceIPFFile.PointCount; pointIdx++)
                {
                    IPFPoint ipfPoint = ipfPoints[pointIdx];

                    // First do fast check for proximity of point to polygon
                    if (ipfPoint.IsContainedBy(boundingBox))
                    {
                        // Point is inside bounding box, do further checks to see if point is inside/outside polygon
                        bool isContainedByPolygon = ipfPoint.IsInside(genPolygon.Points);
                        if ((isContainedByPolygon && (selectMethod == SelectPointMethod.Inside)) || (!isContainedByPolygon && (selectMethod == SelectPointMethod.Outside)))
                        {
                            if (!srcPointIndices.Contains(pointIdx))
                            {
                                srcPointIndices.Add(pointIdx);
                                IPFPoint ipfPointCopy = ipfPoint.CopyIPFPoint();
                                if (isSourceColAdded)
                                {
                                    ipfPointCopy.ColumnValues.Add((genPolygonIdx + 1).ToString());
                                    ipfPointCopy.ColumnValues.Add(genPolygonId);
                                }
                                newIPFFile.AddPoint(ipfPointCopy);
                            }
                        }
                    }
                    else if ((selectMethod == SelectPointMethod.Outside))
                    {
                        // point is outside bounding box. If points outside the polygon have to be selected, this point should be added
                        if (!srcPointIndices.Contains(pointIdx))
                        {
                            srcPointIndices.Add(pointIdx);
                            IPFPoint ipfPointCopy = ipfPoint.CopyIPFPoint();
                            if (isSourceColAdded)
                            {
                                ipfPointCopy.ColumnValues.Add((genPolygonIdx + 1).ToString());
                                ipfPointCopy.ColumnValues.Add(genPolygonId);
                            }
                            newIPFFile.AddPoint(ipfPointCopy);
                        }
                    }
                }
            }

            return newIPFFile;
        }

        /// <summary>
        /// Selects all points that are in the specified IDF-file cells as defined by the zoneValues.
        /// </summary>
        /// <param name="sourceIPFFile"></param>
        /// <param name="zoneIDFFile"></param>
        /// <param name="zoneValues"></param>
        /// <param name="selectMethod">specify if specified points should be inside/outside the specified extent</param>
        /// <param name="srcPointIndices">optional (empty, non-null) list to store indices to selected points in source IPF-file</param>
        /// <param name="isSourceColAdded">if true, two columns with a reference to the source are added: index and ID</param>
        /// <returns>List of selected particle numbers</returns>
        public static IPFFile SelectPoints(IPFFile sourceIPFFile, IDFFile zoneIDFFile, List<float> zoneValues, SelectPointMethod selectMethod, List<int> srcPointIndices = null, bool isSourceColAdded = true)
        {
            IPFFile newIPFFile = new IPFFile();
            newIPFFile.CopyProperties(sourceIPFFile);

            if (isSourceColAdded)
            {
                newIPFFile.AddColumn(sourceIPFFile.FindUniqueColumnName("SourcePolygonIndex"), "0");
                newIPFFile.AddColumn(sourceIPFFile.FindUniqueColumnName("SourcePolygonId"));
            }

            if (srcPointIndices == null)
            {
                // When no list is specified, Create dummy list to speed up inner loop, actual list contents will not be returned as list is a value parameter
                srcPointIndices = new List<int>();
            }

            List<IPFPoint> ipfPoints = sourceIPFFile.Points;
            for (int pointIdx = 0; pointIdx < sourceIPFFile.PointCount; pointIdx++)
            {
                IPFPoint ipfPoint = ipfPoints[pointIdx];

                float ipfPointZoneValue = zoneIDFFile.GetValue((float)ipfPoint.X, (float)ipfPoint.Y);

                bool isInsideZone = false;
                foreach (float zoneValue in zoneValues)
                {
                    if (ipfPointZoneValue.Equals(zoneValue))
                    {
                        isInsideZone = true;
                        break;
                    }
                }

                // First do fast check for proximity of point to polygon
                if (isInsideZone)
                {
                    if (selectMethod == SelectPointMethod.Inside)
                    {
                        if (!srcPointIndices.Contains(pointIdx))
                        {
                            srcPointIndices.Add(pointIdx);
                            IPFPoint ipfPointCopy = ipfPoint.CopyIPFPoint();
                            if (isSourceColAdded)
                            {
                                ipfPointCopy.ColumnValues.Add(ipfPoint.ToString());
                                ipfPointCopy.ColumnValues.Add(ipfPointZoneValue.ToString(IMODFile.EnglishCultureInfo));
                            }
                            newIPFFile.AddPoint(ipfPointCopy);
                        }
                    }
                }
                else if ((selectMethod == SelectPointMethod.Outside))
                {
                    // point is outside bounding box. If points outside the polygon have to be selected, this point should be added
                    if (!srcPointIndices.Contains(pointIdx))
                    {
                        srcPointIndices.Add(pointIdx);
                        IPFPoint ipfPointCopy = ipfPoint.CopyIPFPoint();
                        if (isSourceColAdded)
                        {
                            ipfPointCopy.ColumnValues.Add(ipfPoint.ToString());
                            ipfPointCopy.ColumnValues.Add(ipfPointZoneValue.ToString(IMODFile.EnglishCultureInfo));
                        }
                        newIPFFile.AddPoint(ipfPointCopy);
                    }
                }
            }

            return newIPFFile;
        }

        /// <summary>
        /// Remove all specified listseperators from given string (replace by empty strings)
        /// </summary>
        /// <param name="someString"></param>
        /// <param name="listSeperators">if null default IPF-listseperators are used</param>
        /// <returns></returns>
        public static string RemoveListSeperators(string someString, string listSeperators = null)
        {
            if (listSeperators == null)
            {
                listSeperators = IPFFile.DefaultListSeperators;
            }
            for (int charIdx = 0; charIdx < listSeperators.Length; charIdx++)
            {
                someString = someString.Replace(listSeperators[charIdx].ToString(), string.Empty);
            }
            return someString;
        }

        /// <summary>
        /// Check for equal associated filenames in both IPF-files, which can be a risk. For equal filenames postfix #i is added to associated filenames of IPF-file 2.
        /// For modified filenames it is necessary that the corresponding timeseries is loaded into memory; which is also performed in this method.
        /// </summary>
        /// <param name="ipfFile1"></param>
        /// <param name="ipfFile2"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        public static void FixEqualTSFilenames(IPFFile ipfFile1, IPFFile ipfFile2, Log log, int logIndentLevel)
        {
            HashSet<string> sourceFilenames = new HashSet<string>();
            int addedTXTColIdx = ipfFile2.AssociatedFileColIdx;
            int sourceTXTColIdx = ipfFile1.AssociatedFileColIdx;
            if ((sourceTXTColIdx >= 0) && (addedTXTColIdx >= 0))
            {
                foreach (IPFPoint ipfPoint1 in ipfFile1.Points)
                {
                    string txtFilename = ipfPoint1.ColumnValues[sourceTXTColIdx];
                    sourceFilenames.Add(txtFilename);
                }

                foreach (IPFPoint ipfPoint2 in ipfFile2.Points)
                {
                    string txtFilename = ipfPoint2.ColumnValues[addedTXTColIdx];
                    if (sourceFilenames.Contains(txtFilename))
                    {
                        // IPFPoint 2 has the same associated filename as a point in IPF-file 1
                        // If necessasry, load timeseries and add postfix to associated filename of IPFpoint 2
                        ipfPoint2.LoadTimeseries();
                        int seqNr = 2;
                        string txtFilename2 = FileUtils.AddFilePostFix(txtFilename, "#" + seqNr);
                        while (sourceFilenames.Contains(txtFilename2))
                        {
                            txtFilename2 = FileUtils.AddFilePostFix(txtFilename, "#" + ++seqNr);
                        }
                        ipfPoint2.ColumnValues[addedTXTColIdx] = txtFilename2;
                        ipfPoint2.Timeseries.Filename = txtFilename2;

                        log.AddWarning("Point in IPF-file 2 has same associated filename as point in IPF-file 1: " + txtFilename + ".TXT", logIndentLevel);
                        log.AddInfo("Associated filename for point in IPF-file 2 is renamed to: " + txtFilename2 + ipfFile1.AssociatedFileExtension, logIndentLevel + 1);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Defined if points are selected inside or outside selection
    /// </summary>
    public enum SelectPointMethod
    {
        Undefined,
        Inside,
        Outside
    }
}
