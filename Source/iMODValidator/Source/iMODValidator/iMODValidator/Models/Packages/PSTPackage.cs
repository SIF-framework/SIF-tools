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
using Sweco.SIF.iMODValidator.Models.Runfiles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Models.Packages
{
    public class PSTPackage : IDFPackage
    {
        public static string DefaultKey
        {
            get { return "PST"; }
        }

        public PSTPackage(string packageKey) : base(packageKey)
        {
        }

        public override void ParseRunfilePackageFiles(Runfile runfile, int fileCount, Log log, StressPeriod sp = null)
        {
            string[] lineParts;

            // for now just skip all definition lines

            // parse data set 14: PE_MXITER,PE_STOP,PE_SENS,PE_NPERIOD,PE_NBATCH, PE_TARGET(.),PE_SCALING,PE_PADJ,PE_DRES,PE_KTYPE
            string line14 = runfile.RemoveWhitespace(runfile.ReadLine());
            lineParts = line14.Split(new char[] { ',' });
            int PE_NPERIOD = 0;
            if (!int.TryParse(lineParts[3], out PE_NPERIOD))
            {
                log.AddError("Invalid PE_NPERIOD parameter, an integer is expected: " + lineParts[3]);
            }
            int PE_NBATCH = 0;
            if (!int.TryParse(lineParts[4], out PE_NBATCH))
            {
                log.AddError("Invalid PE_NBATCH parameter, an integer is expected: " + lineParts[4]);
            }

            // parse data set 15: Period Settings (S_PERIOD,E_PERIOD)
            for (int i = 0; i < PE_NPERIOD; i++)
            {
                string line15 = runfile.ReadLine();
            }

            // parse data set 16: Batch Settings
            for (int i = 0; i < PE_NBATCH; i++)
            {
                string line15 = runfile.ReadLine();
            }
            // parse data set 17, Parameters: PACT,PPARAM,PILS,PIZONE,PINI,PDELTA,PMIN,PMAX,PINCREASE 
            for (int i = 0; i < fileCount; i++)
            {
                string line16 = runfile.ReadLine();
            }

            // parse data set 18: NZONES
            string line17 = runfile.RemoveWhitespace(runfile.ReadLine());
            int NZONES = int.Parse(line17);

            // parse data set 19: Parameter Estimation – Zone Definition 
            for (int i = 0; i < NZONES; i++)
            {
                string line16 = runfile.ReadLine();
            }
        }

        public override Package CreateInstance()
        {
            return new PSTPackage(key);
        }
    }
}
