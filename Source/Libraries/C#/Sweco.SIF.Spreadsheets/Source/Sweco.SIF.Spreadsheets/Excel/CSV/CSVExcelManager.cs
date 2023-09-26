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

namespace Sweco.SIF.Spreadsheets.Excel.CSV
{
    /// <summary>
    /// Class for managing CSV-files as a spreadsheet
    /// </summary>
    public class CSVExcelManager : ExcelManager
    {
        public override IWorkbook OpenWorkbook(string filename, string delimiter = null)
        {
            return OpenWorkbook(filename, true, delimiter);
        }

        public override IWorkbook OpenWorkbook(string filename, bool isReadOnly, string delimiter = null)
        {
            return CSVWorkbook.OpenWorkbook(filename, isReadOnly);
        }

        public override void SetVisibility(bool isVisible, bool isMaximized = false)
        {
            // not used
        }

        public override void SetScreenUpdating(bool isScreenUpdating)
        {
            // nothing to do
        }

        public override void SetApplicationWindowSize(int width, int height)
        {
            // not used
        }

        public override void Cleanup()
        {
            // not used
        }

        public override IWorkbook CreateWorkbook(bool isEmptySheetAdded = true, string sheetname = null)
        {
            return new CSVWorkbook(sheetname);
        }

        public override void ShowWorkbook(string workbookFilename)
        {
            throw new NotImplementedException();
        }

        public override bool HasOpenWorkbook(string workbookFilename)
        {
            throw new NotImplementedException();
        }
    }
}
