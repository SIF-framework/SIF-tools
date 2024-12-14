// ISDcreate is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of ISDcreate.
// 
// ISDcreate is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// ISDcreate is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with ISDcreate. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD;

namespace Sweco.SIF.ISDcreate
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

            System.Environment.Exit(exitcode);
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected override void DefineToolProperties()
        {
            AddAuthor("Koen van der Hauw");
            AddAuthor("Koen Jansen");
            ToolPurpose = "Tool for creating ISD-files from IPF- or GEN-files";
        }

        /// <summary>
        /// Starts actual tool process after reading and checking settings
        /// </summary>
        /// <returns>resultcode: 0 for success, 1 for errors</returns>
        protected override int StartProcess()
        {
            int exitcode = 0;

            // Retrieve tool settings that have been parsed from the command-line arguments 
            SIFToolSettings settings = (SIFToolSettings) Settings;

            int fileCount = 0;
            string[] inputFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter, settings.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

            if ((inputFilenames.Length > 1) && (settings.OutputFilename != null))
            {
                throw new ToolException("An output filename is specified, but more than one input file is found for current filter: '" + settings.InputFilter + "'. " +
                                        "Specify just an output path or modify input filter to select only a single file.");
            }

            // Create output path if not yet existing
            if (!Directory.Exists(settings.OutputPath))
            {
                Directory.CreateDirectory(settings.OutputPath);
            }

            Log.AddInfo("Processing input files ...");
            foreach (string inputFilename in inputFilenames)
            {
                // Get result filename
                string outputFilename = null;
                if (settings.OutputFilename == null)
                {
                    outputFilename = Path.Combine(settings.OutputPath, Path.GetFileNameWithoutExtension(inputFilename) + ".ISD");
                }
                else
                {
                    outputFilename = Path.Combine(settings.OutputPath, settings.OutputFilename);
                }

                // Read Input data
                ReadShapeFile(inputFilename, settings, out IMODFile iMODShapeFile, out ShapeFileType shapeFileType, out int shapeCount);
                RetrieveTopBotLevels(iMODShapeFile, shapeFileType, settings, out string topFilename, out string botFilename, out int topColIdx, out int botColIdx, out float topValue, out float botValue);

                // Write ISD-file
                WriteFile(iMODShapeFile, shapeFileType, shapeCount, topFilename, botFilename, topColIdx, botColIdx, topValue, botValue, outputFilename, settings);

                fileCount++;
            }

            ToolSuccessMessage = "Finished processing " + fileCount + " file(s)";

            return exitcode;
        }

        /// <summary>
        /// Read shapefile for ISD-file from input IPF- or GEN-file
        /// </summary>
        /// <param name="inputFilename"></param>
        /// <param name="settings"></param>
        /// <param name="iMODShapeFile"></param>
        /// <param name="shapeFileType"></param>
        /// <param name="shapeCount"></param>
        private void ReadShapeFile(string inputFilename, SIFToolSettings settings, out IMODFile iMODShapeFile, out ShapeFileType shapeFileType, out int shapeCount)
        {
            Log.AddInfo("Reading iMOD-file: " + Path.GetFileName(inputFilename) + " ...", 1);

            iMODShapeFile = null;
            shapeFileType = ShapeFileType.Unknown;
            shapeCount = -1;
            string extension = Path.GetExtension(inputFilename).ToLower();
            if (extension.Equals(".ipf"))
            {
                iMODShapeFile = IPFFile.ReadFile(inputFilename);
                shapeFileType = ShapeFileType.IPF;
                shapeCount = ((IPFFile)iMODShapeFile).PointCount;

                // Check ID columnindex if specified
                int columnCount = ((IPFFile)iMODShapeFile).ColumnNames.Count;
                if ((settings.IdColIdx >= 0) && (settings.IdColIdx >= columnCount))
                {
                    throw new ToolException("Invalid ID column number (" + (settings.IdColIdx + 1) + ") for column count (" + columnCount + ")");
                }
            }
            else if (extension.Equals(".gen"))
            {
                iMODShapeFile = GENFile.ReadFile(inputFilename);
                shapeFileType = ShapeFileType.GEN;
                shapeCount = ((GENFile)iMODShapeFile).Count;

                // Check ID columnindex if specified
                if (settings.IdColIdx >= 0)
                {
                    GENFile inputGENFile = (GENFile)iMODShapeFile;
                    if (!inputGENFile.HasDATFile())
                    {
                        throw new ToolException("Specified ID column number is not usable, no DAT-file defined for shape file: " + Path.GetFileName(iMODShapeFile.Filename));
                    }

                    DATFile inputDATFile =  inputGENFile.DATFile;
                    int columnCount = inputDATFile.ColumnNames.Count;
                    if (settings.IdColIdx >= columnCount)
                    {
                        throw new ToolException("Invalid ID column number (" + (settings.IdColIdx + 1) + ") for column count (" + columnCount + ")");
                    }
                }
            }
            else
            {
                throw new ToolException("Unknown shape file format for ISD-file: " + Path.GetFileName(inputFilename));
            }
        }

        /// <summary>
        /// Retrieve TOP- and BOT-levels
        /// </summary>
        /// <param name="iMODShapeFile"></param>
        /// <param name="shapeFileType"></param>
        /// <param name="settings"></param>
        /// <param name="topFilename"></param>
        /// <param name="botFilename"></param>
        /// <param name="topColIdx"></param>
        /// <param name="botColIdx"></param>
        /// <param name="topValue"></param>
        /// <param name="botValue"></param>
        private void RetrieveTopBotLevels(IMODFile iMODShapeFile, ShapeFileType shapeFileType, SIFToolSettings settings, out string topFilename, out string botFilename, out int topColIdx, out int botColIdx, out float topValue, out float botValue)
        {
            topFilename = null;
            botFilename = null;
            topColIdx = -1;
            botColIdx = -1;
            topValue = float.NaN;
            botValue = float.NaN;
            if (!float.TryParse(settings.TopDefinition, NumberStyles.Float, EnglishCultureInfo, out topValue))
            {
                if (File.Exists(settings.TopDefinition))
                {
                    topFilename = Path.GetFullPath(settings.TopDefinition);
                    Log.AddInfo("TOP-level is filename: " + Path.GetFileName(topFilename), 2);
                }
                else
                {
                    // Try as columnname
                    if (shapeFileType == ShapeFileType.IPF)
                    {
                        IPFFile inputIPFFile = (IPFFile)iMODShapeFile;
                        topColIdx = inputIPFFile.FindColumnNumber(settings.TopDefinition) - 1;
                    }
                    else if (shapeFileType == ShapeFileType.GEN)
                    {
                        GENFile inputGENFile = (GENFile)iMODShapeFile;
                        DATFile inputDATFile = inputGENFile.HasDATFile() ? inputDATFile = inputGENFile.DATFile : null;
                        topColIdx = (inputDATFile != null) ? inputDATFile.GetColIdx(settings.TopDefinition, false) : -1;
                    }
                    else
                    {
                        throw new ToolException("Unknown shape file type: " + shapeFileType);
                    }

                    if (topColIdx == -1)
                    {
                        throw new ToolException("Invalid TOP-level definition, file or column not found: " + settings.TopDefinition);
                    }

                    Log.AddInfo("TOP-level is columnname in " + Path.GetExtension(iMODShapeFile.Filename).Substring(1).ToUpper() + "-file: " + settings.TopDefinition + ", columnindex found: " + topColIdx, 2);
                }
            }
            else
            {
                Log.AddInfo("TOP-level is numeric: " + topValue, 2);
            }

            if (!float.TryParse(settings.BotDefinition, NumberStyles.Float, EnglishCultureInfo, out botValue))
            {

                if (File.Exists(settings.BotDefinition))
                {
                    botFilename = Path.GetFullPath(settings.BotDefinition);
                    Log.AddInfo("BOT-level is filename: " + Path.GetFileName(botFilename), 2);
                }
                else
                {
                    // Try as columnname
                    if (shapeFileType == ShapeFileType.IPF)
                    {
                        IPFFile inputIPFFile = (IPFFile)iMODShapeFile;
                        botColIdx = inputIPFFile.FindColumnNumber(settings.BotDefinition) - 1;
                    }
                    else if (shapeFileType == ShapeFileType.GEN)
                    {
                        GENFile inputGENFile = (GENFile)iMODShapeFile;
                        DATFile inputDATFile = inputGENFile.HasDATFile() ? inputDATFile = inputGENFile.DATFile : null;
                        botColIdx = (inputDATFile != null) ? inputDATFile.GetColIdx(settings.BotDefinition, false) : -1;
                    }
                    if (botColIdx == -1)
                    {
                        throw new ToolException("Invalid BOT-level definition, file or column not found: " + settings.BotDefinition);
                    }
                    Log.AddInfo("BOT-level is columnname in " + Path.GetExtension(iMODShapeFile.Filename).Substring(1).ToUpper() + "-file: " + settings.BotDefinition + ", columnindex found: " + botColIdx, 2);
                }
            }
            else
            {
                Log.AddInfo("BOT-level is numeric: " + botValue, 2);
            }
        }

        /// <summary>
        /// Write ISD-file with filename as specified in settings
        /// </summary>
        /// <param name="outputFilename"></param>
        /// <param name="settings"></param>
        /// <param name="iMODShapeFile"></param>
        /// <param name="shapeFileType"></param>
        /// <param name="shapeCount"></param>
        /// <param name="topFilename"></param>
        /// <param name="botFilename"></param>
        /// <param name="topColIdx"></param>
        /// <param name="botColIdx"></param>
        /// <param name="topValue"></param>
        /// <param name="botValue"></param>
        private void WriteFile(IMODFile iMODShapeFile, ShapeFileType shapeFileType, int shapeCount, string topFilename, string botFilename, int topColIdx, int botColIdx, float topValue, float botValue, string outputFilename, SIFToolSettings settings)
        {
            Log.AddInfo("Writing ISD-file " + Path.GetFileName(outputFilename) + " ...", 1);
            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(outputFilename);
                for (int shapeIdx = 0; shapeIdx < shapeCount; shapeIdx++)
                {
                    string id = null;
                    GENFeature genFeature = null;
                    DATRow datRow = null;
                    IPFPoint ipfPoint = null;

                    // Retrieve iMOD-file, current feature and id
                    IPFFile inputIPFFile = null;
                    GENFile inputGENFile = null;
                    DATFile inputDATFile = null;
                    if (shapeFileType == ShapeFileType.IPF)
                    {
                        inputIPFFile = (IPFFile)iMODShapeFile;

                        ipfPoint = inputIPFFile.Points[shapeIdx];
                        id = (settings.IdColIdx > 0) ? inputIPFFile.Points[shapeIdx].ColumnValues[settings.IdColIdx] : "IPFPoint " + (shapeIdx + 1).ToString();
                    }
                    else if (shapeFileType == ShapeFileType.GEN)
                    {
                        inputGENFile = (GENFile)iMODShapeFile;
                        inputDATFile = inputGENFile.HasDATFile() ? inputDATFile = inputGENFile.DATFile : null;

                        genFeature = inputGENFile.Features[shapeIdx];
                        string featureId = genFeature.ID;
                        bool hasIntId = int.TryParse(featureId, out int intId);
                        datRow = null;
                        if (inputDATFile != null)
                        {
                            datRow = inputDATFile.GetRow(featureId);
                            id = (settings.IdColIdx >= 0) ? datRow[settings.IdColIdx] : (hasIntId ? "GENFeature " + featureId : featureId);
                        }
                        else
                        {
                            id = (hasIntId ? "GENFeature " + featureId : featureId);
                        }
                    }
                    else
                    {
                        throw new ToolException("Undefined input file type: " + iMODShapeFile.Filename);
                    }

                    // Write header for current feature
                    sw.WriteLine("==================================================");
                    sw.WriteLine("SHAPE" + (shapeIdx + 1) + " <- " + id);
                    sw.WriteLine("--------------------------------------------------");

                    // Write feature definition
                    if (shapeFileType == ShapeFileType.IPF)
                    {
                        sw.WriteLine("1027, POINT");
                        sw.WriteLine(settings.N1.ToString("F2", EnglishCultureInfo) + "," + settings.N2.ToString("F2", EnglishCultureInfo));
                    }
                    else if (shapeFileType == ShapeFileType.GEN)
                    {
                        if (genFeature.Points[0].Equals(genFeature.Points[genFeature.Points.Count - 1]))
                        {
                            sw.WriteLine("1025, POLYGON");
                            sw.WriteLine(settings.N1.ToString("F2", EnglishCultureInfo) + "," + settings.N2.ToString("F2", EnglishCultureInfo));
                        }
                        else
                        {
                            sw.WriteLine("1028, LINE");
                            sw.WriteLine(settings.N1.ToString("F2", EnglishCultureInfo));
                        }
                    }
                    else
                    {
                        throw new Exception("Invalid shape type: " + shapeFileType);
                    }

                    // Write TOP- and BOT-levels
                    if (topFilename != null)
                    {
                        sw.WriteLine("\"" + topFilename + "\"");
                    }
                    else if (topColIdx != -1)
                    {
                        string topString = (inputIPFFile != null) ? ipfPoint.ColumnValues[topColIdx] : datRow[topColIdx];
                        if (!float.TryParse(topString, NumberStyles.Float, EnglishCultureInfo, out topValue))
                        {
                            throw new ToolException("Invalid TOP-level value for point " + (shapeIdx + 1) + " in column " + settings.TopDefinition + ": " + topString);
                        }
                        sw.WriteLine(topValue.ToString("F2", EnglishCultureInfo));
                    }
                    else if (!topValue.Equals(float.NaN))
                    {
                        sw.WriteLine(topValue.ToString("F2", EnglishCultureInfo));
                    }
                    if (botFilename != null)
                    {
                        sw.WriteLine("\"" + botFilename + "\"");
                    }
                    else if (botColIdx != -1)
                    {
                        string botString = (inputIPFFile != null) ? ipfPoint.ColumnValues[botColIdx] : datRow[botColIdx];
                        if (!float.TryParse(botString, NumberStyles.Float, EnglishCultureInfo, out botValue))
                        {
                            throw new ToolException("Invalid BOT-level value for point " + (shapeIdx + 1) + " in column " + settings.BotDefinition + ": " + botString);
                        }
                        sw.WriteLine(botValue.ToString("F2", EnglishCultureInfo));
                    }
                    else if (!botValue.Equals(float.NaN))
                    {
                        sw.WriteLine(botValue.ToString("F2", EnglishCultureInfo));
                    }

                    // Write number indicating whether a reference level is used: 0 = reference level is not used; 1 = reference level is used
                    sw.WriteLine("0");

                    // Write vertical interval number
                    sw.WriteLine(settings.VIN);

                    sw.WriteLine("--------------------------------------------------");

                    // Write point(s)
                    if (inputIPFFile != null)
                    {
                        sw.WriteLine("1 <- No. Points Shape");
                        sw.WriteLine(" " + ipfPoint.XString + ", " + ipfPoint.YString);
                    }
                    else
                    {
                        sw.WriteLine(genFeature.Points.Count + " <- No. Points Polygon");
                        for (int pointIdx = 0; pointIdx < genFeature.Points.Count; pointIdx++)
                        {
                            Point point = genFeature.Points[pointIdx];
                            sw.WriteLine(" " + point.XString + ", " + point.YString);
                        }
                    }

                    sw.WriteLine("==================================================");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error when writing ISD-file: " + outputFilename, ex);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }
    }
}
