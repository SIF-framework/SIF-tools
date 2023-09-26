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
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Spreadsheets
{
    /// <summary>
    /// 
    /// </summary>
    public enum PaperSize
    {
        /// <summary>
        /// A2 paper (420 mm by 594 mm)
        /// </summary>
        A2,

        /// <summary>
        /// A3 paper (297 mm by 420 mm)
        /// </summary>
        A3,

        /// <summary>
        /// A4 paper (210 mm by 297 mm)
        /// </summary>
        A4
    }

    /// <summary>
    /// Interface for worksheet
    /// </summary>
    public interface IWorksheet
    {
        /// <summary>
        /// Retrieve the workbook that contains this sheet
        /// </summary>
        IWorkbook Workbook { get; }

        /// <summary>
        /// Activate this worksheet
        /// </summary>
        void Activate();

        /// <summary>
        /// Set the name of this sheet to the specified string
        /// </summary>
        /// <param name="sheetname"></param>
        void SetSheetname(string sheetname);

        /// <summary>
        /// Get the name of this sheet
        /// </summary>
        string GetSheetname();

        /// <summary>
        /// Select specified cell in this sheet
        /// </summary>
        /// <param name="range"></param>
        void Select(Range range);

        /// <summary>
        /// Select specified cell in this sheet
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        void Select(int rowIdx, int colIdx);

        /// <summary>
        /// Activate specified cell in this sheet
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        void Activate(int rowIdx, int colIdx);

        /// <summary>
        /// Set value for specified cell with a string, but find it's type by parsing this string with the CultureInfo as specified in the corresponding Workbook object.
        /// Use SetCellValue(rowIdx, colIdx, (object) value) to actually write the object value to the cell without parsing
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="value"></param>
        void SetCellValue(int rowIdx, int colIdx, string value);

        /// <summary>
        /// Set value for specified cell with a string, but find it's type by parsing this string with the specified CultureInfo, or use null to avoid parsing and always use string type
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="value"></param>
        /// <param name="parseCultureInfo"></param>
        void SetCellValue(int rowIdx, int colIdx, string value, CultureInfo parseCultureInfo);

        /// <summary>
        /// Set value for specified cell with a long
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="value"></param>
        void SetCellValue(int rowIdx, int colIdx, long value);

        /// <summary>
        /// Set value for specified cell with a float
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="value"></param>
        void SetCellValue(int rowIdx, int colIdx, float value);

        /// <summary>
        /// Set value for specified cell with a double
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="value"></param>
        void SetCellValue(int rowIdx, int colIdx, double value);

        /// <summary>
        /// Set value for specified cell with a DateTime object
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="value"></param>
        void SetCellValue(int rowIdx, int colIdx, DateTime value);

        /// <summary>
        /// Set value for specified cell with value of specified object 
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="value"></param>
        void SetCellValue(int rowIdx, int colIdx, object value);

        /// <summary>
        /// Set formula for specified cell. Do not prefix with '='-symbol.
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="formula"></param>
        void SetCellFormula(int rowIdx, int colIdx, string formula);

        /// <summary>
        /// Set R1C1-formula for specified cell. Do not prefix with '='-symbol.
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="formulaR1C1"></param>
        void SetCellFormulaR1C1(int rowIdx, int colIdx, string formulaR1C1);

        /// <summary>
        /// Get cell value as an object with a actual cell value
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        object GetCellObjectValue(int rowIdx, int colIdx);

        /// <summary>
        /// Retrieve cell text value as formatted in Excel workbook
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        string GetCellText(int rowIdx, int colIdx);

        /// <summary>
        /// Retrieve cell value as a string, formatted with CultureInfo as specified in the corresponding Workbook object
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        string GetCellValue(int rowIdx, int colIdx);

        /// <summary>
        /// Retrieve cell value as a string, formatted with the specified CultureInfo, or use null to avoid formatting
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="parseCultureInfo"></param>
        /// <returns></returns>
        string GetCellValue(int rowIdx, int colIdx, CultureInfo parseCultureInfo);

        /// <summary>
        /// Retrieve cell value as a string, formatted with the specified CultureInfo and format string
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="parseCultureInfo"></param>
        /// <param name="format">see C# documentation possible format strings</param>
        /// <returns></returns>
        string GetCellValue(int rowIdx, int colIdx, CultureInfo parseCultureInfo, string format);

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
        bool IsDateTimeCell(int rowIdx, int colIdx, string checkedNumberFormat = null);

        /// <summary>
        /// Retrieve cell value as a DateTime object, assuming the underlying cell value is a DateTime-cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns>DateTime, or null for invalid cell values</returns>
        DateTime? GetCellDateTimeValue(int rowIdx, int colIdx);

        /// <summary>
        /// Get formula in specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        string GetCellFormula(int rowIdx, int colIdx);

        /// <summary>
        /// Get Cell instance for specified Cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        Cell GetCell(int rowIdx, int colIdx);

        /// <summary>
        /// Find specified cell
        /// </summary>
        /// <param name="searchString"></param>
        /// <param name="matchCase">true to match case</param>
        /// <param name="lookAtPart">false to match whole string</param>
        /// <returns></returns>
        Cell FindCell(string searchString, bool matchCase = false, bool lookAtPart = false);

        /// <summary>
        /// Retrieve last cell (before empty cell) moving from specified cell in given direction
        /// </summary>
        /// <param name="cell"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        Cell End(Cell cell, CellDirection direction);

        /// <summary>
        /// Retrieve a Range instance for specified upper left and lower right cells
        /// </summary>
        /// <param name="cell1"></param>
        /// <param name="cell2"></param>
        /// <returns></returns>
        Range GetRange(Cell cell1, Cell cell2);

        /// <summary>
        /// Retrieve a Range instance for specified upper left and lower right cells
        /// </summary>
        /// <param name="rowIdx1">zero-based row index of upper left cell in range</param>
        /// <param name="colIdx1"></param>
        /// <param name="rowIdx2">zero-based row index of lower right cell in range</param>
        /// <param name="colIdx2"></param>
        /// <returns></returns>
        Range GetRange(int rowIdx1, int colIdx1, int rowIdx2, int colIdx2);

        /// <summary>
        /// Retrieve 2D-array with cell values in specified range
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        object[,] GetValues(Range range);

        /// <summary>
        /// Retrieve the number of rows in this sheet
        /// </summary>
        /// <returns></returns>
        int GetRowsCount();

        /// <summary>
        /// Retrieve the number of columns in this sheet
        /// </summary>
        /// <returns></returns>
        int GetColumnsCount();

        /// <summary>
        /// Retrieve the zero-based index of the last row in this worksheet
        /// </summary>
        int LastRowIdx { get; }

        /// <summary>
        /// Retrieve the zero-based index of the last column in this worksheet
        /// </summary>
        int LastColIdx { get; }

        /// <summary>
        /// Insert a column before specified column
        /// </summary>
        /// <param name="colIdx">zero-based column index</param>
        void InsertColumn(int colIdx);

        /// <summary>
        /// Insert a row before specified row
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        void InsertRow(int rowIdx);

        /// <summary>
        /// Delete specified row
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        void DeleteRow(int rowIdx);

        /// <summary>
        /// Delete specified rows
        /// </summary>
        /// <param name="startRowIdx">zero-based row index</param>
        /// <param name="rowCount"></param>
        void DeleteRows(int startRowIdx, int rowCount);

        /// <summary>
        /// Delete specified column
        /// </summary>
        /// <param name="colIdx">zero-based column index</param>
        void DeleteColumn(int colIdx);

        /// <summary>
        /// Delete specified columns
        /// </summary>
        /// <param name="startColIdx">zero-based column index</param>
        /// <param name="colCount"></param>
        void DeleteColumns(int startColIdx, int colCount);

        /// <summary>
        /// Check if the specified cell is empty
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        bool IsEmpty(Cell cell);

        /// <summary>
        /// Check if the specified cell is empty
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        bool IsEmpty(int rowIdx, int colIdx);

        /// <summary>
        /// Retrieve the range of cells that are used, or null for an empty sheet
        /// </summary>
        /// <returns>a range of used cells or null</returns>
        Range GetUsedRange();

        /// <summary>
        /// Merge cells in specified range
        /// </summary>
        /// <param name="rowIdx1">zero-based row index of upper left cell in range</param>
        /// <param name="colIdx1">zero-based column index of upper left cell in range</param>
        /// <param name="rowIdx2">zero-based row index of lower right cell in range</param>
        /// <param name="colIdx2">zero-based column index of lower right cell in range</param>
        void MergeCells(int rowIdx1, int colIdx1, int rowIdx2, int colIdx2);

        /// <summary>
        /// Checks if cells in specified range are merged, but not if these cells are merged together
        /// </summary>
        /// <param name="rowIdx1">zero-based row index of upper left cell in range</param>
        /// <param name="colIdx1">zero-based column index of upper left cell in range</param>
        /// <param name="rowIdx2">zero-based row index of lower right cell in range</param>
        /// <param name="colIdx2">zero-based column index of lower right cell in range</param>
        /// <returns></returns>
        bool IsMerged(int rowIdx1, int colIdx1, int rowIdx2, int colIdx2);

        /// <summary>
        /// Retrieves the unique address strings (R1C1-format) for  all merged cells in this sheet
        /// </summary>
        string[] GetMergedCells();

        /// <summary>
        /// Retrieves the address string (R1C1-format) of the specified cell. For merged cells this will result in a range
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        string GetMergedRange(int rowIdx, int colIdx);

        /// <summary>
        /// Check if the cells in the given range have borders on the specified sides
        /// </summary>
        /// <param name="range"></param>
        /// <param name="checkLeft"></param>
        /// <param name="checkRight"></param>
        /// <param name="checkTop"></param>
        /// <param name="checkBottom"></param>
        /// <returns></returns>
        bool HasBorders(Range range, bool checkLeft = true, bool checkRight = true, bool checkTop = true, bool checkBottom = true);

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
        bool HasBorders(int rowIdx, int colIdx, bool checkLeft = true, bool checkRight = true, bool checkTop = true, bool checkBottom = true);

        /// <summary>
        /// Retrieve the currently defined number format for the specified range
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        string GetNumberFormat(Range range);

        /// <summary>
        /// Retrieve the currently defined number format for the specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        string GetNumberFormat(int rowIdx, int colIdx);

        /// <summary>
        /// Set comment for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="comment"></param>
        void SetComment(int rowIdx, int colIdx, string comment);

        /// <summary>
        /// Delete comment for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        void DeleteComment(int rowIdx, int colIdx);

        /// <summary>
        /// Set color for all border lines in this sheet 
        /// </summary>
        /// <param name="color"></param>
        void SetBorderColor(Color color);

        /// <summary>
        /// Set color and weight for border lines in specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="color"></param>
        /// <param name="weight"></param>
        /// <param name="isInside">if true, specified settings are also used for inner lines</param>
        void SetBorderColor(Range range, Color color, BorderWeight weight, bool isInside = false);

        /// <summary>
        /// Set color for horizontal inner border lines in specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="themeColor">index of color in current theme</param>
        /// <param name="tintAndShade">value that defines tint and shade of theme color</param>
        void SetBorderInsideHorizontalColor(Range range, int themeColor, double tintAndShade);

        /// <summary>
        /// Set color for horizontal inner border lines in specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="themeColor">index of color in current theme</param>
        /// <param name="tintAndShade">value that defines tint and shade of theme color</param>
        void SetBorderEdgeBottomColor(Range range, int themeColor, double tintAndShade);

        /// <summary>
        /// Set color for horizontal inner border lines in specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="color"></param>
        void SetBorderEdgeBottomColor(Range range, Color color);

        //Color GetBorderEdgeLeftColor(Range range);
        //Color GetBorderEdgeRightColor(Range range);
        //Color GetBorderEdgeTopColor(Range range);
        //Color GetBorderEdgeBottomColor(Range range);

        /// <summary>
        /// Set number format for specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="numberFormatString">a format string for the used application</param>
        void SetNumberFormat(Range range, string numberFormatString);

        /// <summary>
        /// Set number format for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="numberFormatString"></param>
        void SetNumberFormat(int rowIdx, int colIdx, string numberFormatString);

        /// <summary>
        /// Specify if text should be wrapped in specified cells
        /// </summary>
        /// <param name="range"></param>
        /// <param name="isWrapped">if true, bold font will be set</param>
        void SetWrapText(Range range, bool isWrapped);

        /// <summary>
        /// Specify if italic font should be used for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="isItalic">if true, italic font will be set</param>
        void SetFontItalic(int rowIdx, int colIdx, bool isItalic);

        /// <summary>
        /// Specify if bold font should be used for specified cell
        /// </summary>
        /// <param name="range"></param>
        /// <param name="isBold">if true, bold font will be set</param>
        void SetFontBold(Range range, bool isBold);

        /// <summary>
        /// Specify if bold font should be used for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="isBold">if true, bold font will be set</param>
        void SetFontBold(int rowIdx, int colIdx, bool isBold);

        /// <summary>
        /// Specify font size to given cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="size">number of pixels</param>
        void SetFontSize(int rowIdx, int colIdx, int size);

        /// <summary>
        /// Get current font color of specified range
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        Color GetFontColor(Range range);

        /// <summary>
        /// Get current font color of specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        Color GetFontColor(int rowIdx, int colIdx);

        /// <summary>
        /// Set font color for specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="color"></param>
        void SetFontColor(Range range, Color color);

        /// <summary>
        /// Set font color for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="color"></param>
        void SetFontColor(int rowIdx, int colIdx, Color color);

        /// <summary>
        /// Get color of cell interior for specified range
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        Color GetInteriorColor(Range range);

        /// <summary>
        /// Get color of cell interior for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <returns></returns>
        Color GetInteriorColor(int rowIdx, int colIdx);

        /// <summary>
        /// Set color of cell interior for specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="color"></param>
        void SetInteriorColor(Range range, Color color);

        /// <summary>
        /// Set color of cell interior for specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="color"></param>
        void SetInteriorColor(int rowIdx, int colIdx, Color color);

        /// <summary>
        /// Set vertical alignment to top of cells for specified range
        /// </summary>
        /// <param name="range"></param>
        void SetVerticalAlignmentTop(Range range);

        /// <summary>
        /// Set vertical alignment to center of cells for specified range
        /// </summary>
        /// <param name="range"></param>
        void SetVerticalAlignmentCenter(Range range);

        /// <summary>
        /// Set horizontal alignment to left of cells for specified range
        /// </summary>
        /// <param name="range"></param>
        void SetHorizontalAlignmentLeft(Range range);

        /// <summary>
        /// Set horizontal alignment to left of specified cell
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        void SetHorizontalAlignmentLeft(int rowIdx, int colIdx);

        /// <summary>
        /// Set orientation of cell contents for specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="degrees"></param>
        void SetOrientation(Range range, int degrees);

        /// <summary>
        /// Turn auto filter on for specified range
        /// </summary>
        /// <param name="range"></param>
        void SetAutoFilter(Range range);

        /// <summary>
        /// Freeze rows above specified row
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        void FreezeRow(int rowIdx);

        /// <summary>
        /// Auto fit column widths in specified range
        /// </summary>
        /// <param name="range">null to fit all column</param>
        void AutoFitColumns(Range range = null);

        /// <summary>
        /// Auto fit width of specified column
        /// </summary>
        /// <param name="colIdx">zero-based column index</param>
        void AutoFitColumn(int colIdx);

        /// <summary>
        /// Set width of specified column
        /// </summary>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="width"></param>
        void SetColumnWidth(int colIdx, int width);

        /// <summary>
        /// Hide columns in specified range
        /// </summary>
        /// <param name="range"></param>
        void HideColumns(Range range);

        /// <summary>
        /// Hide rows in specified range
        /// </summary>
        /// <param name="range"></param>
        void HideRows(Range range);

        /// <summary>
        /// Define hyperlink for specified cell. When address is an Excelsheet, use a subaddress as sheetname!cellref, where sheetname can be surrounded by '-symbols and cellref is an absolute celladdres (e.g. D10)
        /// </summary>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero-based column index</param>
        /// <param name="address"></param>
        /// <param name="subAddress"></param>
        /// <param name="screenTip"></param>
        /// <param name="textToDisplay"></param>
        void SetHyperlink(int rowIdx, int colIdx, string address, string subAddress = null, string screenTip = null, string textToDisplay = null);

        /// <summary>
        /// Create chart object of given type inside this sheet at specified range
        /// </summary>
        /// <param name="range"></param>
        /// <param name="chartType"></param>
        /// <returns></returns>
        IChart CreateChart(Range range, ChartType chartType);

        /// <summary>
        /// Copy characteristics and contents of specified source cell to specified target cell
        /// </summary>
        /// <param name="sourceRowIdx">zero-based row index of source cell</param>
        /// <param name="sourceColIdx">zero-based column index of source cell</param>
        /// <param name="targetRowIdx">zero-based row index of target cell</param>
        /// <param name="targetColIdx">zero-based column index of target cell</param>
        void CopyCell(int sourceRowIdx, int sourceColIdx, int targetRowIdx, int targetColIdx);

        /// <summary>
        /// Copy characteristics and contents of specified source cell to specified target cell
        /// </summary>
        /// <param name="sourceCell"></param>
        /// <param name="targetCell"></param>
        void CopyCell(Cell sourceCell, Cell targetCell);

        /// <summary>
        /// Copy characteristics and contents of cells in specified source range to location defined by specified upper left target cell
        /// </summary>
        /// <param name="range"></param>
        /// <param name="targetCell"></param>
        void CopyRange(Range range, Cell targetCell);

        /// <summary>
        /// Calculate formula's in sheet dynamically
        /// </summary>
        void Calculate();

        /// <summary>
        /// Zoom in to the specified percentage of the current sheet
        /// </summary>
        /// <param name="percentage"></param>
        void Zoom(int percentage);

        /// <summary>
        /// Apply specified header, footer and bitmap to this sheet
        /// </summary>
        /// <param name="leftHeader"></param>
        /// <param name="leftFooter"></param>
        /// <param name="bitmap"></param>
        void ApplyPageSetup(string leftHeader, string leftFooter, Bitmap bitmap = null);

        /// <summary>
        /// Set page orientation of this sheet to landscape
        /// </summary>
        void SetPageOrientationLandscape();

        /// <summary>
        /// Set page size of this sheet to specified size
        /// </summary>
        void SetPapersize(PaperSize papersize);

        /// <summary>
        /// Set view mode of worksheet to page layout
        /// </summary>
        void SetPageLayoutView();

        /// <summary>
        /// Retrieves the underlying object that refers to this implemention of a worksheet
        /// </summary>
        /// <returns></returns>
        object GetBaseObject();
    }
}
