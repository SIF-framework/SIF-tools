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
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Spreadsheets
{
    /// <summary>
    /// Interface for workbook
    /// </summary>
    public interface IWorkbook
    {
        /// <summary>
        /// List with current worksheets in this workbook
        /// </summary>
        List<IWorksheet> Sheets { get; }

        /// <summary>
        /// Save this workbook to specified file
        /// </summary>
        /// <param name="filename"></param>
        void Save(string filename);

        /// <summary>
        /// Close this workbook
        /// </summary>
        void Close();

        /// <summary>
        /// Get or set current CultureInfo with languange settings for this workbook
        /// </summary>
        CultureInfo CultureInfo { get; set; }

        /// <summary>
        /// Full path of this workbook
        /// </summary>
        string Filename { get; set; }

        /// <summary>
        /// Adds a new sheet behind existing sheets
        /// </summary>
        /// <param name="sheetname"></param>
        /// <returns></returns>
        IWorksheet AddSheet(string sheetname);

        /// <summary>
        /// Retrieve sheet with (zero based) index (first sheet has index 0)
        /// </summary>
        /// <param name="sheetIdx">(zerobased) index (first sheet has index 0)</param>
        /// <returns></returns>
        IWorksheet GetSheet(int sheetIdx);

        /// <summary>
        /// Retrieve sheet with the specified name or null if not existing
        /// </summary>
        /// <param name="sheetname"></param>
        /// <returns></returns>
        IWorksheet GetSheet(string sheetname);

        /// <summary>
        /// Retrieve (zero based) sheetindex for the specified name or -1 if not existing
        /// </summary>
        /// <param name="sheetname"></param>
        /// <returns></returns>
        int FindSheetIndex(string sheetname);

        /// <summary>
        /// Deletes the sheet with the specified (zero based) index
        /// </summary>
        /// <param name="sheetIdx"></param>
        void DeleteSheet(int sheetIdx);

        /// <summary>
        /// Move specified sheet behind all other sheets
        /// </summary>
        /// <param name="worksheet"></param>
        void MoveSheetToEnd(IWorksheet worksheet);

        /// <summary>
        /// Move specified sheet before all other sheets
        /// </summary>
        /// <param name="worksheet"></param>
        void MoveSheetToStart(IWorksheet worksheet);

        /// <summary>
        /// Move given sheet before specified target sheet
        /// </summary>
        /// <param name="sourceWorksheet"></param>
        /// <param name="targetWorksheet"></param>
        void MoveSheetBefore(IWorksheet sourceWorksheet, IWorksheet targetWorksheet);

        /// <summary>
        /// Move given sheet after specified target sheet
        /// </summary>
        /// <param name="sourceWorksheet"></param>
        /// <param name="targetWorksheet"></param>
        void MoveSheetAfter(IWorksheet sourceWorksheet, IWorksheet targetWorksheet);

        /// <summary>
        /// Calculate formula's in workbook dynamically
        /// </summary>
        void Calculate();

        /// <summary>
        /// Retrieves the underlying object that refers to this implemention of a workbook
        /// </summary>
        /// <returns></returns>
        object GetBaseObject();
    }
}
