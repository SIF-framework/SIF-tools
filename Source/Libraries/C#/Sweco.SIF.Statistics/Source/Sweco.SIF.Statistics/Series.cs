// Sweco.SIF.Statistics is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.Statistics.
// 
// Sweco.SIF.Statistics is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.Statistics is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.Statistics. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Statistics
{
    /// <summary>
    /// This class stores a series of float values and offers methods to compute several basic statistics 
    /// </summary>
    public class Series
    {
        /// <summary>
        /// Actual values of series to calculate statistics for
        /// </summary>
        protected float[] values;

        /// <summary>
        /// Values of series to calculate statistics for
        /// </summary>
        public float[] Values
        {
            get { return values; }
            set { values = value; }
        }

        /// <summary>
        /// Optional remarks per series value, or null if not used
        /// </summary>
        protected string[] remarks;

        /// <summary>
        /// Optional remarks per series value, or null if not used
        /// </summary>
        public string[] Remarks
        {
            get { return remarks; }
            set { remarks = value; }
        }

        /// <summary>
        /// Optional integer status per series value, or null if not used
        /// </summary>
        protected int[] statuses;

        /// <summary>
        /// Optional integer status per series value, or null if not used
        /// </summary>
        public int[] Statuses
        {
            get { return statuses; }
            set { statuses = value; }
        }

        /// <summary>
        /// Number of current series values to create statistics for
        /// </summary>
        public int Count
        {
            get { return (values != null) ? values.Length : 0; }
        }

        /// <summary>
        /// Create empty Series object
        /// </summary>
        public Series()
        {
        }

        /// <summary>
        /// Create Series object for specified values
        /// </summary>
        public Series(float[] values)
        {
            this.values = values;
        }

        /// <summary>
        /// Create Series object for specified values, remarks and statuses
        /// </summary>
        public Series(float[] values, string[] remarks = null, int[] statuses = null)
        {
            if (((remarks != null) && (values.Length != remarks.Length)) || ((statuses != null) && (values.Length != statuses.Length)))
            {
                throw new Exception("Length of values, remarks and/or statuses arrays don't match");
            }

            this.values = values;
            this.remarks = remarks;
            this.statuses = statuses;
        }

        /// <summary>
        /// Create Series object for specified values, remarks and statuses
        /// </summary>
        public Series(List<float> values, List<string> remarks = null, List<int> statuses = null)
        {
            if (((remarks != null) && (values.Count() != remarks.Count())) || ((statuses != null) && (values.Count() != statuses.Count())))
            {
                throw new Exception("Length of values, remarks and/or statuses arrays don't match");
            }

            this.values = values.ToArray();
            if (remarks != null)
            {
                this.remarks = remarks.ToArray();
            }
            if (statuses != null)
            {
                this.statuses = statuses.ToArray();
            }
        }

        /// <summary>
        /// Return a new Series-object without the items with float.NaN-values and optionally specified other NaN-values
        /// </summary>
        /// <returns></returns>
        public Series RemoveNaNValues(List<float> nanValueList = null)
        {
            List<float> nonNaNvalues = new List<float>();
            List<string> nonNaNremarks = new List<string>();
            List<int> nonNaNstatuses = new List<int>();
            for (int i = 0; i < values.Length; i++)
            {
                if (!values[i].Equals(float.NaN))
                {
                    nonNaNvalues.Add(values[i]);
                    nonNaNremarks.Add(remarks[i]);
                    nonNaNstatuses.Add(statuses[i]);
                }
            }
            if (nanValueList != null)
            {
                List<float> tmpNonNaNvalues = nonNaNvalues;
                List<string> tmpNonNaNremarks = nonNaNremarks;
                List<int> tmpNonNaNstatuses = nonNaNstatuses;
                nonNaNvalues = new List<float>();
                nonNaNremarks = new List<string>();
                nonNaNstatuses = new List<int>();
                for (int i = 0; i < tmpNonNaNvalues.Count; i++)
                {
                    if (!nanValueList.Contains(tmpNonNaNvalues[i]))
                    {
                        nonNaNvalues.Add(tmpNonNaNvalues[i]);
                        nonNaNremarks.Add(tmpNonNaNremarks[i]);
                        nonNaNstatuses.Add(tmpNonNaNstatuses[i]);
                    }
                }
            }
            return new Series(nonNaNvalues, nonNaNremarks, nonNaNstatuses);
        }

        /// <summary>
        /// Calculates sum over values-array 
        /// </summary>
        /// <returns></returns>
        public float Sum()
        {
            float sum = 0;
            for (int i = 0; i < values.Length; i++)
            {
                sum += values[i];
            }
            return sum;
        }

        /// <summary>
        /// Calculates average over values-array
        /// </summary>
        /// <returns></returns>
        public float Average()
        {
            return Sum() / values.Length;
        }

        /// <summary>
        /// Calculates min over values-array 
        /// </summary>
        /// <returns></returns>
        public float Min()
        {
            float min = float.MaxValue;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] < min)
                {
                    min = values[i];
                }
            }
            return min;
        }

        /// <summary>
        /// Calculates max over values-array 
        /// </summary>
        /// <returns></returns>
        public float Max()
        {
            float max = float.MinValue;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] > max)
                {
                    max = values[i];
                }
            }
            return max;
        }

        /// <summary>
        /// Calculates percentiles-array 100 percentile values
        /// </summary>
        /// <returns>percentile array, or null if no values are present</returns>
        public float[] Percentiles()
        {
            float[] percentiles = null;

            // Compute percentiles 
            if ((values != null) && (values.Length > 0))
            {
                List<float> sortedValues;
                sortedValues = new List<float>(values);
                sortedValues.Sort();
                percentiles = new float[101];
                for (int p = 0; p <= 100; p++)
                {
                    int index = (int)(((float)p / 100.0) * (sortedValues.Count - 1));
                    percentiles[p] = sortedValues[index];
                }
            }
            return percentiles;
        }

        /// <summary>
        /// Calculates variance of current values
        /// </summary>
        /// <returns></returns>
        public float Variance()
        {
            float sum = 0;
            int count = 0;
            float avg = Average();
            for (int i = 0; i < values.Length; i++)
            {
                sum += (values[i] - avg) * (values[i] - avg);
                count++;
            }
            return sum / count;
        }

        /// <summary>
        /// Calculates sample variance of current values
        /// </summary>
        public float SampleVariance()
        {
            float sum = 0;
            int count = 0;
            float avg = Average();
            for (int i = 0; i < values.Length; i++)
            {
                sum += (values[i] - avg) * (values[i] - avg);
                count++;
            }
            return sum / (count - 1);
        }

        /// <summary>
        /// Calculates standard deviation of current values: square root of variance
        /// </summary>
        public float StandardDeviation()
        {
            return (float)Math.Sqrt(Variance());
        }

        /// <summary>
        /// Calculates standard error of current values: square root of sample variance
        /// </summary>
        public float StandardError()
        {
            return (float)Math.Sqrt(SampleVariance());
        }

        /// <summary>
        /// Calculates autocovariance of current values for given lag
        /// </summary>
        public float AutoCovariance(int lag)
        {
            float avg = Average();
            float sum = 0;
            float count = 0;
            for (int i = 0; i < values.Length - lag; i++)
            {
                count++;
                sum += (values[i] - avg) * (values[i + lag] - avg);
            }
            return sum / count;
        }

        /// <summary>
        /// Calculates autocorrelaton of current values for given lag: autocovariance / variance
        /// </summary>
        public float Autocorrelation(int lag)
        {
            return AutoCovariance(lag) / Variance();
        }

        /// <summary>
        /// Calculates sample autocorrelaton of current values for given lag: autocovariance / sample variance
        /// </summary>
        public float SampleAutocorrelation(int lag)
        {
            return AutoCovariance(lag) / SampleVariance();
        }

        /// <summary>
        /// Calculates Calculate natural (base e) logarithm of current values
        /// </summary>
        public Series LogTransform()
        {
            Series newSeries = this.Copy();
            newSeries.values = values.LogTransform();
            return newSeries;
        }

        /// <summary>
        /// Copy values, remarks and statuses to a new Series object
        /// </summary>
        /// <returns></returns>
        public virtual Series Copy()
        {
            return (Series)this.MemberwiseClone();
        }
    }
}
