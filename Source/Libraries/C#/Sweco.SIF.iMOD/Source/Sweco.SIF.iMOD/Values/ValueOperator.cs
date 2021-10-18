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
    public enum ValueOperatorType
    {
        Unknown,
        Equal,
        GreatherThan,
        GreatherThanOrEqual,
        LessThan,
        LessThanOrEqual,
        Unequal
    }

    /// <summary>
    /// Class for definition of operator in simple value expression
    /// </summary>
    public class ValueOperator : IEquatable<ValueOperator>
    {
        /// <summary>
        /// Operator type of this ValueOperator
        /// </summary>
        public ValueOperatorType OperatorType;

        /// <summary>
        /// Construct new ValueOperator object with specified type
        /// </summary>
        /// <param name="operatorType"></param>
        public ValueOperator(ValueOperatorType operatorType)
        {
            this.OperatorType = operatorType;
        }

        /// <summary>
        /// Creates readable string for this operator
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            switch (OperatorType)
            {
                case ValueOperatorType.Equal:
                    return "==";
                case ValueOperatorType.GreatherThan:
                    return ">";
                case ValueOperatorType.GreatherThanOrEqual:
                    return ">=";
                case ValueOperatorType.LessThan:
                    return "<";
                case ValueOperatorType.LessThanOrEqual:
                    return "<=";
                case ValueOperatorType.Unequal:
                    return "!=";
                case ValueOperatorType.Unknown:
                    return "?";
                default:
                    throw new Exception("Undefined operator: " + OperatorType.ToString());
            }
        }

        /// <summary>
        /// Parse specified string with an operator to an ValueOperator object. Currently allowed strings: eq, gt, gteq, lt, lteq, uneq
        /// </summary>
        /// <param name="operatorString"></param>
        /// <returns></returns>
        public static ValueOperator ParseValueOperator(string operatorString)
        {
            operatorString = operatorString.ToLower();
            if (operatorString.Equals(ValueOperatorType.Equal.ToString().ToLower()) || operatorString.Equals("eq"))
            {
                return new ValueOperator(ValueOperatorType.Equal);
            }
            if (operatorString.Equals(ValueOperatorType.GreatherThan.ToString().ToLower()) || operatorString.Equals("gt"))
            {
                return new ValueOperator(ValueOperatorType.GreatherThan);
            }
            if (operatorString.Equals(ValueOperatorType.GreatherThanOrEqual.ToString().ToLower()) || operatorString.Equals("gteq"))
            {
                return new ValueOperator(ValueOperatorType.GreatherThanOrEqual);
            }
            if (operatorString.Equals(ValueOperatorType.LessThan.ToString().ToLower()) || operatorString.Equals("lt"))
            {
                return new ValueOperator(ValueOperatorType.LessThan);
            }
            if (operatorString.Equals(ValueOperatorType.LessThanOrEqual.ToString().ToLower()) || operatorString.Equals("lteq"))
            {
                return new ValueOperator(ValueOperatorType.LessThanOrEqual);
            }
            if (operatorString.Equals(ValueOperatorType.Unequal.ToString().ToLower()) || operatorString.Equals("uneq"))
            {
                return new ValueOperator(ValueOperatorType.Unequal);
            }
            return new ValueOperator(ValueOperatorType.Unknown);
        }

        /// <summary>
        /// Redefines == operator in C# and checks equality of ValueOperator and some ValueOperatorType value
        /// </summary>
        /// <param name="valueOperator"></param>
        /// <param name="operatorType"></param>
        /// <returns></returns>
        public static bool operator ==(ValueOperator valueOperator, ValueOperatorType operatorType)
        {
            return valueOperator.OperatorType == operatorType;
        }

        /// <summary>
        /// Redefines == operator in C# and checks unequality of ValueOperator and some ValueOperatorType value
        /// </summary>
        /// <param name="valueOperator"></param>
        /// <param name="operatorType"></param>
        /// <returns></returns>
        public static bool operator !=(ValueOperator valueOperator, ValueOperatorType operatorType)
        {
            return valueOperator.OperatorType != operatorType;
        }

        /// <summary>
        /// Compares two ValueOperators
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(ValueOperator other)
        {
            return (this.OperatorType == other.OperatorType);
        }

        /// <summary>
        /// Compares ValueOperator and some other objects
        /// </summary>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        /// <summary>
        /// HashCode for ValueOperator object: use base class implementation
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
