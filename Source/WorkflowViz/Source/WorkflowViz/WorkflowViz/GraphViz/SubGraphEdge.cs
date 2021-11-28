// WorkflowViz is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of WorkflowViz.
// 
// WorkflowViz is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// WorkflowViz is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with WorkflowViz. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.WorkflowViz.GraphViz
{
    /// <summary>
    /// Class for representation of an edge between two SubGraphs
    /// </summary>
    public class SubGraphEdge
    {
        public SubGraph SubGraph1 { get; set; }
        public SubGraph SubGraph2 { get; set; }
        public string Attributes { get; set; }

        public SubGraphEdge(SubGraph subGraph1, SubGraph subGraph2, string attributes = null)
        {
            SubGraph1 = subGraph1;
            SubGraph2 = subGraph2;
            Attributes = attributes;
        }

        public virtual string CreateGraphString(int indentLevel)
        {
            StringBuilder graphString = new StringBuilder();
            if ((SubGraph1 != null) && (SubGraph1.Nodes.Count > 0) && (SubGraph2 != null) && (SubGraph2.Nodes.Count > 0))
            {
                // Try to find folder nodes to connect subgraphs
                Node folderNode1 = FindNode(SubGraph1, null, "folder");
                Node folderNode2 = FindNode(SubGraph2, null, "folder");

                // If not found, use first node of subgraphs
                if (folderNode1 == null)
                {
                    folderNode1 = SubGraph1.Nodes[0];
                }
                if (folderNode2 == null)
                {
                    folderNode2 = SubGraph2.Nodes[0];
                }

                Utils.AppendLine(graphString, folderNode1.Name + "->" + folderNode2.Name + ((Attributes != null) ? " [" + Attributes + "];" : string.Empty), indentLevel);
            }

            return graphString.ToString();
        }

        private static Node FindNode(SubGraph subGraph, string labelSubstring, string attributeSubstring)
        {
            Node node = null;

            int nodeIdx = 0;
            while ((node == null) && (nodeIdx < subGraph.Nodes.Count))
            {
                if ((labelSubstring != null) && subGraph.Nodes[nodeIdx].Label.Contains(labelSubstring))
                {
                    node = subGraph.Nodes[nodeIdx];
                }
                else if ((attributeSubstring != null) && (subGraph.Nodes[nodeIdx].Attributes != null) && subGraph.Nodes[nodeIdx].Attributes.Contains(attributeSubstring))
                {
                    node = subGraph.Nodes[nodeIdx];
                }
                nodeIdx++;
            }

            return node;
        }
    }
}
