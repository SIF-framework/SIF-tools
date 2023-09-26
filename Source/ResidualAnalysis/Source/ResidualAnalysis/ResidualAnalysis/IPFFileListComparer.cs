// ResidualAnalysis is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of ResidualAnalysis.
// 
// ResidualAnalysis is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// ResidualAnalysis is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with ResidualAnalysis. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.IO;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMOD.Utils;

namespace Sweco.SIF.ResidualAnalysis
{
    class IPFFileListComparer : IComparer<IPFFile>
    {

        public int Compare(IPFFile ipfFile1, IPFFile ipfFile2)
        {
            if ((ipfFile1 == null) && (ipfFile2 == null))
            {
                return 0;
            }
            if (ipfFile1 == null)
            {
                return -1;
            }
            if (ipfFile2 == null)
            {
                return 1;
            }

            if ((ipfFile1.Filename == null) && (ipfFile2.Filename == null))
            {
                return 0;
            }
            if (ipfFile1.Filename == null)
            {
                return -1;
            }
            if (ipfFile2.Filename == null)
            {
                return 1;
            }

            int layer1;
            int layer2;
            try
            {
                layer1 = IMODUtils.GetLayerNumber(ipfFile1.Filename);
            }
            catch (Exception)
            {
                // Try to get layernumber as last numeric value in filename
                layer1 = IMODUtils.GetLastNumericValue(ipfFile1.Filename);
            }
            try
            {
                layer2 = IMODUtils.GetLayerNumber(ipfFile2.Filename);
            }
            catch (Exception)
            {
                // Try to get layernumber as last numeric value in filename
                layer2 = IMODUtils.GetLastNumericValue(ipfFile2.Filename);
            }

            return layer1.CompareTo(layer2);
        }

        private int GetLayerFromModelFilename(string filename)
        {
            string filenameWithoutExt = Path.GetFileNameWithoutExtension(filename);

            string numberString = string.Empty;
            int idx = filenameWithoutExt.Length - 1;
            int digit;
            while (int.TryParse(filenameWithoutExt.Substring(idx, 1), out digit))
            {
                numberString = digit.ToString() + numberString;
                idx--;
            }

            return int.Parse(numberString);
        }
    }
}
