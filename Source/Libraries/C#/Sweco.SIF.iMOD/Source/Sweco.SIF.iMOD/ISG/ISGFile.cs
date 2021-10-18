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
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.ISG;
using Sweco.SIF.iMOD.Legends;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.ISG
{
    /// <summary>
    /// Class to read and analyze ISG-files. See iMOD-manual for details of ISG-files: https://oss.deltares.nl/nl/web/imod/user-manual.
    /// </summary>
    public class ISGFile : IMODFile, IEquatable<ISGFile>
    {
        /// <summary>
        /// Extension string for this type of iMOD-file
        /// </summary>
        public override string Extension
        {
            get { return "ISG"; }
        }

        /// <summary>
        /// Specifies that the actual values are only loaded at first access
        /// </summary>
        protected bool useLazyLoading;

        /// <summary>
        /// Specifies that the actual values are only loaded at first access
        /// </summary>
        public override bool UseLazyLoading
        {
            get { return useLazyLoading; }
            set { useLazyLoading = value; }
        }

        /// <summary>
        /// Creates empty ISGFile object
        /// </summary>
        /// <param name="filename"></param>
        public ISGFile(string filename)
        {
            this.Filename = filename;
        }

        /// <summary>
        /// Currently not implemented
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public virtual bool Equals(ISGFile other)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Currently not implemented
        /// </summary>
        public override void ResetValues()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Currently not implemented
        /// </summary>
        /// <param name="isMemoryCollected"></param>
        public override void ReleaseMemory(bool isMemoryCollected = true)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Currently not implemented
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public override Legend CreateLegend(string description)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Currently not implemented
        /// </summary>
        /// <returns></returns>
        public override long RetrieveElementCount()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Currently not implemented
        /// </summary>
        /// <param name="newFilename"></param>
        /// <returns></returns>
        public override IMODFile Copy(string newFilename = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Currently not implemented
        /// </summary>
        /// <param name="newFilename"></param>
        /// <returns></returns>
        public ISGFile CopyISG(string newFilename = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Currently not implemented
        /// </summary>
        /// <param name="extent"></param>
        /// <returns></returns>
        public virtual ISGFile ClipISG(Extent extent)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Currently not implemented
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="useLazyLoading"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        public static ISGFile ReadFile(string filename, bool useLazyLoading = true, Log log = null, int logIndentLevel = 0)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Currently not implemented
        /// </summary>
        /// <param name="metadata"></param>
        public override void WriteFile(Metadata metadata = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Currently not implemented
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="metadata"></param>
        public override void WriteFile(string filename, Metadata metadata = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Currently not implemented
        /// </summary>
        /// <param name="otherIMODFile"></param>
        /// <param name="comparedExtent"></param>
        /// <param name="isNoDataCompared"></param>
        /// <param name="isContentComparisonForced"></param>
        /// <returns></returns>
        public override bool HasEqualContent(IMODFile otherIMODFile, Extent comparedExtent, bool isNoDataCompared, bool isContentComparisonForced = false)
        {
            throw new NotImplementedException();
        }
    }
}
