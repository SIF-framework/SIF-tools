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
using Sweco.SIF.Common;
using Sweco.SIF.WorkflowViz.Visualisation;
using Sweco.SIF.WorkflowViz.Workflows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.WorkflowViz.GraphViz
{
    /// <summary>
    /// Class for storage and manipulation of GraphViz SubGraphs
    /// </summary>
    public class SubGraph : Graph
    {
        /// <summary>
        /// Defines rank of this SubGraph
        /// </summary>
        public RankDefinition RankDefinition { get; set; }

        /// <summary>
        /// List of edges from this SubGraph to other GraphViz objects
        /// </summary>
        public List<SubGraphEdge> Edges { get; set; }

        protected SubGraph() : base()
        {
            RankDefinition = null;
            Edges = new List<SubGraphEdge>();
        }

        public SubGraph(string name) : base(name)
        {
            RankDefinition = null;
            Edges = new List<SubGraphEdge>();
        }

        public SubGraph(string name, string label) : this(name)
        {
            Label = label;
            Edges = new List<SubGraphEdge>();
        }

        public SubGraph(string name, string label, bool isCluster) : this(name, label)
        {
            IsCluster = isCluster;
            Edges = new List<SubGraphEdge>();
        }

        /// <summary>
        /// Create dot strings for this SubGraph object
        /// </summary>
        /// <param name="indentLevel"></param>
        /// <returns></returns>
        public virtual StringBuilder CreateGraphString(int indentLevel)
        {
            StringBuilder graphString = new StringBuilder();
            Utils.AppendLine(graphString, "subgraph " + (IsCluster ? "cluster" : string.Empty) + Utils.CorrectName(Name) + " {", indentLevel);
            HandleGraphAttributes(graphString, indentLevel + 1);

            // Add attributes of subgraph
            if (Label != null)
            {
                Utils.AppendLine(graphString, "label = " + CommonUtils.EnsureDoubleQuotes(Label) + " fontname=\"" + Settings.FontName + " bold\";", indentLevel + 1); // fontname=\"Impact\"
            }

            // Add nodes and edges
            foreach (Node node in Nodes)
            {
                HandleNodeAttributes(graphString, node, indentLevel + 1);
                foreach (Edge edge in node.Edges)
                {
                    Utils.Append(graphString, CommonUtils.EnsureDoubleQuotes(edge.Node1.Name) + " -> " + CommonUtils.EnsureDoubleQuotes(edge.Node2.Name), indentLevel + 1);
                    if (edge.Attributes != null)
                    {
                        Utils.Append(graphString, " [" + edge.Attributes + "]");
                    }
                    Utils.AppendLine(graphString, ";");
                }
            }

            // Add all subgraphs that are a child of this SubGraph
            foreach (SubGraph subgraph in Subgraphs)
            {
                graphString.Append(subgraph.CreateGraphString(indentLevel + 1));

                foreach (SubGraphEdge edge in subgraph.Edges)
                {
                    graphString.Append(edge.CreateGraphString(2));
                    graphString.AppendLine();
                }
            }

            // Check if rank is defined on the order of the subgraphs
            if ((RankDefinition != null) && (RankDefinition.RankedNodes.Count > 1))
            {
                graphString.AppendLine();
                Utils.AppendLine(graphString, "{", indentLevel + 1);
                Utils.AppendLine(graphString, "rank=\"" + RankDefinition.RankType + "\"", indentLevel + 2);
                if (RankDefinition.RankedNodes.Count > 0)
                {
                    string invisAttributes = "style=\"" + GraphSettings.InvisibleStyle + "\"";
                    string edgeAttributes = "style=solid";
                    if (RankDefinition.EdgeAttributes != null)
                    {
                        edgeAttributes = RankDefinition.EdgeAttributes;
                    }

                    Node prevRankedNode = RankDefinition.RankedNodes[0];
                    for (int nodeIdx = 1; nodeIdx < RankDefinition.RankedNodes.Count; nodeIdx++)
                    {
                        Node rankedNode = RankDefinition.RankedNodes[nodeIdx];
                        string rankNodeOrderString = prevRankedNode.Name + "->" + rankedNode.Name;
                        if (Utils.IsRunscriptsName(prevRankedNode.Label) && Utils.IsRunscriptsName(rankedNode.Label))
                        {
                            rankNodeOrderString += " [" + edgeAttributes + "];"; 
                        }
                        else
                        {
                            rankNodeOrderString += " [" + invisAttributes + "];";
                        }
                        Utils.AppendLine(graphString, rankNodeOrderString, indentLevel + 2);
                        prevRankedNode = rankedNode;
                    }
                }
                Utils.AppendLine(graphString, "}", indentLevel + 1);
            }

            Utils.AppendLine(graphString, "}", indentLevel);

            return graphString;
        }

        /// <summary>
        /// Define rank for this SubGraph. Check GraphViz/dot-manual for description of ranks
        /// </summary>
        /// <param name="rankType"></param>
        /// <param name="rankedNodes"></param>
        /// <param name="edgeAttributes"></param>
        public virtual void DefineRank(string rankType, List<Node> rankedNodes, string edgeAttributes)
        {
            RankDefinition = new RankDefinition(rankType, rankedNodes, edgeAttributes);
        }

        /// <summary>
        /// Add an edge from this SubGraph to specified Subgraph
        /// </summary>
        /// <param name="subGraph"></param>
        /// <param name="attributes">optional attributes of the added edge</param>
        public virtual void AddEdge(SubGraph subGraph, string attributes = null)
        {
            Edges.Add(new SubGraphEdge(this, subGraph, attributes));
        }
    }
}
