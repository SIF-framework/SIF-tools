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
    /// Class that defines the settings for visualisation/creation of a SIF-workflow
    /// </summary>
    public class GraphSettings : Settings
    {
        public static string InvisibleStyle = "invis";
        public static int InvisibleFontSize = 1;
        protected static Dot dot;

        /// <summary>
        /// Dot object for running Dot executable
        /// </summary>
        public static Dot Dot
        {
            get
            {
                if (dot == null)
                {
                    dot = new Dot();
                }
                return dot;
            }
            set { dot = value; }
        }

        /// <summary>
        /// Defines if an outer bounding box should be shown around the graph
        /// </summary>
        public bool IsOuterBBoxShown { get; set; }

        /// <summary>
        /// Name of fohnt family that is used for all labels in the graphs. Check GraphViz-manual for available fontnames.
        /// </summary>
        public string FontName { get; set; }

        public GraphSettings() : base()
        {
            IsOuterBBoxShown = false;
            FontName = Properties.Settings.Default.Fontname;
        }

        public GraphSettings(Settings settings) : base(settings)
        {
            IsOuterBBoxShown = false;
            FontName = Properties.Settings.Default.Fontname;
        }

        public GraphSettings(GraphSettings settings) : base(settings)
        {
            IsOuterBBoxShown = settings.IsOuterBBoxShown;
            FontName = settings.FontName;
        }
    }
}
