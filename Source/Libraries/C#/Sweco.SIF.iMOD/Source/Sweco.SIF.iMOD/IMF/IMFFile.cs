// Sweco.SIF.iMOD is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.iMOD.
// 
// Sweco.SIF.iMOD is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.iMOD is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.iMOD. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMOD.ISG;
using Sweco.SIF.iMOD.Legends;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.IMF
{
    /// <summary>
    /// Class for reading, editing and writing iMOD IMF-files. See iMOD-manual for details of IMF-files: https://oss.deltares.nl/nl/web/imod/user-manual.
    /// </summary>
    public class IMFFile
    {
        /// <summary>
        /// Default XY aspect ratio that is used for fixing aspect ratio
        /// </summary>
        public const float IMFXYRatio = 2.06f;

        /// <summary>
        /// Formatting and other cultureInfo of English (UK) language
        /// </summary>
        protected static CultureInfo EnglishCultureInfo = new CultureInfo("en-GB", false);

        /// <summary>
        /// File Extension of IMF-file
        /// </summary>
        public string Extension
        {
            get { return "IMF"; }
        }

        /// <summary>
        /// List of current map files in IMF
        /// </summary>
        public List<Map> Maps { get; }

        /// <summary>
        /// List of current overlay files in IMF
        /// </summary>
        public List<Overlay> Overlays { get; }

        /// <summary>
        /// Current extent of IMF-file
        /// </summary>
        public Extent Extent { get; set; }

        /// <summary>
        /// Number of missing files in IMF-file
        /// </summary>
        public int MissingFileCount { get; set; }

        /// <summary>
        /// Number of missing files in IMF-file that is selected
        /// </summary>
        public int SelectedMissingFileCount { get; set; }

        /// <summary>
        /// Create empty IMFFile object
        /// </summary>
        public IMFFile()
        {
            Maps = new List<Map>();
            Overlays = new List<Overlay>();
        }

        /// <summary>
        /// Create empty IMF-file with specified extent
        /// </summary>
        /// <param name="extent"></param>
        public IMFFile(Extent extent)
        {
            Maps = new List<Map>();
            Overlays = new List<Overlay>();
            this.Extent = extent;
        }

        /// <summary>
        /// Add Map object to Map-section of IMF-file
        /// </summary>
        /// <param name="map"></param>
        public void AddMap(Map map)
        {
            if (map.Filename == null)
            {
                throw new Exception("Filename cannot be null for a map");
            }
            Maps.Add(map);
        }

        /// <summary>
        /// Add Overlay object to Overlay-section of IMF-file
        /// </summary>
        /// <param name="overlay"></param>
        public void AddOverlay(Overlay overlay)
        {
            if (overlay.Filename == null)
            {
                throw new Exception("Filename cannot be null for an overlay");
            }
            Overlays.Add(overlay);
        }

        /// <summary>
        /// Check if a map with specified filename is already present in this IMFFile
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool ContainsMap(string filename)
        {
            foreach (Map map in Maps)
            {
                if (map.Filename.ToLower().Equals(filename.ToLower()))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check if an overlay with specified filename is already present in this IMFFile
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public bool ContainsOverlay(string filename)
        {
            foreach (Overlay overlay in Overlays)
            {
                if (overlay.Filename.ToLower().Equals(filename.ToLower()))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Read IMF-file with specified filename and create corresponding IMFFile object. 
        /// Existance of referred files is checked, which is logged and stored in MissingFileCount and SelectedMissingFileCount properties of IMFFile.
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        public static IMFFile ReadFile(string filename, Log log, int logIndentLevel = 0)
        {
            IMFFile imfFile = new IMFFile();
            int nact = -1;

            Stream stream = null;
            StreamReader sr = null;
            long linenumber = 0;
            string line = string.Empty;
            int selectedMissingFileCount = 0;
            int selectedFileCount = 0;
            try
            {
                stream = File.OpenRead(filename);
                sr = new StreamReader(stream);

                // Read IMF-header and extent
                line = ReadLine(sr, ref linenumber);
                line = ReadLine(sr, ref linenumber);
                while (line.Equals(string.Empty))
                {
                    line = ReadLine(sr, ref linenumber);
                }
                if (line.ToUpper().StartsWith("NACT="))
                {
                    string[] lineValues = line.Split('=');
                    if (!int.TryParse(lineValues[1], out nact))
                    {
                        throw new ToolException("Invalid NACT-value: " + line);
                    }
                }
                else
                {
                    throw new ToolException("Invalid line at linenumnber " + linenumber + ", expected NACT definition: " + line);
                }

                Extent extent = new Extent();
                try
                {
                    line = ReadLine(sr, ref linenumber);
                    string[] lineValues = line.Split('=');
                    extent.llx = float.Parse(lineValues[1], EnglishCultureInfo);
                    line = ReadLine(sr, ref linenumber);
                    lineValues = line.Split('=');
                    extent.urx = float.Parse(lineValues[1], EnglishCultureInfo);
                    line = ReadLine(sr, ref linenumber);
                    lineValues = line.Split('=');
                    extent.lly = float.Parse(lineValues[1], EnglishCultureInfo);
                    line = ReadLine(sr, ref linenumber);
                    lineValues = line.Split('=');
                    extent.ury = float.Parse(lineValues[1], EnglishCultureInfo);
                }
                catch (Exception ex)
                {
                    throw new ToolException("Invalid line at linenumber " + linenumber + " while parsing Extent : " + line, ex);
                }

                // Skip two lines
                line = ReadLine(sr, ref linenumber);
                line = ReadLine(sr, ref linenumber);

                while (!sr.EndOfStream)
                {
                    line = ReadLine(sr, ref linenumber);
                    if (line.StartsWith("IDFNAME="))
                    {
                        string[] lineValues = line.Split('=');
                        string imodFilename = lineValues[1];
                        if (!File.Exists(imodFilename))
                        {
                            if (log != null)
                            {
                                log.AddWarning("iMOD MAP-file does not exist: " + imodFilename, logIndentLevel);
                                selectedMissingFileCount++;

                                if (CheckIMFFileSelection(sr, ref linenumber, log, logIndentLevel))
                                {
                                    selectedFileCount++;
                                }
                            }
                        }
                    }
                    if (line.StartsWith("GENNAME="))
                    {
                        string[] lineValues = line.Split('=');
                        string genFilename = lineValues[1].Replace('\0', ' ').Trim();
                        if (!File.Exists(genFilename))
                        {
                            if (log != null)
                            {
                                log.AddWarning("iMOD Overlay-file does not exist: " + genFilename, logIndentLevel);
                                selectedMissingFileCount++;

                                if (CheckIMFFileSelection(sr, ref linenumber, log, logIndentLevel))
                                {
                                    selectedFileCount++;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while reading IMF-file line " + linenumber + ": " + line, ex);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
                if (stream != null)
                {
                    stream.Close();
                }
            }
            imfFile.MissingFileCount = selectedMissingFileCount;
            imfFile.SelectedMissingFileCount = selectedFileCount;
            return imfFile;
        }

        /// <summary>
        /// Rretrieve union extent of all iMOD mapfiles
        /// </summary>
        /// <returns>union extent or null if no or only empty iMOD-files are present</returns>
        public Extent GetMaxMapExtent()
        {
            Extent maxExtent = null;
            foreach (Map map in Maps)
            {
                string iMODFilename = map.Filename;
                Extent iMODFileExtent = null;
                switch (Path.GetExtension(iMODFilename).ToLower())
                {
                    case ".idf":
                        IDFFile idfFile = IDFFile.ReadFile(iMODFilename, true);
                        iMODFileExtent = idfFile.Extent;
                        break;
                    case ".ipf":
                        IPFFile ipfFile = IPFFile.ReadFile(iMODFilename, true);
                        iMODFileExtent = ipfFile.Extent;
                        break;
                    case ".gen":
                        GENFile genFile = GENFile.ReadFile(iMODFilename);
                        iMODFileExtent = genFile.Extent;
                        break;
                }

                if (iMODFileExtent != null)
                {
                    if (maxExtent != null)
                    {
                        maxExtent = maxExtent.Union(iMODFileExtent);
                    }
                    else
                    {
                        maxExtent = iMODFileExtent;
                    }
                }
            }

            return maxExtent;
        }

        /// <summary>
        /// Writes IMF-file. Layerselection is handled with the Selected parameter
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="useRelativeIMFPaths"></param>
        public void WriteFile(string filename, bool useRelativeIMFPaths = true)
        {
            string relativePath = string.Empty;
            if (useRelativeIMFPaths)
            {
                relativePath = FileUtils.EnsureTrailingSlash(Path.GetDirectoryName(filename)).ToUpper();
            }

            // Create IMF File
            int mapLegendCount = 0;

            // Add extent
            string imfString = string.Empty;
            if (Extent == null)
            {
                throw new Exception("WriteFile(): IMFFile.Extent cannot be null");
            }
            imfString += "XMIN=     " + Extent.llx.ToString("0.000").Replace(",", ".") + "\r\n";
            imfString += "XMAX=     " + Extent.urx.ToString("0.000").Replace(",", ".") + "\r\n";
            imfString += "YMIN=     " + Extent.lly.ToString("0.000").Replace(",", ".") + "\r\n";
            imfString += "YMAX=     " + Extent.ury.ToString("0.000").Replace(",", ".") + "\r\n";
            imfString += "ITRANSP=         0" + "\r\n";
            imfString += "IASAVE=          1" + "\r\n";
            imfString += "==================================================" + "\r\n";

            for (int legendIdx = 0; legendIdx < Maps.Count; legendIdx++)
            {
                Map map = Maps[legendIdx];
                if (map != null)
                {
                    mapLegendCount++;
                    if (map.Legend is IDFLegend)
                    {
                        IDFLegend idfLegend = (IDFLegend)map.Legend;
                        idfLegend.Sort();
                        bool selected = (map is IDFMap) ? ((IDFMap)map).Selected : (legendIdx == 0);
                        imfString += CreateIDFMapIMFString(map, useRelativeIMFPaths, relativePath, selected);
                    }
                    else if (map.Legend is GENLegend)
                    {
                        GENLegend genLegend = (GENLegend)map.Legend;
                        bool selected = genLegend.Selected;
                        imfString += CreateGENMapIMFString(map, useRelativeIMFPaths, relativePath, selected);
                    }
                    else if (map.Legend is IPFLegend)
                    {
                        IPFLegend ipfLegend = (IPFLegend)map.Legend;
                        ipfLegend.Sort();
                        bool selected = (map is IPFMap) ? ((IPFMap)map).Selected : (legendIdx == 0);
                        imfString += CreateIPFMapIMFString(map, useRelativeIMFPaths, relativePath, selected);
                    }
                    else if (map.Legend is ISGLegend)
                    {
                        ISGLegend isgLegend = (ISGLegend)map.Legend;
                        imfString += CreateISGMapIMFString(map, legendIdx, useRelativeIMFPaths, relativePath);
                    }
                    else
                    {
                        throw new Exception("Unknown maplegend type, could not create IMF-representation for " + map.GetType().Name);
                    }
                }
            }

            imfString += "//////////////////////////////////////////////////" + "\r\n";

            // Add GEN-files
            foreach (Overlay overlay in Overlays)
            {
                imfString += "++++++++++++++++++++++++++++++++++++++++++++++++++" + "\r\n";
                if (useRelativeIMFPaths)
                {
                    imfString += @"GENNAME=" + overlay.Filename.Replace(relativePath, string.Empty) + "\r\n";
                }
                else
                {
                    imfString += @"GENNAME=" + overlay.Filename + "\r\n";
                }
                imfString += "ISEL=            " + (overlay.Legend.Selected ? "T" : "F") + "\r\n";
                imfString += "ITYPE=           1" + "\r\n";
                imfString += "SYMBOL=          " + overlay.Legend.Symbol + "\r\n";
                imfString += "THICKNS=         " + overlay.Legend.Thickness + "\r\n";
                imfString += "RGB=" + Color2Long(overlay.Legend.Color).ToString().PadLeft(14) + "\r\n";
                //                imfString += "RGB=       8421504" + "\r\n";
                imfString += "++++++++++++++++++++++++++++++++++++++++++++++++++" + "\r\n";
            }
            imfString += "[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[[" + "\r\n";

            imfString = "IMOD META-FILE [Generated by Sweco iMOD-tools]\r\n\r\n" +
                        "NACT=            " + mapLegendCount + "\r\n" +
                        imfString;

            if (!Path.GetDirectoryName(filename).Equals(string.Empty) && !Directory.Exists(Path.GetDirectoryName(filename)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
            }

            string imfFilename = filename;
            if (Path.GetExtension(filename).Equals(string.Empty))
            {
                imfFilename += ".IMF";
            }

            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(imfFilename);
                sw.Write(imfString);
            }
            catch (IOException ex)
            {
                if (ex.Message.ToLower().Contains("access") || ex.Message.ToLower().Contains("toegang"))
                {
                    throw new ToolException(Extension + "-file cannot be written, because it is being used by another process: " + filename);
                }
                else
                {
                    throw new Exception("Unexpected error while writing " + Extension + "-file: " + filename, ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while writing " + Extension + "-file: " + filename, ex);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                    sw = null;
                }
            }
        }

        private string CreateGENMapIMFString(Map genMap, bool useRelativeIMFPaths, string relativePath, bool isSelected)
        {
            GENLegend genLegend = (GENLegend)genMap.Legend;
            string imfString = "++++++++++++++++++++++++++++++++++++++++++++++++++" + "\r\n";
            if (useRelativeIMFPaths)
            {
                imfString += "IDFNAME=" + genMap.Filename.ToUpper().Replace(relativePath, string.Empty) + "\r\n";
            }
            else
            {
                imfString += "IDFNAME=" + genMap.Filename.ToUpper() + "\r\n";
            }
            imfString += "ALIAS=  " + Path.GetFileName(genMap.Filename).ToUpper() + "\r\n";
            if (isSelected == true)
            {
                imfString += "ISEL=            T" + "\r\n";
            }
            else
            {
                imfString += "ISEL=            F" + "\r\n";
            }
            imfString += "SCOLOR=    " + Color2Long(genLegend.Color).ToString().PadLeft(5) + "\r\n";
            imfString += "THICKNS=         " + genLegend.Thickness + "\r\n";
            imfString += "IATTRIB=         1" + "\r\n";
            imfString += "IDFI=            0" + "\r\n";
            imfString += "IEQ=             0" + "\r\n";
            imfString += "IDFKIND=         0" + "\r\n";
            imfString += "SYMBOL=          0" + "\r\n";
            imfString += "FADEOUT=         0" + "\r\n";
            imfString += "UNITS=           0" + "\r\n";
            imfString += "PRFTYPE=         0" + "\r\n";
            imfString += "ISCREEN=         0" + "\r\n";
            imfString += "ILEG=            0" + "\r\n";
            imfString += "NCLR=            0" + "\r\n";
            imfString += "--------------------------------------------------" + "\r\n";
            imfString += "LEGEND DEFINITION" + "\r\n";
            imfString += "--------------------------------------------------" + "\r\n";
            imfString += "HEDTXT= " + "\r\n";
            imfString += "                  CLASS=     0.000000" + "\r\n";
            for (int i = 0; i < 7; i++)
            {
                imfString += "CGRAD=           0" + "\r\n";
            }
            imfString += "++++++++++++++++++++++++++++++++++++++++++++++++++" + "\r\n";

            return imfString;
        }

        private static bool CheckIMFFileSelection(StreamReader sr, ref long linenumber, Log log, int logIndentLevel)
        {
            bool isSelected = false;
            string line = ReadLine(sr, ref linenumber);
            if (!line.ToUpper().StartsWith("ISEL"))
            {
                // skip line
                line = ReadLine(sr, ref linenumber);
            }
            if (line.ToUpper().StartsWith("ISEL"))
            {
                string[] stringValues = line.Split('=');
                string selString = stringValues[1].Trim();
                if (selString.ToUpper().Equals("T"))
                {
                    log.AddWarning("File is selected in IMF-file", logIndentLevel + 1);
                    isSelected = true;
                }
            }

            return isSelected;
        }

        private static string ReadLine(StreamReader sr, ref long linenumber)
        {
            string line = sr.ReadLine().Trim();
            linenumber++;
            return line;
        }

        private string CreateISGMapIMFString(Map isgMap, int legendIdx, bool useRelativeIMFPaths, string relativePath)
        {
            string imfString = "++++++++++++++++++++++++++++++++++++++++++++++++++" + "\r\n";
            if (useRelativeIMFPaths)
            {
                imfString += "IDFNAME=" + isgMap.Filename.ToUpper().Replace(relativePath, string.Empty) + "\r\n";
            }
            else
            {
                imfString += "IDFNAME=" + isgMap.Filename.ToUpper() + "\r\n";
            }
            imfString += "ALIAS=  " + Path.GetFileName(isgMap.Filename).ToUpper() + "\r\n";
            if (legendIdx == 0)
            {
                imfString += "ISEL=            T" + "\r\n";
            }
            else
            {
                imfString += "ISEL=            F" + "\r\n";
            }
            imfString += "SCOLOR=" + Color2Long(((ISGLegend)isgMap.Legend).Color).ToString().PadLeft(11) + "\r\n";
            imfString += "THICKNS=" + ((ISGLegend)isgMap.Legend).Thickness.ToString().PadLeft(10) + "\r\n";
            imfString += "++++++++++++++++++++++++++++++++++++++++++++++++++" + "\r\n";

            return imfString;
        }

        private string CreateIPFMapIMFString(Map ipfMap, bool useRelativeIMFPaths, string relativePath, bool isSelected)
        {
            IPFLegend ipfLegend = (IPFLegend)ipfMap.Legend;
            string imfString = "++++++++++++++++++++++++++++++++++++++++++++++++++" + "\r\n";
            if (useRelativeIMFPaths)
            {
                imfString += "IDFNAME=" + ipfMap.Filename.ToUpper().Replace(relativePath, string.Empty) + "\r\n";
            }
            else
            {
                imfString += "IDFNAME=" + ipfMap.Filename.ToUpper() + "\r\n";
            }
            imfString += "ALIAS=  " + Path.GetFileName(ipfMap.Filename).ToUpper() + "\r\n";
            if (isSelected)
            {
                imfString += "ISEL=            T" + "\r\n";
            }
            else
            {
                imfString += "ISEL=            F" + "\r\n";
            }
            if (ipfLegend.ClassList.Count() == 1)
            {
                // Use defined color if a single legend class is defined
                imfString += "SCOLOR=" + Color2Long(ipfLegend.ClassList[0].Color).ToString().PadLeft(11) + "\r\n";
            }
            else
            {
                imfString += "SCOLOR=    4410933" + "\r\n";
            }
            imfString += "THICKNS=" + ipfLegend.Thickness.ToString().PadLeft(10) + "\r\n";
            if (ipfLegend.ClassList.Count() == 1)
            {
                // Use defined color if a single legend class is defined
                imfString += "ILEG=            0" + "\r\n";
            }
            else
            {
                imfString += "ILEG=            1" + "\r\n";
            }
            imfString += "XCOL=            1" + "\r\n";
            imfString += "YCOL=            2" + "\r\n";
            imfString += "ZCOL=            1" + "\r\n";
            imfString += "Z2COL=           1" + "\r\n";
            imfString += "HCOL=            0" + "\r\n";
            imfString += "IAXES=  1111111111" + "\r\n";
            imfString += "TSIZE=           7" + "\r\n";
            imfString += "ASSCOL1=         2" + "\r\n";
            imfString += "ASSCOL2=         0" + "\r\n";
            imfString += "IATTRIB=" + ipfLegend.ColumnIndex.ToString().PadLeft(10) + "\r\n";
            imfString += "IDFI=          250" + "\r\n";
            long ieq = CalculateIEQValue(ipfLegend.SelectedLabelColumns, ipfLegend.IsLabelShown);
            imfString += "IEQ=" + ieq.ToString().PadLeft(14) + "\r\n";
            imfString += "IDFKIND=         0" + "\r\n";
            imfString += "SYMBOL=         14" + "\r\n";
            imfString += "FADEOUT=         0" + "\r\n";
            imfString += "UNITS=           1" + "\r\n";
            imfString += "PRFTYPE=         0" + "\r\n";
            imfString += "ISCREEN=         1" + "\r\n";
            imfString += "NCLR=          " + ipfLegend.ClassList.Count.ToString().PadLeft(3) + "\r\n";
            imfString += "--------------------------------------------------" + "\r\n";
            imfString += "LEGEND DEFINITION" + "\r\n";
            imfString += "--------------------------------------------------" + "\r\n";
            imfString += "HEDTXT= " + "\r\n";
            imfString += "                  CLASS=" + ipfLegend.ClassList[0].MaxValue.ToString().Replace(",", ".").PadLeft(13) + "\r\n";
            for (int i = 0; i < ipfLegend.ClassList.Count; i++)
            {
                RangeLegendClass legendClass = ipfLegend.ClassList[i];
                imfString += "RGB=" + Color2Long(legendClass.Color).ToString().PadLeft(14) + "CLASS=" + legendClass.MinValue.ToString().Replace(",", ".").PadLeft(13) + "    LEGTXT= " + legendClass.Label.Replace("\"", string.Empty) + "\r\n";
            }
            for (int i = 0; i < 7; i++)
            {
                imfString += "CGRAD=           1" + "\r\n";
            }
            imfString += "++++++++++++++++++++++++++++++++++++++++++++++++++" + "\r\n";

            return imfString;
        }

        private string CreateIDFMapIMFString(Map idfMap, bool useRelativeIMFPaths, string relativePath, bool isSelected)
        {
            IDFLegend idfLegend = (IDFLegend)idfMap.Legend;
            string imfString = "++++++++++++++++++++++++++++++++++++++++++++++++++" + "\r\n";
            if (useRelativeIMFPaths)
            {
                imfString += "IDFNAME=" + idfMap.Filename.ToUpper().Replace(relativePath, string.Empty) + "\r\n";
            }
            else
            {
                imfString += "IDFNAME=" + idfMap.Filename.ToUpper() + "\r\n";
            }
            imfString += "ALIAS=  " + Path.GetFileNameWithoutExtension(idfMap.Filename).ToUpper() + "\r\n";
            if (isSelected)
            {
                imfString += "ISEL=            T" + "\r\n";
            }
            else
            {
                imfString += "ISEL=            F" + "\r\n";
            }
            imfString += "SCOLOR= " + ((idfMap is IDFMap) ? ((IDFMap)idfMap).SColor.ToString() : "4410933") + "\r\n";
            imfString += "THICKNS=" + ((idfMap is IDFMap) ? ((IDFMap)idfMap).LineThickness.ToString().PadLeft(10) : "1") + "\r\n";
            imfString += "IATTRIB=         0" + "\r\n";
            imfString += "IDFI=            0" + "\r\n";
            imfString += "IEQ=             0" + "\r\n";
            imfString += "IDFKIND=         1" + "\r\n";
            imfString += "SYMBOL=          0" + "\r\n";
            imfString += "FADEOUT=         0" + "\r\n";
            imfString += "UNITS=           0" + "\r\n";
            imfString += "PRFTYPE=        " + ((idfMap is IDFMap) ? ((IDFMap)idfMap).PRFType.ToString().PadLeft(1) : IDFMap.PRFTypeToInt(IDFMap.PRFTypeFlag.Line).ToString()) + "\r\n";
            imfString += "ISCREEN=         1" + "\r\n";
            imfString += "NCLR=          " + idfLegend.ClassList.Count.ToString().PadLeft(3) + "\r\n";
            imfString += "--------------------------------------------------" + "\r\n";
            imfString += "LEGEND DEFINITION" + "\r\n";
            imfString += "--------------------------------------------------" + "\r\n";
            imfString += "HEDTXT= " + "\r\n";
            imfString += "                  CLASS=" + idfLegend.ClassList[0].MaxValue.ToString().Replace(",", ".").PadLeft(13) + "\r\n";
            for (int i = 0; i < idfLegend.ClassList.Count; i++)
            {
                RangeLegendClass legendClass = idfLegend.ClassList[i];
                imfString += "RGB=" + Color2Long(legendClass.Color).ToString().PadLeft(14) + "CLASS=" + legendClass.MinValue.ToString().Replace(",", ".").PadLeft(13) + "    LEGTXT= " + legendClass.Label.Replace("\"", string.Empty) + "\r\n";
            }
            for (int i = 0; i < 7; i++)
            {
                imfString += "CGRAD=           1" + "\r\n";
            }
            imfString += "++++++++++++++++++++++++++++++++++++++++++++++++++" + "\r\n";

            return imfString;
        }

        /// <summary>
        /// Calculates aspect ratio of current extent this IMF-file
        /// </summary>
        /// <returns></returns>
        public float GetExtentAspectRatio()
        {
            float dx = (Extent.urx - Extent.llx);
            float dy = (Extent.ury - Extent.lly);
            return dx / dy;
        }

        /// <summary>
        /// Changes aspect ratio of current extent to specified ratio to ensure a correct display scale in IMF-file
        /// </summary>
        /// <param name="imfXYRatio">new aspect ratio ((Xur-Xll)/(Yur-Yll)) of extent</param>
        /// <returns>true, when aspect ration is changed</returns>
        public bool FixExtentAspectRatio(float imfXYRatio = 2.06f)
        {
            bool isFixed = false;
            float dx = (Extent.urx - Extent.llx);
            float dy = (Extent.ury - Extent.lly);
            float xyRatio = dx / dy;
            if (((xyRatio / imfXYRatio) < 0.975f) || ((xyRatio / imfXYRatio) > 1.025f))
            {
                float xScaleCorr = 0.5f * ((imfXYRatio * dy) - dx);
                Extent.llx -= xScaleCorr;
                Extent.urx += xScaleCorr;
                isFixed = true;
            }
            return isFixed;
        }

        /// <summary>
        /// Convert list with column one-based indices to long number forIMF
        /// </summary>
        /// <param name="columnIndices">one-based column indices</param>
        /// <param name="isLabelShown"></param>
        /// <param name="isLabelColored"></param>
        /// <returns></returns>
        private long CalculateIEQValue(List<int> columnIndices, bool isLabelShown = true, bool isLabelColored = false)
        {
            long longValue = 0;
            if (isLabelShown)
            {
                longValue = ColumnListToLong(columnIndices);
                if (!isLabelColored)
                {
                    longValue = -longValue;
                }
            }
            return longValue;
        }

        /// <summary>
        /// Convert list with column one-based indices to long number forIMF
        /// </summary>
        /// <param name="columnIndices">one-based column indices</param>
        /// <returns></returns>
        private long ColumnListToLong(List<int> columnIndices)
        {
            long longValue = 0;
            if (columnIndices != null)
            {
                foreach (int colIdx in columnIndices)
                {
                    longValue += (long)Math.Pow(2, (colIdx - 1));
                };
            }
            return longValue;
        }

        /// <summary>
        /// Convert the color from a Color object to an RGB long value (as used for colors in the iMOD IMF-file)
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static long Color2Long(Color color)
        {
            return color.R + color.G * 256 + color.B * 65536;
        }

        /// <summary>
        /// Convert an RGB long value (as used for colors in the iMOD IMF-file) to a Color object
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Color Long2Color(long color)
        {
            return Color.FromArgb((int)color % 256, (int)(color % 65536) / 256, (int)color / 65536);
        }
    }
}
