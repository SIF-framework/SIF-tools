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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;

namespace Sweco.SIF.iMOD.IPF
{
    /// <summary>
    /// Class for storing and processing IPF-points as used in IPFFile objects. 
    /// </summary>
    public class IPFPoint : StringPoint3D, IEquatable<IPFPoint>, IComparer<IPFPoint>
    {
        /// <summary>
        /// IPFFile object that this IPF-point is in
        /// </summary>
        public IPFFile IPFFile { get; set; }

        /// <summary>
        /// Columnvalues for this IPF-point, including X, Y and (optional) Z-values for the first columns
        /// </summary>
        public List<string> ColumnValues
        {
            get { return columnValues; }
            set
            {
                if (value.Count() != IPFFile.ColumnCount)
                {
                    throw new Exception("Invalid number of columnvalues (" + value.Count + ") for columnnames (" + IPFFile.ColumnCount + "): " + value);
                }
                columnValues = value;
            }
        }

        /// <summary>
        /// Optional timeseries for this IPF-point. If lazy loading is used, getting this property value will load the timeseries if defined and not yet loaded. If null, no timeseries is defined.
        /// </summary>
        public IPFTimeseries Timeseries
        {
            get
            {
                if ((timeseries == null) && HasTimeseriesDefined())
                {
                    LoadTimeseries();
                }
                return timeseries;
            }
            set { timeseries = value; }
        }

        private List<string> columnValues;
        private IPFTimeseries timeseries;

        /// <summary>
        /// Creates an IPFPoint instance with the given point object and columnvalues (including x, y (and z) coordinates
        /// </summary>
        /// <param name="ipfFile"></param>
        /// <param name="point"></param>
        /// <param name="columnValues"></param>
        public IPFPoint(IPFFile ipfFile, Point point, List<string> columnValues) : base()
        {
            this.IPFFile = ipfFile;

            this.xString = point.XString;
            this.yString = point.YString;
            if (point is Point3D)
            {
                this.zString = ((Point3D)point).ZString;

                if (columnValues.Count == (ipfFile.ColumnCount - 3))
                {
                    // XYZ-coordinates are not specified in columnvalues, add them here
                    columnValues.InsertRange(0, new List<string>() { xString, yString, zString });
                }
            }
            else
            {
                if (columnValues.Count == (ipfFile.ColumnCount - 2))
                {
                    // Coordinates are not specified in columnvalues, add them here
                    columnValues.InsertRange(0, new List<string>() { xString, yString });
                }
            }

            this.columnValues = columnValues;
            this.timeseries = null;
        }

        /// <summary>
        /// Creates an IPFPoint instance with the given point object and columnvalues (including x, y (and z) coordinates. 
        /// When x, y (and z) coordinates are not included in the columnvalues, the specified point coordinates are used.
        /// </summary>
        /// <param name="ipfFile"></param>
        /// <param name="point"></param>
        /// <param name="columnValues"></param>
        public IPFPoint(IPFFile ipfFile, Point point, string[] columnValues) : this(ipfFile, point, new List<string>(columnValues))
        {
        }

        /// <summary>
        /// Check if column with specified (zero-based) column index contains a float value for this IPF-point
        /// </summary>
        /// <param name="colIdx"></param>
        /// <returns></returns>
        public bool HasFloatValue(int colIdx)
        {
            try
            {
                if (colIdx < columnValues.Count())
                {
                    return (float.TryParse(columnValues[colIdx], System.Globalization.NumberStyles.Any, englishCultureInfo, out float value));
                }
                else
                {
                    return false;
                }
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Retrieve the value of the column with specified (zero-based) column index as a float value
        /// </summary>
        /// <param name="colIdx"></param>
        /// <returns></returns>
        public float GetFloatValue(int colIdx)
        {
            if ((colIdx > 0) && (colIdx < columnValues.Count()))
            {
                try
                {
                    return float.Parse(columnValues[colIdx], englishCultureInfo);
                }
                catch (Exception ex)
                {
                    throw new Exception("Invalid float value at index " + colIdx + " for IPFPoint " + ToString() + ": " + columnValues[colIdx], ex);
                }
            }
            else
            {
                throw new Exception("Invalid column index for IPFPoint " + ToString() + ": " + colIdx);
            }
        }

        /// <summary>
        /// Copy this IPFPoint object as a Point object
        /// </summary>
        /// <returns></returns>
        public override Point Copy()
        {
            return CopyIPFPoint();
        }

        /// <summary>
        /// Copy this IPFPoint object and return result as an IPFPoint object. Note: timeseries object itself is not copied
        /// </summary>
        /// <returns></returns>
        public IPFPoint CopyIPFPoint()
        {
            IPFPoint copiedIPFPoint = new IPFPoint(IPFFile, base.Copy(), CommonUtils.CopyStringList(columnValues));
            copiedIPFPoint.timeseries = this.timeseries;

            return copiedIPFPoint;
        }

        /// <summary>
        /// Checks if a timeseries is present for this IPF-point: 1) an existing timeseries filename is defined or 2) a timeseries is present in memory
        /// </summary>
        /// <returns></returns>
        public bool HasTimeseries()
        {
            return IsTimeseriesLoaded() || HasAssociatedFile();
        }

        /// <summary>
        /// Check if a timeseries is defined for this IPF-point. 
        /// </summary>
        /// <param name="isFloatTXTFilenameAllowed">specify if a float value in the column defined by TextFileColumnIdx is allowed as a valid filename for IPF-timeseries files (default is false)</param>
        /// <returns>true, if a timeseries is defined</returns>
        public bool HasTimeseriesDefined(bool isFloatTXTFilenameAllowed = false)
        {
            // Check for positive TextFileColumnIdx and non-null, non-empty, non-numeric value.  
            return ((IPFFile.AssociatedFileColIdx >= 0) && (IPFFile.AssociatedFileColIdx < ColumnValues.Count) && (columnValues[IPFFile.AssociatedFileColIdx] != null) 
                && !columnValues[IPFFile.AssociatedFileColIdx].Trim().Equals(string.Empty) && (isFloatTXTFilenameAllowed || !HasFloatValue(IPFFile.AssociatedFileColIdx)));
        }

        /// <summary>
        /// Check if a an associated file (e.g. TXT) is defined for this IPF-point
        /// </summary>
        /// <returns></returns>
        public bool HasAssociatedFile()
        {
            if (IPFFile.Filename == null)
            {
                return false;
            }

            string basePath = Path.GetDirectoryName(IPFFile.Filename);
            if (basePath.Equals(string.Empty))
            {
                basePath = Directory.GetCurrentDirectory();
            }

            if (!Directory.Exists(basePath))
            {
                return false;
            }

            if (IPFFile.AssociatedFileColIdx == 0)
            {
                return false;
            }

            if (!((IPFFile.AssociatedFileColIdx > 0) && (IPFFile.AssociatedFileColIdx < columnValues.Count())))
            {
                return false;
            }

            string timeseriesFilename = columnValues[IPFFile.AssociatedFileColIdx] + ".txt";
            if (!Path.IsPathRooted(timeseriesFilename))
            {
                timeseriesFilename = Path.Combine(basePath, timeseriesFilename);
            }
            return File.Exists(timeseriesFilename);
        }

        /// <summary>
        /// Enusre timeseries is loaded if it is available
        /// </summary>
        public void EnsureTimeseriesIsLoaded()
        {
            if (!IsTimeseriesLoaded() && HasAssociatedFile())
            {
                LoadTimeseries();
            }
        }

        /// <summary>
        /// Check if a timeseries is present in memory for this IPF-point, without loading it from file by accessing the Timeseries property.
        /// </summary>
        /// <returns></returns>
        public bool IsTimeseriesLoaded()
        {
            return (timeseries != null);
        }

        /// <summary>
        /// Actually load timeseries data from file into memory
        /// </summary>
        /// <param name="tsIPFFilename">Full filename of IPF-file that defines base path to determine full path of this timeseries, or leave null to retrieve from associated IPF-file</param>
        public void LoadTimeseries(string tsIPFFilename = null)
        {
            string basePath = Path.GetDirectoryName((tsIPFFilename != null) ? tsIPFFilename : IPFFile.Filename);
            if (basePath == null)
            {
                throw new Exception("IPFFile.Filename should be defined when Timeseries should be loaded");
            }
            if (basePath.Equals(string.Empty))
            {
                basePath = Directory.GetCurrentDirectory();
            }
            if (!Directory.Exists(basePath))
            {
                throw new ToolException("IPF-file path not found: " + basePath);
            }
            if (IPFFile.AssociatedFileColIdx <= 0)
            {
                throw new ToolException("Column index for associated filenames is less than or equal to zero, timeseriesfile is not defined for IPF-file " + Path.GetFileName(IPFFile.Filename));
            }
            if (!((IPFFile.AssociatedFileColIdx > 0) && (IPFFile.AssociatedFileColIdx < columnValues.Count())))
            {
                throw new ToolException( "Invalid column index for associated filenames: " + IPFFile.AssociatedFileColIdx + " for IPF-file " + Path.GetFileName(IPFFile.Filename));
            }
            string timeseriesFilename = columnValues[IPFFile.AssociatedFileColIdx];
            if ((timeseriesFilename == null) || timeseriesFilename.Equals(string.Empty))
            {
                return;
                // throw new Exception("No timeseries defined for point " + this.ToString() + " in IPFFile" + Path.GetFileName(ipfFile.Filename));
            }
            timeseriesFilename += "." + IPFFile.AssociatedFileExtension;
            if (!Path.IsPathRooted(timeseriesFilename))
            {
                timeseriesFilename = Path.Combine(basePath, timeseriesFilename);
            }
            if (!File.Exists(timeseriesFilename))
            {
                throw new ToolException("Timeseries file not found: " + timeseriesFilename + " for point " + this.ToString() + " in IPFFile " + Path.GetFileName(IPFFile.Filename));
            }
            this.timeseries = IPFTimeseries.ReadFile(timeseriesFilename, IPFFile.IsCommaCorrectedInTimeseries, IPFFile.SkipTimeseriesNoDataValues);
        }

        /// <summary>
        /// Write timeseries of this IPFPoint to file. Use the filename from the column as as defined by the TextColumn.
        /// </summary>
        public void WriteTimeseries()
        {
            if (timeseries == null)
            {
                throw new Exception("Empty timeseries cannot be written for point: " + ToString());
            }

            string basePath = Path.GetDirectoryName(IPFFile.Filename);
            if (!Directory.Exists(basePath))
            {
                throw new ToolException("IPF-file path not found: " + basePath);
            }
            if (IPFFile.AssociatedFileColIdx == 0)
            {
                throw new ToolException("Column index for associated filenames  is zero, timeseries file is not defined for IPF-file " + Path.GetFileName(IPFFile.Filename));
            }
            if (!((IPFFile.AssociatedFileColIdx > 0) && (IPFFile.AssociatedFileColIdx < columnValues.Count())))
            {
                throw new ToolException("Invalid column index for associated filenames: " + IPFFile.AssociatedFileColIdx + " for IPF-file " + Path.GetFileName(IPFFile.Filename));
            }
            string timeseriesFilename = columnValues[IPFFile.AssociatedFileColIdx];
            if ((timeseriesFilename != null) && !timeseriesFilename.Equals(string.Empty))
            {
                timeseriesFilename = timeseriesFilename.Replace("\"", string.Empty).Replace("'", string.Empty);
                if ((IPFFile.AssociatedFileExtension != null) || !IPFFile.AssociatedFileExtension.Equals(string.Empty))
                {
                    if (!timeseriesFilename.ToLower().EndsWith("." + IPFFile.AssociatedFileExtension.ToLower()))
                    {
                        timeseriesFilename += "." + IPFFile.AssociatedFileExtension;
                    }
                }
                if (!Path.IsPathRooted(timeseriesFilename))
                {
                    timeseriesFilename = Path.Combine(basePath, timeseriesFilename);
                }

                if (!Directory.Exists(Path.GetDirectoryName(timeseriesFilename)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(timeseriesFilename));
                }

                timeseries.WriteFile(timeseriesFilename);
            }
            else
            {
                throw new ToolException("Missing associated filename for IPF-point " + this.ToString() + " with timeseries data in IPF-file " + Path.GetFileName(IPFFile.Filename));
            }
        }

        /// <summary>
        /// Release memory of this (lazy-loadable) object (e.g. for timeseries data)
        /// </summary>
        public void ReleaseMemory()
        {
            timeseries = null;
        }

        /// <summary>
        /// Retrieve average value for this file, either from the corresponding timeseries or use the column value from the column that is defined by the ValueColIdx property.
        /// </summary>
        /// <param name="valueColIdx">index of column to take average value for, or -1 to use associated file index</param>
        /// <returns>average value (or NaN if not present and no timeseries is in memory)</returns>
        private float GetAverageValue(int valueColIdx = -1)
        {
            if (((valueColIdx == -1) || (valueColIdx == this.IPFFile.AssociatedFileColIdx)) && HasTimeseriesDefined())
            {
                if (HasTimeseries())
                {
                    return Timeseries.CalculateAverage();
                }
                else
                {
                    throw new Exception("Cannot calculate average: defined timeseries file does not exist: " + columnValues[IPFFile.AssociatedFileColIdx]);
                }
            }
            else
            {
                if ((valueColIdx > 0) && HasFloatValue(valueColIdx))
                {
                    return GetFloatValue(valueColIdx);
                }
                else
                {
                    throw new Exception("Cannot calculate average: invalid value column index (" + valueColIdx + ") for IPF-point: " + this.ToString());
                }
            }
        }

        /// <summary>
        /// Check is other IPFPoint object is equal to this object by comparing x,y,z coordinates and columnvalues
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IPFPoint other)
        {
            if (!base.Equals(other))
            {
                return false;
            }
            else
            {
                if (this.columnValues.Count == other.columnValues.Count)
                {
                    if (this.columnValues.SequenceEqual(other.columnValues))
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        /// <summary>
        /// Compares two IPFPoint objects: -1 is returned when point1 is null or the coordinates of point1 are smaller, 0 is returned if objects are equal, 1 is returned otherwise
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public int Compare(IPFPoint point1, IPFPoint point2)
        {
            if (point1 == null)
            {
                return -1;
            }
            else if (point2 == null)
            {
                return 1;
            }
            else
            {
                // Compare first to X, and if equal to Y-coordinate
                if (point1.Equals(point2))
                {
                    return 0;
                }
                else if ((Math.Abs(point1.X - point2.X) > 0))
                {
                    return point1.X.CompareTo(point2.X);
                }
                else if ((Math.Abs(point1.Y - point2.Y) > 0))
                {
                    return point1.Y.CompareTo(point2.Y);
                }
                else 
                {
                    return point1.Z.CompareTo(point2.Z);
                }
            }
        }

        /// <summary>
        /// Checks for equality between this and another object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public new bool Equals(object obj)
        {
            if (obj == null)
            {
                return false;
            }
            else if (!(obj is IPFPoint))
            {
                return false;
            }
            else
            {
                IPFPoint other = (IPFPoint)obj;
                return Equals(other);
            }
        }
    }
}
