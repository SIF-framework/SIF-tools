// IPFGENconvert is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IPFGENconvert.
// 
// IPFGENconvert is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IPFGENconvert is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IPFGENconvert. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.GIS;


namespace Sweco.SIF.IPFGENconvert
{
    public class SIFTool : SIFToolBase
    {
        #region Constructor

        /// <summary>
        /// Creates a SIFTool instance and initializes tool name and version and a Log object with the console as a default listener
        /// </summary>
        public SIFTool(SIFToolSettingsBase settings) : base(settings)
        {
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

            System.Environment.Exit(exitcode);
        }

        /// <summary>
        /// Define properties of tool as shown in the tool header (e.g. authors, purpose, license strings)
        /// </summary>
        protected override void DefineToolProperties()
        {
            AddAuthor("Koen van der Hauw");
            AddAuthor("Koen Jansen");
            ToolPurpose = "Tool for converting IPF- to GEN-files or vice versa";
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

            // Create output path if not yet existing
            if (!Directory.Exists(settings.OutputPath))
            {
                Directory.CreateDirectory(settings.OutputPath);
            }

            int logIndentLevel = 0;

            int fileCount = 0;
            string[] inputFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter, settings.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
            if ((inputFilenames.Length > 1) && (settings.OutputFilename != null))
            {
                throw new ToolException("An output filename is specified, but more than one input file is found for current filter: " + settings.InputFilter);
            }

            foreach (string inputFilename in inputFilenames)
            {
                logIndentLevel = 1;

                if (Path.GetExtension(inputFilename).ToLower().Equals(".ipf"))
                {
                    string newFilename = RetrieveOutputFilename(inputFilename, "GEN", settings);
                    ConvertIPFToGEN(inputFilename, newFilename, settings, logIndentLevel);
                    
                }
                else if (Path.GetExtension(inputFilename).ToLower().Equals(".gen"))
                {
                    string newFilename = RetrieveOutputFilename(inputFilename, "IPF", settings);
                    ConvertGENToIPF(inputFilename, newFilename, settings, logIndentLevel);
                }
                else
                {
                    throw new ToolException("Input filename should be a GEN- or IPF-file: " + inputFilename);
                }

                fileCount++;
            }

            ToolSuccessMessage = "Finished processing " + fileCount + " file(s)";

            return exitcode;
        }

        private void ConvertIPFToGEN(string inputFilename, string genFilename, SIFToolSettings settings, int logIndentLevel)
        {
            CheckIPFGENSettings(settings);

            Log.AddInfo("Reading IPF-file " + Path.GetFileName(inputFilename) + " ...", logIndentLevel);
            IPFFile ipfFile = ReadIPFFile(inputFilename, settings);

            if (ipfFile.PointCount > 0)
            {
                Log.AddInfo("Creating GEN-file ...", logIndentLevel);

                GENFile genFile = new GENFile();
                DATFile datFile = CreateDATFile(ipfFile, genFile, settings);


                // Convert IPF-points to GEN-features and add to GEN-file
                ConvertIPFPointsToGEN(ipfFile, genFile, datFile, settings, logIndentLevel);

                WriteGENFile(genFile, genFilename, settings, Log, logIndentLevel);
            }
            else
            {
                Log.AddWarning("IPF-file has no points and is skipped", logIndentLevel + 1);
            }
        }

        protected void ConvertGENToIPF(string inputFilename, string ipfFilename, SIFToolSettings settings, int logIndentLevel)
        {
            Log.AddInfo("Reading GEN-file " + inputFilename + " ...", logIndentLevel);
            GENFile genFile = GENFile.ReadFile(inputFilename);

            if (genFile.Features.Count > 0)
            {
                Log.AddInfo("Creating IPF-file ...", logIndentLevel);

                IPFFile ipfFile = new IPFFile();
                AddColumnames(genFile, ipfFile, settings);

                // Convert GEN-features to IPF-points and add to IPF-file
                ConvertGENFeaturesToIPF(genFile, ipfFile, settings, logIndentLevel);

                Log.AddInfo("Writing IPF-file " + Path.GetFileName(ipfFilename) + " ...", logIndentLevel);
                WriteIPFFile(ipfFile, ipfFilename, settings);
            }
            else
            {
                Log.AddWarning("GEN-file has no features and is skipped", logIndentLevel + 1);
            }
        }

        protected virtual void CheckIPFGENSettings(SIFToolSettings settings)
        {
            if (settings.Method == 1)
            {
                if (settings.MethodParameter.Equals(double.NaN))
                {
                    throw new ToolException("For IPF-GEN-conversion and method 1, parameter m2 should be defined");
                }
            }
        }

        protected virtual void ConvertIPFPointsToGEN(IPFFile ipfFile, GENFile genFile, DATFile datFile, SIFToolSettings settings, int logIndentLevel)
        {
            switch (settings.Method)
            {
                case 1:
                    // Create squares with specified edge length
                    ProcessIPFGENMethod1(ipfFile, genFile, datFile, settings, logIndentLevel);
                    break;
                case 2:
                    // Create convex hull around all points
                    ProcessIPFGENMethod2(ipfFile, genFile, datFile, settings, logIndentLevel);
                    break;
                default:
                    throw new ToolException("Undefined method for IPF-GEN conversion: " + settings.Method);
            }
        }

        protected virtual void ConvertGENFeaturesToIPF(GENFile genFile, IPFFile ipfFile, SIFToolSettings settings, int logIndentLevel)
        {
            switch (settings.Method)
            {
                case 1:
                    AddCenterPoints(genFile, ipfFile, settings);
                    break;
                default:
                    throw new ToolException("Undefined method for GEN-IPF conversion: " + settings.Method);
            }
        }

        protected virtual string RetrieveOutputFilename(string inputFilename, string outputExtension, SIFToolSettings settings)
        {
            string outputFilename;

            if (outputExtension.StartsWith("."))
            {
                outputExtension = outputExtension.Substring(1);
            }

            if (settings.OutputFilename == null)
            {
                outputFilename = Path.Combine(settings.OutputPath, Path.GetFileNameWithoutExtension(inputFilename) + "." + outputExtension);
            }
            else
            {
                outputFilename = Path.Combine(settings.OutputPath, settings.OutputFilename);
            }

            return outputFilename;
        }

        /// <summary>
        /// Convert IPF-points to GEN-features with method 1: create a square around each point
        /// </summary>
        /// <param name="ipfFile"></param>
        /// <param name="genFile"></param>
        /// <param name="datFile"></param>
        /// <param name="settings"></param>
        /// <param name="logIndentLevel"></param>
        protected virtual void ProcessIPFGENMethod1(IPFFile ipfFile, GENFile genFile, DATFile datFile, SIFToolSettings settings, int logIndentLevel)
        {
            int pointID = 1;

            // Read and (pre)process individual IPF-points
            for (int i = 0; i < ipfFile.PointCount; i++)
            {
                IPFPoint ipfPoint = ipfFile.Points[i];

                // Retrieve centerpoint for square
                RetrievePointXY(ipfPoint, out double x, out double y, settings);

                // Create square around point
                double dist = settings.MethodParameter / 2;
                GENFeature genFeature = new GENPolygon(genFile, pointID.ToString());
                genFeature.Points.Add(new DoublePoint(x - dist, y - dist));
                genFeature.Points.Add(new DoublePoint(x - dist, y + dist));
                genFeature.Points.Add(new DoublePoint(x + dist, y + dist));
                genFeature.Points.Add(new DoublePoint(x + dist, y - dist));
                genFeature.Points.Add(new DoublePoint(x - dist, y - dist));

                genFile.AddFeature(genFeature);

                if (ipfFile.ColumnCount > 2)
                {
                    // Point has more than just XY-columns
                    DATRow datRow = CreateDATRow(genFile, genFeature, ipfPoint, ipfFile, pointID, settings);
                    datFile.AddRow(datRow);
                }

                pointID++;
            }
        }

        /// <summary>
        /// Convert IPF-points to GEN-features with method 2: create hull around all points
        /// </summary>
        /// <param name="ipfFile"></param>
        /// <param name="genFile"></param>
        /// <param name="datFile"></param>
        /// <param name="settings"></param>
        /// <param name="logIndentLevel"></param>
        protected virtual void ProcessIPFGENMethod2(IPFFile ipfFile, GENFile genFile, DATFile datFile, SIFToolSettings settings, int logIndentLevel)
        {
            int pointID = 1;
            List<Point> hullPoints = new List<Point>();

            // Read and (pre)process individual IPF-points
            for (int i = 0; i < ipfFile.PointCount; i++)
            {
                IPFPoint ipfPoint = ipfFile.Points[i];

                // Retrieve centerpoint for square
                RetrievePointXY(ipfPoint, out double x, out double y, settings);

                // For method 2 a convex hull is created around all points; store unique points and process later
                DoublePoint hullPoint = new DoublePoint(x, y);
                if (!hullPoints.Contains(hullPoint))
                {
                    hullPoints.Add(hullPoint);
                }

                pointID++;
            }

            Log.AddInfo("Retrieving convex hull ...", logIndentLevel + 1);
            CreateConvexHullGENFile(genFile, ipfFile, hullPoints, settings);
        }

        protected virtual DATFile CreateDATFile(IPFFile ipfFile, GENFile genFile, SIFToolSettings settings)
        {
            DATFile datFile = null;

            if (ipfFile.ColumnCount > 2)
            {
                datFile = new DATFile(genFile);
                genFile.DATFile = datFile;
                datFile.AddColumn("ID");

                // Skip XY-columns
                for (int colIdx = 2; colIdx < ipfFile.ColumnNames.Count; colIdx++)
                {
                    string columnName = datFile.GetUniqueColumnName(ipfFile.ColumnNames[colIdx]);
                    datFile.AddColumn(columnName);
                }
            }

            return datFile;
        }

        protected virtual IPFFile ReadIPFFile(string inputFilename, SIFToolSettings settings)
        {
            IPFFile ipfFile = IPFFile.ReadFile(inputFilename, settings.XColIdx, settings.YColIdx, -1);

            if (settings.Extent != null)
            {
                ipfFile = ipfFile.ClipIPF(settings.Extent);
            }

            return ipfFile;
        }

        /// <summary>
        /// Retrieve XY-coordinates for specified point depending on settings
        /// </summary>
        protected virtual void RetrievePointXY(Point point, out double x, out double y, SIFToolSettings settings)
        {
            if (!settings.SnapCellsize.Equals(double.NaN))
            {
                if (settings.SnapType == SnapType.SnapToGrid)
                {
                    // snap to gridedges based on cellszie
                    x = ((int)settings.SnapCellsize) * Math.Round(point.X / (int)settings.SnapCellsize, 0);
                    y = ((int)settings.SnapCellsize) * Math.Round(point.Y / (int)settings.SnapCellsize, 0);
                }
                else
                {
                    // snap to cellcenter based on cellszie
                    x = (settings.SnapCellsize / 2.0) + ((int)settings.SnapCellsize) * Math.Floor(point.X / (int)settings.SnapCellsize);
                    y = (settings.SnapCellsize / 2.0) + ((int)settings.SnapCellsize) * Math.Floor(point.Y / (int)settings.SnapCellsize);
                }
            }
            else
            {
                // Do not modify XY-coordinates
                x = point.X;
                y = point.Y;
            }
        }

        protected virtual DATRow CreateDATRow(GENFile genFile, GENFeature genFeature, IPFPoint ipfPoint, IPFFile ipfFile, int id, SIFToolSettings settings)
        {
            DATRow datRow = new DATRow();
            datRow.Add(id.ToString());

            // Add row values to shapefile
            if (ipfPoint.ColumnValues != null)
            {
                for (int colIdx = 2; colIdx < ipfFile.ColumnCount; colIdx++)
                {
                    datRow.Add(ipfPoint.ColumnValues[colIdx]);
                }
            }
            return datRow;
        }

        protected virtual void CreateConvexHullGENFile(GENFile genFile, IPFFile ipfFile, List<Point> hullPoints, SIFToolSettings settings)
        {
            // Create temporary GEN-file for hull feature
            GENFile tmpGENFile = new GENFile();
            GENPolygon genPolygon = new GENPolygon(tmpGENFile, "0");

            List<Point> convexHullPoints = ConvexHull.RetrieveConvexHull(hullPoints);
            if (convexHullPoints != null)
            {

                tmpGENFile.AddFeature(genPolygon);
                // idfGENFile.WriteFile(Path.Combine(Directory.GetCurrentDirectory(), "ConvexHull.gen"));
                genPolygon.Points.AddRange(convexHullPoints);
                genPolygon.Points.Add(new DoublePoint(convexHullPoints[0].X, convexHullPoints[0].Y));

                genFile.AddFeatures(tmpGENFile.Features, true);
            }
        }

        protected virtual void WriteGENFile(GENFile genFile, string genFilename, SIFToolSettings settings, Log log, int logIndentLevel)
        {
            if (settings.Postfix != null)
            {
                genFilename = Path.Combine(Path.GetDirectoryName(genFilename), Path.GetFileNameWithoutExtension(genFilename) + settings.Postfix + Path.GetExtension(genFilename));
            }

            log.AddInfo("Writing GEN-file " + Path.GetFileName(genFilename) + " ...", logIndentLevel);

            if (genFile != null)
            {
                genFile.WriteFile(genFilename);
            }
            else
            {
                throw new ToolException("GEN-file could not be created:" + genFilename);
            }
        }

        /// <summary>
        /// Write IPF-file using specified settings
        /// </summary>
        /// <param name="ipfFile"></param>
        /// <param name="ipfFilename"></param>
        /// <param name="settings"></param>
        protected virtual void WriteIPFFile(IPFFile ipfFile, string ipfFilename, SIFToolSettings settings)
        {
            if (settings.Postfix != null)
            {
                ipfFilename = Path.Combine(Path.GetDirectoryName(ipfFilename), Path.GetFileNameWithoutExtension(ipfFilename) + settings.Postfix + Path.GetExtension(ipfFilename));
            }

            if (settings.Extent != null)
            {
                ipfFile = ipfFile.ClipIPF(settings.Extent);
            }

            if (ipfFile != null)
            {
                ipfFile.WriteFile(ipfFilename);
            }
            else
            {
                throw new ToolException("IPF-file could not be created: " + ipfFilename);
            }
        }

        /// <summary>
        /// Add columnnames from GEN-file to specified IPF-file
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="ipfFile"></param>
        /// <param name="basicSettings"></param>
        protected virtual void AddColumnames(GENFile genFile, IPFFile ipfFile, SIFToolSettings settings)
        {
            ipfFile.AddXYColumns();

            if (genFile.HasDATFile())
            {
                // Copy all columnnames from GEN-file
                DATFile datFile = genFile.DATFile;
                for (int colIdx = 0; colIdx < datFile.ColumnNames.Count; colIdx++)
                {
                    string colName = ipfFile.FindUniqueColumnName(datFile.ColumnNames[colIdx]);
                    ipfFile.AddColumn(colName);
                }
            }
        }

        /// <summary>
        /// Add points from features from GEN-file to specified IPF-file
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="ipfFile"></param>
        /// <param name="basicSettings"></param>
        /// <param name="logIndentLevel"></param>
        protected virtual void ConvertFeatures(GENFile genFile, IPFFile ipfFile, SIFToolSettings settings, int logIndentLevel)
        {
            if (settings.Method == 1)
            {
                AddCenterPoints(genFile, ipfFile, settings);
            }
            else
            {
                throw new ToolException("Unknown method for GEN to IPF conversion: " + settings.Method);
            }
        }

        /// <summary>
        /// Add center points of features from GEN-file to specified IPF-file
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="ipfFile"></param>
        /// <param name="settings"></param>
        protected void AddCenterPoints(GENFile genFile, IPFFile ipfFile, SIFToolSettings settings)
        {
            foreach (GENFeature genFeature in genFile.Features)
            {
                List<Point> pointList = genFeature.Points;

                double x = 0;
                double y = 0;
                for (int pointIdx = 0; pointIdx < pointList.Count; pointIdx++)
                {
                    Point point = pointList[pointIdx];
                    x += point.X;
                    y += point.Y;
                }
                Point avgPoint = new DoublePoint(x / (double)pointList.Count, y / (double)pointList.Count);

                List<string> columnValues = RetrieveColumnValues(genFeature, genFile, avgPoint, settings);
                IPFPoint ipfPoint = new IPFPoint(ipfFile, avgPoint, columnValues);
                ipfFile.AddPoint(ipfPoint);
            }
        }

        /// <summary>
        /// Retrieve IPF-point list, starting with XY-coordinates of specified xyPoint, with column values of feature in specified GEN-file
        /// </summary>
        /// <param name="genFeature"></param>
        /// <param name="genFile"></param>
        /// <param name="xyPoint"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual List<string> RetrieveColumnValues(GENFeature genFeature, GENFile genFile, Point xyPoint, SIFToolSettings settings)
        {
            List<string> columnValues = new List<string>();
            columnValues.Add(xyPoint.XString);
            columnValues.Add(xyPoint.YString);
            if (genFile.HasDATFile())
            {
                string id = genFeature.ID;
                DATRow datRow = genFile.DATFile.GetRow(id);
                if (datRow != null)
                {
                    for (int colIdx = 0; colIdx < datRow.Count; colIdx++)
                    {
                        columnValues.Add(datRow[colIdx]);
                    }
                }
            }

            return columnValues;
        }
    }
}
