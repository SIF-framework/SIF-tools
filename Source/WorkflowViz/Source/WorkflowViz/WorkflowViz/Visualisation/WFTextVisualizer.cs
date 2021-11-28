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
    /// Class (simple) textual visualization of a SIF-workflow
    /// </summary>
    public class WFTextVisualizer : WFVisualizer
    {
        public static string TabString = "   ";

        public WFTextVisualizer() : base()
        {
        }

        /// <summary>
        /// Visualize specified workflow in a graph
        /// </summary>
        /// <param name="workflow"></param>
        /// <param name="outputPath"></param>
        /// <param name="recursionLevel">current recursionlevel which is 0 for the highest/top workflow level</param>
        /// <param name="settings">optional settings for this Visualize-call, or null to use settings of this WFGraphVisualizer object</param>
        /// <returns>A string that represents the resulting visualisation</returns>
        public override string Visualize(Workflows.Workflow workflow, string outputPath, int recursionLevel, Settings settings)
        {
            string wfString = Print(workflow, 0, settings.MaxWorkflowLevels);

            if (outputPath == null)
            {
                Console.Write(wfString);
            }
            else
            {
                string outputFilename = Path.Combine(outputPath, workflow.Name + ".txt");
                StreamWriter sw = null;
                try
                {
                    sw = new StreamWriter(outputFilename);
                    sw.Write(wfString);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while writing graph file " + Path.GetFileName(outputFilename), ex);
                }
                finally
                {
                    if (sw != null)
                    {
                        sw.Close();
                    }
                }
            }

            return wfString;
        }

        /// <summary>
        /// Print workflow to string
        /// </summary>
        /// <param name="workflow"></param>
        /// <param name="tabLevel"></param>
        /// <param name="maxWorkflowLevels"></param>
        /// <returns></returns>
        public virtual string Print(Workflow workflow, int tabLevel, int maxWorkflowLevels = int.MaxValue)
        {
            StringBuilder wfString = new StringBuilder();
            AddIndentedLine(wfString, workflow.Label, tabLevel);
            if (workflow.Batchfiles.Count > 0)
            {
                AddIndentedLine(wfString, "Batchfiles:", tabLevel + 1);
                foreach (Batchfile batchfile in workflow.Batchfiles)
                {
                    AddIndentedLine(wfString, batchfile.Name, tabLevel + 1);
                }
                AddIndentedLine(wfString, string.Empty, tabLevel + 1);
            }
            if (workflow.SubWorkflows.Count > 0)
            {
                if (maxWorkflowLevels > 0)
                {
                    AddIndentedLine(wfString, "Sub-workflows:", tabLevel + 1);
                    foreach (Workflow subWorkflow in workflow.SubWorkflows)
                    {
                        wfString.Append(Print(subWorkflow, tabLevel + 1, maxWorkflowLevels - 1));
                    }
                    AddIndentedLine(wfString, string.Empty, tabLevel + 1);
                }
            }

            return wfString.ToString();
        }

        /// <summary>
        /// Add indented string line to specified StringBuilder object
        /// </summary>
        /// <param name="wfString"></param>
        /// <param name="someString"></param>
        /// <param name="indentLevel"></param>
        protected virtual void AddIndentedLine(StringBuilder wfString, string someString, int indentLevel)
        {
            string tabs = string.Empty;
            while (indentLevel-- > 0)
            {
                tabs += TabString;
            }
            wfString.AppendLine(tabs + someString);
        }
    }
}
