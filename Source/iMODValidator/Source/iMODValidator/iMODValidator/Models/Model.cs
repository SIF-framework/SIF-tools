// iMODValidator is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODValidator.
// 
// iMODValidator is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODValidator is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODValidator. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMODValidator.Models.Packages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Models
{
    /// <summary>
    /// Class for storing an iMOD-modelschematisation, including modelproperties
    /// </summary>
    public class Model
    {
        //public const int MaxLayers = 25; // 25; // for debugging purpose
        private string runfilename;
        private string modelresultsPath;
        private SubModel[] submodels;
        private List<Package> packages;
        private string surfaceLevelFilename;
        private int mxnlay;
        private int nlay;
        private int nper;
        private long sdate;
        private int nscl;
        private int iftest;
        private int iconchk;
        private int iipf;
        private int nmult;
        private int idebug;
        private int iexport;
        private int iposwell;
        private int iscen;
        private int ibdg;
        private float minkd;
        private float minc;
        private string bndfile;
        public DateTime? StartDate;
        private string toolOutputPath;
        private Dictionary<string, int> kperDictionary;

        public string Runfilename
        {
            get { return runfilename; }
            set { runfilename = value; }
        }
        /// <summary>
        /// The outputpath from the runfile that the model results are written to
        /// </summary>
        public string ModelresultsPath
        {
            get { return modelresultsPath; }
            set { modelresultsPath = value; }
        }
        public List<Package> Packages
        {
            get { return packages; }
            set { packages = value; }
        }
        public SubModel[] Submodels
        {
            get { return submodels; }
            set { submodels = value; }
        }
        public int NLAY
        {
            get { return nlay; }
            set
            {
                if (value <= 0)
                {
                    throw new Exception("Invalid NLAY value");
                }
                nlay = value;
            }
        }
        public int MXNLAY
        {
            get { return mxnlay; }
            set
            {
                if (value <= 0)
                {
                    throw new Exception("Invalid MXNLAY value");
                }
                mxnlay = value;
            }
        }
        public int NPER
        {
            get { return nper; }
            set
            {
                if (value <= 0)
                {
                    throw new Exception("Invalid NPER value");
                }
                nper = value;
            }
        }
        public long SDATE
        {
            get { return sdate; }
            set
            {
                sdate = value;
                if (sdate > 0)
                {
                    string sdateString = sdate.ToString();
                    try
                    {
                        int year = int.Parse(sdateString.Substring(0, 4));
                        int month = int.Parse(sdateString.Substring(4, 2));
                        int day = int.Parse(sdateString.Substring(6, 2));
                        StartDate = new DateTime(year, month, day);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Invalid date-string: " + sdateString, ex);
                    }
                }
                else
                {
                    StartDate = null;
                }
            }
        }
        public int NSCL
        {
            get { return nscl; }
            set { nscl = value; }
        }
        public int IFTEST
        {
            get { return iftest; }
            set { iftest = value; }
        }
        public int ICONCHK
        {
            get { return iconchk; }
            set
            {
                if ((value == 0) || (value == 1))
                {
                    iconchk = value;
                }
                else
                {
                    throw new Exception("Invalid ICONCHK value");
                }
            }
        }
        public int IIPF
        {
            get { return iipf; }
            set
            {
                if (value <= 1)
                {
                    iipf = value;
                }
                else
                {
                    throw new Exception("Invalid IIPF value");
                }
            }
        }
        public int NMULT
        {
            get { return nmult; }
            set { nmult = value; }
        }
        public int IDEBUG
        {
            get { return idebug; }
            set { idebug = value; }
        }
        public int IEXPORT
        {
            get { return iexport; }
            set { iexport = value; }
        }
        public int IPOSWELL
        {
            get { return iposwell; }
            set { iposwell = value; }
        }
        public int ISCEN
        {
            get { return iscen; }
            set { iscen = value; }
        }
        public int IBDG
        {
            get { return ibdg; }
            set { ibdg = value; }
        }
        public float MINKD
        {
            get { return minkd; }
            set { minkd = value; }
        }
        public float MINC
        {
            get { return minc; }
            set { minc = value; }
        }
        public string BNDFILE
        {
            get { return bndfile; }
            set { bndfile = value; }
        }
        public string SurfaceLevelFilename
        {
            get { return surfaceLevelFilename; }
            set { surfaceLevelFilename = value; }
        }
        /// <summary>
        /// Path that tool can write it's output to
        /// </summary>
        public string ToolOutputPath
        {
            get { return toolOutputPath; }
            set
            {
                toolOutputPath = value;
                //if (!Directory.Exists(Path.GetDirectoryName(toolOutputPath)))
                //{
                //    Directory.CreateDirectory(Path.GetDirectoryName(toolOutputPath));
                //}
            }
        }

        public Model()
        {
            packages = new List<Package>();
            kperDictionary = new Dictionary<string, int>();
        }

        public void AddSnameKperPair(string SNAME, int kper)
        {
            kperDictionary.Add(SNAME, kper);
        }

        public void AddPackage(Package package)
        {
            packages.Add(package);
        }

        public bool HasActivePackage(string packageKey)
        {
            Package package = GetPackage(packageKey);
            return ((package != null) && (package.IsActive));
        }

        public Package GetPackage(string packageKey)
        {
            for (int i = 0; i < packages.Count; i++)
            {
                if (packages[i].HasKeyMatch(packageKey))
                {
                    return packages[i];
                }
            }
            return null;
        }

        /// <summary>
        /// returns bounding box extent of all submodels, including buffer
        /// </summary>
        /// <returns></returns>
        public Extent GetExtent()
        {
            Extent extent = new Extent();
            extent.llx = float.MaxValue;
            extent.lly = float.MaxValue;
            extent.urx = float.MinValue;
            extent.ury = float.MinValue;
            for (int i = 0; i < submodels.Length; i++)
            {
                float buffer = submodels[i].BUFFER;
                if (submodels[i].XMIN - buffer < extent.llx)
                {
                    extent.llx = submodels[i].XMIN - buffer;
                }
                if (submodels[i].YMIN - buffer < extent.lly)
                {
                    extent.lly = submodels[i].YMIN - buffer;
                }
                if (submodels[i].XMAX + buffer > extent.llx)
                {
                    extent.urx = submodels[i].XMAX + buffer;
                }
                if (submodels[i].YMAX + buffer > extent.ury)
                {
                    extent.ury = submodels[i].YMAX + buffer;
                }
            }

            return extent;
        }

        internal Package GetPackage(object defaultKey)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// returns minimum cellsize of all submodels
        /// </summary>
        /// <returns></returns>
        public float GetMinCSize()
        {
            float mincsize = float.MaxValue;
            for (int i = 0; i < submodels.Length; i++)
            {
                if (submodels[i].CSIZE < mincsize)
                {
                    mincsize = submodels[i].CSIZE;
                }
            }

            return mincsize;
        }

        /// <summary>
        /// returns maximum cellsize of all submodels
        /// </summary>
        /// <returns></returns>
        public float GetMaxCSize()
        {
            float maxcsize = float.MinValue;
            for (int i = 0; i < submodels.Length; i++)
            {
                if (submodels[i].CSIZE > maxcsize)
                {
                    maxcsize = submodels[i].CSIZE;
                }
            }

            return maxcsize;
        }

        /// <summary>
        /// returns bounding box extent of all packages
        /// </summary>
        /// <returns></returns>
        public Extent GetPackageExtent()
        {
            Extent extent = new Extent();
            extent.llx = float.MaxValue;
            extent.lly = float.MaxValue;
            extent.urx = float.MinValue;
            extent.ury = float.MinValue;
            int idfFileCount = 0;
            for (int packageIdx = 0; packageIdx < packages.Count; packageIdx++)
            {
                if (packages[packageIdx] is IDFPackage)
                {
                    IDFPackage idfFilePackage = (IDFPackage)packages[packageIdx];
                    if (idfFilePackage != null)
                    {
                        for (int entryIdx = 0; entryIdx < idfFilePackage.GetEntryCount(); entryIdx++)
                        {
                            IDFFile idfFile = idfFilePackage.GetIDFFile(entryIdx);
                            if (idfFile != null)
                            {
                                idfFileCount++;
                                if (idfFile.Extent.llx < extent.llx)
                                {
                                    extent.llx = idfFile.Extent.llx;
                                }
                                if (idfFile.Extent.lly < extent.lly)
                                {
                                    extent.lly = idfFile.Extent.lly;
                                }
                                if (idfFile.Extent.urx > extent.urx)
                                {
                                    extent.urx = idfFile.Extent.urx;
                                }
                                if (idfFile.Extent.ury > extent.ury)
                                {
                                    extent.ury = idfFile.Extent.ury;
                                }
                            }
                        }
                    }
                }
            }

            if (idfFileCount > 0)
            {
                return extent;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// returns maximum cellsize of all packagefiles
        /// </summary>
        /// <returns></returns>
        public float GetMaxPackageCellSize()
        {
            float maxcsize = 0;
            for (int packageIdx = 0; packageIdx < packages.Count; packageIdx++)
            {
                if (packages[packageIdx] is IDFPackage)
                {
                    IDFPackage idfFilePackage = (IDFPackage)packages[packageIdx];
                    if (idfFilePackage != null)
                    {
                        for (int kper = 0; kper <= NPER; kper++)
                        {
                            for (int entryIdx = 0; entryIdx < idfFilePackage.GetEntryCount(kper); entryIdx++)
                            {
                                for (int partIdx = 0; partIdx < idfFilePackage.MaxPartCount; partIdx++)
                                {
                                    IDFFile idfFile = idfFilePackage.GetIDFFile(entryIdx, partIdx, kper);
                                    if (idfFile != null)
                                    {
                                        if (!(idfFile is ConstantIDFFile) && (idfFile.XCellsize > maxcsize))
                                        {
                                            maxcsize = idfFile.XCellsize;
                                        }
                                        if (!(idfFile is ConstantIDFFile) && (idfFile.YCellsize > maxcsize))
                                        {
                                            maxcsize = idfFile.YCellsize;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return maxcsize;
        }

        /// <summary>
        /// returns minimum cellsize of all packagefiles
        /// </summary>
        /// <returns></returns>
        public float GetMinPackageCellSize()
        {
            float mincsize = float.MaxValue;
            for (int packageIdx = 0; packageIdx < packages.Count; packageIdx++)
            {
                if (packages[packageIdx] is IDFPackage)
                {
                    IDFPackage idfFilePackage = (IDFPackage)packages[packageIdx];
                    if (idfFilePackage != null)
                    {
                        for (int kper = 0; kper <= NPER; kper++)
                        {
                            for (int entryIdx = 0; entryIdx < idfFilePackage.GetEntryCount(kper); entryIdx++)
                            {
                                for (int partIdx = 0; partIdx < idfFilePackage.MaxPartCount; partIdx++)
                                {
                                    IDFFile idfFile = idfFilePackage.GetIDFFile(entryIdx, partIdx, kper);
                                    if (idfFile != null)
                                    {
                                        if (idfFile.XCellsize < mincsize)
                                        {
                                            mincsize = idfFile.XCellsize;
                                        }
                                        if (idfFile.YCellsize < mincsize)
                                        {
                                            mincsize = idfFile.YCellsize;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            return mincsize;
        }

        public Model Copy()
        {
            Model newModel = new Model();
            throw new Exception("Model.Copy is not yet supported");
            //newModel.layers = new Layer[this.layers.Length];

            //for (int i = 0; i < layers.Length; i++)
            //{
            //    newModel.layers[i] = new Layer();
            //    newModel.layers[i].idffile = layers[i].idffile.Copy(layers[i].idffile.filename);
            //}

            // return newModel;
        }

        public void Write(string outputFoldername)
        {
            throw new Exception("Model.Write is not yet supported");
            //for (int i = 0; i < layers.Length; i++)
            //{
            //    layers[i].idffile.filename = outputFoldername + Path.DirectorySeparatorChar + Path.GetFileName(layers[i].idffile.filename);
            //    layers[i].idffile.WriteIDFFile();
            //}
        }

        /// <summary>
        /// Releases all memory of lazy loaded content of the files within the packages
        /// </summary>
        public void ReleaseAllPackageMemory(Log log = null)
        {
            long memoryBefore = GC.GetTotalMemory(true);
            foreach (Package package in Packages)
            {
                package.ReleaseMemory(false);
            }
            GC.Collect();
            GC.WaitForFullGCComplete(-1);
            if (log != null)
            {
                long memoryAfter = GC.GetTotalMemory(true);
                log.AddMessage(LogLevel.Debug, ((memoryBefore - memoryAfter) / 1000000) + "Mb memory is released", 1);
            }
        }

        public static DateTime GetStressPeriodDate(DateTime startDate, int kper)
        {
            return ((DateTime)startDate).Add(new TimeSpan(kper - 1, 0, 0, 0));
        }

        public static string GetStressPeriodString(DateTime? startDate, int kper)
        {
            if ((kper > 0) && (startDate != null))
            {
                return ((DateTime)startDate).Add(new TimeSpan(kper - 1, 0, 0, 0)).ToString("yyyyMMdd");
            }
            else
            {
                return "steady-state";
            }
        }

        public int GetKPER(string stressPeriodString)
        {
            int stressPeriodInt;
            if (!int.TryParse(stressPeriodString, out stressPeriodInt))
            {
                if (stressPeriodString.ToLower().Equals("steady-state"))
                {
                    return 0;
                }
                else
                {
                    throw new Exception("Unknown stressperiodstring: " + stressPeriodString);
                }
            }

            if (StartDate != null)
            {
                try
                {
                    if (stressPeriodString.Length != 8)
                    {
                        throw new Exception("Stressperiodstring has not format yyyymmmdd: " + stressPeriodString);
                    }
                    int year = int.Parse(stressPeriodString.Substring(0, 4));
                    int month = int.Parse(stressPeriodString.Substring(4, 2));
                    int day = int.Parse(stressPeriodString.Substring(6, 2));
                    DateTime date = new DateTime(year, month, day);
                    DateTime sdate = (DateTime)StartDate;
                    TimeSpan ts = date.Subtract(sdate);
                    return ts.Days + 1;
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not parse date for string " + stressPeriodString, ex);
                }
            }
            else
            {
                if (kperDictionary.ContainsKey(stressPeriodString))
                {
                    return kperDictionary[stressPeriodString];
                }
                else
                {
                    throw new Exception("Model startdate is not defined and could not find " + stressPeriodString + " in runfile. KPER could not be found.");
                }
            }
        }

        public Metadata CreateMetadata()
        {
            return CreateMetadata(null, null);
        }

        public Metadata CreateMetadata(string description, string source = null)
        {
            Metadata metadata = CreateDefaultMetadata();
            metadata.Modelversion = this.runfilename;
            if (description != null)
            {
                metadata.Description = description;
            }
            if (source != null)
            {
                metadata.Source = source;
            }
            return metadata;
        }

        public static Metadata CreateDefaultMetadata()
        {
            Metadata metadata = new Metadata();
            metadata.Version = "1.0";
            metadata.Modelversion = "-";
            metadata.Source = "-";
            metadata.Producer = "iMODValidator";
            metadata.ProcessDescription = "Automatically generated by iMODValidator";
            metadata.Scale = "-";
            metadata.Unit = "-";
            metadata.Resolution = "-";
            metadata.Organisation = "Sweco Nederland B.V.";
            metadata.Website = "http://www.sweco.nl/ons-aanbod/water/?service=Waterbeheer";
            metadata.Contact = "-";
            metadata.Emailaddress = "info@sweco.nl";
            return metadata;
        }

        public static Metadata CreateDefaultMetadata(IMODFile imodFile, string description)
        {
            Metadata metadata = CreateDefaultMetadata();
            metadata.IMODFilename = Path.GetFileName(imodFile.Filename);
            metadata.Location = Path.GetDirectoryName(imodFile.Filename);
            metadata.PublicationDate = DateTime.Now;
            metadata.Description = description;
            return metadata;
        }

        /// <summary>
        /// Retrieves surface level IDF file that is defined for model, or null if not defined
        /// </summary>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns>surfacelevel IDF file for model or null if not found</returns>
        public IDFFile RetrieveSurfaceLevelFile(Log log = null, int logIndentLevel = 0)
        {
            IDFFile surfacelevelIDFFile = null;
            if (surfaceLevelFilename != null)
            {
                if (!(surfaceLevelFilename.Trim().Equals(string.Empty)))
                {
                    surfacelevelIDFFile = IDFFile.ReadFile(surfaceLevelFilename, false, log, logIndentLevel);
                }
            }
            return surfacelevelIDFFile;
        }

    }
}
