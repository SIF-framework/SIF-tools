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
using Sweco.SIF.Common;
using Sweco.SIF.GIS;

namespace Sweco.SIF.iMOD.IDF
{
    /// <summary>
    /// Class for iteration through cells of multiple IDF-files with different characteristics (i.e. cellsisze, extent). For this the smallest cellsize is used and iteration is from lower left to upper right.
    /// </summary>
    public class IDFCellIterator
    {
        /// <summary>
        /// Available methods for defintion of extent to iterate over
        /// </summary>
        public enum ExtentMethod
        {
            /// <summary>
            /// Use minimum extent of all IDF-files
            /// </summary>
            MinExtent,

            /// <summary>
            /// Use maximum extent of all IDF-files
            /// </summary>
            MaxExtent,

            /// <summary>
            /// Use extent of IDF-file that was first added
            /// </summary>
            FirstExtent
        }

        /// <summary>
        /// The extent method that is currently used for iteration
        /// </summary>
        private ExtentMethod extentMethod;

        /// <summary>
        /// The current iteration extent
        /// </summary>
        private Extent iterationExtent;

        /// <summary>
        /// The IDF-files to iterate over in the order that they were added to this IDFCellIterator
        /// </summary>
        private IDFFile[] idfFiles;

        /// <summary>
        /// The values of the added IDF-files for the current cell. The order of values corresponds with the idfFiles field
        /// </summary>
        private float[] idfValues;

        /// <summary>
        /// The intersection of all added extents
        /// </summary>
        public Extent MinExtent { get; set; }

        /// <summary>
        /// The union of all added extents
        /// </summary>
        public Extent MaxExtent { get; set; }

        /// <summary>
        /// Currently used stepsize in x-direction, defaults to minimum XCellsize of added IDF-files
        /// </summary>
        public float XStepsize { get; private set; }

        /// <summary>
        /// Currently used stepsize in y-direction, defaults to minimum YCellsize of added IDF-files
        /// </summary>
        public float YStepsize { get; private set; }

        /// <summary>
        /// X-coordinate of current cell(center)
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// Y-coordinate of current cell(center)
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// Optional extent used to clip the iteration extent
        /// </summary>
        public Extent ClipExtent { get; set; }

        /// <summary>
        /// Specify if float.NaN-value should be used for retrieved cellvalues and in this way get the same NoData-value for all added IDF-files
        /// </summary>
        public bool IsNaNUsedForNoData { get; set; }

        /// <summary>
        /// Creates an new CellIterator instance (by default iterating through the the minimum extent of all added IDF-files)
        /// </summary>
        /// <param name="clipExtent"></param>
        public IDFCellIterator(Extent clipExtent = null)
        {
            ClearFiles();
            this.ClipExtent = clipExtent;
            this.extentMethod = ExtentMethod.MinExtent;
            IsNaNUsedForNoData = false;
        }

        /// <summary>
        /// Creates an new CellIterator instance for specified IDF-files (by default iterating through the the minimum extent of all added IDF-files)
        /// </summary>
        /// <param name="idfFiles"></param>
        /// <param name="clipExtent"></param>
        public IDFCellIterator(List<IDFFile> idfFiles, Extent clipExtent = null)
        {
            this.ClipExtent = clipExtent;
            ClearFiles();
            extentMethod = ExtentMethod.MinExtent;
            IsNaNUsedForNoData = false;
            foreach (IDFFile idfFile in idfFiles)
            {
                AddIDFFile(idfFile);
            }
        }

        /// <summary>
        /// Redefine the method that is used for determining the iteration extent. MinExtent and MaxExtent values are recalculated.
        /// </summary>
        /// <param name="extentMethod"></param>
        public void RedefineExtentType(ExtentMethod extentMethod)
        {
            if (this.extentMethod != extentMethod)
            {
                this.extentMethod = extentMethod;
                if (extentMethod == ExtentMethod.MinExtent)
                {
                    iterationExtent = MinExtent;
                }
                else if (extentMethod == ExtentMethod.MaxExtent)
                {
                    iterationExtent = MaxExtent;
                }
                else if (extentMethod == ExtentMethod.FirstExtent)
                {
                    if ((idfFiles != null) && (idfFiles.Length > 0))
                    {
                        iterationExtent = idfFiles[0].Extent; ;
                    }
                    else
                    {
                        iterationExtent = null;
                    }
                }
                else
                {
                    throw new Exception("Unknown ExtentMethod: " + extentMethod);
                }

                CalculateMinExtent();
                CalculateMaxExtent();
            }
        }

        /// <summary>
        /// Sets currently used x/y-stepsize for iterating through the current IDF-files. 
        /// Note: The stepsize is recalculated as the minimum stepsize after adding a new IDF-file
        /// </summary>
        /// <param name="stepsize"></param>
        public void SetStepsize(float stepsize)
        {
            XStepsize = stepsize;
            YStepsize = stepsize;
        }

        /// <summary>
        /// Sets currently used x- and y-stepsize for iterating through the current IDF-files. 
        /// Note: The stepsize is recalculated as the minimum stepsize after adding a new IDF-file
        /// </summary>
        /// <param name="xStepsize"></param>
        /// <param name="yStepsize"></param>
        public void SetStepsize(float xStepsize, float yStepsize)
        {
            this.XStepsize = xStepsize;
            this.YStepsize = yStepsize;
        }

        /// <summary>
        /// Remove all added IDF-files and reset stepsize and extent of this IDFCellIterator
        /// </summary>
        public void ClearFiles()
        {
            idfFiles = null;
            idfValues = null;
            XStepsize = float.MaxValue;
            YStepsize = float.MaxValue;
            MinExtent = null;
            MaxExtent = null;
            iterationExtent = null;
        }

        /// <summary>
        /// Adds an IDF-file to be iterated through. The stepsize and Min/MaxExtent are recalculated. Note: only add IDF-file with resolution that is multiple of minimum resolution of all added IDF-files.
        /// </summary>
        /// <param name="idffile"></param>
        public void AddIDFFile(IDFFile idffile)
        {
            if (idffile == null)
            {
                // ignore null-values
                return;
            }

            int fileCount = 0;
            if (idfFiles != null)
            {
                fileCount = idfFiles.Length;
            }
            IDFFile[] newIDFFiles = new IDFFile[fileCount + 1];
            float[] newCellValues = new float[fileCount + 1];

            for (int i = 0; i < fileCount; i++)
            {
                newIDFFiles[i] = idfFiles[i];
                newCellValues[i] = idfValues[i];
            }
            newIDFFiles[fileCount] = idffile;
            newCellValues[fileCount] = float.NaN;
            idfFiles = newIDFFiles;
            idfValues = newCellValues;

            CalculateMinStepsize();
            CalculateMinExtent();
            CalculateMaxExtent();

            if (extentMethod == ExtentMethod.MinExtent)
            {
                iterationExtent = MinExtent;
            }
            else if (extentMethod == ExtentMethod.MaxExtent)
            {
                iterationExtent = MaxExtent;
            }
            else if (extentMethod == ExtentMethod.FirstExtent)
            {
                iterationExtent = idfFiles[0].Extent;
            }
            else
            {
                throw new Exception("Unknown UsedExtentType: " + extentMethod);
            }

            if (ClipExtent != null)
            {
                iterationExtent = iterationExtent.Clip(ClipExtent);
            }
        }

        /// <summary>
        /// Resets the current cell to the cell in the lower left corner of the iteration extent
        /// </summary>
        public void Reset()
        {
            X = iterationExtent.llx + XStepsize / 2.0f;
            Y = iterationExtent.lly + YStepsize / 2.0f;

            if (idfFiles != null)
            {
                RetrieveCellValues();
            }
            else
            {
                throw new Exception("First add IDF-files before calling IDFCellIterator.ResetCurrentCell()");
            }
        }

        /// <summary>
        /// Sets the current cell to the given X- and Y-coordinates
        /// </summary>
        public void SetCurrentCell(float x, float y)
        {
            if (iterationExtent.Contains(x, y))
            {
                this.X = x;
                this.Y = y;

                if (idfFiles != null)
                {
                    RetrieveCellValues();
                }
                else
                {
                    throw new Exception("First add IDF-files before calling IDFCellIterator.SetCurrentCell()");
                }
            }
        }

        /// <summary>
        /// Advance to next cell within current iteration extent. Iterates from lower left to upper right, with stepsize equal to smallest cellsize
        /// </summary>
        /// <returns>true if the next cell is still within the iteration extent</returns>
        public bool MoveNext()
        {
            X += XStepsize;
            if (X >= iterationExtent.urx)
            {
                X = iterationExtent.llx + XStepsize / 2.0f;
                Y += YStepsize;
            }

            if (IsNaNUsedForNoData)
            {
                RetrieveNaNBasedCellValues();
            }
            else
            {
                RetrieveCellValues();
            }

            return ((X >= iterationExtent.llx) && (X < iterationExtent.urx) && (Y >= iterationExtent.lly) && (Y < iterationExtent.ury));
        }

        /// <summary>
        /// Checks if the current cell is still inside the iteration extent
        /// </summary>
        /// <returns></returns>
        public bool IsInsideExtent()
        {
            return ((X >= iterationExtent.llx) && (X < iterationExtent.urx) && (Y >= iterationExtent.lly) && (Y < iterationExtent.ury));
        }

        /// <summary>
        /// Get current cellvalue for the given idfFile, or float.NaN if file is missing
        /// </summary>
        /// <param name="idfFile">one of the IDF-files that is added to the IDFCellIterator</param>
        /// <returns>the cellvalue or float.NaN if outside extent or if null idf</returns>
        public float GetCellValue(IDFFile idfFile)
        {
            if (idfFile == null)
            {
                return float.NaN;
            }

            if (idfFiles != null)
            {
                for (int i = 0; i < idfFiles.Length; i++)
                {
                    if (idfFiles[i] != null)
                    {
                        if (idfFiles[i].Filename.Equals(idfFile.Filename))
                        {
                            return idfValues[i];
                        }
                    }
                }
            }
            return float.NaN;
        }

        /// <summary>
        /// Get array matrix with cellvalues around current cell for the given idfFile, or float.NaN if file is missing
        /// </summary>
        /// <param name="idfFile">one of the IDF-files that is added to the IDFCellIterator</param>
        /// <param name="cellDistance">max distance from given cell (i.e. use 1 for 3x3-grid)</param>
        /// <param name="precision"></param>
        /// <returns>the cellvalues or float.NaN if outside extent or if IDF-file is null</returns>
        public float[][] GetCellValues(IDFFile idfFile, int cellDistance, int precision = -1)
        {
            float[][] cellValues = null;

            if (idfFile != null)
            {
                cellValues = idfFile.GetCellValues(X, Y, cellDistance, precision);
            }

            return cellValues;
        }

        /// <summary>
        /// Checks if all added IDF-files have an equal extent
        /// </summary>
        /// <returns></returns>
        public bool IsEqualExtent()
        {
            return MinExtent.Equals(MaxExtent);
        }

        /// <summary>
        /// Checks if the intersection of the extents of all added IDF-files gives an empty extent
        /// </summary>
        /// <returns></returns>
        public bool IsEmptyExtent()
        {
            return (MinExtent.llx >= MinExtent.urx) || (MinExtent.lly >= MinExtent.ury);
        }

        /// <summary>
        /// Returns and/or logs a standard message about the equality of all added extents
        /// </summary>
        /// <param name="log"></param>
        /// <param name="indentLevel">indent level for message</param>
        /// <param name="addedMessage"></param>
        /// <returns></returns>
        public string CheckExtent(Log log = null, int indentLevel = 0, string addedMessage = null)
        {
            string message = null;

            if (IsEmptyExtent())
            {
                message = "IDFFiles don't have overlapping extent";
                if (addedMessage != null)
                {
                    message += addedMessage;
                }
                if (log != null)
                {
                    log.AddWarning(message, indentLevel);
                    for (int i = 0; i < this.idfFiles.Length; i++)
                    {
                        log.AddWarning("- " + this.idfFiles[i].Filename, indentLevel);
                    }
                }
            }
            else
            {
                if (IsEqualExtent())
                {
                    message = "IDFFiles have equal extent: " + MinExtent.ToString();
                    if (addedMessage != null)
                    {
                        message += addedMessage;
                    }
                    if (log != null)
                    {
                        log.AddInfo(message, indentLevel);
                    }
                }
                else
                {
                    message = "IDFFiles have different extents."
                        + " Min extent: " + MinExtent.ToString()
                        + " Max exent: " + MaxExtent.ToString();
                    if (addedMessage != null)
                    {
                        message += addedMessage;
                    }
                    if (log != null)
                    {
                        log.AddWarning(message, indentLevel);
                    }
                }
            }

            return message;
        }

        /// <summary>
        /// Retrieve cell values for current cell from all added IDF-files
        /// </summary>
        private void RetrieveCellValues()
        {
            for (int i = 0; i < idfFiles.Length; i++)
            {
                if (idfFiles[i] != null)
                {
                    idfValues[i] = idfFiles[i].GetValue(X, Y);
                }
                else
                {
                    idfValues[i] = float.NaN;
                }
            }
        }

        /// <summary>
        /// Retrieve cell values for current cell from all added IDF-files. For both NoData-values and out-of-bound cells float.NaN is retrieved.
        /// </summary>
        private void RetrieveNaNBasedCellValues()
        {
            for (int i = 0; i < idfFiles.Length; i++)
            {
                if (idfFiles[i] != null)
                {
                    idfValues[i] = idfFiles[i].GetNaNBasedValue(X, Y);
                }
                else
                {
                    idfValues[i] = float.NaN;
                }
            }
        }

        /// <summary>
        /// Calculate minimum stepsize (based on cellsize) over all added IDF-files
        /// </summary>
        private void CalculateMinStepsize()
        {
            XStepsize = float.MaxValue;
            YStepsize = float.MaxValue;
            if (idfFiles != null)
            {
                for (int i = 0; i < idfFiles.Length; i++)
                {
                    if (idfFiles[i] != null)
                    {
                        if (idfFiles[i].XCellsize < XStepsize)
                        {
                            XStepsize = idfFiles[i].XCellsize;
                        }
                        if (idfFiles[i].YCellsize < YStepsize)
                        {
                            YStepsize = idfFiles[i].YCellsize;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Calculate the intersection of extents of all added IDF-files
        /// </summary>
        private void CalculateMinExtent()
        {
            MinExtent = new Extent();
            MinExtent.llx = float.MinValue;
            MinExtent.lly = float.MinValue;
            MinExtent.urx = float.MaxValue;
            MinExtent.ury = float.MaxValue;
            if (idfFiles != null)
            {
                for (int i = 0; i < idfFiles.Length; i++)
                {
                    if (idfFiles[i] != null)
                    {
                        if (idfFiles[i].Extent != null)
                        {
                            Extent idfFileExtent = idfFiles[i].Extent;
                            if (idfFileExtent.llx > MinExtent.llx)
                            {
                                MinExtent.llx = idfFileExtent.llx;
                            }
                            if (idfFileExtent.lly > MinExtent.lly)
                            {
                                MinExtent.lly = idfFileExtent.lly;
                            }
                            if (idfFileExtent.urx < MinExtent.urx)
                            {
                                MinExtent.urx = idfFileExtent.urx;
                            }
                            if (idfFileExtent.ury < MinExtent.ury)
                            {
                                MinExtent.ury = idfFileExtent.ury;
                            }
                        }
                    }
                }
            }

            if (ClipExtent != null)
            {
                MinExtent = MinExtent.Clip(ClipExtent);
            }
        }

        /// <summary>
        /// Calculate the union of extents of all added IDF-files
        /// </summary>
        private void CalculateMaxExtent()
        {
            MaxExtent = new Extent();
            MaxExtent.llx = float.MaxValue;
            MaxExtent.lly = float.MaxValue;
            MaxExtent.urx = float.MinValue;
            MaxExtent.ury = float.MinValue;
            if (idfFiles != null)
            {
                for (int i = 0; i < idfFiles.Length; i++)
                {
                    if ((idfFiles[i] != null) && !(idfFiles[i] is ConstantIDFFile))
                    {
                        if (idfFiles[i].Extent != null)
                        {
                            if (idfFiles[i].Extent.llx < MaxExtent.llx)
                            {
                                MaxExtent.llx = idfFiles[i].Extent.llx;
                            }
                            if (idfFiles[i].Extent.lly < MaxExtent.lly)
                            {
                                MaxExtent.lly = idfFiles[i].Extent.lly;
                            }
                            if (idfFiles[i].Extent.urx > MaxExtent.urx)
                            {
                                MaxExtent.urx = idfFiles[i].Extent.urx;
                            }
                            if (idfFiles[i].Extent.ury > MaxExtent.ury)
                            {
                                MaxExtent.ury = idfFiles[i].Extent.ury;
                            }
                        }
                    }
                }
            }

            if (ClipExtent != null)
            {
                MaxExtent = MaxExtent.Clip(ClipExtent);
            }
        }

        /// <summary>
        /// Get all added IDF-files
        /// </summary>
        /// <returns></returns>
        public List<IDFFile> GetIDFFiles()
        {
            return new List<IDFFile>(idfFiles);
        }

        /// <summary>
        /// Get all added IDF-files without constant values
        /// </summary>
        /// <returns></returns>
        public List<IDFFile> GetNonConstantIDFFiles()
        {
            List<IDFFile> nonConstantIDFFiles = new List<IDFFile>();
            for (int i = 0; i < idfFiles.Length; i++)
            {
                if (!(idfFiles[i] is ConstantIDFFile))
                {
                    nonConstantIDFFiles.Add(idfFiles[i]);
                }
            }
            return nonConstantIDFFiles;
        }

        /// <summary>
        /// Get all added IDF-files with constant values
        /// </summary>
        /// <returns></returns>
        public List<IDFFile> GetConstantIDFFiles()
        {
            List<IDFFile> constantIDFFiles = new List<IDFFile>();
            for (int i = 0; i < idfFiles.Length; i++)
            {
                if (idfFiles[i] is ConstantIDFFile)
                {
                    constantIDFFiles.Add(idfFiles[i]);
                }
            }
            return constantIDFFiles;
        }
    }
}
