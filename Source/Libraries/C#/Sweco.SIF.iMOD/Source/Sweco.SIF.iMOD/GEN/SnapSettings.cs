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
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.IPF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.GEN
{
    /// <summary>
    /// Class to define settings for snapping
    /// </summary>
    public class SnapSettings
    {
        /// <summary>
        /// maximum snapping snapping distance
        /// </summary>
        public double SnapTolerance;
        /// <summary>
        /// Specify that a snap distance column and snap distance values are added to a DATFile object for this feature
        /// </summary>
        public bool IsSnapDistanceAdded;
        /// <summary>
        /// The name of the column with the minimally snapped distance for this feature
        /// </summary>
        public string MinSnapDistanceColName;
        /// <summary>
        /// The name of the column with the maximally snapped distance for this feature
        /// </summary>
        public string MaxSnapDistanceColName;
        /// <summary>
        /// Specify that the points that were actually moved are written to an IPFFile
        /// </summary>
        public bool IsSnappedIPFPointAdded;
        /// <summary>
        /// The IPFFile object that the snapped points are added to, including the snapped distance for which a column is added
        /// </summary>
        public IPFFile SnappedToPointsIPFFile;
        /// <summary>
        /// The name of the IPFFile column with the snapped distance
        /// </summary>
        public IPFFile SnappedFromPointsIPFFile;
        /// <summary>
        /// The name of the IPFFile column with the snapped distance
        /// </summary>
        public string SnapDistanceColName;
        /// <summary>
        /// The name of the FeatureId column with the id of the feature that was snapped from/to
        /// </summary>
        public string FeatureIdColName;
        /// <summary>
        /// The name of the IPFFile column with the filename (excluding extension) of the snapped point
        /// </summary>
        public string SnappedFileColName;

        // singleton instance
        private static SnapSettings defaultSnapSettings;

        public static SnapSettings DefaultSnapSettings
        {
            get
            {
                if (defaultSnapSettings == null)
                {
                    defaultSnapSettings = new SnapSettings();
                }
                return defaultSnapSettings;
            }
        }

        public SnapSettings(float snapTolerance = 0.01f, bool isSnapDistanceAdded = true, string MinSnapDistanceColName = "MinSnapDist", string MaxSnapDistanceColName = "MaxSnapDist")
        {
            this.SnapTolerance = snapTolerance;
            this.IsSnapDistanceAdded = isSnapDistanceAdded;
            this.MinSnapDistanceColName = MinSnapDistanceColName;
            this.MaxSnapDistanceColName = MaxSnapDistanceColName;
            this.IsSnappedIPFPointAdded = false;
            this.SnappedToPointsIPFFile = null;
            this.SnapDistanceColName = "SnapDistance";
            this.FeatureIdColName = "FeatureId";
            this.SnappedFileColName = "SnappedFile";
        }
    }
}
