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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Spreadsheets
{
    /// <summary>
    /// General base class for managing spreadsheet files. Note: All indices are zerobased (first item has index 0)
    /// </summary>
    public abstract class SpreadsheetManager
    {
        /// <summary>
        /// Gets name of application that is used to manage the spreadsheet
        /// </summary>
        public abstract string ApplicationName { get; }

        /// <summary>
        /// Opens specified workbook file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="isReadOnly">specify true to avoid access issue when already open in another application, default is true</param>
        /// <param name="delimiter">optional list delimiter character in case of csv workbooks, e.g. ";"</param>
        /// <returns></returns>
        public abstract IWorkbook OpenWorkbook(string filename, bool isReadOnly, string delimiter = null);

        /// <summary>
        /// Opens specified workbook file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="delimiter">optional list delimiter character in case of csv workbooks, e.g. ";"</param>
        /// <returns></returns>
        public abstract IWorkbook OpenWorkbook(string filename, string delimiter = null);

        /// <summary>
        /// Sets visibility of spreadsheet. It depends on the specific implementation wether the spreadsheet is actually shown
        /// </summary>
        /// <param name="isVisible"></param>
        /// <param name="isMaximized">specify true to maximize Excel when set to visible state</param>
        public abstract void SetVisibility(bool isVisible, bool isMaximized = false);

        /// <summary>
        /// Sets screenupdating of spreadsheet. It depends on the specific implementation wether the spreadsheet is actually shown and updated
        /// </summary>
        /// <param name="isScreenUpdating"></param>
        public abstract void SetScreenUpdating(bool isScreenUpdating);

        /// <summary>
        /// Sets the window size of the application that is used to manage the spreadsheets
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public abstract void SetApplicationWindowSize(int width, int height);

        /// <summary>
        /// Handles cleanup after opening some spreadsheet. Ensures the corresponding application is closed correctly
        /// </summary>
        public abstract void Cleanup();

        /// <summary>
        /// Creates a new workbook  with the application that corresponds withthe SpreadsheetManager instance
        /// </summary>
        /// <param name="isEmptySheetAdded">true, if an empty sheet is added to the new workbook</param>
        /// <param name="sheetname">the name of the added sheet, if isEmptySheetAdded is true</param>
        /// <returns></returns>
        public abstract IWorkbook CreateWorkbook(bool isEmptySheetAdded = true, string sheetname = null);

        /// <summary>
        /// Opens and shows the specified workbook with the application that corresponds withthe SpreadsheetManager instance
        /// </summary>
        /// <param name="workbookFilename"></param>
        public abstract void ShowWorkbook(string workbookFilename);

        /// <summary>
        /// Checks if the specified workbook file is opened  with the application that corresponds withthe SpreadsheetManager instance
        /// </summary>
        /// <param name="workbookFilename"></param>
        /// <returns></returns>
        public abstract bool HasOpenWorkbook(string workbookFilename);
    }

    /// <summary>
    /// Available directions relative to cells
    /// </summary>
    public enum CellDirection
    {
        /// <summary>
        /// Upwards from current cell
        /// </summary>
        Up,

        /// <summary>
        /// Downwards from current cell
        /// </summary>
        Down,

        /// <summary>
        /// To the right of current cell
        /// </summary>
        ToRight,

        /// <summary>
        /// To the left of current cell
        /// </summary>
        ToLeft
    }

    /// <summary>
    /// Available weights of border lines
    /// </summary>
    public enum BorderWeight
    {
        /// <summary>
        /// Very thin, dotted line
        /// </summary>
        Hairline,

        /// <summary>
        /// Thin line
        /// </summary>
        Thin,

        /// <summary>
        /// Medium line
        /// </summary>
        Medium,

        /// <summary>
        /// Thick line
        /// </summary>
        Thick
    }

    /// <summary>
    /// Available types of chart
    /// </summary>
    public enum ChartType
    {
        /// <summary>
        /// A line chart
        /// </summary>
        LineChart,

        /// <summary>
        /// A scatter chart
        /// </summary>
        ScatterChart,

        /// <summary>
        /// A clustered bar chart
        /// </summary>
        BarClustered,

        /// <summary>
        /// A clustered column chart
        /// </summary>
        ColumnClustered
    }

    /// <summary>
    /// Available marker styles
    /// </summary>
    public enum MarkerStyle
    {
        /// <summary>
        /// Square marker
        /// </summary>
        Square,

        /// <summary>
        /// Circular marker
        /// </summary>
        Circle,

        /// <summary>
        /// Dash marker
        /// </summary>
        Dash,

        /// <summary>
        /// Diamond marker
        /// </summary>
        Diamond,

        /// <summary>
        /// No marker
        /// </summary>
        None,

        /// <summary>
        /// Triangular marker
        /// </summary>
        Triangle,

        /// <summary>
        /// Dot marker
        /// </summary>
        Dot,

        /// <summary>
        /// Plus marker
        /// </summary>
        Plus,

        /// <summary>
        /// Star marker
        /// </summary>
        Star
    }
}
