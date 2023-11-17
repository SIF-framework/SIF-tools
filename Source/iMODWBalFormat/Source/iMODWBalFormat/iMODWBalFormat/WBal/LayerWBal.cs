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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iMODWBalFormat
{
    public class LayerWBal
    {
        public DateTime? DateTime;
        public int Layer;
        public int Zone;
        public List<WBalPost> Posts;

        protected double zoneArea;
        public double ZoneArea 
        {
            get { return zoneArea; }
            set { zoneArea = value; } 
        }

        public float TotalIn;
        public float TotalOut;
        public float TotalSum;

        public LayerWBal(DateTime? dateTime, int layer, int zone)
        {
            this.DateTime = dateTime;
            this.Layer = layer;
            this.Zone = zone;
            Posts = new List<WBalPost>();
        }

        public void AddPost(WBalPost post)
        {
            Posts.Add(post);
            TotalIn += post.In;
            TotalOut += post.Out;
            TotalSum += post.Sum;
        }
    }
}
