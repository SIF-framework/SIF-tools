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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.ISG
{
    public class ISGSegment
    {
        /// <summary>
        /// Name of the segment, use quotes to distinguish names with empty spaces
        /// </summary>
        public string Label;
        /// <summary>
        /// Record number that deﬁnes the ﬁrst coordinate (node) in the associated ISP-ﬁle
        /// </summary>
        public int ISEG;
        /// <summary>
        /// Number of records in the ISP-ﬁle that describes the segment by coordinates
        /// </summary>
        public int NSEG;
        /// <summary>
        /// Record number that deﬁnes the ﬁrst calculation points on the segment ISEG
        /// within the associated ISD1-ﬁle
        /// </summary>
        public int ICLC;
        /// <summary>
        /// Number of calculation points on segment ISEG
        /// </summary>
        public int NCLC;
        /// <summary>
        /// Record number that deﬁnes the ﬁrst cross-section on the segment ISEG within
        /// the associated ISC1-ﬁle
        /// </summary>
        public int ICRS;
        /// <summary>
        /// Number of cross-sections on segment ISEG
        /// </summary>
        public int NCRS;
        /// <summary>
        /// Record number that deﬁnes the ﬁrst weir/structure on the segment ISEG within
        /// the associated IST1-ﬁle
        /// </summary>
        public int ISTW;
        /// <summary>
        /// Number of weirs/structures on segment ISEG
        /// </summary>
        public int NSTW;
        /// <summary>
        /// Obsolete
        /// </summary>
        public int IQHR;
        /// <summary>
        /// Obsolete
        /// </summary>
        public int NQHR;

        private List<ISGNode> nodes;
        public List<ISGNode> Nodes
        {
            get { return nodes; }
            set { nodes = value; }
        }

        private List<ISGCalculationPoint> calculationPoints;
        public List<ISGCalculationPoint> CalculationPoints
        {
            get { return calculationPoints; }
            set { calculationPoints = value; }
        }

        private List<ISGCrossSection> crossSections;
        public List<ISGCrossSection> CrossSections
        {
            get { return crossSections; }
            set { crossSections = value; }
        }

        private List<ISGStructure> structures;
        public List<ISGStructure> Structures
        {
            get { return structures; }
            set { structures = value; }
        }

        public ISGSegment()
        {
            calculationPoints = null;
            crossSections = null;
            structures = null;
        }

        public ISGSegment(string label, int iseg, int nseg, int iclc, int nclc, int icrs, int ncrs, int istw, int nstw, int iqhr = 0, int nqhr = 0)
        {
            Label = label;
            ISEG = iseg;
            NSEG = nseg;
            ICLC = iclc;
            NCLC = nclc;
            ICRS = icrs;
            NCRS = ncrs;
            ISTW = istw;
            NSTW = nstw;
            IQHR = iqhr;
            NQHR = nqhr;
        }

        public ISGSegment(string label, int nseg, int nclc, int ncrs, int nstw, int nqhr = 0)
        {
            Label = label;
            ISEG = 0;
            NSEG = nseg;
            ICLC = 0;
            NCLC = nclc;
            ICRS = 0;
            NCRS = ncrs;
            ISTW = 0;
            NSTW = nstw;
            IQHR = 0;
            NQHR = nqhr;
        }

        public bool HasOverlap(Extent extent)
        {
            foreach (ISGNode node in nodes)
            {
                if (node.IsContainedBy(extent))
                {
                    return true;
                }
            }
            return false;
        }

        public float GetLength()
        {
            float curDistance = 0;
            float partDistance = 0;
            int nodeIdx = 0;
            ISGNode isgNode = nodes[0];
            float x = isgNode.X;
            float y = isgNode.Y;
            float nextX;
            float nextY;
            while (nodeIdx < (nodes.Count() - 1))
            {
                nextX = nodes[nodeIdx + 1].X;
                nextY = nodes[nodeIdx + 1].Y;
                float dX = (nextX - x);
                float dY = (nextY - y);
                partDistance = (float)Math.Sqrt(dX * dX + dY * dY);
                curDistance += partDistance;
                x = nextX;
                y = nextY;
                nodeIdx++;
            }
            return curDistance;
        }

        /// <summary>
        /// Retrieves coordinate for a given distance on this segment
        /// </summary>
        /// <param name="segmentDistance"></param>
        /// <returns>coordinate or null if distance is beyond length</returns>
        public ISGCoordinate GetCoordinate(float segmentDistance)
        {
            if (nodes.Count() == 0)
            {
                throw new Exception("ISG-segment without nodes: " + Label);
            }

            float curDistance = 0;
            float partDistance = 0;
            int nodeIdx = 0;
            ISGNode isgNode = nodes[0];
            float x = isgNode.X;
            float y = isgNode.Y;
            float nextX;
            float nextY;
            while ((nodeIdx < nodes.Count() - 1) && (curDistance < segmentDistance))
            {
                nextX = nodes[nodeIdx + 1].X;
                nextY = nodes[nodeIdx + 1].Y;
                float dX = (nextX - x);
                float dY = (nextY - y);
                partDistance = (float)Math.Sqrt(dX * dX + dY * dY);
                if ((segmentDistance - (curDistance + partDistance)) < ISGFile.DistanceErrorMargin)
                {
                    float fraction = (segmentDistance - curDistance) / partDistance;
                    return new ISGCoordinate(x + fraction * dX, y + fraction * dY);
                }
                curDistance += partDistance;
                x = nextX;
                y = nextY;
                nodeIdx++;
            }
            if (Math.Abs(curDistance - segmentDistance) < ISGFile.DistanceErrorMargin)
            {
                return nodes[nodeIdx];
            }
            else
            {
                return null;
            }
        }

        public Extent GetExtent()
        {
            Extent extent = new Extent();
            if (nodes.Count() > 0)
            {
                extent.llx = this.nodes[0].X;
                extent.lly = this.nodes[0].Y;
                extent.urx = extent.llx;
                extent.ury = extent.lly;

                for (int nodeIdx = 1; nodeIdx < nodes.Count(); nodeIdx++)
                {
                    float x = nodes[nodeIdx].X;
                    float y = nodes[nodeIdx].Y;
                    if (x < extent.llx)
                    {
                        extent.llx = x;
                    }
                    if (x > extent.urx)
                    {
                        extent.urx = x;
                    }
                    if (y < extent.lly)
                    {
                        extent.lly = y;
                    }
                    if (y > extent.ury)
                    {
                        extent.ury = y;
                    }
                }
                return extent;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Find calculation point at or after the specified distance on this segment. 
        /// If no calculation points are present after the specified distance, the last calculation point is returned.
        /// </summary>
        /// <param name="segmentDistance"></param>
        /// <returns></returns>
        public ISGCalculationPoint GetCalculationPoint(float segmentDistance)
        {
            for (int cpIdx = 0; cpIdx < calculationPoints.Count(); cpIdx++)
            {
                if (calculationPoints[cpIdx].DIST >= segmentDistance)
                {
                    return calculationPoints[cpIdx];
                }
            }
            return calculationPoints[calculationPoints.Count() - 1];
        }

        public ISGSegment Copy()
        {
            ISGSegment segmentCopy = new ISGSegment(Label, NSEG, NCLC, NCRS, NQHR);
            segmentCopy.nodes = new List<ISGNode>();
            for (int nodeIdx = 0; nodeIdx < NSEG; nodeIdx++)
            {
                segmentCopy.nodes.Add(new ISGNode(segmentCopy, nodes[nodeIdx].X, nodes[nodeIdx].Y));
            }
            segmentCopy.calculationPoints = new List<ISGCalculationPoint>();
            for (int cpIdx = 0; cpIdx < NCLC; cpIdx++)
            {
                segmentCopy.calculationPoints.Add(calculationPoints[cpIdx].Copy());
            }
            segmentCopy.crossSections = new List<ISGCrossSection>();
            for (int csIdx = 0; csIdx < NCRS; csIdx++)
            {
                segmentCopy.crossSections.Add(crossSections[csIdx].Copy());
            }
            segmentCopy.structures = new List<ISGStructure>();
            for (int structureIdx = 0; structureIdx < NSTW; structureIdx++)
            {
                segmentCopy.structures.Add(structures[structureIdx].Copy());
            }
            return segmentCopy;
        }
    }
}
