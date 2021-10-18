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
    /// Method used to find outliers
    /// </summary>
    public enum OutlierMethodEnum
    {
        IQR,
        HistogramGap
    }

    /// <summary>
    /// Base range that is used to find valid non-outlier values. The larger the base interval, the less outliers will be found
    /// </summary>
    public enum OutlierBaseRangeEnum
    {
        Pct95_5,
        Pct90_10,
        Pct75_25
    }

    /// <summary>
    /// This class is able to calculate multiple statistics in one run for a list of float values, which gives some performance advantages. 
    /// Skipped values can be defined. Also the class supports the HistogramGap method for finding outliers. Statistics are calculated by calling a ComputeStatistics method.
    /// </summary>
    public class Statistics
    {
        /// <summary>
        /// List of values to calculate statistics for
        /// </summary>
        protected List<float> values;

        /// <summary>
        /// List of values that are excluded from statistics
        /// </summary>
        protected List<float> skippedValuesList;

        /// <summary>
        /// Number of values for which statistics were calculated (acfter optional removal(s) of skipped values)
        /// </summary>
        protected long count;

        /// <summary>
        /// Total number of added values (before optional removal(s) of skipped values)
        /// </summary>
        protected long totalCount;

        /// <summary>
        /// Percentage of current amount of values (after optional removal of skipped values) over total amount of values that were added (before optional removal of skipped values)
        /// </summary>
        protected float countPercentage;

        /// <summary>
        /// Calculated minimum value
        /// </summary>
        protected float min;

        /// <summary>
        /// Calculated maximum value
        /// </summary>
        protected float max;

        /// <summary>
        /// Calculated sum of values
        /// </summary>
        protected float sum;

        /// <summary>
        /// Calculated (arithmetic) mean of values
        /// </summary>
        protected float mean;

        /// <summary>
        /// Calculated standard deviation of values
        /// </summary>
        protected float sd;

        /// <summary>
        /// Calculated sample standard deviation of values
        /// </summary>
        protected float sampleSD;

        /// <summary>
        /// Calculated median (or P50) of values
        /// </summary>
        protected float median;

        /// <summary>
        /// Calculated Q1 (first quantile or P25) of values
        /// </summary>
        protected float q1;

        /// <summary>
        /// Calculated Q3 (third quantile or P75) of values
        /// </summary>
        protected float q3;

        /// <summary>
        /// Calculated Inter Quartile Range (IQR): Q3 - Q1
        /// </summary>
        protected float iqr;

        /// <summary>
        /// Calculated quartile skew coefficient (qs)
        /// </summary>
        protected float quartileSkew;

        /// <summary>
        /// Array (with length 101) of calculated percentiles 
        /// </summary>
        protected float[] percentiles;

        /// <summary>
        /// Current number of values (after optional removal(s) of skipped values)
        /// </summary>
        public long Count
        {
            get { return count; }
        }

        /// <summary>
        /// Total number of added values (before optional removal(s) of skipped values)
        /// </summary>
        public long TotalCount
        {
            get { return totalCount; }
        }

        /// <summary>
        /// Percentage of current amount of values (after optional removal of skipped values) over total amount of values that were added (before optional removal of skipped values)
        /// </summary>
        public float CountPercentage
        {
            get { return countPercentage; }
        }

        /// <summary>
        /// Calculated minimum value
        /// </summary>
        public float Min
        {
            get { return min; }
        }

        /// <summary>
        /// Calculated maximum value
        /// </summary>
        public float Max
        {
            get { return max; }
        }

        /// <summary>
        /// Calculated (arithmetic) mean of values
        /// </summary>
        public float Mean
        {
            get { return mean; }
        }

        /// <summary>
        /// Calculated standard deviation of values
        /// </summary>
        public float SD
        {
            get { return sd; }
        }

        /// <summary>
        /// Calculated sample standard deviation of values, which uses (N - 1.5) instead of N in the calcution of the variance.
        /// (See https://en.wikipedia.org/wiki/Standard_deviation)
        /// </summary>
        public float SampleSD
        {
            get { return sampleSD; }
        }

        /// <summary>
        /// Calculated median (or P50) of values
        /// </summary>
        public float Median
        {
            get { return median; }
        }

        /// <summary>
        /// Calculated Q1 (first quantile or P25) of values
        /// </summary>
        public float Q1
        {
            get { return q1; }
        }

        /// <summary>
        /// Calculated Q3 (third quantile or P75) of values
        /// </summary>
        public float Q3
        {
            get { return q3; }
        }

        /// <summary>
        /// Calculated Inter Quartile Range (IQR): Q3 - Q1, a robust measure of variability
        /// </summary>
        public float IQR
        {
            get { return iqr; }
        }

        /// <summary>
        /// Array (with length 101) of calculated percentiles 
        /// </summary>
        public float[] Percentiles
        {
            get { return percentiles; }
        }

        /// <summary>
        /// Calculated sum of values
        /// </summary>
        public float Sum
        {
            get { return sum; }
        }

        /// <summary>
        /// The quartile skew coefficient (qs) is a more resistant measure of skewness.
        /// the difference in distances of the upper and lower quartiles from the median, 
        /// divided by the IQR. A right-skewed distribution again has positive qs. 
        /// A left-skewed distribution has negative qs. Similar to the trimmed mean and 
        /// IQR, qs uses the central 50 percent of the data. 
        /// </summary>
        public float QuartileSkew
        {
            get { return quartileSkew; }
        }

        /// <summary>
        /// Lower value of calculated range with outliers
        /// </summary>
        public float outlierRangeLowerValue;

        /// <summary>
        /// Upper value of calculated range with outliers
        /// </summary>
        public float outlierRangeUpperValue;

        /// <summary>
        /// Lower value of calculated range with outliers
        /// </summary>
        public float OutlierRangeLowerValue
        {
            get { return outlierRangeLowerValue; }
        }

        /// <summary>
        /// Upper value of calculated range with outliers
        /// </summary>
        public float OutlierRangeUpperValue
        {
            get { return outlierRangeUpperValue; }
        }

        /// <summary>
        /// Create new, empty Statistics object
        /// </summary>
        public Statistics()
        {
            ResetStatistics();
            this.values = null;
            this.totalCount = 0;
            this.skippedValuesList = new List<float>();
            percentiles = new float[101];
        }

        /// <summary>
        /// Create new Statistics object with specified values
        /// </summary>
        public Statistics(float[] values)
        {
            ResetStatistics();
            this.values = new List<float>(values);
            this.totalCount = this.values.Count;
            this.skippedValuesList = new List<float>();
            percentiles = new float[101];
        }

        /// <summary>
        /// Create new Statistics object with specified values
        /// </summary>
        public Statistics(List<float> values)
        {
            ResetStatistics();
            this.values = values;
            this.totalCount = this.values.Count;
            this.skippedValuesList = new List<float>();
            percentiles = new float[101];
        }

        /// <summary>
        /// Reset statistic values to inital (zero) values, use a ComputeStatistics method to (re)calculate statistics
        /// </summary>
        public void ResetStatistics()
        {
            this.count = 0;
            this.countPercentage = 0;
            min = float.NaN;
            max = float.NaN;
            sum = float.NaN;
            mean = float.NaN;
            sd = float.NaN;
            median = float.NaN;
            q1 = float.NaN;
            q3 = float.NaN;
            iqr = float.NaN;
            percentiles = null;

            outlierRangeLowerValue = float.NaN;
            outlierRangeUpperValue = float.NaN;
        }

        /// <summary>
        /// Add a new value to the current list of values
        /// </summary>
        /// <param name="value"></param>
        public void AddValue(float value)
        {
            if (values == null)
            {
                values = new List<float>();
            }
            values.Add(value);
            totalCount++;
        }

        /// <summary>
        /// Remove skipped values and calculate basic statistics (which is relatively fast): all, except percentiles and outliers
        /// </summary>
        /// <param name="isMinMaxComputed">if true, min and max values are also computed</param>
        /// <param name="areValuesReleased">if true, memory for values is released after finishing calculation</param>
        /// <param name="isMemoryCollected">if true, garbage is collected after finishing calculation</param>
        public virtual void ComputeBasicStatistics(bool isMinMaxComputed = true, bool areValuesReleased = true, bool isMemoryCollected = false)
        {
            RemoveSkippedValues();
            if ((values != null) && (values.Count() > 0))
            {

                DoComputeBasicStatistics(isMinMaxComputed);
                if (areValuesReleased)
                {
                    ReleaseValuesMemory(isMemoryCollected);
                }
            }
        }

        /// <summary>
        /// Remove skipped values and calculate percentiles, including min, max and median and Q1, Q3, IQR and quartile skewness
        /// </summary>
        public void ComputePercentiles(bool areValuesReleased = true, bool isMemoryCollected = false)
        {
            RemoveSkippedValues();
            if ((values != null) && (values.Count() > 0))
            {
                List<float> sortedList;
                DoComputePercentileStatistics(out sortedList);
                if (areValuesReleased)
                {
                    ReleaseValuesMemory(isMemoryCollected);
                }
            }
        }

        /// <summary>
        /// Remove skipped values and calculate percentile and outlier statistics
        /// </summary>
        public virtual void ComputeOutlierStatistics(OutlierMethodEnum outlierMethod, OutlierBaseRangeEnum outlierBaseRange, float multiplier, bool areValuesReleased = true, bool isMemoryCollected = false)
        {
            RemoveSkippedValues();
            if ((values != null) && (values.Count() > 3))
            {
                DoComputeOutlierStatistics(outlierMethod, outlierBaseRange, multiplier);
                if (areValuesReleased)
                {
                    ReleaseValuesMemory(isMemoryCollected);
                }
            }
        }

        /// <summary>
        /// Remove skipped values and calculate all statistics
        /// </summary>
        public virtual void ComputeAllStatistics(OutlierMethodEnum outlierMethod, OutlierBaseRangeEnum outlierBaseRange, float multiplier, bool areValuesReleased = true, bool isMemoryCollected = false)
        {
            ResetStatistics();

            if ((values != null) && (values.Count() > 0))
            {
                RemoveSkippedValues();
                DoComputeBasicStatistics();
                DoComputeOutlierStatistics(outlierMethod, outlierBaseRange, multiplier);
                if (areValuesReleased)
                {
                    ReleaseValuesMemory(isMemoryCollected);
                }
            }
        }

        /// <summary>
        /// Calculate basic statistics (which is relatively fast): all, except percentiles and outliers
        /// </summary>
        /// <param name="isMinMaxComputed"></param>
        protected void DoComputeBasicStatistics(bool isMinMaxComputed = false)
        {
            this.count = values.Count();
            this.countPercentage = totalCount.Equals(0) ? 0f : ((float) count / (float)totalCount);
            this.sum = 0;
            foreach (float value in values)
            {
                this.sum += value;
            }
            this.mean = this.sum / count;

            float diffSum = 0;
            this.sd = float.NaN; //  dm.StandardDeviation;
            if (isMinMaxComputed)
            {
                min = float.MaxValue;
                max = float.MinValue;
                foreach (float value in values)
                {
                    if (value < min)
                    {
                        min = value;
                    }
                    if (value > max)
                    {
                        max = value;
                    }

                    diffSum += (mean - value) * (mean - value);
                }
            }
            else
            {
                foreach (float value in values)
                {
                    diffSum += (mean - value) * (mean - value);
                }
            }
            this.sd = (float)Math.Sqrt(diffSum / count);
            this.sampleSD = (float)Math.Sqrt(diffSum / (count - 1.5f));
        }

        /// <summary>
        /// Calculate percentiles, including min, max and median and Q1, Q3, IQR and quartile skewness
        /// </summary>
        protected void DoComputePercentileStatistics(out List<float> sortedValues)
        {
            this.count = values.Count;
            sortedValues = new List<float>(values);
            sortedValues.Sort();
            percentiles = new float[101];
            for (int p = 0; p <= 100; p++)
            {
                int index = (int)(((float)p / 100.0) * (sortedValues.Count - 1));
                percentiles[p] = sortedValues[index];
            }

            this.min = percentiles[0];
            this.max = percentiles[100];
            this.median = percentiles[50];

            this.q1 = percentiles[25];
            this.q3 = percentiles[75];
            this.iqr = Q3 - Q1;
            this.quartileSkew = ((Q3 - Median) - (Median - Q1)) / (Q3 - Q1);
        }

        /// <summary>
        /// Calculate percentiles and outlier statistics
        /// </summary>
        protected virtual void DoComputeOutlierStatistics(OutlierMethodEnum outlierMethod, OutlierBaseRangeEnum outlierBaseRange, float multiplier)
        {
            // Compute percentiles 
            List<float> sortedValues;
            DoComputePercentileStatistics(out sortedValues);

            // Compute outlier base range for histogram-gap method
            float baseRangeLowerPct = 0;
            float baseRangeLowerValue = percentiles[(int)baseRangeLowerPct];
            float baseRangeUpperPct = 100;
            float baseRangeUpperValue = percentiles[(int)baseRangeUpperPct];
            float baseRange = 0;
            switch (outlierBaseRange)
            {
                case OutlierBaseRangeEnum.Pct75_25:
                    baseRangeLowerPct = 25;
                    baseRangeUpperPct = 75;
                    break;
                case OutlierBaseRangeEnum.Pct90_10:
                    baseRangeLowerPct = 10;
                    baseRangeUpperPct = 90;
                    break;
                case OutlierBaseRangeEnum.Pct95_5:
                    baseRangeLowerPct = 5;
                    baseRangeUpperPct = 95;
                    break;
                default:
                    throw new Exception("Invalid outlier base range enum: " + outlierBaseRange);
            }
            baseRangeLowerValue = percentiles[(int)baseRangeLowerPct];
            baseRangeUpperValue = percentiles[(int)baseRangeUpperPct];
            baseRange = baseRangeUpperValue - baseRangeLowerValue;

            // Compute outlier range
            switch (outlierMethod)
            {
                case OutlierMethodEnum.IQR:
                    outlierRangeLowerValue = baseRangeLowerValue - multiplier * baseRange;
                    outlierRangeUpperValue = baseRangeUpperValue + multiplier * baseRange;
                    break;
                case OutlierMethodEnum.HistogramGap:
                    // Calculate binsize
                    float binSize = multiplier * baseRange * ((float)Math.Pow(values.Count(), -1.0 / 3.0));
                    float margin = (binSize / 100);

                    // Calculate lower- and upperrange values (initialize to extreme defaults)
                    outlierRangeLowerValue = sortedValues[0];
                    outlierRangeUpperValue = sortedValues[sortedValues.Count() - 1];
                    // Search lower outlier range value only in low value part ( < baseRangeLowerPct / 100)
                    for (int i = (int)(sortedValues.Count() * baseRangeLowerPct / 100); i >= 0; i--)
                    {
                        // Find values in sorted list that differ more than the calculated binsize
                        if ((sortedValues[i + 1] - sortedValues[i]) > binSize)
                        {
                            outlierRangeLowerValue = sortedValues[i + 1] - margin;
                            i = 0;
                        }
                    }
                    for (int i = (int)(sortedValues.Count() * baseRangeUpperPct / 100); i < sortedValues.Count(); i++)
                    {
                        if ((sortedValues[i] - sortedValues[i - 1]) > binSize)
                        {
                            outlierRangeUpperValue = sortedValues[i - 1] + margin;
                            i = sortedValues.Count();
                        }
                    }

                    sortedValues = null;
                    break;
                default:
                    throw new Exception("Invalid outlier method:" + outlierMethod.ToString());
            }
        }

        /// <summary>
        /// Adds a value to the list of skipped values
        /// </summary>
        /// <param name="skippedValue"></param>
        public void AddSkippedValue(float skippedValue)
        {
            skippedValuesList.Add(skippedValue);
        }

        /// <summary>
        /// Remove skipped values from current list of values
        /// </summary>
        public void RemoveSkippedValues()
        {
            if ((skippedValuesList != null) && (values != null) && (skippedValuesList.Count() > 0) && (values.Count() > 0))
            {
                List<float> newList = new List<float>(values.Count());
                foreach (float value in values)
                {
                    if (!skippedValuesList.Contains(value))
                    {
                        newList.Add(value);
                    }
                }
                this.values = newList;
            }
        }

        /// <summary>
        /// Release memory used for values
        /// </summary>
        /// <param name="isMemoryCollected"></param>
        public virtual void ReleaseValuesMemory(bool isMemoryCollected = true)
        {
            values = null;
            totalCount = 0;
            if (isMemoryCollected)
            {
                GC.Collect();
            }
        }
    }
}
