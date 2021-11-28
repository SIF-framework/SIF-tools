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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.WorkflowViz.GraphViz
{
    /// <summary>
    /// Class for storing a dot edge object, including attributes
    /// </summary>
    public class Edge
    {
        protected Node node1;
        public Node Node1
        {
            get { return node1; }
            set { node1 = value; }
        }

        protected Node node2;
        public Node Node2
        {
            get { return node2; }
            set { node2 = value; }
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

        public Edge(Node node1, Node node2)
        {
            this.node1 = node1;
            this.node2 = node2;
            this.attributes = null;
        }
    }
}
