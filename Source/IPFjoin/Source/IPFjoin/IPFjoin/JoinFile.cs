// IPFjoin is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IPFjoin.
// 
// IPFjoin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IPFjoin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IPFjoin. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.IPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IPFjoin
{
    /// <summary>
    /// Class that describes an object that represents some kind of table with columnsand that can be joined to an IPF-file
    /// </summary>
    public class JoinFile
    {
        /// <summary>
        /// Filename of the joined object
        /// </summary>
        public string Filename { get; protected set; }

        /// <summary>
        /// Reference to the joined object
        /// </summary>
        public object FileObject { get; protected set; }

        /// <summary>
        /// Column names for each of the columns of the joined object
        /// </summary>
        public List<string > ColumnNames { get; protected set; }

        /// <summary>
        /// Index of column with of associated files
        /// </summary>
        public int AssociatedFileColIdx { get; protected set; }

        /// <summary>
        /// Filename extension of associated files
        /// </summary>
        public string AssociatedFileExtension{ get; protected set; }

        /// <summary>
        /// Table ([row][col]) with rows with a list of column values per row. Each row can contain an optional timeseries.
        /// </summary>
        public List<JoinRow> Rows { get; protected set; }

        /// <summary>
        /// Column index for X-coordinate, or -1 if not existing
        /// </summary>
        public int XColIdx { get; set; }

        /// <summary>
        /// Column index for Y-coordinate, or -1 if not existing
        /// </summary>
        public int YColIdx { get; set; }

        protected JoinFile()
        {
            Filename = null;
            FileObject = null;
            ColumnNames = null;
            Rows = null;
            XColIdx = -1;
            YColIdx = -1;
            AssociatedFileColIdx = -1;
            AssociatedFileExtension = null;
        }

        /// <summary>
        /// Create Joinfile object for specified filename and prepare datastructures for join
        /// </summary>
        /// <param name="filename"></param>
        public JoinFile(string filename, int xColIdx = -1, int yColIdx = -1) : base()
        {
            if (!File.Exists(filename))
            {
                throw new ToolException("File not found: " + filename);
            }

            XColIdx = xColIdx;
            YColIdx = yColIdx;

            ImportFile(filename);
        }

        /// <summary>
        /// Create Joinfile object for specified IMODFile object and prepare datastructures for join
        /// </summary>
        /// <param name="imodFile"></param>
        public JoinFile(IPFFile ipfFile) : base()
        {
            ImportIPFFile(ipfFile);
        }

        /// <summary>
        /// Import join file with specified filename. Currently only IPF-files are supported.
        /// </summary>
        /// <param name="filename"></param>
        protected virtual void ImportFile(string filename)
        {
            switch (Path.GetExtension(filename).ToLower())
            {
                case ".ipf":
                    ImportIPFFile(filename);
                    break;
                default:
                    throw new ToolException("Unknown file type for join file: " + filename);
            }
        }

        /// <summary>
        /// Import IPF-file with specified filename as join file
        /// </summary>
        /// <param name="filename"></param>
        protected void ImportIPFFile(string filename)
        {
            IPFFile ipfFile = null;
            if ((XColIdx >= 0) && (YColIdx >= 0))
            {
                ipfFile = IPFFile.ReadFile(filename, XColIdx, YColIdx);
            }
            else
            {
                ipfFile = IPFFile.ReadFile(filename);
            }
            
            ImportIPFFile(ipfFile);
        }

        /// <summary>
        /// Import specified IPFFile object as join file
        /// </summary>
        /// <param name="filename"></param>
        protected void ImportIPFFile(IPFFile ipfFile)
        {
            Filename = ipfFile.Filename;
            FileObject = ipfFile;
            ColumnNames = ipfFile.ColumnNames;
            AssociatedFileColIdx = ipfFile.AssociatedFileColIdx;
            AssociatedFileExtension = ipfFile.AssociatedFileExtension;
            Rows = new List<JoinRow>();
            foreach (IPFPoint ipfPoint in ipfFile.Points)
            {
                JoinRow row = new JoinRow(ipfPoint.ColumnValues, ipfPoint.HasTimeseries() ? ipfPoint.Timeseries : null);
                Rows.Add(row);
            }
        }

        /// <summary>
        /// Finds zero-based columnindex of specified column string, which is either a columnname or a column index. 
        /// If the given string contains an integer number, this number is returned as integer index.
        /// </summary>
        /// <param name="columnNameOrIdx"></param>
        /// <param name="isMatchWhole"></param>
        /// <param name="isMatchCase"></param>
        /// <param name="isNumber">if true, a numeric <paramref name="columnNameOrIdx"/> string is treated as a columnumber and decreased by one to return a columnindex</param>
        /// <returns>zero-based columnindex or -1 if not found</returns>
        public int FindColumnIndex(string columnNameOrIdx, bool isMatchWhole = true, bool isMatchCase = false, bool isNumber = true)
        {
            int colIdx = -1;
            if (int.TryParse(columnNameOrIdx, out colIdx))
            {
                if (isNumber)
                {
                    return (colIdx >= 1) ? (colIdx - 1) : -1;
                }
                else
                {
                    return (colIdx >= 0) ? colIdx : -1;
                }
            }

            return FindColumnName(columnNameOrIdx, isMatchWhole, isMatchCase);
        }

        /// <summary>
        /// Finds zero-based columnindex of specified columnname. If not found -1 is returned.
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="isMatchWhole">use true to match only whole words</param>
        /// <param name="isMatchCase">use true to match case</param>
        /// <returns>zero-based columnindex or -1 if not found</returns>
        public int FindColumnName(string columnName, bool isMatchWhole = true, bool isMatchCase = false)
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
        /// Retrieve Point object for specified xy-indices and column values or null if XY-indices are not defined
        /// </summary>
        /// <param name="row2"></param>
        /// <returns>Point object with XY-coordinates or null if not defined</returns>
        public Point RetrievePoint(JoinRow row2)
        {
            if ((XColIdx >= 0) && (YColIdx >= 0))
            {
                return new StringPoint(row2[XColIdx], row2[YColIdx]);
            }
            else
            {
                return null;
            }
        }
    }

    public class JoinRow : List<string>
    {
        public bool HasTimeseries { get; set; }
        public IPFTimeseries Timeseries { get; set; }

        public JoinRow() : base()
        {
            HasTimeseries = false;
            Timeseries = null;
        }

        public JoinRow(int capacity) : base(capacity)
        {
            HasTimeseries = false;
            Timeseries = null;
        }

        public JoinRow(IEnumerable<string> collection) : base(collection)
        {
            HasTimeseries = false;
            Timeseries = null;
        }

        public JoinRow(IEnumerable<string> collection, IPFTimeseries ipfTimeseries) : base(collection)
        {
            HasTimeseries = (ipfTimeseries != null);
            Timeseries = ipfTimeseries;
        }
    }
}
