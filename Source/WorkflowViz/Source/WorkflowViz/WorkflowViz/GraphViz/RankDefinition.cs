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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.WorkflowViz.GraphViz
{
    public class RankDefinition
    {
        /// <summary>
        /// Rank constraints on the nodes in a subgraph. One of: same, min, max, source, sink
        /// </summary>
        public string RankType { get; set; }

        public List<Node> RankedNodes { get; set; }

        public string EdgeAttributes { get; set; }

        public RankDefinition(string rankType, List<Node> rankedNodes, string edgeAttributes)
        {
            RankType = rankType;
            RankedNodes = rankedNodes;
            EdgeAttributes = edgeAttributes;
        }

        public override string ToString()
        {
            List<string> nodeNames = new List<string>();
            foreach (Node node in RankedNodes)
            {
                nodeNames.Add(node.Name);
            }
            return "Type: " + RankType + "; " + CommonUtils.ToString(nodeNames, "->");
        }
    }

}
