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
using Sweco.SIF.Spreadsheets.Excel.CSV;
using Sweco.SIF.Spreadsheets.Excel.EPPLUS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Spreadsheets.Excel
{
    /// <summary>
    /// Class for creating specific ExcelManager instances
    /// </summary>
    public class ExcelManagerFactory
    {
        public enum ExcelManagerType
        {
            Default,
            EPPlus,
            CSV
        }

        public static ExcelManager CreateExcelManager(ExcelManagerType excelManagerType = ExcelManagerType.Default)
        {
            if ((excelManagerType == ExcelManagerType.EPPlus) || (excelManagerType == ExcelManagerType.Default))
            {
                return new EPPlusExcelManager();
            }
            else if ((excelManagerType == ExcelManagerType.CSV))
            {
                return new CSVExcelManager();
            }
            else
            {
                throw new Exception("Unknown ExcelManagerType: " + excelManagerType.ToString());
            }
        }

        public static ExcelManager CreateExcelManager(string filename)
        {
            if (Path.GetExtension(filename).ToLower().Equals(".xlsx"))
            {
                return new EPPlusExcelManager();
            }
            else if (Path.GetExtension(filename).ToLower().Equals(".csv") || Path.GetExtension(filename).ToLower().Equals(".txt"))
            {
                return new CSVExcelManager();
            }
            else
            {
                throw new Exception("Unknown ExcelManagerType for extension of filename: " + Path.GetFileName(filename));
            }
        }

        public static bool IsExcelInstalled()
        {
            Type officeType = Type.GetTypeFromProgID("Excel.Application");

            if (officeType == null)
            {
                // Excel is not installed.
                return false;
            }
            else
            {
                // Excel is installed.
                return true;
            }
        }

    }
}
