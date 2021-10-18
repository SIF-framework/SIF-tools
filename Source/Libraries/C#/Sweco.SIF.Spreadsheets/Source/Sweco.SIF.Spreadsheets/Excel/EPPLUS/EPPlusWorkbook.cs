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
using OfficeOpenXml;
using Sweco.SIF.Common.ExceptionHandling;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Spreadsheets.Excel.EPPLUS
{
    /// <summary>
    /// Class for processing Excel workbooks with EPPlus-library
    /// </summary>
    public class EPPlusWorkbook : IWorkbook
    {
        protected static CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

        internal ExcelPackage excelPackage; // used for saving the workbook
        internal ExcelWorkbook epplusWorkbook;

        public string Filename { get; set; }

        public EPPlusWorkbook(ExcelPackage excelPackage, string filename)
        {
            this.Filename = filename;
            this.excelPackage = excelPackage;
            this.epplusWorkbook = excelPackage.Workbook;

            // Cell addresses, number formats and formulas are culture-insensitive. This is the way OOXML is stored and is then translated too your culture when the workbook is opened in Excel.
            // Addresses are separated by a comma (,).
            // Example worksheet.Cells["A1:C1,C3"].Style.Font.Bold = true.
            // Numberformats use dot for decimal (.) and comma (,) for thousand separator.
            // Example worksheet.Cells["B2:B3"].Style.NumberFormat.Format = "#,##0.00";.
            // Formulas use comma (,) to separate parameters.
            // Example worksheet.Cells["C11"].Formula="SUBTOTAL(9,\"C1:C10\")";.
            this.cultureInfo = englishCultureInfo;
            try
            {
                bool hasHyperlinkStyle = false;
                for (int styleIdx = 0; styleIdx < epplusWorkbook.Styles.NamedStyles.Count; styleIdx++)
                {
                    if (epplusWorkbook.Styles.NamedStyles[styleIdx].Name.Equals("Hyperlink"))
                    {
                        hasHyperlinkStyle = true;
                        styleIdx = epplusWorkbook.Styles.NamedStyles.Count;
                    }
                }
                if (!hasHyperlinkStyle)
                {
                    var namedStyle = epplusWorkbook.Styles.CreateNamedStyle("HyperLink");   //This one is language dependent
                    namedStyle.Style.Font.UnderLine = true;
                    namedStyle.Style.Font.Color.SetColor(Color.Blue);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Could not check and/or create Hyperlink named style: ", ex.GetBaseException().Message);
            }
        }

        protected CultureInfo cultureInfo;
        /// <summary>
        /// Used for formatting and parsing Cell string values 
        /// </summary>
        public CultureInfo CultureInfo
        {
            get { return cultureInfo; }
            set { cultureInfo = value; }
        }

        public static IWorkbook OpenWorkbook(string workbookFilename, bool isReadOnly)
        {
            if (File.Exists(workbookFilename))
            {
                ExcelPackage excelPackage = null;
                if (isReadOnly)
                {
                    FileStream fileStream = new FileStream(workbookFilename, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    excelPackage = new ExcelPackage(fileStream);
                }
                else
                {
                    FileInfo fileInfo = new FileInfo(workbookFilename);
                    excelPackage = new ExcelPackage(fileInfo);
                }

                return new EPPlusWorkbook(excelPackage, workbookFilename);
            }
            else
            {
                throw new LibraryException("Workbookfile not found: " + workbookFilename);
            }
        }

        public List<IWorksheet> Sheets
        {
            get
            {
                List<IWorksheet> sheets = new List<IWorksheet>();
                for (int sheetIdx = 0; sheetIdx < epplusWorkbook.Worksheets.Count; sheetIdx++)
                {
                    sheets.Add(new EPPlusWorksheet(this, epplusWorkbook.Worksheets[sheetIdx + 1]));
                }
                return sheets;
            }
        }

        public void Save(string filename)
        {
            excelPackage.SaveAs(new FileInfo(filename));
            this.Filename = filename;
        }

        public void Close()
        {
            excelPackage.Dispose();
        }

        public IWorksheet AddSheet(string sheetname)
        {
            ExcelWorksheet epplusSheet = epplusWorkbook.Worksheets.Add(sheetname ?? "Sheet" + (epplusWorkbook.Worksheets.Count + 1));
            return new EPPlusWorksheet(this, epplusSheet);
        }

        public IWorksheet GetSheet(int sheetIdx)
        {
            return new EPPlusWorksheet(this, epplusWorkbook.Worksheets[sheetIdx + 1]);
        }

        public IWorksheet GetSheet(string sheetname)
        {
            int idx = FindSheetIndex(sheetname);
            if (idx >= 0)
            {
                return GetSheet(idx);
            }
            return null;
        }

        public int FindSheetIndex(string sheetname)
        {
            for (int idx = 1; idx <= Sheets.Count; idx++)
            {
                if (epplusWorkbook.Worksheets[idx].Name.ToLower().Equals(sheetname.ToLower()))
                {
                    return idx - 1;
                }
            }
            return -1;
        }

        public void DeleteSheet(int sheetIdx)
        {
            if ((sheetIdx < 0) || (sheetIdx >= epplusWorkbook.Worksheets.Count))
            {
                throw new LibraryException("Invalid sheetIdx for DeleteSheet: " + sheetIdx);
            }
            epplusWorkbook.Worksheets.Delete(sheetIdx + 1);
        }

        public void MoveSheetToEnd(IWorksheet worksheet)
        {
            int idx = FindSheetIndex(worksheet.GetSheetname());
            epplusWorkbook.Worksheets.MoveToEnd(idx + 1);
        }

        public void MoveSheetToStart(IWorksheet worksheet)
        {
            int idx = FindSheetIndex(worksheet.GetSheetname());
            epplusWorkbook.Worksheets.MoveToStart(idx + 1);
        }

        public void MoveSheetBefore(IWorksheet sourceWorksheet, IWorksheet targetWorksheet)
        {
            int sourceIdx = FindSheetIndex(sourceWorksheet.GetSheetname());
            int targetIdx = FindSheetIndex(targetWorksheet.GetSheetname());
            epplusWorkbook.Worksheets.MoveBefore(sourceIdx + 1, targetIdx + 1);
        }

        public void MoveSheetAfter(IWorksheet sourceWorksheet, IWorksheet targetWorksheet)
        {
            int sourceIdx = FindSheetIndex(sourceWorksheet.GetSheetname());
            int targetIdx = FindSheetIndex(targetWorksheet.GetSheetname());
            epplusWorkbook.Worksheets.MoveAfter(sourceIdx + 1, targetIdx + 1);
        }

        public void Calculate()
        {
            epplusWorkbook.Calculate();
        }

        public object GetBaseObject()
        {
            return epplusWorkbook;
        }

        public string GetUserName()
        {
            return System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
        }

    }
}
