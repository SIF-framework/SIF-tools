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
using Sweco.SIF.WorkflowViz.Status;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.WorkflowViz.Workflows
{
    /// <summary>
    /// Class for representation, reading and handling of SIF-workflows, i.e. the whole structure with (sub)directories, batchfiles and logfiles.
    /// </summary>
    public class Workflow
    {
        protected static CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);
        protected static int wfCount = 0;

        /// <summary>
        /// String, starting with 'WF', followed by a numeric value, that identifies workflow
        /// </summary>
        public string ID { get; set; }

        /// <summary>
        /// String, starting with 'WF', based on label but without spaces, that identifies workflow
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// String to write in visual representations of workflow, including spaces and optional leading numeric values
        /// </summary>
        public string Label { get; set; }

        public Workflow Parent { get; private set; }
        public string FullPath { get; private set; }
        public List<Batchfile> Batchfiles { get; }
        public List<Workflow> SubWorkflows { get; private set; }
        public DateTime MinDate { get; private set; }
        public DateTime MaxDate { get; private set; }
        public int TotalBatchfileCount { get; private set; }
        public int TotalWorkflowCount { get; private set; }
        public RunStatus RunStatus { get; set; }

        protected Workflow()
        {
            this.ID = "WF" + ++wfCount;
            this.FullPath = null;
            this.Name = null;
            this.RunStatus = RunStatus.Undefined;
            this.Parent = null;

            MinDate = DateTime.MaxValue;
            MaxDate = DateTime.MinValue;
            TotalBatchfileCount = 0;
            TotalWorkflowCount = 0;

            Batchfiles = new List<Batchfile>();
            SubWorkflows = new List<Workflow>();
        }

        /// <summary>
        /// Read SIF-workflow in specified path
        /// </summary>
        /// <param name="path"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static Workflow ReadPath(string path, WorkflowSettings settings)
        {
            Workflow workflow = new Workflow();
            workflow.FullPath = path;
            workflow.Label = Path.GetFileName(path);
            workflow.Name = CorrctWorkflowName("WF" + workflow.Label.Replace(" ", string.Empty));

            try
            {
                // Read batchfiles (including logfiles)
                string[] batchFilenames = Directory.GetFiles(path, "*.bat");
                CommonUtils.SortAlphanumericStrings(batchFilenames);

                foreach (string batchFilename in batchFilenames)
                {
                    workflow.AddBatchFile(batchFilename, settings);
                }

                // Read subdirectories
                string[] subDirectories = Directory.GetDirectories(path);
                foreach (string subDirectory in subDirectories)
                {
                    workflow.AddSubWorkflow(subDirectory, settings);
                }
                workflow.SortSubWorkflows();

            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while reading Workflow path: " + path, ex);
            }

            return workflow;
        }

        private static string CorrctWorkflowName(string wfName)
        {
            string[] invalidSubstrings = new string[] { "'", "{", "}", "(", ")", "[", "]", "!", "@", "#", "$", "%", "^", "&", "=", ";", ",", "`", "~" };

            foreach (string s in invalidSubstrings)
            {
                if (wfName.Contains(s))
                {
                    wfName = wfName.Replace(s, string.Empty);
                }
            }

            return wfName;
        }

        /// <summary>
        /// Check if the specified string matches one of the strings in the specified list
        /// </summary>
        /// <param name="someString"></param>
        /// <param name="stringList"></param>
        /// <param name="isMatchCase"></param>
        /// <returns></returns>
        protected bool HasMatch(string someString, List<string> stringList, bool isMatchCase = true)
        {
            if (stringList != null)
            {
                foreach (string excludedString in stringList)
                {
                    if (isMatchCase)
                    {
                        if (someString.Contains(excludedString))
                        {
                            return true;
                        }
                    }
                    else
                    {
                        if (someString.ToLower().Contains(excludedString.ToLower()))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// Add batchfile object to this (Sub)Workflow object with specified settings
        /// </summary>
        /// <param name="batchFilename"></param>
        /// <param name="settings"></param>
        protected virtual void AddBatchFile(string batchFilename, WorkflowSettings settings)
        {
            if (!HasMatch(Path.GetFileNameWithoutExtension(batchFilename), settings.ExcludedStrings))
            {
                Batchfile batchfile = new Batchfile(batchFilename);
                AddBatchFile(batchfile);
            }
        }

        /// <summary>
        /// Add batchfile object to this (Sub)Workflow object
        /// </summary>
        /// <param name="batchfile"></param>
        protected virtual void AddBatchFile(Batchfile batchfile)
        {
            Batchfiles.Add(batchfile);
            TotalBatchfileCount++;

            // Correct min- and maxdates of workflow for batchfile dates
            if (batchfile.HasLogfile())
            {
                if (batchfile.Logfile.LastWriteTime < MinDate)
                {
                    MinDate = batchfile.Logfile.LastWriteTime;
                }
                if (batchfile.Logfile.LastWriteTime > MaxDate)
                {
                    MaxDate = batchfile.Logfile.LastWriteTime;
                }
            }

            // Correct runstatus of workflow for batchfile runstatus 
            if ((batchfile.RunStatus != RunStatus.Ignored) && !Utils.IsRunscriptsName(batchfile.Name))
            {
                if (RunStatus != batchfile.RunStatus)
                {
                    if (RunStatus == RunStatus.Undefined)
                    {
                        RunStatus = batchfile.RunStatus;
                    }
                    else if ((RunStatus == RunStatus.Error) || (batchfile.RunStatus == RunStatus.Error))
                    {
                        RunStatus = RunStatus.Error;
                    }
                    else if ((RunStatus == RunStatus.Outdated) || (batchfile.RunStatus == RunStatus.Outdated))
                    {
                        RunStatus = RunStatus.Outdated;
                    }
                    else if (((RunStatus == RunStatus.None) || (RunStatus == RunStatus.CompletedPartially) || (RunStatus == RunStatus.Completed)) && ((batchfile.RunStatus == RunStatus.None) || (batchfile.RunStatus == RunStatus.CompletedPartially) || (batchfile.RunStatus == RunStatus.Completed)))
                    {
                        RunStatus = RunStatus.CompletedPartially;
                    }
                    else
                    {
                        RunStatus = RunStatus.Unknown;
                    }
                }
            }
        }

        /// <summary>
        /// Add SubWorkflow to this (Sub)Workflow object with specified settings
        /// </summary>
        /// <param name="subDirectory"></param>
        /// <param name="settings"></param>
        protected virtual void AddSubWorkflow(string subDirectory, WorkflowSettings settings)
        {
            if (!HasMatch(Path.GetFileNameWithoutExtension(subDirectory), settings.ExcludedStrings))
            {
                Workflow subWorkflow = Workflow.ReadPath(subDirectory, settings);

                if (subWorkflow.TotalBatchfileCount > 0)
                {
                    AddSubWorkflow(subWorkflow);
                }
            }
        }

        /// <summary>
        /// Add SubWorkflow to this (Sub)Workflow object
        /// </summary>
        /// <param name="subWorkflow"></param>
        protected virtual void AddSubWorkflow(Workflow subWorkflow)
        {
            subWorkflow.Parent = this;
            SubWorkflows.Add(subWorkflow);
            TotalBatchfileCount += subWorkflow.TotalBatchfileCount;
            TotalWorkflowCount += 1 + subWorkflow.TotalWorkflowCount;

            if (subWorkflow.MinDate < MinDate)
            {
                MinDate = subWorkflow.MinDate;
            }
            if (subWorkflow.MaxDate > MaxDate)
            {
                MaxDate = subWorkflow.MaxDate;
            }

            // Correct runstatus of workflow for batchfile runstatus 
            if (subWorkflow.RunStatus != RunStatus.Ignored)
            {
                if (RunStatus != subWorkflow.RunStatus)
                {
                    if (RunStatus == RunStatus.Undefined)
                    {
                        RunStatus = subWorkflow.RunStatus;
                    }
                    else if ((RunStatus == RunStatus.Error) || (subWorkflow.RunStatus == RunStatus.Error))
                    {
                        RunStatus = RunStatus.Error;
                    }
                    else if ((RunStatus == RunStatus.Outdated) || (subWorkflow.RunStatus == RunStatus.Outdated))
                    {
                        RunStatus = RunStatus.Outdated;
                    }
                    else if (((RunStatus == RunStatus.None) || (RunStatus == RunStatus.CompletedPartially) || (RunStatus == RunStatus.Completed)) && ((subWorkflow.RunStatus == RunStatus.None) || (subWorkflow.RunStatus == RunStatus.CompletedPartially) || (subWorkflow.RunStatus == RunStatus.Completed)))
                    {
                        RunStatus = RunStatus.CompletedPartially;
                    }
                    else
                    {
                        RunStatus = RunStatus.Unknown;
                    }
                }
            }
        }

        /// <summary>
        /// Sort current SubWorkflows of this Workflow object in alphabetical order of names
        /// </summary>
        protected virtual void SortSubWorkflows()
        {
            List<Workflow> sortedWorkflows = new List<Workflow>();
            List<string> subWorkflowNames = new List<string>();
            Dictionary<string, Workflow> subWorkflowDictionary = new Dictionary<string, Workflow>();
            foreach (Workflow subWorkflow in SubWorkflows)
            {
                subWorkflowNames.Add(subWorkflow.Name);
                subWorkflowDictionary.Add(subWorkflow.Name, subWorkflow);
            }

            CommonUtils.SortAlphanumericStrings(subWorkflowNames);
            foreach (string subWorkflowName in subWorkflowNames)
            {
                sortedWorkflows.Add(subWorkflowDictionary[subWorkflowName]);
            }
            SubWorkflows = sortedWorkflows;
        }

        /// <summary>
        /// Sort current SubWorkflows of this Workflow object in specified order of labels
        /// </summary>
        /// <param name="wfLabelOrder"></param>
        public virtual void SortSubWorkflows(List<string> wfLabelOrder)
        {
            List<Workflow> newSubWorkflows = new List<Workflow>();
            List<Workflow> remainingSubworkflows = SubWorkflows;
            foreach (string wfLabel in wfLabelOrder)
            {
                List<Workflow> nextSubworkflows = new List<Workflow>();
                foreach (Workflow subWorkflow in remainingSubworkflows)
                {
                    if (subWorkflow.Label.StartsWith(wfLabel))
                    {
                        newSubWorkflows.Add(subWorkflow);
                    }
                    else
                    {
                        nextSubworkflows.Add(subWorkflow);
                    }
                }
                remainingSubworkflows = nextSubworkflows;
            }
            newSubWorkflows.AddRange(remainingSubworkflows);
            SubWorkflows = newSubWorkflows;
        }

        /// <summary>
        /// Write a representative string for this Workflow object, i.e. its name
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }
    }
}
