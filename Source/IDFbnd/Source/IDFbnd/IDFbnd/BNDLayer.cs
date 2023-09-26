// IDFbnd is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFbnd.
// 
// IDFbnd is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFbnd is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFbnd. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.IDF;

namespace Sweco.SIF.IDFbnd
{
    /// <summary>
    /// Class for storing boundary layer and related information
    /// </summary>
    public class BNDLayer
    {
        public string BNDFilename { get; protected set; }
        public IDFFile BNDIDFFile { get; protected set; }

        public BNDLayer(string bndFilename)
        {
            this.BNDFilename = bndFilename;
            this.BNDIDFFile = IDFFile.ReadFile(BNDFilename);
        }

        /// <summary>
        /// Enlarge IDF-file if specified extent is larger
        /// </summary>
        /// <param name="extent"></param>
        public virtual void Enlarge(Extent extent)
        {
            if (!BNDIDFFile.Extent.Contains(extent))
            {
                BNDIDFFile = BNDIDFFile.EnlargeIDF(extent);
            }
        }
    }
}
