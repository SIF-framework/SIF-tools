// IMFcreate is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IMFcreate.
// 
// IMFcreate is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IMFcreate is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IMFcreate. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.Spreadsheets;
using Sweco.SIF.Spreadsheets.Excel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IMFcreate
{
    /// <summary>
    /// Class for storing and reading colors of REGIS-units from an Excel XLSX-file with a REGIS-substring (e.g. part before -t-), and RGB colors in columns 2-4
    /// </summary>
    public class REGISColorDef
    {
        public string Filename;
        public Dictionary<string, Color> ColorDictionary;

        public REGISColorDef()
        {
            ColorDictionary = new Dictionary<string, Color>();
        }

        public static REGISColorDef ReadFile(string regisColorsFilename, Log log)
        {
            REGISColorDef regisColorDef = new REGISColorDef();
            regisColorDef.Filename = regisColorsFilename;

            if (!Path.GetExtension(regisColorsFilename).ToLower().Equals(".xlsx"))
            {
                throw new ToolException("XSLX-file expected for definition of REGIS colors: " + regisColorsFilename);
            }

            ExcelManager excelManager = ExcelManagerFactory.CreateExcelManager(ExcelManagerFactory.ExcelManagerType.EPPlus);
            IWorkbook workbook = excelManager.OpenWorkbook(regisColorsFilename);
            IWorksheet sheet = workbook.GetSheet(0);

            // Check for header in row 1
            int rowIdx = 0;
            int value;
            string cellValue = sheet.GetCellValue(rowIdx, 1);
            if (!int.TryParse(cellValue, out value))
            {
                // skip header
                rowIdx++;
            }

            while (!sheet.IsEmpty(rowIdx, 0))
            {
                string regisSubstring = sheet.GetCellValue(rowIdx, 0);
                Color regisColor;
                try
                {
                    string rgbValueRedString = sheet.GetCellValue(rowIdx, 1);
                    string rgbValueGreenString = sheet.GetCellValue(rowIdx, 2);
                    string rgbValueBlueString = sheet.GetCellValue(rowIdx, 3);

                    regisColor = Color.FromArgb(int.Parse(rgbValueRedString), int.Parse(rgbValueGreenString), int.Parse(rgbValueBlueString));
                }
                catch (Exception ex)
                {
                    throw new ToolException("Could not parse RGB color in row " + (rowIdx + 1), ex);
                }

                if (!regisColorDef.ColorDictionary.ContainsKey(regisSubstring))
                {
                    regisColorDef.ColorDictionary.Add(regisSubstring.ToUpper(), regisColor);
                }
                else
                {
                    log.AddWarning("Skipping REGIS color key which is already defined: " + regisSubstring);
                }

                rowIdx++;
            }

            return regisColorDef;
        }
    }
}
