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
    public class CAPPackage : IDFPackage
    {
        public const int BNDEntryIdx = 0;
        public const int LGNEntryIdx = 1;
        public const int SurfaceLevelEntryIdx = 5;

        public static string DefaultKey
        {
            get { return "CAP"; }
        }

        public CAPPackage(string packageKey)
            : base(packageKey)
        {
            alternativeKeys.AddRange(new string[] { "CAPSIM" });
        }

        public string GetSurfaceLevelFilename()
        {
            string surfaceLevelFilename = null;
            IDFPackageFile surfaceLevelIDFPackageFile = GetIDFPackageFile(SurfaceLevelEntryIdx);
            if (surfaceLevelIDFPackageFile != null)
            {
                surfaceLevelFilename = surfaceLevelIDFPackageFile.FName;
            }
            return surfaceLevelFilename;
        }

        public override void ParseRunfilePackageFiles(Runfile runfile, int entryCount, Log log, StressPeriod sp = null)
        {
            string wholeLine;
            string[] lineParts;

            if (entryCount > 0)
            {
                for (int entryIdx = 1; entryIdx <= SurfaceLevelEntryIdx + 1; entryIdx++)
                {
                    // FCT,IMP,FNAME or just FNAME
                    wholeLine = runfile.RemoveWhitespace(runfile.ReadLine());
                    lineParts = wholeLine.Split(new char[] { ',' });
                    if (lineParts.Length == 1)
                    {
                        // add the single file to the package (and model) with dummy ilay, period, etc.
                        string fname = lineParts[0].Replace("\"", "");

                        // TODO: add PackageFile support for MetaSWAP .inp and .sim files
                        // AddFile will throw an exception for these files
                        AddFile(1, 1, 0, fname, entryCount);
                    }
                    else
                    {
                        if (lineParts.Length != 3)
                        {
                            log.AddError(Key, model.Runfilename, "Unexpected parameter count in " + Key + "-package for input file assignment: " + wholeLine);
                        }
                        else
                        {
                            // simply add the file to the package (and model), also add non-existent file to keep order for adding the same
                            string fname = lineParts[2].Replace("\"", "").Replace("'", "");
                            AddFile(1, float.Parse(lineParts[0], englishCultureInfo), float.Parse(lineParts[1], englishCultureInfo), fname, entryCount);
                            log.AddMessage(LogLevel.Trace, "Added file " + entryIdx + " to package " + Key + ": " + fname, 1);
                        }
                    }
                }

                // For now skip files below surfacelevelfile
                log.AddInfo("CAP-package lines below line " + SurfaceLevelEntryIdx + " are currently skipped");
                for (int entryIdx = SurfaceLevelEntryIdx + 2; entryIdx <= entryCount; entryIdx++)
                {
                    wholeLine = runfile.RemoveWhitespace(runfile.ReadLine());
                }
            }
        }

        public override Package CreateInstance()
        {
            return new CAPPackage(key);
        }
    }
}
