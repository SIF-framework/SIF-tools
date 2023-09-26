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
using Sweco.SIF.GIS;

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
        /// <param name="isTimeseriesClipped">if true, all date/value columns are clipped to specified period</param>
        /// <param name="srcPointIndices">optional (empty, non-null) list to store indices to selected points in source IPF-file</param>
        /// <returns></returns>
        public static IPFFile SelectPoints(IPFFile sourceIPFFile, DateTime? timeseriesStartDate, DateTime? timeseriesEndDate, int valueColIdx, bool isTimeseriesClipped, List<int> srcPointIndices = null)
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
                        IPFTimeseries clippedIPFTimeseries = (ipfPoint.Timeseries).Select(timeseriesStartDate, timeseriesEndDate, -1);

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

                        // For all of the specified value columns check if number of NoData-values is equal to number of value in specified period
                        bool isPointSelected = true;
                        foreach (int selValueColIdx in selValueColIndices)
                        {
                            List<float> valueList = clippedIPFTimeseries.ValueColumns[selValueColIdx];

                            int noDataCount = clippedIPFTimeseries.RetrieveNoDataCount(selValueColIdx);
                            if (valueList.Count == noDataCount)
                            {
                                isPointSelected = false;
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
    }
}
