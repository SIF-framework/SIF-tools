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
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.IPF
{
    public class IPFCluster
    {
        CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

        public IPFFile IPFFile { get; set; }
        public string ID { get; set; }
        public List<IPFPoint> Points { get; protected set; }
        public IPFTimeseries Timeseries { get; protected set; }
        public float Average { get; protected set; }

        public IPFCluster(IPFFile ipfFile)
        {
            ID = string.Empty;
            Points = new List<IPFPoint>();
            Timeseries = null;
            Average = float.NaN;
        }

        public IPFCluster(IPFFile ipfFile, string id) : this(ipfFile)
        {
            this.ID = id;
        }

        public void AddPoint(IPFPoint point)
        {
            Points.Add(point);
            Timeseries = null;
            Average = float.NaN;
        }

        public void AddPoints(List<IPFPoint> points)
        {
            foreach (IPFPoint point in points)
            {
                if (!this.Points.Contains(point))
                {
                    Points.Add(point);
                }
            }
            Timeseries = null;
            Average = float.NaN;
        }

        public static List<IPFPoint> ClustersToPoints(List<IPFCluster> clusters)
        {
            List<IPFPoint> points = new List<IPFPoint>();
            if (clusters != null)
            {
                foreach (IPFCluster cluster in clusters)
                {
                    foreach (IPFPoint point in cluster.Points)
                    {
                        if (!points.Contains(point))
                        {
                            points.Add(point);
                        }
                    }
                }
            }
            return points;
        }

        /// <summary>
        /// Checks if some points in cluster have timeseries
        /// </summary>
        /// <returns></returns>
        public bool HasTimeseries()
        {
            foreach (IPFPoint point in Points)
            {
                if (point.HasTimeseries())
                {
                    return true;
                }
            }
            return false;
        }

        public void CalculateTimeseries(int avgValueColIdx = -1)
        {
            // First retrieve timeseries
            IPFTimeseries[] tsArray = new IPFTimeseries[Points.Count()];
            for (int pointIdx = 0; pointIdx < Points.Count(); pointIdx++)
            {
                IPFTimeseries ts = null;
                IPFPoint point = Points[pointIdx];
                if (point.HasTimeseries())
                {
                    ts = point.Timeseries;
                }
                else
                {
                    if (avgValueColIdx >= 0)
                    {
                        // Create dummy timeseries
                        List<DateTime> dummyDates = new List<DateTime>();
                        List<float> dummyValues = new List<float>();
                        dummyDates.Add(new DateTime());
                        dummyValues.Add(point.GetFloatValue(avgValueColIdx));
                        ts = new IPFTimeseries(dummyDates, dummyValues);
                    }
                    else
                    {
                        throw new Exception("Could not get timeseries or average value for point: " + point.ToString() + ". Specified columnindex is < 0");
                    }
                }
                tsArray[pointIdx] = ts;
            }

            // Retrieve all dates and sort
            List<DateTime> dates = new List<DateTime>();
            for (int pointIdx = 0; pointIdx < tsArray.Length; pointIdx++)
            {
                foreach (DateTime date in tsArray[pointIdx].Timestamps)
                {
                    if (!dates.Contains(date))
                    {
                        dates.Add(date);
                    }
                }
            }
            dates.Sort();

            // Now retrieve and sum all values in order of dates
            float[] values = new float[dates.Count()];
            for (int pointIdx = 0; pointIdx < tsArray.Length; pointIdx++)
            {
                // loop through all available dates
                IPFTimeseries ts = tsArray[pointIdx];
                for (int dateIdx = 0; dateIdx < dates.Count(); dateIdx++)
                {
                    // Find corresponding value for current point
                    float value = ts.GetValue(dates[dateIdx]);
                    values[dateIdx] += value;
                }
            }

            Timeseries = new IPFTimeseries(dates, new List<float>(values));
        }

        /// <summary>
        /// Creates new IPFPoint object with xy-coordinates of cluster and rest of given ipfPoint object
        /// </summary>
        /// <param name="ipfFile"></param>
        /// <param name="ipfPoint"></param>
        /// <param name="valueColIdx"></param>
        /// <returns></returns>
        public IPFPoint ToPoint(IPFFile ipfFile, IPFPoint ipfPoint, int valueColIdx = -1)
        {
            // Calculate average/center coordinate
            float sumX = 0;
            float sumY = 0;
            foreach (IPFPoint point in Points)
            {
                sumX += (float) point.X;
                sumY += (float) point.Y;
            }
            FloatPoint clusterPoint = new FloatPoint(sumX / Points.Count(), sumY / Points.Count());
            IPFPoint clusterIPFPoint = new IPFPoint(ipfFile, clusterPoint, new string[ipfFile.ColumnCount]);
            clusterIPFPoint.ColumnValues[0] = clusterPoint.XString;
            clusterIPFPoint.ColumnValues[1] = clusterPoint.YString;

            if ((Timeseries != null) && (Timeseries.Timestamps != null) && (ipfFile.AssociatedFileColIdx >= 0))
            {
                clusterIPFPoint.Timeseries = Timeseries;

                if (!this.ID.Equals(string.Empty))
                {
                    clusterIPFPoint.ColumnValues[ipfFile.AssociatedFileColIdx] = this.ID;
                }
                else
                {
                    clusterIPFPoint.ColumnValues[ipfFile.AssociatedFileColIdx] =
                        FileUtils.AddFilePostFix(clusterIPFPoint.ColumnValues[ipfFile.AssociatedFileColIdx], "_cluster");
                }
            }
            else if (valueColIdx >= 0)
            {
                clusterIPFPoint.ColumnValues[valueColIdx] = Average.ToString("F3", englishCultureInfo);
            }

            return clusterIPFPoint;
        }

        public void CalculateAverage(int valueColIdx)
        {
            if (valueColIdx < 0)
            {
                throw new Exception("Specified value column index is negative");
            }

            float sum = 0;
            foreach (IPFPoint point in Points)
            {
                try
                {
                    sum += float.Parse(point.ColumnValues[valueColIdx], englishCultureInfo);
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not parse value in column " + valueColIdx + " of point: " + point.ToString() + ": " + point.ColumnValues[valueColIdx], ex);
                }
            }
            Average = sum / Points.Count();
        }
    }
}
