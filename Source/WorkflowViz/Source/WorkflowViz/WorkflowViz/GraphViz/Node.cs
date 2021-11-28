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
using Sweco.SIF.WorkflowViz.Status;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.WorkflowViz.GraphViz
{
    /// <summary>
    /// Class for storing and manipulating GraphViz Node objects
    /// </summary>
    public class Node : GraphObject
    {
        private static int IssueCount = 0;

        protected string shape;
        public string Shape
        {
            get { return shape; }
            set { shape = value; }
        }

        protected string attributes;
        /// <summary>
        /// Overrides Node style and color
        /// </summary>
        public string Attributes
        {
            get { return attributes; }
            set { attributes = value; }
        }

        protected string style;
        public string Style
        {
            get { return style; }
            set { style = value; }
        }

        protected string color;
        public string Color
        {
            get { return color; }
            set { color = value; }
        }

        protected List<Edge> edges;
        public List<Edge> Edges
        {
            get { return edges; }
            set { edges = value; }
        }

        protected string sourceFilename;
        public string SourceFilename
        {
            get { return sourceFilename; }
            set { sourceFilename = value; }
        }

        protected Node() : base()
        {
            this.edges = new List<Edge>();
            this.minDate = null;
            this.maxDate = null;
            this.sourceFilename = null;
            this.runStatus = RunStatus.Undefined;
        }

        protected Node(string name) : this()
        {
            this.Name = name;
        }

        public Node(string name, string label) : this(name)
        {
            this.Label = label;
        }

        private RunStatus runStatus;
        public RunStatus RunStatus
        {
            get { return runStatus; }
            set { runStatus = value; }
        }

        private DateTime? minDate;
        public DateTime? MinDate
        {
            get { return minDate; }
        }
        private DateTime? maxDate;
        public DateTime? MaxDate
        {
            get { return maxDate; }
        }

        /// <summary>
        /// Node constructor with min/max-dates
        /// </summary>
        /// <param name="name"></param>
        /// <param name="label"></param>
        /// <param name="minDate">minimumdate in node-substree</param>
        /// <param name="maxDate">maximumdate in node-substree, equal to mindate if only one node is present in subtree</param>
        public Node(string name, string label, DateTime? minDate, DateTime? maxDate, RunStatus runStatus, string sourceFilename) : this(name, label)
        {
            this.minDate = minDate;
            this.maxDate = maxDate;
            this.runStatus = runStatus;
            this.sourceFilename = sourceFilename;
        }

        public virtual void AddEdge(Node node)
        {
            Edge edge = new Edge(this, node);
            edges.Add(edge);
        }

        public virtual void AddEdge(Node node, string attributes)
        {
            Edge edge = new Edge(this, node);
            edge.Attributes = attributes;
            edges.Add(edge);
        }

        /// <summary>
        /// Add an edge between this node and specified other node and optionally add default attributes based on min/max-datetime
        /// </summary>
        /// <param name="node"></param>
        /// <param name="addAutoAttributes"></param>
        public virtual void AddEdge(Node node, bool addAutoAttributes, string outputPath)
        {
            Edge edge = new Edge(this, node);
            if (addAutoAttributes)
            {
                if ((this.maxDate != null) && (node.minDate != null) && (this.maxDate > node.minDate))
                {
                    DateTime thisMaxDate = ((DateTime)this.maxDate);
                    DateTime nodeMinDate = ((DateTime)node.minDate);
                    string url = (this.sourceFilename != null) ? Path.GetDirectoryName(sourceFilename) : null;
                    string msgString = thisMaxDate.ToShortDateString() + " " + thisMaxDate.ToShortTimeString() + " > " + nodeMinDate.ToShortDateString() + " " + nodeMinDate.ToShortTimeString();
                    if (outputPath != null)
                    {
                        // Create HTML-table file
                        url = CreateHTMLIssueTable(Path.Combine(outputPath, "Issues"), this, node, msgString);
                    }
                    string attributes = "penwidth=3.0,color=red,weight=2,edgeURL=\"" + url.Replace("\\", "/") + "\",tooltip=\"" + msgString + "\"";
                    edge.Attributes = attributes;
                }
                else
                {
                    edge.Attributes = "weight=2";
                }
            }
            else
            {
                edge.Attributes = "weight=2";
            }
            edges.Add(edge);
        }

        /// <summary>
        /// Create HTML-file that contains specified message string with an issue between both specified nodes
        /// </summary>
        /// <param name="outputPath"></param>
        /// <param name="node1"></param>
        /// <param name="node2"></param>
        /// <param name="msgString"></param>
        /// <returns></returns>
        protected virtual string CreateHTMLIssueTable(string outputPath, Node node1, Node node2, string msgString)
        {
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            StreamWriter sw = null;
            string htmlFilename = Path.Combine(outputPath, "nodeissue" + ++IssueCount + ".html");

            try
            {
                sw = new StreamWriter(htmlFilename);

                sw.WriteLine("<html>");
                sw.WriteLine("<!-- WorkflowViz generated map -->");
                sw.WriteLine("  <head>");
                sw.WriteLine("    <meta charset=\"utf-8\">");
                sw.WriteLine("    <title>Node issue " + node1.Name + " and " + node2.Name + "</title>");
                sw.WriteLine("  </head>");
                sw.WriteLine("  <body>");
                sw.WriteLine("    <h1>Node issue</h1>");
                sw.WriteLine("<table border=\"1\" cellpadding=\"4\" cellspacing=\"0\">");
                sw.WriteLine("  <tr>");
                sw.WriteLine("    <td><b>Parent node</b></td>");
                sw.WriteLine("    <td>" + Path.GetFileName((Path.HasExtension(node1.sourceFilename)) ? Path.GetDirectoryName(Path.GetDirectoryName(node1.sourceFilename)) : Path.GetDirectoryName(node1.sourceFilename)) + "</td>");
                sw.WriteLine("  </tr>");
                sw.WriteLine("  <tr>");
                sw.WriteLine("    <td><b>Node1</b></td>");
                sw.WriteLine("    <td><a href=\"" + (Path.HasExtension(node1.sourceFilename) ? Path.GetDirectoryName(node1.sourceFilename) : node1.sourceFilename) + "\">" + node1.Label + "</a></td>");
                sw.WriteLine("  </tr>");
                sw.WriteLine("  <tr>");
                sw.WriteLine("    <td><b>Node2</b></td>");
                sw.WriteLine("    <td><a href=\"" + (Path.HasExtension(node2.sourceFilename) ? Path.GetDirectoryName(node2.sourceFilename) : node2.sourceFilename) + "\">" + node2.Label + "</a></td>");
                sw.WriteLine("  </tr>");
                sw.WriteLine("  <tr>");
                sw.WriteLine("    <td>Issue</td>");
                sw.WriteLine("    <td>" + msgString + "</td>");
                sw.WriteLine("  </tr>");
                sw.WriteLine("</table>");
                sw.WriteLine("</body>");
                sw.WriteLine("</html>");

                return htmlFilename;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while writing graph file " + Path.GetFileName(htmlFilename), ex);
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
