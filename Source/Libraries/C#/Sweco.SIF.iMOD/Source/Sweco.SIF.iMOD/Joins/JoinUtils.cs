// Sweco.SIF.iMOD is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.iMOD.
// 
// Sweco.SIF.iMOD is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.iMOD is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.iMOD. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.Joins
{
    public static class JoinUtils
    {
        /// <summary>
        /// Retrieve JoinType from string value
        /// </summary>
        /// <param name="joinTypeString"></param>
        /// <returns></returns>
        /// <exception cref="ToolException"></exception>
        public static JoinType ParseJoinType(string joinTypeString)
        {
            if (!int.TryParse(joinTypeString, out int typeValue))
            {
                throw new ToolException("Could not parse integer value for JoinType:" + joinTypeString);
            }
            return ParseJoinType(typeValue);
        }

        /// <summary>
        /// Retrieve JoinType from int value
        /// </summary>
        /// <param name="typeValue"></param>
        /// <returns></returns>
        /// <exception cref="ToolException"></exception>
        public static JoinType ParseJoinType(int typeValue)
        {
            JoinType joinType;
            switch (typeValue)
            {
                case 0:
                    joinType = JoinType.Natural;
                    break;
                case 1:
                    joinType = JoinType.Inner;
                    break;
                case 2:
                    joinType = JoinType.FullOuter;
                    break;
                case 3:
                    joinType = JoinType.LeftOuter;
                    break;
                case 4:
                    joinType = JoinType.RightOuter;
                    break;
                default:
                    throw new ToolException("Unexpected JoinType value: " + typeValue);
            }

            return joinType;
        }
    }
}
