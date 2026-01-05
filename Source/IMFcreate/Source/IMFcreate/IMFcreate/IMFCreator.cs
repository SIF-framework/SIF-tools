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
using Sweco.SIF.IMFcreate.REGISVersions;
using Sweco.SIF.iMOD.DLF;
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
        private const float IMFXYRatio = 1.85f;
        private const string DefaultIMFFilename = "IMFcreate.IMF";
        private const string ParameterSectionKeyword = "[PARAMETERS]";
        private const string Parameter_ExtentKeyword = "EXTENT";
        private const string Parameter_OpeniMODKeyword = "OPENIMOD";
        private const string Parameter_AddOnceKeyword = "ADDONCE";
        private const string Parameter_iMODExecutablePathKeyword = "IMODEXE";
        private const string Parameter_IMFFilenameKeyword = "IMFFILENAME";
        private const string MapsSectionKeyword = "[MAPS]";
        private const string Maps_FileKeyword = "FILE";
        private const string Maps_LegendKeyword = "LEGEND";
        private const string Maps_AliasKeyword = "ALIAS";
        private const string Maps_DLFLegendKeyword = "CSLEGEND";
        private const string Maps_IsSelectedKeyword = "SELECTED";
        private const string Maps_ColumnNumberKeyword = "COLUMN";
        private const string Maps_TextSizeKeyword = "TEXTSIZE";
        private const string Maps_ThicknessKeyword = "THICKNESS";
        private const string Maps_ColorKeyword = "COLOR";
        private const string Maps_LineColorKeyword = "LINECOLOR";
        private const string Maps_FillColorKeyword = "FILLCOLOR";
        private const string Maps_PRFTypeKeyword = "PRFTYPE";

        private const string CrosssectionSectionKeyword = "[CROSSSECTION]";
        private const string Crosssection_REGISKeyword = "REGIS";
        private const string Crosssection_REGISColorsKeyword = "REGISCOLORS";
        private const string Crosssection_REGISOrderFileKeyword = "REGISORDER";
        private const string Crosssection_LayersAsLinesKeyword = "LAYERSASLINES";
        private const string Crosssection_LayersLineColorKeyword = "LINECOLOR";
        private const string Crosssection_LayersAsPlanesKeyword = "LAYERSASPLANES";
        private const string IDFExtension = ".IDF";
        private const string GENExtension = ".GEN";
        private const string IFFExtension = ".IFF";
        private const string IPFExtension = ".IPF";
        private const string IMFExtension = ".IMF";
        private const string OverlaysSectionKeyword = "[OVERLAYS]";
        private const string Overlays_ThicknessKeyword = "THICKNESS";
        private const string Overlays_ColorKeyword = "COLOR";
        private const string Overlays_SelectedKeyword = "SELECTED";
        private const string REGISTNOColorsKeyword = "TNO"; // TNO REGIS-colors
        private const string REGISAQFColorsKeyword = "AQF"; // Yellow/Green-colors for aquifers/aquitards
        private const int DefaultThickness = 3;
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
                throw new Exception("Unexpected error when reading INI-file: " + Path.GetFileName(inputFile), ex);
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
                imfFile.Extent = imfFile.Extent.Enlarge(0.15f);
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
                line = line.Trim();
                string currentFilename = null;

                // Rettrieve full path of GEN-filename
                try
                {
                    currentFilename = GetFullPath(line.Replace("\"", string.Empty).Trim(), parameters.INIPath);
                }
                catch (Exception ex)
                {
                    throw new ToolException("Invalid filename: " + line, ex);
                }

                // Check if filename contains wildcards. If so, retrieves all corresponding filenames in current path
                List<string> iMODFilenames = ExpandFilename(currentFilename);
                CommonUtils.SortAlphanumericStrings(iMODFilenames);

                // Parse properties
                Color color = Color.Black;
                int thickness = DefaultThickness;
                bool selected = true;
                line = !srINIFile.EndOfStream ? srINIFile.ReadLine() : null;
                while ((line != null) && line.Contains("="))
                {
                    string[] parameter = line.Split('=');
                    if (parameter[0].Trim().ToUpper().Equals(Overlays_ThicknessKeyword))
                    {
                        thickness = int.Parse(parameter[1]);
                    }
                    else if (parameter[0].Trim().ToUpper().Equals(Overlays_SelectedKeyword))
                    {
                        try
                        {
                            selected = (int.Parse(parameter[1]) == 1);
                        }
                        catch (Exception ex)
                        {
                            throw new ToolException("Could not parse " + Maps_IsSelectedKeyword + "-value: " + parameter[1], ex);
                        }
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

                foreach (string genFilename in iMODFilenames)
                {
                    if (File.Exists(genFilename))
                    {
                        GENLegend genLegend = new GENLegend(thickness, color);
                        if (!parameters.IsAddOnce || !imfFile.ContainsOverlay(genFilename))
                        {
                            genLegend.Selected = selected;
                            imfFile.AddOverlay(new Overlay(genLegend, genFilename));
                            log.AddInfo("added " + Path.GetFileName(genFilename), 2);
                        }
                        else
                        {
                            log.AddInfo("File is already added and is skipped: " + Path.GetFileName(genFilename), 2);
                        }
                    }
                    else
                    {
                        log.AddWarning("Overlay file/filter not found and skipped: " + genFilename, 2);
                        line = !srINIFile.EndOfStream ? srINIFile.ReadLine() : null;
                    }
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
                line = line.Trim();
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
                        log.AddInfo("reading REGISORDER-file '" + Path.GetFileName(regisOrderFilename) + "' ...", 2);
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

                            log.AddInfo("reading REGISCOLOR-file ...", 2);
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

                    log.AddInfo("adding REGIS-files ... ", 2, false);
                    int fileCount = CreateREGIS2DEntry(imfFile, GetFullPath(regisDirectory, parameters.INIPath), parameters, regisColorDictionary, log, settings);
                    log.AddInfo(fileCount.ToString() + " found");
                    if (fileCount == 0)
                    {
                        log.AddWarning("No REGIS-files found, check input path: " + regisDirectory);
                    }
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

                    log.AddInfo("adding Layermodel-files (as lines) ... ", 2, false);
                    int fileCount = CreateLayer2DEntry(imfFile, layerDirectoryString, IDFMap.PRFTypeFlag.Line, parameters, topLineColor, botLineColor, settings);
                    log.AddInfo(fileCount.ToString() + " found");
                    if (fileCount == 0)
                    {
                        log.AddWarning("No Layermodel-files found, check config XML-file and input path(s): " + layerDirectoryString);
                    }
                }
                else if (lineValues[0].Trim().Equals(Crosssection_LayersAsPlanesKeyword))
                {
                    string layerDirectoryString = lineValues[1].Trim();

                    log.AddInfo("adding Layermodel-files (as planes) ... ", 2, false);
                    int fileCount = CreateLayer2DEntry(imfFile, layerDirectoryString, IDFMap.PRFTypeFlag.Fill, parameters, topLineColor, botLineColor, settings);
                    log.AddInfo(fileCount.ToString() + " found");
                    if (fileCount == 0)
                    {
                        log.AddWarning("No Layermodel-files found, check config XML-file and input path(s): " + layerDirectoryString);
                    }
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

        private int CreateLayer2DEntry(IMFFile imfFile, string layerDirectoryString, IDFMap.PRFTypeFlag prfTypeFlag, INIParameters parameters, Color topLineColor, Color botLineColor, SIFToolSettings settings)
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
                    if ((file.Name.ToUpper().Contains(Properties.Settings.Default.TOPFilePrefix) || file.Name.ToUpper().Contains(Properties.Settings.Default.BOTFilePrefix)) && !file.Name.ToUpper().Contains(Properties.Settings.Default.SDLFilePrefix))
                    {
                        if (!layerFileNames.Contains(file.FullName))
                        {
                            layerFileNames.Add(file.FullName);
                        }
                    }
                }
            }

            // When TOPFilePrefix and BOTFilePrefix are TOP and BOT as expected in most iMOD-models, use the default layerNrPrefixes, otherwise define layerNrPrefixes explicitly
            List<string> layerNrPrefixes = null;
            if (!Properties.Settings.Default.TOPFilePrefix.ToUpper().Equals("TOP") && !Properties.Settings.Default.BOTFilePrefix.ToUpper().Equals("BOT"))
            {
                layerNrPrefixes = new List<string>() { Properties.Settings.Default.TOPFilePrefix, Properties.Settings.Default.BOTFilePrefix };
            }
            layerFileNames = IMODUtils.SortiMODLayerFilenames(layerFileNames, Properties.Settings.Default.TOPFilePrefix, Properties.Settings.Default.BOTFilePrefix, layerNrPrefixes);

            if (prfTypeFlag == IDFMap.PRFTypeFlag.Fill)
            {
                int yellowNumerator = 0;
                int greenNumerator = 0;
                foreach (string fullFilename in layerFileNames)
                {
                    IDFFile idfFile = IDFFile.ReadFile(fullFilename, !settings.IsUpdateIMODFiles);
                    IDFMap layerIDFMap = IDFMap.CreateSurfaceLevelMap("default", idfFile.MinValue, idfFile.MaxValue, fullFilename, true);
                    layerIDFMap.SetPRFType(IDFMap.PRFTypeFlag.Active);
                    layerIDFMap.AddPRFTypeFlag(prfTypeFlag);
                    Color layerColor = Color.Gray;

                    string filename = Path.GetFileName(fullFilename);
                    if (filename.ToUpper().Contains(Properties.Settings.Default.TOPFilePrefix))
                    {
                        layerColor = typesOfYellow[yellowNumerator];
                        yellowNumerator += 1;
                        if (yellowNumerator >= typesOfYellow.Count)
                        {
                            yellowNumerator = 0;
                        }
                    }

                    if (filename.ToUpper().Contains(Properties.Settings.Default.BOTFilePrefix))
                    {
                        layerColor = typesOfGreen[greenNumerator];
                        greenNumerator += 1;
                        if (greenNumerator >= typesOfGreen.Count)
                        {
                            greenNumerator = 0;
                        }
                    }
                    layerIDFMap.SColor = CommonUtils.Color2Long(layerColor);
                    if (!parameters.IsAddOnce || !imfFile.ContainsMap(layerIDFMap.Filename))
                    {
                        imfFile.AddMap(layerIDFMap);
                    }
                }
            }

            if (prfTypeFlag == IDFMap.PRFTypeFlag.Line)
            {
                foreach (string layerFilename in layerFileNames)
                {
                    IDFFile idfFile = IDFFile.ReadFile(layerFilename, !settings.IsUpdateIMODFiles);
                    IDFMap layerIDFMap = IDFMap.CreateSurfaceLevelMap("default", idfFile.MinValue, idfFile.MaxValue, layerFilename, true);
                    layerIDFMap.SetPRFType(IDFMap.PRFTypeFlag.Active);
                    layerIDFMap.AddPRFTypeFlag(prfTypeFlag);
                    if (layerFilename.ToUpper().Contains(Properties.Settings.Default.BOTFilePrefix))
                    {
                        layerIDFMap.SColor = CommonUtils.Color2Long(botLineColor);
                    }
                    else
                    {
                        layerIDFMap.SColor = CommonUtils.Color2Long(topLineColor);
                    }
                    if (!parameters.IsAddOnce || !imfFile.ContainsMap(layerIDFMap.Filename))
                    {
                        imfFile.AddMap(layerIDFMap);
                    }
                }
            }

            return layerFileNames.Count;
        }

        private void ParseMapsSection(StreamReader srINIFile, IMFFile imfFile, Log log, INIParameters parameters, ref string line, SIFToolSettings settings)
        {
            // Set defaults
            Color color = Color.Black;
            Color fillColor = Color.Red;
            Color lineColor = Color.Red;
            int prfType = -1;
            int thickness = 2;
            int ipfColumn = 3;
            int textSize = 0;
            bool IsColumnAdjusted = false;
            bool selected = false;
            string legFilename = null;
            string aliasDefinition = null;  // note: currently the aliasDefinition is used as a prefix before the source filename, excluding extension. An underscore is added after the prefix.
            string dlfFilename = null;
            bool IsLineColorAdjusted = false;
            bool IsFillColorAdjusted = false;

            // Parse lines until a new section if started (and line strarts with '['-symbol
            log.AddInfo("adding maps ...", 1);
            string prevFileExtension = null;
            line = srINIFile.ReadLine();
            while ((line != null) && (line.Trim().Length != 0) && !line.StartsWith("["))
            {
                line = line.Trim();

                // First read (first) line following Maps-keyword, with filename
                string currentFilename = line;
                if (line.ToUpper().StartsWith(Maps_FileKeyword + "="))
                {
                    // If line starts with FILE, parse as key-value pair, otherwise the whole line is read as a filename
                    currentFilename = line.Split('=')[1];
                }
                else
                {
                    log.AddWarning("Missing " + Maps_FileKeyword + "-keyword: skipping '" + currentFilename + "' and rest of MAPS-section!", 2);

                    // No Map-files found, skip rest of MAPS-section
                    while ((line != null) && (line.Trim().Length != 0) && !line.Contains("["))
                    {
                        line = srINIFile.ReadLine();
                        line = line.Trim();
                    }
                    return;
                }

                // Keep settings from previous file if it was of the same type, otherwise use default settings
                if ((prevFileExtension == null) || !currentFilename.ToLower().EndsWith(prevFileExtension.ToLower()))
                {
                    // Reset defaults for a new iMOD-file extension
                    color = Color.Black;
                    fillColor = Color.Red;
                    lineColor = Color.Red;
                    thickness = 3;
                    ipfColumn = 3;
                    IsColumnAdjusted = false;
                    selected = false;
                    legFilename = null;
                    dlfFilename = null;
                    IsLineColorAdjusted = false;
                    IsFillColorAdjusted = false;
                    // prevExtension = 
                }

                // Rettrieve full path of filename
                try
                {
                    currentFilename = GetFullPath(currentFilename, parameters.INIPath);
                }
                catch (Exception ex)
                {
                    throw new ToolException("Invalid filename: " + currentFilename, ex);
                }

                // Check if filename contains wildcards. If so, retrieves all corresponding filenames in current path
                List<string> iMODFilenames = ExpandFilename(currentFilename);
                CommonUtils.SortAlphanumericStrings(iMODFilenames);

                // Read lines as long as =-symbols are found and no other FILE-keyword is found
                line = srINIFile.ReadLine();
                while ((line != null) && line.Contains("=") && !line.StartsWith(Maps_FileKeyword + "="))
                {
                    line = line.Trim();
                    string[] parameter = line.Split('=');
                    if (parameter[0].Trim().ToUpper().Equals(Maps_LegendKeyword))
                    {
                        legFilename = GetFullPath(parameter[1], parameters.INIPath);
                    }
                    else if (parameter[0].Trim().ToUpper().Equals(Maps_DLFLegendKeyword))
                    {
                        dlfFilename = GetFullPath(parameter[1], parameters.INIPath);
                    }
                    else if (line.ToUpper().StartsWith(Maps_AliasKeyword + "="))
                    {
                        aliasDefinition = parameter[1];
                        if (aliasDefinition.Equals(string.Empty))
                        {
                            aliasDefinition = null;
                        }
                    }
                    else if (parameter[0].Trim().ToUpper().Equals(Maps_IsSelectedKeyword))
                    {
                        try
                        {
                            selected = (int.Parse(parameter[1]) == 1);
                        }
                        catch (Exception ex)
                        {
                            throw new ToolException("Could not parse " + Maps_IsSelectedKeyword + "-value: " + parameter[1], ex);
                        }
                    }
                    else if (parameter[0].Trim().ToUpper().Equals(Maps_ThicknessKeyword))
                    {
                        thickness = int.Parse(parameter[1]);
                    }
                    else if (parameter[0].Trim().ToUpper().Equals(Maps_ColumnNumberKeyword))
                    {
                        ipfColumn = int.Parse(parameter[1]);
                        IsColumnAdjusted = true;
                    }
                    else if (parameter[0].Trim().ToUpper().Equals(Maps_TextSizeKeyword))
                    {
                        textSize = int.Parse(parameter[1]);
                    }
                    else if (parameter[0].Trim().ToUpper().Equals(Maps_ColorKeyword))
                    {
                        color = ParseRGBString(parameter[1]);
                    }
                    else if (parameter[0].Trim().ToUpper().Equals(Maps_FillColorKeyword))
                    {
                        fillColor = ParseRGBString(parameter[1]);
                        IsFillColorAdjusted = true;
                    }
                    else if (parameter[0].Trim().ToUpper().Equals(Maps_LineColorKeyword))
                    {
                        lineColor = ParseRGBString(parameter[1]);
                        IsLineColorAdjusted = true;
                    }
                    else if (parameter[0].Trim().ToUpper().Equals(Maps_PRFTypeKeyword))
                    {
                        prfType = ParsePRFType(parameter[1]);
                    }
                    else
                    {
                        log.AddWarning("Parameter " + parameter[0] + " is not a valid parameter in section [MAPS].");
                    }

                    line = srINIFile.ReadLine();
                }

                // Now apply settings to all filenames that were defined just above the settings that have just been read
                for (int idx = 0; idx < iMODFilenames.Count; idx++)
                {
                    string iMODFilename = iMODFilenames[idx].Trim();

                    if (!File.Exists(iMODFilename))
                    {
                        log.AddWarning("File/path not found and skipped: " + currentFilename);
                        continue;
                    }

                    if (iMODFilename.ToUpper().EndsWith(IDFExtension))
                    {
                        prevFileExtension = IDFExtension;
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
                        if (aliasDefinition != null)
                        {
                            idfMap.Alias = aliasDefinition + "_" + Path.GetFileNameWithoutExtension(idfMap.Filename);
                        }
                        idfMap.SetPRFType((prfType != -1) ? prfType : Map.PRFTypeToInt(IDFMap.PRFTypeFlag.Active));
                        if (IsLineColorAdjusted || IsFillColorAdjusted)
                        {
                            if (IsLineColorAdjusted)
                            {
                                idfMap.AddPRFTypeFlag(IDFMap.PRFTypeFlag.Line);
                                idfMap.SColor = CommonUtils.Color2Long(lineColor);
                            }
                            if (IsFillColorAdjusted)
                            {
                                idfMap.AddPRFTypeFlag(IDFMap.PRFTypeFlag.Fill);
                                idfMap.SColor = CommonUtils.Color2Long(fillColor);
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
                        prevFileExtension = GENExtension;
                        if (!parameters.IsAddOnce || !imfFile.ContainsMap(iMODFilename))
                        {
                            GENLegend genLegend = new GENLegend(thickness, color);
                            string alias = (aliasDefinition != null) ? aliasDefinition + "_" + Path.GetFileNameWithoutExtension(iMODFilename) : null;
                            imfFile.AddMap(new Map(genLegend, iMODFilename, alias));
                            log.AddInfo("added " + Path.GetFileName(iMODFilename), 2);
                        }
                    }
                    else if (iMODFilename.ToUpper().EndsWith(IFFExtension))
                    {
                        prevFileExtension = IFFExtension;
                        if (!parameters.IsAddOnce || !imfFile.ContainsMap(iMODFilename))
                        {
                            GENLegend iffLegend = new GENLegend(thickness, color);
                            string alias = (aliasDefinition != null) ? aliasDefinition + "_" + Path.GetFileNameWithoutExtension(iMODFilename) : null;
                            imfFile.AddMap(new Map(iffLegend, iMODFilename, alias));
                            log.AddInfo("added " + Path.GetFileName(iMODFilename), 2);
                        }
                    }
                    else if (iMODFilename.ToUpper().EndsWith(IPFExtension))
                    {
                        prevFileExtension = IPFExtension;
                        IPFMap ipfMap = null;
                        if ((legFilename != null) && (legFilename.Length > 0))
                        {
                            if (!parameters.IsAddOnce || !imfFile.ContainsMap(iMODFilename))
                            {
                                ipfMap = CreateIPFMap(legFilename.Trim(), color, selected, ipfColumn, iMODFilename);
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
                                IPFLegend defaultIPFLegend = IPFLegend.CreateLegend("default IPF legend", "All values", color);
                                ipfMap = new IPFMap(defaultIPFLegend, iMODFilename);
                                ipfMap.SColor = CommonUtils.Color2Long(color);
                                ipfMap.Selected = selected;
                            }
                        }

                        if (ipfMap != null)
                        {
                            ipfMap.SetPRFType((prfType != -1) ? prfType : Map.PRFTypeToInt(IDFMap.PRFTypeFlag.Active));

                            if (ipfMap.Legend != null)
                            {
                                ipfMap.IPFLegend.Thickness = thickness;
                                if ((textSize > 0))
                                {
                                    // Use specified column for labelling
                                    if (ipfMap.IPFLegend.SelectedLabelColumns == null)
                                    {
                                        ipfMap.IPFLegend.SelectedLabelColumns = new List<int>();
                                    }
                                    ipfMap.IPFLegend.SelectedLabelColumns.Add(ipfMap.IPFLegend.ColumnNumber);
                                    ipfMap.IPFLegend.IsLabelShown = true;
                                    ipfMap.IPFLegend.TextSize = textSize;
                                }
                            }
                            if (aliasDefinition != null)
                            {
                                ipfMap.Alias = aliasDefinition + "_" + Path.GetFileNameWithoutExtension(ipfMap.Filename);
                            }
                            if (dlfFilename != null)
                            {
                                ipfMap.DLFFile = DLFFile.ReadFile(dlfFilename);
                            }
                            imfFile.AddMap(ipfMap);
                            log.AddInfo("added " + Path.GetFileName(iMODFilename), 2);
                        }
                    }
                    else
                    {
                        log.AddWarning("File with unknown extension is not added to IMF-file: " + iMODFilename);
                    }
                }
            }
        }

        /// <summary>
        /// Retrieve file(s) specified by given file reference, which may be a single filename, contain wildcards or be directory name.
        /// A directory name will result in all files that are directly under this directory. If string could not be expanded to existing 
        /// files the same path string is returned.
        /// 
        /// </summary>
        /// <param name="pathString"></param>
        /// <returns></returns>
        private List<string> ExpandFilename(string pathString)
        {
            // Check if filename contains wildcards. If so, retrieves all corresponding filenames in current path
            List<string> iMODFilenames = new List<string>();
            if (Path.GetFileName(pathString).Contains("*") || Path.GetFileName(pathString).Contains("?"))
            {
                string path = Path.GetDirectoryName(pathString);
                string filter = Path.GetFileName(pathString);
                string[] filenames = Directory.Exists(path) ? Directory.GetFiles(path, filter) : new string[0];
                foreach (string filename in filenames)
                {
                    // Skip iMOD metadata files
                    if (!Path.GetExtension(filename).ToUpper().Equals(".MET"))
                    {
                        iMODFilenames.Add(filename);
                    }
                }
            }
            else if (File.Exists(pathString))
            {
                iMODFilenames.Add(pathString);
            }
            else if (Directory.Exists(pathString))
            {
                string[] filenames = Directory.GetFiles(pathString);
                foreach (string filename in filenames)
                {
                    // Skip iMOD metadata files
                    if (!Path.GetExtension(filename).ToUpper().Equals(".MET"))
                    {
                        iMODFilenames.Add(filename);
                    }
                }
            }

            if (iMODFilenames.Count == 0)
            {
                // Readd path string with filters or an unexisting filename if it could not be evalualed
                iMODFilenames.Add(pathString);
            }

            return iMODFilenames;
        }

        private int ParsePRFType(string prfTypeString)
        {
            int prfType;
            if (!int.TryParse(prfTypeString, out prfType))
            {
                throw new ToolException("Could not parse PRFType-value: " + prfTypeString);
            }

            return prfType;
        }

        private Color ParseRGBString(string rgbString)
        {
            try
            {
                string[] rgbValues = rgbString.Split(',');
                int red = int.Parse(rgbValues[0]);
                int green = int.Parse(rgbValues[1]);
                int blue = int.Parse(rgbValues[2]);
                return Color.FromArgb(red, green, blue);
            }
            catch (Exception ex)
            {
                throw new ToolException("Could not parse RGB-value: " + rgbString, ex);
            }
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
                        if (!parameters.IMFFilename.ToUpper().EndsWith(".IMF"))
                        {
                            // Add IMF extension, do not use Path.ChangeExtension() which does not work if the filename contains a dot but no extension is given (as is expected for parameters.IMFFilename)
                            parameters.IMFFilename = parameters.IMFFilename + ".IMF";
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

        private int CreateREGIS2DEntry(IMFFile imfFile, string regisDirectory, INIParameters parameters, Dictionary<string, Color> regisColorDictionary, Log log, SIFToolSettings settings)
        {
            List<string> fileNamesOrdered = ReadAndOrderRegisDirectory(regisDirectory, log);
            if (fileNamesOrdered.Count == 0)
            {
                return 0;
            }

            REGISVersion regisVersion = RetrieveREGISVersion(fileNamesOrdered[0]);
            foreach (string filename in fileNamesOrdered)
            {
                string fullPath = regisDirectory + "\\" + filename;

                string layername = regisVersion.CorrectLayerLevelName(filename);
                string[] layernameParts = layername.Split('-');
                string regisLayerString = layernameParts[0];
                string topOrbot = layernameParts[1];
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
                    regisIDFMap.SColor = CommonUtils.Color2Long(fillColor);
                    if (!parameters.IsAddOnce || !imfFile.ContainsMap(regisIDFMap.Filename))
                    {
                        imfFile.AddMap(regisIDFMap);
                    }
                }
            }

            return fileNamesOrdered.Count;
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
                        {"MSz1".ToUpper(),Color.FromArgb(135,206,235)},
                        {"MSk1".ToUpper(),Color.FromArgb(105,161,160)},
                        {"MSz2".ToUpper(),Color.FromArgb(135,206,235)},
                        {"MSk2".ToUpper(),Color.FromArgb(115,176,185)},
                        {"MSz3".ToUpper(),Color.FromArgb(135,206,235)},
                        {"MSc".ToUpper(),Color.FromArgb(105,161,160)},
                        {"MSz4".ToUpper(),Color.FromArgb(135,206,235)},
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
            List<string> fileNamesOrdered = new List<string>();
            if (Directory.Exists(regisPath))
            {
                DirectoryInfo regisDirectory = new DirectoryInfo(regisPath);
                FileInfo[] regisFiles = regisDirectory.GetFiles("*.IDF");
                List<string> regisFilenames = new List<string>();
                foreach (FileInfo file in regisFiles)
                {
                    regisFilenames.Add(file.Name);
                }

                if (regisFilenames.Count == 0)
                {
                    log.AddWarning("No REGIS-filenames found");
                }

                List<string> regisUnitsOrdered = regisEenheden;
                if (layerOrderPrefixIdxDictionary != null)
                {
                    regisUnitsOrdered = new List<string>();
                    foreach (string prefix in layerOrderPrefixIdxDictionary.Keys)
                    {
                        regisUnitsOrdered.Add(prefix.ToUpper());
                    }
                }

                REGISVersion regisVersion = RetrieveREGISVersion(regisFilenames[0]);

                foreach (string regisUnit in regisUnitsOrdered)
                {
                    int fileIdx = 0;
                    while (fileIdx < regisFilenames.Count)
                    {
                        string filename = regisFilenames[fileIdx];

                        // Correct for alternative REGIS name formats
                        string layername = regisVersion.CorrectLayerLevelName(filename);

                        string[] layernameParts = layername.Split('-');
                        string reigsLayerUnit = layernameParts[0];
                        string regisLayerUnitPart = layernameParts[0].Split('_')[0];
                        // If full regis unit is present in orderlist, compare full name (including postfix), otherwise just test main part
                        bool hasMatch = false;
                        if (regisUnitsOrdered.Contains(reigsLayerUnit.ToUpper()))
                        {
                            hasMatch = reigsLayerUnit.ToUpper().Equals(regisUnit.ToUpper());
                        }
                        else
                        {
                            hasMatch = regisLayerUnitPart.ToUpper().Equals(regisUnit.ToUpper());
                        }
                        if (hasMatch)
                        {
                            if ((layernameParts.Length > 1)  && layernameParts[1].ToLower().Equals("b"))
                            {
                                // First add top-file when existing
                                string topFilename = regisVersion.GetTOPFilename(filename);
                                if (File.Exists(Path.Combine(regisPath, topFilename)))
                                {
                                    fileNamesOrdered.Add(topFilename);
                                    regisFilenames.Remove(topFilename);
                                }
                                else
                                {
                                    log.AddWarning("Missing TOP-file: " + topFilename);
                                }
                                fileNamesOrdered.Add(filename);
                                regisFilenames.Remove(filename);
                            }
                            else if ((layernameParts.Length > 1) && layernameParts[1].ToLower().Equals("t"))
                            {
                                // First add top-file
                                fileNamesOrdered.Add(filename);
                                regisFilenames.Remove(filename);

                                // Now check for bot-file
                                string botFilename = regisVersion.GetBOTFilename(filename);
                                if (File.Exists(Path.Combine(regisPath, botFilename)))
                                {
                                    fileNamesOrdered.Add(botFilename);
                                    regisFilenames.Remove(botFilename);
                                }
                                else
                                {
                                    log.AddWarning("Missing BOT-file: " + botFilename);
                                }
                            }
                            else
                            {
                                // Note, file may be a thickness- or kh/kv-file, do not log all these files
                                fileIdx++;
                            }
                        }
                        else
                        {
                            fileIdx++;
                        }
                    }
                }
            }

            return fileNamesOrdered;
        }

        protected virtual REGISVersion RetrieveREGISVersion(string regisFilename)
        {
            return REGISVersionFactory.RetrieveVersion(regisFilename);
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

        /// <summary>
        /// Create IPFMap object which can be passed for IMF-file creation.
        /// </summary>
        /// <param name="legFilename"></param>
        /// <param name="color"></param>
        /// <param name="isSelected"></param>
        /// <param name="columnNumber">one-based column number. Use negative number to start from the last column, with -1 indicating the last column</param>
        /// <param name="ipfFilename"></param>
        /// <param name="legendDescription"></param>
        /// <returns></returns>
        public static IPFMap CreateIPFMap(string legFilename, Color color, bool isSelected, int columnNumber, string ipfFilename = null, string legendDescription = "IPF legend")
        {
            IPFMap ipfMap = new IPFMap(legendDescription, isSelected);
            ipfMap.IPFLegend.ImportClasses(legFilename);
            if (columnNumber < 0)
            {
                // For negative columnindices start from the last column with -1 indicating the last column
                IPFFile ipfFile = IPFFile.ReadFile(ipfFilename, true);
                columnNumber = ipfFile.ColumnCount + columnNumber + 1;
            }
            ipfMap.IPFLegend.ColumnNumber= columnNumber;
            ipfMap.SColor = CommonUtils.Color2Long (color);
            ipfMap.Filename = ipfFilename;
            return ipfMap;
        }
    }
}
