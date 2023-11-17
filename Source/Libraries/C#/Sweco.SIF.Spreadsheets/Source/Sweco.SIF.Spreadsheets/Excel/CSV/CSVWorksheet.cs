// Sweco.SIF.Spreadsheets is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.Spreadsheets.
// 
// Sweco.SIF.Spreadsheets is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.Spreadsheets is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.Spreadsheets. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.Common.ExceptionHandling;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text;

namespace Sweco.SIF.Spreadsheets.Excel.CSV
{
    /// <summary>
    /// Class for processing CSV-files as a worksheet
    /// </summary>
    public class CSVWorksheet : IWorksheet
    {
        /// <summary>
        /// The 2D-object array that contains this sheet
        /// </summary>
        private string[][] csvCells = null; // 2D-matrix with cells [row][col]

        public int RowCount { get; set; }
        public int ColumnCount { get; set; }

        private Range selectedRange = null;

        /// <summary>
        /// The CSV workbook that contains this sheet
        /// </summary>
        internal IWorkbook workbook;

        /// <summary>
        /// Create new EPPlus worksheet object
        /// </summary>
        /// <param name="workbook"></param>
        internal CSVWorksheet(CSVWorkbook workbook)
        {
            this.workbook = workbook;
            this.RowCount = 0;
            this.ColumnCount = 0;
        }

        /// <summary>
        /// Retrieve the workbook that contains this sheet
        /// </summary>
        public IWorkbook Workbook
        {
            get { return workbook; }
        }

        /// <summary>
        /// Activate this worksheet
        /// </summary>
        public void Activate()
        {
            // ignore, CSV-file has only a single sheet which is always activated
        }

        /// <summary>
        /// Set the name of this sheet to the specified string
        /// </summary>
        /// <param name="sheetname"></param>
        public void SetSheetname(string sheetname)
        {
            // epplusWorksheet.Name = sheetname;
        }

        /// <summary>
        /// Get the name of this sheet
        /// </summary>
        public string GetSheetname()
        {
            return Path.GetFileNameWithoutExtension(workbook.Filename);
        }

        /// <summary>
        /// Select specified cell in this sheet
        /// </summary>
        /// <param name="range"></param>
        public void Select(Range range)
        {
            selectedRange = range;
        }

        /// <summary>
        /// Select specified cell in this sheet
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        public void Select(int rowIdx, int colIdx)
        {
            Cell cell = new Cell(this, rowIdx, colIdx);
            selectedRange = new Range(this, cell, cell);
        }

        /// <summary>
        /// Activate specified cell in this sheet
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        public void Activate(int rowIdx, int colIdx)
        {
            Select(rowIdx, colIdx);
        }

        /// <summary>
        /// Set value for specified cell with a string, but find it's type by parsing this string with the CultureInfo as specified in the corresponding Workbook object.
        /// Use SetCellValue(rowIdx, colIdx, (object) value) to actually write the object value to the cell without parsing
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="value"></param>
        public void SetCellValue(int rowIdx, int colIdx, string value)
        {
            csvCells[rowIdx][colIdx] = value;
        }

        /// <summary>
        /// Set value for specified cell with a string, but find it's type by parsing this string with the specified CultureInfo, or use null to avoid parsing and always use string type
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="value"></param>
        /// <param name="parseCultureInfo"></param>
        public void SetCellValue(int rowIdx, int colIdx, string value, CultureInfo parseCultureInfo)
        {
            // Ignore culture info

            csvCells[rowIdx][colIdx] = value;
        }

        /// <summary>
        /// Set value for specified cell with a long
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="value"></param>
        public void SetCellValue(int rowIdx, int colIdx, long value)
        {
            csvCells[rowIdx][colIdx] = value.ToString();
        }

        /// <summary>
        /// Set value for specified cell with a float
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="value"></param>
        public void SetCellValue(int rowIdx, int colIdx, float value)
        {
            csvCells[rowIdx][colIdx] = value.ToString(workbook.CultureInfo);
        }

        /// <summary>
        /// Set value for specified cell with a double
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="value"></param>
        public void SetCellValue(int rowIdx, int colIdx, double value)
        {
            csvCells[rowIdx][colIdx] = value.ToString(workbook.CultureInfo);
        }

        /// <summary>
        /// Set value for specified cell with a DateTime object
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="value"></param>
        public void SetCellValue(int rowIdx, int colIdx, DateTime value)
        {
            csvCells[rowIdx][colIdx] = value.ToString(workbook.CultureInfo);
        }

        /// <summary>
        /// Set value for specified cell with value of specified object 
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="value"></param>
        public void SetCellValue(int rowIdx, int colIdx, object value)
        {
            csvCells[rowIdx][colIdx] = value.ToString();
        }

        /// <summary>
        /// Set formula for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="formula"></param>
        public void SetCellFormula(int rowIdx, int colIdx, string formula)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Set R1C1-formula for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="formulaR1C1"></param>
        public void SetCellFormulaR1C1(int rowIdx, int colIdx, string formulaR1C1)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get cell value as an object with a actual cell value
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        public object GetCellObjectValue(int rowIdx, int colIdx)
        {
            if (IsCellDefined(rowIdx, colIdx))
            {
                return csvCells[rowIdx][colIdx];
            }
            else
            {
                return null;
            }
        }

        private bool IsCellDefined(int rowIdx, int colIdx)
        {
            return (rowIdx < RowCount) && (colIdx < ColumnCount);
        }

        /// <summary>
        /// Retrieve cell text value as formatted in Excel workbook
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        public string GetCellText(int rowIdx, int colIdx)
        {
            if (IsCellDefined(rowIdx, colIdx))
            {
                return csvCells[rowIdx][colIdx];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve cell value as a string, formatted with CultureInfo as specified in the corresponding Workbook object
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        public string GetCellValue(int rowIdx, int colIdx)
        {
            if (IsCellDefined(rowIdx, colIdx))
            {
                return csvCells[rowIdx][colIdx];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve cell value as a string, formatted with the specified CultureInfo, or use null to avoid formatting
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public string GetCellValue(int rowIdx, int colIdx, CultureInfo cultureInfo)
        {
            if (IsCellDefined(rowIdx, colIdx))
            {
                return csvCells[rowIdx][colIdx];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve cell value as a string, formatted with the specified CultureInfo and format string
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="cultureInfo"></param>
        /// <param name="format">see C# documentation possible format strings</param>
        /// <returns></returns>
        public string GetCellValue(int rowIdx, int colIdx, CultureInfo cultureInfo, string format)
        {
            if (IsCellDefined(rowIdx, colIdx))
            {
                return csvCells[rowIdx][colIdx].ToString(cultureInfo);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get formula in specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        public string GetCellFormula(int rowIdx, int colIdx)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Get Cell instance for specified Cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        public Cell GetCell(int rowIdx, int colIdx)
        {
            return new Cell(this, rowIdx, colIdx);
        }

        /// <summary>
        /// Find specified cell
        /// </summary>
        /// <param name="searchString"></param>
        /// <param name="matchCase">true to match case</param>
        /// <param name="lookAtPart">false to match whole string</param>
        /// <returns></returns>
        public Cell FindCell(string searchString, bool matchCase = false, bool lookAtPart = false)
        {
            if (matchCase)
            {
                for (int rowIdx = 0; rowIdx < RowCount; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < ColumnCount; colIdx++)
                    {
                        string value = GetCellValue(rowIdx, colIdx);
                        if (value != null)
                        {
                            if (value.Equals(searchString) || (lookAtPart && value.Contains(searchString)))
                            {
                                return new Cell(this, rowIdx, colIdx);
                            }
                        }
                    }
                }
            }
            else
            {
                searchString = searchString.ToLower();
                for (int rowIdx = 0; rowIdx < RowCount; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < ColumnCount; colIdx++)
                    {
                        string value = GetCellValue(rowIdx, colIdx);
                        if (value != null)
                        {
                            if (value.ToLower().Equals(searchString))
                            {
                                return new Cell(this, rowIdx, colIdx);
                            }
                        }
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Retrieve last cell (before empty cell) moving from specified cell in given direction
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public Cell End(Cell cell, CellDirection direction)
        {
            int rowIdx = cell.RowIdx;
            int colIdx = cell.ColIdx;
            string value = GetCellValue(rowIdx, colIdx);

            int rowStep;
            int colStep;
            switch (direction)
            {
                case CellDirection.Up:
                    rowStep = -1;
                    colStep = 0;
                    break;
                case CellDirection.Down:
                    rowStep = 1;
                    colStep = 0;
                    break;
                case CellDirection.ToLeft:
                    rowStep = 0;
                    colStep = -1;
                    break;
                case CellDirection.ToRight:
                    rowStep = 0;
                    colStep = 1;
                    break;
                default:
                    throw new LibraryException("Unknown CellDirection: " + direction.ToString());
            }

            if ((value != null) && !value.Equals(string.Empty))
            {
                // The specified cell contains a value, loop until an empty cell is found
                int nextRowIdx = rowIdx;
                int nextColIdx = colIdx;
                while (((value != null) && !value.Equals(string.Empty)) && (nextRowIdx >= 0) && (nextColIdx >= 0) && (nextRowIdx < RowCount) && (nextColIdx < ColumnCount))
                {
                    rowIdx = nextRowIdx;
                    colIdx = nextColIdx;
                    nextRowIdx += rowStep;
                    nextColIdx += colStep;
                    if ((rowIdx >= 0) && (colIdx >= 0) && (rowIdx < RowCount) && (colIdx < ColumnCount))
                    {
                        value = GetCellValue(nextRowIdx, nextColIdx);
                        if (value == null)
                        {
                            value = GetCellFormula(nextRowIdx, nextColIdx);
                        }
                    }
                }
            }
            else
            {
                // The specified cell is empty, loop until value is found
                while (((value == null) || value.Equals(string.Empty)) && (rowIdx >= 0) && (colIdx >= 0) && (rowIdx < RowCount) && (colIdx < ColumnCount))
                {
                    rowIdx += rowStep;
                    colIdx += colStep;
                    if ((rowIdx >= 0) && (colIdx >= 0) && (rowIdx < RowCount) && (colIdx < ColumnCount))
                    {
                        value = GetCellValue(rowIdx, colIdx);
                        if (value == null)
                        {
                            value = GetCellFormula(rowIdx, colIdx);
                        }
                    }
                }
                if (colIdx == ColumnCount)
                {
                    colIdx = ColumnCount - 1;
                }
                if (colIdx == -1)
                {
                    colIdx = 0;
                }
                if (rowIdx == RowCount)
                {
                    rowIdx = RowCount - 1;
                }
                if (rowIdx == -1)
                {
                    rowIdx = 0;
                }
            }
            return new Cell(this, rowIdx, colIdx);
        }

        /// <summary>
        /// Retrieve a Range instance for specified upper left and lower right cells
        /// </summary>
        /// <param name="cell1"></param>
        /// <param name="cell2"></param>
        /// <returns></returns>
        public Range GetRange(Cell cell1, Cell cell2)
        {
            return new Range(this, cell1, cell2);
        }

        /// <summary>
        /// Retrieve a Range instance for specified upper left and lower right cells
        /// </summary>
        /// <param name="rowIdx1">zero-based row index of upper left cell in range</param>
        /// <param name="colIdx1"></param>
        /// <param name="rowIdx2">zero-based row index of lower right cell in range</param>
        /// <param name="colIdx2"></param>
        /// <returns></returns>
        public Range GetRange(int rowIdx1, int colIdx1, int rowIdx2, int colIdx2)
        {
            return new Range(this, rowIdx1, colIdx1, rowIdx2, colIdx2);
        }

        /// <summary>
        /// Retrieve 2D-array with cell values in specified range
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public object[,] GetValues(Range range)
        {
            object[,] values = new object[range.RowIdx2 - range.RowIdx1 + 1, range.ColIdx2 - range.ColIdx1 + 1];
            int subRowIdx = 0;
            int subColIdx = 0;
            int rangeRowIdx = -1;
            int rangeColIdx = -1;
            try
            {
                for (rangeRowIdx = range.RowIdx1; rangeRowIdx <= Math.Min(RowCount - 1, range.RowIdx2); rangeRowIdx++)
                {
                    for (rangeColIdx = range.ColIdx1; rangeColIdx <= Math.Min(ColumnCount - 1, range.ColIdx2); rangeColIdx++)
                    {
                        values[subRowIdx, subColIdx] = csvCells[rangeRowIdx][rangeColIdx];
                        subColIdx++;
                    }
                    subRowIdx++;
                    subColIdx = 0;
                }
            }
            catch (Exception ex)
            {
                throw new LibraryException("Could not get values in range [(" + range.RowIdx1 + "," + range.ColIdx1 + "),("
                    + range.RowIdx2 + "," + range.ColIdx2 + ")]. Error for cell (" + rangeRowIdx + "," + rangeColIdx + ")", ex);
            }
            return values;
        }

        /// <summary>
        /// Retrieve the number of rows in this sheet
        /// </summary>
        /// <returns></returns>
        public int GetRowsCount()
        {
            return RowCount;
        }

        /// <summary>
        /// Retrieve the number of columns in this sheet
        /// </summary>
        /// <returns></returns>
        public int GetColumnsCount()
        {
            return ColumnCount;
        }

        /// <summary>
        /// Retrieve the zero-based index of the last row in this worksheet
        /// </summary>
        public int LastRowIdx
        {
            get { return RowCount - 1; }
        }

        /// <summary>
        /// Retrieve the zero-based index of the last column in this worksheet
        /// </summary>
        public int LastColIdx
        {
            get { return ColumnCount - 1; }
        }

        /// <summary>
        /// Insert a column before specified column
        /// </summary>
        /// <param name="colIdx">zero-based column index</param>
        public void InsertColumn(int colIdx)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Insert a row before specified row
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        public void InsertRow(int rowIdx)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Delete specified row
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        public void DeleteRow(int rowIdx)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Delete specified rows
        /// </summary>
        /// <param name="startRowIdx">zero-based row index</param>
        /// <param name="rowCount"></param>
        public void DeleteRows(int startRowIdx, int rowCount)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Delete specified column
        /// </summary>
        /// <param name="colIdx">zero-based column index</param>
        public void DeleteColumn(int colIdx)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Delete specified columns
        /// </summary>
        /// <param name="startColIdx">zero-based column index</param>
        /// <param name="colCount"></param>
        public void DeleteColumns(int startColIdx, int colCount)
        {
            throw new NotImplementedException();
        }

        public void Clear(Range range = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Remove all comments in sheet
        /// </summary>
        public void ClearComments()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Check if the specified cell is empty
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public bool IsEmpty(Cell cell)
        {
            string cellValue = csvCells[cell.RowIdx][cell.ColIdx];
            return ((cellValue == null) || cellValue.Equals(string.Empty));
        }

        /// <summary>
        /// Check if the specified cell is empty
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        public bool IsEmpty(int rowIdx, int colIdx)
        {
            if (IsCellDefined(rowIdx, colIdx))
            {
                string cellValue = csvCells[rowIdx][colIdx];
                return ((cellValue == null) || cellValue.Equals(string.Empty));
            }
            else
            {
                return true; 
            }
        }

        /// <summary>
        /// Retrieve the range of cells that are used, or null for an empty sheet
        /// </summary>
        /// <returns>a range of used cells or null</returns>
        public Range GetUsedRange()
        {
            return new Range(this, 0, 0, RowCount - 1, ColumnCount - 1);
        }

        /// <summary>
        /// Merge cells in specified range
        /// </summary>
        /// <param name="rowIdx1">zero-based row index of upper left cell in range</param>
        /// <param name="colIdx1">zero-based column index of upper left cell in range</param>
        /// <param name="rowIdx2">zero-based row index of lower right cell in range</param>
        /// <param name="colIdx2">zero-based column index of lower right cell in range</param>
        public void MergeCells(int rowIdx1, int colIdx1, int rowIdx2, int colIdx2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Checks if cells in specified range are merged, but not if these cells are merged together
        /// </summary>
        /// <param name="rowIdx1">zero-based row index of upper left cell in range</param>
        /// <param name="colIdx1">zero-based column index of upper left cell in range</param>
        /// <param name="rowIdx2">zero-based row index of lower right cell in range</param>
        /// <param name="colIdx2">zero-based column index of lower right cell in range</param>
        /// <returns></returns>
        public bool IsMerged(int rowIdx1, int colIdx1, int rowIdx2, int colIdx2)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves the unique address strings (R1C1-format) for  all merged cells in this sheet
        /// </summary>
        public string[] GetMergedCells()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves the address string (R1C1-format) of the specified cell. For merged cells this will result in a range
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        public string GetMergedRange(int rowIdx, int colIdx)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Check if the cells in the given range have borders on the specified sides
        /// </summary>
        /// <param name="range"></param>
        /// <param name="checkLeft"></param>
        /// <param name="checkRight"></param>
        /// <param name="checkTop"></param>
        /// <param name="checkBottom"></param>
        /// <returns></returns>
        public bool HasBorders(Range range, bool checkLeft = true, bool checkRight = true, bool checkTop = true, bool checkBottom = true)
        {
            return false;
        }

        /// <summary>
        /// Check if the given cell has borders on the specified sides
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="checkLeft"></param>
        /// <param name="checkRight"></param>
        /// <param name="checkTop"></param>
        /// <param name="checkBottom"></param>
        /// <returns></returns>
        public bool HasBorders(int rowIdx, int colIdx, bool checkLeft = true, bool checkRight = true, bool checkTop = true, bool checkBottom = true)
        {
            return false;
        }

        /// <summary>
        /// Retrieve the currently defined number format for the specified range
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public string GetNumberFormat(Range range)
        {
            return null;
        }

        /// <summary>
        /// Retrieve the currently defined number format for the specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        public string GetNumberFormat(int rowIdx, int colIdx)
        {
            return null;
        }

        /// <summary>
        /// Set comment for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="comment"></param>
        public void SetComment(int rowIdx, int colIdx, string comment)
        {
            // ignore
        }

        /// <summary>
        /// Delete comment for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        public void DeleteComment(int rowIdx, int colIdx)
        {
            // ignore
        }

        //public System.Drawing.Color GetBorderEdgeTopColor(Range range)
        //{
        //    ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
        //    string rgb = epplusRange.Style.Border.Top.Color.Rgb;
        //    // todo
        //    return Color.Black;
        //}

        //public System.Drawing.Color GetBorderEdgeLeftColor(Range range)
        //{
        //    ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
        //    string rgb = epplusRange.Style.Border.Left.Color.Rgb;
        //    // todo
        //    return Color.Black;
        //}

        //public System.Drawing.Color GetBorderEdgeRightColor(Range range)
        //{
        //    ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
        //    string rgb = epplusRange.Style.Border.Right.Color.Rgb;
        //    // todo
        //    return Color.Black;
        //}

        //public System.Drawing.Color GetBorderEdgeBottomColor(Range range)
        //{
        //    ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
        //    string rgb = epplusRange.Style.Border.Bottom.Color.Rgb;
        //    // todo
        //    return Color.Black;
        //}

        /// <summary>
        /// Set color for all border lines in this sheet 
        /// </summary>
        /// <param name="color"></param>
        public void SetBorderColor(System.Drawing.Color color)
        {
            // ignore
        }

        /// <summary>
        /// Set color for all border lines in this sheet 
        /// </summary>
        /// <param name="color"></param>
        /// <param name="weight"></param>
        public void SetBorderColor(System.Drawing.Color color, BorderWeight weight = BorderWeight.Thin)
        {
            // ignore
        }

        /// <summary>
        /// Set color and weight for border lines in specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="color"></param>
        /// <param name="weight"></param>
        /// <param name="isInside">if true, specified settings are also used for inner lines</param>
        public void SetBorderColor(Range range, System.Drawing.Color color, BorderWeight weight, bool isInside = false)
        {
            // ignore
        }

        /// <summary>
        /// Set color for horizontal inner border lines in specified range (not available for EPPlus)
        /// </summary>
        /// <param name="range"></param>
        /// <param name="themeColor">index of color in current theme</param>
        /// <param name="tintAndShade">value that defines tint and shade of theme color</param>
        public void SetBorderInsideHorizontalColor(Range range, int themeColor, double tintAndShade)
        {
            // ignore
        }

        /// <summary>
        /// Set color for horizontal inner border lines in specified range (not available for EPPlus)
        /// </summary>
        /// <param name="range"></param>
        /// <param name="themeColor">index of color in current theme</param>
        /// <param name="tintAndShade">value that defines tint and shade of theme color</param>
        public void SetBorderEdgeBottomColor(Range range, int themeColor, double tintAndShade)
        {
            // ignore
        }

        /// <summary>
        /// Set color for horizontal inner border lines in specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="color"></param>
        public void SetBorderEdgeBottomColor(Range range, System.Drawing.Color color)
        {
            // ignore
        }

        /// <summary>
        /// Set number format for specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="numberFormatString">a format string for the used application</param>
        public void SetNumberFormat(Range range, string numberFormatString)
        {
            // ignore
        }

        /// <summary>
        /// Set number format for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="numberFormatString"></param>
        public void SetNumberFormat(int rowIdx, int colIdx, string numberFormatString)
        {
            // ignore
        }

        /// <summary>
        /// Specify if text should be wrapped in specified cells
        /// </summary>
        /// <param name="range"></param>
        /// <param name="isWrapped">if true, bold font will be set</param>
        public void SetWrapText(Range range, bool isWrapped)
        {
            // ignore
        }

        /// <summary>
        /// Specify if italic font should be used for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="isItalic">if true, italic font will be set</param>
        public void SetFontItalic(int rowIdx, int colIdx, bool isItalic)
        {
            // ignore
        }

        /// <summary>
        /// Specify if bold font should be used for specified cell
        /// </summary>
        /// <param name="range"></param>
        /// <param name="isBold">if true, bold font will be set</param>
        public void SetFontBold(Range range, bool isBold)
        {
            // ignore
        }

        /// <summary>
        /// Specify if bold font should be used for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="isBold">if true, bold font will be set</param>
        public void SetFontBold(int rowIdx, int colIdx, bool isBold)
        {
            // ignore
        }

        /// <summary>
        /// Specify font size to given cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="size">number of pixels</param>
        public void SetFontSize(int rowIdx, int colIdx, int size)
        {
            // ignore
        }

        /// <summary>
        /// Checks if CSV-cell is formatted as a date and/or time. Note: following checks are done in this order: 
        /// - Cell value actually is a DateTime object
        /// - Formatted cell text contains a colon (':')
        /// - Numberformat contains specified substring (default: "-d")
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="checkedNumberFormat">a substring in the Numberformat-string to regard cell value as a DateTime</param>
        /// <returns></returns>
        public bool IsDateTimeCell(int rowIdx, int colIdx, string checkedNumberFormat = null)
        {
            string value = GetCellValue(rowIdx, colIdx);

            if (DateTime.TryParse(value, workbook.CultureInfo, DateTimeStyles.None, out DateTime datetime))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieve cell value as a DateTime object, assuming the underlying cell value is a DateTime-cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        public DateTime? GetCellDateTimeValue(int rowIdx, int colIdx)
        {
            string value = GetCellValue(rowIdx, colIdx);

            if (DateTime.TryParse(value, workbook.CultureInfo, DateTimeStyles.None, out DateTime datetime))
            {
                return datetime;
            }

            return null;
        }

        /// <summary>
        /// Get current font color of specified range
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public Color GetFontColor(Range range)
        {
            return Color.Black;
        }

        /// <summary>
        /// Get current font color of specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        public Color GetFontColor(int rowIdx, int colIdx)
        {
            return Color.Black;
        }

        /// <summary>
        /// Set font color for specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="color"></param>
        public void SetFontColor(Range range, Color color)
        {
            // ignore
        }

        /// <summary>
        /// Set font color for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="color"></param>
        public void SetFontColor(int rowIdx, int colIdx, Color color)
        {
            // ignore
        }


        /// <summary>
        /// Get color of cell interior for specified range
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public Color GetInteriorColor(Range range)
        {
            return Color.White;
        }

        /// <summary>
        /// Get color of cell interior for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        public Color GetInteriorColor(int rowIdx, int colIdx)
        {
            return Color.White;
        }

        /// <summary>
        /// Set color of cell interior for specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="color"></param>
        public void SetInteriorColor(Range range, Color color)
        {
            // ignore
        }

        /// <summary>
        /// Set color of cell interior for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="color"></param>
        public void SetInteriorColor(int rowIdx, int colIdx, Color color)
        {
            // ignore
        }

        /// <summary>
        /// Set vertical alignment to center of cells for specified range
        /// </summary>
        /// <param name="range"></param>
        public void SetVerticalAlignmentTop(Range range)
        {
            // ignore
        }

        /// <summary>
        /// Set vertical alignment to center of cells for specified range
        /// </summary>
        /// <param name="range"></param>
        public void SetVerticalAlignmentCenter(Range range)
        {
            // ignore
        }

        /// <summary>
        /// Set horizontal alignment to left of cells for specified range
        /// </summary>
        /// <param name="range"></param>
        public void SetHorizontalAlignmentLeft(Range range)
        {
            // ignore
        }

        /// <summary>
        /// Set horizontal alignment to left of specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        public void SetHorizontalAlignmentLeft(int rowIdx, int colIdx)
        {
            // ignore
        }

        /// <summary>
        /// Set orientation of cell contents for specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="degrees"></param>
        public void SetOrientation(Range range, int degrees)
        {
            // ignore
        }

        /// <summary>
        /// Turn auto filter on for specified range
        /// </summary>
        /// <param name="range"></param>
        public void SetAutoFilter(Range range)
        {
            // ignore
        }

        /// <summary>
        /// Freeze rows above specified row
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        public void FreezeRow(int rowIdx)
        {
            // ignore
        }

        /// <summary>
        /// Auto fit column widths in specified range
        /// </summary>
        /// <param name="range">null to fit all column</param>
        public void AutoFitColumns(Range range = null)
        {
            // ignore
        }

        /// <summary>
        /// Auto fit width of specified column
        /// </summary>
        /// <param name="colIdx">zero-based column index</param>
        public void AutoFitColumn(int colIdx)
        {
            // ignore
        }

        /// <summary>
        /// Set width of specified column
        /// </summary>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="width"></param>
        public void SetColumnWidth(int colIdx, int width)
        {
            // ignore
        }

        /// <summary>
        /// Hide columns in specified range
        /// </summary>
        /// <param name="range"></param>
        public void HideColumns(Range range)
        {
            // ignore
        }

        /// <summary>
        /// Hide rows in specified range
        /// </summary>
        /// <param name="range"></param>
        public void HideRows(Range range)
        {
            // ignore
        }

        /// <summary>
        /// Define hyperlink for specified cell. When address is an Excelsheet, use a subaddress as sheetname!cellref, where sheetname can be surrounded by '-symbols and cellref is an absolute celladdres (e.g. D10)
        /// </summary>
        /// <param name="rowIdx"></param>
        /// <param name="colIdx"></param>
        /// <param name="address"></param>
        /// <param name="subAddress"></param>
        /// <param name="screenTip"></param>
        /// <param name="textToDisplay"></param>
        public void SetHyperlink(int rowIdx, int colIdx, string address, string subAddress = null, string screenTip = null, string textToDisplay = null)
        {
            // ignore
        }

        /// <summary>
        /// Create chart object of given type inside this sheet at specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="chartType"></param>
        /// <returns></returns>
        public IChart CreateChart(Range range, ChartType chartType)
        {
            return null;
        }

        /// <summary>
        /// Copy characteristics and contents of specified source cell to specified target cell
        /// </summary>
        /// <param name="sourceRowIdx">zero-based row index of source cell</param>
        /// <param name="sourceColIdx">zero-based column index of source cell</param>
        /// <param name="targetRowIdx">zero-based row index of target cell</param>
        /// <param name="targetColIdx">zero-based column index of target cell</param>
        public void CopyCell(int sourceRowIdx, int sourceColIdx, int targetRowIdx, int targetColIdx)
        {
            // ignore
        }

        /// <summary>
        /// Copy characteristics and contents of specified source cell to specified target cell
        /// </summary>
        /// <param name="sourceCell"></param>
        /// <param name="targetCell"></param>
        public void CopyCell(Cell sourceCell, Cell targetCell)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Copy characteristics and contents of cells in specified source range to location defined by specified upper left target cell
        /// </summary>
        /// <param name="range"></param>
        /// <param name="targetCell"></param>
        public void CopyRange(Range range, Cell targetCell)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calculate formula's in sheet dynamically
        /// </summary>
        public void Calculate()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Calculate specified Excel-formula dynamically
        /// </summary>
        /// <param name="formula"></param>
        /// <returns>result of formula</returns>
        public object Calculate(string formula)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Zoom in to the specified percentage of the current sheet
        /// </summary>
        /// <param name="percentage"></param>
        public void Zoom(int percentage)
        {
            // ignore
        }

        /// <summary>
        /// Apply specified header, footer and bitmap to this sheet
        /// </summary>
        /// <param name="leftHeader"></param>
        /// <param name="leftFooter"></param>
        /// <param name="bitmap"></param>
        public void ApplyPageSetup(string leftHeader, string leftFooter, System.Drawing.Bitmap bitmap = null)
        {
            // ignore
        }

        /// <summary>
        /// Set page orientation of this sheet to landscape
        /// </summary>
        public void SetPageOrientationLandscape()
        {
            // ignore
        }

        /// <summary>
        /// Set page size of this sheet to specified size
        /// </summary>
        /// <param name="papersize"></param>
        public void SetPapersize(PaperSize papersize)
        {
            // ignore
        }

        /// <summary>
        /// Set view mode of worksheet to page layout
        /// </summary>
        public void SetPageLayoutView()
        {
            // ignore
        }

        /// <summary>
        /// Retrieves the underlying EPPlus-object that refers to the EPPlus-implemention of a worksheet
        /// </summary>
        /// <returns></returns>
        public object GetBaseObject()
        {
            return csvCells;
        }

        internal static CSVWorksheet ReadCSVSheet(CSVWorkbook workbook, string filename, string listSeperator)
        {
            string[] csvStringLines = File.ReadAllLines(filename);

            CSVWorksheet sheet = new CSVWorksheet(workbook);

            List<string[]> tmpCSVcells = new List<string[]>(csvStringLines.Length);
            int maxLineValueCount = 0;
            for (int lineIdx = 0; lineIdx < csvStringLines.Length; lineIdx++)
            {
                string[] lineValues = csvStringLines[lineIdx].Split(new string[] { listSeperator }, StringSplitOptions.None);
                if (lineValues.Length > maxLineValueCount)
                {
                    maxLineValueCount = lineValues.Length;
                }
                tmpCSVcells.Add(lineValues);
            }

            // Create new cell matrix with exactly the specified number of columns in each row, filling up with null values if necesssary
            string[][] csvCells = new string[csvStringLines.Length][];
            for (int lineIdx = 0; lineIdx < csvStringLines.Length; lineIdx++)
            {
                csvCells[lineIdx] = new string[maxLineValueCount];
                tmpCSVcells[lineIdx].CopyTo(csvCells[lineIdx], 0);
            }
            sheet.csvCells = csvCells;
            sheet.RowCount = csvCells.Length;
            sheet.ColumnCount = maxLineValueCount;

            return sheet;
        }

        internal void Save(string filename, string ListSeperator)
        {
            StringBuilder csvStringBuilder = new StringBuilder();
            if ((RowCount > 0) && (ColumnCount > 0))
            {
                for (int rowIdx = 0; rowIdx < RowCount; rowIdx++)
                {
                    csvStringBuilder.Append(csvCells[rowIdx][0]);
                    for (int colIdx = 1; colIdx < ColumnCount; colIdx++)
                    {
                        csvStringBuilder.Append(ListSeperator);
                        csvStringBuilder.Append(csvCells[rowIdx][colIdx]);
                    }
                    csvStringBuilder.AppendLine();
                }
            }

            FileUtils.WriteFile(filename, csvStringBuilder.ToString());
        }

    }
}
