// IDFbnd is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFbnd.
// 
// IDFbnd is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFbnd is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFbnd. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.IDF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IDFbnd
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

        protected string[] BNDFiles { get; set; }

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

            System.Environment.Exit(exitcode);
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected override void DefineToolProperties()
        {
            AddAuthor("Koen van der Hauw");
            ToolPurpose = "SIF-tool for correcting boundary around active IDF-cells";
        }

        /// <summary>
        /// Starts actual tool process after reading and checking settings
        /// </summary>
        /// <returns>resultcode: 0 for success, 1 for errors</returns>
        protected override int StartProcess()
        {
            int exitcode = 0;

            // Retrieve tool settings that have been parsed from the command-line arguments 
            SIFToolSettings settings = (SIFToolSettings) Settings;

            BNDFiles = Directory.GetFiles(settings.InputPath, settings.BndFilterString);
            if (BNDFiles.Length == 0)
            {
                throw new ToolException("No IDF-files found for filter '" + settings.BndFilterString + "' in: " + Path.GetFullPath(settings.InputPath));
            }

            for (int fileIdx = 0; fileIdx < BNDFiles.Length; fileIdx++)
            {
                string currentFilePath = BNDFiles[fileIdx];
                if (!Path.GetExtension(currentFilePath).ToLower().Equals(".idf"))
                {
                    continue;
                }

                string outputFilePath = Path.Combine(settings.OutputPath, Path.GetFileName(currentFilePath));
                Log.AddInfo("Processing file " + Path.GetFileName(currentFilePath) + " ...");
                if (!(File.Exists(outputFilePath)) || settings.IsOverwrite)
                {
                    BNDLayer bndLayer = GetBoundaryLayer(currentFilePath, fileIdx, settings);
                    ProcessInputFile(bndLayer, settings, Log);
                }
                else
                {
                    Log.AddWarning("Skipped existing outputfile " + Path.GetFileName(currentFilePath), 1);
                }
            }

            ToolSuccessMessage = "Finished processing";

            return exitcode;
        }

        protected virtual void ProcessInputFile(BNDLayer bndLayer, SIFToolSettings settings, Log log)
        {
            float boundaryValue = settings.BoundaryValue;
            float activeValue = settings.ActiveValue;
            float inactiveValue = settings.InactiveValue;

            IDFFile resultBNDIDFFile = bndLayer.BNDIDFFile.CorrectBoundary(activeValue, boundaryValue, inactiveValue, settings.Extent, false, settings.KeepInactiveCells, settings.IsDiagonallyChecked);
            resultBNDIDFFile.Filename = Path.Combine(settings.OutputPath, Path.GetFileName(bndLayer.BNDFilename));

            // Now remove cells outside boundary
            if (settings.IsOuterCorrection)
            {
                // Remove active cells outside boundary or extent
                CorrectOuterCells(resultBNDIDFFile, activeValue, boundaryValue, inactiveValue, settings.Extent, log);

                // Correct for unnecessary diagonal cells when not diagonally checked
                if (!settings.IsDiagonallyChecked)
                {
                    // Remove diagonally redundant boundary cells when not diagonally checked
                    CorrectOuterBoundary(resultBNDIDFFile, activeValue, boundaryValue, inactiveValue, log);
                }
            }

            log.AddInfo("Writing result file " + Path.GetFileName(resultBNDIDFFile.Filename), 1);
            Metadata metadata = CreateMetadata(bndLayer, settings);
            resultBNDIDFFile.WriteFile(metadata);
        }
            
        /// <summary>
        /// Inactivate cells outside boundary cells inside specified bndIDFFile: 
        /// - if an extent is specified, all cells outside this extent are inactivated
        /// - if no extent is specified, all active cells outside boundary cells are inactivated
        /// </summary>
        /// <param name="bndIDFFile"></param>
        /// <param name="activeValue"></param>
        /// <param name="bndValue"></param>
        /// <param name="inactiveValue"></param>
        /// <param name="extent"></param>
        /// <param name="log"></param>
        protected void CorrectOuterCells(IDFFile bndIDFFile, float activeValue, float bndValue, float inactiveValue, Extent extent, Log log)
        {
            if (extent != null)
            {
                // Retrieve row/column indices. GetRowIdx takes row below upper/lower edge and right of left/right edge of extent, so correct bottom row and right column with cellsize
                int extentTopRow = bndIDFFile.GetRowIdx(extent.ury);
                int extentBotRow = bndIDFFile.GetRowIdx(extent.lly + bndIDFFile.YCellsize);
                int extentLeftCol = bndIDFFile.GetColIdx(extent.llx);
                int extentRightCol = bndIDFFile.GetColIdx(extent.urx - bndIDFFile.XCellsize);

                // Start from cells at edge of IDF grid, add these to a queue of cells to visit
                Queue<IDFCell> cellQueue = new Queue<IDFCell>();
                // Remove part above extent
                for (int rowIdx = 0; rowIdx < extentTopRow; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < bndIDFFile.NCols; colIdx++)
                    {
                        bndIDFFile.values[rowIdx][colIdx] = inactiveValue;
                    }
                }
                // Remove part below extent
                for (int rowIdx = extentBotRow + 1; rowIdx < bndIDFFile.NRows; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < bndIDFFile.NCols; colIdx++)
                    {
                        bndIDFFile.values[rowIdx][colIdx] = inactiveValue;
                    }
                }
                // Remove part left from extent
                for (int rowIdx = extentTopRow; rowIdx <= extentBotRow; rowIdx++)
                {
                    for (int colIdx = 0; colIdx < extentLeftCol; colIdx++)
                    {
                        bndIDFFile.values[rowIdx][colIdx] = inactiveValue;
                    }
                }
                // Remove part right from extent
                for (int rowIdx = extentTopRow; rowIdx <= extentBotRow; rowIdx++)
                {
                    for (int colIdx = extentRightCol + 1; colIdx < bndIDFFile.NCols; colIdx++)
                    {
                        bndIDFFile.values[rowIdx][colIdx] = inactiveValue;
                    }
                }
            }
            else
            {
                // Only correct active cells that can be reached via active cells outside boundary, don't pass through other cells
                IDFFile srcBNDIDFFile = bndIDFFile.CopyIDF(string.Empty);
                IDFFile visitedCellsIDFFile = bndIDFFile.CopyIDF(string.Empty);
                visitedCellsIDFFile.SetValues(0);
                visitedCellsIDFFile.ReplaceValues(bndIDFFile, activeValue, 0);
                visitedCellsIDFFile.ReplaceValues(bndIDFFile, bndIDFFile.NoDataValue, visitedCellsIDFFile.NoDataValue);

                int bndTopRow = 0;
                int bndBotRow = bndIDFFile.NRows - 1;
                int bndLeftCol = 0;
                int bndRightCol = bndIDFFile.NCols - 1;

                // Start from cells at edge of IDF grid, add these to a queue of cells to visit
                Queue<IDFCell> cellQueue = new Queue<IDFCell>();
                for (int rowIdx = bndTopRow; rowIdx <= bndBotRow; rowIdx++)
                {
                    // Don't change or visit boundary cells
                    float cellValue = bndIDFFile.values[rowIdx][bndLeftCol];
                    if (!cellValue.Equals(bndValue))
                    {
                        cellQueue.Enqueue(new IDFCell(rowIdx, bndLeftCol));
                        visitedCellsIDFFile.values[rowIdx][bndLeftCol] = 1;
                    }
                    cellValue = bndIDFFile.values[rowIdx][bndRightCol];
                    if (!cellValue.Equals(bndValue))
                    {
                        cellQueue.Enqueue(new IDFCell(rowIdx, bndRightCol));
                        visitedCellsIDFFile.values[rowIdx][bndRightCol] = 1;
                    }
                }
                for (int colIdx = bndLeftCol + 1; colIdx < bndRightCol; colIdx++)
                {
                    // Don't change or visit boundary cells
                    float cellValue = bndIDFFile.values[bndTopRow][colIdx];
                    if (!cellValue.Equals(bndValue))
                    {
                        cellQueue.Enqueue(new IDFCell(bndTopRow, colIdx));
                        visitedCellsIDFFile.values[bndTopRow][colIdx] = 1;
                    }
                    cellValue = bndIDFFile.values[bndBotRow][colIdx];
                    if (!cellValue.Equals(bndValue))
                    {
                        cellQueue.Enqueue(new IDFCell(bndBotRow, colIdx));
                        visitedCellsIDFFile.values[bndBotRow][colIdx] = 1;
                    }
                }

                //IDFFile baseIDFFile = visitedCellsIDFFile.CopyIDF(string.Empty);
                //baseIDFFile.ResetValues();

                // Now start visiting cells to find outer cells
                while (cellQueue.Count > 0)
                {
                    //visitedCellsIDFFile.WriteFile(Path.Combine(((SIFToolSettings)this.Settings).OutputPath, "VisitedCells.IDF"));
                    //IDFFile queuedIDFFile = IDFUtils.ConvertToIDF(cellQueue, baseIDFFile, true, float.NaN, 1f);
                    //queuedIDFFile.WriteFile(Path.Combine(((SIFToolSettings)this.Settings).OutputPath, "QueuedCells.IDF"));

                    // Take next cell to be visited
                    IDFCell currentCell = cellQueue.Dequeue();
                    int currentRowIdx = currentCell.RowIdx;
                    int currentColIdx = currentCell.ColIdx;

                    //IDFFile currentCellIDFFile = IDFUtils.ConvertToIDF(new List<IDFCell>() { currentCell}, baseIDFFile, true, float.NaN, 1f);
                    //currentCellIDFFile.WriteFile(Path.Combine(((SIFToolSettings)this.Settings).OutputPath, "CurrentCell.IDF"));

                    float cellValue = bndIDFFile.values[currentRowIdx][currentColIdx];
                    // Replace all cellvalues that have an active value with an inactive value. Other values are left as they are
                    // Values other than active, inactive or boundary can be replaced before with option r
                    if (cellValue.Equals(activeValue))
                    {
                        bndIDFFile.values[currentRowIdx][currentColIdx] = inactiveValue;
                    }

                    // Check all neighbours (always check diagonally for outer cell correction)
                    for (int rowSubIdx = -1; rowSubIdx <= 1; rowSubIdx++)
                    {
                        for (int colSubIdx = -1; colSubIdx <= 1; colSubIdx++)
                        {
                            if ((rowSubIdx == 0) || (colSubIdx == 0))
                            {
                                int neighbourRowIdx = currentRowIdx + rowSubIdx;
                                int neighbourColIdx = currentColIdx + colSubIdx;

                                // Check that neighbour is inside input grid
                                if ((neighbourRowIdx >= 0) && (neighbourRowIdx < bndIDFFile.NRows) && (neighbourColIdx >= 0) && (neighbourColIdx < bndIDFFile.NCols))
                                {
                                    float neighbourCellValue = bndIDFFile.values[neighbourRowIdx][neighbourColIdx];
                                    // Only check cells via active neighbours, don't go through inactive or boundary barriers
                                    if (neighbourCellValue.Equals(activeValue))
                                    {
                                        // If neighbour has not yet been visited, add it to the queue to visit
                                        if (visitedCellsIDFFile.values[neighbourRowIdx][neighbourColIdx].Equals(0))
                                        {
                                            cellQueue.Enqueue(new IDFCell(neighbourRowIdx, neighbourColIdx));
                                            visitedCellsIDFFile.values[neighbourRowIdx][neighbourColIdx] = 1;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                long activeCellCount = bndIDFFile.RetrieveValueCount(activeValue);
                if (activeCellCount == 0)
                {
                    bndIDFFile.values = srcBNDIDFFile.values;
                }
            }
        }

        /// <summary>
        /// Replace boundary values by inactive value if neighbouring cells already form a boundary between active and inactive cells.
        /// This effectively removes diagonally redundant boundary cells. 
        /// </summary>
        /// <param name="idfFile"></param>
        /// <param name="activeValue"></param>
        /// <param name="bndValue"></param>
        /// <param name="inactiveValue"></param>
        /// <param name="log"></param>
        protected void CorrectOuterBoundary(IDFFile idfFile, float activeValue, float bndValue, float inactiveValue, Log log)
        {
            float[][] idfValues = idfFile.values;
            float idfNoDataValue = idfFile.NoDataValue;

            int bndBotRow = idfFile.NRows - 1;
            int bndRightCol = idfFile.NCols - 1;
            for (int rowIdx = 0; rowIdx < idfFile.NRows; rowIdx++)
            {
                for (int colIdx = 0; colIdx < idfFile.NCols; colIdx++)
                {
                    if (idfValues[rowIdx][colIdx].Equals(bndValue))
                    {
                        bool hasTopBndCell = (rowIdx > 0) && idfValues[rowIdx - 1][colIdx].Equals(bndValue);
                        bool hasLeftBndCell = (colIdx > 0) && idfValues[rowIdx][colIdx - 1].Equals(bndValue);
                        bool hasBotBndCell = (rowIdx < bndBotRow) && idfValues[rowIdx + 1][colIdx].Equals(bndValue);
                        bool hasRightBndCell = (colIdx < bndRightCol) && idfValues[rowIdx][colIdx + 1].Equals(bndValue);
                        if (hasTopBndCell && hasLeftBndCell)
                        {
                            // ?B?
                            // BB?
                            // ???

                            // Check topleft cell for active value
                            if ((rowIdx > 0) && (colIdx > 0) && idfValues[rowIdx - 1][colIdx - 1].Equals(activeValue))
                            {
                                // AB?
                                // BB?
                                // ???

                                // Check bottom or right cell for inactive or NoData-value
                                if (((rowIdx < bndBotRow) && idfValues[rowIdx + 1][colIdx].Equals(inactiveValue)) || ((colIdx < bndRightCol) && idfValues[rowIdx][colIdx + 1].Equals(inactiveValue)))
                                {
                                    idfValues[rowIdx][colIdx] = inactiveValue;
                                }
                                else if (((rowIdx < bndBotRow) && idfValues[rowIdx + 1][colIdx].Equals(inactiveValue)) || ((colIdx < bndRightCol) && idfValues[rowIdx][colIdx + 1].Equals(idfNoDataValue)))
                                {
                                    idfValues[rowIdx][colIdx] = inactiveValue;
                                }
                            }
                        }
                        if (hasTopBndCell && hasRightBndCell)
                        {
                            // Check topright cell for active value
                            if ((rowIdx > 0) && (colIdx < bndRightCol) && idfValues[rowIdx - 1][colIdx + 1].Equals(activeValue))
                            {
                                // ?BA
                                // ?BB
                                // ???
                                if (((colIdx > 0) && idfValues[rowIdx][colIdx - 1].Equals(inactiveValue)) || ((rowIdx < bndBotRow) && idfValues[rowIdx + 1][colIdx].Equals(inactiveValue)))
                                {
                                    idfValues[rowIdx][colIdx] = inactiveValue;
                                }
                                else if (((rowIdx > 0) && idfValues[rowIdx - 1][colIdx].Equals(inactiveValue)) || ((rowIdx < bndBotRow) && idfValues[rowIdx + 1][colIdx].Equals(idfNoDataValue)))
                                {
                                    idfValues[rowIdx][colIdx] = inactiveValue;
                                }
                            }
                        }
                        if (hasBotBndCell && hasRightBndCell)
                        {
                            // Check botright cell for active value
                            if ((rowIdx < bndBotRow) && (colIdx < bndRightCol) && idfValues[rowIdx + 1][colIdx + 1].Equals(activeValue))
                            {
                                // ???
                                // ?BB
                                // ?BA
                                if (((rowIdx > 0) && idfValues[rowIdx - 1][colIdx].Equals(inactiveValue)) || ((colIdx > 0) && idfValues[rowIdx][colIdx - 1].Equals(inactiveValue)))
                                {
                                    idfValues[rowIdx][colIdx] = inactiveValue;
                                }
                                else if (((rowIdx > 0) && idfValues[rowIdx - 1][colIdx].Equals(inactiveValue)) || ((colIdx < bndRightCol) && idfValues[rowIdx][colIdx + 1].Equals(idfNoDataValue)))
                                {
                                    idfValues[rowIdx][colIdx] = inactiveValue;
                                }
                            }
                        }
                        if (hasBotBndCell && hasLeftBndCell)
                        {
                            // Check botleft cell for active value
                            if ((rowIdx < bndBotRow) && (colIdx > 0) && idfValues[rowIdx + 1][colIdx - 1].Equals(activeValue))
                            {
                                // ???
                                // BB?
                                // AB?
                                if (((rowIdx < bndBotRow) && idfValues[rowIdx + 1][colIdx].Equals(inactiveValue)) || ((colIdx < bndRightCol) && idfValues[rowIdx][colIdx + 1].Equals(inactiveValue)))
                                {
                                    idfValues[rowIdx][colIdx] = inactiveValue;
                                }
                            }
                            else if (((rowIdx < bndBotRow) && idfValues[rowIdx + 1][colIdx].Equals(inactiveValue)) || ((colIdx < bndRightCol) && idfValues[rowIdx][colIdx + 1].Equals(idfNoDataValue)))
                            {
                                idfValues[rowIdx][colIdx] = inactiveValue;
                            }
                        }
                        if ((rowIdx > 0) && (colIdx > 0) && (rowIdx < bndBotRow) && (colIdx < bndRightCol) &&
                            !idfValues[rowIdx + 1][colIdx].Equals(activeValue) && !idfValues[rowIdx][colIdx + 1].Equals(activeValue)
                            && !idfValues[rowIdx - 1][colIdx].Equals(activeValue) && !idfValues[rowIdx][colIdx - 1].Equals(activeValue))
                        {
                            idfValues[rowIdx][colIdx] = inactiveValue;
                        }
                    }
                }
            }
        }

        protected virtual Metadata CreateMetadata(BNDLayer bndLayer, SIFToolSettings settings)
        {
            // Create metadata for outputfile
            Metadata metadata = bndLayer.BNDIDFFile.CreateMetadata("Boundary (value " + settings.BoundaryValue + ") corrected around cells with (inner)value " + settings.ActiveValue);
            metadata.ProcessDescription = "Boundary corrected with IDFbnd.exe around source cells with (inner)value " + settings.ActiveValue;
            if (settings.Extent != null)
            {
                metadata.Description += ", starting at extent " + settings.Extent.ToString();
                metadata.ProcessDescription += ", starting at specified extent";
            }
            if (settings.IsDiagonallyChecked)
            {
                metadata.ProcessDescription += "; neighbour cells are diagonally checked to find boundary";
            }
            metadata.Unit = "-";
            metadata.Resolution = "-";
            metadata.Scale = "-";

            return metadata;
        }

        protected virtual BNDLayer GetBoundaryLayer(string bndFilename, int fileIdx, SIFToolSettings settings)
        {
            BNDLayer bndLayer = new BNDLayer(bndFilename);
            if (settings.Extent != null)
            {
                bndLayer.Enlarge(settings.Extent);
            }
            return bndLayer;
        }

    }
}
