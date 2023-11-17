// IPFplot is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IPFplot.
// 
// IPFplot is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IPFplot is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IPFplot. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.IPF;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZedGraph;

namespace Sweco.SIF.IPFplot
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

        protected List<Color> colors = null;
        protected List<DashStyle?> lineTypes = null;
        protected List<SymbolType> markerTypes = null;
        protected List<float> lineSizes = null;
        protected Panel plotPanel;
        protected ZedGraphControl zedGraphControl = null;

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
            AddAuthor("Koen Jansen");
            ToolPurpose = "Tool for plotting IPF-files";
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
            string outputPath = settings.OutputPath;

            // Create output path if not yet existing
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            string[] inputFilenames = settings.Plotrefs.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            Log.AddInfo("Processing input files ...");

            // Read inputfiles
            Log.AddInfo("Reading timeseries parameters ...");
            if ((inputFilenames == null) || (inputFilenames.Length == 0) || !File.Exists(inputFilenames[0]) || !Path.GetExtension(inputFilenames[0]).ToLower().Equals(".ipf"))
            {
                throw new ToolException("First IPF-entry should be an existing inputFilename: " + (((inputFilenames != null) && (inputFilenames.Length > 0)) ? inputFilenames[0] : "<missing IPF-filename>"));
            }

            int logIndentLevel = 0;
            List<IPFFile> ipfFiles = new List<IPFFile>();
            List<Dictionary<string, IPFPoint>> ipfFileDictionaries = new List<Dictionary<string, IPFPoint>>();
            ReadInputIPFFiles(inputFilenames, out ipfFiles, out ipfFileDictionaries, settings, Log, logIndentLevel);

            // Setting graphical base setup
            InitializePlotSettings(inputFilenames, settings);

            // Define base/first IPF-file
            IPFFile baseIPFFile = ipfFiles[0];
            int basePointCount = baseIPFFile.PointCount;
            string seriesLabel = GetSeriesLabel(0, settings);

            // Create graph per point of first IPF-file
            int plotCount = 0;
            Log.AddInfo("Processing " + baseIPFFile.PointCount + " points from base IPF-file ...");
            for (int pointIdx = 0; pointIdx < baseIPFFile.PointCount; pointIdx++)
            {
                bool isDataMissing = false;
             
                // Retrieve point and optional id
                IPFPoint ipfPoint = baseIPFFile.Points[pointIdx];
                string id = RetrieveIdString(ipfPoint, settings, 0);

                zedGraphControl.GraphPane.Title.Text = (id != null) ? id : ipfPoint.ToString(0);

                // Process base/first timeseries
                IPFTimeseries ipfTimeseries = GetIPFTimeSeries(ipfPoint, pointIdx, id, inputFilenames, settings);
                if (ipfTimeseries != null)
                {
                    int valueListIdx = GetValueListIndex(ipfTimeseries, settings, 0);
                    CheckColumnIndex(ipfTimeseries, settings, valueListIdx);

                    DateTime minDate = DateTime.MaxValue;
                    DateTime maxDate = DateTime.MinValue;
                    SetMinMaxDate(ipfTimeseries, ref minDate, ref maxDate);

                    zedGraphControl.GraphPane.CurveList.Clear();
                    AddSeries(ref isDataMissing, zedGraphControl, ipfTimeseries, valueListIdx, seriesLabel, colors, markerTypes[0], lineTypes[0], lineSizes[0], 0, inputFilenames.Count(), settings); 

                    // Process other timeseries
                    if (inputFilenames.Length > 1)
                    {
                        for (int fileIdx = 1; fileIdx < inputFilenames.Length; fileIdx++)
                        {
                            string inputFilename2 = ExpandFilenameFilter(inputFilenames[fileIdx]);
                            if (inputFilename2 == null)
                            {
                                // Timeseries object is not an existing file, check for column index or column name in first IPF-file
                                string columnString = inputFilenames[fileIdx];
                                int colIdx = -1;
                                float constantValue = float.NaN;
                                ParsePlotRef2(columnString, baseIPFFile, ipfPoint, fileIdx, ref colIdx, ref constantValue);

                                // Add constant line to graph
                                string constantSeriesLabel = GetConstantLabel(baseIPFFile, fileIdx, colIdx, settings);
                                AddConstantTimeseries(zedGraphControl, constantValue, minDate, maxDate, constantSeriesLabel, colors[fileIdx], fileIdx, DashStyle.Dash, 2.0F);
                            }
                            else
                            {
                                IPFFile ipfFile2 = ipfFiles[fileIdx];
                                string seriesLabel2 = GetSeriesLabel(fileIdx, settings);

                                // Find corresponding point in other timeseries
                                IPFPoint ipfPoint2 = FindPoint(ipfFile2, ipfFileDictionaries, ipfPoint, id, pointIdx, baseIPFFile, fileIdx, settings, logIndentLevel);
                                
                                // Process other timeseries if found
                                if (ipfPoint2 != null)
                                {
                                    IPFTimeseries ipfTimeseries2 = GetIPFTimeSeries(ipfPoint2, pointIdx, id, inputFilenames, settings);
                                    if (ipfTimeseries2 != null)
                                    {
                                        int valueListIdx2 = GetValueListIndex(ipfTimeseries2, settings, fileIdx);

                                        SetMinMaxDate(ipfTimeseries2, ref minDate, ref maxDate);
                                        AddSeries(ref isDataMissing, zedGraphControl, ipfTimeseries2, valueListIdx2, seriesLabel2, colors, markerTypes[fileIdx], lineTypes[fileIdx], lineSizes[fileIdx], fileIdx, inputFilenames.Length, settings);
                                    }
                                    else
                                    {
                                        isDataMissing = true;
                                    }
                                }
                                else
                                {
                                    isDataMissing = true;
                                }
                            }
                        }
                    }
                    if (!IsSkipped(isDataMissing, settings))
                    {
                        WriteResult(ipfTimeseries, id, pointIdx, minDate, maxDate, zedGraphControl, outputPath, settings);
                        plotCount++;
                    }
                    else
                    {
                        Log.AddWarning("Plot has missing IPF-points or empty timeseries and is skipped: " + id, 1);
                    }
                }
            }

            ToolSuccessMessage = "Finished processing " + inputFilenames.Length + " input series, " + baseIPFFile.PointCount + " IPF-points and created " + plotCount +" plots";

            return exitcode;
        }

        protected bool IsSkipped(bool isDataMissing, SIFToolSettings settings)
        {
            return isDataMissing && settings.IsMissingPointExcluded;
        }

        protected virtual string GetSeriesLabel(int fileIdx, SIFToolSettings settings)
        {
            string seriesLabel = ((settings.SeriesLabels != null) && (settings.SeriesLabels.Count > fileIdx)) ? settings.SeriesLabels[fileIdx] : "series " + (fileIdx + 1);

            return seriesLabel;
        }

        /// <summary>
        /// Retrieve label for a series with a constant value, either from a specific column or an (un)named constant
        /// </summary>
        /// <param name="baseIPFFile"></param>
        /// <param name="fileIdx"></param>
        /// <param name="colIdx"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual string GetConstantLabel(IPFFile baseIPFFile, int fileIdx, int colIdx, SIFToolSettings settings)
        {
            string label = null;
            if ((settings.SeriesLabels != null) && (fileIdx > 0) && (fileIdx < settings.SeriesLabels.Count))
            {
                label = settings.SeriesLabels[fileIdx];
            }
            else
            {
                label = ((colIdx != -1) ? baseIPFFile.ColumnNames[colIdx] : "constant " + (fileIdx + 1));
            }

            return label;
        }

        /// <summary>
        /// Retrieve valuelist index in associated file for specified timeseries and file index
        /// </summary>
        /// <param name="ipfTimeseries"></param>
        /// <param name="settings"></param>
        /// <param name="fileIdx">zero based index in input IPF-files/plotrefs</param>
        /// <returns></returns>
        protected int GetValueListIndex(IPFTimeseries ipfTimeseries, SIFToolSettings settings, int fileIdx)
        {
            int valueListIdx = ((settings.ValueListNumbers != null) && (fileIdx < settings.ValueListNumbers.Count)) ? settings.ValueListNumbers[fileIdx] - 1 : 0;
            return valueListIdx;
        }

        /// <summary>
        /// Rertrieve first filename that matches with specified filter
        /// </summary>
        /// <param name="filenameFilter"></param>
        /// <returns></returns>
        protected static string ExpandFilenameFilter(string filenameFilter)
        {
            string fname = null;

            try
            {
                string path = Path.GetDirectoryName(filenameFilter);
                string filter = Path.GetFileName(filenameFilter);
                string[] files = Directory.GetFiles(path, filter);

                if (files.Length > 0)
                {
                    fname = files[0];
                }
            }
            catch (Exception)
            {
                // ignore, leave null
            }

            return fname;
        }

        protected virtual void InitializePlotSettings(string[] inputFilenames, SIFToolSettings settings)
        {
            // Retrieve specified graph settings for series
            colors = GetColors(settings, inputFilenames.Length + 1);
            lineTypes = GetLineTypes(settings, inputFilenames.Length + 1);
            markerTypes = GetMarkerTypes(settings, inputFilenames.Length + 1);
            lineSizes = GetLineSizes(settings, inputFilenames.Length + 1);

            plotPanel = new Panel();
            plotPanel.Size = new Size(800, 600);
            zedGraphControl = AddZedGraphControl(plotPanel, plotPanel.Size);
        }

        protected virtual List<float> GetLineSizes(SIFToolSettings settings, int count)
        {
            List<float> lineSizes = new List<float>();
            FillList(lineSizes, SIFToolSettings.DefaultLineSize, count);
            return lineSizes;
        }

        protected virtual List<DashStyle?> GetLineTypes(SIFToolSettings settings, int count)
        {
            List<DashStyle?> lineTypes = new List<DashStyle?>();
            FillList(lineTypes, SIFToolSettings.DefaultLineType, count);
            return lineTypes;
        }

        protected virtual List<SymbolType> GetMarkerTypes(SIFToolSettings settings, int count)
        {
            List<SymbolType> markerTypes = new List<SymbolType>();
            FillList(markerTypes, SIFToolSettings.DefaultMarkerType, count);
            return markerTypes;
        }

        protected virtual List<Color> GetColors(SIFToolSettings settings, int colorCount)
        {
            List<Color> colors = new List<Color>();
            colors.AddRange(settings.UserColors);

            Random rnd = new Random(DateTime.Now.Second);
            while (colors.Count < colorCount)
            {
                colors.Add(Color.FromArgb(rnd.Next(255), rnd.Next(255), rnd.Next(255)));
            }
            return colors;
        }

        protected void FillList<T>(List<T> list, T item, int fillCount)
        {
            while (list.Count < fillCount)
            {
                list.Add(item);
            }
        }

        private static bool IsIdDefined(SIFToolSettings settings, int ipfFileIdx)
        {
            return (settings.IdFormatStrings != null) && (ipfFileIdx < settings.IdFormatStrings.Count) && ((settings.IdFormatStrings[ipfFileIdx] != null) || !settings.IdFormatStrings[ipfFileIdx].Equals(string.Empty));
        }

        private void ReadInputIPFFiles(string[] inputFilenames, out List<IPFFile> ipfFiles, out List<Dictionary<string, IPFPoint>> ipfFileDictionaries, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            ipfFiles = new List<IPFFile>();
            ipfFileDictionaries = new List<Dictionary<string, IPFPoint>>();

            logIndentLevel = logIndentLevel + 1;

            for (int ipfFileIdx = 0; ipfFileIdx < inputFilenames.Length; ipfFileIdx++)
            {
                string inputFilename = inputFilenames[ipfFileIdx];

                if (!File.Exists(inputFilename))
                {
                    // Not a file, but it may be a constant value or column name
                    ipfFiles.Add(null);
                    ipfFileDictionaries.Add(null);
                    log.AddInfo("Constant value: " + inputFilename, logIndentLevel);
                }
                else
                {
                    try
                    {
                        log.AddInfo("Reading IPF-file " + Path.GetFileName(inputFilename) + " ...", logIndentLevel);
                        inputFilename = ExpandFilenameFilter(inputFilename);
                        IPFFile ipfFile = IPFFile.ReadFile(inputFilename, false, null, 0, null, false);
                        ipfFiles.Add(ipfFile);
                        if (IsIdDefined(settings, ipfFileIdx))
                        {
                            Dictionary<string, IPFPoint> ipfFileDictionary = new Dictionary<string, IPFPoint>();
                            foreach (IPFPoint ipfPoint in ipfFile.Points)
                            {
                                string id = RetrieveIdString(ipfPoint, settings, ipfFileIdx);
                                if (id != null)
                                {
                                    if (!ipfFileDictionary.ContainsKey(id))
                                    {
                                        ipfFileDictionary.Add(id, ipfPoint);
                                    }
                                    else
                                    {
                                        log.AddWarning("ID already present and skipped: " + id, logIndentLevel);
                                    }
                                }
                            }
                            ipfFileDictionaries.Add(ipfFileDictionary);
                        }
                        else
                        {
                            ipfFileDictionaries.Add(null);
                        }

                        if (ipfFileIdx > 0)
                        {
                            if (IsIdDefined(settings, ipfFileIdx) && IsIdDefined(settings, 0))
                            {
                                log.AddInfo("match by Id", logIndentLevel +1);
                            }
                            else if (ipfFiles[0].PointCount == ipfFile.PointCount)
                            {
                                log.AddInfo("match by number", logIndentLevel + 1);
                            }
                            else
                            {
                                log.AddInfo("match by XY and columnvalues", logIndentLevel + 1);
                            }
                        }
                        else
                        {
                            // log.AddInfo(string.Empty);
                        }
                    }
                    catch (Exception)
                    {
                        throw new Exception("Error while reading IPF-file: " + inputFilename);
                    }
                }
            }
        }

        protected ZedGraphControl AddZedGraphControl(Panel panel, Size graphSize)
        {
            ZedGraphControl zedGraphControl = new ZedGraphControl();

            // zedGraphControl.Dock = System.Windows.Forms.DockStyle.Fill;
            zedGraphControl.Location = new System.Drawing.Point(3, 3);
            zedGraphControl.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            zedGraphControl.Name = "zedGraphControl";
            //zedGraphControl.ScrollGrace = 0D;
            //zedGraphControl.ScrollMaxX = 0D;
            //zedGraphControl.ScrollMaxY = 0D;
            //zedGraphControl.ScrollMaxY2 = 0D;
            //zedGraphControl.ScrollMinX = 0D;
            //zedGraphControl.ScrollMinY = 0D;
            //zedGraphControl.ScrollMinY2 = 0D;
            // zedGraphControl.TabIndex = 0;
            zedGraphControl.Size = graphSize;

            // GraphPane object holds one or more Curve objects (or plots)
            GraphPane graphPane = zedGraphControl.GraphPane;
            graphPane.LineType = LineType.Normal;

            // graphPane.CurveList.Clear();
            graphPane.XAxis.Title = new AxisLabel("Date", "Arial", 14, Color.Black, true, false, false);
            graphPane.YAxis.Title = new AxisLabel("Level (m+NAP)", "Arial", 14, Color.Black, true, false, false);
            graphPane.XAxis.Title.FontSpec.Border.IsVisible = false;
            graphPane.YAxis.Title.FontSpec.Border.IsVisible = false;
            graphPane.YAxis.MajorGrid.IsZeroLine = false;

            graphPane.XAxis.Type = AxisType.Date;
            graphPane.XAxis.Scale.Format = "yyyy-MM-dd";
            // graphPane.XAxis.Scale.MinGrace = 0;
            // graphPane.XAxis.Scale.MaxGrace = 0;
            // graphPane.XAxis.Scale.MajorUnit = DateUnit.Month;
            // graphPane.XAxis.Scale.MinorUnit = DateUnit.Day;
            // graphPane.XAxis.Scale.IsPreventLabelOverlap = true;
            // graphPane.XAxis.Scale.Min = DateTime.Now.Subtract(new TimeSpan(1, 0, 0, 0, 0)).ToOADate();
            // graphPane.XAxis.Scale.Max = DateTime.Now.ToOADate();

            // graphPane.Legend.Position = ZedGraph.LegendPos.TopCenter;
            graphPane.Legend.Border.IsVisible = false;

            graphPane.IsFontsScaled = false;

            // Refreshing the plot
            zedGraphControl.AxisChange();
            zedGraphControl.Invalidate();
            zedGraphControl.Refresh();

            panel.Controls.Clear();
            panel.Controls.Add(zedGraphControl);

            return zedGraphControl;
        }

        private static string RetrieveIdString(IPFPoint ipfPoint, SIFToolSettings settings, int fileIdx)
        {
            if (!IsIdDefined(settings, fileIdx))
            {
                return null;
            }

            string formatString = settings.IdFormatStrings[fileIdx];
            if ((formatString == null) || formatString.Equals(string.Empty))
            {
                return null;
            }

            
            // Handle integer Id string
            if (int.TryParse(formatString, out int colNr))
            {
                // whole formatstring is an integer, use as one-based column number and convert to (zero-based) index
                int colIdx = colNr - 1;
                
                if ((colIdx < 0) || (colIdx >= ipfPoint.ColumnValues.Count))
                {
                    throw new ToolException("ID columnnumber (" + formatString + ") should be larger than zero and or less than or equal to column count (" + ipfPoint.ColumnValues.Count + ")");
                }
                else
                {
                    return ipfPoint.ColumnValues[colIdx];
                }
            }

            // Handle non-integer Id string, which can be a column number or a column expression, which allowed combinations of arbritrary string and column references between brackets
            string id = string.Empty;
            int idx1 = formatString.IndexOf('{'); // index in string to current forward bracket '{', or -1 if not found
            int idx2 = -1;                        // index in string to current backward bracket '}', or -1 if not found
            while (idx1 >= 0)
            {
                // Retrieve column reference (name or number) inside optional expression brackets
                id += formatString.Substring(idx2 + 1, idx1 - idx2 - 1);
                idx2 = formatString.IndexOf('}', idx1 + 1);
                string colRefString = formatString.Substring(idx1 + 1, idx2 - idx1 - 1);
                int colIdx = -1;

                // Parse column reference as a number or name
                if (int.TryParse(colRefString, out colNr))
                {
                    // Column reference is a number, convert to index and check validity
                    colIdx = colNr - 1;
                    if (colIdx < 0)
                    {
                        throw new ToolException("Invalid column number string (zero or negative): " + colRefString);
                    }
                    else if (colIdx >= ipfPoint.ColumnValues.Count)
                    {
                        throw new ToolException("Invalid column number string (larger than number of columns): " + colRefString);
                    }
                }
                else
                {
                    // Column reference is not a number, check columname
                    colIdx = ipfPoint.IPFFile.FindColumnName(colRefString);
                    if (colIdx == -1)
                    {
                        throw new ToolException("Column number string '" + colRefString + "' in ID formatstring is not a column number or column name: " + formatString);
                    }
                }

                // Valid column index found
                string idSubstring = ipfPoint.ColumnValues[colIdx];
                if ((idSubstring != null) && idSubstring.Contains("\\"))
                {
                    // Remove directory part if string seems to be a filename
                    idSubstring = Path.GetFileName(idSubstring);
                }
                id += idSubstring;

                idx1 = formatString.IndexOf('{', idx2 + 1);
            }
            id += formatString.Substring(idx2 + 1);

            return id;
        }

        protected virtual IPFTimeseries GetIPFTimeSeries(IPFPoint ipfPoint, int pointIdx, string id, string[] inputFilenames, SIFToolSettings settings)
        {
            IPFTimeseries ipfTimeseries = null;
            try
            {
                ipfTimeseries = ipfPoint.Timeseries;
            }
            catch (Exception)
            {
                throw new ToolException("Invalid timeseries file for point " + (pointIdx + 1) + ", " + id + "(" + ipfPoint.ToString(0) + ")" + " in file: " + Path.GetFileName(inputFilenames[0]));
            }

            return ipfTimeseries;
        }

        protected virtual void CheckColumnIndex(IPFTimeseries ipfTimeseries, SIFToolSettings settings, int valueListIdx)
        {
            if ((valueListIdx < 0) || (valueListIdx >= ipfTimeseries.ValueColumns.Count))
            {
                throw new ToolException("Value column number (" + (valueListIdx + 1) + " is not valid for associated file '" + Path.GetFileName(ipfTimeseries.Filename) + "' with " + ipfTimeseries.ValueColumns.Count + " column(s)");
            }
        }

        private void SetMinMaxDate(IPFTimeseries ipfTimeseries, ref DateTime minDate, ref DateTime maxDate)
        {
            if ((ipfTimeseries.Timestamps.Count > 0) && (ipfTimeseries.Timestamps[0] < minDate))
            {
                minDate = ipfTimeseries.Timestamps[0];
            }
            if ((ipfTimeseries.Timestamps.Count > 0) && (ipfTimeseries.Timestamps[ipfTimeseries.Timestamps.Count - 1] > maxDate))
            {
                maxDate = ipfTimeseries.Timestamps[ipfTimeseries.Timestamps.Count - 1];
            }
        }

        protected virtual void AddSeries(ref bool isDataMissing, ZedGraphControl zedGraphControl, IPFTimeseries ipfTimeseries, int valueListIdx, string seriesLabel, List<Color> colors, SymbolType symbolType, DashStyle? lineType, float lineSize, int fileIdx, int fileCount, SIFToolSettings settings)
        {
            float avgValue = ipfTimeseries.CalculateAverage(valueListIdx);
            if (!avgValue.Equals(ipfTimeseries.NoDataValue) && !avgValue.Equals(float.NaN))
            {
                AddTimeseries(zedGraphControl, ipfTimeseries, valueListIdx, seriesLabel, colors[fileIdx], symbolType, settings, lineType, lineSize);
            }
            else if (fileIdx >= 0)
            {
                isDataMissing = true;
            }
        }
        
        protected LineItem AddTimeseries(ZedGraphControl zedGraphControl, IPFTimeseries ipfTimeseries, int valueListIdx, string label, Color color, SymbolType symbolType, SIFToolSettings settings, DashStyle? lineDashStyle = DashStyle.Solid, float lineThickness = 1)
        {
            List<double> dblList1 = new List<double>();
            List<double> dblList2 = new List<double>();
            for (int i = 0; i < ipfTimeseries.Timestamps.Count; i++)
            {
                float value = ipfTimeseries.ValueColumns[valueListIdx][i];
                DateTime time = ipfTimeseries.Timestamps[i];
                float noDataValue = ipfTimeseries.NoDataValues[valueListIdx];
                HandleNoDataValues(value, time, ref dblList1, ref dblList2, noDataValue, settings);
            }

            LineItem rasterCurve = zedGraphControl.GraphPane.AddCurve(label, new PointPairList(dblList1.ToArray(), dblList2.ToArray()), color, symbolType);
            rasterCurve.Line.Width = lineThickness;
            rasterCurve.Symbol.Size = 4;
            rasterCurve.Symbol.Fill.Type = FillType.Solid;
            rasterCurve.Line.IsVisible = (lineDashStyle != null);
            rasterCurve.Line.Style = (lineDashStyle != null) ? ((DashStyle)lineDashStyle) : DashStyle.Solid;
            //rasterCurve.Line.Fill = new Fill(Color.Blue);

            return rasterCurve;
        }

        protected virtual void HandleNoDataValues(float value, DateTime time, ref List<double> dblList1, ref List<double> dblList2, float noDataValue, SIFToolSettings settings)
        {
            if (!value.Equals(noDataValue))
            {
                dblList1.Add(new XDate(time));
                dblList2.Add(value);
            }
        }

        protected virtual LineItem AddConstantTimeseries(ZedGraphControl zedGraphControl, float constantValue, DateTime dateTime1, DateTime dateTime2, string label, Color color, int fileIdx, DashStyle lineDashStyle = DashStyle.Solid, float lineThickness = 1)
        {
            double[] dummyDates = new double[] { new XDate(dateTime1), new XDate(dateTime2) };
            double[] constantValues = new double[] { constantValue, constantValue };

            LineItem rasterCurve = zedGraphControl.GraphPane.AddCurve(label, new PointPairList(dummyDates, constantValues), color, ZedGraph.SymbolType.None);
            rasterCurve.Line.Width = lineThickness;
            rasterCurve.Line.Style = lineDashStyle;
            return rasterCurve;
        }

        protected void ParsePlotRef2(string columnString, IPFFile baseIPFFile, IPFPoint ipfPoint, int fileIdx, ref int colIdx, ref float constantValue)
        {
            // Retrieve column index for specified column reference string
            if (int.TryParse(columnString, out int colNr))
            {
                // String contains an integer, it is assumed to be a column number
                colIdx = colNr - 1;
                if (colIdx < 0)
                {
                    throw new ToolException("Invalid column number string (zero or negative): " + columnString);
                }
                else if (colIdx >= ipfPoint.ColumnValues.Count)
                {
                    throw new ToolException("Invalid column number string (larger than number of columns): " + columnString);
                }
            }
            else
            {
                // String does not contain an integer, check columname of first IPF-file
                colIdx = baseIPFFile.FindColumnName(columnString);
            }

            if (colIdx == -1)
            {
                // String does not contain an integer or an existing column name, check for floating point constant value
                if (!float.TryParse(columnString, NumberStyles.Float, EnglishCultureInfo, out constantValue))
                {
                    throw new ToolException("No valid IPF-file, column index/name (in first IPF-file) or floating point value found for entry " + (fileIdx + 1) + ": " + columnString);
                }
            }
            else
            {
                // A valid column index is found, retrieve column value
                string columnValueString = ipfPoint.ColumnValues[colIdx];
                if (columnValueString != null)
                {
                    if (!float.TryParse(columnValueString, NumberStyles.Float, EnglishCultureInfo, out constantValue))
                    {
                        throw new ToolException("Column value for column number '" + colIdx + 1 + "' has invalid plot value: " + columnValueString);
                    }
                }
            }
        }

        private IPFPoint FindPoint(IPFFile ipfFile2, List<Dictionary<string, IPFPoint>> ipfFileDictionaries, IPFPoint ipfPoint, string id, int pointIdx, IPFFile baseIPFFile, int fileIdx, SIFToolSettings settings, int logIndentLevel)
        {
            IPFPoint ipfPoint2 = null;
            if (IsIdDefined(settings, fileIdx))
            {
                ipfPoint2 = ipfFileDictionaries[fileIdx][id];
                if (ipfPoint2 == null)
                {
                    Log.AddInfo("Point " + ((id != null) ? id : ipfPoint.ToString()) + " not found by ID in series " + (fileIdx + 1) + " and is skipped.");
                }
            }
            else if (ipfFile2.PointCount == baseIPFFile.PointCount)
            {
                ipfPoint2 = ipfFile2.Points[pointIdx];
            }
            else
            {
                // Try to find point in other IPF-file with same XY-coordinates and same columnvalues
                int pointIdx2 = ipfFile2.IndexOf(ipfPoint);
                if (pointIdx2 >= 0)
                {
                    ipfPoint2 = ipfFile2.Points[pointIdx2];
                }
                else
                {
                    // Try to find point in other IPF-file with same XY-coordinates and same columnvalues
                    pointIdx2 = ipfFile2.IndexOf(ipfPoint, ipfFile2.AssociatedFileColIdx, ipfPoint.IPFFile.AssociatedFileColIdx);
                    if (pointIdx2 >= 0)
                    {
                        ipfPoint2 = ipfFile2.Points[pointIdx2];
                    }
                    else
                    {
                        Log.AddWarning("Point " + ((id != null) ? id : ipfPoint.ToString()) + " not found in series " + (fileIdx + 1) + " and is skipped.", logIndentLevel);
                    }
                }
            }
            return ipfPoint2;
        }

        protected virtual void WriteResult(IPFTimeseries ipfTimeseries, string id, int pointIdx, DateTime minDate, DateTime maxDate, ZedGraphControl zedGraphControl, string outputPath, SIFToolSettings settings)
        {
            CreateWindow(zedGraphControl, minDate, maxDate, settings);
            RefreshWindow(zedGraphControl, outputPath, id, pointIdx);
        }

        protected virtual void CreateWindow(ZedGraphControl zedGraphControl, DateTime minDate, DateTime maxDate, SIFToolSettings settings, IPFTimeseries ipfTimeSeries = null)
        {
            zedGraphControl.GraphPane.XAxis.Scale.Min = new XDate(minDate);
            zedGraphControl.GraphPane.XAxis.Scale.Max = new XDate(maxDate);
        }

        protected void RefreshWindow(ZedGraphControl zedGraphControl, string outputPath, string id, int pointIdx)
        {
            zedGraphControl.AxisChange();
            zedGraphControl.Invalidate();
            zedGraphControl.Refresh();
            string pngFilename = Path.Combine(outputPath, ((id != null) ? id : "timeseries" + pointIdx) + ".png");
            string pngPath = Path.GetDirectoryName(pngFilename);
            if ((pngPath != null) && !pngPath.Equals(string.Empty) && !Directory.Exists(pngPath))
            {
                Directory.CreateDirectory(pngPath);
            }
            zedGraphControl.MasterPane.GetImage().Save(pngFilename);
        }
    }
}
