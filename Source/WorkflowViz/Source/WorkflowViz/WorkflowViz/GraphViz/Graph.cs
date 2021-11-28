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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.WorkflowViz.GraphViz
{
    /// <summary>
    /// Base class for GraphViz Graphs
    /// </summary>
    public abstract class Graph : GraphObject
    {
        /// <summary>
        /// Font size for the title (with copyright notice) under the graph
        /// </summary>
        public static int Title_FontSize = 9;

        /// <summary>
        /// Font size for all node labels
        /// </summary>
        public static int Node_FontSize = Properties.Settings.Default.Node_FontSize;

        /// <summary>
        /// Margin (width,height in inches) for nodes, see https://graphviz.org/docs/attrs/margin. Leave empty to use Graphviz default (0.11,0.055) or use 0 to have no margin.
        /// </summary>
        public static string Node_Margin = Properties.Settings.Default.Node_Margin;

        /// <summary>
        /// Overrides Node style and color
        /// </summary>
        public string Attributes { get; set; }

        public bool IsCluster { get; set; }
        public string Style { get; set; }
        public string Color { get; set; }
        public List<Node> Nodes { get; set; }
        public List<SubGraph> Subgraphs { get; set; }
        public GraphSettings Settings { get; set; }

        protected Graph()
        {
            Attributes = null;
            IsCluster = false;
            Style = null;
            Color = null;
            Nodes = new List<Node>();
            Subgraphs = new List<SubGraph>();
            Settings = new GraphSettings();
        }

        public Graph(string name) : this()
        {
            Name = name;
        }

        public Graph(string name, string label) : this(name)
        {
            Label = label;
        }

        public Graph(string name, string label, bool isCluster) : this(name, label)
        {
            IsCluster = isCluster;
        }

        public Graph(string name, string label, bool isCluster, GraphSettings settings) : this(name, label, isCluster)
        {
            Settings = settings;
        }

        public void AddNode(Node node)
        {
            Nodes.Add(node);
        }

        public void AddSubGraph(SubGraph subgraph)
        {
            Subgraphs.Add(subgraph);
        }

        /// <summary>
        /// Process currently defined Graph attributes (in this Graph object) and add corresponding dot strings to specified dot graph string
        /// </summary>
        /// <param name="graphString"></param>
        /// <param name="indentLevel"></param>
        public virtual void HandleGraphAttributes(StringBuilder graphString, int indentLevel)
        {
            if ((Attributes != null) || (Style != null) || (Color != null) || (URL != null))
            {
                Utils.Append(graphString, "graph [", indentLevel);
                if (Attributes != null)
                {
                    Utils.Append(graphString, Attributes);
                }
                if (((Attributes == null) || !Attributes.Contains("style")) && (Style != null))
                {
                    if (!graphString[graphString.Length - 1].Equals('['))
                    {
                        Utils.Append(graphString, ",");
                    }
                    Utils.Append(graphString, "style=" + Style);
                }
                if (((Attributes == null) || !Attributes.Contains("style")) && (Color != null))
                {
                    if (!graphString[graphString.Length - 1].Equals('['))
                    {
                        Utils.Append(graphString, ",");
                    }
                    Utils.Append(graphString, "color=" + Color);
                }
                if (((Attributes == null) || !Attributes.Contains("URL")) && (URL != null))
                {
                    if (!graphString[graphString.Length - 1].Equals('['))
                    {
                        Utils.Append(graphString, ",");
                    }
                    Utils.Append(graphString, "URL=\"" + URL.Replace("\\", "/") + "\"");
                }
                Utils.AppendLine(graphString, "];");
            }
        }

        /// <summary>
        /// Process specified Node object and add corresponding strings to specified dot graph string
        /// </summary>
        /// <param name="graphString"></param>
        /// <param name="node"></param>
        /// <param name="indentLevel"></param>
        public virtual void HandleNodeAttributes(StringBuilder graphString, Node node, int indentLevel)
        {
            Utils.Append(graphString, node.Name + " [label=" + CommonUtils.EnsureDoubleQuotes(node.Label), indentLevel);
            if (node.Attributes != null)
            {
                Utils.Append(graphString, "," + node.Attributes);
            }
            if (((node.Attributes == null) || !node.Attributes.Contains("style")) && (node.Style != null))
            {
                Utils.Append(graphString, ",style=" + "\"" + node.Style + "\"");
            }
            if (((node.Attributes == null) || !node.Attributes.Contains("color")) && (node.Color != null))
            {
                Utils.Append(graphString, ",color=\"" + node.Color + "\"");
            }
            if (((node.Attributes == null) || !node.Attributes.Contains("URL")) && (node.URL != null))
            {
                Utils.Append(graphString, ",URL=" + "\"" + node.URL.Replace("\\", "/") + "\"");
            }
            Utils.AppendLine(graphString, "];");
        }
    }
}
