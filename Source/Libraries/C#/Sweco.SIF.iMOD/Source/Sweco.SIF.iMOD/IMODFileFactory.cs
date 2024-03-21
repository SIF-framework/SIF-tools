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
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.IPF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD
{
    /// <summary>
    /// Class for creating IMODFile objects
    /// </summary>
    public class IMODFileFactory
    {
        /// <summary>
        /// Read iMOD-file and create an IMODFile object for it
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="useLazyLoading"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        public static IMODFile ReadIMODFile(string filename, bool useLazyLoading = false, Log log = null, int logIndentLevel = 0)
        {
            string ext = Path.GetExtension(filename).ToLower();
            if (ext.Equals(".idf"))
            {
                return IDFFile.ReadFile(filename, useLazyLoading, log, logIndentLevel);
            }
            else if (ext.Equals(".ipf"))
            {
                return IPFFile.ReadFile(filename, useLazyLoading, log, logIndentLevel);
            }
            else if (ext.Equals(".gen"))
            {
                GENFile genFile = GENFile.ReadFile(filename, false);
                return genFile;
            }
            else
            {
                throw new Exception("Unknown type of iMOD-file: " + filename);
            }
        }
    }
}
