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
using Sweco.SIF.iMOD.ISG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.ISG
{
    /// <summary>
    /// In an ISGNetwork object, for each ISG node
    /// </summary>
    public class ISGNetwork
    {
        private ISGFile isgFile;
        public ISGFile ISGFile
        {
            get { return isgFile; }
        }

        protected SortedList<long, List<ISGNode>> xyIndexedNodesList;
        protected Dictionary<ISGNode, List<ISGNode>> nodeConnections;
        protected Dictionary<ISGNode, List<double>> nodeConnectionWeights;
        protected Extent extent;
        protected static float xyToleranceDecimalFactor;

        public ISGNetwork(ISGFile isgFile, int xyToleranceDecimalCount = 2)
        {
            this.isgFile = isgFile;
            xyIndexedNodesList = null;
            nodeConnections = null;
            extent = null;
            xyToleranceDecimalFactor = (float)Math.Pow(10, xyToleranceDecimalCount);
        }

        public long RetrieveNetworkSegmentCount(double llx, double lly, double urx, double ury)
        {
            Extent extent = new Extent((float)llx, (float)lly, (float)ury, (float)ury);
            return RetrieveNetworkSegmentCount(extent);
        }

        public long RetrieveNetworkSegmentCount(float llx, float lly, float urx, float ury)
        {
            Extent extent = new Extent(llx, lly, ury, ury);
            return RetrieveNetworkSegmentCount(extent);
        }

        public long RetrieveNetworkSegmentCount(Extent extent)
        {
            long nodeCount = 0;
            foreach (ISGSegment segment in isgFile.Segments)
            {
                ISGNode node = segment.Nodes[0];
                if ((extent != null) && extent.Contains(node.X, node.Y))
                {
                    nodeCount++;
                }
            }
            return nodeCount;
        }

        /// <summary>
        /// Builds an ISG-network in which (connected) nodes can be searched
        /// Note: the xy-coordinates in the network are stored with an accuracy of 1cm
        /// </summary>
        public void BuildNetwork(double llx, double lly, double urx, double ury)
        {
            Extent extent = new Extent((float)llx, (float)lly, (float)ury, (float)ury);
            BuildNetwork(extent);
        }

        /// <summary>
        /// Builds an ISG-network in which (connected) nodes can be searched
        /// Note: the xy-coordinates in the network are stored with an accuracy of 1cm
        /// </summary>
        public void BuildNetwork(float llx, float lly, float urx, float ury)
        {
            Extent extent = new Extent(llx, lly, ury, ury);
            BuildNetwork(extent);
        }

        /// <summary>
        /// Builds an ISG-network in which (connected) nodes can be searched
        /// Note: the xy-coordinates in the network are stored with an accuracy of 1cm
        /// </summary>
        public void BuildNetwork(Extent extent = null)
        {
            xyIndexedNodesList = new SortedList<long, List<ISGNode>>();
            nodeConnections = new Dictionary<ISGNode, List<ISGNode>>();
            nodeConnectionWeights = new Dictionary<ISGNode, List<double>>();
            this.extent = extent;

            foreach (ISGSegment segment in isgFile.Segments)
            {
                List<ISGNode> segmentNodes = segment.Nodes;
                int NSEG = segment.NSEG;

                List<ISGNode> xyNodes = null;
                List<ISGNode> connectedNodes = null;
                List<double> connectedNodeWeights = null;
                for (int nodeIdx = 0; nodeIdx < NSEG; nodeIdx++)
                {
                    ISGNode node = segmentNodes[nodeIdx];
                    if ((extent != null) && extent.Contains(node.X, node.Y))
                    {
                        // First add node to list of nodes at this xy-location
                        long key = GetKey(node);
                        if (xyIndexedNodesList.TryGetValue(key, out xyNodes))
                        {
                            xyNodes.Add(node);
                        }
                        else
                        {
                            xyNodes = new List<ISGNode>();
                            xyNodes.Add(node);
                            xyIndexedNodesList.Add(key, xyNodes);
                        }

                        // Second add node to nodeConnection list and connect with direct neighbours on this segment
                        connectedNodes = new List<ISGNode>();
                        connectedNodeWeights = new List<double>();
                        if (nodeIdx > 0)
                        {
                            connectedNodes.Add(segmentNodes[nodeIdx - 1]);
                            double distance = node.DistanceTo(segmentNodes[nodeIdx - 1]);
                            connectedNodeWeights.Add(distance);
                        }
                        if (nodeIdx < NSEG - 1)
                        {
                            connectedNodes.Add(segmentNodes[nodeIdx + 1]);
                            double distance = node.DistanceTo(segmentNodes[nodeIdx + 1]);
                            connectedNodeWeights.Add(distance);
                        }
                        nodeConnections.Add(node, connectedNodes);
                        nodeConnectionWeights.Add(node, connectedNodeWeights);
                    }
                }
            }

            // For each node in the network, also add connection to other nodes at the same location
            for (int nodeIdx = 0; nodeIdx < nodeConnections.Count(); nodeIdx++)
            {
                ISGNode node = nodeConnections.Keys.ElementAt(nodeIdx);
                List<ISGNode> otherNodes = GetOtherNodes(node);
                nodeConnections[node].AddRange(otherNodes);
                // For each other node, add a distance of zero
                List<double> connectionDistances = nodeConnectionWeights[node];
                for (int nodeIdx2 = 0; nodeIdx2 < otherNodes.Count(); nodeIdx2++)
                {
                    connectionDistances.Add(0d);
                }
            }
        }

        public Extent Extent
        {
            get { return extent; }
        }

        public bool IsConnected(double x1, double y1, double x2, double y2, double pointEqualityMargin = 0)
        {
            return IsConnected((float)x1, (float)y1, (float)x2, (float)y2, (float)pointEqualityMargin);
        }

        public bool IsConnected(float x1, float y1, float x2, float y2, float pointEqualityMargin = 0)
        {
            // First get all nodes at the given x,y-location
            List<ISGNode> nodes = GetNodes(x1, y1, pointEqualityMargin);
            for (int nodeIdx1 = 0; nodeIdx1 < nodes.Count(); nodeIdx1++)
            {
                // Now for each node retrieve the nodes that they're connected to
                List<ISGNode> connectedNodes = GetConnectedNodes(nodes[nodeIdx1]);
                for (int nodeIdx2 = 0; nodeIdx2 < connectedNodes.Count(); nodeIdx2++)
                {
                    if (connectedNodes[nodeIdx2].DistanceTo(x2, y2) < pointEqualityMargin)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Retrieve other ISG-nodes at the location of the specified node within a defined (rectangle) buffer.
        /// If no buffer is specified the nodes at the given location are returned. 
        /// Note: the xy-coordinates in the network are stored with an accuracy of 1cm
        /// </summary>
        /// <param name="?"></param>
        /// <param name="rectangleBuffersize"></param>
        /// <returns></returns>
        public List<ISGNode> GetOtherNodes(ISGNode isgNode, float rectangleBuffersize = 0)
        {
            List<ISGNode> allNodeList = GetNodes(isgNode.X, isgNode.Y, rectangleBuffersize);
            List<ISGNode> otherNodeList = new List<ISGNode>();
            foreach (ISGNode otherNode in allNodeList)
            {
                // Only add nodes that are different from the specified one
                if (otherNode != isgNode)
                {
                    otherNodeList.Add(otherNode);
                }
            }
            return otherNodeList;
        }

        /// <summary>
        /// Retrieve ISG-nodes from curent ISG-network at x,y-location within a defined (rectangle) buffer.
        /// If no buffer is specified the nodes at the given location are returned. 
        /// Note: the xy-coordinates in the network are stored with an accuracy of 1cm
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="rectangleBuffersize"></param>
        /// <returns></returns>
        public List<ISGNode> GetNodes(float x, float y, float rectangleBuffersize = 0)
        {
            List<ISGNode> selectedNodes = new List<ISGNode>();

            float minX = x - rectangleBuffersize;
            float minY = y - rectangleBuffersize;
            float maxX = x + rectangleBuffersize;
            float maxY = y + rectangleBuffersize;

            long minKey = GetKey(minX, minY);
            long maxKey = GetKey(maxX, maxY);

            // binary search to find first index with an x equal to or above the minX
            float stepSize = xyIndexedNodesList.Count() / 2.0f;
            int index = (int)(stepSize + 0.5f);
            while (stepSize > 1f)
            {
                stepSize /= 2;
                if (xyIndexedNodesList.Keys[index] < minKey)
                {
                    index = (int)(index + stepSize + 0.5f);
                }
                else if (xyIndexedNodesList.Keys[index] > minKey)
                {
                    index = (int)(index - stepSize + 0.5f); ;
                }
                else
                {
                    stepSize = 0;
                }
            }
            if ((index < xyIndexedNodesList.Count()) && (xyIndexedNodesList.Keys[index] < minKey))
            {
                index++;
            }
            else if ((index < xyIndexedNodesList.Count()) && xyIndexedNodesList.Keys[index].Equals(minKey) && rectangleBuffersize.Equals(0))
            {
                // If no buffer is requested, we're done
                return xyIndexedNodesList.Values[index];
            }


            // now do a sequential search for all elements within the given buffer extent
            long key;
            while ((index < xyIndexedNodesList.Count()) && ((key = xyIndexedNodesList.Keys[index]) <= maxKey))
            {
                float keyY = GetY(key);
                if ((keyY >= minY) && (keyY <= maxY))
                {
                    selectedNodes.AddRange(xyIndexedNodesList.Values[index]);
                }
                index++;
            }

            return selectedNodes;
        }

        /// <summary>
        /// Retrieve all ISGNodes in the current network
        /// </summary>
        /// <returns></returns>
        public List<ISGNode> GetNodes()
        {
            return nodeConnections.Keys.ToList<ISGNode>();
        }

        public List<ISGNode> GetConnectedNodes(ISGNode isgNode)
        {
            return nodeConnections[isgNode];
        }

        public List<double> GetConnectedNodeWeights(ISGNode isgNode)
        {
            return nodeConnectionWeights[isgNode];
        }

        public List<ISGNode> GetConnectedNodesDeprecated(ISGNode isgNode)
        {
            // Add connected nodes within segment of the specified node
            List<ISGNode> connectedNodes = new List<ISGNode>();
            ISGSegment segment = isgNode.ISGSegment;
            // Find index of specified node on segment
            int nodeIdx = 0;
            while (!(segment.Nodes[nodeIdx].Equals(isgNode)) && nodeIdx < segment.NSEG)
            {
                nodeIdx++;
            }
            if (nodeIdx == segment.NSEG)
            {
                throw new Exception("Node not found on segment that is defined for that node: " + isgNode.ToString());
            }
            // Add nodes on this segment on both sides of specified node
            if (nodeIdx > 0)
            {
                connectedNodes.Add(segment.Nodes[nodeIdx - 1]);
            }
            if (nodeIdx < segment.NSEG - 1)
            {
                connectedNodes.Add(segment.Nodes[nodeIdx + 1]);
            }

            // Add connected nodes from other segments (at distance 0)
            connectedNodes.AddRange(GetOtherNodes(isgNode));

            return connectedNodes;
        }

        protected static long GetKey(ISGNode node)
        {
            // "Round" X and Y at 2 decimals
            return ((int)(node.X * xyToleranceDecimalFactor)) * 100000000L + (int)(node.Y * xyToleranceDecimalFactor);
        }

        protected static long GetKey(float x, float y)
        {
            // "Round" X and Y at 2 decimals
            return ((int)(x * xyToleranceDecimalFactor)) * 100000000L + (int)(y * xyToleranceDecimalFactor); ;
        }

        protected static float GetY(long key)
        {
            return (key % 100000000) / xyToleranceDecimalFactor;
        }

        protected static float GetX(long key)
        {
            return (key / 1000000000) / xyToleranceDecimalFactor;
        }
    }
}
