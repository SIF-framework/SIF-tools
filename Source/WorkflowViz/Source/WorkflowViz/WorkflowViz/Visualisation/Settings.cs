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
using Sweco.SIF.WorkflowViz.GraphViz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.WorkflowViz.Visualisation
{
    /// <summary>
    /// Base class for visualisation settings
    /// </summary>
    public abstract class Settings
    {
        /// <summary>
        /// Source path of workflow to visualize
        /// </summary>
        public string SourcePath { get; set; }

        /// <summary>
        /// Visualization level: maximum number of workflow levels to show in one diagram
        /// </summary>
        public int MaxWorkflowLevels { get; set; }

        /// <summary>
        /// Recursion level: subworkflow depth to generate diagrams for
        /// </summary>
        public int MaxRecursionLevel { get; set; }

        /// <summary>
        /// Specifies if batchfiles are shown at highest graph level
        /// </summary>
        public bool IsBatchfileShown { get; set; }

        /// <summary>
        /// Specifies if Runscripts batchfiles are shown above toplevel workflows
        /// </summary>
        public bool IsRunScriptsMode { get; set; }

        /// <summary>
        /// Specifies if inconsistency check for edges is skipped (i.e. date order of logfiles and/or subworkflows, as shown by edge color)
        /// </summary>
        public bool IsEdgeCheckSkipped { get; set; }

        /// <summary>
        /// Comma-seperated substrings in subworkflow names that define visualization order
        /// </summary>
        public List<string> WorkflowOrder { get; set; }

        /// <summary>
        /// Output format of visualisation
        /// </summary>
        public OutputFormat OutputFormat { get; set; }

        protected Settings()
        {
            SourcePath = string.Empty;
            MaxWorkflowLevels = int.MaxValue;
            MaxRecursionLevel = int.MaxValue;
            IsBatchfileShown = true;
            IsRunScriptsMode = true;
            IsEdgeCheckSkipped = false;
            OutputFormat = OutputFormat.Undefined;
            WorkflowOrder = null;
        }

        protected Settings(Settings settings)
        {
            SourcePath = settings.SourcePath;
            MaxRecursionLevel = settings.MaxRecursionLevel;
            MaxWorkflowLevels = settings.MaxWorkflowLevels;
            IsBatchfileShown = settings.IsBatchfileShown;
            IsRunScriptsMode = settings.IsRunScriptsMode;
            IsEdgeCheckSkipped = settings.IsEdgeCheckSkipped;
            OutputFormat = settings.OutputFormat;
            WorkflowOrder = settings.WorkflowOrder;
        }
    }
}
