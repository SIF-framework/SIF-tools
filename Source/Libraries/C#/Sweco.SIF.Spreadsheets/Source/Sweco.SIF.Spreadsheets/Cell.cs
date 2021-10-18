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
    /// Wrapper for referencing a spreadsheet cell
    /// </summary>
    public class Cell
    {
        /// <summary>
        /// Sheet that cell belongs to
        /// </summary>
        public IWorksheet Sheet;

        /// <summary>
        /// Zero-based row index of this cell
        /// </summary>
        public int RowIdx;

        /// <summary>
        /// Zero-based column index of this cell
        /// </summary>
        public int ColIdx;

        /// <summary>
        /// Create cell instance for specified sheet, row and index
        /// </summary>
        /// <param name="sheet"></param>
        /// <param name="rowIdx">zero-based row index</param>
        /// <param name="colIdx">zero based column index</param>
        public Cell(IWorksheet sheet, int rowIdx, int colIdx)
        {
            this.Sheet = sheet;
            this.RowIdx = rowIdx;
            this.ColIdx = colIdx;
        }

        /// <summary>
        /// String with cell value
        /// </summary>
        public string Value
        {
            get { return Sheet.GetCellValue(RowIdx, ColIdx); }
        }

        /// <summary>
        /// Retrieve last cell (before empty cell) moving from this cell in given direction
        /// </summary>
        /// <param name="direction"></param>
        /// <returns></returns>
        public virtual Cell End(CellDirection direction)
        {
            return Sheet.End(this, direction);
        }

        /// <summary>
        /// Check if this cell is empty
        /// </summary>
        /// <returns></returns>
        public virtual bool IsEmpty()
        {
            return Sheet.IsEmpty(this);
        }
    }
}
