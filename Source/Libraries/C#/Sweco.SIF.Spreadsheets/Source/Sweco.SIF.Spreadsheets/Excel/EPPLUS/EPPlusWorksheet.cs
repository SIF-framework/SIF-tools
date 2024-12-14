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
using OfficeOpenXml;
using OfficeOpenXml.Style;
using Sweco.SIF.Common.ExceptionHandling;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Spreadsheets.Excel.EPPLUS
{
    /// <summary>
    /// Class for processing Excel worksheets with EPPlus-library
    /// </summary>
    public class EPPlusWorksheet : IWorksheet
    {
        /// <summary>
        /// The EPPlus workbook that contains this sheet
        /// </summary>
        internal EPPlusWorkbook workbook = null;

        /// <summary>
        /// Underlying object with EPPlus-implentation of this worksheet
        /// </summary>
        internal ExcelWorksheet epplusWorksheet;

        /// <summary>
        /// Underlying object with EPPlus-implentation of cells in this worksheet
        /// </summary>
        internal ExcelRange epplusCells;

        /// <summary>
        /// Create new EPPlus worksheet object
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="epplusWorksheet"></param>
        internal EPPlusWorksheet(EPPlusWorkbook workbook, ExcelWorksheet epplusWorksheet)
        {
            this.workbook = workbook;
            this.epplusWorksheet = epplusWorksheet;
            this.epplusCells = epplusWorksheet.Cells;
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
            this.workbook.epplusWorkbook.View.ActiveTab = this.epplusWorksheet.Index - 1;
        }

        /// <summary>
        /// Set the name of this sheet to the specified string
        /// </summary>
        /// <param name="sheetname"></param>
        public void SetSheetname(string sheetname)
        {
            epplusWorksheet.Name = sheetname;
        }

        /// <summary>
        /// Get the name of this sheet
        /// </summary>
        public string GetSheetname()
        {
            return epplusWorksheet.Name;
        }

        /// <summary>
        /// Select specified cell in this sheet
        /// </summary>
        /// <param name="range"></param>
        public void Select(Range range)
        {
            ExcelRange epplusRange = epplusCells[range.Cell1.RowIdx + 1, range.Cell1.ColIdx + 1, range.Cell2.RowIdx + 1, range.Cell2.ColIdx + 1];
            epplusWorksheet.Select(epplusRange);
        }

        /// <summary>
        /// Select specified cell in this sheet
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        public void Select(int rowIdx, int colIdx)
        {
            epplusWorksheet.Select(epplusCells[rowIdx + 1, colIdx + 1]);
        }

        /// <summary>
        /// Activate specified cell in this sheet
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        public void Activate(int rowIdx, int colIdx)
        {
            epplusWorksheet.View.ActiveCell = epplusCells[rowIdx + 1, colIdx + 1].Address;
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
            epplusCells[rowIdx + 1, colIdx + 1].Value = SpreadsheetUtils.ConvertStringValue(value, workbook.CultureInfo);
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
            epplusCells[rowIdx + 1, colIdx + 1].Value = (parseCultureInfo != null) ? SpreadsheetUtils.ConvertStringValue(value, parseCultureInfo) : value;
        }

        /// <summary>
        /// Set value for specified cell with a long
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="value"></param>
        public void SetCellValue(int rowIdx, int colIdx, long value)
        {
            epplusCells[rowIdx + 1, colIdx + 1].Value = value;
        }

        /// <summary>
        /// Set value for specified cell with a float
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="value"></param>
        public void SetCellValue(int rowIdx, int colIdx, float value)
        {
            if (value.Equals(float.NaN))
            {
                // Excel gives an error when a NaN-value is written to Excel by EPPlus, convert it to a string
                epplusCells[rowIdx + 1, colIdx + 1].Value = value.ToString();
            }
            else
            {
                epplusCells[rowIdx + 1, colIdx + 1].Value = value;
            }
        }

        /// <summary>
        /// Set value for specified cell with a double
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="value"></param>
        public void SetCellValue(int rowIdx, int colIdx, double value)
        {
            if (value.Equals(float.NaN))
            {
                // Excel gives an error when a NaN-value is written to Excel by EPPlus, convert it to a string
                epplusCells[rowIdx + 1, colIdx + 1].Value = value.ToString();
            }
            else
            {
                epplusCells[rowIdx + 1, colIdx + 1].Value = value;
            }
        }

        /// <summary>
        /// Set value for specified cell with a DateTime object
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="value"></param>
        public void SetCellValue(int rowIdx, int colIdx, DateTime value)
        {
            epplusCells[rowIdx + 1, colIdx + 1].Value = value;
        }

        /// <summary>
        /// Set value for specified cell with value of specified object 
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="value"></param>
        public void SetCellValue(int rowIdx, int colIdx, object value)
        {
            if (value is float)
            {
                SetCellValue(rowIdx, colIdx, (float)value);
            }
            else if (value is double)
            {
                SetCellValue(rowIdx, colIdx, (double)value);
            }
            else
            {
                epplusCells[rowIdx + 1, colIdx + 1].Value = value;
            }
        }

        /// <summary>
        /// Set formula for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="formula"></param>
        public void SetCellFormula(int rowIdx, int colIdx, string formula)
        {
            epplusCells[rowIdx + 1, colIdx + 1].Formula = formula;
        }

        /// <summary>
        /// Set R1C1-formula for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="formulaR1C1"></param>
        public void SetCellFormulaR1C1(int rowIdx, int colIdx, string formulaR1C1)
        {
            epplusCells[rowIdx + 1, colIdx + 1].FormulaR1C1 = formulaR1C1;
        }

        /// <summary>
        /// Get cell value as an object with a actual cell value
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        public object GetCellObjectValue(int rowIdx, int colIdx)
        {
            return epplusCells[rowIdx + 1, colIdx + 1].Value;
        }

        /// <summary>
        /// Retrieve cell text value as formatted in Excel workbook
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        public string GetCellText(int rowIdx, int colIdx)
        {
            return epplusCells[rowIdx + 1, colIdx + 1].Text;
        }

        /// <summary>
        /// Retrieve cell value as a string, formatted with CultureInfo as specified in the corresponding Workbook object
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        public string GetCellValue(int rowIdx, int colIdx)
        {
            object value = epplusCells[rowIdx + 1, colIdx + 1].Value;
            if (value != null)
            {
                return SpreadsheetUtils.ConvertObjectValue(value, workbook.CultureInfo);
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
            object value = epplusCells[rowIdx + 1, colIdx + 1].Value;
            if (value != null)
            {
                return (cultureInfo != null) ? SpreadsheetUtils.ConvertObjectValue(value, cultureInfo) : value.ToString();
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
            object value = epplusCells[rowIdx + 1, colIdx + 1].Value;
            if (value != null)
            {
                return (cultureInfo != null) ? SpreadsheetUtils.ConvertObjectValue(value, cultureInfo, format) : value.ToString();
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
            return epplusCells[rowIdx + 1, colIdx + 1].Formula;
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
            ExcelAddressBase dimRange = epplusWorksheet.Dimension;
            if (dimRange == null)
            {
                return null;
            }

            long lastRowIdx = dimRange.Rows;
            long lastColIdx = dimRange.Columns;

            if (matchCase)
            {
                for (int row = 1; row <= lastRowIdx; row++)
                {
                    for (int col = 1; col <= lastColIdx; col++)
                    {
                        string value = GetCellValue(row - 1, col - 1);
                        if (value != null)
                        {
                            if (value.Equals(searchString) || (lookAtPart && value.Contains(searchString)))
                            {
                                return new Cell(this, row - 1, col - 1);
                            }
                        }
                    }
                }
            }
            else
            {
                searchString = searchString.ToLower();
                for (int row = 1; row <= lastRowIdx; row++)
                {
                    for (int col = 1; col <= lastColIdx; col++)
                    {
                        string value = GetCellValue(row - 1, col - 1);
                        if (value != null)
                        {
                            if (value.ToLower().Equals(searchString))
                            {
                                return new Cell(this, row - 1, col - 1);
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
                while (((value != null) && !value.Equals(string.Empty)) && (nextRowIdx >= 0) && (nextColIdx >= 0) && (nextRowIdx < epplusWorksheet.Cells.Rows) && (nextColIdx < epplusWorksheet.Cells.Columns))
                {
                    rowIdx = nextRowIdx;
                    colIdx = nextColIdx;
                    nextRowIdx += rowStep;
                    nextColIdx += colStep;
                    if ((rowIdx >= 0) && (colIdx >= 0) && (rowIdx < epplusWorksheet.Cells.Rows) && (colIdx < epplusWorksheet.Cells.Columns))
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
                while (((value == null) || value.Equals(string.Empty)) && (rowIdx >= 0) && (colIdx >= 0) && (rowIdx < epplusWorksheet.Cells.Rows) && (colIdx < epplusWorksheet.Cells.Columns))
                {
                    rowIdx += rowStep;
                    colIdx += colStep;
                    if ((rowIdx >= 0) && (colIdx >= 0) && (rowIdx < epplusWorksheet.Cells.Rows) && (colIdx < epplusWorksheet.Cells.Columns))
                    {
                        value = GetCellValue(rowIdx, colIdx);
                        if (value == null)
                        {
                            value = GetCellFormula(rowIdx, colIdx);
                        }
                    }
                }
                if (colIdx == epplusWorksheet.Cells.Columns)
                {
                    colIdx = epplusWorksheet.Cells.Columns - 1;
                }
                if (colIdx == -1)
                {
                    colIdx = 0;
                }
                if (rowIdx == epplusWorksheet.Cells.Rows)
                {
                    rowIdx = epplusWorksheet.Cells.Rows - 1;
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
                for (rangeRowIdx = range.RowIdx1; rangeRowIdx <= Math.Min(epplusWorksheet.Cells.Rows - 1, range.RowIdx2); rangeRowIdx++)
                {
                    for (rangeColIdx = range.ColIdx1; rangeColIdx <= Math.Min(epplusWorksheet.Cells.Columns - 1, range.ColIdx2); rangeColIdx++)
                    {
                        values[subRowIdx, subColIdx] = epplusCells[rangeRowIdx + 1, rangeColIdx + 1].Value;
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
            return epplusWorksheet.Cells.Rows;
        }

        /// <summary>
        /// Retrieve the number of columns in this sheet
        /// </summary>
        /// <returns></returns>
        public int GetColumnsCount()
        {
            return epplusWorksheet.Cells.Columns;
        }

        /// <summary>
        /// Retrieve the zero-based index of the last row in this worksheet
        /// </summary>
        public int LastRowIdx
        {
            get { return epplusWorksheet.Cells.Rows - 1; }
        }

        /// <summary>
        /// Retrieve the zero-based index of the last column in this worksheet
        /// </summary>
        public int LastColIdx
        {
            get { return epplusWorksheet.Cells.Columns - 1; }
        }

        /// <summary>
        /// Insert a column before specified column
        /// </summary>
        /// <param name="colIdx">zero-based column index</param>
        public void InsertColumn(int colIdx)
        {
            epplusWorksheet.InsertColumn(colIdx + 1, 1);
        }

        /// <summary>
        /// Insert a row before specified row
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        public void InsertRow(int rowIdx)
        {
            epplusWorksheet.InsertRow(rowIdx + 1, 1);
        }

        /// <summary>
        /// Delete specified row
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        public void DeleteRow(int rowIdx)
        {
            epplusWorksheet.DeleteRow(rowIdx + 1);
        }

        /// <summary>
        /// Delete specified rows
        /// </summary>
        /// <param name="startRowIdx">zero-based row index</param>
        /// <param name="rowCount"></param>
        public void DeleteRows(int startRowIdx, int rowCount)
        {
            epplusWorksheet.DeleteRow(startRowIdx, rowCount);
        }

        /// <summary>
        /// Delete specified column
        /// </summary>
        /// <param name="colIdx">zero-based column index</param>
        public void DeleteColumn(int colIdx)
        {
            epplusWorksheet.DeleteColumn(colIdx + 1);
        }

        /// <summary>
        /// Delete specified columns
        /// </summary>
        /// <param name="startColIdx">zero-based column index</param>
        /// <param name="colCount"></param>
        public void DeleteColumns(int startColIdx, int colCount)
        {
            epplusWorksheet.DeleteColumn(startColIdx + 1, colCount);
        }

        /// <summary>
        /// Clear cells in specified range; note because of a bug in EPPlus, clearing ranges does not work properly if drawing are present.
        /// </summary>
        /// <param name="range"></param>
        public void Clear(Range range = null)
        {
            if (range != null)
            {
                ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
                epplusRange.Clear();
            }
            else
            {
                /// Because of a bug in EPPlus, comments in drawings are not properly removed when Clea() is called. This can be solved by clearing Drawings explicitly before.
                epplusWorksheet.Drawings.Clear();
                epplusCells.Clear();
            }
        }

        /// <summary>
        /// Remove all comments in sheet
        /// </summary>
        public void ClearComments()
        {
            List<ExcelComment> comments = new List<ExcelComment>();
            for (int idx = 0; idx < epplusWorksheet.Comments.Count; idx++)
            {
                comments.Add(epplusWorksheet.Comments[idx]);
            }

            for (int idx = 0; idx < comments.Count; idx++)
            {
                if (comments[idx] != null)
                {
                    try
                    {
                        epplusWorksheet.Comments.Remove(comments[idx]);
                    }
                    catch (Exception)
                    {
                        // ignore
                    }
                }
            }
        }

        /// <summary>
        /// Check if the specified cell is empty
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public bool IsEmpty(Cell cell)
        {
            object cellValue = epplusCells[cell.RowIdx + 1, cell.ColIdx + 1].Value;
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
            object cellValue = epplusCells[rowIdx + 1, colIdx + 1].Value;
            return ((cellValue == null) || cellValue.Equals(string.Empty));
        }

        /// <summary>
        /// Retrieve the range of cells that are used, or null for an empty sheet
        /// </summary>
        /// <returns>a range of used cells or null</returns>
        public Range GetUsedRange()
        {
            ExcelAddressBase epplusUsedRange = epplusWorksheet.Dimension;
            Range usedRange = null;
            if (epplusUsedRange != null)
            {
                usedRange = new Range(this, epplusUsedRange.Start.Row - 1, epplusUsedRange.Start.Column - 1, epplusUsedRange.End.Row - 1, epplusUsedRange.End.Column - 1);
            }
            return usedRange;
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
            ExcelRange epplusRange = epplusCells[rowIdx1 + 1, colIdx1 + 1, rowIdx2 + 1, colIdx2 + 1];
            epplusRange.Merge = true;
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
            ExcelRange epplusRange = epplusCells[rowIdx1 + 1, colIdx1 + 1, rowIdx2 + 1, colIdx2 + 1];
            return epplusRange.Merge;
        }

        /// <summary>
        /// Retrieves the unique address strings (R1C1-format) for  all merged cells in this sheet
        /// </summary>
        public string[] GetMergedCells()
        {
            return epplusWorksheet.MergedCells.ToArray();
        }

        /// <summary>
        /// Retrieves the address string (R1C1-format) of the specified cell. For merged cells this will result in a range
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        public string GetMergedRange(int rowIdx, int colIdx)
        {
            return epplusWorksheet.MergedCells[rowIdx + 1, colIdx + 1];
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
            bool hasBorders = true;

            ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
            long lastRowIdx = range.Cell2.RowIdx;
            long lastColIdx = range.Cell2.ColIdx;

            // Check top left
            hasBorders &= HasBorders(range.Cell1.RowIdx, range.Cell1.ColIdx, true, false, true, false);
            // Check lower left
            hasBorders &= HasBorders(range.Cell2.RowIdx, range.Cell1.ColIdx, true, false, false, true);
            // Check top right
            hasBorders &= HasBorders(range.Cell1.RowIdx, range.Cell2.ColIdx, false, true, true, false);
            // Check lower right
            hasBorders &= HasBorders(range.Cell2.RowIdx, range.Cell2.ColIdx, false, true, false, true);

            // Check left side
            for (int rowIdx = range.Cell1.RowIdx + 1; rowIdx <= lastRowIdx; rowIdx++)
            {
                hasBorders &= HasBorders(rowIdx, range.Cell1.ColIdx, true, false, false, false);
            }
            // Check right side
            for (int rowIdx = range.Cell1.RowIdx + 1; rowIdx <= lastRowIdx; rowIdx++)
            {
                hasBorders &= HasBorders(rowIdx, range.Cell2.ColIdx, false, true, false, false);
            }
            // Check top side
            for (int colIdx = range.Cell1.ColIdx; colIdx <= lastColIdx; colIdx++)
            {
                hasBorders &= HasBorders(range.Cell1.RowIdx, colIdx, false, false, true, false);
            }
            // Check lower side
            for (int colIdx = range.Cell1.ColIdx; colIdx <= lastColIdx; colIdx++)
            {
                hasBorders &= HasBorders(range.Cell2.RowIdx, colIdx, false, false, false, true);
            }

            return hasBorders;
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
            ExcelRange epplusRange = epplusCells[rowIdx + 1, colIdx + 1];

            // EPPlus doesn't see the border of a neighbouring cell ... 

            bool hasBorders = true;

            bool hasEdge = (!checkLeft || (epplusRange.Style.Border.Left.Style != ExcelBorderStyle.None));
            if (!hasEdge && (colIdx > 0))
            {
                ExcelRange epplusLeftRange = epplusCells[rowIdx + 1, colIdx];
                hasEdge = (epplusLeftRange.Style.Border.Right.Style != ExcelBorderStyle.None);
            }
            hasBorders &= hasEdge;

            hasEdge = (!checkRight || (epplusRange.Style.Border.Right.Style != ExcelBorderStyle.None));
            if (!hasEdge)
            {
                ExcelRange epplusRightRange = epplusCells[rowIdx + 1, colIdx + 2];
                hasEdge = (epplusRightRange.Style.Border.Left.Style != ExcelBorderStyle.None);
            }
            hasBorders &= hasEdge;

            hasEdge = (!checkTop || (epplusRange.Style.Border.Top.Style != ExcelBorderStyle.None));
            if (!hasEdge && (rowIdx > 0))
            {
                ExcelRange epplusTopRange = epplusCells[rowIdx, colIdx + 1];
                hasEdge = (epplusTopRange.Style.Border.Bottom.Style != ExcelBorderStyle.None);
            }
            hasBorders &= hasEdge;

            hasEdge = (!checkBottom || (epplusRange.Style.Border.Bottom.Style != ExcelBorderStyle.None));
            if (!hasEdge)
            {
                ExcelRange epplusBottomRange = epplusCells[rowIdx + 2, colIdx + 1];
                hasEdge = (epplusBottomRange.Style.Border.Top.Style != ExcelBorderStyle.None);
            }
            hasBorders &= hasEdge;

            return hasBorders;
        }

        /// <summary>
        /// Retrieve the currently defined number format for the specified range
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public string GetNumberFormat(Range range)
        {
            ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
            return epplusRange.Style.Numberformat.Format;
        }

        /// <summary>
        /// Retrieve the currently defined number format for the specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        public string GetNumberFormat(int rowIdx, int colIdx)
        {
            ExcelRange epplusRange = epplusCells[rowIdx + 1, colIdx + 1];
            return epplusRange.Style.Numberformat.Format;
        }

        /// <summary>
        /// Set comment for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="comment"></param>
        public void SetComment(int rowIdx, int colIdx, string comment)
        {
            ExcelRange epplusRange = epplusCells[rowIdx + 1, colIdx + 1];
            if (epplusRange.Comment != null)
            {
                epplusRange.Comment.Text = string.Empty;
            }
            if ((comment != null) && !comment.Equals(string.Empty))
            {
                string username = workbook.GetUserName();
                if ((username == null) || username.Equals(string.Empty))
                {
                    username = "REF";
                }
                epplusRange.AddComment(comment, username);
            }
        }

        /// <summary>
        /// Delete comment for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        public void DeleteComment(int rowIdx, int colIdx)
        {
            ExcelRange epplusRange = epplusCells[rowIdx + 1, colIdx + 1];
            if (epplusRange.Comment != null)
            {
                epplusWorksheet.Comments.Remove(epplusRange.Comment);
            }
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
            SetBorderColor(color, BorderWeight.Thin);
        }

        /// <summary>
        /// Set color for all border lines in this sheet 
        /// </summary>
        /// <param name="color"></param>
        /// <param name="weight"></param>
        public void SetBorderColor(System.Drawing.Color color, BorderWeight weight = BorderWeight.Thin)
        {
            ExcelBorderStyle borderStyle = ExcelBorderStyle.None;
            switch (weight)
            {
                case BorderWeight.Hairline:
                    borderStyle = ExcelBorderStyle.Hair;
                    break;
                case BorderWeight.Thin:
                    borderStyle = ExcelBorderStyle.Thin;
                    break;
                case BorderWeight.Medium:
                    borderStyle = ExcelBorderStyle.Medium;
                    break;
                case BorderWeight.Thick:
                    borderStyle = ExcelBorderStyle.Thick;
                    break;
                default:
                    throw new LibraryException("Unknown Borderweight enum: " + weight);
            }

            epplusCells.Style.Border.BorderAround(borderStyle, color);
            epplusCells.Style.Border.Left.Style = borderStyle;
            epplusCells.Style.Border.Left.Color.SetColor(color);
            epplusCells.Style.Border.Top.Style = borderStyle;
            epplusCells.Style.Border.Top.Color.SetColor(color);
            epplusCells.Style.Border.Bottom.Style = borderStyle;
            epplusCells.Style.Border.Bottom.Color.SetColor(color);
            epplusCells.Style.Border.Right.Style = borderStyle;
            epplusCells.Style.Border.Right.Color.SetColor(color);

            SetBorderColor(new Range(this, 0, 0, 0, epplusCells.Columns - 1), color, weight, true);
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
            ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];

            ExcelBorderStyle borderStyle = ExcelBorderStyle.None;
            switch (weight)
            {
                case BorderWeight.Hairline:
                    borderStyle = ExcelBorderStyle.Hair;
                    break;
                case BorderWeight.Thin:
                    borderStyle = ExcelBorderStyle.Thin;
                    break;
                case BorderWeight.Medium:
                    borderStyle = ExcelBorderStyle.Medium;
                    break;
                case BorderWeight.Thick:
                    borderStyle = ExcelBorderStyle.Thick;
                    break;
                default:
                    throw new LibraryException("Unknown Borderweight enum: " + weight);
            }

            epplusRange.Style.Border.BorderAround(borderStyle, color);

            if (isInside)
            {
                epplusRange.Style.Border.Left.Style = borderStyle;
                epplusRange.Style.Border.Left.Color.SetColor(color);
                epplusRange.Style.Border.Top.Style = borderStyle;
                epplusRange.Style.Border.Top.Color.SetColor(color);
                epplusRange.Style.Border.Bottom.Style = borderStyle;
                epplusRange.Style.Border.Bottom.Color.SetColor(color);
                epplusRange.Style.Border.Right.Style = borderStyle;
                epplusRange.Style.Border.Right.Color.SetColor(color);
            }
        }

        /// <summary>
        /// Set color for horizontal inner border lines in specified range (not available for EPPlus)
        /// </summary>
        /// <param name="range"></param>
        /// <param name="themeColor">index of color in current theme</param>
        /// <param name="tintAndShade">value that defines tint and shade of theme color</param>
        public void SetBorderInsideHorizontalColor(Range range, int themeColor, double tintAndShade)
        {
            //ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
            //if (epplusRange.Style.Border.Bottom.Style == ExcelBorderStyle.None)
            //{
            //    epplusRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
            //}
            //epplusRange.Style.Border.Bottom.Color.LookupColor(0);
            //epplusRange.Style.Border.Bottom.Color.Tint = (decimal) tintAndShade;
        }

        /// <summary>
        /// Set color for horizontal inner border lines in specified range (not available for EPPlus)
        /// </summary>
        /// <param name="range"></param>
        /// <param name="themeColor">index of color in current theme</param>
        /// <param name="tintAndShade">value that defines tint and shade of theme color</param>
        public void SetBorderEdgeBottomColor(Range range, int themeColor, double tintAndShade)
        {
            // not available
        }

        /// <summary>
        /// Set color for horizontal bottom border line in specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="color"></param>
        public void SetBorderEdgeBottomColor(Range range, System.Drawing.Color color)
        {
            ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
            ExcelBorderStyle borderStyle = epplusRange.Style.Border.Bottom.Style;
            if (borderStyle == ExcelBorderStyle.None)
            {
                borderStyle = ExcelBorderStyle.Thin;
            }

            // Force bottom style since it can happen that a borderstyle is only partially present in the range
            epplusRange.Style.Border.Bottom.Style = borderStyle;

            // Actually set color
            epplusRange.Style.Border.Bottom.Color.SetColor(color);
        }

        /// <summary>
        /// Set color for horizontal top border line in specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="color"></param>
        public void SetBorderEdgeTopColor(Range range, System.Drawing.Color color)
        {
            ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
            ExcelBorderStyle borderStyle = epplusRange.Style.Border.Bottom.Style;
            if (borderStyle == ExcelBorderStyle.None)
            {
                borderStyle = ExcelBorderStyle.Thin;
            }

            // Force bottom style since it can happen that a borderstyle is only partially present in the range
            epplusRange.Style.Border.Top.Style = borderStyle;

            // Actually set color
            epplusRange.Style.Border.Top.Color.SetColor(color);
        }

        /// <summary>
        /// Set number format for specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="numberFormatString">a format string for the used application</param>
        public void SetNumberFormat(Range range, string numberFormatString)
        {
            ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
            epplusRange.Style.Numberformat.Format = numberFormatString;
        }

        /// <summary>
        /// Set number format for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="numberFormatString"></param>
        public void SetNumberFormat(int rowIdx, int colIdx, string numberFormatString)
        {
            ExcelRange epplusRange = epplusCells[rowIdx + 1, colIdx + 1];
            epplusRange.Style.Numberformat.Format = numberFormatString;
        }

        /// <summary>
        /// Specify if text should be wrapped in specified cells
        /// </summary>
        /// <param name="range"></param>
        /// <param name="isWrapped">if true, bold font will be set</param>
        public void SetWrapText(Range range, bool isWrapped)
        {
            ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
            epplusRange.Style.WrapText = isWrapped;
        }

        /// <summary>
        /// Specify if italic font should be used for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="isItalic">if true, italic font will be set</param>
        public void SetFontItalic(int rowIdx, int colIdx, bool isItalic)
        {
            ExcelRange epplusRange = epplusCells[rowIdx + 1, colIdx + 1];
            epplusRange.Style.Font.Italic = isItalic;
        }

        /// <summary>
        /// Specify if bold font should be used for specified cell
        /// </summary>
        /// <param name="range"></param>
        /// <param name="isBold">if true, bold font will be set</param>
        public void SetFontBold(Range range, bool isBold)
        {
            ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
            epplusRange.Style.Font.Bold = isBold;
        }

        /// <summary>
        /// Specify if bold font should be used for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="isBold">if true, bold font will be set</param>
        public void SetFontBold(int rowIdx, int colIdx, bool isBold)
        {
            ExcelRange epplusRange = epplusCells[rowIdx + 1, colIdx + 1];
            epplusRange.Style.Font.Bold = isBold;
        }

        /// <summary>
        /// Specify font size to given cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="size">number of pixels</param>
        public void SetFontSize(int rowIdx, int colIdx, int size)
        {
            ExcelRange epplusRange = epplusCells[rowIdx + 1, colIdx + 1];
            epplusRange.Style.Font.Size = size;
        }

        /// <summary>
        /// Checks if Excel cell is formatted as a date and/or time. Note: following checks are done in this order: 
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
            ExcelRange cell = epplusCells[rowIdx + 1, colIdx + 1];
            if ((cell.Value is DateTime) || cell.Text.Contains(":"))
            {
                return true;
            }

            if (checkedNumberFormat == null)
            {
                checkedNumberFormat = "-d";
            }

            return (cell.Style.Numberformat != null) && cell.Style.Numberformat.Format.ToLower().Contains(checkedNumberFormat);
        }

        /// <summary>
        /// Retrieve cell value as a DateTime object, assuming the underlying cell value is a DateTime-cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        public DateTime? GetCellDateTimeValue(int rowIdx, int colIdx)
        {
            object value = epplusCells[rowIdx + 1, colIdx + 1].Value;
            if (value != null)
            {
                if (value is DateTime)
                {
                    return (DateTime?) value;
                } 
                else if (value is double)
                {
                    return DateTime.FromOADate((double)value);
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Get current font color of specified range
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public Color GetFontColor(Range range)
        {
            ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
            string rgb = epplusRange.Style.Font.Color.Rgb;
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
            ExcelRange epplusRange = epplusCells[rowIdx + 1, colIdx + 1];
            string rgb = epplusRange.Style.Font.Color.Rgb;
            return Color.Black;
        }

        /// <summary>
        /// Set font color for specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="color"></param>
        public void SetFontColor(Range range, Color color)
        {
            ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
            epplusRange.Style.Font.Color.SetColor(color);
        }

        /// <summary>
        /// Set font color for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="color"></param>
        public void SetFontColor(int rowIdx, int colIdx, Color color)
        {
            ExcelRange epplusRange = epplusCells[rowIdx + 1, colIdx + 1];
            epplusRange.Style.Font.Color.SetColor(color);
        }


        /// <summary>
        /// Get color of cell interior for specified range
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public Color GetInteriorColor(Range range)
        {
            ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
            string rgb = epplusRange.Style.Fill.BackgroundColor.Rgb;
            return Color.Black;
        }

        /// <summary>
        /// Get color of cell interior for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        public Color GetInteriorColor(int rowIdx, int colIdx)
        {
            ExcelRange epplusRange = epplusCells[rowIdx + 1, colIdx + 1];
            string rgb = epplusRange.Style.Fill.BackgroundColor.Rgb;
            return Color.Black;
        }

        /// <summary>
        /// Set color of cell interior for specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="color"></param>
        public void SetInteriorColor(Range range, Color color)
        {
            ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
            if (epplusRange.Style.Fill.PatternType == ExcelFillStyle.None)
            {
                epplusRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            }
            epplusRange.Style.Fill.BackgroundColor.SetColor(color);
        }

        /// <summary>
        /// Set color of cell interior for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="color"></param>
        public void SetInteriorColor(int rowIdx, int colIdx, Color color)
        {
            ExcelRange epplusRange = epplusCells[rowIdx + 1, colIdx + 1];
            if (epplusRange.Style.Fill.PatternType == ExcelFillStyle.None)
            {
                epplusRange.Style.Fill.PatternType = ExcelFillStyle.Solid;
            }
            epplusRange.Style.Fill.BackgroundColor.SetColor(color);
        }

        /// <summary>
        /// Set vertical alignment to center of cells for specified range
        /// </summary>
        /// <param name="range"></param>
        public void SetVerticalAlignmentTop(Range range)
        {
            ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
            epplusRange.Style.VerticalAlignment = ExcelVerticalAlignment.Top;
        }

        /// <summary>
        /// Set vertical alignment to center of cells for specified range
        /// </summary>
        /// <param name="range"></param>
        public void SetVerticalAlignmentCenter(Range range)
        {
            ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
            epplusRange.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }

        /// <summary>
        /// Set horizontal alignment to left of cells for specified range
        /// </summary>
        /// <param name="range"></param>
        public void SetHorizontalAlignmentLeft(Range range)
        {
            ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
            epplusRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
        }

        /// <summary>
        /// Set horizontal alignment to left of specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        public void SetHorizontalAlignmentLeft(int rowIdx, int colIdx)
        {
            ExcelRange epplusRange = epplusCells[rowIdx + 1, colIdx + 1];
            epplusRange.Style.HorizontalAlignment = ExcelHorizontalAlignment.Left;
        }

        /// <summary>
        /// Set orientation of cell contents for specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="degrees"></param>
        public void SetOrientation(Range range, int degrees)
        {
            ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
            epplusRange.Style.TextRotation = degrees;
        }

        /// <summary>
        /// Turn auto filter on for specified range
        /// </summary>
        /// <param name="range"></param>
        public void SetAutoFilter(Range range)
        {
            ExcelRange epplusRange = epplusCells[range.RowIdx1 + 1, range.ColIdx1 + 1, range.RowIdx2 + 1, range.ColIdx2 + 1];
            epplusRange.AutoFilter = true;
        }

        /// <summary>
        /// Freeze rows above specified row
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        public void FreezeRow(int rowIdx)
        {
            epplusWorksheet.View.FreezePanes(rowIdx + 2, 1);
        }

        /// <summary>
        /// Auto fit column widths in specified range
        /// </summary>
        /// <param name="range">null to fit all column</param>
        public void AutoFitColumns(Range range = null)
        {
            if (range != null)
            {
                epplusWorksheet.Cells[range.Cell1.RowIdx + 1, range.Cell1.ColIdx + 1, range.Cell2.RowIdx + 1, range.Cell2.ColIdx + 1].AutoFitColumns();
            }
            else
            {
                epplusCells.AutoFitColumns();
            }
        }

        /// <summary>
        /// Auto fit width of specified column
        /// </summary>
        /// <param name="colIdx">zero-based column index</param>
        public void AutoFitColumn(int colIdx)
        {
            epplusWorksheet.Column(colIdx + 1).AutoFit();
        }

        /// <summary>
        /// Set width of specified column
        /// </summary>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="width"></param>
        public void SetColumnWidth(int colIdx, int width)
        {
            epplusWorksheet.Column(colIdx + 1).Width = width;
        }

        /// <summary>
        /// Hide columns in specified range
        /// </summary>
        /// <param name="range"></param>
        public void HideColumns(Range range)
        {
            for (int colIdx = range.ColIdx1; colIdx <= range.ColIdx2; colIdx++)
            {
                epplusWorksheet.Column(colIdx).Hidden = true;
            }
        }

        /// <summary>
        /// Hide rows in specified range
        /// </summary>
        /// <param name="range"></param>
        public void HideRows(Range range)
        {
            for (int rowIdx = range.RowIdx1; rowIdx <= range.RowIdx2; rowIdx++)
            {
                epplusWorksheet.Row(rowIdx).Hidden = true;
            }
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
            // Can't get hyperlinks to work in normal way, use workaround with Excel formula
            if ((address != null) && ((!Path.IsPathRooted(address) && !address.Contains(":")) || (Path.IsPathRooted(address) && File.Exists(address))))
            {
                if (subAddress == null)
                {
                    epplusCells[rowIdx + 1, colIdx + 1].Formula = "HYPERLINK(\"" + address + "\",\"" + textToDisplay + "\")";
                }
                else
                {
                    if (subAddress.Contains(" "))
                    {
                        if (!subAddress.StartsWith("'"))
                        {
                            subAddress = "'" + subAddress;
                        }
                        if (!subAddress.Contains("!"))
                        {
                            if (!subAddress.EndsWith("'"))
                            {
                                subAddress = subAddress + "'";
                            }
                        }
                        else
                        {
                            if (!subAddress.Contains("'!"))
                            {
                                subAddress = subAddress.Replace("!", "'!");
                            }
                        }
                    }
                    if (!subAddress.Contains("!"))
                    {
                        subAddress += "!A1";
                    }
                    epplusCells[rowIdx + 1, colIdx + 1].Formula = "HYPERLINK(\"" + address + "#" + subAddress + "\",\"" + textToDisplay + "\")";
                }
            }
            else if (address == null)
            {
                if (subAddress.Contains(" "))
                {
                    if (!subAddress.StartsWith("'"))
                    {
                        subAddress = "'" + subAddress;
                    }
                    if (!subAddress.Contains("!"))
                    {
                        if (!subAddress.EndsWith("'"))
                        {
                            subAddress = subAddress + "'";
                        }
                    }
                    else
                    {
                        if (!subAddress.Contains("'!"))
                        {
                            subAddress = subAddress.Replace("!", "'!");
                        }
                    }
                }
                if (!subAddress.Contains("!"))
                {
                    subAddress += "!A1";
                }
                epplusCells[rowIdx + 1, colIdx + 1].Formula = "HYPERLINK(\"#" + subAddress + "\",\"" + textToDisplay + "\")";
            }
            else
            {
                // example links
                // HYPERLINK to a website:
                // excelRange.Hyperlink = new Uri("http://www.google.com", UriKind.Absolute);  
                // HYPERLINK to another sheet within same excel file:
                // excelRange.Hyperlink = new Uri("#'Sheet2'!B1", UriKind.Relative);  
                // HYPERLINK to any local file:
                // excelRange.Hyperlink = new Uri(@ "D:\sample.xlsx");  

                Uri uri = null;
                if (address != null)
                {
                    if (subAddress != null)
                    {
                        uri = new Uri(new Uri(address, UriKind.Absolute), subAddress);
                        textToDisplay = textToDisplay ?? address + subAddress;
                    }
                    else
                    {
                        uri = new Uri(address, UriKind.Absolute);
                        textToDisplay = textToDisplay ?? address;
                    }
                }
                else
                {
                    if (subAddress != null)
                    {
                        uri = new Uri(subAddress, UriKind.Relative);
                        textToDisplay = textToDisplay ?? subAddress;
                    }
                    else
                    {
                        throw new Exception("Invalid hyperlink: please specify address and/or subaddress");
                    }
                }

                epplusCells[rowIdx + 1, colIdx + 1].Hyperlink = new ExcelHyperLink(uri.ToString(), textToDisplay);
            }

            try
            {
                epplusCells[rowIdx + 1, colIdx + 1].StyleName = "HyperLink";
            }
            catch (Exception)
            {
                // ignore, Hyperlink style may not have been created correctly
            }
        }

        /// <summary>
        /// Create chart object of given type inside this sheet at specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="chartType"></param>
        /// <returns></returns>
        public IChart CreateChart(Range range, ChartType chartType)
        {
            EPPlusChart chart = new EPPlusChart(this, chartType);
            chart.SetPosition(range);
            return chart;
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
            ExcelRange sourceEPPlusCell = epplusWorksheet.Cells[sourceRowIdx + 1, sourceColIdx + 1];
            ExcelRange targetEPPlusCell = epplusWorksheet.Cells[targetRowIdx + 1, targetColIdx + 1];
            sourceEPPlusCell.Copy(targetEPPlusCell);
        }

        /// <summary>
        /// Copy characteristics and contents of specified source cell to specified target cell
        /// </summary>
        /// <param name="sourceCell"></param>
        /// <param name="targetCell"></param>
        public void CopyCell(Cell sourceCell, Cell targetCell)
        {
            ExcelRange sourceEPPlusCell = epplusWorksheet.Cells[sourceCell.RowIdx + 1, sourceCell.ColIdx + 1];
            ExcelWorksheet targetSheet = (ExcelWorksheet)targetCell.Sheet.GetBaseObject();
            ExcelRange targetEPPlusCell = targetSheet.Cells[targetCell.RowIdx + 1, targetCell.ColIdx + 1];
            sourceEPPlusCell.Copy(targetEPPlusCell);
        }

        /// <summary>
        /// Copy characteristics and contents of cells in specified source range to location defined by specified upper left target cell
        /// </summary>
        /// <param name="range"></param>
        /// <param name="targetCell"></param>
        public void CopyRange(Range range, Cell targetCell)
        {
            ExcelWorksheet targetSheet = (ExcelWorksheet)targetCell.Sheet.GetBaseObject();

            ExcelRange targetEPPlusCell = targetSheet.Cells[targetCell.RowIdx + 1, targetCell.ColIdx + 1];
            epplusWorksheet.Cells[range.Cell1.RowIdx + 1, range.Cell1.ColIdx + 1, range.Cell2.RowIdx + 1, range.Cell2.ColIdx + 1].Copy(targetEPPlusCell);
        }

        /// <summary>
        /// Calculate formula's in sheet dynamically
        /// </summary>
        public void Calculate()
        {
            epplusWorksheet.Calculate();
        }

        /// <summary>
        /// Calculate specified Excel-formula dynamically
        /// </summary>
        /// <param name="formula"></param>
        /// <returns>result of formula</returns>
        public object Calculate(string formula)
        {
            return epplusWorksheet.Calculate(formula);
        }

        /// <summary>
        /// Zoom in to the specified percentage of the current sheet
        /// </summary>
        /// <param name="percentage"></param>
        public void Zoom(int percentage)
        {
            epplusWorksheet.View.ZoomScale = percentage;
        }

        /// <summary>
        /// Apply specified header, footer and bitmap to this sheet
        /// </summary>
        /// <param name="leftHeader"></param>
        /// <param name="leftFooter"></param>
        /// <param name="bitmap"></param>
        public void ApplyPageSetup(string leftHeader, string leftFooter, System.Drawing.Bitmap bitmap = null)
        {
            ExcelWorksheetView epplusView = epplusWorksheet.View;
            epplusWorksheet.HeaderFooter.FirstHeader.LeftAlignedText = leftHeader;
            epplusWorksheet.HeaderFooter.FirstFooter.LeftAlignedText = leftFooter;
            epplusWorksheet.HeaderFooter.FirstHeader.InsertPicture(bitmap, PictureAlignment.Right);
        }

        /// <summary>
        /// Set page orientation of this sheet to landscape
        /// </summary>
        public void SetPageOrientationLandscape()
        {
            epplusWorksheet.PrinterSettings.Orientation = eOrientation.Landscape;
            //epplusWorksheet.PrinterSettings.FitToHeight = 0;
            //epplusWorksheet.PrinterSettings.FitToPage = true;
            //epplusWorksheet.PrinterSettings.FitToWidth = 1;

        }

        /// <summary>
        /// Set page size of this sheet to specified size
        /// </summary>
        /// <param name="papersize"></param>
        public void SetPapersize(PaperSize papersize)
        {
            switch (papersize)
            {
                case PaperSize.A2:
                    epplusWorksheet.PrinterSettings.PaperSize = ePaperSize.A2;
                    break;
                case PaperSize.A3:
                    epplusWorksheet.PrinterSettings.PaperSize = ePaperSize.A3;
                    break;
                case PaperSize.A4:
                    epplusWorksheet.PrinterSettings.PaperSize = ePaperSize.A4;
                    break;
                default:
                    throw new LibraryException("Unknown papersize: " + papersize);
            }
        }

        /// <summary>
        /// Set view mode of worksheet to page layout
        /// </summary>
        public void SetPageLayoutView()
        {
            epplusWorksheet.View.PageLayoutView = true;
        }

        /// <summary>
        /// Retrieves the underlying EPPlus-object that refers to the EPPlus-implemention of a worksheet
        /// </summary>
        /// <returns></returns>
        public object GetBaseObject()
        {
            return epplusWorksheet;
        }
    }
}
