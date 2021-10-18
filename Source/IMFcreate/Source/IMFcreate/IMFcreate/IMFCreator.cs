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
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.IMF;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMOD.Legends;
using Sweco.SIF.iMOD.Utils;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IMFcreate
{
    /// <summary>
    /// Class for creating IMF-files based on INI-file
    /// </summary>
    public class IMFCreator
    {
        private const float IMFXYRatio = 2.06f;
        private const string DefaultIMFFilename = "CREATEDIMF";
        private const string ParameterSectionKeyword = "[PARAMETERS]";
        private const string Parameter_ExtentKeyword = "EXTENT";
        private const string Parameter_OpeniMODKeyword = "OPENIMOD";
        private const string Parameter_AddOnceKeyword = "ADDONCE";
        private const string Parameter_iMODExecutablePathKeyword = "IMODEXE";
        private const string Parameter_IMFFilenameKeyword = "IMFFILENAME";
        private const string MapsSectionKeyword = "[MAPS]";
        private const string Maps_FileKeyword = "FILE";
        private const string Maps_LegendKeyword = "LEGEND";
        private const string Maps_IsSelectedKeyword = "SELECTED";
        private const string Maps_ColumnIndexKeyword = "COLUMN";
        private const string Maps_ThicknessKeyword = "THICKNESS";
        private const string Maps_ColorKeyword = "COLOR";
        private const string Maps_LineColorKeyword = "LINECOLOR";
        private const string Maps_FillColorKeyword = "FILLCOLOR";

        private const string CrosssectionSectionKeyword = "[CROSSSECTION]";
        private const string Crosssection_REGISKeyword = "REGIS";
        private const string Crosssection_REGISColorsKeyword = "REGISCOLORS";
        private const string Crosssection_REGISOrderFileKeyword = "REGISORDER";
        private const string Crosssection_LayersAsLinesKeyword = "LAYERSASLINES";
        private const string Crosssection_LayersLineColorKeyword = "LINECOLOR";
        private const string Crosssection_LayersAsPlanesKeyword = "LAYERSASPLANES";
        private const string IDFExtension = ".IDF";
        private const string GENExtension = ".GEN";
        private const string IPFExtension = ".IPF";
        private const string IMFExtension = ".IMF";
        private const string OverlaysSectionKeyword = "[OVERLAYS]";
        private const string Overlays_ThicknessKeyword = "THICKNESS";
        private const string Overlays_ColorKeyword = "COLOR";
        private const string REGISTNOColorsKeyword = "TNO"; // TNO REGIS-colors
        private const string REGISAQFColorsKeyword = "AQF"; // Yellow/Green-colors for aquifers/aquitards
        private const int defaultThickness = 3;
        private static Dictionary<string, Color> regisTNOColorDictionary = null;
        private static Dictionary<string, Color> regisGeohydrologicalColorDictionary = null;
        private static List<string> regisEenheden = null;
        private static List<Color> typesOfYellow = null;
        private static List<Color> typesOfGreen = null;
        private static Color defaultTOPLineColor;
        private static Color defaultBOTLineColor;

        private Dictionary<string, int> layerOrderPrefixIdxDictionary;
        private Dictionary<int, string> layerOrderIdxPrefixDictionary;

        protected static CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

        public IMFCreator()
        {
            DoREGISInitialization();

            // Define default color settings
            typesOfYellow = new List<Color>{Color.FromArgb(255, 255, 129),
                Color.FromArgb(255, 254, 2), Color.FromArgb(230, 224, 3)};
            typesOfGreen = new List<Color>{Color.FromArgb(34, 101, 0),
                Color.FromArgb(3, 197, 0), Color.FromArgb(2, 125, 10),
                Color.FromArgb(9, 226, 3), Color.FromArgb(0, 184, 2)};
            defaultTOPLineColor = Color.FromArgb(128, 0, 0);
            defaultBOTLineColor = Color.LightCoral;
        }

        public void StartProcess(SIFToolSettings settings, Log log)
        {
            string inputFile = settings.INIFilename;
            string outputPath = settings.OutputPath;

            IMFFile imfFile = new IMFFile();
            imfFile.Extent = null; // new Extent(-1F, -1F, 1F, 1F);
            StreamReader srINIFile = null;
            INIParameters parameters = new INIParameters();
            parameters.IMFFilename = DefaultIMFFilename;
            parameters.INIPath = Path.GetDirectoryName(settings.INIFilename);

            try
            {
                log.AddInfo("Reading INI-file: " + Path.GetFileName(inputFile) + " ...");
                srINIFile = new StreamReader(inputFile);
                string line = string.Empty;
                while (!srINIFile.EndOfStream)
                {
                    while (!srINIFile.EndOfStream && line.Trim().Equals(string.Empty))
                    {
                        line = srINIFile.ReadLine();
                    }

                    if (line.Trim().ToUpper().Equals(ParameterSectionKeyword))
                    {
                        ParseParametersSection(srINIFile, imfFile, log, ref line, ref parameters, settings);
                    }
                    else if (line.Trim().ToUpper().Equals(MapsSectionKeyword))
                    {
                        ParseMapsSection(srINIFile, imfFile, log, parameters, ref line, settings);
                    }
                    else if (line.Trim().ToUpper().Equals(CrosssectionSectionKeyword))
                    {
                        ParseCrosssectionSection(srINIFile, imfFile, log, parameters, ref line, settings);
                    }
                    else if (line.Trim().ToUpper().Equals(OverlaysSectionKeyword))
                    {
                        ParseOverlaysSection(srINIFile, imfFile, log, parameters, ref line, settings);
                    }
                    else if (line.StartsWith("REM "))
                    {
                        // ignore, read next line
                        line = srINIFile.ReadLine();
                    }
                    else if (!line.Trim().Equals(string.Empty))
                    {
                        throw new ToolException("Unkown keyword: " + line);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error when reading INI-file:" + Path.GetFileName(inputFile), ex);
            }
            finally
            {
                if (srINIFile.EndOfStream)
                {
                    srINIFile.Close();
                }
            }

            if (imfFile.Extent == null)
            {
                imfFile.Extent = imfFile.GetMaxMapExtent();
            }

            // Check and fix aspect ratio
            float xyRatio = imfFile.GetExtentAspectRatio();
            if (imfFile.FixExtentAspectRatio(IMFXYRatio))
            {
                log.AddInfo("Fixed aspect ratio for extent from " + xyRatio.ToString("F3", englishCultureInfo) + " to " + IMFXYRatio.ToString("F3", englishCultureInfo) + " ...");

                // Enlarge extent to fit between axes
                imfFile.Extent = imfFile.Extent.Enlarge(0.125f);
            }

            string outputFilename = parameters.IMFFilename;
            if (!Path.IsPathRooted(outputFilename))
            {
                outputFilename = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(parameters.IMFFilename) + IMFExtension);
            }

            log.AddInfo("Writing IMF-file '" + Path.GetFileName(outputFilename) + "' ...");
            imfFile.WriteFile(outputFilename, false);

            if (parameters.IsIMODopened)
            {
                StartIMOD(parameters.IMODPath, outputFilename, log);
            }
        }

        private void ParseOverlaysSection(StreamReader srINIFile, IMFFile imfFile, Log log, INIParameters parameters, ref string line, SIFToolSettings settings)
        {
            log.AddInfo("adding overlay files.....", 1);
            line = !srINIFile.EndOfStream ? srINIFile.ReadLine() : null;
            while ((line != null) && (line.Trim().Length != 0))
            {
                string imodFilename = GetFullPath(line.Replace("\"", string.Empty).Trim(), parameters.INIPath);
                if (File.Exists(imodFilename))
                {
                    Color color = Color.Black;
                    int thickness = defaultThickness;
                    line = !srINIFile.EndOfStream ? srINIFile.ReadLine() : null;
                    while ((line != null) && line.Contains("="))
                    {
                        string[] parameter = line.Split('=');
                        if (parameter[0].Trim().ToUpper().Equals(Overlays_ThicknessKeyword))
                        {
                            thickness = int.Parse(parameter[1]);
                        }
                        else if (parameter[0].Trim().ToUpper().Equals(Overlays_ColorKeyword))
                        {
                            string[] rgbStrings = parameter[1].Split(',');
                            if (rgbStrings.Length != 3)
                            {
                                throw new ToolException("Expected three RGB-colors, but found " + rgbStrings.Length + ": " + parameter[1]);
                            }
                            int red = int.Parse(rgbStrings[0]);
                            int green = int.Parse(rgbStrings[1]);
                            int blue = int.Parse(rgbStrings[2]);
                            color = Color.FromArgb(red, green, blue);
                        }
                        line = !srINIFile.EndOfStream ? srINIFile.ReadLine() : null;
                    }
                    GENLegend genLegend = new GENLegend(thickness, color);
                    if (!parameters.IsAddOnce || !imfFile.ContainsOverlay(imodFilename))
                    {
                        imfFile.AddOverlay(new Overlay(genLegend, imodFilename));
                        log.AddInfo("added " + Path.GetFileName(imodFilename), 2);
                    }
                    else
                    {
                        log.AddInfo("File is already added and is skipped: " + Path.GetFileName(imodFilename), 2);
                    }
                }
                else
                {
                    throw new ToolException("Overlay file not found: " + imodFilename);
                }
            }
        }

        private void ParseCrosssectionSection(StreamReader srINIFile, IMFFile imfFile, Log log, INIParameters parameters, ref string line, SIFToolSettings settings)
        {
            Color topLineColor = defaultTOPLineColor;
            Color botLineColor = defaultBOTLineColor;

            log.AddInfo("adding cross sections ...", 1);
            bool isNextLinePeeked;
            line = srINIFile.ReadLine();
            while ((line != null) && line.Contains("="))
            {
                isNextLinePeeked = false;
                string[] lineValues = line.Split('=');
                if (lineValues[0].Trim().Equals(Crosssection_REGISKeyword))
                {
                    string regisDirectory = GetFullPath(lineValues[1].Trim(), parameters.INIPath);
                    if (!Directory.Exists(regisDirectory))
                    {
                        throw new ToolException("REGIS-path not found: " + regisDirectory);
                    }

                    line = srINIFile.ReadLine();
                    isNextLinePeeked = true;
                    lineValues = line.Split('=');
                    if (lineValues[0].Trim().Equals(Crosssection_REGISOrderFileKeyword))
                    {
                        isNextLinePeeked = false;
                        string regisOrderFilename = lineValues[1].Trim();
                        regisOrderFilename = GetFullPath(regisOrderFilename, parameters.INIPath);
                        if (!File.Exists(regisOrderFilename))
                        {
                            throw new ToolException("REGISORDER filename not found: " + regisOrderFilename);
                        }
                        ReadOrderFile(regisOrderFilename, log);

                        line = srINIFile.ReadLine();
                        isNextLinePeeked = true;
                        lineValues = line.Split('=');
                    }

                    Dictionary<string, Color> regisColorDictionary = regisGeohydrologicalColorDictionary;
                    if (lineValues[0].Trim().Equals(Crosssection_REGISColorsKeyword))
                    {
                        isNextLinePeeked = false;
                        if (lineValues[1].Trim().ToUpper().Equals(REGISTNOColorsKeyword))
                        {
                            regisColorDictionary = regisTNOColorDictionary;
                        }
                        else if (lineValues[1].Trim().ToUpper().Equals(REGISAQFColorsKeyword))
                        {
                            regisColorDictionary = regisGeohydrologicalColorDictionary;
                        }
                        else
                        {
                            string regisColorsFilename = GetFullPath(lineValues[1], parameters.INIPath);
                            if (!File.Exists(regisColorsFilename))
                            {
                                throw new ToolException("REGISCOLORS file does not exist: " + regisColorsFilename);
                            }
                            REGISColorDef regisColorDef = REGISColorDef.ReadFile(regisColorsFilename, log);
                            if (regisColorDef != null)
                            {
                                regisColorDictionary = regisColorDef.ColorDictionary;
                            }
                            else
                            {
                                throw new ToolException("Could not read REGISCOLOR file: " + regisColorsFilename);
                            }
                        }
                    }

                    CreateREGIS2DEntry(imfFile, GetFullPath(regisDirectory, parameters.INIPath), parameters, regisColorDictionary, log, settings);

                    log.AddInfo("added REGIS files", 2);
                }

                else if (lineValues[0].Trim().Equals(Crosssection_LayersAsLinesKeyword))
                {
                    string layerDirectoryString = lineValues[1].Trim();

                    line = srINIFile.ReadLine();
                    isNextLinePeeked = true;
                    lineValues = line.Split('=');
                    if (lineValues[0].Trim().Equals(Crosssection_LayersLineColorKeyword))
                    {
                        isNextLinePeeked = false;

                        try
                        {
                            string[] colorStrings = lineValues[1].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                            topLineColor = ParseRGBString(colorStrings[0]);
                            botLineColor = topLineColor;
                            if (colorStrings.Length > 1)
                            {
                                botLineColor = ParseRGBString(colorStrings[1]);
                            }
                        }
                        catch (Exception)
                        {
                            throw new ToolException("Could not parse RGB LINECOLOR for " + Crosssection_LayersLineColorKeyword + ": " + lineValues[1]);
                        }
                    }

                    CreateLayer2DEntry(imfFile, layerDirectoryString, IDFMap.PRFTypeFlag.Line, parameters, topLineColor, botLineColor, settings);
                    log.AddInfo("added model layers", 2);
                }
                else if (lineValues[0].Trim().Equals(Crosssection_LayersAsPlanesKeyword))
                {
                    string layerDirectoryString = lineValues[1].Trim();
                    CreateLayer2DEntry(imfFile, layerDirectoryString, IDFMap.PRFTypeFlag.Fill, parameters, topLineColor, botLineColor, settings);
                }
                else
                {
                    log.AddWarning("Parameter " + lineValues[0] + " is not valid or missing a companion parameter for section [CROSSSECTION]");
                }

                if (!isNextLinePeeked)
                {
                    line = srINIFile.ReadLine();
                }
                else
                {
                    isNextLinePeeked = false;
                }
            }
        }

        private void CreateLayer2DEntry(IMFFile imfFile, string layerDirectoryString, IDFMap.PRFTypeFlag prfTypeFlag, INIParameters parameters, Color topLineColor, Color botLineColor, SIFToolSettings settings)
        {
            List<string> layerFileNames = new List<string>();

            // Retrieve layers files from specified paths
            string[] layerDirectories = layerDirectoryString.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string layerDirectory in layerDirectories)
            {
                string fullLayerDirectory = GetFullPath(layerDirectory, parameters.INIPath);
                if (!Directory.Exists(fullLayerDirectory))
                {
                    throw new ToolException("layers path not found: " + fullLayerDirectory);
                }
                DirectoryInfo directoryLayerModel = new DirectoryInfo(fullLayerDirectory);
                FileInfo[] Files = directoryLayerModel.GetFiles("*.IDF");
                foreach (FileInfo file in Files)
                {
                    if ((file.Name.ToUpper().Contains("TOP") || file.Name.ToUpper().Contains("BOT")) && !file.Name.ToUpper().Contains("SDL"))
                    {
                        layerFileNames.Add(file.FullName);
                    }
                }
            }

            layerFileNames = IMODUtils.SortiMODLayerFilenames(layerFileNames);

            if (prfTypeFlag == IDFMap.PRFTypeFlag.Fill)
            {
                int yellowNumerator = 0;
                int greenNumerator = 0;
                foreach (string filename in layerFileNames)
                {
                    string fullPath = filename;
                    IDFFile idfFile = IDFFile.ReadFile(filename, !settings.IsUpdateIMODFiles);
                    IDFMap layerIDFMap = IDFMap.CreateSurfaceLevelMap("default", idfFile.MinValue, idfFile.MaxValue, fullPath, true);
                    layerIDFMap.SetPRFType(IDFMap.PRFTypeFlag.Active);
                    layerIDFMap.AddPRFTypeFlag(prfTypeFlag);
                    Color layerColor = Color.Gray;
                    if (filename.ToUpper().Contains("TOP"))
                    {
                        layerColor = typesOfYellow[yellowNumerator];
                        yellowNumerator += 1;
                        if (yellowNumerator > 2)
                        {
                            yellowNumerator = 0;
                        }
                    }

                    if (filename.ToUpper().Contains("BOT"))
                    {
                        layerColor = typesOfGreen[greenNumerator];
                        greenNumerator += 1;
                        if (greenNumerator > 4)
                        {
                            greenNumerator = 0;
                        }
                    }
                    layerIDFMap.SColor = IMFFile.Color2Long(layerColor);
                    if (!parameters.IsAddOnce || !imfFile.ContainsMap(layerIDFMap.Filename))
                    {
                        imfFile.AddMap(layerIDFMap);
                    }
                }
            }

            if (prfTypeFlag == IDFMap.PRFTypeFlag.Line)
            {
                //foreach (int layer in layerNumbers)
                //{
                //    string topFile = topString.Replace("XXX", layer.ToString());
                //    filenamesLayerModel.Add(topFile);
                //}

                foreach (string layerFilename in layerFileNames)
                {
                    IDFFile idfFile = IDFFile.ReadFile(layerFilename, !settings.IsUpdateIMODFiles);
                    IDFMap layerIDFMap = IDFMap.CreateSurfaceLevelMap("default", idfFile.MinValue, idfFile.MaxValue, layerFilename, true);
                    layerIDFMap.SetPRFType(IDFMap.PRFTypeFlag.Active);
                    layerIDFMap.AddPRFTypeFlag(prfTypeFlag);
                    if (layerFilename.ToUpper().Contains("BOT"))
                    {
                        layerIDFMap.SColor = IMFFile.Color2Long(botLineColor);
                    }
                    else
                    {
                        layerIDFMap.SColor = IMFFile.Color2Long(topLineColor);
                    }
                    if (!parameters.IsAddOnce || !imfFile.ContainsMap(layerIDFMap.Filename))
                    {
                        imfFile.AddMap(layerIDFMap);
                    }
                }
            }
        }

        private void ParseMapsSection(StreamReader srINIFile, IMFFile imfFile, Log log, INIParameters parameters, ref string line, SIFToolSettings settings)
        {
            // Set defaults
            Color color = Color.Black;
            Color fillColor = Color.Red;
            Color lineColor = Color.Red;
            int thickness = 3;
            int ipfColumn = 3;
            bool IsColumnAdjusted = false;
            bool selected = false;
            string legFilename = null;
            bool IsLineColorAdjusted = false;
            bool IsFillColorAdjusted = false;

            log.AddInfo("adding maps ...", 1);
            string prevExtension = null;
            line = srINIFile.ReadLine();
            while ((line != null) && (line.Trim().Length != 0) && !line.Contains("["))
            {
                line = line.Trim();
                string iMODFilePath = line;
                if (line.ToUpper().StartsWith(Maps_FileKeyword + "="))
                {
                    // If line starts with FILE parse as key-value pair, otherwise the whole line is read as a filename
                    iMODFilePath = line.Split('=')[1];
                }
                if ((prevExtension == null) || !iMODFilePath.EndsWith(prevExtension))
                {
                    // Reset defaults
                    color = Color.Black;
                    fillColor = Color.Red;
                    lineColor = Color.Red;
                    thickness = 3;
                    ipfColumn = 3;
                    IsColumnAdjusted = false;
                    selected = false;
                    legFilename = null;
                    IsLineColorAdjusted = false;
                    IsFillColorAdjusted = false;
                    // prevExtension = 
                }

                if (iMODFilePath.Contains("="))
                {
                    // No Map-files found, rest of reading MAPS-section
                    while ((line != null) && (line.Trim().Length != 0) && !line.Contains("["))
                    {
                        line = srINIFile.ReadLine();
                        line = line.Trim();
                    }
                    return;
                }

                try
                {
                    iMODFilePath = GetFullPath(iMODFilePath, parameters.INIPath);
                }
                catch (Exception ex)
                {
                    throw new ToolException("Invalid iMOD-filename: " + iMODFilePath, ex);
                }

                List<string> iMODFilenames = new List<string>();
                if (Path.GetFileName(iMODFilePath).Contains("*") || Path.GetFileName(iMODFilePath).Contains("?"))
                {
                    string path = Path.GetDirectoryName(iMODFilePath);
                    string filter = Path.GetFileName(iMODFilePath);
                    string[] filenames = Directory.GetFiles(path, filter);
                    foreach (string filename in filenames)
                    {
                        // Skip iMOD metadata files
                        if (!Path.GetExtension(filename).ToUpper().Equals(".MET"))
                        {
                            iMODFilenames.Add(filename);
                        }
                    }
                }
                else if (File.Exists(iMODFilePath))
                {
                    iMODFilenames.Add(iMODFilePath);
                }
                else if (Directory.Exists(iMODFilePath))
                {
                    string[] filenames = Directory.GetFiles(iMODFilePath);
                    foreach (string filename in filenames)
                    {
                        // Skip iMOD metadata files
                        if (!Path.GetExtension(filename).ToUpper().Equals(".MET"))
                        {
                            iMODFilenames.Add(filename);
                        }
                    }
                }
                 CommonUtils.SortAlphanumericStrings(iMODFilenames);

                if (iMODFilenames.Count > 0)
                {
                    line = srINIFile.ReadLine();
                    while ((line != null) && line.Contains("=") && !line.StartsWith(Maps_FileKeyword + "="))
                    {
                        line = line.Trim();
                        string[] parameter = line.Split('=');
                        if (parameter[0].Trim().ToUpper().Equals(Maps_LegendKeyword))
                        {
                            legFilename = GetFullPath(parameter[1], parameters.INIPath);
                            line = srINIFile.ReadLine();
                        }
                        else if (parameter[0].Trim().ToUpper().Equals(Maps_IsSelectedKeyword))
                        {
                            if (int.Parse(parameter[1]) == 1)
                            {
                                selected = true;
                            }
                            else
                            {
                                selected = false;
                            }
                            line = srINIFile.ReadLine();
                        }
                        else if (parameter[0].Trim().ToUpper().Equals(Maps_ThicknessKeyword))
                        {
                            thickness = int.Parse(parameter[1]);
                            line = srINIFile.ReadLine();
                        }
                        else if (parameter[0].Trim().ToUpper().Equals(Maps_ColumnIndexKeyword))
                        {
                            ipfColumn = int.Parse(parameter[1]);
                            IsColumnAdjusted = true;
                            line = srINIFile.ReadLine();
                        }
                        else if (parameter[0].Trim().ToUpper().Equals(Maps_ColorKeyword))
                        {
                            color = ParseRGBString(parameter[1]);
                            line = srINIFile.ReadLine();
                        }
                        else if (parameter[0].Trim().ToUpper().Equals(Maps_FillColorKeyword))
                        {
                            fillColor = ParseRGBString(parameter[1]);
                            IsFillColorAdjusted = true;
                            line = srINIFile.ReadLine();
                        }
                        else if (parameter[0].Trim().ToUpper().Equals(Maps_LineColorKeyword))
                        {
                            lineColor = ParseRGBString(parameter[1]);
                            IsLineColorAdjusted = true;
                            line = srINIFile.ReadLine();
                        }
                        else
                        {
                            log.AddWarning("Parameter " + parameter[0] + " is not a valid parameter in section [MAPS].");
                            line = srINIFile.ReadLine();
                        }
                    }

                    for (int idx = 0; idx < iMODFilenames.Count; idx++)
                    {
                        string iMODFilename = iMODFilenames[idx].Trim();
                        if (iMODFilename.ToUpper().EndsWith(IDFExtension))
                        {
                            prevExtension = IDFExtension;
                            IDFMap idfMap = null;
                            if ((legFilename != null) && (legFilename.Length > 0))
                            {
                                idfMap = CreateIDFMap(legFilename.Trim(), selected, iMODFilename);
                            }
                            else
                            {
                                IDFFile idfFile = IDFFile.ReadFile(iMODFilename, !settings.IsUpdateIMODFiles);
                                idfMap = IDFMap.CreateSurfaceLevelMap("default", idfFile.MinValue, idfFile.MaxValue, iMODFilename, selected);
                            }
                            idfMap.SetPRFType(IDFMap.PRFTypeFlag.Active);
                            if (IsLineColorAdjusted || IsFillColorAdjusted)
                            {
                                if (IsLineColorAdjusted)
                                {
                                    idfMap.AddPRFTypeFlag(IDFMap.PRFTypeFlag.Line);
                                    idfMap.SColor = IMFFile.Color2Long(lineColor);
                                }
                                if (IsFillColorAdjusted)
                                {
                                    idfMap.AddPRFTypeFlag(IDFMap.PRFTypeFlag.Fill);
                                    idfMap.SColor = IMFFile.Color2Long(fillColor);
                                }
                            }
                            else
                            {
                                idfMap.SetPRFType(IDFMap.PRFTypeFlag.Active);
                                idfMap.AddPRFTypeFlag(IDFMap.PRFTypeFlag.Line);
                            }
                            if (!parameters.IsAddOnce || !imfFile.ContainsMap(idfMap.Filename))
                            {
                                imfFile.AddMap(idfMap);
                                log.AddInfo("added " + Path.GetFileName(iMODFilename), 2);
                            }
                            else
                            {
                                log.AddInfo("File is already added and is skipped: " + Path.GetFileName(iMODFilename), 2);
                            }
                        }
                        else if (iMODFilename.ToUpper().EndsWith(GENExtension))
                        {
                            prevExtension = GENExtension;
                            GENLegend genLegend = new GENLegend(thickness, color);
                            if (!parameters.IsAddOnce || !imfFile.ContainsMap(iMODFilename))
                            {
                                imfFile.AddMap(new Map(genLegend, iMODFilename));
                            }
                        }
                        else if (iMODFilename.ToUpper().EndsWith(IPFExtension))
                        {
                            log.AddInfo("added " + Path.GetFileName(iMODFilename), 2);
                            if ((legFilename != null) && (legFilename.Length > 0))
                            {
                                if (!parameters.IsAddOnce || !imfFile.ContainsMap(iMODFilename))
                                {
                                    imfFile.AddMap(CreateIPFMap(legFilename.Trim(), selected, ipfColumn, iMODFilename));
                                    if (!IsColumnAdjusted)
                                    {
                                        log.AddWarning("A legend is defined for the IPF-File, but it is not assigned to a column.");
                                    }
                                }
                            }
                            else
                            {
                                if (!parameters.IsAddOnce || !imfFile.ContainsMap(iMODFilename))
                                {
                                    prevExtension = IPFExtension;
                                    IPFLegend defaultIPFLegend = IPFLegend.CreateLegend("default IPF legend", "All values", Color.Black);
                                    imfFile.AddMap(new IPFMap(defaultIPFLegend, iMODFilename));
                                }
                            }
                        }
                        else
                        {
                            log.AddWarning("File with unknown extension is not added to IMF-file: " + iMODFilename);
                        }
                    }
                }
                else
                {
                    log.AddWarning("File/path not found and skipped: " + iMODFilePath);
                    line = srINIFile.ReadLine();
                }
            }
        }

        private Color ParseRGBString(string rgbString)
        {
            string[] rgbValues = rgbString.Split(',');
            int red = int.Parse(rgbValues[0]);
            int green = int.Parse(rgbValues[1]);
            int blue = int.Parse(rgbValues[2]);
            return Color.FromArgb(red, green, blue);
        }

        private void ParseParametersSection(TextReader srINIFile, IMFFile imfFile, Log log, ref string line, ref INIParameters parameters, SIFToolSettings settings)
        {
            line = srINIFile.ReadLine();
            while ((line != null) && line.Contains("="))
            {
                line = line.Trim();
                string[] parameter = line.Split('=');
                try
                {
                    if (parameter[0].Trim().ToUpper().Equals(Parameter_ExtentKeyword))
                    {
                        if (parameter[1].Length > 2)
                        {
                            string[] extentCorners = parameter[1].Split(',');
                            Extent extent = new Extent(
                                float.Parse(extentCorners[0], englishCultureInfo),
                                float.Parse(extentCorners[1], englishCultureInfo),
                                float.Parse(extentCorners[2], englishCultureInfo),
                                float.Parse(extentCorners[3], englishCultureInfo));
                            parameters.Extent = extent;
                            imfFile.Extent = extent;
                            log.AddInfo("Extent for IMF project is: " + extent.ToString());
                        }
                        else
                        {
                            // leave extent null, which will be set later, when all mapfiles have been read
                            // log.AddWarning("There is no extent defined, you might have to zoom to layer in iMOD to make the layers visible.");
                        }

                    }
                    else if (parameter[0].Trim().ToUpper().Equals(Parameter_OpeniMODKeyword))
                    {
                        if (int.Parse(parameter[1]) == 1)
                        {
                            parameters.IsIMODopened = true;
                        }
                    }
                    else if (parameter[0].Trim().ToUpper().Equals(Parameter_AddOnceKeyword))
                    {
                        if (int.Parse(parameter[1]) == 1)
                        {
                            parameters.IsAddOnce = true;
                        }
                    }
                    else if (parameter[0].Trim().ToUpper().Equals(Parameter_iMODExecutablePathKeyword))
                    {
                        string iMODPath = GetFullPath(parameter[1], parameters.INIPath);
                        if (File.Exists(iMODPath))
                        {
                            parameters.IMODPath = iMODPath;
                        }
                        else
                        {
                            log.AddWarning("Viewing IMF is skipped. Specified iMOD-executable not found: " + iMODPath);
                            parameters.IsIMODopened = false;
                        }
                    }
                    else if (parameter[0].Trim().ToUpper().Equals(Parameter_IMFFilenameKeyword))
                    {
                        parameters.IMFFilename = GetFullPath(parameter[1], parameters.INIPath);
                        if (!Path.HasExtension(parameters.IMFFilename))
                        {
                            parameters.IMFFilename = Path.ChangeExtension(parameters.IMFFilename, "IMF");
                        }
                    }
                    else
                    {
                        throw new ToolException("Parameter " + parameter[0] + " is not a valid parameter");
                    }
                }
                catch (Exception ex)
                {
                    throw new ToolException("Error while parsing keyword " + parameter[0], ex);
                }

                line = srINIFile.ReadLine();
            }
        }

        private string GetFullPath(string path, string basePath)
        {
            if ((path != null) && !path.Equals(string.Empty))
            {
                string filter = null;
                if (Path.GetFileName(path).Contains("*") || Path.GetFileName(path).Contains("?"))
                {
                    filter = Path.GetFileName(path);
                    path = Path.GetDirectoryName(path);
                }

                if (!Path.IsPathRooted(path))
                {
                    path = Path.GetFullPath(Path.Combine(basePath, path));
                }
                if (filter != null)
                {
                    path = Path.Combine(path, filter);
                }

                return path;
            }
            else
            {
                return null;
            }
        }

        private void CreateREGIS2DEntry(IMFFile imfFile, string regisDirectory, INIParameters parameters, Dictionary<string, Color> regisColorDictionary, Log log, SIFToolSettings settings)
        {
            List<string> fileNamesOrdered = ReadAndOrderRegisDirectory(regisDirectory, log);
            foreach (string file in fileNamesOrdered)
            {
                string fullPath = regisDirectory + "\\" + file;
                string[] filenameParts = file.Split('-');
                string regisLayerString = filenameParts[0];
                string topOrbot = filenameParts[1];
                IDFMap regisIDFMap = null;
                if (File.Exists(fullPath))
                {
                    IDFFile idfFile = IDFFile.ReadFile(fullPath, !settings.IsUpdateIMODFiles);
                    bool isEmptyFileSkipped = false; // todo: make option
                    if (isEmptyFileSkipped)
                    {
                        if (idfFile.RetrieveElementCount() > 0)
                        {
                            regisIDFMap = IDFMap.CreateSurfaceLevelMap("default", idfFile.MinValue, idfFile.MaxValue, fullPath, true);
                        }
                    }
                    else
                    {
                        regisIDFMap = IDFMap.CreateSurfaceLevelMap("default", idfFile.MinValue, idfFile.MaxValue, fullPath, true);
                    }
                }
                if (regisIDFMap != null)
                {
                    regisIDFMap.SetPRFType(IDFMap.PRFTypeFlag.Active);
                    Color fillColor = Color.FromArgb(255, 255, 255);
                    if (topOrbot.ToLower().Equals("t"))
                    {
                        fillColor = GetREGISColor(regisLayerString, regisColorDictionary, log);
                        regisIDFMap.AddPRFTypeFlag(IDFMap.PRFTypeFlag.Legend);
                    }
                    regisIDFMap.AddPRFTypeFlag(IDFMap.PRFTypeFlag.Fill);
                    regisIDFMap.SColor = IMFFile.Color2Long(fillColor);
                    if (!parameters.IsAddOnce || !imfFile.ContainsMap(regisIDFMap.Filename))
                    {
                        imfFile.AddMap(regisIDFMap);
                    }
                }
            }
        }

        private static void DoREGISInitialization()
        {
            if (regisEenheden == null)
            {
                regisEenheden = new List<string>(new string[] {"HLc","BXz1","BXSCk1","BXz2",
                "BXLMk1","BXk1","BXz3","BXk2","BXz4","KRz1","KRWYk1","KRz2","KRk1","KRz3","BEz1",
                "BEROk1","BEk1","BEz2","BEk2","BEz3","KWz1","WBv1","EEz1","EEk1","EEz2","EEk2",
                "EEz3","KRZUk1","KRz4","KRTWk1","KRz5","DRz1","DRUIk1","DRz2","DRGIk1","DRz3",
                "DRGIk2","DTc","DNz1","URz1","URk1","URz2","URk2","URz3","PEz1","PEk1","PEz2",
                "PEk2","PEz3","URz4","URk3","URz5","STz1","STk1","STz2","APz1","SYz1","SYk1",
                "SYz2","SYk2","SYz3","SYk3","SYz4","PZWAz1","WAk1","PZWAz2","WAk2","PZk1","PZWAz3",
                "WAk3","PZc","PZWAz4","MSz1","MSk1","MSz2","MSk2","MSz3","MSc","MSz4","KIz1","KIk1",
                "KIz2","KIk2","KIz3","KIk3","KIz4","KIk4","KIz5","OOz1","OOk1","OOz2","OOc","OOz3",
                "IEz1","IEk1","IEz2","IEk2","IEz3","BRz1","BRk1","BRz2","VIb1","BRz3","VIb2","BRz4",
                "VEVOc","RUz1","RUBOk1","RUz2","RUk1","RUz3","RUk2","RUz4","TOz1","TOGOk1","TOz2",
                "TOZEWAk1","TOz3","DOz1","DOASk1","DOz2","DOk1","DOz3","DOIEk1","DOz4","LAc","HTc",
                "HOq","MTq","GUq","VAc","AKc"});

                if (regisGeohydrologicalColorDictionary == null)
                {
                    regisGeohydrologicalColorDictionary = new Dictionary<string, Color> {
                        {"HLc".ToUpper(),Color.FromArgb(0,128,0)},
                        {"BXz1".ToUpper(),Color.FromArgb(255,255,175)},
                        {"BXSCk1".ToUpper(),Color.FromArgb(0,190,0)},
                        {"BXz2".ToUpper(),Color.FromArgb(255,255,125)},
                        {"BXLMk1".ToUpper(),Color.FromArgb(0,225,0)},
                        {"BXk1".ToUpper(),Color.FromArgb(0,128,0)},
                        {"BXz3".ToUpper(),Color.FromArgb(255,255,25)},
                        {"BXk2".ToUpper(),Color.FromArgb(0,190,0)},
                        {"BXz4".ToUpper(),Color.FromArgb(225,225,0)},
                        {"KRz1".ToUpper(),Color.FromArgb(200,200,0)},
                        {"KRWYk1".ToUpper(),Color.FromArgb(0,225,0)},
                        {"KRz2".ToUpper(),Color.FromArgb(150,150,0)},
                        {"KRk1".ToUpper(),Color.FromArgb(0,128,0)},
                        {"KRz3".ToUpper(),Color.FromArgb(255,255,175)},
                        {"BEz1".ToUpper(),Color.FromArgb(255,255,125)},
                        {"BEROk1".ToUpper(),Color.FromArgb(0,190,0)},
                        {"BEk1".ToUpper(),Color.FromArgb(0,225,0)},
                        {"BEz2".ToUpper(),Color.FromArgb(255,255,25)},
                        {"BEk2".ToUpper(),Color.FromArgb(0,128,0)},
                        {"BEz3".ToUpper(),Color.FromArgb(225,225,0)},
                        {"KWz1".ToUpper(),Color.FromArgb(200,200,0)},
                        {"WBv1".ToUpper(),Color.FromArgb(0,190,0)},
                        {"EEz1".ToUpper(),Color.FromArgb(150,150,0)},
                        {"EEk1".ToUpper(),Color.FromArgb(0,225,0)},
                        {"EEz2".ToUpper(),Color.FromArgb(255,255,175)},
                        {"EEk2".ToUpper(),Color.FromArgb(0,128,0)},
                        {"EEz3".ToUpper(),Color.FromArgb(255,255,125)},
                        {"KRZUk1".ToUpper(),Color.FromArgb(0,190,0)},
                        {"KRz4".ToUpper(),Color.FromArgb(255,255,25)},
                        {"KRTWk1".ToUpper(),Color.FromArgb(0,225,0)},
                        {"KRz5".ToUpper(),Color.FromArgb(225,225,0)},
                        {"DRz1".ToUpper(),Color.FromArgb(200,200,0)},
                        {"DRUIk1".ToUpper(),Color.FromArgb(0,128,0)},
                        {"DRz2".ToUpper(),Color.FromArgb(150,150,0)},
                        {"DRGIk1".ToUpper(),Color.FromArgb(0,190,0)},
                        {"DRz3".ToUpper(),Color.FromArgb(255,255,175)},
                        {"DRGIk2".ToUpper(),Color.FromArgb(0,225,0)},
                        {"DTc".ToUpper(),Color.FromArgb(0,128,0)},
                        {"DNz1".ToUpper(),Color.FromArgb(255,255,125)},
                        {"URz1".ToUpper(),Color.FromArgb(255,255,25)},
                        {"URk1".ToUpper(),Color.FromArgb(0,190,0)},
                        {"URz2".ToUpper(),Color.FromArgb(225,225,0)},
                        {"URk2".ToUpper(),Color.FromArgb(0,225,0)},
                        {"URz3".ToUpper(),Color.FromArgb(200,200,0)},
                        {"PEz1".ToUpper(),Color.FromArgb(150,150,0)},
                        {"PEk1".ToUpper(),Color.FromArgb(0,128,0)},
                        {"PEz2".ToUpper(),Color.FromArgb(255,255,175)},
                        {"PEk2".ToUpper(),Color.FromArgb(0,190,0)},
                        {"PEz3".ToUpper(),Color.FromArgb(255,255,125)},
                        {"URz4".ToUpper(),Color.FromArgb(255,255,25)},
                        {"URk3".ToUpper(),Color.FromArgb(0,225,0)},
                        {"URz5".ToUpper(),Color.FromArgb(225,225,0)},
                        {"STz1".ToUpper(),Color.FromArgb(200,200,0)},
                        {"STk1".ToUpper(),Color.FromArgb(0,128,0)},
                        {"STz2".ToUpper(),Color.FromArgb(150,150,0)},
                        {"APz1".ToUpper(),Color.FromArgb(255,255,175)},
                        {"SYz1".ToUpper(),Color.FromArgb(255,255,125)},
                        {"SYk1".ToUpper(),Color.FromArgb(0,190,0)},
                        {"SYz2".ToUpper(),Color.FromArgb(255,255,25)},
                        {"SYk2".ToUpper(),Color.FromArgb(0,225,0)},
                        {"SYz3".ToUpper(),Color.FromArgb(225,225,0)},
                        {"SYk3".ToUpper(),Color.FromArgb(0,128,0)},
                        {"SYz4".ToUpper(),Color.FromArgb(200,200,0)},
                        {"PZWAz1".ToUpper(),Color.FromArgb(150,150,0)},
                        {"WAk1".ToUpper(),Color.FromArgb(0,190,0)},
                        {"PZWAz2".ToUpper(),Color.FromArgb(255,255,175)},
                        {"WAk2".ToUpper(),Color.FromArgb(0,225,0)},
                        {"PZk1".ToUpper(),Color.FromArgb(0,128,0)},
                        {"PZWAz3".ToUpper(),Color.FromArgb(255,255,125)},
                        {"WAk3".ToUpper(),Color.FromArgb(0,190,0)},
                        {"PZc".ToUpper(),Color.FromArgb(0,225,0)},
                        {"PZWAz4".ToUpper(),Color.FromArgb(255,255,25)},
                        {"MSz1".ToUpper(),Color.FromArgb(225,225,0)},
                        {"MSk1".ToUpper(),Color.FromArgb(0,128,0)},
                        {"MSz2".ToUpper(),Color.FromArgb(200,200,0)},
                        {"MSk2".ToUpper(),Color.FromArgb(0,190,0)},
                        {"MSz3".ToUpper(),Color.FromArgb(150,150,0)},
                        {"MSc".ToUpper(),Color.FromArgb(0,225,0)},
                        {"MSz4".ToUpper(),Color.FromArgb(255,255,175)},
                        {"KIz1".ToUpper(),Color.FromArgb(255,255,125)},
                        {"KIk1".ToUpper(),Color.FromArgb(0,128,0)},
                        {"KIz2".ToUpper(),Color.FromArgb(255,255,25)},
                        {"KIk2".ToUpper(),Color.FromArgb(0,190,0)},
                        {"KIz3".ToUpper(),Color.FromArgb(225,225,0)},
                        {"KIk3".ToUpper(),Color.FromArgb(0,225,0)},
                        {"KIz4".ToUpper(),Color.FromArgb(200,200,0)},
                        {"KIk4".ToUpper(),Color.FromArgb(0,128,0)},
                        {"KIz5".ToUpper(),Color.FromArgb(150,150,0)},
                        {"OOz1".ToUpper(),Color.FromArgb(255,255,175)},
                        {"OOk1".ToUpper(),Color.FromArgb(0,190,0)},
                        {"OOz2".ToUpper(),Color.FromArgb(255,255,125)},
                        {"OOc".ToUpper(),Color.FromArgb(0,225,0)},
                        {"OOz3".ToUpper(),Color.FromArgb(255,255,25)},
                        {"IEz1".ToUpper(),Color.FromArgb(225,225,0)},
                        {"IEk1".ToUpper(),Color.FromArgb(0,128,0)},
                        {"IEz2".ToUpper(),Color.FromArgb(200,200,0)},
                        {"IEk2".ToUpper(),Color.FromArgb(0,190,0)},
                        {"IEz3".ToUpper(),Color.FromArgb(150,150,0)},
                        {"BRz1".ToUpper(),Color.FromArgb(255,255,175)},
                        {"BRk1".ToUpper(),Color.FromArgb(0,128,0)},
                        {"BRz2".ToUpper(),Color.FromArgb(255,255,125)},
                        {"VIb1".ToUpper(),Color.FromArgb(139,69,19)},
                        {"BRz3".ToUpper(),Color.FromArgb(255,255,25)},
                        {"VIb2".ToUpper(),Color.FromArgb(139,69,19)},
                        {"BRz4".ToUpper(),Color.FromArgb(225,225,0)},
                        {"VEVOc".ToUpper(),Color.FromArgb(0,128,0)},
                        {"RUz1".ToUpper(),Color.FromArgb(200,200,0)},
                        {"RUBOk1".ToUpper(),Color.FromArgb(0,190,0)},
                        {"RUz2".ToUpper(),Color.FromArgb(150,150,0)},
                        {"RUk1".ToUpper(),Color.FromArgb(0,225,0)},
                        {"RUz3".ToUpper(),Color.FromArgb(255,255,175)},
                        {"RUk2".ToUpper(),Color.FromArgb(0,128,0)},
                        {"RUz4".ToUpper(),Color.FromArgb(255,255,125)},
                        {"TOz1".ToUpper(),Color.FromArgb(255,255,25)},
                        {"TOGOk1".ToUpper(),Color.FromArgb(0,190,0)},
                        {"TOz2".ToUpper(),Color.FromArgb(225,225,0)},
                        {"TOZEWAk1".ToUpper(),Color.FromArgb(0,225,0)},
                        {"TOz3".ToUpper(),Color.FromArgb(200,200,0)},
                        {"DOz1".ToUpper(),Color.FromArgb(150,150,0)},
                        {"DOASk1".ToUpper(),Color.FromArgb(0,128,0)},
                        {"DOz2".ToUpper(),Color.FromArgb(255,255,175)},
                        {"DOk1".ToUpper(),Color.FromArgb(0,190,0)},
                        {"DOz3".ToUpper(),Color.FromArgb(255,255,125)},
                        {"DOIEk1".ToUpper(),Color.FromArgb(0,225,0)},
                        {"DOz4".ToUpper(),Color.FromArgb(255,255,25)},
                        {"LAc".ToUpper(),Color.FromArgb(0,128,0)},
                        {"HTc".ToUpper(),Color.FromArgb(0,190,0)},
                        {"HOq".ToUpper(),Color.FromArgb(200,200,200)},
                        {"MTq".ToUpper(),Color.FromArgb(150,150,150)},
                        {"GUq".ToUpper(),Color.FromArgb(100,100,100)},
                        {"VAc".ToUpper(),Color.FromArgb(0,225,0)},
                        {"AKc".ToUpper(),Color.FromArgb(0,128,0)}};
                }

                if (regisTNOColorDictionary == null)
                {
                    regisTNOColorDictionary = new Dictionary<string, Color> {
                        {"HLc".ToUpper(),Color.FromArgb(12,129,12)},
                        {"BXz1".ToUpper(),Color.FromArgb(255,235,0)},
                        {"BXSCk1".ToUpper(),Color.FromArgb(215,175,0)},
                        {"BXz2".ToUpper(),Color.FromArgb(255,235,0)},
                        {"BXLMk1".ToUpper(),Color.FromArgb(255,190,0)},
                        {"BXk1".ToUpper(),Color.FromArgb(255,190,0)},
                        {"BXz3".ToUpper(),Color.FromArgb(255,235,0)},
                        {"BXk2".ToUpper(),Color.FromArgb(215,175,0)},
                        {"BXz4".ToUpper(),Color.FromArgb(255,235,0)},
                        {"KRz1".ToUpper(),Color.FromArgb(176,48,96)},
                        {"KRWYk1".ToUpper(),Color.FromArgb(86,0,0)},
                        {"KRz2".ToUpper(),Color.FromArgb(176,48,96)},
                        {"KRk1".ToUpper(),Color.FromArgb(111,0,0)},
                        {"KRz3".ToUpper(),Color.FromArgb(176,48,96)},
                        {"BEz1".ToUpper(),Color.FromArgb(200,200,255)},
                        {"BEROk1".ToUpper(),Color.FromArgb(160,140,155)},
                        {"BEk1".ToUpper(),Color.FromArgb(170,155,180)},
                        {"BEz2".ToUpper(),Color.FromArgb(200,200,255)},
                        {"BEk2".ToUpper(),Color.FromArgb(180,170,205)},
                        {"BEz3".ToUpper(),Color.FromArgb(200,200,255)},
                        {"KWz1".ToUpper(),Color.FromArgb(172,169,43)},
                        {"WBv1".ToUpper(),Color.FromArgb(117,37,0)},
                        {"EEz1".ToUpper(),Color.FromArgb(190,255,115)},
                        {"EEk1".ToUpper(),Color.FromArgb(135,210,40)},
                        {"EEz2".ToUpper(),Color.FromArgb(190,255,115)},
                        {"EEk2".ToUpper(),Color.FromArgb(145,225,65)},
                        {"EEz3".ToUpper(),Color.FromArgb(190,255,115)},
                        {"KRZUk1".ToUpper(),Color.FromArgb(136,0,0)},
                        {"KRz4".ToUpper(),Color.FromArgb(176,48,96)},
                        {"KRTWk1".ToUpper(),Color.FromArgb(156,18,46)},
                        {"KRz5".ToUpper(),Color.FromArgb(176,48,96)},
                        {"DRz1".ToUpper(),Color.FromArgb(255,127,80)},
                        {"DRUIk1".ToUpper(),Color.FromArgb(225,82,5)},
                        {"DRz2".ToUpper(),Color.FromArgb(255,127,80)},
                        {"DRGIk1".ToUpper(),Color.FromArgb(235,97,30)},
                        {"DRz3".ToUpper(),Color.FromArgb(255,127,80)},
                        {"DRGIk2".ToUpper(),Color.FromArgb(235,97,30)},
                        {"DTc".ToUpper(),Color.FromArgb(156,156,156)},
                        {"DNz1".ToUpper(),Color.FromArgb(250,250,210)},
                        {"URz1".ToUpper(),Color.FromArgb(189,183,107)},
                        {"URk1".ToUpper(),Color.FromArgb(149,123,7)},
                        {"URz2".ToUpper(),Color.FromArgb(189,183,107)},
                        {"URk2".ToUpper(),Color.FromArgb(159,138,32)},
                        {"URz3".ToUpper(),Color.FromArgb(189,183,107)},
                        {"PEz1".ToUpper(),Color.FromArgb(238,130,238)},
                        {"PEk1".ToUpper(),Color.FromArgb(208,85,163)},
                        {"PEz2".ToUpper(),Color.FromArgb(238,130,238)},
                        {"PEk2".ToUpper(),Color.FromArgb(218,100,188)},
                        {"PEz3".ToUpper(),Color.FromArgb(238,130,238)},
                        {"URz4".ToUpper(),Color.FromArgb(189,183,107)},
                        {"URk3".ToUpper(),Color.FromArgb(169,153,57)},
                        {"URz5".ToUpper(),Color.FromArgb(189,183,107)},
                        {"STz1".ToUpper(),Color.FromArgb(205,92,92)},
                        {"STk1".ToUpper(),Color.FromArgb(185,62,42)},
                        {"STz2".ToUpper(),Color.FromArgb(205,92,92)},
                        {"APz1".ToUpper(),Color.FromArgb(218,165,32)},
                        {"SYz1".ToUpper(),Color.FromArgb(255,228,181)},
                        {"SYk1".ToUpper(),Color.FromArgb(215,168,81)},
                        {"SYz2".ToUpper(),Color.FromArgb(255,228,181)},
                        {"SYk2".ToUpper(),Color.FromArgb(225,183,106)},
                        {"SYz3".ToUpper(),Color.FromArgb(255,228,181)},
                        {"SYk3".ToUpper(),Color.FromArgb(235,198,131)},
                        {"SYz4".ToUpper(),Color.FromArgb(255,228,181)},
                        {"PZWAz1".ToUpper(),Color.FromArgb(255,204,0)},
                        {"WAk1".ToUpper(),Color.FromArgb(215,105,0)},
                        {"PZWAz2".ToUpper(),Color.FromArgb(255,204,0)},
                        {"WAk2".ToUpper(),Color.FromArgb(225,120,0)},
                        {"PZk1".ToUpper(),Color.FromArgb(205,180,0)},
                        {"PZWAz3".ToUpper(),Color.FromArgb(255,204,0)},
                        {"WAk3".ToUpper(),Color.FromArgb(235,135,0)},
                        {"PZc".ToUpper(),Color.FromArgb(235,225,0)},
                        {"PZWAz4".ToUpper(),Color.FromArgb(255,204,0)},
                        {"MSz1".ToUpper(),Color.FromArgb(135,206,135)},
                        {"MSk1".ToUpper(),Color.FromArgb(105,161,160)},
                        {"MSz2".ToUpper(),Color.FromArgb(135,206,135)},
                        {"MSk2".ToUpper(),Color.FromArgb(115,176,185)},
                        {"MSz3".ToUpper(),Color.FromArgb(135,206,135)},
                        {"MSc".ToUpper(),Color.FromArgb(105,161,160)},
                        {"MSz4".ToUpper(),Color.FromArgb(135,206,135)},
                        {"KIz1".ToUpper(),Color.FromArgb(188,143,143)},
                        {"KIk1".ToUpper(),Color.FromArgb(138,93,68)},
                        {"KIz2".ToUpper(),Color.FromArgb(188,143,143)},
                        {"KIk2".ToUpper(),Color.FromArgb(148,103,83)},
                        {"KIz3".ToUpper(),Color.FromArgb(188,143,143)},
                        {"KIk3".ToUpper(),Color.FromArgb(158,113,98)},
                        {"KIz4".ToUpper(),Color.FromArgb(188,143,143)},
                        {"KIk4".ToUpper(),Color.FromArgb(168,123,113)},
                        {"KIz5".ToUpper(),Color.FromArgb(188,143,143)},
                        {"OOz1".ToUpper(),Color.FromArgb(118,157,39)},
                        {"OOk1".ToUpper(),Color.FromArgb(88,112,0)},
                        {"OOz2".ToUpper(),Color.FromArgb(118,157,39)},
                        {"OOc".ToUpper(),Color.FromArgb(88,112,0)},
                        {"OOz3".ToUpper(),Color.FromArgb(118,157,39)},
                        {"IEz1".ToUpper(),Color.FromArgb(236,121,193)},
                        {"IEk1".ToUpper(),Color.FromArgb(206,76,118)},
                        {"IEz2".ToUpper(),Color.FromArgb(236,121,193)},
                        {"IEk2".ToUpper(),Color.FromArgb(216,91,143)},
                        {"IEz3".ToUpper(),Color.FromArgb(236,121,193)},
                        {"BRz1".ToUpper(),Color.FromArgb(108,188,150)},
                        {"BRk1".ToUpper(),Color.FromArgb(88,158,100)},
                        {"BRz2".ToUpper(),Color.FromArgb(108,188,150)},
                        {"VIb1".ToUpper(),Color.FromArgb(123,57,0)},
                        {"BRz3".ToUpper(),Color.FromArgb(108,188,150)},
                        {"VIb2".ToUpper(),Color.FromArgb(133,72,0)},
                        {"BRz4".ToUpper(),Color.FromArgb(108,188,150)},
                        {"VEVOc".ToUpper(),Color.FromArgb(92,85,0)},
                        {"RUz1".ToUpper(),Color.FromArgb(184,123,184)},
                        {"RUBOk1".ToUpper(),Color.FromArgb(144,63,138)},
                        {"RUz2".ToUpper(),Color.FromArgb(184,123,184)},
                        {"RUk1".ToUpper(),Color.FromArgb(154,78,163)},
                        {"RUz3".ToUpper(),Color.FromArgb(184,123,184)},
                        {"RUk2".ToUpper(),Color.FromArgb(164,93,188)},
                        {"RUz4".ToUpper(),Color.FromArgb(184,123,184)},
                        {"TOz1".ToUpper(),Color.FromArgb(90,159,219)},
                        {"TOGOk1".ToUpper(),Color.FromArgb(60,114,144)},
                        {"TOz2".ToUpper(),Color.FromArgb(90,159,219)},
                        {"TOZEWAk1".ToUpper(),Color.FromArgb(70,129,169)},
                        {"TOz3".ToUpper(),Color.FromArgb(90,159,219)},
                        {"DOz1".ToUpper(),Color.FromArgb(216,191,216)},
                        {"DOASk1".ToUpper(),Color.FromArgb(176,131,116)},
                        {"DOz2".ToUpper(),Color.FromArgb(216,191,216)},
                        {"DOk1".ToUpper(),Color.FromArgb(186,146,141)},
                        {"DOz3".ToUpper(),Color.FromArgb(216,191,216)},
                        {"DOIEk1".ToUpper(),Color.FromArgb(196,161,166)},
                        {"DOz4".ToUpper(),Color.FromArgb(216,191,216)},
                        {"LAc".ToUpper(),Color.FromArgb(208,32,144)},
                        {"HTc".ToUpper(),Color.FromArgb(178,34,34)},
                        {"HOq".ToUpper(),Color.FromArgb(210,105,30)},
                        {"MTq".ToUpper(),Color.FromArgb(255,160,102)},
                        {"GUq".ToUpper(),Color.FromArgb(245,222,179)},
                        {"VAc".ToUpper(),Color.FromArgb(21,153,79)},
                        {"AKc".ToUpper(),Color.FromArgb(152,231,205)}};
                }

                // Check all regisEenheden are present in colordictionary
                foreach (string regisEenheid in regisEenheden)
                {
                    if (!regisTNOColorDictionary.ContainsKey(regisEenheid.ToUpper()))
                    {
                        throw new Exception("Defined REGIS-eenheden doesn't match with TNO REGIS-colors: " + regisEenheid);
                    }

                    if (!regisGeohydrologicalColorDictionary.ContainsKey(regisEenheid.ToUpper()))
                    {
                        throw new Exception("Defined REGIS-eenheden doesn't match with geohydrological REGIS-colors: " + regisEenheid);
                    }
                }
            }

        }

        public static void StartIMOD(string iMODExecutablePath, string imfFilename, Log log, int indentLevel = 0)
        {
            string outputString;
            if (!Path.IsPathRooted(iMODExecutablePath))
            {
                iMODExecutablePath = Path.Combine(Directory.GetCurrentDirectory(), iMODExecutablePath);
            }

            if (File.Exists(iMODExecutablePath))
            {
                if (log != null)
                {
                    log.AddInfo("Starting iMOD...", indentLevel);
                    log.AddInfo("\"" + iMODExecutablePath + "\" \"" + imfFilename + "\"", indentLevel + 1);
                }

                try
                {
                    iMODExecutablePath = Path.GetFullPath(iMODExecutablePath);
                    string command = Path.GetFileName(iMODExecutablePath) + " \"" + imfFilename + " \"";
                    int exitCode = CommonUtils.ExecuteCommand(command, -1, out outputString, Path.GetDirectoryName(iMODExecutablePath));
                    if ((log != null) && (outputString.Length > 0))
                    {
                        log.AddInfo(outputString);
                    }
                    else if (exitCode != 0)
                    {
                        log.AddInfo("Could not start iMOD, exitcode: " + exitCode);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while starting iMOD", ex);
                }
            }
            else
            {
                if (log != null)
                {
                    log.AddWarning("Specified iMOD-executable does not exist: " + iMODExecutablePath);
                    log.AddInfo("Current directory is: " + Directory.GetCurrentDirectory(), 1);
                }
            }
        }

        public Color GetREGISColor(string regisLayerString, Dictionary<string, Color> regisColorDictionary, Log log)
        {
            Color regisColor = Color.Gray;
            if (regisColorDictionary.ContainsKey(regisLayerString.ToUpper()))
            {
                regisColor = regisColorDictionary[regisLayerString.ToUpper()];
            }
            else
            {
                // If no direct match was found, check if the layer starts with some REGIS-colorkey
                bool isFound = false;
                foreach (string key in regisColorDictionary.Keys)
                {
                    if (regisLayerString.ToUpper().StartsWith(key))
                    {
                        regisColor = regisColorDictionary[key];
                        isFound = true;
                        break;
                    }
                }

                if (!isFound)
                {
                    log.AddWarning("REGIS string not found in color definitions: " + regisLayerString);
                }
            }
            return regisColor;
        }

        public List<string> ReadAndOrderRegisDirectory(string regisPath, Log log)
        {
            DirectoryInfo regisDirectory = new DirectoryInfo(regisPath);
            FileInfo[] Files = regisDirectory.GetFiles("*.IDF");
            List<string> fileNames = new List<string>();
            foreach (FileInfo file in Files)
            {
                fileNames.Add(file.Name);
            }

            List<string> regisUnitsOrdered = regisEenheden;
            if (layerOrderPrefixIdxDictionary != null)
            {
                regisUnitsOrdered = new List<string>();
                foreach (string prefix in layerOrderPrefixIdxDictionary.Keys)
                {
                    regisUnitsOrdered.Add(prefix);
                }
            }

            List<string> fileNamesOrdered = new List<string>();
            foreach (string regisEenheid in regisUnitsOrdered)
            {
                foreach (string fileName in fileNames)
                {
                    string[] filenameParts = fileName.Split('-');
                    if (filenameParts[0].ToUpper().StartsWith(regisEenheid.ToUpper()))
                    {
                        if (filenameParts[1].ToLower().Equals("t"))
                        {
                            fileNamesOrdered.Add(fileName);
                            string bottomFilename = fileName.Replace("-t-", "-b-");
                            if (filenameParts[1].Equals("T"))
                            {
                                bottomFilename = fileName.Replace("-T-", "-B-");
                            }
                            if (File.Exists(Path.Combine(regisPath, bottomFilename)))
                            {
                                fileNamesOrdered.Add(bottomFilename);
                            }
                            else
                            {
                                // Skip bottomfile
                                log.AddWarning("Missing bottomfile: " + bottomFilename);
                            }
                        }
                    }
                }
            }
            return fileNamesOrdered;
        }

        protected void ReadOrderFile(string orderTextFilename, Log log)
        {
            layerOrderPrefixIdxDictionary = new Dictionary<string, int>();
            layerOrderIdxPrefixDictionary = new Dictionary<int, string>();

            Stream stream = null;
            StreamReader sr = null;
            try
            {
                stream = File.Open(orderTextFilename, FileMode.Open, FileAccess.Read);
                sr = new StreamReader(stream);
                int lineCount = 0;
                string wholeLine = null;

                // Skip commment lines
                while ((!sr.EndOfStream) && ((wholeLine == null) || (wholeLine.Trim().StartsWith("#"))))
                {
                    wholeLine = sr.ReadLine();
                    lineCount++;
                }

                // Read last part of each line as the layer prefix
                int layerIdx = 0;
                while (wholeLine != null)
                {
                    string[] lineValues = wholeLine.Trim().Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    //if (!int.TryParse(lineValues[0], out layerIdx))
                    //{
                    //    throw new ToolException("Could not parse layer index in line " + lineCount + ": " + lineValues[0]);
                    //}
                    string layerprefix = lineValues[lineValues.Length - 1].ToLower();
                    layerOrderPrefixIdxDictionary.Add(layerprefix, layerIdx);
                    layerOrderIdxPrefixDictionary.Add(layerIdx, layerprefix);

                    if (!sr.EndOfStream)
                    {
                        wholeLine = sr.ReadLine();
                        lineCount++;
                        layerIdx++;
                    }
                    else
                    {
                        wholeLine = null;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not read order textfile: " + orderTextFilename, ex);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
            }
        }

        public static IDFMap CreateIDFMap(string legFilename, bool isSelected, string idfFilename = null, string legendDescription = "IDF legend")
        {
            IDFMap idfMap = new IDFMap(legendDescription, isSelected);
            idfMap.IDFLegend.ImportClasses(legFilename);
            idfMap.Filename = idfFilename;
            return idfMap;
        }

        public static IPFMap CreateIPFMap(string legFilename, bool isSelected, int columnIndex, string ipfFilename = null, string legendDescription = "IPF legend")
        {
            IPFMap ipfMap = new IPFMap(legendDescription, isSelected);
            ipfMap.IPFLegend.ImportClasses(legFilename);
            ipfMap.IPFLegend.ColumnIndex = columnIndex;
            ipfMap.Filename = ipfFilename;
            return ipfMap;
        }

    }
}
