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
    /// Wrapper for referencing a range of spreadsheet cells
    /// </summary>
    public class Range
    {
        /// <summary>
        /// 
        /// </summary>
        public IWorksheet Sheet;

        /// <summary>
        /// Zero-based row index of upper left cell in range
        /// </summary>
        public int RowIdx1;


        /// <summary>
        /// Zero-based column index of upper left cell in range
        /// </summary>
        public int ColIdx1;

        /// <summary>
        /// Zero-based row index of lower right cell in range
        /// </summary>
        public int RowIdx2;

        /// <summary>
        /// Zero-based column index of lower right cell in range
        /// </summary>
        public int ColIdx2;

        /// <summary>
        /// Upper left cell in range
        /// </summary>
        public Cell Cell1
        {
            get { return new Cell(Sheet, RowIdx1, ColIdx1); }
            set { RowIdx1 = value.RowIdx; ColIdx1 = value.ColIdx; }
        }

        /// <summary>
        /// Lower right cell in range
        /// </summary>
        public Cell Cell2
        {
            get { return new Cell(Sheet, RowIdx2, ColIdx2); }
            set { RowIdx2 = value.RowIdx; ColIdx2 = value.ColIdx; }
        }

        /// <summary>
        /// Create a new range instance
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="rowIdx1"></param>
        /// <param name="colIdx1"></param>
        /// <param name="rowIdx2"></param>
        /// <param name="colIdx2"></param>
        public Range(IWorksheet sheet, int rowIdx1, int colIdx1, int rowIdx2, int colIdx2)
        {
            this.Sheet = sheet;
            this.RowIdx1 = rowIdx1;
            this.ColIdx1 = colIdx1;
            this.RowIdx2 = rowIdx2;
            this.ColIdx2 = colIdx2;
        }

        /// <summary>
        /// Create a new range instance
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="cell1"></param>
        /// <param name="cell2"></param>
        public Range(IWorksheet sheet, Cell cell1, Cell cell2)
        {
            this.Sheet = sheet;
            this.RowIdx1 = cell1.RowIdx;
            this.ColIdx1 = cell1.ColIdx;
            this.RowIdx2 = cell2.RowIdx;
            this.ColIdx2 = cell2.ColIdx;
        }

        /// <summary>
        /// Retrieve 2D-array with all values in this range
        /// </summary>
        /// <returns></returns>
        public object[,] GetValues()
        {
            return Sheet.GetValues(this);
        }

        /// <summary>
        /// Retrieve an address string for this range 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return SpreadsheetUtils.ExcelColumnFromNumber(ColIdx1 + 1) + (RowIdx1 + 1) + ":" + SpreadsheetUtils.ExcelColumnFromNumber(ColIdx2 + 1) + (RowIdx2 + 1);
        }
    }
}
