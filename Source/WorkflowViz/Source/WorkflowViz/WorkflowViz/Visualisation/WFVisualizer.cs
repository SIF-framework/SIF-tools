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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.WorkflowViz.Visualisation
{
    /// <summary>
    /// Base class for implementations that can visualize a SIF-workflow
    /// </summary>
    public abstract class WFVisualizer
    {
        protected Workflow workflow;
        protected Settings settings;

        public LogTable LogTable { get; set; }

        public WFVisualizer()
        {
            LogTable = new LogTable();
        }

        protected WFVisualizer(Settings settings) : this()
        {
            this.settings = settings;
        }

        /// <summary>
        /// Visualize specified workflow in a graph
        /// </summary>
        /// <param name="workflow"></param>
        /// <param name="outputPath"></param>
        /// <param name="recursionLevel">current recursionlevel which is 0 for the highest/top workflow level</param>
        /// <param name="settings"></param>
        /// <returns>Some string that represents the resulting visualisation</returns>
        public abstract string Visualize(Workflow workflow, string outputPath, int recursionLevel, Settings settings = null);
    }
}
