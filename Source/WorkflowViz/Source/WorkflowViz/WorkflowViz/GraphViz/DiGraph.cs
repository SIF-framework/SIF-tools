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
using Sweco.SIF.WorkflowViz.Visualisation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.WorkflowViz.GraphViz
{
    /// <summary>
    /// Class for representation and manipulation of a GraphViz DiGraph (a directed graph)
    /// </summary>
    public class DiGraph : Graph
    {
        public DiGraph(string name) : base(name)
        {
            Settings = new GraphSettings();
        }

        public DiGraph(string name, GraphSettings settings) : base(name)
        {
            Settings = settings;
        }


        /// <summary>
        /// Create dot strings for this DiGraph object
        /// </summary>
        /// <returns></returns>
        public virtual string CreateGraphString()
        {
            StringBuilder graphString = new StringBuilder();
            Utils.AppendLine(graphString, "digraph " + Utils.CorrectName(Name) + " {", 0);

            AddGlobalAttributes(graphString);

            foreach (Node node in Nodes)
            {
                HandleNodeAttributes(graphString, node, 1);
                foreach (Edge edge in node.Edges)
                {
                    Utils.AppendLine(graphString, edge.Node1.Name + " -> " + edge.Node2.Name + ";", 1);
                }
            }

            foreach (SubGraph subgraph in Subgraphs)
            {
                graphString.Append(subgraph.CreateGraphString(1));
                // graphString.AppendLine();

                foreach (SubGraphEdge edge in subgraph.Edges)
                {
                    graphString.Append(edge.CreateGraphString(2));
                    graphString.AppendLine();
                }
            }

            Utils.AppendLine(graphString, "}");
            return graphString.ToString();
        }

        /// <summary>
        /// Add dot strings for global DiGraph attributes based on current settings
        /// </summary>
        /// <param name="graphString"></param>
        protected virtual void AddGlobalAttributes(StringBuilder graphString)
        {
            HandleGraphAttributes(graphString, 1);
            Utils.AppendLine(graphString, "graph [fontname=\"" + Settings.FontName + "\"];", 1);
            Utils.AppendLine(graphString, "edge [fontname=\"" + Settings.FontName + "\"];", 1);
            Utils.AppendLine(graphString, "node [style=\"rounded\",shape=box,fontname=\"" + Settings.FontName + "\",fontsize=" + Graph.Node_FontSize + ",tooltip = \" \",margin=\"" + Graph.Node_Margin + "\",width=0,height=0];", 1);
            Utils.AppendLine(graphString, "fontname=\"" + Settings.FontName + "\"", 1);
            Utils.AppendLine(graphString, "fontsize=" + Title_FontSize, 1);
            Utils.AppendLine(graphString, "labeljust=l", 1);
            Utils.AppendLine(graphString, "tooltip = \" \"", 1);
            Utils.AppendLine(graphString, "label=\"Workflow visualization with SIF-tool WorkflowViz (\u00a9 Sweco 2021), using GraphViz. Created: " + DateTime.Now.ToShortDateString() + " " + DateTime.Now.ToShortTimeString()
                + "\\lWorkflow path: " + Settings.SourcePath.Replace("\\", "\\\\") + "\\l\"", 1);
            Utils.AppendLine(graphString, "overlap=true;", 1);
            Utils.AppendLine(graphString, "splines = false;", 1);

            Utils.AppendLine(graphString, "", 1);

        }

        /// <summary>
        /// Write dot GV-file for this DiGraph object and all specified underlying graph elements
        /// </summary>
        /// <param name="filename"></param>
        public virtual void WriteGraphFile(string filename)
        {
            string graphString = CreateGraphString();

            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(filename);
                sw.Write(graphString);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while writing graph file " + Path.GetFileName(filename), ex);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }
    }
}
