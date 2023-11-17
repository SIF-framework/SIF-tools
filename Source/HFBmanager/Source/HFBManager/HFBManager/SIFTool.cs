// HFBmanager is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of HFBmanager.
// 
// HFBmanager is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HFBmanager is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HFBmanager. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.Spreadsheets;
using Sweco.SIF.Spreadsheets.Excel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.HFBManager
{
    public class SIFTool : SIFToolBase
    {
        #region Constructor

        /// <summary>
        /// Creates a SIFTool instance and initializes tool name and version and a Log object with the console as a default listener
        /// </summary>
        public SIFTool(SIFToolSettingsBase settings) : base(settings)
        {
            SetLicense(new SIFGPLLicense(this));
            settings.RegisterSIFTool(this);
        }

        #endregion

        /// <summary>
        /// Entry point of tool
        /// </summary>
        /// <param name="args">command-line arguments</param>
        static void Main(string[] args)
        {
            int exitcode = -1;
            SIFTool tool = null;
            try
            {
                // Use SwecoTool Framework to handle license check, write of toolname and version, parsing arguments, writing of logfile and if specified so handling exeptions
                SIFToolSettings settings = new SIFToolSettings(args);
                tool = new SIFTool(settings);

                exitcode = tool.Run();
            }
            catch (ToolException ex)
            {
                ExceptionHandler.HandleToolException(ex, tool?.Log);
                exitcode = 1;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, tool?.Log);
                exitcode = 1;
            }

            Environment.Exit(exitcode);
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected override void DefineToolProperties()
        {
            AddAuthor("Koen van der Hauw");
            ToolPurpose = "SIF-tool for manipulating or retrieving info about GEN-files for HFB-package";
        }

        /// <summary>
        /// Starts actual tool process after reading and checking settings
        /// </summary>
        /// <returns>resultcode: 0 for success, 1 for errors</returns>
        protected override int StartProcess()
        {
            int exitcode = 0;

            // Retrieve tool settings that have been parsed from the command-line arguments 
            SIFToolSettings settings = (SIFToolSettings)Settings;

            // Place worker code here
            string outputPath = settings.OutputPath;

            // Create output path if not yet existing
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // An example for reading files from a path and creating a new file...
            string[] inputFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter, settings.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            if ((inputFilenames.Length > 1) && (settings.OutputFilename != null))
            {
                if (Path.GetExtension(settings.OutputFilename).ToLower().Equals(".gen"))
                {
                    throw new ToolException("An output GEN-file is specified, but more than one input file is found for filter '" + settings.InputFilter + "'. " +
                                            "Specify just an output path or modify input filter to select only a single file.");
                }
            }

            List<string> units = ReadUnitOrder(settings, Log, 0);

            Log.AddInfo("Processing input files ...");
            int fileCount = 0;
            foreach (string inputFilename in inputFilenames)
            {
                ProcessGENFile(inputFilename, units, settings, Log, 1);

                if ((settings.OutputFilename != null) && Path.GetExtension(settings.OutputFilename).ToLower().Equals(".csv"))
                {
                    settings.MergedCSVFilename = Path.Combine(settings.OutputPath, settings.OutputFilename);
                }

                fileCount++;
            }

            ToolSuccessMessage = "Finished processing " + fileCount + " file(s)";

            return exitcode;
        }

        /// <summary>
        /// Retrieve list of all lower case unit names in order as defined by order file in settings
        /// </summary>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        private List<string> ReadUnitOrder(SIFToolSettings settings, Log log, int logIndentLevel)
        {
            List<string> units = new List<string>();

            string extension = Path.GetExtension(settings.OrderFilename).ToLower();
            if ((settings.OrderFilename != null) && (extension.Equals(".xlsx") || extension.Equals(".csv")))
            {
                Log.AddInfo("Reading order file ...", logIndentLevel);
                ExcelManager excelManager = null;
                if (extension.Equals(".xlsx"))
                {
                    excelManager = ExcelManagerFactory.CreateExcelManager(ExcelManagerFactory.ExcelManagerType.EPPlus);
                }
                else
                {
                    excelManager = ExcelManagerFactory.CreateExcelManager(ExcelManagerFactory.ExcelManagerType.CSV);
                }

                IWorkbook workbook = excelManager.OpenWorkbook(settings.OrderFilename);
                int sheetIdx = SpreadsheetUtils.FindSheetIndex(workbook, settings.OrderFileSheetRef);
                int rowIdx = settings.OrderFileRowNr - 1;
                int colIdx = settings.OrderFileColNr -1 ;

                IWorksheet worksheet = workbook.GetSheet(sheetIdx);

                while (!worksheet.IsEmpty(rowIdx, colIdx))
                {
                    string unit = worksheet.GetCellValue(rowIdx, colIdx);
                    units.Add(unit.ToLower());
                    rowIdx++;
                }

                workbook.Close();

                excelManager.Cleanup();
            }
            else
            {
                throw new ToolException("Unknown extension for order file: " + Path.GetFileName(settings.OrderFilename));
            }

            return units;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="inputGENFilename"></param>
        /// <param name="units">ordered units in which higher units have smaller/lower incides)</param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        protected void ProcessGENFile(string inputGENFilename, List<string> units, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            Log.AddInfo("Reading file " + Path.GetFileName(inputGENFilename) + " ...", logIndentLevel);

            GENFile inputGENFile = GENFile.ReadFile(inputGENFilename);
            if (inputGENFile.HasDATFile())
            {
                DATFile inputDATFile = inputGENFile.DATFile;
                int unitColIdx = inputDATFile.FindColumnIndex(settings.UnitColumnRef);
                if (unitColIdx < 0)
                {
                    Log.AddWarning("Invalid unit column reference (" + settings.UnitColumnRef + "), GEN-file is skipped: " + Path.GetFileName(inputGENFilename), logIndentLevel + 1);
                }

                foreach (FaultAction faultAction in settings.FaultActions)
                {
                    switch (faultAction)
                    {
                        case FaultAction.Split:
                            GENFile outputGENFile = SplitGENFile(inputGENFile, inputDATFile, unitColIdx, units, settings, log, logIndentLevel);

                            // Use output GEN-file as input GEN-file for next action
                            inputGENFile = outputGENFile;
                            inputDATFile = inputGENFile.DATFile;
                            break;

                        case FaultAction.CreateCSV:
                            CreateCSVFile(inputGENFile, inputDATFile, unitColIdx, units, settings, log, logIndentLevel);
                            break;
                        default:
                            throw new Exception("Unexpected fault action: " + faultAction);

                    }
                }
            }
            else
            {
                Log.AddWarning("GEN-file has no DAT-file and is skipped: " + Path.GetFileName(inputGENFilename), logIndentLevel + 1);
            }
        }

        private void CreateCSVFile(GENFile inputGENFile, DATFile inputDATFile, int unitColIdx, List<string> units, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            log.AddInfo("Creating Fault CSV-file with specified weights for GEN-file ...", logIndentLevel);

            StringBuilder faultCSVString = new StringBuilder();
            if (settings.MergedCSVFilename != null)
            {
                if (File.Exists(settings.MergedCSVFilename))
                {
                    log.AddInfo("Reading existing fault CSV-file '" + Path.GetFileName(settings.MergedCSVFilename) + "' ...", logIndentLevel + 1);
                    string inputCSVString = FileUtils.ReadFile(settings.MergedCSVFilename);
                    faultCSVString.Append(inputCSVString);
                }
                else
                {
                    throw new ToolException("Specified input CSV-file does not exist: " + settings.MergedCSVFilename);
                }

            }
            else
            {
                faultCSVString.AppendLine("MODELLAAG,WEERSTAND,BREUK");
            }


            if (settings.FaultDefinitions == null)
            {
                log.AddInfo("No fault definitions specified, using default weight: " + settings.DefaultWeight, logIndentLevel + 1);
            }

            // Loop through all faults, check which fault units are present and retrieve resistance per unit
            List<string> faultUnits = new List<string>();
            for (int featureIdx = 0; featureIdx < inputGENFile.Count; featureIdx++)
            {
                GENFeature feature = inputGENFile.Features[featureIdx];
                DATRow datRow = inputDATFile.GetRow(feature.ID);
                string unit = datRow[unitColIdx];

                if (!faultUnits.Contains(unit))
                {
                    faultUnits.Add(unit.ToLower());
                }
            }

            // Loop through all units in reverse order, check if a fault exists for this unit and retrieve weight from fault definitions
            for (int unitIdx = units.Count - 1; unitIdx >= 0; unitIdx--)
            {
                string unit = units[unitIdx];
                if (faultUnits.Contains(unit))
                {
                    log.AddInfo("Searching weight for unit '" + unit + "'", logIndentLevel + 1);

                    float weight = RetrieveWeight(unit, units, settings, log, logIndentLevel + 1);
                    string inputGENFilename = Path.GetFileNameWithoutExtension(inputGENFile.Filename);
                    string csvRowString = "0," + weight.ToString(EnglishCultureInfo) + "," + inputGENFilename + "_" + unit.ToUpper() + ".GEN";
                    faultCSVString.AppendLine(csvRowString);
                }
            }

            string outputCSVFilename = RetrieveOutputFilename(inputGENFile.Filename, settings.OutputPath, settings.InputPath, settings.OutputFilename, "CSV");
            Log.AddInfo("Writing resulting CSV-file '" + Path.GetFileName(outputCSVFilename) + "' ...", logIndentLevel + 1);
            FileUtils.WriteFile(outputCSVFilename, faultCSVString.ToString());
        }

        private float RetrieveWeight(string unit, List<string> units, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            // Check which fault definition and weight applies to unit
            int unitIdx = units.IndexOf(unit.ToLower());
            if (unitIdx < 0)
            {
                throw new ToolException("Unknown unit: " + unit);
            }

            if (settings.FaultDefinitions != null)
            {
                // Find first definition with upperunit below specified unit
                int upperUnitIdx = 0;
                FaultDefinition faultDefinition = null;
                int faultDefIdx = 0;
                while (faultDefIdx < settings.FaultDefinitions.Count)
                {
                    faultDefinition = settings.FaultDefinitions[faultDefIdx];
                    upperUnitIdx = units.IndexOf(faultDefinition.UpperUnit.ToLower());
                    if (upperUnitIdx < 0)
                    {
                        throw new ToolException("Unknown unit for fault definition: " + faultDefinition);
                    }

                    if (unitIdx < upperUnitIdx)
                    {
                        // A fault definition upper unit is found below specified unit
                        break;
                    }

                    faultDefIdx++;
                }

                // Use previous fault definition to specify weight
                if (faultDefIdx > 0)
                {
                    faultDefinition = settings.FaultDefinitions[faultDefIdx - 1];
                    log.AddInfo("Using fault definition with upper unit '" + faultDefinition.UpperUnit + "' for unit " + unit, logIndentLevel + 1);

                    return faultDefinition.Weight;
                }
                else
                {
                    log.AddInfo("No fault definition found, using defaukt weight: " + settings.DefaultWeight, logIndentLevel + 1);

                    return settings.DefaultWeight;
                }
            }
            else
            {
                return settings.DefaultWeight;
            }
        }

        private GENFile SplitGENFile(GENFile inputGENFile, DATFile inputDATFile, int unitColIdx, List<string> units, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            GENFile outputGENFile = null;
            if (settings.FaultDefinitions == null)
            {
                log.AddInfo("No fault definition specified, splitting is skipped (input GEN-file is copied) ...", logIndentLevel);
                outputGENFile = inputGENFile;
            }
            else
            {
                log.AddInfo("Splitting GEN-file with specified Fault Definition ...", logIndentLevel);

                outputGENFile = new GENFile();
                outputGENFile.AddDATFile();
                DATFile outputDATFile = outputGENFile.DATFile;
                outputDATFile.AddColumns(inputDATFile.ColumnNames);

                // First copy all existing features and keep existing ID's
                for (int featureIdx = 0; featureIdx < inputGENFile.Count; featureIdx++)
                {
                    GENFeature feature = inputGENFile.Features[featureIdx];
                    outputGENFile.AddFeature(feature);
                }

                // Now check which features need to be copied with modified units
                if (settings.FaultDefinitions != null)
                {
                    foreach (FaultDefinition faultDefinition in settings.FaultDefinitions)
                    {
                        log.AddInfo("Processing fault definition for unit '" + faultDefinition.UpperUnit + "' ...", logIndentLevel + 1);
                        int upperUnitIdx = units.IndexOf(faultDefinition.UpperUnit.ToLower());

                        if (upperUnitIdx < 0)
                        {
                            throw new ToolException("Unknown unit for fault definition: " + faultDefinition);
                        }

                        // Check if feature should be copied according to specified upper units
                        for (int featureIdx = 0; featureIdx < inputGENFile.Count; featureIdx++)
                        {
                            GENFeature feature = inputGENFile.Features[featureIdx];
                            DATRow datRow = inputDATFile.GetRow(feature.ID);
                            if (datRow == null)
                            {
                                throw new ToolException("Feature " + feature.ID + " is not found in DAT-file, check that ID is defined: " + Path.GetFileName(inputDATFile.Filename));
                            }
                            string unit = datRow[unitColIdx];
                            int unitIdx = units.IndexOf(unit.ToLower());
                            if (unitIdx < 0)
                            {
                                throw new ToolException("Unknown unit for feature " + feature.ID + ": " + unit);
                            }

                            // check if fault unit of current feature is higher than specified fault upper unit according to specified ranking
                            if (unitIdx < upperUnitIdx)
                            {
                                // Current unit is above specified upper unit, so make a copy with name of upper unit
                                log.AddInfo("Specified fault upper unit '" + faultDefinition.UpperUnit + "' is below unit '" + unit + "' of feature " + feature.ID + ". Fault is copied.", logIndentLevel + 2);
                                GENFeature featureCopy = feature.Copy();

                                // Add feature to new GEN-file (which will copy DAT-row automatically)
                                outputGENFile.AddFeature(featureCopy, true);

                                string newID = featureCopy.ID;
                                DATRow newDATRow = outputGENFile.DATFile.GetRow(newID);
                                newDATRow[unitColIdx] = faultDefinition.UpperUnit;
                            }
                        }
                    }
                }
            }

            string outputGENFilename;
            if ((settings.OutputFilename != null) && Path.GetExtension(settings.OutputFilename).ToLower().Equals(".csv"))
            {
                // ignore CSV output filename
                outputGENFilename = RetrieveOutputFilename(inputGENFile.Filename, settings.OutputPath, settings.InputPath, null, "GEN");
            }
            else
            {
                outputGENFilename = RetrieveOutputFilename(inputGENFile.Filename, settings.OutputPath, settings.InputPath, settings.OutputFilename, "GEN");

            }
            Log.AddInfo("Writing resulting GEN-file '" + Path.GetFileName(outputGENFilename) + "' ...", logIndentLevel + 1);
            outputGENFile.WriteFile(outputGENFilename);

            return outputGENFile;
        }
    }
}
