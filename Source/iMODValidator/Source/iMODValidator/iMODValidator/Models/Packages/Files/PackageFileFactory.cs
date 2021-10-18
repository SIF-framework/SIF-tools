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
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Models.Packages.Files
{
    public class PackageFileFactory
    {
        private static CultureInfo cultureInfo = null;
        public static CultureInfo CultureInfo
        {
            get
            {
                if (cultureInfo == null)
                {
                    // use english as a default cultureinfo
                    cultureInfo = new CultureInfo("en-GB", false);
                }
                return cultureInfo;
            }
            set { cultureInfo = value; }
        }

        public static PackageFile CreatePackageFile(Package package, string fname, int ilay, float fct, float imp, StressPeriod stressPeriod = null)
        {
            // todo provide registration-mechanism, for now directly implement the four possibilities

            float constantValue;
            if (float.TryParse(fname, NumberStyles.Any, CultureInfo, out constantValue))
            {
                return new ConstantIDFPackageFile(package, constantValue, ilay, fct, imp, stressPeriod);
            }
            else
            {
                string ext = Path.GetExtension(fname).ToLower();
                switch (ext)
                {
                    case ".idf":
                        return new IDFPackageFile(package, fname, ilay, fct, imp, stressPeriod);
                    case ".ipf":
                        return new IPFPackageFile(package, fname, ilay, fct, imp, stressPeriod);
                    case ".gen":
                        return new GENPackageFile(package, fname, ilay, fct, imp, stressPeriod);
                    case ".isg":
                        return null; // ignore ISG-files
                    case ".inp":
                    default:
                        throw new Exception("Unknown file format: " + ext + " for file " + fname);
                }
            }
        }
    }
}
