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
using Sweco.SIF.iMOD.IMF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.Joins
{
    /// <summary>
    /// Class that describes an object that represents some kind of table with columnsand that can be joined to an iMOD-file with columns
    /// </summary>
    public abstract class JoinFile
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
        public List<string> ColumnNames { get; protected set; }

        /// <summary>
        /// Table ([row][col]) with rows with a list of column values per row. Each row can contain an optional timeseries.
        /// </summary>
        public List<JoinRow> Rows { get; protected set; }

        protected JoinFile()
        {
            Filename = null;
            FileObject = null;
            ColumnNames = null;
            Rows = null;
        }

        /// <summary>
        /// Create Joinfile object for specified filename and prepare datastructures for join
        /// </summary>
        /// <param name="filename"></param>
        public JoinFile(string filename) : base()
        {
            if (!File.Exists(filename))
            {
                throw new ToolException("File not found: " + filename);
            }

            Filename = filename;
        }

        /// <summary>
        /// Import IPF-file with defined filename as join file and define FileObject, ColumnNames and Rows
        /// </summary>
        /// <param name="joinSettings"></param>
        public abstract void ImportFile(JoinSettings joinSettings);

        /// <summary>
        /// Checks if ImportFile() has been implemented correctly in subclass
        /// </summary>
        /// <exception cref="Exception"></exception>
        protected virtual void CheckImport()
        {
            if (FileObject == null)
            {
                throw new Exception("Ensure ImportFile() is called, which should have defined FileObject for file: " + Path.GetFileName(Filename));
            }

            if ((ColumnNames == null) || (ColumnNames.Count == 0))
            {
                throw new Exception("Ensure ImportFile() is called, which should have defined ColumnNames for file: " + Path.GetFileName(Filename));
            }
        }
        /// <summary>
        /// Retrieve a list of column indices as defined by the specified string with comma-seperated column references (name or number)
        /// </summary>
        /// <param name="keyStringsString">string with comma-seperated key strings</param>
        /// <returns></returns>
        public virtual List<int> RetrieveKeyIndices(string keyStringsString)
        {
            CheckImport();

            if ((keyStringsString == null) || keyStringsString.Equals(string.Empty))
            {
                throw new Exception("Join key cannot be empty");
            }
            List<int> keyIndices = new List<int>();

            string[] keyStrings = keyStringsString.Split(new char[] { ',' });
            foreach (string keyString in keyStrings)
            {
                int colIndex = FindColumnIndex(keyString);
                if (colIndex == -1)
                {
                    throw new ToolException("Columnnumber or -name not defined for " + Path.GetExtension(Filename).Substring(1).ToUpper() + "-file '" + Path.GetFileName(Filename) + "': " + keyString);
                }

                keyIndices.Add(colIndex);
            }

            return keyIndices;
        }

        /// <summary>
        /// Create a dictionary with unique keys that are mapped to lists of indices with all rows (in this JoinFile) with the same key.
        /// Each key is a string with columnvalues, seperated by a semicolon ';', specified by the given keyIndices list.
        /// </summary>
        /// <param name="keyIndices"></param>
        /// <param name="isSorted">if true a SortedDictionary is used, resulting in sorted keys, but slower lookup speed</param>
        /// <returns></returns>
        public IDictionary<string, List<int>> RetrieveKeyDictionary( List<int> keyIndices, bool isSorted = false)
        {
            CheckImport();

            if ((keyIndices == null) || (keyIndices.Count == 0))
            {
                throw new Exception("keyIndices cannot be empty when retrieving KeyDictionary");
            }

            IDictionary<string, List<int>> keyDictionary = isSorted ? (IDictionary<string, List<int>>)new SortedDictionary<string, List<int>>() : (IDictionary<string, List<int>>)(new Dictionary<string, List<int>>());
            for (int pointIdx = 0; pointIdx < Rows.Count; pointIdx++)
            {
                List<string> columnValues = Rows[pointIdx];

                string keyString = string.Empty;
                foreach (int keyIndex in keyIndices)
                {
                    if (keyIndex >= columnValues.Count)
                    {
                        throw new ToolException("Invalid key columnnumber " + (keyIndex + 1) + " for joinFile row " + (pointIdx + 1) + " with " + columnValues.Count + " value(s): " + CommonUtils.ToString(columnValues));
                    }
                    keyString += columnValues[keyIndex] + ";";
                }
                keyString = keyString.Substring(0, keyString.Length - 1);

                if (!keyDictionary.ContainsKey(keyString))
                {
                    // Add empty list for this key
                    keyDictionary.Add(keyString, new List<int>());
                }

                // Add this point index to list of indices of this key
                keyDictionary[keyString].Add(pointIdx);
            }

            return keyDictionary;
        }

        /// <summary>
        /// Retrieve selected column indices; depending on settings some columns may be skipped (e.g. key-columns for Natural joins)
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="keyColIndices">column indices of key columns, or use null to ignore</param>
        /// <returns></returns>
        public virtual List<int> GetColumnIndices(JoinSettings settings, List<int> keyColIndices = null)
        {
            CheckImport();

            List<int> selectedColIndices = new List<int>();

            for (int colIdx2 = 0; colIdx2 < ColumnNames.Count; colIdx2++)
            {
                // Add all columns from this JoinFile (except for natural joins where key-columns are skipped)
                if ((settings.JoinType != JoinType.Natural) || (keyColIndices == null) || !keyColIndices.Contains(colIdx2))
                {
                    selectedColIndices.Add(colIdx2);
                }
            }

            return selectedColIndices;
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

            CheckImport();

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
    }
}
