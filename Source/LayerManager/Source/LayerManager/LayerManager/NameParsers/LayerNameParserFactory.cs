// LayerManager is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of LayerManager.
// 
// LayerManager is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// LayerManager is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with LayerManager. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.LayerManager.NameParsers
{
    public class LayerNameParserFactory
    {
        private static List<LayerNameParser> nameParsers = null;

        public static void RegisterNameParser(LayerNameParser nameParser)
        {
            if (nameParsers == null)
            {
                nameParsers = new List<LayerNameParser>();
            }

            nameParsers.Add(nameParser);
        }

        public static LayerNameParser RetrieveNameParser(string layerFilename)
        {
            if (nameParsers != null)
            {
                foreach (LayerNameParser nameParser in nameParsers)
                {
                    if (nameParser.IsValidLayerFilename(layerFilename))
                    {
                        return nameParser;
                    }
                }
            }

            throw new ToolException("Unknown format for REGIS-filename: " + Path.GetFileName(layerFilename));
        }
    }
}
