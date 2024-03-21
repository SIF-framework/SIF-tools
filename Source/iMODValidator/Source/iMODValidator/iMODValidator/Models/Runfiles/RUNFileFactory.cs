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
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Models.Runfiles
{
    public class RUNFileFactory
    {
        internal static RUNFile CreateRUNFileObject(string filename)
        {
            string extension = Path.GetExtension(filename).ToLower();
            switch (extension)
            {
                case ".run":
                    return new V5RUNFile(filename);
                case ".prj":
                    return new V5PRJFile(filename);
                default:
                    throw new ToolException("Unknown iMOD-model projectfile type, currently only RUN- and PRJ-files (.RUN and .PRJ) are supported: " + filename);
            }
        }
    }
}
