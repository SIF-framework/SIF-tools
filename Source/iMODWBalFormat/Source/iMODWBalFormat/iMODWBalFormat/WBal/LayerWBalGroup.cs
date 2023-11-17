// iMODWBalFormat is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODWBalFormat.
// 
// iMODWBalFormat is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODWBalFormat is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODWBalFormat. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.iMOD.GEN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iMODWBalFormat.WBal
{
    public class LayerWBalGroup
    {
        public int Zone;
        public string Name;
        public List<LayerWBal> LayerWBals;
        public double ZoneArea;

        public float TotalIn;
        public float TotalOut;
        public float TotalSum;

        public GENFeature genFeature;

        public LayerWBalGroup(int zone, string name, List<LayerWBal> layerWBals, double zoneArea, GENFeature genFeature = null)
        {
            this.Zone = zone;
            this.Name = name;
            this.LayerWBals = layerWBals;
            this.ZoneArea = zoneArea;
            this.genFeature = genFeature;

            // Sum totals over all layers
            for (int layerWBalIdx = 0; layerWBalIdx < layerWBals.Count(); layerWBalIdx++)
            {
                LayerWBal layerWBal = layerWBals[layerWBalIdx];
                TotalIn += layerWBal.TotalIn;
                TotalOut += layerWBal.TotalOut;
                TotalSum += layerWBal.TotalSum;
            }
        }

        public void AddLayerWBal(LayerWBal layerWBal)
        {
            if (layerWBal.Zone != Zone)
            {
                throw new Exception("Added LayerWBal object (zone=" + layerWBal.Zone + " should have same zone id as group zone id (" + Zone + ")");
            }
            LayerWBals.Add(layerWBal);
            TotalIn += layerWBal.TotalIn;
            TotalOut += layerWBal.TotalOut;
            TotalSum += layerWBal.TotalSum;
        }
    }
}
