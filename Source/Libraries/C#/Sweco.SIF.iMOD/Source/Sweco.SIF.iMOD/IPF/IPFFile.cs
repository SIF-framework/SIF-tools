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
using Sweco.SIF.iMOD.Legends;

namespace Sweco.SIF.iMOD.IPF
{
    /// <summary>
    /// Class to read, process and write iMOD IPF-files. See iMOD-manual for details of IPF-files: https://oss.deltares.nl/nl/web/imod/user-manual.
    /// </summary>
    public class IPFFile : IMODFile, IEquatable<IPFFile>
    {
        /// <summary>
        /// Definition of empty column value 
        /// </summary>
        public const string EmptyValue = "\"\"";

        /// <summary>
        /// Default file extension for associated filenames, which is used when not defined in IPF-file
        /// </summary>
        public const string DefaultAssociatedFileExtension = "TXT";

        /// <summary>
        /// User defined list seperators that are used for an IPF-file before trying the standard list seperators: comma, semicolon, tab and space
        /// </summary>
        public static string UserDefinedListSeperators { get; set; } = null;

        /// <summary>
        /// Default list seperators that are tried in the given order when reading an IPF-file. 
        /// It consists of the User defined list seperators combined with the standard list seperators: comma, semicolon, tab and space
        /// </summary>
        public static string DefaultListSeperators
        {
            get { return AddStandardListSeperators(UserDefinedListSeperators); }
        }

        /// <summary>
        /// File extension of this iMOD-file without dot-prefix
        /// </summary>
        public override string Extension
        {
            get { return "IPF"; }
        }

        /// <summary>
        /// List seperator that was used between columnnames/columnvalues when this IPF-file was read, or null if it was not read from a file.
        /// The list seperator is selected from the DefaultListSeperators property and the one that fits columnnames and values is used.
        /// </summary>
        public char? ListSeperatorRead { get; set; }

        /// <summary>
        /// List seperator that will be used between columnnames/columnvalues when this IPF-file is written. 
        /// </summary>
        public char ListSeperatorWrite { get; set; }

        /// <summary>
        /// Columnnames for the data of each IPFPoint in this IPF-file
        /// </summary>
        public List<string> ColumnNames { get; set; }

        /// <summary>
        /// Number of columns in this IPF-file
        /// </summary>
        public int ColumnCount
        {
            get { return ColumnNames.Count; }
        }

        /// <summary>
        /// Zero-based index of column with name of associated file, or -1 if not used.
        /// Note: in the IPF-file itself (so before reading or after writing) a one-based column number is used.
        /// </summary>
        public int AssociatedFileColIdx { get; set; }

        /// <summary>
        /// File extension of associated files, e.g. TXT
        /// Note: in the IPF-file itself (so before reading or after writing) a one-based column number is used.
        /// </summary>
        public string AssociatedFileExtension { get; set; }

        /// <summary>
        /// List of IPF-points that are currently in memory for this IPF-file
        /// </summary>
        protected List<IPFPoint> points;

        /// <summary>
        /// List of IPF-points for this IPF-file
        /// </summary>
        public List<IPFPoint> Points
        {
            get
            {
                if (points != null)
                {
                    return points;
                }
                else
                {
                    if (UseLazyLoading)
                    {
                        this.ReadIPFFile(true);
                    }
                    return points;
                }
            }
        }

        /// <summary>
        /// Number of IPF-points that are in memory for this IPFPoint object
        /// </summary>
        public int PointCount
        {
            get { return (points != null) ? points.Count() : pointCount; }
        }

        /// <summary>
        /// Type of file, used for IPF-files with timevariant information. See iMOD-manual for a description.
        /// Currently not used in SIF, just preserved when present in IPF-files.
        /// </summary>
        public int ITYPE { get; set; }

        /// <summary>
        /// Optional logfile to write warnings that occur during reading or processing of the IPF-file
        /// </summary>
        public Log Log { get; set; }

        /// <summary>
        /// Indent level for added log messages
        /// </summary>
        public int LogIndentLevel { get; set; }

        /// <summary>
        /// Specifies if lazy loading is used when reading IPF-files: only column definitions are read at first, the actual pointdata is only loaded when first used.
        /// </summary>
        public override bool UseLazyLoading { get; set; }

        /// <summary>
        /// Specify if commas (dutch decimal seperator, replace by english decimal seperator) in values should be corrected when list seperator seems to be a space
        /// </summary>
        public bool IsCommaCorrectedInTimeseries { get; set; }

        /// <summary>
        /// Specify if warning should be given for mismatches in number of columnnames and columnvalues with current listseperator (ListSeperatorRead)
        /// </summary>
        public static bool IsWarnedForColumnMismatch { get; set; } = true;

        /// <summary>
        /// Specify if warning should be given when adding points that are already existing (based on coordinate) in this IPF-file.
        /// </summary>
        public static bool IsWarnedForExistingPoints { get; set; } = false;

        /// <summary>
        /// Number of decimals for floating point values when writing IPF-files, use -1 for all decimals
        /// </summary>
        public static int DecimalCount { get; set; } = -1;

        /// <summary>
        /// Number of IPF-points as defined in the IPF file with the filename that is currently defined for this object. -1 is used if file is not existing yet. 
        /// Note: this may differ from the number of points in memory and is used to implement lazy loading for IPF-files.
        /// </summary>
        private int pointCount;

        /// <summary>
        /// Specifies if quote correction is used for this IPF-file when (lazy) loading IPF-file data. If true, listseperators within quoted strings in IPF-files are correctly parsed, but slows parsing and reading IPF-file.
        /// </summary>
        private bool IsQuoteCorrected = false;

        /// <summary>
        /// Specifies which (zero-based) column index is used for the x-coordinate of this IPF-file when (lazy) loading IPF-file data
        /// </summary>
        private int XColIdx;

        /// <summary>
        /// Specifies which (zero-based) column index is used for the y-coordinate of this IPF-file when (lazy) loading IPF-file data
        /// </summary>
        private int YColIdx;

        /// <summary>
        /// Specifies which (zero-based) column index is used for the z-coordinate of this IPF-file when (lazy) loading IPF-file data, use -1 if not defined
        /// </summary>
        private int ZColIdx;

        /// <summary>
        /// Creates empty IPFFile object
        /// </summary>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        public IPFFile(Log log = null, int logIndentLevel = 0)
            : base()
        {
            Initialize(log, logIndentLevel);
        }

        /// <summary>
        /// Initialize new, empty IPFPoint object
        /// </summary>
        private void Initialize(Log log = null, int logIndentLevel = 0)
        {
            this.AssociatedFileColIdx = -1;
            this.UseLazyLoading = false;
            this.ColumnNames = new List<string>();
            this.ITYPE = 0;
            this.ListSeperatorRead = null;
            this.ListSeperatorWrite = ',';
            this.points = new List<IPFPoint>();
            this.pointCount = -1;   // No points loaded in memory
            this.Log = log;
            this.LogIndentLevel = logIndentLevel;
            this.IsQuoteCorrected = false;
            this.XColIdx = 0;
            this.YColIdx = 1;
            this.ZColIdx = 2;
        }

        /// <summary>
        /// Reads an IPF-file into memory. The decimalseperator is assumed to be a point ('.'). 
        /// The used list seperator is the first one from DefaultListSeperators that matches with number of columns and column values in first row, and is stored in the ListSeperatorRead property.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="useLazyLoading">if true, point data is only loaded when actually referenced</param>
        /// <param name="useQuoteCorrection">if true, listseperators within quotes strings are correctly parsed, slows parsing</param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <param name="clipExtent"></param>
        /// <returns></returns>
        public static IPFFile ReadFile(string filename, bool useLazyLoading = false, bool useQuoteCorrection = false, Log log = null, int logIndentLevel = 0, Extent clipExtent = null)
        {
            return ReadFile(filename, 0, 1, 2, useLazyLoading, useQuoteCorrection, log, logIndentLevel, clipExtent);
        }

        /// <summary>
        /// Reads an IPF-file into memory. The decimalseperator is assumed to be a point ('.'). 
        /// The used list seperator is the first one from DefaultListSeperators that matches with number of columns and column values in first row, and is stored in the ListSeperatorRead property.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="xColIdx">zero-based column index of x-coordinate</param>
        /// <param name="yColIdx">zero-based column index of y-coordinate</param>
        /// <param name="useLazyLoading">if true, point data is only loaded when actually referenced</param>
        /// <param name="useQuoteCorrection">if true, listseperators within quotes strings are correctly parsed, slows parsing</param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <param name="clipExtent"></param>
        /// <returns></returns>
        public static IPFFile ReadFile(string filename, int xColIdx, int yColIdx, bool useLazyLoading = false, bool useQuoteCorrection = false, Log log = null, int logIndentLevel = 0, Extent clipExtent = null)
        {
            return ReadFile(filename, xColIdx, yColIdx, -1, useLazyLoading, useQuoteCorrection, log, logIndentLevel, clipExtent);
        }

        /// <summary>
        /// Reads an IPF-file into memory. The decimalseperator is assumed to be a point ('.'). 
        /// The used list seperator is the first one from DefaultListSeperators that matches with number of columns and column values in first row, and is stored in the ListSeperatorRead property.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="xColIdx"></param>
        /// <param name="yColIdx"></param>
        /// <param name="zColIdx">if -1, z-coordinate is not read/used</param>
        /// <param name="useLazyLoading">if true, point data is only loaded when actually referenced</param>
        /// <param name="isQuoteCorrected">if true, listseperators within quotes strings are correctly parsed, slows parsing</param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <param name="clipExtent"></param>
        /// <returns></returns>
        public static IPFFile ReadFile(string filename, int xColIdx, int yColIdx, int zColIdx, bool useLazyLoading = false, bool isQuoteCorrected = false, Log log = null, int logIndentLevel = 0, Extent clipExtent = null)
        {
            IPFFile ipfFile = new IPFFile(log, logIndentLevel);
            ipfFile.Filename = filename;
            ipfFile.UseLazyLoading = useLazyLoading;
            ipfFile.IsQuoteCorrected = isQuoteCorrected;
            ipfFile.XColIdx = xColIdx;
            ipfFile.YColIdx = yColIdx;
            ipfFile.ZColIdx = zColIdx;

            if (clipExtent != null)
            {
                // Specify the modified extent, which will force clipping after reading the IPF-file 
                ipfFile.modifiedExtent = clipExtent.Copy();
            }
            ipfFile.ReadIPFFile(!useLazyLoading);

            return ipfFile;
        }

        /// <summary>
        /// Reads IPF file with defined filename and into memory. The decimalseperator is assumed to be a point ('.')
        /// The used list seperator is the first one from DefaultListSeperators that matches with number of columns and column values in first row, and is stored in the ListSeperatorRead property.
        /// If the first line does not contain an integer, CSV-format is tried with the defined DefaultListSeperators.
        /// </summary>
        /// <param name="arePointsLoaded">specifies that the pointdata (including timeseries) should be loaded now</param>
        /// <returns></returns>
        private void ReadIPFFile(bool arePointsLoaded)
        {
            Stream stream = null;
            StreamReader sr = null;
            string line = null;

            try
            {
                if ((Filename == null) || Filename.Equals(string.Empty) || !File.Exists(Filename))
                {
                    throw new ToolException("IPF-file doesn't exist: " + Filename);
                }

                stream = File.OpenRead(Filename);
                sr = new StreamReader(stream);

                // Parse first line with number of points
                line = sr.ReadLine().Trim();
                if (!int.TryParse(line, out pointCount))
                {
                    // IPF-file does not have standard format with optional associated files, try CSV-format with default list seperators
                    ReadCSVFile(Filename, null, IsQuoteCorrected, XColIdx, YColIdx, ZColIdx, Log, LogIndentLevel);
                    return;
                }

                // Parse second line with number of columns and optional ITYPE-value
                int columnCount;
                try
                {
                    line = sr.ReadLine().Trim();
                    string[] lineValues = line.Split(DefaultListSeperators.ToCharArray());
                    if (lineValues.Length == 1)
                    {
                        columnCount = ParseInt(line);
                    }
                    else if (lineValues.Length == 2)
                    {
                        columnCount = ParseInt(lineValues[0]);
                        ITYPE = ParseInt(lineValues[1]);
                    }
                    else
                    {
                        throw new ToolException("Unexpected number of values in NFIELDS,ITYPE-line: " + line);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not read number of columns (or optional ITYPE) in line \"" + line + "\" for IPF-file " + Filename, ex);
                }

                // Read columnnames
                this.ColumnNames.Clear();
                if (columnCount > 0)
                {
                    for (int i = 0; i < columnCount; i++)
                    {
                        this.AddColumn(RemoveIPFLineComment(sr.ReadLine().Trim()));
                    }
                }
                CheckColumnNames();

                // Check that columnindices for x- and y-coordinates are less than number of columns
                if (XColIdx >= this.ColumnCount)
                {
                    throw new ToolException("Specified x-columnindex (" + XColIdx + ") is larger than number of columns (" + this.ColumnCount + ")");
                }
                if (YColIdx >= this.ColumnCount)
                {
                    throw new ToolException("Specified y-columnindex (" + YColIdx + ") is larger than number of columns (" + this.ColumnCount + ")");
                }

                // retrieve TXT-columnindex
                string associatedFileDefString = string.Empty;
                try
                {
                    associatedFileDefString = sr.ReadLine();
                }
                catch (Exception ex)
                {
                    throw new ToolException("Could not read line with associated file index at line \"" + line + "\" for IPF-file " + Filename, ex);
                }

                ParseAssociatedFileDef(associatedFileDefString, DefaultListSeperators, out int associatedFileColIdx, out string associatedFileExtension);
                if (associatedFileColIdx >= columnCount)
                {
                    throw new ToolException("One-based index (" + (associatedFileColIdx + 1) + ") of column with associated filename is larger than defined number of columns (" + columnCount + ")");
                }
                this.AssociatedFileColIdx = associatedFileColIdx;
                this.AssociatedFileExtension = associatedFileExtension;

                // Read points from file
                this.ReadPoints(sr, arePointsLoaded, IsQuoteCorrected, XColIdx, YColIdx, ZColIdx);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while reading " + Filename, ex);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
                if (stream != null)
                {
                    stream.Close();
                }
            }
        }

        /// <summary>
        /// Read point data from StreamReader to IPF-file, assume header with columnnames has been read. 
        /// The used list seperator is the first one from DefaultListSeperators that matches with number of columns and column values in first row, and is stored in the ListSeperatorRead property.
        /// </summary>
        /// <param name="sr"></param>
        /// <param name="arePointsLoaded">if true, points are actually loaded into points field</param>
        /// <param name="isQuoteCorrected"></param>
        /// <param name="xColIdx"></param>
        /// <param name="yColIdx"></param>
        /// <param name="zColIdx">use -1 to not read z-coordinate</param>
        /// <param name="pointCount">number of points to read or -1 to read until end of file</param>
        private void ReadPoints(StreamReader sr, bool arePointsLoaded, bool isQuoteCorrected, int xColIdx, int yColIdx, int zColIdx, int pointCount = -1)
        {
            int columnCount = ColumnNames.Count;
            bool hasZCoordinate = HasZColumn() && zColIdx >= 0;
            ListSeperatorRead = null;
            char listSeperator = char.MinValue;

            if (!arePointsLoaded)
            {
                this.points = null;
            }
            else
            {
                // Actually read and check point data
                points = new List<IPFPoint>();

                int pointIdx = 0;
                while (!sr.EndOfStream && ((pointCount == -1) || (pointIdx < pointCount)))
                {
                    if (!sr.EndOfStream)
                    {
                        // Read next line and remove whitespace
                        string pointLine = RemoveIPFLineComment(sr.ReadLine()).Trim();
                        while (pointLine.Contains("  "))
                        {
                            pointLine = pointLine.Replace("  ", " ");
                        }

                        // For the first point, find a valid listseperator that fits current line
                        if (ListSeperatorRead == null)
                        {
                            listSeperator = FindListSeperator(pointLine, columnCount, DefaultListSeperators, IsWarnedForColumnMismatch);
                            ListSeperatorRead = listSeperator;
                        }

                        // Split current line with listseperator
                        string[] columnValues = null;
                        if (pointLine.Contains("\""))
                        {
                            if (isQuoteCorrected)
                            {
                                columnValues = CommonUtils.SplitQuoted(pointLine, listSeperator, '\"', true, true);
                            }
                            else
                            {
                                pointLine = pointLine.Replace("\"", string.Empty);
                                columnValues = pointLine.Split(listSeperator);
                            }
                        }
                        else
                        {
                            columnValues = pointLine.Split(listSeperator);
                        }

                        if (columnValues != null)
                        {

                            if (IsWarnedForColumnMismatch && (columnValues.Length != columnCount))
                            {
                                string msg = "Invalid number of columnvalues for point " + (pointIdx + 1) + " in IPF-file " + Path.GetFileName(Filename) + ": "
                                    + "\r\n" + columnCount + " columnvalues expected, found: " + pointLine
                                    + "\r\nListseperator determined for this IPF-file is '" + listSeperator.ToString() + "'. Valid listseperators: '" + GetListSeperatorsString(DefaultListSeperators) + "'";
                                if (Log != null)
                                {
                                    Log.AddWarning(msg, LogIndentLevel);
                                }
                                else
                                {
                                    throw new Exception(msg);
                                }
                            }

                            string x = columnValues[xColIdx];
                            string y = columnValues[yColIdx];
                            string z = "0";

                            Point point = null;
                            if (hasZCoordinate)
                            {
                                z = columnValues[zColIdx];
                                point = new StringPoint3D(x, y, z);
                            }
                            else
                            {
                                point = new StringPoint(x, y);
                            }

                            // Add point with its columnvalues
                            IPFPoint ipfPoint = new IPFPoint(this, point, columnValues);
                            AddPoint(ipfPoint);
                        }
                        else
                        {
                            if ((pointCount != -1) && (Log != null))
                            {
                                Log.AddWarning("EOF: " + pointIdx + "/" + this.pointCount + " processed, pointnr " + (pointIdx + 1) + " not found: " + Path.GetFileName(Filename), LogIndentLevel);
                            }
                        }
                    }
                    else
                    {
                        // empty line, ignore
                    }

                    pointIdx++;
                }
                if ((pointCount != -1) && (pointIdx != pointCount))
                {
                    throw new ToolException("Number of points in IPF-file (" + pointIdx + ") is less than number of points as defined in header (" + pointCount + ")");
                }

                this.pointCount = pointCount;
                this.IsQuoteCorrected = isQuoteCorrected;
                this.XColIdx = xColIdx;
                this.YColIdx = yColIdx;
                this.ZColIdx = zColIdx;
                this.extent = CalculateExtent();
                this.fileExtent = this.extent.Copy();

                if ((this.modifiedExtent != null) && !this.modifiedExtent.Contains(this.Extent))
                {
                    // Clipping has been defined/used for this IPFFile. Data may have been released in the meantime as part of lazy loading mechanism, therefore perform clip (again) now
                    IPFFile clippedIPFFile = ClipIPF(modifiedExtent);
                    this.pointCount = clippedIPFFile.pointCount;
                    this.MaxValue = clippedIPFFile.MaxValue;
                    this.MinValue = clippedIPFFile.MinValue;
                    this.points = clippedIPFFile.points;
                    this.extent = modifiedExtent.Copy();
                    this.modifiedExtent = modifiedExtent.Copy();
                    // leave file extent to its current value
                }
            }
        }

        /// <summary>
        /// Reads a CSV-formatted IPF-file into memory. Double quotes are removed. English notation is used for parsing values.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="listSeperator">if single list seperator symbol, or null to try DefaultListSeperators</param>
        /// <param name="useQuoteCorrection">if true, listseperators within quotes strings are correctly parsed, slows parsing</param>
        /// <param name="xColIdx"></param>
        /// <param name="yColIdx"></param>
        /// <param name="zColIdx">-1 if not used</param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        public static IPFFile ReadCSVFile(string filename, char? listSeperator, bool useQuoteCorrection = false, int xColIdx = 0, int yColIdx = 1, int zColIdx = 2, Log log = null, int logIndentLevel = 0)
        {
            IPFFile ipfFile = null;
            Stream stream = null;
            StreamReader sr = null;
            string line = null;
            char[] listSeperators = (listSeperator == null) ? DefaultListSeperators.ToCharArray() : new char[] { ((char)listSeperator) };
            char selectedListSeperator;

            try
            {
                if (!File.Exists(filename))
                {
                    throw new ToolException("CSV-file doesn't exist: " + filename);
                }

                ipfFile = new IPFFile();
                ipfFile.Filename = filename;
                ipfFile.AssociatedFileColIdx = -1;

                stream = File.OpenRead(filename);
                sr = new StreamReader(stream);

                line = sr.ReadLine().Trim();

                // Parse first line with columnnames and select list seperator that results in more than 2 columns, otherwise first list seperator is used
                string[] columnNames = null;
                int listseperatorIdx = 0;
                do
                {
                    selectedListSeperator = listSeperators[listseperatorIdx];
                    columnNames = CommonUtils.SplitQuoted(line, selectedListSeperator, '\"', true, true);
                } while ((columnNames.Length < 2) && (++listseperatorIdx < listSeperators.Length));
                if (listseperatorIdx == listSeperators.Length)
                {
                    selectedListSeperator = listSeperators[0];
                    columnNames = CommonUtils.SplitQuoted(line, selectedListSeperator, '\"', true, true);
                }

                if (columnNames != null)
                {
                    for (int i = 0; i < columnNames.Length; i++)
                    {
                        ipfFile.AddColumn(columnNames[i]);
                    }
                }
                ipfFile.CheckColumnNames();

                // Use ReadPoints() method from IPFFile since there is no difference once columns have been read. List seperator is searched again in there, and must match number of columns.
                ipfFile.ReadPoints(sr, true, useQuoteCorrection, xColIdx, yColIdx, zColIdx);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while reading " + filename + ". Check seperators!", ex);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
                if (stream != null)
                {
                    stream.Close();
                }
            }

            return ipfFile;
        }

        /// <summary>
        /// Adds first obligatory columns for IPF-point coordinates
        /// </summary>
        /// <param name="xColumnName"></param>
        /// <param name="yColumnName"></param>
        /// <param name="zColumnName"></param>
        public void AddXYColumns(string xColumnName = "X", string yColumnName = "Y", string zColumnName = null)
        {
            AddColumn(xColumnName);
            AddColumn(yColumnName);
            if (zColumnName != null)
            {
                AddColumn(zColumnName);
            }
        }

        /// <summary>
        /// Adds a column with the specified name behind the existing columns. Existing points will get the optionally specified default value
        /// </summary>
        /// <param name="columnNames"></param>
        /// <param name="defaultColumnValues"></param>
        public void AddColumns(List<string> columnNames, List<string> defaultColumnValues = null)
        {
            for (int idx = 0; idx < columnNames.Count; idx++)
            {
                string columnName = columnNames[idx];
                string defaultColumnValue = ((defaultColumnValues != null) && (defaultColumnValues.Count > idx)) ? defaultColumnValues[idx] : "";
                AddColumn(columnName, defaultColumnValue);
            }
        }

        /// <summary>
        /// Adds a column with the specified name behind the existing columns. Existing points will get the optionally specified default value
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="defaultColumnValue">value to add for all points, when number of column values is less than number of column names</param>
        public void AddColumn(string columnName, string defaultColumnValue = "")
        {
            // If columns exist already check if an X- or Y-column is defined, otherwise add XY-columns first
            if (ColumnCount > 0)
            {
                if (!ColumnNames[0].ToUpper().Contains("X"))
                {
                    throw new ToolException("IPF-file is missing an X-column: " + (Filename ?? string.Empty));
                }
            }
            if (ColumnCount > 1)
            {
                if (!ColumnNames[1].ToUpper().Contains("Y"))
                {
                    throw new ToolException("IPF-file is missing an Y-column: " + (Filename ?? string.Empty));
                }
            }
            if (ColumnCount == 0)
            {
                if (!columnName.ToUpper().Contains("X"))
                {
                    throw new ToolException("First IPF-column should contain an 'X', added columnname '" + columnName + "' is not valid for IPF-file: " + (Filename ?? string.Empty));
                }
            }
            else if (ColumnCount == 1)
            {
                if (!columnName.ToUpper().Contains("Y"))
                {
                    throw new ToolException("First two IPF-columns should contain 'X' and 'Y', added columnname '" + columnName + "' is not valid for IPF-file: " + (Filename ?? string.Empty));
                }
            }

            if (!ColumnNames.Contains(columnName))
            {
                ColumnNames.Add(columnName);
            }
            else
            {
                throw new ToolException("Added columnname already exists in IPF-file: " + columnName);
            }

            if (points != null)
            {
                foreach (IPFPoint ipfPoint in points)
                {
                    if (ipfPoint.ColumnValues.Count < ColumnNames.Count)
                    {
                        ipfPoint.ColumnValues.Add(defaultColumnValue);
                    }
                }
            }
        }

        /// <summary>
        /// Removes a column with the specified index. For existing points the corresponding value will be removed as well
        /// </summary>
        /// <param name="colIdx"></param>
        public void RemoveColumn(int colIdx)
        {
            if (colIdx >= ColumnNames.Count)
            {
                throw new Exception("To be removed column index is equal to or higher than number of columns");
            }

            ColumnNames.RemoveAt(colIdx);
            foreach (IPFPoint ipfPoint in Points)
            {
                if (colIdx < ipfPoint.ColumnValues.Count)
                {
                    ipfPoint.ColumnValues.RemoveAt(colIdx);
                }
            }
        }

        /// <summary>
        /// Determine equality up to the level of the filename
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IPFFile other)
        {
            return base.Equals(other);
        }

        /// <summary>
        /// Retrieves index of first point with xy-coordinates and column values equal to specified IPFPoint object
        /// </summary>
        /// <param name="ipfPoint"></param>
        /// <returns></returns>
        public int IndexOf(IPFPoint ipfPoint)
        {
            for (int i = 0; i < Points.Count; i++)
            {
                IPFPoint thisIPFPoint = Points[i];
                if (ipfPoint.Equals(thisIPFPoint))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Retrieve index of first point with xy-coordinates equal to specified Point object
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public int IndexOf(Point point)
        {
            for (int i = 0; i < Points.Count; i++)
            {
                Point ipfPoint = Points[i];
                if (point.Equals(ipfPoint))
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Retrieve point at specified (zero-based) index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IPFPoint GetPoint(int index)
        {
            if (index < Points.Count)
            {
                return points[index];
            }
            return null;
        }

        /// <summary>
        /// Retrieves first IPFPoint with coordinates and columnvalues equal to specified IPFPoint object
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private IPFPoint GetPoint(IPFPoint point)
        {
            int index = IndexOf(point);
            if (index >= 0)
            {
                return Points[index];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve first IPFPoint with coordinates equal to specified Point object
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private IPFPoint GetPoint(Point point)
        {
            int index = IndexOf(point);
            if (index >= 0)
            {
                return Points[index];
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// Retrieve points within given distance from specified x and y-coordinates
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="maxDistance"></param>
        /// <returns>List of selected points or null if no points were selected</returns>
        public List<IPFPoint> GetPoints(float x, float y, double maxDistance)
        {
            List<IPFPoint> selectedPoints = new List<IPFPoint>();
            for (int i = 0; i < this.points.Count; i++)
            {
                if (points[i].GetDistance(x, y) < maxDistance)
                {
                    selectedPoints.Add(points[i]);
                }
            }
            return (selectedPoints.Count > 0) ? selectedPoints : null;
        }

        /// <summary>
        /// Adds a single point to this IPF-file. Existing points are not added and a warning is given when IsWarnedForExistingPoints property is set.
        /// </summary>
        /// <param name="point"></param>
        public void AddPoint(IPFPoint point)
        {
            if (!Points.Contains(point))
            {
                points.Add(point);
            }
            else if (IsWarnedForExistingPoints)
            {
                if (Log != null)
                {
                    Log.AddWarning("Point (" + point.ToString() + " is already existing in IPF-file '" + Path.GetFileName(this.Filename) + "'", LogIndentLevel);
                }
            }
        }

        /// <summary>
        /// Adds one mor more points to this IPF-file. Existing points are not added and a warning is given when IsWarnedForExistingPoints property is set.
        /// </summary>
        /// <param name="points"></param>
        public void AddPoints(ICollection<IPFPoint> points)
        {
            foreach (IPFPoint point in points)
            {
                AddPoint(point);
            }
        }

        /// <summary>
        /// Removed specified IPF-point from the set of points in this IPF-file
        /// </summary>
        /// <param name="removedPoint"></param>
        public void RemovePoint(IPFPoint removedPoint)
        {
            Points.Remove(removedPoint);
        }

        /// <summary>
        /// Removed specified IPF-points from the set of points in this IPF-file
        /// </summary>
        /// <param name="removedPoints"></param>
        public void RemovePoints(List<IPFPoint> removedPoints)
        {
            foreach (IPFPoint point in removedPoints)
            {
                Points.Remove(point);
            }
        }

        /// <summary>
        /// Finds zero-based columnindex of specified columnname. If not found -1 is returned.
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="isMatchWhole">use true to match only whole words</param>
        /// <param name="isMatchCase">use true to match case</param>
        /// <returns>zero-based columnindex or -1 if not found</returns>
        public int FindColumnName(string columnName, bool isMatchWhole = false, bool isMatchCase = false)
        {
            int colIdx = -1;

            for (colIdx = 0; colIdx < ColumnNames.Count(); colIdx++)
            {
                string ipfColumnname = ColumnNames[colIdx];
                if (!isMatchCase)
                {
                    ipfColumnname = ipfColumnname.ToLower();
                    columnName = columnName.ToLower();
                }
                if (isMatchWhole)
                {
                    if (ipfColumnname.Equals(columnName))
                    {
                        return colIdx;
                    }
                }
                else if (ipfColumnname.Contains(columnName))
                {
                    return colIdx;
                }
            }
            return -1;
        }

        /// <summary>
        /// Find unique name for specified columnname by adding a sequencenumber starting with 2
        /// </summary>
        /// <param name="initialColumnName"></param>
        /// <returns></returns>
        public string FindUniqueColumnName(string initialColumnName)
        {
            int idx = 2;
            string colname = initialColumnName;
            while (FindColumnIndex(colname, false) >= 0)
            {
                colname = initialColumnName + idx;
                idx++;
            }
            return colname;
        }

        /// <summary>
        /// Finds zero-based columnindex of specified column string, which is either a columnname or a column index. 
        /// If the given string contains an integer number, this number is returned as integer index.
        /// If not found -1 is returned.
        /// </summary>
        /// <param name="columnNameOrIdx"></param>
        /// <param name="isMatchWhole"></param>
        /// <param name="isMatchCase"></param>
        /// <returns>zero-based columnindex or -1 if not found</returns>
        protected int FindColumnIndex(string columnNameOrIdx, bool isMatchWhole = false, bool isMatchCase = false)
        {
            int colIdx = -1;
            if (int.TryParse(columnNameOrIdx, out colIdx))
            {
                return (colIdx >= 0) ? colIdx : -1;
            }

            return FindColumnName(columnNameOrIdx, isMatchWhole, isMatchCase);
        }

        /// <summary>
        /// Retrieves string array with columnvalues for point at specified index, excluding x/y(/z)-values
        /// </summary>
        /// <param name="index"></param>
        /// <returns>string array with columnvalues or null for unexisting index/point</returns>
        public List<string> GetColumnValues(int index)
        {
            if (index < PointCount)
            {
                IPFPoint ipfPoint = Points[index];
                if (ipfPoint != null)
                {
                    return ipfPoint.ColumnValues;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns true if this IDF-file has values at or between the given min and max values
        /// </summary>
        /// <param name="valueColIdx">Index of column to check for values</param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns>An exception is thrown if column values are not numeric</returns>
        public bool HasValues(int valueColIdx, float minValue, float maxValue)
        {
            if (valueColIdx >= 0)
            {
                foreach (IPFPoint ipfPoint in Points)
                {
                    float value = ipfPoint.GetFloatValue(valueColIdx);
                    if ((value >= minValue) && (value <= maxValue))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// Write this IPFFile object and specified metadata to filename as defined in the IPFFile object. Double quotes are added around values with spaces.
        public override void WriteFile(Metadata metadata = null)
        {
            if (this.Filename != null)
            {
                WriteFile(metadata);
            }
            else
            {
                throw new Exception("Could not write IPF-file, no filename is defined");
            }
        }

        /// <summary>
        /// Determine equality up to the level of the contents
        /// </summary>
        /// <param name="otherIMODFile"></param>
        /// <param name="comparedExtent"></param>
        /// <param name="isNoDataCompared"></param>
        /// <param name="isContentComparisonForced"></param>
        /// <returns></returns>
        public override bool HasEqualContent(IMODFile otherIMODFile, Extent comparedExtent, bool isNoDataCompared, bool isContentComparisonForced = false)
        {
            if (!(otherIMODFile is IPFFile))
            {
                return false;
            }

            IPFFile otherIPFFile = (IPFFile)otherIMODFile;
            if (!isContentComparisonForced && this.Equals(otherIPFFile))
            {
                return true;
            }

            IPFFile extentBaseIPFFile = this;
            IPFFile extentOtherIPFFile = otherIPFFile;
            if (comparedExtent != null)
            {
                extentBaseIPFFile = this.ClipIPF(comparedExtent);
                extentOtherIPFFile = otherIPFFile.ClipIPF(comparedExtent);
            }

            if (extentBaseIPFFile.PointCount != extentOtherIPFFile.PointCount)
            {
                return false;
            }

            if (extentBaseIPFFile.ColumnCount != extentOtherIPFFile.ColumnCount)
            {
                return false;
            }

            foreach (IPFPoint ipfPoint in extentBaseIPFFile.Points)
            {
                // First check if point is also present in other IPF-file
                if (!extentOtherIPFFile.Points.Contains(ipfPoint))
                {
                    return false;
                }
                else
                {
                    // check contents for this point
                    List<string> baseColumnStrings = ipfPoint.ColumnValues;
                    List<string> otherColumnStrings = extentOtherIPFFile.GetPoint(ipfPoint).ColumnValues;
                    if (!baseColumnStrings.SequenceEqual<string>(otherColumnStrings))
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Write this IPFFile object to specified filename. Double quotes are added around values with spaces. The Filename property of this object is set to the new filname. 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="metadata"></param>
        public override void WriteFile(string filename, Metadata metadata = null)
        {
            WriteFile(filename, metadata, true);
        }

        /// <summary>
        /// Write this IPFFile object to filename as defined in the IPFFile object. Double quotes are added around values with spaces.
        /// </summary>
        /// <param name="isTimeseriesWritten">if true, timeseries data is also written to seperate txt-files</param>
        public void WriteFile(bool isTimeseriesWritten)
        {
            WriteFile(null, isTimeseriesWritten);
        }

        /// <summary>
        /// Write this IPFFile object and specified metadata to filename as defined in the IPFFile object. Double quotes are added around values with spaces.
        /// Double quotes are around for values with spaces
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="isTimeseriesWritten">if true, timeseries data is also written to seperate txt-files</param>
        public void WriteFile(Metadata metadata = null, bool isTimeseriesWritten = true)
        {
            if (this.Filename != null)
            {
                WriteFile(this.Filename, metadata, isTimeseriesWritten);
            }
            else
            {
                throw new Exception("Could not write IPF, no filename specified");
            }
        }

        /// <summary>
        /// Write this IPFFile object to specified filename. Double quotes are added around values that contain any of the defined list seperators of this IPF-file object. 
        /// The first defined listseperator character is used as list seperator for writing this IPF-file. The Filename property of this object is set to the new filename. 
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="metadata"></param>
        /// <param name="isTimeseriesWritten">if true, an available timeseries is written to disk as well as a seperate txt-file</param>
        public void WriteFile(string filename, Metadata metadata = null, bool isTimeseriesWritten = true)
        {
            string sourceIPFFilename = this.Filename;
            StreamWriter sw = null;
            try
            {
                if ((filename == null) || filename.Equals(string.Empty))
                {
                    throw new Exception("No filename specified for IDFFile.WriteFile()");
                }
                if (!Path.GetDirectoryName(filename).Equals(string.Empty) && !Directory.Exists(Path.GetDirectoryName(filename)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filename));
                }

                if (Path.GetExtension(filename).Equals(string.Empty))
                {
                    filename += ".IPF";
                }

                // Create empty file
                sw = new StreamWriter(filename);

                // Write column definitions
                sw.WriteLine(points.Count);
                if (ITYPE > 0)
                {
                    sw.WriteLine(ColumnCount + ListSeperatorWrite.ToString() + ITYPE);
                }
                else
                {
                    sw.WriteLine(ColumnCount);
                }
                for (int i = 0; i < ColumnCount; i++)
                {
                    sw.WriteLine(ColumnNames[i]);
                }

                // Write column definition for associated files
                if ((AssociatedFileExtension == null) || AssociatedFileExtension.Equals(string.Empty))
                {
                    AssociatedFileExtension = DefaultAssociatedFileExtension;
                }

                sw.WriteLine((AssociatedFileColIdx + 1) + ListSeperatorWrite.ToString() + AssociatedFileExtension);

                // Write points
                if (points != null)
                {
                    foreach (IPFPoint ipfPoint in points)
                    {
                        WriteIPFPointRecord(sw, ipfPoint, ListSeperatorWrite);
                    }
                }

                this.Filename = filename;

                // Update pointCount
                pointCount = points.Count;
            }
            catch (IOException ex)
            {
                if (ex.Message.ToLower().Contains("access") || ex.Message.ToLower().Contains("toegang"))
                {
                    throw new ToolException(Extension + "-file cannot be written, because it is being used by another process: " + filename);
                }
                else
                {
                    throw new Exception("Unexpected error while writing " + Extension + "-file: " + filename, ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while writing IPF-file: " + filename, ex);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }

            // Write metadata if defined
            if (metadata != null)
            {
                // force metadata to refer to this IDF-file
                metadata.IMODFilename = filename;
                metadata.METFilename = null;
                metadata.Type = Extension;
                metadata.Resolution = "-";
                metadata.WriteMetaFile();
            }

            // Write timeseries data to TXT-files if present
            if (isTimeseriesWritten)
            {
                string ipfFileFilename = Filename.ToLower();
                foreach (IPFPoint point in Points)
                {
                    if (point.HasTimeseries())
                    {
                        // Check if filename of IPFFile that is administrated for points is equal to filename of this IPF-file. If not associated file has to be written relative to this IPFFile.
                        bool isIPFFilenameModified = ((sourceIPFFilename != null) && !ipfFileFilename.Equals(sourceIPFFilename.ToLower())) || ((point.IPFFile.Filename != null) && !point.IPFFile.Filename.ToLower().Equals(ipfFileFilename));

                        // If timeseries is in memory it may have been changed and will be written as well, otherwise file is not written again unless location of IPFFile is changed.
                        bool isTimeseriesLoaded = point.IsTimeseriesLoaded();
                        if (isTimeseriesLoaded || isIPFFilenameModified)
                        {
                            if (isIPFFilenameModified && !isTimeseriesLoaded)
                            {
                                // This situation will occur when lazy loading is used
                                try
                                {
                                    point.LoadTimeseries();
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception("Could not load timeseries for point " + point.ToString() + ", IPF-file " + Path.GetFileName(this.Filename), ex);
                                }
                            }

                            // Reset IPF-file of this point, which fill ensure relative paths for associated files will lead to new (possibly changed) IPF-file location
                            point.IPFFile = this;

                            // Now write timeseries to new location. Note absolute paths for associated file will cause old associated files to be overwritten...
                            point.WriteTimeseries();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Writes an IPF-file and generates seperate TXT-files with the specified levels (e.g. filter levels) to define intervals. The Filename property of this object is set to the new filname.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="idColIdx">columm index of ID, which is used as the filename for the generated level txt-files</param>
        /// <param name="subIdColIdx">optional columm index of subID, which is added to the id, seperated by an underscore ('_'), or -1 if not used</param>
        /// <param name="levelColIndices">column indices for the actual levels that should be used for the intervals in the txt-files</param>
        /// <param name="addedDistances">relative distance to be added to each specified level, use zero values or null to ignore</param>
        /// <param name="intervalCodes">code letters for each interval, this should be one less than the number of levelColIndices</param>
        /// <param name="metadata"></param>
        public void WriteLevelFile(string filename, int idColIdx, int subIdColIdx, List<int> levelColIndices, List<string> intervalCodes,
            List<float> addedDistances = null, Metadata metadata = null)
        {
            if (idColIdx >= ColumnCount)
            {
                throw new ToolException("IPFFile.WriteLevelFile error: idColIdx (" + idColIdx + ") is equal to or higher than column count (" + ColumnCount + ")");
            }

            if (subIdColIdx >= ColumnCount)
            {
                throw new ToolException("IPFFile.WriteLevelFile error: subIdColIdx (" + subIdColIdx + ") is equal to or higher than column count (" + ColumnCount + ")");
            }

            if (levelColIndices.Count != intervalCodes.Count + 1)
            {
                throw new ToolException("IPFFile.WriteLevelFile error: count of levelColIndices should be one more than count of intervalCodes (" + levelColIndices.Count + " vs. " + intervalCodes + ")");
            }

            int levelIdx = 0;
            while (levelIdx < levelColIndices.Count)
            {
                if (levelColIndices[levelIdx] < 0)
                {
                    throw new ToolException("IPFFile.WriteLevelFile: levelColIndex is below zero at index " + levelIdx + ": " + levelColIndices[levelIdx].ToString());
                }
                if (levelColIndices[levelIdx] >= this.ColumnCount)
                {
                    throw new ToolException("IPFFile.WriteLevelFile: specified IPFFile (" + Path.GetFileName(this.Filename) + ") has less columns (" + ColumnCount + ") than specified in levelColIndices-list at index " + levelIdx + ": " + levelColIndices[levelIdx].ToString());
                }
                levelIdx++;
            }

            this.AddColumn("LevelFilename");

            StreamWriter sw = null;
            try
            {
                if ((filename == null) || filename.Equals(string.Empty))
                {
                    throw new ToolException("No filename specified for IDFFile.WriteFile()");
                }
                if (!Directory.Exists(Path.GetDirectoryName(filename)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filename));
                }
                sw = new StreamWriter(filename);

                // Write column definitions. Note write number of points that is actually in memory
                sw.WriteLine(points.Count);
                sw.WriteLine(ColumnCount);
                for (int i = 0; i < this.ColumnCount; i++)
                {
                    sw.WriteLine(this.ColumnNames[i]);
                }

                // Write textcolumn definition
                sw.WriteLine(ColumnCount + ",TXT");

                // Write points
                if (this.points != null)
                {
                    foreach (IPFPoint ipfPoint in this.points)
                    {
                        string id = ipfPoint.ColumnValues[idColIdx];
                        if ((subIdColIdx > 0) && !id.Contains("_"))
                        {
                            id += "_" + ipfPoint.ColumnValues[subIdColIdx];
                        }
                        string txtFilenameWithoutExtension = Path.Combine(Path.GetFileNameWithoutExtension(filename), id);
                        try
                        {
                            ipfPoint.ColumnValues[ColumnCount - 1] = txtFilenameWithoutExtension;
                            WriteIPFPointRecord(sw, ipfPoint, ListSeperatorWrite);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Could not write IPF point record: " + txtFilenameWithoutExtension, ex);
                        }
                    }
                }

                this.Filename = filename;

                // Update pointCount
                pointCount = points.Count;
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while writing IPF-file", ex);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }

            if (metadata != null)
            {
                // force metadata to refer to this IDF-file
                metadata.IMODFilename = filename;
                metadata.METFilename = null;
                metadata.Type = Extension;
                metadata.Resolution = "-";
                metadata.WriteMetaFile();
            }

            // Now write all levelfiles 
            foreach (IPFPoint point in Points)
            {
                string id = point.ColumnValues[idColIdx];
                if ((subIdColIdx > 0) && !id.Contains("_"))
                {
                    id += "_" + point.ColumnValues[subIdColIdx];
                }
                try
                {
                    string txtFilenameWithoutExtension = Path.Combine(Path.GetDirectoryName(filename), Path.Combine(Path.GetFileNameWithoutExtension(filename), id.Replace("\"", "")));
                    string txtFilename = txtFilenameWithoutExtension + ".txt";
                    string txtString = levelColIndices.Count.ToString() + "\n";
                    txtString += "5,2\n";
                    txtString += "\"Grensvlak(m-mv)\",-99999\n";
                    txtString += "Lithologie,-99999\n";
                    txtString += "Kleur,-9999\n";
                    txtString += "Zandmediaanklasse,-99999\n";
                    txtString += "\"Humus bijmenging\",-99999\n";
                    levelIdx = 0;
                    while (levelIdx < levelColIndices.Count)
                    {
                        if (!float.TryParse(point.ColumnValues[levelColIndices[levelIdx]].Replace(",", "."), System.Globalization.NumberStyles.Float, EnglishCultureInfo, out float level))
                        {
                            throw new ToolException("Point (" + point.ToString() + ") has invalid (non-float) value in column " + levelColIndices[levelIdx] + ": " + point.ColumnValues[levelColIndices[levelIdx]]);
                        }
                        if ((addedDistances != null) && (levelIdx < addedDistances.Count))
                        {
                            level += addedDistances[levelIdx];
                        }
                        if (levelIdx < levelColIndices.Count - 1)
                        {
                            txtString += Math.Round(level, 3).ToString("F" + DecimalCount, EnglishCultureInfo) + "," + intervalCodes[levelIdx] + ",\"\",geen,\"geen\"\n";
                        }
                        else
                        {
                            txtString += Math.Round(level, 3).ToString("F" + DecimalCount, EnglishCultureInfo) + ",-,-,-,-\n";
                        }
                        levelIdx++;
                    }
                    if (!Directory.Exists(Path.GetDirectoryName(txtFilename)))
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(txtFilename));
                    }
                    File.WriteAllText(txtFilename, txtString);
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not write level file: " + Path.GetDirectoryName(filename) + "\\" + id.Replace("\"", "") + ".txt", ex);
                }
            }
        }

        /// <summary>
        /// Write this IPFFile object to a CSV-file with the specified list seperator. Decimal seperator is always a point.
        /// </summary>
        /// <param name="csvFilename"></param>
        /// <param name="listSeperator">if null, ListSeperatorWrite is used</param>
        public void WriteCSVFile(string csvFilename, char? listSeperator = null)
        {
            if (listSeperator == null)
            {
                listSeperator = ListSeperatorWrite;
            }

            StreamWriter sw = null;
            try
            {
                if ((csvFilename == null) || csvFilename.Equals(string.Empty))
                {
                    throw new Exception("No filename specified for IDFFile.WriteFile()");
                }
                if (!Directory.Exists(Path.GetDirectoryName(csvFilename)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(csvFilename));
                }
                sw = new StreamWriter(csvFilename);

                // Write column definitions
                for (int i = 0; i < this.ColumnCount; i++)
                {
                    sw.Write(this.ColumnNames[i]);
                    if (i != (this.ColumnCount - 1))
                    {
                        sw.Write(((char) listSeperator).ToString());
                    }
                }
                sw.WriteLine();

                // Write points
                if (this.points != null)
                {
                    foreach (IPFPoint ipfPoint in this.points)
                    {
                        WriteIPFPointRecord(sw, ipfPoint, (char) listSeperator);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while writing CSV-file", ex);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }

        /// <summary>
        /// Remove all current values (rows) for this IPFFile object
        /// </summary>
        public override void ResetValues()
        {
            if (points != null)
            {
                points.Clear();
            }
            else
            {
                // Set to empty list, instead of null, to distinguish between not loaded points and zero points.
                points = new List<IPFPoint>();
            }
            extent = null;

            // Note: do not reset pointCount, since number of points in IPF-file is not changed yet.
        }

        /// <summary>
        /// Create (deep) copy of this GEN-file object, including GEN-features
        /// </summary>
        /// <param name="newFilename"></param>
        /// <returns>IMODFile object</returns>
        public override IMODFile Copy(string newFilename = null)
        {
            return CopyIPF(newFilename);
        }

        /// <summary>
        /// Create (deep) copy of this GEN-file object, including GEN-features
        /// </summary>
        /// <param name="newFilename"></param>
        /// <returns>IPFFile object</returns>
        public IPFFile CopyIPF(string newFilename = null)
        {
            IPFFile copiedFile = ClipIPF((Extent)null);
            copiedFile.Filename = newFilename;
            return copiedFile;
        }

        /// <summary>
        /// Retrieves the number of elements (points) with a value in this IPF-file 
        /// </summary>
        /// <returns></returns>
        public override long RetrieveElementCount()
        {
            return PointCount;
        }

        /// <summary>
        /// Retrieve Timeseries object for point at specified index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public IPFTimeseries RetrieveTimeseries(int index)
        {
            if (index < PointCount)
            {
                IPFPoint ipfPoint = Points[index];
                if (ipfPoint != null)
                {
                    return ipfPoint.Timeseries;
                }
            }
            return null;
        }

        /// <summary>
        /// Create legend for this IPF-file with specified description
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public override Legend CreateLegend(string description)
        {
            return CreateLegend(description, null);
        }

        /// <summary>
        /// Create legend for this IPF-file with specified description and single color
        /// </summary>
        /// <param name="description"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public Legend CreateLegend(string description, System.Drawing.Color? color = null)
        {
            System.Drawing.Color actualColor = System.Drawing.Color.Gray;
            if (color != null)
            {
                actualColor = (System.Drawing.Color)color;
            }

            IPFLegend ipfLegend = new IPFLegend(description);
            ipfLegend.AddClass(new RangeLegendClass(float.MinValue, float.MaxValue, Path.GetFileNameWithoutExtension(Filename), actualColor));
            return ipfLegend;
        }

        /// <summary>
        /// Clips IPF-file with specified extent. Note: filename is copied.
        /// </summary>
        /// <param name="clipExtent"></param>
        /// <param name="isInvertedClip">if true, the points inside the specified extent are flipped away</param>
        /// <param name="clippedTextFilenames">all textfiles that are outside clipExtent and have been clipped are added with lowercase filename to the specified list</param>
        /// <returns></returns>
        public IPFFile ClipIPF(Extent clipExtent, bool isInvertedClip = false, List<string> clippedTextFilenames = null)
        {
            IPFFile clippedIPFFile = CopyProperties();

            foreach (IPFPoint ipfPoint in Points)
            {
                if ((clipExtent == null) || (!isInvertedClip && ipfPoint.IsContainedBy(clipExtent)) || (isInvertedClip && !ipfPoint.IsContainedBy(clipExtent)))
                {
                    clippedIPFFile.AddPoint(ipfPoint.CopyIPFPoint());
                }
                else
                {
                    if ((clippedTextFilenames != null) && (AssociatedFileColIdx > 0))
                    {
                        string textFilename = Path.Combine(Path.GetDirectoryName(clippedIPFFile.Filename), ipfPoint.ColumnValues[AssociatedFileColIdx].Replace("\"", "") + ".TXT");
                        clippedTextFilenames.Add(textFilename.ToLower());
                    }
                }
            }
            clippedIPFFile.fileExtent = (fileExtent != null) ? fileExtent.Copy() : null;
            clippedIPFFile.modifiedExtent = (clipExtent != null) ? clipExtent.Copy() : null;
            clippedIPFFile.extent = (clipExtent != null) ? clipExtent.Copy() : null;

            if (Legend != null)
            {
                clippedIPFFile.Legend = Legend.Copy();
            }

            // Note: do not modify pointCount, this represents the number of points in the actual IPF-file

            return clippedIPFFile;
        }

        /// <summary>
        /// Release memory (e.g. from Timeseries and IPFPoints) when lazy loading is defined. 
        /// </summary>
        public override void ReleaseMemory(bool isMemoryCollected = true)
        {
            if (points != null)
            {
                if (pointCount != points.Count)
                {
                    if (Log != null)
                    {
                        Log.AddWarning("Memory is released, but modifications were not saved.", LogIndentLevel);
                    }
                }

                // Remove timeseries data from memory
                foreach (IPFPoint point in points)
                {
                    point.ReleaseMemory();
                }

                // Remove point data from memory
                points = null;

                if (isMemoryCollected)
                {
                    GC.Collect();
                }
            }
        }

        /// <summary>
        /// Create a new IPFFile object with a copy of all IPF-properties, including column names, but exclude actual points
        /// </summary>
        /// <returns>an IPFFile copy, but without points</returns>
        public IPFFile CopyProperties()
        {
            IPFFile copiedIPFFile = new IPFFile();
            copiedIPFFile.Filename = Filename;
            copiedIPFFile.ColumnNames = CommonUtils.CopyStringList(ColumnNames);
            copiedIPFFile.AssociatedFileColIdx = AssociatedFileColIdx;
            copiedIPFFile.NoDataValue = NoDataValue;
            copiedIPFFile.UseLazyLoading = UseLazyLoading;
            copiedIPFFile.ListSeperatorRead = ListSeperatorRead;
            copiedIPFFile.Legend = Legend;
            copiedIPFFile.ITYPE = ITYPE;
            // do not copy log properties, this is object specific

            return copiedIPFFile;
        }

        /// <summary>
        /// Check if a z-coordinate is defined for the points in this IPF-file
        /// </summary>
        /// <returns></returns>
        protected bool HasZColumn()
        {
            bool hasZCoordinate = false;

            if ((ColumnNames.Count > 2) && (ColumnNames[0].ToLower().Contains("x")))
            {
                // retrieve part of coordinate-columnname without the coordinate name (e.g. x) itself
                // assume the (y- and) z-coordinate have the same name structure
                string coordinateStringPart = ColumnNames[0].ToLower().Replace("x", string.Empty);

                hasZCoordinate = (ColumnNames[2].ToLower().Replace("z", string.Empty).Equals(coordinateStringPart));
            }
            return hasZCoordinate;
        }

        /// <summary>
        /// Check for invalid (i.e. empty) columnnames
        /// </summary>
        protected void CheckColumnNames()
        {
            if ((ColumnNames == null))
            {
                throw new Exception("No columnnames defined");
            }

            for (int colIdx = 0;  colIdx < ColumnNames.Count; colIdx++)
            {
                string columnname = ColumnNames[colIdx];
                if ((columnname == null) || columnname.Equals(string.Empty))
                {
                    throw new ToolException("Empty columnname at zero-based index " + colIdx + " is not allowed");
                }
            }
        }


        /// <summary>
        /// Adds standard list seperators (comma, semicolon, space and tab) to specified list seperators
        /// </summary>
        /// <param name="listSeperators"></param>
        /// <returns></returns>
        private static string AddStandardListSeperators(string listSeperators = null)
        {
            if (listSeperators == null)
            {
                listSeperators = string.Empty;
            }
            if (!listSeperators.Contains(','))
            {
                listSeperators += ',';
            }
            if (!listSeperators.Contains(';'))
            {
                listSeperators += ';';
            }
            if (!listSeperators.Contains(' '))
            {
                listSeperators += ' ';
            }
            if (!listSeperators.Contains('\t'))
            {
                listSeperators += '\t';
            }

            return listSeperators;
        }

        /// <summary>
        /// Find listseperator that best fits given line, given an expected number of columns
        /// </summary>
        /// <param name="pointLine"></param>
        /// <param name="columnCount"></param>
        /// <param name="listSeperators">one or more listseperator chararacters in order to try</param>
        /// <param name="isWarnedForColumnMismatch">when no list seperator with a matching column count is found: if true, an Exception is thrown, if false the first list seperator is used</param>
        /// <returns></returns>
        private static char FindListSeperator(string pointLine, int columnCount, string listSeperators = null, bool isWarnedForColumnMismatch = true)
        {
            // Try the specified listseperator(s) in given order.
            if (listSeperators != null)
            {
                for (int i = 0; i < listSeperators.Length; i++)
                {
                    if (IsValidListSeperator(pointLine, columnCount, listSeperators[i]))
                    {
                        return listSeperators[i];
                    }
                }
            }

            if (IsWarnedForColumnMismatch)
            {
                throw new ToolException("Unknown listseperator in IPF: listsepators '" + GetListSeperatorsString(listSeperators) + "' don't match defined columncount ("
                + columnCount + ") for first IPF-point: " + pointLine);
            }
            else
            {
                return listSeperators[0];
            }
        }

        /// <summary>
        /// Check if a string will be split into specified number of columns with given list seperator
        /// </summary>
        /// <param name="pointLine"></param>
        /// <param name="columnCount"></param>
        /// <param name="listSeperator"></param>
        /// <returns></returns>
        private static bool IsValidListSeperator(string pointLine, int columnCount, char listSeperator)
        {
            string[] columnValuesTest = pointLine.Split(new char[] { listSeperator });
            return (columnValuesTest.Length == columnCount);
        }

        /// <summary>
        /// Retrieve readable string with all currently defined listseperators for IPF-files, including specified extra user defined list seperators
        /// </summary>
        /// <param name="listSeperators"></param>
        /// <returns></returns>
        private static string GetListSeperatorsString(string listSeperators)
        {
            return listSeperators.Replace("\t", "TAB");
        }

        /// <summary>
        /// Parse trimmed string to integer, exception are not caught
        /// </summary>
        /// <param name="intString"></param>
        /// <returns></returns>
        private static int ParseInt(string intString)
        {
            return int.Parse(intString.Trim());
        }

        /// <summary>
        /// Parse IPF line string with definition of TXT-file column
        /// </summary>
        /// <param name="associatedFileDefLine"></param>
        /// <param name="listSeperators"></param>
        /// <param name="associatedFileColIdx">zero based index of column for associated filenames, or -1 if not used</param>
        /// <param name="associatedFileExt">filename extension of associated files, or null if not defined</param>
        /// <returns></returns>
        private static void ParseAssociatedFileDef(string associatedFileDefLine, string listSeperators, out int associatedFileColIdx, out string associatedFileExt)
        {
            if ((associatedFileDefLine == null) || associatedFileDefLine.Equals(string.Empty))
            {
                throw new ToolException("Definition line for index of associated filenames is empty");
            }

            // Remove whitespace and comments
            associatedFileDefLine = RemoveIPFLineComment(associatedFileDefLine).Trim();
            while (associatedFileDefLine.Contains("  "))
            {
                associatedFileDefLine = associatedFileDefLine.Replace("  ", " ");
            }

            // Split line with list seperator
            string[] lineValues = associatedFileDefLine.Split(listSeperators.ToCharArray());
            associatedFileColIdx = -1;
            try
            {
                associatedFileColIdx = int.Parse(lineValues[0]) - 1;
            }
            catch (Exception)
            {
                throw new ToolException("Unexpected character(s) found for index of column with associated filenames: " + associatedFileDefLine);
            }

            associatedFileExt = null;
            if (lineValues.Length > 1)
            {
                associatedFileExt = lineValues[1];
                if (FileUtils.HasInvalidPathCharacters(associatedFileExt))
                {
                    throw new ToolException("File extension in line with definition for associated filenames contains invalid characters: " + associatedFileExt);
                }
            }

            if (lineValues.Length > 2)
            {
                throw new ToolException("Line with definition for associated filenames contains an unexpected parameters (only index and extension are expected): " + associatedFileDefLine);
            }
        }

        /// <summary>
        /// Write details of specified IPFPoint to line in IPF-file, with specified list seperator. 
        /// Values that include any of the list seperators comma, space or tab are surrounded by double quotes.
        /// </summary>
        /// <param name="sw"></param>
        /// <param name="ipfPoint"></param>
        /// <param name="listseperator"></param>
        private void WriteIPFPointRecord(StreamWriter sw, IPFPoint ipfPoint, char listseperator)
        {
            List<string> columnValues = ipfPoint.ColumnValues;
            int lastCoordinateIdx = HasZColumn() ? 3 : 2;

            // Assume at least two column's are present (normally at least x and y)
            for (int colIdx = 0; colIdx < ipfPoint.ColumnValues.Count; colIdx++)
            {
                string value = columnValues[colIdx];
                if (value != null && !value.Equals(string.Empty))
                {
                    value = value.Trim();
                    if (colIdx < lastCoordinateIdx)
                    {
                        // Check for and commas and points in XY-coordinates
                        if (value.Contains(',') && !value.Contains('.'))
                        {
                            // Only commas are present, assumme it is dutch format and replace y decimal point
                            value = value.Replace(",", ".");
                        }
                    }

                    if ((DecimalCount >= 0) && !int.TryParse(value, out int intValue)
                        && double.TryParse(value.Replace(",", "."), System.Globalization.NumberStyles.Float, EnglishCultureInfo, out double dblValue))
                    {
                        value = Math.Round(dblValue, DecimalCount).ToString(EnglishCultureInfo);
                    }
                    else if (columnValues[colIdx].Contains(" ") || columnValues[colIdx].Contains(",") || columnValues[colIdx].Contains("\t"))
                    {
                        value = CommonUtils.EnsureDoubleQuotes(columnValues[colIdx]);
                    }
                }
                else
                {
                    // For last column value, write empty string as two double quotes, since iMOD will raise an error for a final empty string
                    if (colIdx == columnValues.Count - 1)
                    {
                        value = "\"\"";
                    }
                }

                if (colIdx == 0)
                {
                    sw.Write(value);
                }
                else
                {
                    sw.Write(listseperator.ToString() + value);
                }
            }
            sw.WriteLine();
        }

        /// <summary>
        /// Remove comment part from line in IPF-file
        /// </summary>
        /// <param name="ipfLine"></param>
        /// <returns></returns>
        private static string RemoveIPFLineComment(string ipfLine)
        {
            int idx = ipfLine.IndexOf("!");
            if (idx > 0)
            {
                return ipfLine.Remove(idx);
            }
            else
            {
                return ipfLine;
            }
        }

        /// <summary>
        /// Calculate extent of bounding box around current points in IPF-file
        /// </summary>
        /// <returns></returns>
        private Extent CalculateExtent()
        {
            float llx = float.MaxValue;
            float urx = float.MinValue;
            float lly = float.MaxValue;
            float ury = float.MinValue;

            if (points != null)
            {
                foreach (Point point in points)
                {
                    try
                    {
                        float x = float.Parse(point.XString, EnglishCultureInfo);
                        float y = float.Parse(point.YString, EnglishCultureInfo);

                        if (x < llx)
                        {
                            llx = x;
                        }
                        if (x > urx)
                        {
                            urx = x;
                        }
                        if (y < lly)
                        {
                            lly = y;
                        }
                        if (y > ury)
                        {
                            ury = y;
                        }
                    }
                    catch (Exception)
                    {
                        throw new ToolException("Could not read point coordinates: " + point.ToString());
                    }
                }
            }

            return new Extent(llx, lly, urx, ury);
        }

    }
}
