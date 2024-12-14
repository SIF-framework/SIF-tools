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
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.GIS;

namespace Sweco.SIF.iMOD.IDF
{
    /// <summary>
    /// Class for handling a constant value as an IDF-file
    /// </summary>
    public class ConstantIDFFile : IDFFile
    {
        /// <summary>
        /// The constant value that this object represents
        /// </summary>
        public float ConstantValue { get; }

        // The class is actually implemented with one extremely large cell, with an extent large enough to cover the whole of the Netherlands
        private const float MaxUrX = 1000000;
        private const float MaxUrY = 1000000;

        /// <summary>
        /// Create new ConstantIDFFile for specified value
        /// </summary>
        /// <param name="constantValue"></param>
        public ConstantIDFFile(float constantValue)
            : base()
        {
            // Define 1 cell with the constant value
            Initialize(constantValue.ToString(), new Extent(0, 0, MaxUrX, MaxUrY), MaxUrX, MaxUrY, NoDataValue, true);  // always use lazy loading for constant value IDF-file
            this.ConstantValue = constantValue;
            this.MinValue = constantValue;
            this.MaxValue = constantValue;
        }

        /// <summary>
        /// Retrieve gridvalue at given coordinate (x,y), or nodata when outside bounds
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public override float GetValue(float x, float y)
        {
            return ConstantValue;
        }

        /// <summary>
        /// SetValue is not defined for ConstantIDFFile and should not be called
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        public override void SetValue(float x, float y, float value)
        {
            throw new Exception("SetValue should not be called for ConstantIDFFile");
        }

        /// <summary>
        /// SetValues is not defined for ConstantIDFFile and should not be called
        /// </summary>
        /// <param name="value"></param>
        public override void SetValues(float value)
        {
            throw new Exception("SetValues should not be called for ConstantIDFFile");
        }

        /// <summary>
        /// AddValue is not defined for ConstantIDFFile and should not be called
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="value"></param>
        public override void AddValue(float x, float y, float value)
        {
            throw new Exception("AddValue should not be called for ConstantIDFFile");
        }

        /// <summary>
        /// ResetValues is not defined for ConstantIDFFile and should not be called
        /// </summary>
        public override void ResetValues()
        {
            throw new Exception("ResetValues should not be called for ConstantIDFFile");
        }

        /// <summary>
        /// Updates min- and maxvalues, which are constant for a ConsantIDFFile
        /// </summary>
        public override void UpdateMinMaxValue()
        {
            // ignore, constant value and min/max should not have changed
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void LoadValues()
        {
            // When loadvalues is called, actual underlying values are references by the Values property: declare memory for 1 cel and set value equal to constant value
            DeclareValuesMemory();
            base.SetValues(ConstantValue);
        }

        /// <summary>
        /// Copies this ConstantIDFFile object
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="isValueCopy"></param>
        /// <returns></returns>
        public override IDFFile CopyIDF(string filename, bool isValueCopy = true)
        {
            return new ConstantIDFFile(this.ConstantValue);
        }

        /// <summary>
        /// Clips default extent of this ConstantIDFFile object to specified extent. For a constant IDF-file only the extent is changed to reflect the clipped status
        /// </summary>
        /// <param name="clipExtent"></param>
        /// <param name="isInvertedClip"></param>
        /// <returns></returns>
        public override IDFFile ClipIDF(Extent clipExtent, bool isInvertedClip = false)
        {
            ConstantIDFFile clippedFile = new ConstantIDFFile(this.ConstantValue);

            if (isInvertedClip)
            {
                // Keep original extent
                clipExtent = this.extent;
            }

            // Actually clip extent (which is never null)
            clippedFile.modifiedExtent = extent.Clip(clipExtent);
            clippedFile.extent = clipExtent.Copy();
            // leave file extent null, as it is not defined for a constant IDF-file

            clippedFile.XCellsize = clippedFile.modifiedExtent.urx - clippedFile.modifiedExtent.llx;
            clippedFile.YCellsize = clippedFile.modifiedExtent.ury - clippedFile.modifiedExtent.lly;

            return clippedFile;
        }

        /// <summary>
        /// Checks (for non-NoData-value) in this constant IDF-file if it is greater than or equal to the given value
        /// </summary>
        /// <param name="testValue"></param>
        /// <returns>1 if true, 0 if false</returns>
        public override IDFFile IsGreaterEqual(float testValue)
        {
            float value = this.ConstantValue;
            if (value.Equals(this.NoDataValue))
            {
                value = this.NoDataCalculationValue;
            }

            if (value >= testValue)
            {
                return new ConstantIDFFile(1);
            }
            else
            {
                return new ConstantIDFFile(0);
            }
        }

        /// <summary>
        /// Checks for all non-NoData-values in this IDF if they are greater than the given value
        /// </summary>
        /// <param name="testValue"></param>
        /// <returns>1 if true, 0 if false</returns>
        public override IDFFile IsGreater(float testValue)
        {
            float value = this.ConstantValue;
            if (value.Equals(this.NoDataValue))
            {
                value = this.NoDataCalculationValue;
            }

            if (value > testValue)
            {
                return new ConstantIDFFile(1);
            }
            else
            {
                return new ConstantIDFFile(0);
            }
        }

        /// <summary>
        /// Checks for all non-NoData-values in this IDF if they are lesser than the given value
        /// </summary>
        /// <param name="testValue"></param>
        /// <returns>1 if true, 0 if false</returns>
        public override IDFFile IsLesser(float testValue)
        {
            float value = this.ConstantValue;
            if (value.Equals(this.NoDataValue))
            {
                value = this.NoDataCalculationValue;
            }

            if (value < testValue)
            {
                return new ConstantIDFFile(1);
            }
            else
            {
                return new ConstantIDFFile(0);
            }
        }

        /// <summary>
        /// Checks for all non-NoData-values in this IDF if they are lesser than or equal to the given value
        /// </summary>
        /// <param name="testValue"></param>
        /// <returns>1 if true, 0 if false</returns>
        public override IDFFile IsLesserEqual(float testValue)
        {
            float value = this.ConstantValue;
            if (value.Equals(this.NoDataValue))
            {
                value = this.NoDataCalculationValue;
            }

            if (value <= testValue)
            {
                return new ConstantIDFFile(1);
            }
            else
            {
                return new ConstantIDFFile(0);
            }
        }

        /// <summary>
        /// Checks for all values in this IDF if they are not equal to the given value
        /// </summary>
        /// <param name="testValue"></param>
        /// <returns>1 if true, 0 if false</returns>
        public override IDFFile IsNotEqual(float testValue)
        {
            float value = this.ConstantValue;
            if (value.Equals(this.NoDataValue))
            {
                value = this.NoDataCalculationValue;
            }

            if (value != testValue)
            {
                return new ConstantIDFFile(1);
            }
            else
            {
                return new ConstantIDFFile(0);
            }
        }

        /// <summary>
        /// Allocate the constantvalue of this object to a new IDF-file with the same characteristics as the specified IDF-file. 
        /// When the constantvalue is NoData, is set to the NoDataCalculationValue of this ConstantIDFFile if it was defined, or otherwise to the NoData-value of the specified IDF-file.
        /// </summary>
        /// <param name="someIDFFile"></param>
        /// <returns></returns>
        public IDFFile Allocate(IDFFile someIDFFile)
        {
            IDFFile newIDFFile = null;
            float value = ConstantValue;
            if (value.Equals(NoDataValue) || value.Equals(float.NaN))
            {
                // Set new value to NoDataCalculationValue if it is defined, otherwise use NoData-value of specified other IDF-file
                value = (NoDataCalculationValue.Equals(float.NaN) ? someIDFFile.NoDataValue : NoDataCalculationValue);
            }

            if (someIDFFile is ConstantIDFFile)
            {
                newIDFFile = new ConstantIDFFile(value);
            }
            else
            {
                newIDFFile = someIDFFile.CopyIDF(Path.Combine(Path.GetDirectoryName(someIDFFile.Filename), Path.GetFileNameWithoutExtension(someIDFFile.Filename) + "_allocated.idf"));
                newIDFFile.SetValues(value);
            }

            return newIDFFile;
        }

        /// <summary>
        /// Implementation of + operator for ConstantIDFFile and ConstantIDFFile which gives a new ConstantIDFFile object
        /// </summary>
        /// <param name="idfFile1">IDFFile</param>
        /// <param name="idfFile2">ConstantIDFFile</param>
        /// <returns></returns>
        public static IDFFile operator +(ConstantIDFFile idfFile1, ConstantIDFFile idfFile2)
        {
            return new ConstantIDFFile(((ConstantIDFFile)idfFile1).ConstantValue + ((ConstantIDFFile)idfFile2).ConstantValue);
        }
    }
}
