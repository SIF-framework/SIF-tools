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
using Sweco.SIF.iMOD.IDF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Models.Files
{
    public class IDFUpscaler
    {
        private IDFFile baseIDFFile;
        private UpscaleMethodEnum upscaleMethod;
        private Dictionary<float, IDFFile> scaledIDFFileDictionary = new Dictionary<float, IDFFile>();
        private Extent scaleExtent;
        private string savePath;
        private Log log;
        private int logIndentLevel;
        private Extent currentAlignExtent;

        public UpscaleMethodEnum UpscaleMethod
        {
            get { return upscaleMethod; }
            set { upscaleMethod = value; }
        }

        public Extent ScaleExtent
        {
            get { return scaleExtent; }
            set { scaleExtent = value; }
        }

        public IDFUpscaler(IDFFile idfFile, UpscaleMethodEnum upscaleMethod, string savePath = null, Log log = null, int logIndentLevel = 0)
        {
            Initialize(idfFile, upscaleMethod, idfFile.Extent, savePath, log, logIndentLevel);
        }

        public IDFUpscaler(IDFFile idfFile, UpscaleMethodEnum upscaleMethod, Extent scaleExtent, string savePath = null, Log log = null, int logIndentLevel = 0)
        {
            Initialize(idfFile, upscaleMethod, scaleExtent, savePath, log, logIndentLevel);
        }

        private void Initialize(IDFFile idfFile, UpscaleMethodEnum upscaleMethod, Extent scaleExtent, string savePath, Log log, int logIndentLevel)
        {
            scaledIDFFileDictionary = new Dictionary<float, IDFFile>();
            this.baseIDFFile = idfFile;
            this.upscaleMethod = upscaleMethod;
            this.scaleExtent = scaleExtent;
            this.savePath = savePath;
            this.log = log;
            this.logIndentLevel = logIndentLevel;
            if (idfFile != null)
            {
                scaledIDFFileDictionary.Add(idfFile.XCellsize, idfFile);
            }
            this.currentAlignExtent = null;
        }

        /// <summary>
        /// Retrieves a surfacelevelfile with the same or coarser resolution than the given cellsize
        /// </summary>
        /// <param name="cellsize"></param>
        /// <returns></returns>
        public IDFFile RetrieveIDFFile(float cellsize, Extent alignExtent = null)
        {
            IDFFile scaledIDFFile = baseIDFFile;

            if ((baseIDFFile != null) && (baseIDFFile.XCellsize < cellsize))
            {
                currentAlignExtent = alignExtent;
                if (!scaledIDFFileDictionary.ContainsKey(cellsize) || ((currentAlignExtent != null) && currentAlignExtent.Equals(alignExtent)))
                {
                    this.currentAlignExtent = alignExtent;
                    scaledIDFFile = baseIDFFile.Upscale(cellsize, upscaleMethod, scaleExtent, alignExtent);
                    if (log != null)
                    {
                        log.AddInfo("Scaled IDF-file " + Path.GetFileName(baseIDFFile.Filename) + " to cellsize " + cellsize + " with upscalemethod: " + upscaleMethod.ToString(), logIndentLevel);
                    }
                    if (savePath != null)
                    {
                        scaledIDFFile.WriteFile(Path.Combine(savePath, Path.GetFileName(scaledIDFFile.Filename)));
                    }
                    scaledIDFFileDictionary.Add(cellsize, scaledIDFFile);
                }
                scaledIDFFile = scaledIDFFileDictionary[cellsize];
            }

            return scaledIDFFile;
        }

        public void ReleaseMemory(bool isMemoryCollected)
        {
            foreach (IDFFile idfFile in scaledIDFFileDictionary.Values)
            {
                if (idfFile != null)
                {
                    idfFile.ReleaseMemory(isMemoryCollected);
                }
            }
            if (baseIDFFile != null)
            {
                baseIDFFile.ReleaseMemory(isMemoryCollected);
            }
        }
    }
}
