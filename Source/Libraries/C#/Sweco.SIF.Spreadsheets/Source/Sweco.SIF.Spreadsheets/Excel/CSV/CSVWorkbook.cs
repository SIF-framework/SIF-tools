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
using Sweco.SIF.Common;
using Sweco.SIF.Common.ExceptionHandling;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Spreadsheets.Excel.CSV
{
    /// <summary>
    /// Class for processing CSV-files as a workbook. A CSV-file has a single worksheet.
    /// </summary>
    class CSVWorkbook : IWorkbook
    {
        protected static CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

        public string Filename { get; set; }
        public string ListSeperator { get; set; }

        protected CSVWorksheet csvWorksheet;

        public CSVWorkbook(string filename)
        {
            this.Filename = filename;
            this.cultureInfo = englishCultureInfo;
            this.csvWorksheet = null;
            this.ListSeperator = ",";
        }

        public CSVWorkbook(string filename, string ListSeperator) : this(filename)
        {
            this.ListSeperator = ListSeperator;
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

        public static IWorkbook OpenWorkbook(string csvFilename, bool isReadOnly)
        {
            if (File.Exists(csvFilename))
            {
                return new CSVWorkbook(csvFilename);
            }
            else
            {
                throw new LibraryException("CSV-file not found: " + csvFilename);
            }
        }

        public List<IWorksheet> Sheets
        {
            get
            {
                if (csvWorksheet == null)
                {
                    csvWorksheet = CSVWorksheet.ReadCSVSheet(this, Filename, ListSeperator);
                }

                List<IWorksheet> sheets = new List<IWorksheet>();
                sheets.Add(csvWorksheet);
                return sheets;
            }
        }

        private CSVWorksheet ReadCSVSheet(string filename)
        {
            return CSVWorksheet.ReadCSVSheet(this, filename, ListSeperator);
        }

        public void Save(string filename)
        {
            csvWorksheet.Save(filename, ListSeperator);
            this.Filename = filename;
        }

        public void Close()
        {
            csvWorksheet = null;
        }

        public IWorksheet AddSheet(string sheetname)
        {
            throw new NotImplementedException();
        }

        public IWorksheet GetSheet(int sheetIdx)
        {
            if (sheetIdx > 0)
            {
                throw new LibraryException("SheetIdx cannot be larger than 0 for CSV-files: " + sheetIdx);
            }
            
            if (csvWorksheet == null)
            {
                csvWorksheet = CSVWorksheet.ReadCSVSheet(this, Filename, ListSeperator);
            }

            return csvWorksheet;
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
            if (sheetname.ToLower().Equals(Path.GetFileNameWithoutExtension(sheetname).ToLower()))
            {
                return 0;
            }
            else
            {
                return -1;
            }
        }

        public void DeleteSheet(int sheetIdx)
        {
            throw new NotImplementedException();
        }

        public void MoveSheetToEnd(IWorksheet worksheet)
        {
            throw new NotImplementedException();
        }

        public void MoveSheetToStart(IWorksheet worksheet)
        {
            throw new NotImplementedException();
        }

        public void MoveSheetBefore(IWorksheet sourceWorksheet, IWorksheet targetWorksheet)
        {
            throw new NotImplementedException();
        }

        public void MoveSheetAfter(IWorksheet sourceWorksheet, IWorksheet targetWorksheet)
        {
            throw new NotImplementedException();
        }

        public void Calculate()
        {
            throw new NotImplementedException();
        }

        public object GetBaseObject()
        {
            return csvWorksheet;
        }

        public string GetUserName()
        {
            return System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
        }

    }
}
