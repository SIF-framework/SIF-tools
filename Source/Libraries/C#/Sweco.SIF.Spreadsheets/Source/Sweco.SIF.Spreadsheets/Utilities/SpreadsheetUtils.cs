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
using Sweco.SIF.Common.ExceptionHandling;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Spreadsheets
{
    /// <summary>
    /// SIF utilities for spreadsheet processing
    /// </summary>
    public class SpreadsheetUtils
    {
        /// <summary>
        /// Maximum lengh of sheet names
        /// </summary>
        public const int MaxSheetnameLength = 31;

        /// <summary>
        /// Convert column number to Excel column string (first column (1) is A) 
        /// source: http://stackoverflow.com/questions/837155/fastest-function-to-generate-excel-column-letters-in-c-sharp
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public static string ExcelColumnFromNumber(int column)
        {
            string columnString = "";
            decimal columnNumber = column;
            while (columnNumber > 0)
            {
                decimal currentLetterNumber = (columnNumber - 1) % 26;
                char currentLetter = (char)(currentLetterNumber + 65);
                columnString = currentLetter + columnString;
                columnNumber = (columnNumber - (currentLetterNumber + 1)) / 26;
            }
            return columnString;
        }


        /// <summary>
        /// Convert Excel column string to column number (first column (A) is 1) 
        /// source: http://stackoverflow.com/questions/837155/fastest-function-to-generate-excel-column-letters-in-c-sharp
        /// </summary>
        /// <param name="column"></param>
        /// <returns></returns>
        public static int NumberFromExcelColumn(string column)
        {
            int retVal = 0;
            string col = column.ToUpper();
            for (int iChar = col.Length - 1; iChar >= 0; iChar--)
            {
                char colPiece = col[iChar];
                int colNum = colPiece - 64;
                retVal = retVal + colNum * (int)Math.Pow(26, col.Length - (iChar + 1));
            }
            return retVal;
        }

        /// <summary>
        /// Retrieve sheet index from a string with either a sheet name or a sheet number in specified workbook
        /// Note: -1 is returned for an invalid sheet ID
        /// </summary>
        /// <param name="workbook"></param>
        /// <param name="sheetID"></param>
        /// <returns>zero-based sheet index or -1 for an invalid sheet ID</returns>
        public static int FindSheetIndex(IWorkbook workbook, string sheetID)
        {
            int sheetIdx = workbook.FindSheetIndex(sheetID);
            if (sheetIdx < 0)
            {
                if (!int.TryParse(sheetID, out int sheetNr))
                {
                    return -1;
                }

                sheetIdx = sheetNr - 1;
                if ((sheetIdx < 0) || (sheetIdx >= workbook.Sheets.Count))
                {
                    return -1;
                }
            }

            return sheetIdx;
        }

        /// <summary>
        /// Correct give string to a valid string for a sheet name
        /// </summary>
        /// <param name="sheetname"></param>
        /// <returns></returns>
        public static string CorrectSheetname(string sheetname)
        {
            string correctedSheetname = sheetname.Replace("/", "_");
            correctedSheetname = sheetname.Replace("\\", "_");
            correctedSheetname = sheetname.Replace("'", string.Empty);
            if (correctedSheetname.Length > MaxSheetnameLength)
            {
                correctedSheetname = correctedSheetname.Replace(" ", string.Empty);
                correctedSheetname = correctedSheetname.Replace("_", string.Empty);
                correctedSheetname = correctedSheetname.Replace(":", string.Empty);
                correctedSheetname = correctedSheetname.Replace(".", string.Empty);
                correctedSheetname = correctedSheetname.Replace("-", string.Empty);
                correctedSheetname = correctedSheetname.Replace(" ", string.Empty);
                if (correctedSheetname.Length > MaxSheetnameLength)
                {
                    correctedSheetname = correctedSheetname.Substring(0, 15) + correctedSheetname.Substring(correctedSheetname.Length - 16, 16);
                }
            }
            return correctedSheetname;
        }

        /// <summary>
        /// Convert object with a cell value to a default string based on specified CultureInfo
        /// </summary>
        /// <param name="objectValue"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public static string ConvertObjectValue(object objectValue, CultureInfo cultureInfo)
        {
            if (objectValue is double)
            {
                return ((double)objectValue).ToString(cultureInfo);
            }
            if (objectValue is int)
            {
                return ((int)objectValue).ToString(cultureInfo);
            }
            if (objectValue is int)
            {
                return ((int)objectValue).ToString(cultureInfo);
            }
            if (objectValue is DateTime)
            {
                return ((DateTime)objectValue).ToString(cultureInfo);
            }
            if (objectValue is float)
            {
                return ((float)objectValue).ToString(cultureInfo);
            }
            if (objectValue is bool)
            {
                return ((bool)objectValue).ToString(cultureInfo);
            }

            return objectValue.ToString();
        }

        /// <summary>
        /// Convert object with a cell value to a string based on specified CultureInfo and format
        /// </summary>
        /// <param name="objectValue"></param>
        /// <param name="cultureInfo"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string ConvertObjectValue(object objectValue, CultureInfo cultureInfo, string format)
        {
            if (objectValue is double)
            {
                return ((double)objectValue).ToString(format, cultureInfo);
            }
            if (objectValue is int)
            {
                return ((int)objectValue).ToString(format, cultureInfo);
            }
            if (objectValue is int)
            {
                return ((int)objectValue).ToString(format, cultureInfo);
            }
            if (objectValue is DateTime)
            {
                return ((DateTime)objectValue).ToString(format, cultureInfo);
            }
            if (objectValue is float)
            {
                return ((float)objectValue).ToString(format, cultureInfo);
            }
            if (objectValue is bool)
            {
                return ((bool)objectValue).ToString(cultureInfo);
            }

            return objectValue.ToString();
        }

        /// <summary>
        /// Parse specified string to an object that corresponds with the string value, based on specified CultureInfo
        /// </summary>
        /// <param name="stringValue"></param>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public static object ConvertStringValue(string stringValue, CultureInfo cultureInfo)
        {
            if (bool.TryParse(stringValue, out bool boolValue))
            {
                return boolValue;
            }
            else if (int.TryParse(stringValue, out int intValue))
            {
                return intValue;
            }
            else if (double.TryParse(stringValue, NumberStyles.Any, cultureInfo, out double dblValue))
            {
                return dblValue;
            }
            else if (DateTime.TryParse(stringValue, out DateTime dateTimeValue))
            {
                return dateTimeValue;
            }
            else
            {
                return stringValue;
            }
        }
    }
}
