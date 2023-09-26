// IDFGENconvert is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFGENconvert.
// 
// IDFGENconvert is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFGENconvert is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFGENconvert. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMOD.Values;

namespace Sweco.SIF.IDFGENconvert
{
    /// <summary>
    /// Class for conversion of IDF-files to GEN-files
    /// </summary>
    public class IDFGENConverter
    {
        public const int StatisticsDecimalCount = 3;

        protected SIFToolSettings settings;
        protected Log log;

        public IDFGENConverter(SIFToolSettings settings, Log log)
        {
            this.settings = settings;
            this.log = log;
        }

        /// <summary>
        /// Initalize result GEN-file and metadata file based on specified settings
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="metadata"></param>
        /// <param name="settings"></param>
        /// <param name="inputFilename"></param>
        public virtual void InitializeGENFile(ref GENFile genFile, ref Metadata metadata, SIFToolSettings settings, string inputFilename)
        {
            if ((settings.IsMerged) && (genFile == null))
            {
                // Results are merged in a single GEN-file
                genFile = new GENFile();
                genFile.Filename = Path.Combine(settings.OutputPath, settings.MergedGENFilename);
                AddDATFile(genFile);

                metadata = new Metadata("Automatic conversion from IDF to GEN-file with convex hull around non-NoData and non-zero cells");
                metadata.Source = settings.InputPath + "; " + settings.InputFilter;
            }
            else
            {
                // A resulting GEN-file is created for each input IDF-file
                genFile = new GENFile();
                if ((settings.OutputFilename != null) && Path.GetExtension(settings.OutputFilename).ToLower().Equals(".gen"))
                {
                    genFile.Filename = Path.Combine(settings.OutputPath, Path.GetFileNameWithoutExtension(settings.OutputFilename + ".GEN"));
                }
                else
                {
                    genFile.Filename = Path.Combine(settings.OutputPath, Path.GetFileNameWithoutExtension(inputFilename) + ".GEN");
                }
                
                AddDATFile(genFile);

                metadata = new Metadata("Automatic conversion from IDF to GEN-file with convex hull around non-NoData and non-zero cells");
                metadata.Source = inputFilename;
            }
        }

        /// <summary>
        /// Convert IDF-file to specified GEN-file
        /// </summary>
        /// <param name="inputIDFFilename">input IDF-filename</param>
        /// <param name="genFile">GEN-file to which converted GEN-features will be added. It should have a DAT-file with columns: SourceFile, Source, Idx, SourceValue</param>
        /// <param name="outputPath"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        public virtual bool Convert(string inputIDFFilename, ref GENFile genFile, string outputPath, int logIndentLevel)
        {
            log.AddInfo("converting " + Path.GetFileName(inputIDFFilename) + " to '" + settings.GetHullTypeString() +  "' ...", logIndentLevel);

            // Read input IDF-file and remove optionally skipped values
            IDFFile inputIDFFile = IDFFile.ReadFile(inputIDFFilename);
            if (settings.SkippedValues != null)
            {
                log.AddInfo("skipping values ...", logIndentLevel);
                foreach (ValueRange range in settings.SkippedValues)
                {
                    if (range.V1.Equals(range.V2))
                    {
                        inputIDFFile.ReplaceValues((float)range.V1, inputIDFFile.NoDataValue);
                    }
                    else
                    {
                        inputIDFFile.ReplaceValues(range, inputIDFFile.NoDataValue);
                    }
                }
            }

            // Convert IDF-file to GEN-file
            GENFile idfGENFile = Convert(inputIDFFile, genFile.Features.Count + 1, outputPath, logIndentLevel);

            // Add feartures from resulting GEN-file to output GEN-file
            if ((idfGENFile != null) && (idfGENFile.Features.Count > 0))
            {
                int genFeatureIdx = genFile.Features.Count + 1;
                foreach (GENFeature genFeature in idfGENFile.Features)
                {
                    // string sourceID = genFeature.ID;

                    //if ((genFeature.ID == "12933") || (genFeature.ID == "12934"))
                    //{
                    //    int a = 0;
                    //}

                    DATRow datRow = idfGENFile.DATFile.GetRow(genFeature.ID);
//                    genFeature.GENFile.DATFile.RemoveRow(sourceID);
//                    genFeature.ID = genFeatureIdx.ToString();
                    if (datRow == null)
                    {
                    //    datRow[0] = genFeature.ID;
                    //    genFeature.GENFile.DATFile.AddRow(datRow);
                    //}
                    //else
                    //{
                        log.AddWarning("No DAT-row found for GEN-feature " + genFeature.ID);
                    }

                    // Add GEN-feature, but use renumbered id, to get sequential id's in resulting GEN-file 
                    genFile.AddFeature(genFeature, true, genFeatureIdx);

                    genFeatureIdx++;
                }
            }

            return true;
        }

        /// <summary>
        /// Convert specified IDF-file to GEN-file with specified settings.HullType 
        /// </summary>
        /// <param name="inputIDFFile">input IDF-file</param>
        /// <param name="featureIdx">integer value with ID for generated GEN-feature</param>
        /// <param name="outputPath">path to write IPF-file when Hull-type is 0</param>
        /// <param name="logIndentLevel"></param>
        /// <returns>GEN-file with (one or more) feature(s), null if no features to convert or </returns>
        protected virtual GENFile Convert(IDFFile inputIDFFile, int featureIdx, string outputPath, int logIndentLevel)
        {
            GENFile idfGENFile = new GENFile();
            AddDATFile(idfGENFile);

            if (settings.HullType == 0)
            {
                List<Point> points = null;
                List<float> values = null;
                RetrieveIDFPoints(inputIDFFile, out points, out values);

                // Just write IPF-file, no GEN-features are added for this option
                string ipfFilename = null;
                if (settings.IsMerged)
                {
                    ipfFilename = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(settings.MergedGENFilename + ".IPF"));
                }
                else
                {
                    ipfFilename = Path.Combine(outputPath, Path.GetFileNameWithoutExtension((settings.OutputFilename != null) ? settings.OutputFilename : inputIDFFile.Filename) + ".IPF");
                }
                WriteIPFFile(ipfFilename, points, values, settings.IsMerged ? Path.GetFileName(inputIDFFile.Filename) : null);
            }
            else if (settings.HullType == 1)
            {
                List<Point> idfPoints = null;
                List<float> values = null;
                RetrieveIDFPoints(inputIDFFile, out idfPoints, out values);
                List<Point> convexHullPoints = GIS.ConvexHull.RetrieveConvexHull(idfPoints);
                if (convexHullPoints != null)
                {
                    GENPolygon genPolygon = new GENPolygon(idfGENFile, 1);
                    genPolygon.Points.AddRange(convexHullPoints);
                    genPolygon.Points.Add(new DoublePoint(convexHullPoints[0].X, convexHullPoints[0].Y));

                    idfGENFile.AddFeature(genPolygon);
                    AddDATRow(idfGENFile, genPolygon.ID, inputIDFFile.Filename, 1, values);
                }
                else if (idfPoints.Count >= 1)
                {
                    Extent idfExtent = inputIDFFile.RetrieveExtent();
                    List<Point> extentPoints = idfExtent.ToPointList();
                    extentPoints.Add(extentPoints[0]);

                    idfGENFile.AddFeature(new GENPolygon(idfGENFile, featureIdx, extentPoints));
                    AddDATRow(idfGENFile, featureIdx.ToString(), inputIDFFile.Filename, 1, values);
                }
                else
                {
                    throw new ToolException("Could not create convex hull for " + idfPoints.Count + " selected cells in IDF-file: " + inputIDFFile.Filename);
                }
            }
            else
            {
                throw new ToolException("Unknown hullType: " + settings.HullType);
            }

            return idfGENFile;
        }

        /// <summary>
        /// Add DATFile object to specified GEN-file and add column names
        /// </summary>
        /// <param name="genFile"></param>
        protected virtual void AddDATFile(GENFile genFile)
        {
            genFile.AddDATFile();
            genFile.DATFile.AddColumns(new List<string>() { "SourceFile", "Idx", "Count", "Average", "SD", "Median", "IQR", "Min", "Max" });
        }

        /// <summary>
        /// Add DAT-row with specified id and value statistics to GEN-file
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="id"></param>
        /// <param name="sourceFilename"></param>
        /// <param name="index"></param>
        /// <param name="values"></param>
        protected virtual void AddDATRow(GENFile genFile, string id, string sourceFilename, int index, List<float> values)
        {
            Statistics.Statistics stats = new Statistics.Statistics(values);
            stats.ComputeBasicStatistics(false, false, false);
            stats.ComputePercentiles();

            // Remove old row with default empty column values when it exists
            genFile.DATFile.RemoveRow(id);

            genFile.DATFile.AddRow(new DATRow(new string[] {
                id,
                Path.GetFileName(sourceFilename),
                index.ToString(),
                values.Count.ToString(),
                Math.Round(stats.Mean, StatisticsDecimalCount).ToString(SIFTool.EnglishCultureInfo),
                Math.Round(stats.SD, StatisticsDecimalCount).ToString(SIFTool.EnglishCultureInfo),
                Math.Round(stats.Median, StatisticsDecimalCount).ToString(SIFTool.EnglishCultureInfo),
                Math.Round(stats.IQR, StatisticsDecimalCount).ToString(SIFTool.EnglishCultureInfo),
                Math.Round(stats.Min, StatisticsDecimalCount).ToString(SIFTool.EnglishCultureInfo),
                Math.Round(stats.Max, StatisticsDecimalCount).ToString(SIFTool.EnglishCultureInfo) }));
        }

        /// <summary>
        /// Write IPF-file with specified points and values
        /// </summary>
        /// <param name="ipfFilename"></param>
        /// <param name="points"></param>
        /// <param name="values"></param>
        /// <param name="source"></param>
        protected void WriteIPFFile(string ipfFilename, List<Point> points, List<float> values = null, string source = null)
        {
            IPFFile ipfFile = new IPFFile();
            ipfFile.AddXYColumns();
            ipfFile.AddColumn("value");

            if (values == null)
            {
                values = new List<float>();
                for (int pointIdx = 0; pointIdx < points.Count(); pointIdx++)
                {
                    values.Add(pointIdx + 1);
                }
            }

            if (source != null)
            {
                // Add source to points
                ipfFile.AddColumn("Source");
                for (int idx = 0; idx < points.Count; idx++)
                {
                    Point point = points[idx];
                    ipfFile.Points.Add(new IPFPoint(ipfFile, point, new List<string>() { point.XString, point.YString, values[idx].ToString(SIFTool.EnglishCultureInfo), source }));
                }
            }
            else
            {
                for (int idx = 0; idx < points.Count; idx++)
                {
                    Point point = points[idx];
                    ipfFile.Points.Add(new IPFPoint(ipfFile, point, new List<string>() { point.XString, point.YString, values[idx].ToString(SIFTool.EnglishCultureInfo) }));
                }
            }
            ipfFile.WriteFile(ipfFilename);
        }

        /// <summary>
        /// Retrieve list of points for non-NoData cells in specified IDF-file
        /// </summary>
        /// <param name="currentIDFFile"></param>
        /// <param name="points"></param>
        protected void RetrieveIDFPoints(IDFFile currentIDFFile, out List<Point> points)
        {
            points = new List<Point>();
            IDFCellIterator cellIterator = new IDFCellIterator();
            cellIterator.AddIDFFile(currentIDFFile);
            cellIterator.Reset();
            float halfCellWidth = currentIDFFile.XCellsize / 2;
            while (cellIterator.IsInsideExtent())
            {
                float value = cellIterator.GetCellValue(currentIDFFile);
                if (!value.Equals(0) && !value.Equals(currentIDFFile.NoDataValue))
                {
                    float x = cellIterator.X; // +halfCellWidth;
                    float y = cellIterator.Y; // +halfCellWidth;
                    Point point = new DoublePoint(x, y);
                    points.Add(point);
                }
                cellIterator.MoveNext();
            }
        }

        /// <summary>
        /// Retrieve lists of points and values for non-NoData cells in specified IDF-file
        /// </summary>
        /// <param name="currentIDFFile"></param>
        /// <param name="points"></param>
        protected void RetrieveIDFPoints(IDFFile currentIDFFile, out List<Point> points, out List<float> values)
        {
            points = new List<Point>();
            values = new List<float>();

            IDFCellIterator cellIterator = new IDFCellIterator();
            cellIterator.AddIDFFile(currentIDFFile);
            cellIterator.Reset();
            while (cellIterator.IsInsideExtent())
            {
                float value = cellIterator.GetCellValue(currentIDFFile);
                if (!value.Equals(0) && !value.Equals(currentIDFFile.NoDataValue))
                {
                    float x = cellIterator.X; // +halfCellWidth;
                    float y = cellIterator.Y; // +halfCellWidth;
                    Point point = new DoublePoint(x, y);
                    points.Add(point);
                    values.Add(value);
                }
                cellIterator.MoveNext();
            }
        }
    }
}
