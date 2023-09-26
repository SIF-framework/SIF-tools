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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.IDF
{
    /// <summary>
    /// Class for store information about a specific cell in an IDF-file. 
    /// </summary>
    public class IDFCell : IEquatable<IDFCell>, IComparer<IDFCell>
    {
        /// <summary>
        /// Zero-based row index of this cell in IDF-file
        /// </summary>
        public int RowIdx { get; set; }

        /// <summary>
        /// Zero-based column index of this cell in IDF-file
        /// </summary>
        public int ColIdx { get; set; }

        /// <summary>
        /// value of this cell (which may be different from cell in corresponding IDF-file)
        /// </summary>
        public float Value { get; set; }

        /// <summary>
        /// Create new IDFCell object with specified row and column index
        /// </summary>
        /// <param name="rowIdx"></param>
        /// <param name="colIdx"></param>
        public IDFCell(int rowIdx, int colIdx)
        {
            this.RowIdx = rowIdx;
            this.ColIdx = colIdx;
            this.Value = float.NaN;
        }

        /// <summary>
        /// Create new IDFCell object with specified row and column index
        /// </summary>
        /// <param name="rowIdx"></param>
        /// <param name="colIdx"></param>
        /// <param name="value"></param>
        public IDFCell(int rowIdx, int colIdx, float value) : this(rowIdx, colIdx)
        {
            this.RowIdx = rowIdx;
            this.ColIdx = colIdx;
            this.Value = value;
        }

        /// <summary>
        /// Return -1,0 or 1 if RowIdx, ColIdx or Value of cell1 are smaller, equal or larger than indices/value of cell2
        /// </summary>
        /// <param name="cell1"></param>
        /// <param name="cell2"></param>
        /// <returns></returns>
        public int Compare(IDFCell cell1, IDFCell cell2)
        {
            if (cell1 == null)
            {
                return -1;
            }
            else if (cell2 == null)
            {
                return 1;
            }
            else
            {
                // Compare first to RowIdx, and if equal to ColIdx
                if (cell1.RowIdx != cell2.RowIdx)
                {
                    return cell1.RowIdx.CompareTo(cell2.RowIdx);
                }
                else if (cell1.ColIdx != cell2.ColIdx)
                {
                    return cell1.ColIdx.CompareTo(cell2.ColIdx);
                }
                else
                {
                    return cell1.Value.CompareTo(cell2.Value);
                }
            }
        }

        /// <summary>
        /// Checks if this instance equals the specified other IDFCell object based on Row and Column indices and Value
        /// /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IDFCell other)
        {
            if (other == null)
            {
                return false;
            }
            else
            {
                return (other.RowIdx == this.RowIdx) && (other.ColIdx == this.ColIdx) && (other.Value.Equals(this.Value));
            }
        }

        /// <summary>
        /// Returns the hashcode for this instance, based on row and column indices
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return RowIdx.GetHashCode() ^ ColIdx.GetHashCode();
        }

        /// <summary>
        /// Return a string for current IDF-cell with row- and columindex between parenthesis
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "(" + RowIdx + "," + ColIdx + (Value.Equals(float.NaN) ? string.Empty : (":"  + Value.ToString())) + ")";
        }
    }
}
