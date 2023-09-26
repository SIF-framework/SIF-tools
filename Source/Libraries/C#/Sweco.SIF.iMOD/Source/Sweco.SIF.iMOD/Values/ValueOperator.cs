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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.Values
{
    /// <summary>
    /// Available types of ValueOperator
    /// </summary>
    public enum ValueOperator
    {
        Undefined,
        Equal,
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Unequal
    }

    /// <summary>
    /// Class for definition of operator in simple value expression
    /// </summary>
    public static class ValueOperatorUtils
    {
        /// <summary>
        /// Creates readable string for this operator
        /// </summary>
        /// <returns></returns>
        public static string ToSymbolString(this ValueOperator valueOperator)
        {
            switch (valueOperator)
            {
                case ValueOperator.Equal:
                    return "==";
                case ValueOperator.GreaterThan:
                    return ">";
                case ValueOperator.GreaterThanOrEqual:
                    return ">=";
                case ValueOperator.LessThan:
                    return "<";
                case ValueOperator.LessThanOrEqual:
                    return "<=";
                case ValueOperator.Unequal:
                    return "!=";
                case ValueOperator.Undefined:
                    return "?";
                default:
                    throw new Exception("Undefined operator: " + valueOperator.ToString());
            }
        }

        /// <summary>
        /// Parse specified string with an operator to an valueOperator object. Currently allowed strings: eq, gt, gteq, lt, lteq, uneq
        /// </summary>-/
        /// <param name="operatorString"></param>
        /// <returns></returns>
        public static ValueOperator ParseString(string operatorString)
        {
            operatorString = operatorString.ToLower();
            if (operatorString.Equals(ValueOperator.Equal.ToString().ToLower()) || operatorString.Equals("eq"))
            {
                return ValueOperator.Equal;
            }
            if (operatorString.Equals(ValueOperator.GreaterThan.ToString().ToLower()) || operatorString.Equals("gt"))
            {
                return ValueOperator.GreaterThan;
            }
            if (operatorString.Equals(ValueOperator.GreaterThanOrEqual.ToString().ToLower()) || operatorString.Equals("gteq"))
            {
                return ValueOperator.GreaterThanOrEqual;
            }
            if (operatorString.Equals(ValueOperator.LessThan.ToString().ToLower()) || operatorString.Equals("lt"))
            {
                return ValueOperator.LessThan;
            }
            if (operatorString.Equals(ValueOperator.LessThanOrEqual.ToString().ToLower()) || operatorString.Equals("lteq"))
            {
                return ValueOperator.LessThanOrEqual;
            }
            if (operatorString.Equals(ValueOperator.Unequal.ToString().ToLower()) || operatorString.Equals("uneq"))
            {
                return ValueOperator.Unequal;
            }
            return ValueOperator.Undefined;
        }

    }
}
