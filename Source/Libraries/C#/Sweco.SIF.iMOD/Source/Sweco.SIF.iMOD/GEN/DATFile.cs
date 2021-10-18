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

namespace Sweco.SIF.iMOD.GEN
{
    /// <summary>
    /// Class to read, process, modify and write DAT-files. The first line is a header and contains columnnames. 
    /// The first column should contain an ID that is equal to the ID of one or more features in the corresponding GEN-file. Check iMOD-manual for full definition.
    /// </summary>
    public class DATFile
    {
        /// <summary>
        /// File extension of DAT-files
        /// </summary>
        public const string Extension = "DAT";

        /// <summary>
        /// Default columnname for ID-column
        /// </summary>
        public const string IDColumnName = "ID";

        /// <summary>
        /// GENFile object that this DAT-file corresponds with
        /// </summary>
        protected GENFile GENFile { get; set; }

        /// <summary>
        /// Filename of this DAT-file
        /// </summary>
        public string Filename
        {
            get 
            {
                string filename = null;
                if (GENFile != null)
                {
                    if (GENFile.Filename != null)
                    {
                        // Ensure extension is in same case as extension of corresponding GEN-filename
                        filename = Path.Combine(Path.GetDirectoryName(GENFile.Filename), Path.GetFileNameWithoutExtension(GENFile.Filename) 
                            + "." + (Path.GetExtension(GENFile.Filename).ToLower().Equals(Path.GetExtension(GENFile.Filename)) ? Extension.ToLower() : Extension.ToUpper()));
                    }
                }
                return filename;
            }
        }

        /// <summary>
        /// Columnnames in this DAT-file
        /// </summary>
        public List<string> ColumnNames { get; set; }

        /// <summary>
        /// List with all rows in DAT-file
        /// </summary>
        public List<DATRow> Rows
        {
            get { return new List<DATRow>(rowDictionary.Values); }
        }

        /// <summary>
        /// Implementation of DATRow collection: a dictionary with ID's and corresponding DATRow objects, for fast search on ID's
        /// </summary>
        private SortedDictionary<string, DATRow> rowDictionary;

        /// <summary>
        /// Specifies if an error should be thrown if a duplicate ID is found
        /// </summary>
        public static bool IsErrorOnDuplicateID { get; set; } = false;

        /// <summary>
        /// Specifies if a warning should be shown if columns of DAT-file do not match
        /// </summary>
        public static bool IsWarnedOnColumnMismatch { get; set; } = true;

        /// <summary>
        /// Creates an empty DATFile object for the specified GEN-file
        /// </summary>
        /// <param name="genFile"></param>
        public DATFile(GENFile genFile)
        {
            this.GENFile = genFile;
            this.ColumnNames = new List<string>();
            this.rowDictionary = new SortedDictionary<string,DATRow>();
        }

        /// <summary>
        /// Read DAT-file for specified GEN-file
        /// </summary>
        /// <param name="genFile"></param>
        /// <returns>null if not existing</returns>
        public static DATFile ReadFile(GENFile genFile)
        {
            DATFile datFile = new DATFile(genFile);
            datFile.ReadFile();
            return datFile;
        }

        /// <summary>
        /// Read data from file for this DAT-file.
        /// </summary>
        internal void ReadFile()
        {
            Stream stream = null;
            StreamReader sr = null;
            try
            {
                stream = File.OpenRead(Filename);
                sr = new StreamReader(stream);
                int lineNumber = 0;

                // Parse first line with columnnames
                this.ColumnNames.Clear();
                string wholeLine = sr.ReadLine();
                lineNumber++;

                // Detect ist seperator from first line
                // Note: for DAT-files only space and comma are allowed as seperators according to the iMOD-manual
                string[] rowValues = null;
                char listSeperator;
                if (wholeLine.Contains(","))
                {
                    listSeperator = ',';
                }
                else if (wholeLine.Contains(" "))
                {
                    listSeperator = ' ';
                }
                else
                {
                    // assume there is just one column
                    listSeperator = ',';
                }

                // Split current line with listseperators, correcting for single quotes
                rowValues = CommonUtils.SplitQuoted(wholeLine, listSeperator, '\'', true, true);
                ColumnNames = new List<string>(rowValues);

                CheckColumns();

                // Start reading rows
                this.rowDictionary.Clear();
                int columnCount = ColumnNames.Count;
                while (!sr.EndOfStream)
                {
                    wholeLine = sr.ReadLine();
                    lineNumber++;

                    // Split current line with listseperator, correcting for single quotes
                    rowValues = CommonUtils.SplitQuoted(wholeLine, listSeperator, '\'', true, true);
                    if (IsWarnedOnColumnMismatch && (rowValues.Length != columnCount))
                    {
                        string msg = "Invalid number of columnvalues for row in line " + (lineNumber) + " of DAT-file " + Path.GetFileName(Filename) + ": "
                            + "\r\n" + columnCount + " columnvalues expected, found: " + wholeLine 
                            + "\r\nListseperator determined for this IPF-file is '" + listSeperator + "'. Valid listseperators are ',' and ' '"
                            + "\r\nCheck use of apostrophes and/or quotes in values, e.g. 'S-GRAVELAND";
                        throw new ToolException(msg);
                    }
                    AddRow(new DATRow(rowValues.ToList()));
                }
            }
            catch (ToolException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new Exception("Could not read DAT-file: " + Path.GetFileName(Filename), ex);
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
        /// Write DAT-file with filename as defined in this object
        /// </summary>
        public void WriteFile()
        {
            StreamWriter sw = null;
            try
            {
                CheckColumns();

                if ((Filename == null) || Filename.Equals(string.Empty))
                {
                    throw new Exception("No filename specified for DATFile.WriteFile()");
                }
                if (!Path.GetDirectoryName(Filename).Equals(string.Empty) && !Directory.Exists(Path.GetDirectoryName(Filename)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Filename));
                }

                if (!Directory.Exists(Path.GetDirectoryName(Filename)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(Filename));
                }
                sw = new StreamWriter(Filename);

                // Write header
                string header = string.Empty;
                for (int colIdx = 0; colIdx < ColumnNames.Count; colIdx++)
                {
                    header += GENUtils.CorrectString(ColumnNames[colIdx]);
                    if (colIdx < ColumnNames.Count - 1)
                    {
                        header += ",";
                    }
                }
                sw.WriteLine(header);

                // Write rows
                for (int rowIdx = 0; rowIdx < rowDictionary.Count; rowIdx++)
                {
                    List<string> row = rowDictionary.ElementAt(rowIdx).Value;
                    string rowString = row.ToString();
                    sw.WriteLine(rowString);
                }
            }
            catch (IOException ex)
            {
                if (ex.Message.ToLower().Contains("access") || ex.Message.ToLower().Contains("toegang"))
                {
                    throw new ToolException(Extension + "-file cannot be written, because it is being used by another process: " + Filename);
                }
                else
                {
                    throw new Exception("Unexpected error while writing " + Extension + "-file: " + Filename, ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while writing " + Extension + "-file: " + Filename, ex);
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
        /// Add specified columnnames to this DAT-file. For all existing rows the specified default value is used.
        /// </summary>
        /// <param name="columnNames"></param>
        /// <param name="defaultValues">a value for each added column for all existing rows</param>
        public void AddColumns(List<string> columnNames, List<string> defaultValues = null)
        {
            if ((defaultValues != null) && (columnNames.Count != defaultValues.Count))
            {
                throw new Exception("Number of columnnames (" + columnNames.Count + ") and default values (" + defaultValues.Count + ") don't match");
            }

            for (int colIdx = 0; colIdx < columnNames.Count; colIdx++)
            {
                AddColumn(columnNames[colIdx], (defaultValues != null) ? defaultValues[colIdx] : string.Empty);
            }
        }

        /// <summary>
        /// Adds a column with the specified name. If the column already exists, a -1 is returned.
        /// Existing rows will get the optionally specified default value. 
        /// The first column in the DAT-file should be an ID-column, which is responsibility of caller.
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="defaultColumnValue"></param>
        /// <returns>the (zero-based) index of the added column, or -1 if a column with this name already exists</returns>
        public int AddColumn(string columnName, string defaultColumnValue = "")
        {
            int colIdx = -1;

            if (!this.ColumnNames.Contains(columnName))
            {
                ColumnNames.Add(columnName);
                colIdx = ColumnNames.Count - 1;

                foreach (List<string> row in rowDictionary.Values)
                {
                    row.Add(defaultColumnValue);
                }
            }

            return colIdx;
        }

        /// <summary>
        /// Adds an ID column, which should be the first column in the DAT-file.
        /// </summary>
        /// <returns>the zero baed column index of the ID column, which should always be 0</returns>
        public int AddIDColumn()
        {
            if (ColumnNames.Count > 0)
            {
                throw new ToolException("An ID-column can only be added to empty DAT-file, existing columns found: " + CommonUtils.ToString(ColumnNames));
            }

            int colIdx = AddColumn(IDColumnName);

            return colIdx;
        }

        /// <summary>
        /// Removes a column with the specified (zero-based) index. For existing points/rows this column value will be removed as well
        /// </summary>
        /// <param name="colIdx"></param>
        public void RemoveColumn(int colIdx)
        {
            CheckColumns();

            if (colIdx == 0)
            {
                throw new Exception("The ID-column, the first column, cannot be removed");
            }

            if (colIdx >= ColumnNames.Count)
            {
                throw new Exception("Index (zero-based) of column to be removed column is equal to or higher than number of columns");
            }

            ColumnNames.RemoveAt(colIdx);
            foreach (List<string> row in rowDictionary.Values)
            {
                if (colIdx < row.Count)
                {
                    row.RemoveAt(colIdx);
                }
            }
        }

        /// <summary>
        /// Find unique name (within this DAT-file) for specified columnname by adding a sequencenumber starting with 2, when specified columnname already exists
        /// </summary>
        /// <param name="initialColumnName"></param>
        /// <returns>unique column name</returns>
        public string GetUniqueColumnName(string initialColumnName)
        {
            int idx = 2;
            string colname = initialColumnName;
            while (GetColIdx(colname, false) >= 0)
            {
                colname = initialColumnName + idx;
                idx++;
            }
            return colname;
        }

        /// <summary>
        /// Add a row with values to this DAT-file. When the number of values doesn't match the number of columns a ToolException is thrown.
        /// When a row with the same ID already exists, an ToolException is thrown (when IsErrorOnDuplicateID is true) or the new row is ignored without a warning.
        /// </summary>
        /// <param name="row"></param>
        public void AddRow(DATRow row)
        {
            // Check that number of values matches column count
            if (row.Count != ColumnNames.Count)
            {
                throw new ToolException("Number of row-values (" + row.Count + ") doesn't match number of columns (" + ColumnNames.Count + ") for added row " + rowDictionary.Count + ": " + row.ToString());
            }

            // Retrieve ID-value
            string id = row[0].Replace("'", string.Empty);

            // Check that a row with the same id is not yet present
            if (rowDictionary.ContainsKey(id))
            {
                if (IsErrorOnDuplicateID)
                {
                    throw new ToolException("Row with ID " + row[0] + " is already existing in DATFile " + Path.GetFileName(Filename) + ", row cannot be added: " + row.ToString());
                }
                else
                {
                    // Ignore this row. iMOD GEN-file format allows duplicate GEN-features, in that case a single ID-should be added in the corresponding DAT-file. 
                    return;
                }
            }
            else
            {
                rowDictionary.Add(id, row);
            }
        }

        /// <summary>
        /// Add multiple rows to this DAT-file
        /// </summary>
        /// <param name="rows"></param>
        public void AddRows(ICollection<DATRow> rows)
        {
            foreach (DATRow row in rows)
            {
                AddRow(row);
            }
        }

        /// <summary>
        /// Check if a row with specified ID exists in this DAT-file
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool ContainsID(string id)
        {
            return rowDictionary.ContainsKey(id);
        }

        /// <summary>
        /// Retrieve row with specified ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns>null if not found</returns>
        public DATRow GetRow(string id)
        {
            if (rowDictionary.ContainsKey(id))
            {
                return rowDictionary[id];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve (zero-based) index in Rows list for given id. This method is slow, do not use for large sets, but use GetRow(id) to retrieve a row for an id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>-1 if not found</returns>
        public int GetRowIdx(string id)
        {
            string idUnquoted = id.Replace("'", string.Empty);
            string idQuoted = "'" + idUnquoted + "'";

            for (int rowIdx = 0; rowIdx < rowDictionary.Count; rowIdx++)
            {
                string rowId = rowDictionary.Keys.ElementAt(rowIdx);
                if ((rowId.Equals(idUnquoted) || rowId.Equals(idQuoted)))
                {
                    return rowIdx;
                }
            }
            return -1;
        }

        /// <summary>
        /// Find (zero-based) index of column with specified columnname
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="isCaseMatched"></param>
        /// <returns>-1 if not found</returns>
        public int GetColIdx(string columnName, bool isCaseMatched = false)
        {
            for (int colIdx = 0; colIdx < ColumnNames.Count; colIdx++)
            {
                if (isCaseMatched)
                {
                    if (ColumnNames[colIdx].Equals(columnName))
                    {
                        return colIdx;
                    }
                }
                else
                {
                    if (ColumnNames[colIdx].ToLower().Equals(columnName.ToLower()))
                    {
                        return colIdx;
                    }
                }
            }
            return -1;
        }

        /// <summary>
        /// Creates a copy of this DAT-file for another GEN-file. No check is done for correspondence of ID's
        /// </summary>
        /// <param name="genFile"></param>
        /// <returns></returns>
        public DATFile Copy(GENFile genFile)
        {
            DATFile datFile = new DATFile(genFile);
            datFile.ColumnNames = ColumnNames.ToList();
            datFile.rowDictionary = new SortedDictionary<string, DATRow>();
            for (int rowIdx = 0; rowIdx < rowDictionary.Count(); rowIdx++)
            {
                string id = rowDictionary.ElementAt(rowIdx).Key;
                DATRow row = rowDictionary.ElementAt(rowIdx).Value;
                datFile.rowDictionary.Add(id, row.Copy());
            }
            return datFile;
        }

        /// <summary>
        /// Remove row with specified ID from this DAT-file
        /// </summary>
        /// <param name="id"></param>
        public void RemoveRow(string id)
        {
            if (rowDictionary.ContainsKey(id))
            {
                rowDictionary.Remove(id);
            }
        }

        /// <summary>
        /// Set value of column with specified name to given string for all existing rows. Column is added if not yet existing.
        /// </summary>
        /// <param name="columnName"></param>
        /// <param name="value"></param>
        public void SetColumnValue(string columnName, string value)
        {
            if (!ColumnNames.Contains(columnName))
            {
                this.AddColumn(columnName, value);
            }
            else
            {
                int colIdx = ColumnNames.IndexOf(columnName);
                foreach (List<string> row in rowDictionary.Values)
                {
                    row[colIdx] = value;
                }
            }
        }

        /// <summary>
        /// Set value of column with specified (zero-based) index to given string for all existing rows. 
        /// When the column index is out of range, an Exception is thrown.
        /// </summary>
        /// <param name="colIdx"></param>
        /// <param name="value"></param>
        public void SetColumnValue(int colIdx, string value)
        {
            if ((colIdx >= 0) && (colIdx < ColumnNames.Count))
            {
                foreach (List<string> row in rowDictionary.Values)
                {
                    row[colIdx] = value;
                }
            }
            else
            {
                throw new Exception("Column index is out of range for DAT-file '" + Path.GetFileName(Filename) + "': " + colIdx);
            }
        }

        /// <summary>
        /// Ensures that a DATRow exists for all features. If missing, the feature id is added with the specified default values 
        /// or the other DAT-columns are left empty
        /// </summary>
        /// <param name="defaultValues">a list with default stringvalues, should be equal to number of column including or excluding ID-column which is ignored if present</param>
        public void AddMissingRows(List<string> defaultValues = null)
        {
            if (GENFile != null)
            {
                if ((defaultValues.Count != ColumnNames.Count) && (defaultValues.Count != ColumnNames.Count - 1))
                {
                    throw new Exception("Invalid number of default values specified (" + defaultValues.Count + ") for number of columns (" + ColumnNames.Count + ", including ID)");
                }

                for (int featureIdx = 0; featureIdx < GENFile.Features.Count; featureIdx++)
                {
                    GENFeature genFeature = GENFile.Features[featureIdx];
                    if (!rowDictionary.ContainsKey(genFeature.ID))
                    {
                        // ID is not present in DATFile, add an new row with empty default values for other columns
                        List<string> values = new List<string>();
                        values.Add(genFeature.ID);
                        for (int colIdx = 1; colIdx < ColumnNames.Count; colIdx++)
                        {
                            if (defaultValues != null)
                            {
                                if (defaultValues.Count == ColumnNames.Count)
                                {
                                    values.Add(defaultValues[colIdx]);
                                }
                                else
                                {
                                    values.Add(defaultValues[colIdx - 1]);
                                }
                            }
                            else
                            {
                                values.Add(string.Empty);
                            }
                        }
                        AddRow(new DATRow(values));
                    }
                }
            }
        }

        /// <summary>
        /// Does basic check on columns: currently is only checked that columncount is larger than zero
        /// </summary>
        private void CheckColumns()
        {
            if (ColumnNames.Count == 0)
            {
                throw new Exception("No columnnames found in DAT-file");
            }
            //// Check for "ID"-name in first column. Note: this is not mandatory, iMOD doesn't create an ID column itself for converted shapefiles ...
            //if (!columnNames[0].ToUpper().Equals(IDColumnName))
            //{
            //    throw new Exception("The first columnname of DAT-file should be equal to '" + IDColumnName + "' (not case sensitive), column name found: '" + columnNames[0] + "'");
            //}
        }
    }
}
