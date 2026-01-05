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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.ISG
{
    public class ISGNode : ISGCoordinate
    {
        /// <summary>
        /// Byte length for single precision ISG-files
        /// </summary>
        public const int SingleByteLength = 8;
        /// <summary>
        /// Byte length for double precision ISG-files
        /// </summary>
        public const int DoubleByteLength = 16;

        public ISGSegment ISGSegment = null;

        protected ISGNode(ISGSegment isgSegment)
        {
            ISGSegment = isgSegment;
        }

        public ISGNode(ISGSegment isgSegment, float x, float y) : base(x, y)
        {
            ISGSegment = isgSegment;
        }

        public float CalculateDistance()
        {
            ISGNode prevNode = ISGSegment.Nodes[0];
            float distance = 0;
            if (this != prevNode)
            {
                int nodeIdx = 1;
                while (prevNode != this)
                {
                    ISGNode node = ISGSegment.Nodes[nodeIdx];
                    float dx = (prevNode.X - node.X);
                    float dy = (prevNode.Y - node.Y);
                    distance += (float)Math.Sqrt(dx * dx + dy * dy);
                    prevNode = node;
                    nodeIdx++;
                }
            }
            return distance;
        }

        public override string ToString()
        {
            return "Segment: " + ISGSegment.Label + " (" + Math.Round(X, 3).ToString("F3", IMODFile.EnglishCultureInfo) + "," + Math.Round(Y, 3).ToString("F3", IMODFile.EnglishCultureInfo) + ")";
        }
    }
}
