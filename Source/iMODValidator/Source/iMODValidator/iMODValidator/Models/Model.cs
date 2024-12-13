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
        public DateTime? StartDate;

        /// <summary>
        /// List will all available stressperiods for this model
        /// </summary>
        public List<StressPeriod> StressPeriods { get; set; }

        /// <summary>
        /// Dictionary with SNAME-StressPeriod pairs
        /// </summary>
        private Dictionary<string, StressPeriod> snameDictionary;
        /// <summary>
        /// Dictionary with KPER-StressPeriod pairs
        /// </summary>
        private Dictionary<int, StressPeriod> kperDictionary;
        /// <summary>
        /// Dictionary with date-StressPeriod pairs
        /// </summary>
        private Dictionary<DateTime, StressPeriod> dateDictionary;

        /// <summary>
        /// First free KPER-value to use for next SNAME in dictionary. KPER's are used in definition of stressperiods in RUN-file and to store stress periode SNAME-strings in an array
        /// </summary>
        private int nextKPER;

        /// <summary>
        /// Dictionary with all currently defined periods for this model, which maps the name of the period to the corresponding start and end date
        /// </summary>
        Dictionary<string, DateTime?> periodDictionary;

        /// <summary>
        /// The outputpath from the runfile that the model results are written to
        /// </summary>
        public string ModelresultsPath { get; set; }
        public string RUNFilename { get; set; }
        public List<Package> Packages { get; set; }
        public SubModel[] Submodels { get; set; }

        public int NLAY { get; set; }
        public int MXNLAY { get; set; }
        public int NPER { get; set; }

        /// <summary>
        /// Specify ISAVEENDDATE=1 to save each ﬁle with a time stamp equal to the end of the corresponding stress period (and/or time step). 
        /// By default ISAVEENDDATE=0 and the time stamp will be equal to the start date of each stress period (and/or time step). 
        /// Note This keyword was obsolete since v3.0 and had a different purposes at that time. Be careful whenever a runﬁle is used that was compatible for v3.0 or older.
        /// </summary>
        public int ISAVEENDDATE { get; set; }

        public int ICONCHK { get; set; }
        public int IIPF { get; set; }

        public int NSCL { get; set; }
        public int IFTEST { get; set; }
        public int NMULT { get; set; }
        public int IDEBUG { get; set; }
        public int IEXPORT { get; set; }
        public int IPOSWELL { get; set; }
        public int ISCEN { get; set; }
        public int IBDG { get; set; }
        public float MINKD { get; set; }
        public float MINC { get; set; }
        public string BNDFILE { get; set; }
        public string SurfaceLevelFilename { get; set; }

        private long sdate;
        public long SDATE_deprecated
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

        /// <summary>
        /// Path that tool can write it's output to
        /// </summary>
        public string ToolOutputPath { get; set; }

        public Model()
        {
            Packages = new List<Package>();
            nextKPER = 1;
            snameDictionary = new Dictionary<string, StressPeriod>();
            kperDictionary = new Dictionary<int, StressPeriod>();
            dateDictionary = new Dictionary<DateTime, StressPeriod>();
            periodDictionary = new Dictionary<string, DateTime?>();
            StressPeriods = new List<StressPeriod>();
        }

        /// <summary>
        /// Retrieve (and increase) next free KPER
        /// </summary>
        /// <returns></returns>
        public int RetrieveNextKPER()
        {
            // return next free KPER-value
            int kper = nextKPER;
            nextKPER++;
            return kper;
        }

        public bool HasSNAME(string SNAME)
        {
            return snameDictionary.ContainsKey(SNAME);
        }

        public bool HasKPER(int KPER)
        {
            return kperDictionary.ContainsKey(KPER);
        }

        public bool HasDate(DateTime date)
        {
            return dateDictionary.ContainsKey(date);
        }

        /// <summary>
        /// Add specified stress period to model
        /// </summary>
        /// <param name="stressPeriod"></param>
        public void AddStressPeriod(StressPeriod stressPeriod)
        {
            if (stressPeriod != null)
            {
                StressPeriods.Add(stressPeriod);
                snameDictionary.Add(stressPeriod.SNAME, stressPeriod);
                kperDictionary.Add(stressPeriod.KPER, stressPeriod);
                if (stressPeriod.DateTime != null)
                {
                    dateDictionary.Add((DateTime)stressPeriod.DateTime, stressPeriod);
                }
            }
        }

        /// <summary>
        /// Find StressPeriod object for specified SNAME or return null if not found
        /// </summary>
        /// <param name="SNAME"></param>
        /// <returns>StressPeriod object or null if not found</returns>
        public StressPeriod RetrieveStressPeriod(string SNAME)
        {
            if (snameDictionary.ContainsKey(SNAME))
            {
                return snameDictionary[SNAME];
            }
            else if (SNAME.ToUpper().Equals(StressPeriod.SteadyStateSNAME))
            {
                return StressPeriod.SteadyState;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Find StressPeriod object for specified KPER or return null if not found
        /// </summary>
        /// <param name="KPER"></param>
        /// <returns>StressPeriod object or null if not found</returns>
        public StressPeriod RetrieveStressPeriod(int KPER)
        {
            if (KPER == 0)
            {
                return StressPeriod.SteadyState;
            }
            else
            {
                if (kperDictionary.ContainsKey(KPER))
                {
                    return kperDictionary[KPER];
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Find StressPeriod object for specified datetime or return null if not found
        /// </summary>
        /// <param name="datetime"></param>
        /// <returns>StressPeriod object or null if not found</returns>
        public StressPeriod RetrieveStressPeriod(DateTime datetime)
        {
            if (dateDictionary.ContainsKey(datetime))
            {
                return dateDictionary[datetime];
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Find StressPeriod object for specified datetime or return null if not found
        /// </summary>
        /// <param name="datetime"></param>
        /// <returns>StressPeriod object or null if not found</returns>
        public StressPeriod RetrieveStressPeriod(DateTime? datetime)
        {
            if (datetime != null)
            {
                if (dateDictionary.ContainsKey((DateTime)datetime))
                {
                    return dateDictionary[(DateTime)datetime];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return StressPeriod.SteadyState;
            }
        }

        /// <summary>
        /// Find KPER for specified SNAME or return -1 if not found
        /// </summary>
        /// <param name="SNAME"></param>
        /// <returns>KPER-value or -1 if not found</returns>
        public int RetrieveKPER(string SNAME)
        {
            if (SNAME != null)
            {
                if (snameDictionary.ContainsKey(SNAME))
                {
                    return snameDictionary[SNAME].KPER;
                }
                else if (SNAME.ToUpper().Equals(StressPeriod.SteadyStateSNAME))
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }
            else
            {
                return -1;
            }
        }

        /// <summary>
        /// Retrieve SNAME for specified KPER or return null if not found
        /// </summary>
        /// <param name="KPER"></param>
        /// <returns></returns>
        public string RetrieveSNAME(int KPER)
        {
            if (kperDictionary.ContainsKey(KPER))
            {
                return kperDictionary[KPER].SNAME;
            }
            else if (KPER == 0)
            {
                return StressPeriod.SteadyStateSNAME;
            }
            else
            {
                return null;
            }
        }


        public void AddPackage(Package package)
        {
            Packages.Add(package);
        }

        public bool HasActivePackage(string packageKey)
        {
            Package package = GetPackage(packageKey);
            return ((package != null) && (package.IsActive));
        }

        public Package GetPackage(string packageKey)
        {
            for (int i = 0; i < Packages.Count; i++)
            {
                if (Packages[i].HasKeyMatch(packageKey))
                {
                    return Packages[i];
                }
            }
            return null;
        }

        /// <summary>
        /// Checks if this model is a steady-state model
        /// </summary>
        /// <returns></returns>
        public bool IsSteadyStateModel()
        {
            return ((NPER <= 1) && (StartDate == null));
        }

        /// <summary>
        /// returns bounding box extent of all submodels, including buffer
        /// </summary>
        /// <returns>bounding box extent or null if no submodels are defined</returns>
        public Extent GetExtent()
        {
            Extent extent = null;

            if ((Submodels != null) && (Submodels.Length > 0))
            {
                extent = new Extent();
                extent.llx = float.MaxValue;
                extent.lly = float.MaxValue;
                extent.urx = float.MinValue;
                extent.ury = float.MinValue;
                for (int i = 0; i < Submodels.Length; i++)
                {
                    float buffer = Submodels[i].BUFFER;
                    if (Submodels[i].XMIN - buffer < extent.llx)
                    {
                        extent.llx = Submodels[i].XMIN - buffer;
                    }
                    if (Submodels[i].YMIN - buffer < extent.lly)
                    {
                        extent.lly = Submodels[i].YMIN - buffer;
                    }
                    if (Submodels[i].XMAX + buffer > extent.llx)
                    {
                        extent.urx = Submodels[i].XMAX + buffer;
                    }
                    if (Submodels[i].YMAX + buffer > extent.ury)
                    {
                        extent.ury = Submodels[i].YMAX + buffer;
                    }
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
            for (int i = 0; i < Submodels.Length; i++)
            {
                if (Submodels[i].CSIZE < mincsize)
                {
                    mincsize = Submodels[i].CSIZE;
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
            for (int i = 0; i < Submodels.Length; i++)
            {
                if (Submodels[i].CSIZE > maxcsize)
                {
                    maxcsize = Submodels[i].CSIZE;
                }
            }

            return maxcsize;
        }

        /// <summary>
        /// returns maximum layer number of all packages
        /// </summary>
        /// <returns>maximum layer or -1 if no package files are found</returns>
        public int GetMaxLayer()
        {
            int maxlayer = -1;
            if (Packages != null)
            {
                for (int packageIdx = 0; packageIdx < Packages.Count; packageIdx++)
                {
                    Package package = Packages[packageIdx];
                    int maxPackageLayer = package.GetMaxLayer();
                    if (maxPackageLayer > maxlayer)
                    {
                        maxlayer = maxPackageLayer;
                    }
                }
            }

            return maxlayer;
        }

        /// <summary>
        /// returns maximum KPER of all packages
        /// </summary>
        /// <returns>maximum KPER or -1 if no package files are found</returns>
        public int GetMaxKPER()
        {
            int maxKPER = -1;
            for (int packageIdx = 0; packageIdx < Packages.Count; packageIdx++)
            {
                Package package = Packages[packageIdx];
                int maxPackageKPER = package.GetMaxKPER();
                if (maxPackageKPER > maxKPER)
                {
                    maxKPER = maxPackageKPER;
                }
            }

            return maxKPER;
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
            for (int packageIdx = 0; packageIdx < Packages.Count; packageIdx++)
            {
                if (Packages[packageIdx] is IDFPackage)
                {
                    IDFPackage idfFilePackage = (IDFPackage)Packages[packageIdx];
                    if (idfFilePackage != null)
                    {
                        for (int entryIdx = 0; entryIdx < idfFilePackage.GetEntryCount(); entryIdx++)
                        {
                            IDFFile idfFile = idfFilePackage.GetIDFFile(entryIdx);
                            if ((idfFile != null) && !(idfFile is ConstantIDFFile))
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
            for (int packageIdx = 0; packageIdx < Packages.Count; packageIdx++)
            {
                if (Packages[packageIdx] is IDFPackage)
                {
                    IDFPackage idfFilePackage = (IDFPackage)Packages[packageIdx];
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
            for (int packageIdx = 0; packageIdx < Packages.Count; packageIdx++)
            {
                if (Packages[packageIdx] is IDFPackage)
                {
                    IDFPackage idfFilePackage = (IDFPackage)Packages[packageIdx];
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

        public static DateTime CalculateDate(DateTime startDate, int days)
        {
            return ((DateTime)startDate).Add(new TimeSpan(days, 0, 0, 0));
        }

        /// <summary>
        /// Create SNAME string from specified date and format
        /// </summary>
        /// <param name="dateTime"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string CreateSNAME(DateTime? startDate, string format = "yyyyMMdd")
        {
            return ((DateTime)startDate).ToString(format);
        }

        /// <summary>
        /// Create SNAME string from startdate and number of days
        /// </summary>
        /// <param name="startDate"></param>
        /// <param name="days"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string CreateSNAME(DateTime? startDate, int days, string format = "yyyyMMdd")
        {
            return ((DateTime)startDate).Add(new TimeSpan(days - 1, 0, 0, 0)).ToString(format);
        }

        public Metadata CreateMetadata()
        {
            return CreateMetadata(null, null);
        }

        public Metadata CreateMetadata(string description, string source = null)
        {
            Metadata metadata = CreateDefaultMetadata();
            metadata.Modelversion = this.RUNFilename;
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
            if (SurfaceLevelFilename != null)
            {
                if (!(SurfaceLevelFilename.Trim().Equals(string.Empty)))
                {
                    surfacelevelIDFFile = IDFFile.ReadFile(SurfaceLevelFilename, false, log, logIndentLevel);
                }
            }
            return surfacelevelIDFFile;
        }

        public void AddPeriod(string period)
        {
            if (!periodDictionary.ContainsKey(period))
            {
                // Add period without definition
                periodDictionary.Add(period, null);
            }
        }

        public bool HasPeriod(string period)
        {
            return (periodDictionary.ContainsKey(period));
        }

        public List<string> RetrieveUndefinedPeriods()
        {
            List<string> periods = new List<string>();
            foreach (string period in periodDictionary.Keys)
            {
                if (periodDictionary[period] == null)
                {
                    periods.Add(period);
                }
            }

            return periods;
        }

        /// <summary>
        /// Add definition for period. If period is not present yet, it is added
        /// </summary>
        /// <param name="period"></param>
        /// <param name="date"></param>
        public void AddPeriodDefinition(string period, DateTime date)
        {
            AddPeriod(period);
            periodDictionary[period] = date;
        }
    }
}
