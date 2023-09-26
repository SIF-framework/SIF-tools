// IDFselect is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFselect.
// 
// IDFselect is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFselect is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFselect. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.GIS.Utilities;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.Values;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IDFselect
{
    public class SIFTool : SIFToolBase
    {
        #region Constructor

        /// <summary>
        /// Creates a SIFTool instance and initializes tool name and version and a Log object with the console as a default listener
        /// </summary>
        public SIFTool(SIFToolSettingsBase settings) : base(settings)
        {
            SetLicense(new SIFGPLLicense(this));
            settings.RegisterSIFTool(this);
        }

        #endregion

        /// <summary>
        /// Entry point of tool
        /// </summary>
        /// <param name="args">command-line arguments</param>
        static void Main(string[] args)
        {
            int exitcode = -1;
            SIFTool tool = null;
            try
            {
                // Use SwecoTool Framework to handle license check, write of toolname and version, parsing arguments, writing of logfile and if specified so handling exeptions
                SIFToolSettings settings = new SIFToolSettings(args);
                tool = new SIFTool(settings);

                exitcode = tool.Run();
            }
            catch (ToolException ex)
            {
                ExceptionHandler.HandleToolException(ex, tool?.Log);
                exitcode = 1;
            }
            catch (Exception ex)
            {
                ExceptionHandler.HandleException(ex, tool?.Log);
                exitcode = 1;
            }

            Environment.Exit(exitcode);
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected override void DefineToolProperties()
        {
            AddAuthor("Koen van der Hauw");
            ToolPurpose = "SIF-tool for selection of cells in IDF-files";
        }

        /// <summary>
        /// Starts actual tool process after reading and checking settings
        /// </summary>
        /// <returns>resultcode: 0 for success, 1 for errors</returns>
        protected override int StartProcess()
        {
            int exitcode = 0;

            // Retrieve tool settings that have been parsed from the command-line arguments 
            SIFToolSettings settings = (SIFToolSettings)Settings;

            string outputPath = settings.OutputPath;

            // Create output path if not yet existing
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // An example for reading files from a path and creating a new file...
            string[] inputIDFFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter, settings.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            if (inputIDFFilenames.Length == 0)
            {
                throw new ToolException("No files found with filter '" + settings.InputFilter + "' in path: " + settings.InputPath);
            }

            IDFFile zoneIDFFile = null;
            if (settings.SelectionString != null)
            {
                Log.AddInfo("Reading selection zone ...");
                zoneIDFFile = ParseIDFFileString(settings.SelectionString, "Please specify an existing IDF-file or floating point value as zone file/value: " + settings.SelectionString);
            }

            IDFFile conditionalIDFFile = null;
            if (settings.ConditionalIDFString != null)
            {
                Log.AddInfo("Reading conditional IDF-file/value ...");
                conditionalIDFFile = ParseIDFFileString(settings.ConditionalIDFString, "Please specify an existing IDF-file or floating point value as conditional operand: " + settings.ConditionalIDFString);
            }

            Log.AddInfo("Processing input files ...");
            int fileIdx = 0;
            while (fileIdx < inputIDFFilenames.Length)
            {
                string inputIDFFilename = inputIDFFilenames[fileIdx++];

                Log.AddInfo("Processing IDF-file " + Path.GetFileName(inputIDFFilename));
                IDFFile inputIDFFile = IDFFile.ReadFile(inputIDFFilename, true);

                if (settings.ExcludedValues != null)
                {
                    // Remove excluded values
                    foreach (float excludedValue in settings.ExcludedValues)
                    {
                        inputIDFFile.ReplaceValues(excludedValue, inputIDFFile.NoDataValue);
                    }
                }

                // Perform selection depending on specified options, start with a copy of input file
                IDFFile resultIDFFile = null;
                if (settings.ConnectionFlags != SIFToolSettings.CONNECTIONOPTION_NONE)
                {
                    Log.AddInfo("Selecting connected cells ... ", 1, false);
                    long sourceCellCount = inputIDFFile.RetrieveElementCount();
                    if (zoneIDFFile != null)
                    {
                        // When defined, use specified zone file
                        resultIDFFile = SelectConnectedCells(inputIDFFile, zoneIDFFile, settings, Log);
                    }
                    else
                    {
                        // Use input file as zone file for connected cells, but process as a single zone with value 1
                        zoneIDFFile = inputIDFFile.CopyIDF(null, true);
                        zoneIDFFile.ReplaceValues(zoneIDFFile, 1);

                        zoneIDFFile.WriteFile(Path.Combine(Path.Combine(settings.OutputPath, "debug"), "value_zone.IDF"));

                        resultIDFFile = SelectConnectedCells(inputIDFFile, zoneIDFFile, settings, Log);
                    }

                    long resultCellCount = resultIDFFile.RetrieveElementCount();
                    Log.AddInfo("selected " + resultCellCount + " / " + sourceCellCount + " cells");
                }
                else if (zoneIDFFile != null)
                {
                    Log.AddInfo("Selecting cells in specified zone ... ", 1, false);
                    long sourceCellCount = inputIDFFile.RetrieveElementCount();

                    if (zoneIDFFile.Extent.Intersects(inputIDFFile.Extent))
                    {
                        if (!zoneIDFFile.Extent.IsAligned(inputIDFFile.Extent, inputIDFFile.XCellsize, inputIDFFile.YCellsize))
                        {
                            throw new ToolException("Input IDF-file extent (" + inputIDFFile.Extent.ToString() + ") is not aligned with zone IDF-file extent (" + zoneIDFFile.Extent.ToString() + ") and cannot be analyzed. Ensure matching cellsizes and extents!");
                        }

                        // Do normal selection of cells: remove all cells that have a NoData-value in selection IDF-file from input/result IDF-file
                        resultIDFFile = inputIDFFile.CopyIDF(null, true);
                        IDFFile zoneNoDataIDFFile = zoneIDFFile.IsEqual(zoneIDFFile.NoDataValue);
                        zoneNoDataIDFFile = zoneNoDataIDFFile.ClipIDF(resultIDFFile.Extent);
                        resultIDFFile = (zoneNoDataIDFFile != null) ? resultIDFFile.ClipIDF(zoneNoDataIDFFile.Extent) : null;
                        if ((zoneNoDataIDFFile != null) && (resultIDFFile != null))
                        {
                            if (zoneNoDataIDFFile.XCellsize > resultIDFFile.XCellsize)
                            {
                                zoneNoDataIDFFile = zoneNoDataIDFFile.ScaleDown(resultIDFFile.XCellsize, DownscaleMethodEnum.Block);
                            }
                            else if (zoneNoDataIDFFile.XCellsize < resultIDFFile.XCellsize)
                            {
                                zoneNoDataIDFFile = zoneNoDataIDFFile.ScaleUp(resultIDFFile.XCellsize, UpscaleMethodEnum.Boundary);
                            }
                            resultIDFFile.ReplaceValues(zoneNoDataIDFFile, 1, resultIDFFile.NoDataValue);
                        }
                    }
                    else
                    {
                        resultIDFFile = inputIDFFile.CopyIDF(null);
                        resultIDFFile.SetValues(resultIDFFile.NoDataValue);
                    }

                    long resultCellCount = 0;
                    if (resultIDFFile != null)
                    {
                        resultCellCount = resultIDFFile.RetrieveElementCount();
                    }
                    Log.AddInfo("selected " + resultCellCount + " / " + sourceCellCount + " cells");
                }
                else
                {
                    resultIDFFile = inputIDFFile.CopyIDF(null);
                }

                if (settings.ConditionalOperator != ValueOperator.Undefined)
                {
                    Log.AddInfo("Selecting cells with conditional expression ... ", 1, false);
                    long sourceCellCount = resultIDFFile.RetrieveElementCount();

                    resultIDFFile = SelectCells(resultIDFFile, conditionalIDFFile, settings, Log);

                    long resultCellCount = resultIDFFile.RetrieveElementCount();
                    Log.AddInfo("selected " + resultCellCount + " / " + sourceCellCount + " cells");
                }

                // Write resultfile
                WriteResultFile(inputIDFFilename, resultIDFFile, settings, inputIDFFilenames.Length == 1);
            }

            ToolSuccessMessage = "Finished processing " + fileIdx + " file(s)";

            return exitcode;
        }

        protected IDFFile ParseIDFFileString(string idfFileString, string exceptionMessage = null)
        {
            IDFFile idfFile = null;
            if (File.Exists(idfFileString))
            {
                idfFile = IDFFile.ReadFile(idfFileString, true);
            }
            else
            {
                float constantValue;
                if (!float.TryParse(idfFileString, NumberStyles.Float, EnglishCultureInfo, out constantValue))
                {
                    if (exceptionMessage == null)
                    {
                        exceptionMessage = "Please specify an existing IDF-file or floating point value: " + idfFileString;
                    }
                    throw new ToolException(exceptionMessage);
                }
                idfFile = new ConstantIDFFile(constantValue);
            }

            return idfFile;
        }

        protected IDFFile SelectCells(IDFFile inputIDFFile, IDFFile selIDFFile, SIFToolSettings settings, Log log)
        {
            IDFFile resultIDF = inputIDFFile.CopyIDF("sel.IDF");
            if (!(selIDFFile is ConstantIDFFile))
            {
                if (!selIDFFile.Extent.Equals(inputIDFFile.Extent))
                {
                    if (!inputIDFFile.Extent.Contains(selIDFFile.Extent))
                    {
                        selIDFFile = selIDFFile.ClipIDF(inputIDFFile.Extent);
                    }
                    if (inputIDFFile.Extent.Equals(selIDFFile.Extent))
                    {
                        selIDFFile = selIDFFile.EnlargeIDF(inputIDFFile.Extent);
                    }
                }
            }

            IDFFile condIDF = null;
            switch (settings.ConditionalOperator)
            {
                case ValueOperator.Equal:
                    condIDF = inputIDFFile.IsEqual(selIDFFile);
                    break;
                case ValueOperator.Unequal:
                    condIDF = inputIDFFile.IsNotEqual(selIDFFile);
                    break;
                case ValueOperator.LessThan:
                    condIDF = inputIDFFile.IsLesser(selIDFFile);
                    break;
                case ValueOperator.LessThanOrEqual:
                    condIDF = inputIDFFile.IsLesserEqual(selIDFFile);
                    break;
                case ValueOperator.GreaterThan:
                    condIDF = inputIDFFile.IsGreater(selIDFFile);
                    break;
                case ValueOperator.GreaterThanOrEqual:
                    condIDF = inputIDFFile.IsGreaterEqual(selIDFFile);
                    break;
                default:
                    throw new Exception("Unexpected conditional operator: " + settings.ConditionalOperator);
            }

            resultIDF.ResetValues();
            resultIDF.ReplaceValues(condIDF, 1, inputIDFFile);

            return resultIDF;
        }

        /// <summary>
        /// Select all (non-NoData) cells in input IDF-file that are connected, have enough overlap and are large enough (as defined by conditions in settings)
        /// </summary>
        /// <param name="inputIDFFile">IDF-file from which cells should be selected</param>
        /// <param name="selIDFFile">selection IDF-file</param>
        /// <param name="settings"></param>
        /// <param name="log"></param>
        /// <returns>IDF-file with selected cells</returns>
        protected IDFFile SelectConnectedCells(IDFFile inputIDFFile, IDFFile selIDFFile, SIFToolSettings settings, Log log)
        {
            IDFFile resultIDFFile = null;
            Queue<IDFCell> cellQueue = null;
            IDFFile visitedIDFFile = inputIDFFile.CopyIDF("IsVisited.IDF");
            visitedIDFFile.SetValues(0f);

            float inputNoDataValue = inputIDFFile.NoDataValue;
            float selNoDataValue = selIDFFile.NoDataValue;
            bool isConnectedByValue = (settings.ConnectionFlags & SIFToolSettings.CONNECTIONOPTION_BYVALUE) > 0;
            bool isConnectedDiagonally = (settings.ConnectionFlags & SIFToolSettings.CONNECTIONOPTION_DIAGONAL) > 0;
            bool useSelValue = (settings.ConnectionFlags & SIFToolSettings.CONNECTIONOPTION_SELVALUE) > 0;

            resultIDFFile = new IDFFile("ConnectedCells.IDF", inputIDFFile.Extent, inputIDFFile.XCellsize, inputIDFFile.YCellsize, inputIDFFile.NoDataValue);
            resultIDFFile.NoDataCalculationValue = inputIDFFile.NoDataCalculationValue;
            resultIDFFile.ResetValues();
            float resultNoDataValue = resultIDFFile.NoDataValue;

            // If extent and cellsize are equal, use a fast iteration loop
            if (inputIDFFile.Extent.Equals(selIDFFile.Extent) && inputIDFFile.XCellsize.Equals(selIDFFile.XCellsize) && inputIDFFile.YCellsize.Equals(selIDFFile.YCellsize))
            {
                float[][] inputValues = inputIDFFile.Values;
                float[][] selvalues = selIDFFile.Values;
                float[][] resultValues = resultIDFFile.values;
                float inputValue = float.NaN;
                float selValue = float.NaN;

                // Loop through all cells and for each Non-NoData cell in the selection grid, select all connected cells from the inputgrid
                for (int rowIdx = 0; rowIdx < inputIDFFile.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < inputIDFFile.NCols; colIdx++)
                    {
                        inputValue = inputValues[rowIdx][colIdx];

                        selValue = selvalues[rowIdx][colIdx];
                        if ((!selValue.Equals(selNoDataValue)) && (!inputValue.Equals(inputNoDataValue)))
                        {
                            // cell is selected, check if not visited before
                            if (visitedIDFFile.values[rowIdx][colIdx].Equals(0))
                            {
                                List<IDFCell> connectedCells = new List<IDFCell>();
                                connectedCells.Add(new IDFCell(rowIdx, colIdx));
                                visitedIDFFile.values[rowIdx][colIdx] = 1;

                                // Start visiting neighbours, connect selected neighbors and visit their neighbors as well
                                cellQueue = new Queue<IDFCell>();
                                cellQueue.Enqueue(new IDFCell(rowIdx, colIdx));
                                while (cellQueue.Count > 0)
                                {
                                    IDFCell inputCell = cellQueue.Dequeue();
                                    ConnectNeighbours(connectedCells, inputIDFFile, inputCell, inputValue, visitedIDFFile, cellQueue, isConnectedByValue, isConnectedDiagonally);
                                }

                                ProcessConnectedCells(connectedCells, selIDFFile, resultIDFFile, inputIDFFile, useSelValue, selValue, settings);
                            }
                        }
                    }
                }
            }
            else
            {
                // extent and/or cellsize is not equal, use a robust iterator

                IDFCellIterator cellIterator = new IDFCellIterator();
                cellIterator.RedefineExtentMethod(IDFCellIterator.ExtentMethod.MaxExtent);
                cellIterator.AddIDFFile(inputIDFFile);
                cellIterator.AddIDFFile(selIDFFile);

                float inputValue = float.NaN;
                float selValue = float.NaN;

                cellIterator.Reset();
                while (cellIterator.IsInsideExtent())
                {
                    int rowIdx = inputIDFFile.GetRowIdx(cellIterator.Y);
                    int colIdx = inputIDFFile.GetColIdx(cellIterator.X);

                    inputValue = cellIterator.GetCellValue(inputIDFFile);
                    selValue = cellIterator.GetCellValue(selIDFFile);
                    if (!selValue.Equals(float.NaN) && !selValue.Equals(selNoDataValue) && !inputValue.Equals(float.NaN) && !inputValue.Equals(inputNoDataValue))
                    {
                        // cell is selected, check if not visited before
                        if (visitedIDFFile.values[rowIdx][colIdx].Equals(0))
                        {
                            List<IDFCell> connectedCells = new List<IDFCell>();
                            connectedCells.Add(new IDFCell(rowIdx, colIdx));
                            visitedIDFFile.values[rowIdx][colIdx] = 1;

                            // Start visiting neighbours
                            cellQueue = new Queue<IDFCell>();
                            cellQueue.Enqueue(new IDFCell(rowIdx, colIdx));
                            while (cellQueue.Count > 0)
                            {
                                IDFCell inputCell = cellQueue.Dequeue();
                                ConnectNeighbours(connectedCells, inputIDFFile, inputCell, inputValue, visitedIDFFile, cellQueue, isConnectedByValue, isConnectedDiagonally);
                            }

                            ProcessConnectedCells(connectedCells, selIDFFile, resultIDFFile, inputIDFFile, useSelValue, selValue, settings);
                        }
                    }

                    cellIterator.MoveNext();
                }
            }

            return resultIDFFile;
        }

        /// <summary>
        /// Add <paramref name="connectedCells"/> from <paramref name="selIDFFile"/> to <paramref name="resultIDFFile"/> when specified conditions apply 
        /// (i.e. number/percentage of cells that overlap with <paramref name="selIDFFile"/> and size of largest rectangle inside connected cells)
        /// </summary>
        /// <param name="connectedCells">List of cells in <paramref name="selIDFFile"/></param>
        /// <param name="selIDFFile">IDF-file with selected cells and values</param>
        /// <param name="resultIDFFile">IDF-file to add selected cells to when specified conditions apply</param>
        /// <param name="inputIDFFile">used for result value if <paramref name="useConstValue"/> is false</param>
        /// <param name="useConstValue">if true, use <paramref name="constValue"/> intead of cellvalues from <paramref name="inputIDFFile"/></param>
        /// <param name="constValue">constant value to use for all result cells if <paramref name="useConstValue"/> is true</param>
        /// <param name="settings">used settings: MinConnectedWidth, MinConnectedHeight, MinConnectedOverlapCount, MinConnectedOverlapFraction</param>
        private void ProcessConnectedCells(List<IDFCell> connectedCells, IDFFile selIDFFile, IDFFile resultIDFFile, IDFFile inputIDFFile, bool useConstValue, float constValue, SIFToolSettings settings)
        {
            if (connectedCells.Count > 0)
            {
                float minConnectedWidth = settings.MinConnectedWidth;
                float minConnectedHeight = settings.MinConnectedHeight;
                if (settings.MinConnectedUnit == Unit.Meters)
                {
                    // Note: minConnectedWidth and Height should be defined as number of cells
                    minConnectedWidth /= selIDFFile.XCellsize;
                    minConnectedHeight /= selIDFFile.YCellsize;
                }

                bool isValidConnection = true;
                if ((settings.MinConnectedOverlapCount >= 0) || (settings.MinConnectedOverlapFraction >= 0f))
                {
                    // Count number of actually overlapping cells
                    int overlappingCellCount = 0;
                    foreach (IDFCell idfCell in connectedCells)
                    {
                        float x = resultIDFFile.GetX(idfCell.ColIdx);
                        float y = resultIDFFile.GetY(idfCell.RowIdx);

                        float selValue = selIDFFile.GetValue(x, y);
                        if (!selValue.Equals(float.NaN) && !(selValue.Equals(resultIDFFile.NoDataValue)))
                        {
                            overlappingCellCount++;
                        }
                    }

                    // Check that the number/fraction of connected cells is large enough: either overlap count or fraction should be large enough
                    if (settings.MinConnectedOverlapOperator == LogicalOperator.None)
                    {
                        if (settings.MinConnectedOverlapCount >= 0)
                        {
                            isValidConnection = (overlappingCellCount >= settings.MinConnectedOverlapCount);
                        }
                        else
                        {
                            isValidConnection = ((float)overlappingCellCount / (float)connectedCells.Count) > settings.MinConnectedOverlapFraction;
                        }
                    }
                    else
                    {
                        if (settings.MinConnectedOverlapOperator == LogicalOperator.AND)
                        {
                            isValidConnection = (overlappingCellCount >= settings.MinConnectedOverlapCount) && (((float)overlappingCellCount / (float)connectedCells.Count) > settings.MinConnectedOverlapFraction);
                        }
                        else
                        {
                            isValidConnection = (overlappingCellCount >= settings.MinConnectedOverlapCount) || (((float)overlappingCellCount / (float)connectedCells.Count) > settings.MinConnectedOverlapFraction);
                        }
                    }

                    if ((isValidConnection) && ((settings.MinConnectedWidth > 0) || (settings.MinConnectedHeight > 0)))
                    {
                        // Check that inner dimension of connected cells is large enough

                        // Convert connected cells grid to matrix
                        int minRowIdx = int.MaxValue;
                        int minColIdx = int.MaxValue;
                        int maxRowIdx = 0;
                        int maxColIdx = 0;
                        foreach (IDFCell idfCell in connectedCells)
                        {
                            if (idfCell.RowIdx < minRowIdx)
                            {
                                minRowIdx = idfCell.RowIdx;
                            }
                            if (idfCell.RowIdx > maxRowIdx)
                            {
                                maxRowIdx = idfCell.RowIdx;
                            }
                            if (idfCell.ColIdx < minColIdx)
                            {
                                minColIdx = idfCell.ColIdx;
                            }
                            if (idfCell.ColIdx > maxColIdx)
                            {
                                maxColIdx = idfCell.ColIdx;
                            }
                        }

                        // Retrieve total heigt / width of local matrix
                        int matrixheight = maxRowIdx - minRowIdx + 1;
                        int matrixWidth = maxColIdx - minColIdx + 1;

                        // Initialize matrix with zeroes
                        int[][] localMatrix = new int[matrixheight][];
                        for (int rowIdx = 0; rowIdx < matrixheight; rowIdx++)
                        {
                            localMatrix[rowIdx] = new int[matrixWidth];
                            for (int colIdx = 0; colIdx < matrixWidth; colIdx++)
                            {
                                localMatrix[rowIdx][colIdx] = 0;
                            }
                        }

                        // Add value 1 for connected cells
                        foreach (IDFCell idfCell in connectedCells)
                        {
                            localMatrix[idfCell.RowIdx - minRowIdx][idfCell.ColIdx - minColIdx] = 1;
                        }
                        
                        MatrixUtils.FindMaxRectangle(localMatrix, out int llRow, out int llCol, out int maxWidth, out int maxHeight);
                        if (maxWidth < settings.MinConnectedWidth)
                        {
                            isValidConnection = false;
                        }
                        else if (maxHeight < settings.MinConnectedHeight)
                        {
                            isValidConnection = false;
                        }
                    }
                }

                if (isValidConnection)
                {
                    foreach (IDFCell idfCell in connectedCells)
                    {
                        resultIDFFile.values[idfCell.RowIdx][idfCell.ColIdx] = useConstValue ? constValue : inputIDFFile.values[idfCell.RowIdx][idfCell.ColIdx];
                    }
                }
            }
        }

        /// <summary>
        /// Retrieve neighhbor cells of <paramref name="inputCell"/> that are connected (as defined by settings) and add to <paramref name="connectedCells"/> list and to <paramref name="cellQueue"/> for further visiting
        /// </summary>
        /// <param name="connectedCells"></param>
        /// <param name="inputIDFFile"></param>
        /// <param name="inputCell"></param>
        /// <param name="inputValue"></param>
        /// <param name="visitedIDFFile"></param>
        /// <param name="cellQueue"></param>
        /// <param name="isConnectedByValue"></param>
        /// <param name="isConnectedDiagonally"></param>
        protected void ConnectNeighbours(List<IDFCell> connectedCells, IDFFile inputIDFFile, IDFCell inputCell, float inputValue, IDFFile visitedIDFFile, Queue<IDFCell> cellQueue, bool isConnectedByValue, bool isConnectedDiagonally)
        {
            float[][] cellValues = inputIDFFile.GetCellValues(inputCell.RowIdx, inputCell.ColIdx, 1);

            // Check all neighbours
            for (int rowSubIdx = -1; rowSubIdx <= 1; rowSubIdx++)
            {
                for (int colSubIdx = -1; colSubIdx <= 1; colSubIdx++)
                {
                    if (isConnectedDiagonally || (rowSubIdx * colSubIdx == 0))
                    {
                        int neighbourRowIdx = inputCell.RowIdx + rowSubIdx;
                        int neighbourColIdx = inputCell.ColIdx + colSubIdx;
                        // Check that neighbour is inside input grid
                        if ((neighbourRowIdx >= 0) && (neighbourRowIdx < inputIDFFile.NRows) && (neighbourColIdx >= 0) && (neighbourColIdx < inputIDFFile.NCols))
                        {
                            // If neighbour has not yet been visited (and should be visited (with value 0)), add it to the queue to visit
                            if (visitedIDFFile.values[neighbourRowIdx][neighbourColIdx].Equals(0))
                            {
                                if ((isConnectedByValue && inputIDFFile.values[neighbourRowIdx][neighbourColIdx].Equals(inputValue))
                                    || (!isConnectedByValue && !inputIDFFile.values[neighbourRowIdx][neighbourColIdx].Equals(inputIDFFile.NoDataValue)))
                                {
                                    connectedCells.Add(new IDFCell(neighbourRowIdx, neighbourColIdx));
                                    visitedIDFFile.values[neighbourRowIdx][neighbourColIdx] = 1;
                                    cellQueue.Enqueue(new IDFCell(neighbourRowIdx,neighbourColIdx));
                                }
                            }
                        }
                    }
                }
            }
        }

        protected virtual void WriteResultFile(string inputIDFFilename, IDFFile resultIDFFile, SIFToolSettings settings, bool isSingleResultFile)
        {
            if ((resultIDFFile != null) && (!settings.SkipEmptyResult || (resultIDFFile.RetrieveValueCount() > 0)))
            {
                string resultIDFFilename = settings.OutputFilename;
                if (isSingleResultFile && (settings.OutputFilename != null))
                {
                    resultIDFFilename = settings.OutputFilename;
                }
                else
                {
                    resultIDFFilename = Path.GetFileName(inputIDFFilename);
                }
                Log.AddInfo("writing IDF-file " + resultIDFFilename + " ...", 1);
                resultIDFFile.WriteFile(Path.Combine(settings.OutputPath, resultIDFFilename));
            }
            else
            {
                Log.AddInfo("Empty result IDF-file is skipped: " + Path.GetFileName(inputIDFFilename));
            }
        }
        }
}
