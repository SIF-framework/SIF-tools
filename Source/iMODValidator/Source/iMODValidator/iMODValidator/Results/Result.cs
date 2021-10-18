// iMODValidator is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODValidator.
// 
// iMODValidator is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODValidator is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODValidator. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.iMOD.Legends;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Results
{
    /// <summary>
    /// This base class represents any kind of result from some action
    /// </summary>
    public abstract class Result
    {
        protected CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

        /// <summary>
        /// A name that defines the type of this result, e.g. Error, Warning, Information, etc
        /// </summary>
        public abstract string TypeName { get; }

        /// <summary>
        /// An integer value (or code) that represents this result(type) (and can be used in a maplegend, etc)
        /// </summary> 
        protected int resultValue;
        public int ResultValue
        {
            get { return resultValue; }
            set { resultValue = value; }
        }

        /// <summary>
        /// The number of times that this result has occurred  (for some given location, situation, moment, etc)
        /// </summary>
        protected long resultCount;
        public long ResultCount
        {
            get { return resultCount; }
            set { resultCount = value; }
        }

        /// <summary>
        /// Some other value related to this result
        /// </summary>
        protected long value1;
        public long Value1
        {
            get { return value1; }
            set { value1 = value; }
        }

        /// <summary>
        /// Some other value related to this result
        /// </summary>
        protected long value2;
        public long Value2
        {
            get { return value2; }
            set { value2 = value; }
        }

        /// <summary>
        /// A short description of this type of result
        /// </summary>
        protected string shortDescription = "";
        public string ShortDescription
        {
            get { return shortDescription; }
            set { shortDescription = value; }
        }

        /// <summary>
        /// A more detailed description of this type of result. If not defined, the short description will be used
        /// </summary>
        protected string detailedDescription;
        public string DetailedDescription
        {
            get
            {
                if ((detailedDescription == null) || detailedDescription.Equals(""))
                {
                    return shortDescription;
                }
                else
                {
                    return detailedDescription;
                }
            }
            set { detailedDescription = value; }
        }

        protected Result()
        {
            this.resultCount = 1;
            this.value1 = 0;
            this.value2 = 0;
        }

        public Result(int resultValue, string shortDescription, string detailedDescription = "") : this()
        {
            this.resultValue = resultValue;
            this.shortDescription = shortDescription;
            this.detailedDescription = detailedDescription;
        }

        /// <summary>
        /// Creates a legendclass for the resultvalue with the short and (optionally) detailed descriptions 
        /// </summary>
        /// <returns></returns>
        public ValueLegendClass CreateLegendValueClass()
        {
            return new ValueLegendClass(resultValue, resultValue + " - " + shortDescription, detailedDescription);
        }

        /// <summary>
        /// Creates a legendclass for the resultvalue with the short and (optionally) detailed descriptions and specified color
        /// </summary>
        /// <param name="color">the color of this class in the legend</param>
        /// <param name="isValueShownInLabel">if true, the resultvalue is added in the legendlabel, otherwise just the ahortdescription is shown</param>
        /// <returns></returns>
        public ValueLegendClass CreateLegendValueClass(Color color, bool isValueShownInLabel = false)
        {
            if (isValueShownInLabel)
            {
                return new ValueLegendClass(resultValue, resultValue + " - " + shortDescription, color, detailedDescription);
            }
            else
            {
                return new ValueLegendClass(resultValue, shortDescription, color, detailedDescription);
            }
        }
    }
}
