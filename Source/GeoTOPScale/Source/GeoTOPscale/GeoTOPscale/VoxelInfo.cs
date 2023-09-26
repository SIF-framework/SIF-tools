// GeoTOPScale is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of GeoTOPScale.
// 
// GeoTOPScale is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// GeoTOPScale is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with GeoTOPScale. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.IDF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.GeoTOPscale
{
    public class VoxelInfo
    {
        public string[] StratFilenames { get; protected set; }
        public string[] LithoFilenames { get; protected set; }
        public float Thickness { get; protected set; }
        public float XCellSize { get; protected set; }
        public float YCellSize { get; protected set; }
        public Extent Extent { get; protected set; }

        public VoxelInfo(string stratPath, string lithoPath)
        {
            StratFilenames = Directory.GetFiles(stratPath);
            LithoFilenames = Directory.GetFiles(lithoPath);

            // Determine voxel thickness; assume same thickness for all voxels in litho and stratigraphy
            IDFFile stratIDFFile = IDFFile.ReadFile(StratFilenames[0], true);
            Thickness = stratIDFFile.TOPLevel - stratIDFFile.BOTLevel;
            XCellSize = stratIDFFile.XCellsize;
            YCellSize = stratIDFFile.YCellsize;
            Extent = stratIDFFile.Extent;
        }
    }
}
