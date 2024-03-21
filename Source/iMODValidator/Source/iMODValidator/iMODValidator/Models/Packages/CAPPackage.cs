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
using Sweco.SIF.iMODValidator.Models.Packages.Files;
using Sweco.SIF.iMODValidator.Models.Runfiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Models.Packages
{
    public enum CAPEntryCode
    {
        BND,    // boundary settings, metaswap elements are removed from BND<0
        LUSE,   // landuse code, represented LUSE_SVAT.INP
        RTZ,    // rootzone thickness
        SFU,    // soil Physical Unit
        MET,    // meteo station number
        SEV,    // surface elevation
        ARR,    // ARtificial recharge type (irrigation) (if ARMWP=0)
        ART,    // location of Artificial REcharge (if ARMWP=1)
        ARL1,   // Artificial Recharge location, number of model layer from which water is extracted (if ARMWP=0)
        ARL2,   // IPF-file with locations for artificial recharge (if ARMWP=1)
        ARC,    // Artificial Recharge Capacity (if ARMWP=0)
        WTA,    // Wetted Area
        UBA,    // Urban Area
        PDU,    // Ponding Depth Urban Area
        PDR,    // Ponding Depth Rural Area
        OFU,    // Runoff Resistance Urban Area
        OFR,    // Runoff Resistance Rural Area
        ONU,    // Runon Resistance Urban Area
        ONR,    // Runon Resistance Rural Area
        QIU,    // QINFBASIC Urban Area, infiltration capacity of soil surface
        QIR,    // QINFBASIC Rural Area
        PWT,    // Perched Water Table Level
        SMF,    // Soil Moisture Factor (adjusting soil moisture coefficient)
        CFC,    // Conductivity Factor (adjusting vertical conductivity)
        PLN,    // Plot Number from a steering point of SLO
        SLO,    // Steering Location
        PDLev,  // Plot Drainage Level
        PDRes,  // Plot Drainage Resistance
    }

    public class CAPPackage : Package
    {
        public const string ExtraFilesSubString = "EXTRA FILES";
        public List<string> ExtraFilenames { get; protected set; }

        public static string DefaultKey
        {
            get { return "CAP"; }
        }

        public override int MaxPartCount
        {
            get
            {
                return 22;   // CAP-package has 22 files per entry
            }
        }

        /// <summary>
        /// Abbreviations for each of the individual parts/files of this package
        /// </summary>
        protected static string[] partAbbreviations = null;
        public override string[] PartAbbreviations
        {
            get
            {
                if (partAbbreviations == null)
                {
                    partAbbreviations = new string[MaxPartCount];
                    for (int i = 0; i < MaxPartCount; i++)
                    {
                        partAbbreviations[i] = GetCAPEntryCode(i).ToString();
                    }
                }

                return partAbbreviations;
            }
        }

        public CAPPackage(string packageKey) : base(packageKey)
        {
            alternativeKeys.AddRange(new string[] { "CAPSIM" });
        }

        public override Package CreateInstance()
        {
            return new CAPPackage(key);
        }

        public static int GetEntryIdx(CAPEntryCode capEntryCode, string extension = ".run", IMODVersion iMODversion = IMODVersion.v5)
        {
            switch (extension.ToLower())
            {
                case ".run":
                    return GetRunFileEntryIdx(capEntryCode, iMODversion);
                default:
                    throw new Exception("Extension: " + extension + " is not yet supported.");
            }
        }

        public static int GetRunFileEntryIdx(CAPEntryCode capEntryCode, IMODVersion iMODversion)
        {
            switch (iMODversion)
            {
                case IMODVersion.v5:
                    return GetRunFileV5EntryIdx(capEntryCode);
                default:
                    throw new Exception("IMOD version: " + iMODversion + " is not yet supported.");

            }
        }

        public static int GetRunFileV5EntryIdx(CAPEntryCode capEntryCode)
        {
            switch (capEntryCode)
            {
                case CAPEntryCode.BND:
                    return 0;
                case CAPEntryCode.LUSE:
                    return 1;
                case CAPEntryCode.RTZ:
                    return 2;
                case CAPEntryCode.SFU:
                    return 3;
                case CAPEntryCode.MET:
                    return 4;
                case CAPEntryCode.SEV:
                    return 5;
                case CAPEntryCode.ARR:
                    return 6;
                case CAPEntryCode.ART:
                    return 6;
                case CAPEntryCode.ARL1:
                    return 7;
                case CAPEntryCode.ARL2:
                    return 7;
                case CAPEntryCode.ARC:
                    return 7;
                case CAPEntryCode.WTA:
                    return 8;
                case CAPEntryCode.UBA:
                    return 9;
                case CAPEntryCode.PDU:
                    return 10;
                case CAPEntryCode.PDR:
                    return 11;
                case CAPEntryCode.OFU:
                    return 12;
                case CAPEntryCode.OFR:
                    return 13;
                case CAPEntryCode.ONU:
                    return 14;
                case CAPEntryCode.ONR:
                    return 15;
                case CAPEntryCode.QIU:
                    return 16;
                case CAPEntryCode.QIR:
                    return 17;
                case CAPEntryCode.PWT:
                    return 18;
                case CAPEntryCode.SMF:
                    return 19;
                case CAPEntryCode.CFC:
                    return 20;
                case CAPEntryCode.PLN:
                    return 21;
                case CAPEntryCode.SLO:
                    return 22;
                case CAPEntryCode.PDLev:
                    return 23;
                case CAPEntryCode.PDRes:
                    return 24;
                default:
                    throw new Exception("CAP-package entry code: " + capEntryCode + " does not exist for iMODversion v5 RUN-file");
            }
        }

        public static CAPEntryCode GetCAPEntryCode(int capEntryIdx)
        {
            switch (capEntryIdx)
            {
                case 0:
                    return CAPEntryCode.BND;
                case 1:
                    return CAPEntryCode.LUSE;
                case 2:
                    return CAPEntryCode.RTZ;
                case 3:
                    return CAPEntryCode.SFU;
                case 4:
                    return CAPEntryCode.MET;
                case 5:
                    return CAPEntryCode.SEV;
                case 6:
                    return CAPEntryCode.ARR;
                    // return CAPEntryCode.ART;
                case 7:
                    // return CAPEntryCode.ARL1;
                    // return CAPEntryCode.ARL2;
                    return CAPEntryCode.ARC;
                case 8:
                    return CAPEntryCode.WTA;
                case 9:
                    return CAPEntryCode.UBA;
                case 10:
                    return CAPEntryCode.PDU;
                case 11:
                    return CAPEntryCode.PDR;
                case 12:
                    return CAPEntryCode.OFU;
                case 13:
                    return CAPEntryCode.OFR;
                case 14:
                    return CAPEntryCode.ONU;
                case 15:
                    return CAPEntryCode.ONR;
                case 16:
                    return CAPEntryCode.QIU;
                case 17:
                    return CAPEntryCode.QIR;
                case 18:
                    return CAPEntryCode.PWT;
                case 19:
                    return CAPEntryCode.SMF;
                case 20:
                    return CAPEntryCode.CFC;
                case 21:
                    return CAPEntryCode.PLN;
                case 22:
                    return CAPEntryCode.SLO;
                case 23:
                    return CAPEntryCode.PDLev;
                case 24:
                    return CAPEntryCode.PDRes;
                default:
                    throw new Exception("Undefined CAP-package entry index: " + capEntryIdx + " for iMODversion v5 RUN-file");
            }
        }

        public string GetExtraFilename(string subString)
        {
            foreach (string extraFilename in ExtraFilenames)
            {
                if (extraFilename.ToLower().Contains(subString.ToLower()))
                {
                    return extraFilename.ToLower();
                }
            }
            throw new Exception("Extra filename: " + subString + " is not found in CAP-package within runfile");
        }

        public override void ParseRUNFilePackageFiles(RUNFile runfile, int entryCount, Log log, int logIndentLevel, StressPeriod sp = null)
        {
            string wholeLine;
            string[] lineParts;
            ExtraFilenames = new List<string>();

            if (entryCount > 0)
            {
                for (int entryIdx = 0; entryIdx < entryCount; entryIdx++)
                {
                    // FCT,IMP,FNAME or just FNAME
                    wholeLine = runfile.RemoveWhitespace(runfile.ReadLine());
                    lineParts = wholeLine.Split(new char[] { ',' });

                    if (lineParts.Length == 1)
                    {
                        // add the single file to the package (and model) with dummy ilay, period, etc.
                        string fname = lineParts[0].Replace("\"", "");
                        ExtraFilenames.Add(fname);
                    }
                    else
                    {
                        if (lineParts.Length != 3)
                        {
                            log.AddError(Key, model.RUNFilename, "Unexpected parameter count in " + Key + "-package for input file assignment: " + wholeLine, logIndentLevel);
                        }
                        else
                        {
                            // simply add the file to the package (and model), also add non-existent file to keep order for adding the same
                            string fname = lineParts[2].Replace("\"", "").Replace("'", "");
                            AddFile(1, float.Parse(lineParts[0], englishCultureInfo), float.Parse(lineParts[1], englishCultureInfo), fname, 1);
                            log.AddMessage(LogLevel.Trace, "Added file " + entryIdx + " to package " + Key + ": " + fname, logIndentLevel);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Read/parse entries in PRJ-file this package for specified stress period.
        /// Note: after finishing, first line from the next package is not yet read.
        /// </summary>
        /// <param name="prjFile"></param>
        /// <param name="kper"></param>
        /// <param name="NSUB"></param>
        /// <param name="NSYSTEM"></param>
        /// <param name="maxKPER"></param>
        /// <param name="stressPeriod"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        protected override void ParsePRJFilePackageStressPeriodFiles(PRJFile prjFile, int NSUB, int NSYSTEM, int maxKPER, StressPeriod stressPeriod, Log log, int logIndentLevel)
        {
            base.ParsePRJFilePackageStressPeriodFiles(prjFile, NSUB, NSYSTEM, maxKPER, stressPeriod, log, logIndentLevel);

            ExtraFilenames = new List<string>();
            string wholeLine = prjFile.PeekLine();
            if (!wholeLine.Contains(ExtraFilesSubString))
            {
                log.AddError("Missing 'NFILES'," + ExtraFilesSubString + "'-line in " + Key + "-package", logIndentLevel);
                return;
            }

            wholeLine = prjFile.RemoveWhitespace(prjFile.ReadLine().Trim());
            int commentIdx = wholeLine.IndexOf("###");
            wholeLine = (commentIdx > 0) ? wholeLine.Substring(0, commentIdx) : wholeLine;
            string[] lineParts = prjFile.Split(wholeLine, new char[] { ',' });
            if (lineParts.Length != 2)
            {
                log.AddError("Invalid 'NFILES'," + ExtraFilesSubString + "'-line in " + Key + "-package", logIndentLevel);
                return;
            }

            int extraFileCount = int.Parse(lineParts[0]);
            for (int fileIdx = 0; fileIdx < extraFileCount; fileIdx++)
            {
                wholeLine = prjFile.RemoveWhitespace(prjFile.ReadLine().Trim());
                // add the single file to the package(and model) with dummy ilay, period, etc.
                string fname = wholeLine.Replace("\"", string.Empty).Replace("'", string.Empty);
                ExtraFilenames.Add(fname);
            }
        }
    }
}
