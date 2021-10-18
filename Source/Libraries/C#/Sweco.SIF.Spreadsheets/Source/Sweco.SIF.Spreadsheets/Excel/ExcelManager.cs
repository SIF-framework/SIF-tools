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

namespace Sweco.SIF.Spreadsheets.Excel
{
    /// <summary>
    /// Base class for managing Excel spreadsheet files
    /// </summary>
    public abstract class ExcelManager : SpreadsheetManager
    {
        public static CultureInfo EnglishCultureInfo = new CultureInfo("en-GB", false);
        public static CultureInfo DutchCultureInfo = new CultureInfo("nl-NL", false);

        protected bool isExcelVisible;

        public override string ApplicationName
        {
            get { return "Excel"; }
        }

        protected ExcelManager()
            : base()
        {
            this.isExcelVisible = false;
        }

    }
}
