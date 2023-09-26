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
using Sweco.SIF.WorkflowViz.GraphViz;
using Sweco.SIF.WorkflowViz.Status;
using Sweco.SIF.WorkflowViz.Workflows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.WorkflowViz.Visualisation
{
    /// <summary>
    /// Class for visualisation of a SIF-workflow structure (defined by subdirectories, batchfiles and logfiles) with a GraphViz DiGraph
    /// </summary>
    public class WFGraphVisualizer : WFVisualizer
    {
        protected const string WorkflowFilenamePostfix = " workflow";

        protected static int dummyCount = 0;

        public WFGraphVisualizer(GraphSettings graphSettings) : base(graphSettings)
        {
        }

        /// <summary>
        /// Visualize specified workflow in a graph; create HTML-file with graph for this workflow and all subworkflows recursively; add hyperlinks to nodes and workflow clusters
        /// </summary>
        /// <param name="workflow"></param>
        /// <param name="outputPath"></param>
        /// <param name="recursionLevel">current recursionlevel which is 0 for the highest/top workflow level</param>
        /// <param name="settings">optional settings for this Visualize-call, or null to use settings of this WFGraphVisualizer object</param>
        /// <returns>Filename of the resulting visualisation</returns>
        public override string Visualize(Workflows.Workflow workflow, string outputPath, int recursionLevel, Settings settings = null)
        {
            GraphSettings graphSettings = InitializeGraphSettings(settings);

            if (outputPath == null)
            {
                throw new ToolException("Please specify an output path");
            }

            // Determine outputformat: 1) by settings; 2) by output filename extension; 3) by default HTML
            OutputFormat outputFormat = graphSettings.OutputFormat;
            if ((outputFormat == OutputFormat.Undefined) && !Path.GetExtension(outputPath).Equals(string.Empty))
            {
                outputFormat = Dot.ParseOutputFormat(Path.GetExtension(outputPath));
                if (outputFormat == OutputFormat.Undefined)
                {
                    throw new ToolException("Extension '" + Path.GetExtension(outputPath) + "' is not defined for outputfile: " + outputPath);
                }
            }
            if (outputFormat == OutputFormat.Undefined)
            {
                outputFormat = OutputFormat.HTML;
            }

            // Determine output path and filename: 1) for toplevel, use specified filename, or 2) use default Workflow.Name and 'workflow' postfix
            string outputFilename = null;
            if (!Path.GetExtension(outputPath).Equals(string.Empty))
            {
                if (recursionLevel == 0)
                {
                    // Use specified outputfilename only at the top level, otherwise ignore
                    outputFilename = Path.GetFileName(outputPath);
                }
                outputPath = Path.GetDirectoryName(outputPath);
            }
            else
            {
                outputPath = Path.Combine(outputPath, workflow.Label);
            }
            if (outputFilename != null)
            {
                outputFilename = Path.Combine(outputPath, outputFilename);
            }
            else
            {
                outputFilename = Path.Combine(outputPath, workflow.Label + WorkflowFilenamePostfix + "." + Dot.GetFormatString(outputFormat));
            }

            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // Check if subdirectories below the current path should be visualized as well
            if (recursionLevel < graphSettings.MaxRecursionLevel)
            {
                GraphSettings subsettings = new GraphSettings(graphSettings);
                subsettings.IsOuterBBoxShown = true;
                subsettings.IsBatchfileShown = true;

                // Call Visualize recursively for subworkflows: create HTML
                //for (int subWorkflowIdx = workflow.SubWorkflows.Count - 1; subWorkflowIdx > 0; subWorkflowIdx--)
                for (int subWorkflowIdx = 0; subWorkflowIdx < workflow.SubWorkflows.Count; subWorkflowIdx++)
                {
                    Workflow subWorkflow = workflow.SubWorkflows[subWorkflowIdx];
                    Console.WriteLine(Indent("Visualizing workflow " + subWorkflow.Label + " ...", recursionLevel + 1));
                    subsettings.SourcePath = subWorkflow.FullPath;
                    Visualize(subWorkflow, outputPath, recursionLevel + 1, subsettings);
                }
            }

            // Create HTML-file for this workflow
            string gvFilename = Path.Combine(outputPath, workflow.Label + WorkflowFilenamePostfix + ".gv");
            DiGraph diGraph = CreateDiGraph(workflow.Name, workflow, outputPath, (GraphSettings)graphSettings);
            diGraph.WriteGraphFile(gvFilename);

            int exitCode;
            if (outputFormat == OutputFormat.HTML)
            {
                Console.WriteLine(Indent("Creating " + Dot.GetFormatString(OutputFormat.CMAPX) + " and " + Dot.GetFormatString(OutputFormat.PNG) + "-file ...", recursionLevel + 1));
                string cmapxFilename = Path.Combine(outputPath, workflow.Label + WorkflowFilenamePostfix + "." + Dot.GetFormatString(OutputFormat.CMAPX));
                string pngFilename = Path.Combine(outputPath, workflow.Label + WorkflowFilenamePostfix + "." + Dot.GetFormatString(OutputFormat.PNG));
                exitCode = GraphSettings.Dot.Run(gvFilename, cmapxFilename, OutputFormat.CMAPX, pngFilename, OutputFormat.PNG);
                if (exitCode == 0)
                {
                    Console.WriteLine(Indent("Creating HTML-file ...", recursionLevel + 1));
                    exitCode = CreateHTMLFile(workflow.Name, outputFilename, cmapxFilename, Path.GetFileName(pngFilename));
                }
            }
            else
            {
                Console.WriteLine("\tCreating " + Dot.GetFormatString(outputFormat) + "-file ...");
                exitCode = GraphSettings.Dot.Run(gvFilename, outputFilename, outputFormat);
            }

            if (exitCode != 0)
            {
                throw new Exception("Some error occurred while running dot.exe");
            }

            return outputFilename;
        }

        protected virtual GraphSettings InitializeGraphSettings(Settings settings)
        {
            if (settings == null)
            {
                // Use settings from this GraphVisualizer object
                settings = this.settings;
                if (settings == null)
                {
                    // Create default settings
                    settings = new GraphSettings();
                }
            }

            if (!(settings is GraphSettings))
            {
                throw new Exception("WFGraphVisualizer.Visualize() should be called with GraphSettings object");
            }
            return (GraphSettings)settings;
        }

        /// <summary>
        /// Create a DiGraph object for specified SIF-workflow and settings
        /// </summary>
        /// <param name="name"></param>
        /// <param name="workflow"></param>
        /// <param name="outputPath"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual DiGraph CreateDiGraph(string name, Workflow workflow, string outputPath, GraphSettings settings)
        {
            DiGraph diGraph = new DiGraph(name, settings);

            AddWorkflow(diGraph, workflow, outputPath, 0, settings.MaxWorkflowLevels, settings.IsRunScriptsMode, settings.IsBatchfileShown, settings.IsOuterBBoxShown, null);

            return diGraph;
        }

        /// <summary>
        /// Add graph for subworkflow to specified graph
        /// </summary>
        /// <param name="graph"></param>
        /// <param name="workflow"></param>
        /// <param name="outputPath"></param>
        /// <param name="workflowLevel">Current workflow level in this diagram</param>
        /// <param name="maxWorkflowLevels">Maximum number of workflow levels to show in this diagram</param>
        /// <param name="isRunScriptsMode"></param>
        /// <param name="isBatchFileShown"></param>
        /// <param name="isCluster"></param>
        /// <param name="orderNode">A node that defines the order for the added subworkflow in the higher level workflow</param>
        /// <returns></returns>
        protected virtual GraphObject AddWorkflow(Graph graph, Workflow workflow, string outputPath, int workflowLevel, int maxWorkflowLevels, bool isRunScriptsMode, bool isBatchFileShown, bool isCluster = true, Node orderNode = null)
        {
            if ((workflow.Parent != null) && (workflow.Parent.Parent == null) && (workflow.SubWorkflows.Count == 0))
            {
                // Force batchfiles to be shown when a workflow does not have subworkflows
                isBatchFileShown = true;
            }

            if (workflowLevel == maxWorkflowLevels)
            {
                // Add workflows that are at the end of the graph (no shown batchfiles and no shown subworkflows) as a simple subgraph with a single node
                Node node = new Node("SN" + ++dummyCount, workflow.Label, workflow.MinDate, workflow.MaxDate, workflow.RunStatus, workflow.FullPath);
                node.URL = CreateURL(outputPath, workflow, WorkflowFilenamePostfix); 
                node.Attributes = "shape=folder,border=1,style=filled";
                node.Style = "filled";
                node.Color = GetStatusColor(workflow.RunStatus);
                graph.AddNode(node);

                if (orderNode != null)
                {
                    // Add runscripts node for this subworkflow
                    graph.AddNode(orderNode);

                    if (Utils.IsRunscriptsName(orderNode.Label))
                    {
                        orderNode.AddEdge(node, "color=grey, weight=2");
                    }
                    else
                    {
                        orderNode.AddEdge(node, "style=\"" + GraphSettings.InvisibleStyle + "\", weight=2");
                    }
                }
                return node;
            }
            else
            {
                // Add this workflow as a subgraph 
                SubGraph subgraph = new SubGraph(workflow.ID, (workflowLevel == 0) ? workflow.Label : string.Empty, isCluster);
                if (workflowLevel == 0)
                {
                    subgraph.Attributes = "ranksep=1.0,fontsize=" + Graph.Node_FontSize + ",labeljust=c";
                }
                subgraph.URL = CreateURL(outputPath, workflow, WorkflowFilenamePostfix);

                Node subGraphNode = null;
                if (workflowLevel > 0)
                {
                    subGraphNode = new Node("FN" + workflow.ID, workflow.Label);
                    subGraphNode.Attributes = "shape=folder,border=1,style=\"filled,bold\",color=gray10,fillcolor=\"" + GetStatusColor(workflow.RunStatus) + "\",fontname=\"" + ((GraphSettings) settings).FontName + " bold\"";
                    subgraph.AddNode(subGraphNode);

                    if (orderNode != null)
                    {
                        // Add runscripts node for this subworkflow
                        subgraph.AddNode(orderNode);

                        if (Utils.IsRunscriptsName(orderNode.Label))
                        {
                            orderNode.AddEdge(subGraphNode, "color=grey, weight=2");
                        }
                        else
                        {
                            orderNode.AddEdge(subGraphNode, "style=\"" + GraphSettings.InvisibleStyle + "\", weight=2");
                        }
                    }
                }

                // Add batchfiles of this workflow to the subgraph
                Node prevNode = null;
                Batchfile prevBatchfile = null;
                Node subWorkflowOrderNode = null;
                List<Node> runscriptsNodes = new List<Node>();
                List<Node> orderNodes = new List<Node>();
                if (isBatchFileShown)
                {
                    SubGraph batchFileSubGraph = new SubGraph("BS" + ++dummyCount, "", false);

                    // Create dummy node to fix order inside cluster
                    string nodeID = "ON" + ++dummyCount;
                    subWorkflowOrderNode = new Node(nodeID, nodeID, workflow.MinDate, workflow.MaxDate, workflow.RunStatus, workflow.FullPath);
                    subWorkflowOrderNode.Style = GraphSettings.InvisibleStyle;
                    subWorkflowOrderNode.Shape = "plain";
                    subWorkflowOrderNode.Attributes = "height=0.02,fontsize=\"" + GraphSettings.InvisibleFontSize + "\",margin=0.01";
                    if (workflow.SubWorkflows.Count > 0)
                    {
                        batchFileSubGraph.AddNode(subWorkflowOrderNode);
                    }

                    for (int batchfileIdx = 0; batchfileIdx < workflow.Batchfiles.Count; batchfileIdx++)
                    {
                        Batchfile batchfile = workflow.Batchfiles[batchfileIdx];
                        DateTime? dateTime = (batchfile.Logfile != null) ? (DateTime?)batchfile.Logfile.LastWriteTime : null;
                        Node node = new Node(batchfile.ID, batchfile.Name, dateTime, dateTime, batchfile.RunStatus, batchfile.Filename);
                        node.Style = "rounded,filled";
                        node.Color = GetStatusColor(batchfile.RunStatus);
                        node.URL = Path.GetDirectoryName(batchfile.Filename);

                        LogTable.Add(new LogEntry(batchfile));

                        // Handle RunScripts batchfiles differently when in RunScripts-mode
                        if (!isRunScriptsMode || (workflowLevel == maxWorkflowLevels) || !Utils.IsRunscriptsName(batchfile.Name))
                        {
                            // Create a new node for the current batchfile
                            if (prevNode != null)
                            {
                                prevNode.AddEdge(node, !settings.IsEdgeCheckSkipped, outputPath); 
                            }
                            else if (subGraphNode != null)
                            {
                                // This is the first batchfile for this subgraph, connect to subgraph label but do not show the edge
                                subGraphNode.AddEdge(node, "style=\"" + GraphSettings.InvisibleStyle + "\"");
                            }

                            if (!isRunScriptsMode)
                            {
                                batchFileSubGraph.AddNode(node);
                            }
                            else
                            {
                                subgraph.AddNode(node);
                            }

                            prevNode = node;
                            prevBatchfile = batchfile;
                        }
                        else
                        {
                            node.Label = node.Label.Replace(Batchfile.RunscriptsName + " ", Batchfile.RunscriptsName + "\\n");
                            runscriptsNodes.Add(node);
                        }
                    }
                    if (!isRunScriptsMode && (batchFileSubGraph.Nodes.Count > 0))
                    {
                        subgraph.AddSubGraph(batchFileSubGraph);

                        if ((workflow.SubWorkflows.Count > 0) && (batchFileSubGraph.Nodes.Count > 1))
                        {
                            subWorkflowOrderNode.AddEdge(batchFileSubGraph.Nodes[1], "style=\"" + GraphSettings.InvisibleStyle + "\", weight=2");
                            orderNodes.Add(subWorkflowOrderNode);
                        }
                    }
                }

                if (workflowLevel < maxWorkflowLevels)
                {
                    // Add subworkflows of this workflow to the subgraph
                    Workflow prevSubWorkflow = null;
                    GraphObject prevGraphObject = null;

                    List<string> runscriptsOrder = new List<string>();
                    if (settings.WorkflowOrder != null)
                    {
                        runscriptsOrder.InsertRange(0, settings.WorkflowOrder);
                    }
                    workflow.SortSubWorkflows(runscriptsOrder);
                    for (int subWorkflowIdx = 0; subWorkflowIdx < workflow.SubWorkflows.Count; subWorkflowIdx++)
                    {
                        Workflow subWorkflow = workflow.SubWorkflows[subWorkflowIdx];
                        subWorkflowOrderNode = null;
                        if ((isRunScriptsMode) && (workflowLevel < (maxWorkflowLevels - 1))) // && (workflowLevel == 0))
                        {
                            subWorkflowOrderNode = FindRunScriptsNode(subWorkflow, runscriptsNodes);
                        }
                        if ((subWorkflowOrderNode == null) && (workflowLevel < (maxWorkflowLevels - 1))) // && (workflowLevel == 0))
                        {
                            // Create dummy node to fix order inside cluster
                            string nodeID = "ON" + ++dummyCount;
                            subWorkflowOrderNode = new Node(nodeID, nodeID, workflow.MinDate, workflow.MaxDate, workflow.RunStatus, workflow.FullPath);
                            subWorkflowOrderNode.Style = GraphSettings.InvisibleStyle;
                            subWorkflowOrderNode.Shape = "plain";
                            subWorkflowOrderNode.Attributes = "height=0.02,fontsize=\""+ GraphSettings.InvisibleFontSize + "\",margin=0.01";
                        }
                        if (subWorkflowOrderNode != null)
                        {
                            orderNodes.Add(subWorkflowOrderNode);
                        }

                        GraphObject graphObject = AddWorkflow(subgraph, subWorkflow, outputPath, workflowLevel + 1, maxWorkflowLevels, isRunScriptsMode, isBatchFileShown, true, subWorkflowOrderNode);
                        if (prevSubWorkflow != null)
                        { 
                            if (workflow.Parent != null) 
                            {
                                if (graphObject is Node)
                                {
                                    AddSubgraphConnection(prevGraphObject, graphObject, outputPath);
                                }
                            }
                            if (!settings.IsEdgeCheckSkipped && (subgraph.IsCluster) && (prevGraphObject is SubGraph) && (graphObject is SubGraph))
                            {
                                DateTime subGraph1DateTime = prevSubWorkflow.MaxDate;
                                DateTime subGraph2DateTime = subWorkflow.MinDate;
                                bool hasInconsistency = (subGraph1DateTime > subGraph2DateTime);
                                ((SubGraph)prevGraphObject).AddEdge((SubGraph)graphObject, "style=dashed" + (hasInconsistency ? ", color=red, penwidth=2.0" : string.Empty) + ", constraint=false");
                            }
                        }
                        else if ((subGraphNode != null) && (graphObject is Node))
                        {
                            // This is the first batchfile for this subgraph, connect to subgraph label but do not show the edge
                            if (subWorkflowOrderNode != null)
                            {
                                subGraphNode.AddEdge(subWorkflowOrderNode, "style=\"" + GraphSettings.InvisibleStyle + "\"");
                            }
                            else
                            {
                                subGraphNode.AddEdge((Node)graphObject, "style=\"" + GraphSettings.InvisibleStyle + "\"");
                            }
                        }

                        prevSubWorkflow = subWorkflow;
                        prevGraphObject = graphObject;

                        if (workflowLevel < (maxWorkflowLevels - 1))
                        {
                            // Force order of runscripts above corresponding subworkflows
                            subgraph.DefineRank("same", orderNodes, "style=solid");
                        }
                    }
                }

                graph.AddSubGraph(subgraph);

                return subgraph;
            }
        }

        /// <summary>
        /// Retrieve a list with all labels of specified nodes
        /// </summary>
        /// <param name="nodes"></param>
        /// <returns></returns>
        protected List<string> RetrieveNodeLabels(List<Node> nodes)
        {
            List<string> labels = new List<string>();
            foreach (Node node in nodes)
            {
                labels.Add(node.Label);
            }
            return labels;
        }

        /// <summary>
        /// Retrieve a list with all labels of specified (sub)workflows
        /// </summary>
        /// <param name="workflows"></param>
        /// <returns></returns>
        protected List<string> RetrieveWorkflowLabels(List<Workflow> workflows)
        {
            List<string> labels = new List<string>();
            foreach (Workflow wf in workflows)
            {
                labels.Add(wf.Label);
            }
            return labels;
        }

        /// <summary>
        /// Indent a string with the specified tablevel
        /// </summary>
        /// <param name="someString"></param>
        /// <param name="indentLevel"></param>
        /// <returns></returns>
        protected virtual string Indent(string someString, int indentLevel)
        {
            string tabString = "\t";

            string tabStrings = string.Empty;
            for (int idx = 0; idx < indentLevel; idx++)
            {
                tabStrings += tabString;
            }
            return tabStrings + someString;
        }

        /// <summary>
        /// Retrieve order of specified workflows, partly defined by the labels of the specified nodes. 
        /// All workflows with labels that are equal to one of the specied nodelabels are first added in the node order, after this other workflows are added in alphabetic order.
        /// </summary>
        /// <param name="workflows"></param>
        /// <param name="nodes"></param>
        /// <returns></returns>
        protected virtual List<string> RetrieveWorkflowOrder(List<Workflow> workflows, List<Node> nodes)
        {
            List<string> nodeLabels = RetrieveNodeLabels(nodes);
            List<string> workflowLabels = RetrieveWorkflowLabels(workflows);

            // Loop through all nodes and check if they contain a workflow label
            List<string> orderedLabels = new List<string>();
            for (int idx = 0; idx < nodeLabels.Count; idx++)
            {
                string nodeLabel = nodeLabels[idx];

                // Correct for runscripts label
                if (Utils.IsRunscriptsName(nodeLabel))
                {
                    nodeLabel = nodeLabel.Replace(Batchfile.RunscriptsName + " ", string.Empty);
                    nodeLabel = nodeLabel.Replace(Batchfile.RunscriptsName + "\\n", string.Empty);
                }

                // Find corresponding workflow
                string workflowLabel = null;
                foreach (string wfLabel in workflowLabels)
                {
                    if (nodeLabel.Contains(wfLabel))
                    {
                        workflowLabel = wfLabel;
                    }
                }

                if ((workflowLabel != null) && !orderedLabels.Contains(workflowLabel))
                {
                    orderedLabels.Add(workflowLabel);
                }

            }
            return orderedLabels;
        }

        /// <summary>
        /// Create an URL string that points to specified workflow, for usage in dot objects
        /// </summary>
        /// <param name="outputPath"></param>
        /// <param name="workflow"></param>
        /// <param name="workflowPostfix"></param>
        /// <returns></returns>
        protected virtual string CreateURL(string outputPath, Workflow workflow, string workflowPostfix)
        {
            string htmlPath = Path.Combine(Path.Combine(outputPath, workflow.Label), workflow.Label + workflowPostfix + ".HTML");
            string url;
            if (File.Exists(htmlPath))
            {
                // A HTML-file for this subworkflow exists, add hyperlink to HTML-file of that subworkflow
                url = htmlPath;
            }
            else
            {
                // No HTML-file exists for subworkflow, add hyperlink to subdirectory
                url = workflow.FullPath;
            }
            return url;
        }

        /// <summary>
        /// Craete an HTML-file with dot.exe for the specified CMAPX- and corresponding PNG-files, that have been created before with dot.exe
        /// </summary>
        /// <param name="graphName"></param>
        /// <param name="htmlFilename"></param>
        /// <param name="cmapxFilename"></param>
        /// <param name="pngFilename"></param>
        /// <returns></returns>
        protected virtual int CreateHTMLFile(string graphName, string htmlFilename, string cmapxFilename, string pngFilename)
        {
            StreamReader sr = null;
            StreamWriter sw = null;
            try
            {
                sr = new StreamReader(cmapxFilename);
                string cmpaxString = sr.ReadToEnd();
                cmpaxString = cmpaxString.Replace(FileUtils.EnsureTrailingSlash(Path.GetDirectoryName(htmlFilename)), string.Empty);
                cmpaxString = cmpaxString.Replace(FileUtils.EnsureTrailingSlash(Path.GetDirectoryName(htmlFilename)).Replace("\\", "/"), string.Empty);

                sw = new StreamWriter(htmlFilename);
                sw.WriteLine("<img src=\"" + pngFilename + "\" usemap=\"#" + Utils.CorrectName(graphName) + "\" alt=\"graphviz graph\" />");
                sw.WriteLine("<!-- WorkflowViz generated map -->");
                sw.Write(cmpaxString);

                return 0;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while writing graph file " + Path.GetFileName(htmlFilename), ex);
            }
            finally
            {
                if (sw != null)
                {
                    sr.Close();
                    sw.Close();
                }
            }
        }

        /// <summary>
        /// Find Node object with RunScripts batchfile that corresponds with specified subworkflow
        /// </summary>
        /// <param name="subWorkflow"></param>
        /// <param name="batchfiles"></param>
        /// <returns></returns>
        protected virtual Node FindRunScriptsNode(Workflow subWorkflow, List<Node> runscriptsNodes)
        {
            string subWorkflowLabel = subWorkflow.Label.ToLower();
            foreach (Node node in runscriptsNodes)
            {
                string nodeLabel = node.Label.Replace("\\n", " ");
                if (nodeLabel.Replace(Batchfile.RunscriptsName + " ", string.Empty).Replace(Batchfile.RunscriptsName + "\\n", string.Empty).ToLower().Equals(subWorkflowLabel))
                {
                    return node;
                }
            }
            return null;
        }

        /// <summary>
        /// Add a connection between two GraphViz SubGraph objects
        /// </summary>
        /// <param name="object1"></param>
        /// <param name="object2"></param>
        /// <param name="outputPath"></param>
        protected virtual void AddSubgraphConnection(GraphObject object1, GraphObject object2, string outputPath)
        {
            if ((object1 is SubGraph) && (object2 is SubGraph))
            {
                SubGraph subgraph1 = (SubGraph)object1;
                SubGraph subgraph2 = (SubGraph)object2;

                // Add edge between last node of previous subgraph to first node of next subgraph
                if ((subgraph1.Nodes.Count > 0) && (subgraph2.Nodes.Count > 0))
                {
                    subgraph1.Nodes[subgraph1.Nodes.Count - 1].AddEdge(subgraph2.Nodes[0], !settings.IsEdgeCheckSkipped, outputPath);
                }
            }
            else if ((object1 is Node) && (object2 is Node))
            {
                Node node1 = (Node)object1;
                Node node2 = (Node)object2;

                node1.AddEdge(node2, !settings.IsEdgeCheckSkipped, outputPath);
            }
            else if ((object1 is Node) && (object2 is SubGraph))
            {
                Node node1 = (Node)object1;
                SubGraph subgraph2 = (SubGraph)object2;

                if (subgraph2.Nodes.Count > 0)
                {
                    node1.AddEdge(subgraph2.Nodes[0], !settings.IsEdgeCheckSkipped, outputPath);
                }
            }
            else if ((object1 is SubGraph) && (object2 is Node))
            {
                SubGraph subgraph1 = (SubGraph)object1;
                Node node2 = (Node)object2;

                if (subgraph1.Nodes.Count > 0)
                {
                    subgraph1.Nodes[subgraph1.Nodes.Count - 1].AddEdge(node2, !settings.IsEdgeCheckSkipped, outputPath);
                }
            }
            else
            {
                throw new Exception("Unsupported combination of Graphobjects");
            }
        }

        /// <summary>
        /// Get the dot-colorname for the specified WorkflowViz-status 
        /// </summary>
        /// <param name="runStatus"></param>
        /// <returns></returns>
        protected virtual string GetStatusColor(RunStatus runStatus)
        {
            string color = null;
            switch (runStatus)
            {
                case RunStatus.Undefined:
                    color = "lightgrey";
                    break;
                case RunStatus.Completed:
                    color = "darkolivegreen3";
                    break;
                case RunStatus.None:
                    color = "grey";
                    break;
                case RunStatus.Ignored:
                    color = "grey95";
                    break;
                case RunStatus.Outdated:
                    color = "goldenrod1";
                    break;
                case RunStatus.Error:
                    color = "#ff8080"; // light red
                    break;
                case RunStatus.CompletedPartially:
                    color = "khaki1";
                    break;
                case RunStatus.Unknown:
                    color = "firebrick1";
                    break;
                default:
                    throw new Exception("Invalid status: " + runStatus);
            }
            return color;
        }

        /// <summary>
        /// Add edges from all specified nodes to all specified subgraphs that contain one or more nodes
        /// </summary>
        /// <param name="nodes"></param>
        /// <param name="subgraphs"></param>
        protected virtual void AddRunscriptEdges(List<Node> nodes, List<SubGraph> subgraphs)
        {
            // Check for Runscript-batchfiles that run a subworkflow
            foreach (Node node in nodes)
            {
                if ((node.Label != null) && Utils.IsRunscriptsName(node.Label))
                {
                    // Find subworkflow with corresponding name
                    string subGraphLabel = node.Label.Replace(Batchfile.RunscriptsName + " ", string.Empty).Replace(Batchfile.RunscriptsName + "\\n", string.Empty);
                    SubGraph subgraph = FindSubGraph(subgraphs, subGraphLabel);
                    if ((subgraph != null) && (subgraph.Nodes.Count > 0))
                    {
                        // Get first node of subgraph
                        Node sNode1 = subgraph.Nodes[0];
                        node.AddEdge(sNode1);
                    }
                }
            }
        }

        /// <summary>
        /// Find the SubGraph with specified label within list of specified subGraphs
        /// </summary>
        /// <param name="subgraphs"></param>
        /// <param name="label"></param>
        /// <returns></returns>
        protected virtual SubGraph FindSubGraph(List<SubGraph> subgraphs, string label)
        {
            foreach (SubGraph subgraph in subgraphs)
            {
                if (subgraph.Label.Equals(label))
                {
                    return subgraph;
                }
            }
            return null;
        }
    }
}
