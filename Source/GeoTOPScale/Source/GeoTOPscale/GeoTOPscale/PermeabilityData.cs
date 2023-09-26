// GeoTOPScale is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of GeoTOPScale.
// 
// GeoTOPScale is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// GeoTOPScale is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with GeoTOPScale. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Spreadsheets.Excel;
using Sweco.SIF.Spreadsheets;
using System.IO;
using System.Globalization;
using Sweco.SIF.iMOD.IDF;

namespace Sweco.SIF.GeoTOPscale
{
    public class PermeabilityData
    {
        protected Dictionary<string, float[]> PermeabilityDictionary { get; set; }

        public PermeabilityData()
        {
        }

        public Dictionary<string, float[]> ReadPermeabilityTable(SIFToolSettings settings, Log log)
        {
            log.AddInfo("Reading Excel workbook '" + Path.GetFileName(settings.KTableFilename) + "' ...");
            PermeabilityDictionary = new Dictionary<string, float[]>();

            SpreadsheetManager excelManager = ExcelManagerFactory.CreateExcelManager(ExcelManagerFactory.ExcelManagerType.EPPlus);
            IWorkbook workbook;
            try
            {
                workbook = excelManager.OpenWorkbook(settings.KTableFilename);
            }
            catch
            {
                throw new ToolException("Could not read permeability workbook: " + settings.KTableFilename);
            }

            IWorksheet sheet = null;
            if (int.TryParse(settings.KTableSheetID, out int sheetnumber))
            {
                try
                {
                    sheet = workbook.GetSheet(sheetnumber - 1);
                }
                catch (Exception)
                {
                    throw new ToolException("Sheetnumber " + sheetnumber + " of permeability table does not exist in Excel-file: " + settings.KTableFilename);
                }
            }
            else
            {
                try
                {
                    sheet = workbook.GetSheet(settings.KTableSheetID);
                }
                catch (Exception)
                {
                    throw new ToolException("Sheetname " + settings.KTableSheetID + " of permeability table does not exist in Excel-file: " + settings.KTableFilename);
                }
            }

            int lastRowIdx = sheet.End(new Cell(sheet, sheet.LastRowIdx, 1), CellDirection.Up).RowIdx;
            int rowIdx = settings.KTableDataRowIdx;
            try
            {
                while (rowIdx <= lastRowIdx)
                {
                    string stratString = sheet.GetCellValue(rowIdx, settings.KTableStratColIdx);
                    string lithoString = sheet.GetCellValue(rowIdx, settings.KTableLithoColIdx);
                    string khString = sheet.GetCellValue(rowIdx, settings.KTableKHColIdx, SIFTool.EnglishCultureInfo);
                    string kvString = sheet.GetCellValue(rowIdx, settings.KTableKVColIdx, SIFTool.EnglishCultureInfo);
                    string key = stratString + lithoString;
                    if (!float.TryParse(khString, NumberStyles.Float, SIFTool.EnglishCultureInfo, out float kh))
                    {
                        throw new ToolException("Could not parse kh-value: " + khString + " in permeability table in row number: " + (rowIdx + 1));
                    }
                    if (!float.TryParse(kvString, NumberStyles.Float, SIFTool.EnglishCultureInfo, out float kv))
                    {
                        throw new ToolException("Could not parse kv-value: " + kvString + " in permeability table in row number: " + (rowIdx + 1));
                    }

                    float[] value = { kh, kv };
                    PermeabilityDictionary.Add(key, value);
                    rowIdx++;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error in row " + (rowIdx + 1) + " while reading permeability table", ex);
            }

            return PermeabilityDictionary;
        }

        public void RetrieveKValues(Layer selLayer, float layerThickness, IDFFile totalThicknessIDFFile, IDFFile resistanceStackIDFFile, ref IDFFile khIDFFile, ref IDFFile kvIDFFile, IDFFile stratIDFFile, IDFFile lithoIDFFile, string modelOutputPath, SIFToolSettings settings)
        {
            stratIDFFile.EnsureLoadedValues();
            lithoIDFFile.EnsureLoadedValues();
            stratIDFFile = stratIDFFile.ClipIDF(selLayer.TOPIDFFile.Extent);
            lithoIDFFile = lithoIDFFile.ClipIDF(selLayer.TOPIDFFile.Extent);
            float localTop = float.NaN;

            if (stratIDFFile.TOPLevel.Equals(stratIDFFile.NoDataValue) || stratIDFFile.TOPLevel.Equals(stratIDFFile.NoDataCalculationValue))
            { 
                throw new ToolException("stratigraphy voxel TOP/BOT-level is missing for: " + stratIDFFile.Filename);
            }
            else
            {
                localTop = stratIDFFile.TOPLevel;
                khIDFFile.SetITBLevels(localTop, localTop - layerThickness);
            }

            for (int rowIdx = 0; rowIdx < stratIDFFile.NRows; rowIdx++)
            {
                float y = stratIDFFile.GetY(rowIdx);
                for (int colIdx = 0; colIdx < stratIDFFile.NCols; colIdx++)
                {
                    float x = stratIDFFile.GetX(colIdx);
                    float stratcode = stratIDFFile.GetValue(x, y);
                    float lithoclass = lithoIDFFile.GetValue(x, y);
                    float top = selLayer.TOPIDFFile.GetValue(x, y);
                    float bot = selLayer.BOTIDFFile.GetValue(x, y);
                    bool hasValue = true;
                    if (lithoclass.Equals(lithoIDFFile.NoDataValue) || stratcode.Equals(stratIDFFile.NoDataValue) || lithoclass.Equals(float.NaN) || stratcode.Equals(float.NaN)
                        || top.Equals(selLayer.TOPIDFFile.NoDataValue) || bot.Equals(selLayer.BOTIDFFile.NoDataValue) || top.Equals(float.NaN) || bot.Equals(float.NaN))
                    {
                        hasValue = false;
                    }
                    else if (settings.BoundaryLayerSelectionMethod < 0 || settings.BoundaryLayerSelectionMethod > 2)
                    {
                        throw new ToolException("Boundary selection method must be 0, 1 or 2");
                    }
                    else
                    {
                        switch (settings.BoundaryLayerSelectionMethod)
                        {
                            case 0:
                                if (localTop > top || (localTop - layerThickness) < bot)
                                {
                                    hasValue = false;
                                }
                                break;
                            // else hasvalue == true
                            case 1:
                                if (localTop > (top + layerThickness) || localTop < bot)
                                {
                                    hasValue = false;
                                }
                                break;
                            // else hasvalue == true
                            case 2:
                                if (localTop > (top + 0.5 * layerThickness) || (localTop - 0.5 * layerThickness) < bot)
                                {
                                    hasValue = false;
                                }
                                break;
                                // else hasvalue == true
                        }
                    }

                    if (!hasValue)
                    {
                        khIDFFile.SetValue(x, y, khIDFFile.NoDataValue);
                        kvIDFFile.SetValue(x, y, kvIDFFile.NoDataValue);
                    }
                    else
                    {
                        if (!PermeabilityDictionary.TryGetValue(stratcode.ToString() + lithoclass.ToString(), out float[] karray))
                        {
                            throw new ToolException("Combination of stratigraphy code and litho classe could not be found: " + stratcode.ToString() + ", " + lithoclass.ToString());
                        }
                        else if ((karray[0] == 0) || (karray[1] == 0))
                        {
                            throw new ToolException("Combination of stratigraphy code and litho classe is 0: " + stratcode.ToString() + ", " + lithoclass.ToString());
                        }
                        else
                        {
                            khIDFFile.SetValue(x, y, karray[0]);
                            kvIDFFile.SetValue(x, y, karray[1]);
                            float oldThickness = totalThicknessIDFFile.GetValue(x, y);
                            float oldResistance = resistanceStackIDFFile.GetValue(x, y);
                            float newResistance = oldResistance + layerThickness / karray[1];
                            totalThicknessIDFFile.SetValue(x, y, oldThickness + layerThickness);
                            resistanceStackIDFFile.SetValue(x, y, newResistance);
                        }
                    }
                }
            }
            // totalThicknessIDFFile.WriteFile(Path.Combine(modelOutputPath, "thickness"));
            // resistanceStackIDFFile.WriteFile(Path.Combine(modelOutputPath, "resistanceStack"));
        }

    }
}
