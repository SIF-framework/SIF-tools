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
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.Legends;
using Sweco.SIF.iMOD.Utils;

namespace Sweco.SIF.iMOD.GEN
{
    /// <summary>
    /// Class to read, modify and write GEN-files. See iMOD-manual for details of GEN-files: https://oss.deltares.nl/nl/web/imod/user-manual.
    /// </summary>
    public class GENFile : IMODFile
    {
        /// <summary>
        /// File extension of this iMOD-file without dot-prefix
        /// </summary>
        public override string Extension
        {
            get { return "GEN"; }
        }

        /// <summary>
        /// List of GENFeature objects in this GEN-file
        /// </summary>
        public List<GENFeature> Features { get; protected set; }

        /// <summary>
        /// The number of GENFeature objects in this GEN-file
        /// </summary>
        public int Count
        {
            get { return Features.Count; }
        }


        /// <summary>
        /// DAT-file corresponding with this GEN-file, or null if not existing
        /// </summary>
        public DATFile DATFile
        {
            get
            {
                if (datFile != null)
                {
                    return datFile;
                }
                else if ((UseLazyLoading) && HasDATFile())
                {
                    // When lazy loading is used, DAT-file is only read when referenced first
                    ReadDATFile();
                    return datFile;
                }
                else
                {
                    return null;
                }
            }
            set { datFile = value; }
        }

        /// <summary>
        /// Specify if lazy loading is used for GEN-file, meaning the DAT-file is only loaded when actually referenced
        /// </summary>
        public override bool UseLazyLoading { get; set; }

        /// <summary>
        /// Specifies if an error should be thrown if a duplicate ID is found. Note; the DAT-file has its own setting for reporting this error.
        /// </summary>
        public static bool IsErrorOnDuplicateID = false;

        private DATFile datFile;

        /// <summary>
        /// Creates an empty GEN-file
        /// </summary>
        public GENFile()
        {
            extent = null;
            fileExtent = null;
            modifiedExtent = null;
            Features = new List<GENFeature>();
        }

        /// <summary>
        /// Read specified GEN-file (binary or ASCII)
        /// </summary>
        /// <param name="fname"></param>
        /// <param name="isIDRecalculated">if true, identical ID's are renumbered for the memory instance, also in the DAT-file if present</param>
        /// <returns></returns>
        public static GENFile ReadFile(string fname, bool isIDRecalculated = false)
        {
            if (IsBinaryGENFile(fname))
            {
                throw new ToolException("Currently reading binary GEN-files is not supported in SIF-basis: " + Path.GetFileName(fname));
            }
            else
            {
                return ReadASCIGENFile(fname, isIDRecalculated);
            }
        }

        /// <summary>
        /// Checks if specified file is a binary GEN-file
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static bool IsBinaryGENFile(string filename)
        {
            int controlValue = 0;
            Stream stream = null;
            BinaryReader br = null;
            try
            {
                stream = File.OpenRead(filename);
                br = new BinaryReader(stream);

                controlValue = br.ReadInt32();   // should be 0x20 (32d) for GEN-file
            }
            catch (EndOfStreamException ex)
            {
                throw new Exception("Unexpected end of file while reading header of " + filename, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not read GEN-file " + filename, ex);
            }
            finally
            {
                if (br != null)
                {
                    br.Close();
                }
            }

            return (controlValue == 0x20);
        }

        /// <summary>
        /// Read specified GEN-file
        /// </summary>
        /// <param name="fname"></param>
        /// <param name="isIDRecalculated">if true, identical ID's are renumbered for the memory instance, also in the DAT-file if present</param>
        /// <returns></returns>
        public static GENFile ReadASCIGENFile(string fname, bool isIDRecalculated = false)
        {
            Stream stream = null;
            StreamReader sr = null;
            GENFile genFile = null;
            int lineNumber = 0;
            try
            {
                int currentStartID = 1;
                stream = File.OpenRead(fname);
                sr = new StreamReader(stream);

                genFile = new GENFile();
                genFile.Filename = fname;
                while (!sr.EndOfStream)
                {
                    string wholeLine = sr.ReadLine();
                    lineNumber++;

                    if (!wholeLine.Trim().ToUpper().Equals("END") && !wholeLine.Trim().Equals(string.Empty))
                    {
                        string id = wholeLine.Trim();

                        List<Point> points = new List<Point>();
                        string[] lineValues; // = wholeLine.Trim().Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                        while (!sr.EndOfStream && !wholeLine.Trim().ToUpper().Equals("END"))
                        {
                            wholeLine = sr.ReadLine();
                            lineNumber++;
                            if (!wholeLine.Trim().ToUpper().Equals("END"))
                            {
                                lineValues = wholeLine.Trim().Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                                if ((lineValues.Length == 2) || (lineValues.Length == 3))
                                {
                                    if (!double.TryParse(lineValues[0], NumberStyles.Float, EnglishCultureInfo, out double x))
                                    {
                                        throw new ToolException("Invalid x-coordinate at line " + lineNumber + ": " + lineValues[0]);
                                    }
                                    if (!double.TryParse(lineValues[1], NumberStyles.Float, EnglishCultureInfo, out double y))
                                    {
                                        throw new ToolException("Invalid y-coordinate at line " + lineNumber + ": " + lineValues[1]);
                                    }
                                    Point point = null;
                                    if (lineValues.Length == 3)
                                    {
                                        if (!double.TryParse(lineValues[2], NumberStyles.Float, EnglishCultureInfo, out double z))
                                        {
                                            throw new ToolException("Invalid z-coordinate at line " + lineNumber + ": " + lineValues[2]);
                                        }
                                        point = new DoublePoint3D(x, y, z);
                                    }
                                    else
                                    {
                                        point = new DoublePoint(x, y);
                                    }
                                    points.Add(point);
                                }
                                else
                                {
                                    throw new ToolException("Unexpected coordinate count at line " + lineNumber + ": " + wholeLine);
                                }
                            }
                        }
                        GENFeature addedFeature = genFile.AddFeature(points, id, isIDRecalculated, currentStartID);
                        if (isIDRecalculated)
                        {
                            currentStartID = int.Parse(addedFeature.ID) + 1;
                        }
                    }
                }
            }
            catch (ToolException ex)
            {
                throw new ToolException("Error for line " + lineNumber + " of genfile: " + Path.GetFileName(fname), ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Error for line " + lineNumber + " of genfile: " + Path.GetFileName(fname), ex);
            }
            finally
            {
                if (sr != null)
                {
                    sr.Close();
                }
                if (stream != null)
                {
                    stream.Close();
                }
            }

            genFile.UpdateExtent();

            if (!genFile.UseLazyLoading && genFile.HasDATFile())
            {
                try
                {
                    genFile.ReadDATFile();
                }
                catch (ToolException ex)
                {
                    throw ex;
                }
                catch (Exception ex)
                {
                    throw new Exception("Error while reading DAT-file: " + Path.GetFileName(Path.ChangeExtension(genFile.Filename, "DAT")), ex);
                }
            }

            return genFile;
        }

        /// <summary>
        /// Checks if this genFile has an existing DAT-file in the same directory as this GEN-file
        /// </summary>
        /// <returns></returns>
        public bool HasDATFile()
        {
            if ((Filename != null) && !Filename.Equals(string.Empty))
            {
                string datFilename = Path.Combine(Path.GetDirectoryName(Filename), Path.GetFileNameWithoutExtension(Filename) + "." + DATFile.Extension);
                return File.Exists(datFilename);
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Read corresponding DAT-file if existing, otherwise throw error. For internal use, property DATFile is intended for public use
        /// </summary>
        protected void ReadDATFile()
        {
            if (HasDATFile())
            {
                datFile = DATFile.ReadFile(this);
            }
            else
            {
                string datFilename = Path.Combine(Path.GetDirectoryName(Filename), Path.GetFileNameWithoutExtension(Filename) + "." + DATFile.Extension);
                throw new ToolException("DATFile doesn't exist: " + datFilename);
            }
        }

        /// <summary>
        /// Remove feature with specified ID from GEN-file and from DAT-file
        /// </summary>
        /// <param name="id"></param>
        public void RemoveFeature(string id)
        {
            for (int featureIdx = 0; featureIdx < Features.Count; featureIdx++)
            {
                GENFeature feature = Features[featureIdx];
                if (feature.ID.Equals(id))
                {
                    RemoveFeatureAt(featureIdx);
                    return;
                }
            }
        }

        /// <summary>
        /// Remove feature at specified index in list of features from GEN-file and from DAT-file
        /// </summary>
        /// <param name="featureIdx"></param>
        public void RemoveFeatureAt(int featureIdx)
        {
            if (featureIdx < Features.Count)
            {
                string id = Features[featureIdx].ID;
                if (DATFile != null)
                {
                    datFile.RemoveRow(id);
                }
                Features.RemoveAt(featureIdx);
            }
        }

        /// <summary>
        /// Remove all features from GEN-file
        /// </summary>
        public void ClearFeatures()
        {
            if (Features != null)
            {
                Features.Clear();
            }
            else
            {
                Features = new List<GENFeature>();
            }
            extent = null;
        }

        /// <summary>
        /// Remove all features from this GEN-file object. Identical to method ClearFeatures().
        /// </summary>
        public override void ResetValues()
        {
            ClearFeatures();
        }

        /// <summary>
        /// Add specified features to this GEN-file object
        /// </summary>
        /// <param name="genFeatures"></param>
        /// <param name="isIDRecalculated">if true, new ID's are assigned to all added features</param>
        /// <param name="startID">ID to start with when recalculated</param>
        public void AddFeatures(List<GENFeature> genFeatures, bool isIDRecalculated = false, int startID = 1)
        {
            int currentStartID = startID;
            foreach (GENFeature genFeature in genFeatures)
            {
                AddFeature(genFeature, isIDRecalculated, currentStartID);
                if (isIDRecalculated)
                {
                    // Speed up recalculation by continuing with value after previously calculated id
                    currentStartID = int.Parse(genFeature.ID) + 1;
                }
            }
        }

        /// <summary>
        /// Add feature to GEN-file, note: DAT-files, recalculation of IDs or intermediate updates of extent are costly
        /// </summary>
        /// <param name="genFeature"></param>
        /// <param name="isIDRecalculated">recalculate ID of added features and DAT-rows if current ID is empty or already existing</param>
        /// <param name="startID">ID to start with when recalculated</param>
        /// <param name="isExtentUpdated"></param>
        public void AddFeature(GENFeature genFeature, bool isIDRecalculated = false, int startID = 1, bool isExtentUpdated = true)
        {
            bool hasRecalculatedId = false;
            string recalculatedId = genFeature.ID;
            string originalIdString = genFeature.ID;

            // Recalculate if current ID is empty or current ID is already existing or currentId is not an integer value
            if (isIDRecalculated && ((genFeature.ID == null) || genFeature.ID.Equals(string.Empty) || (GetFeature(genFeature.ID) != null) || !int.TryParse(genFeature.ID, out int originalIdValue)))
            {
                int currentId = startID;
                string currentIdstring = currentId.ToString();
                while (GetFeature(currentIdstring) != null)
                {
                    currentId++;
                    currentIdstring = currentId.ToString();
                }

                hasRecalculatedId = true;
                recalculatedId = currentIdstring;
                originalIdString = genFeature.ID;
                genFeature.ID = currentIdstring;
            }
            else if (IsErrorOnDuplicateID && (GetFeature(genFeature.ID) != null))
            {
                throw new ToolException("Feature with id " + genFeature.ID + " is already existing in GEN-file " + Path.GetFileName(Filename));
            }

            if ((genFeature.GENFile != null) && (genFeature.GENFile.datFile != null))
            {
                // Retrieve DATRow of added feature if existing
                DATFile featureDATFile = genFeature.GENFile.DATFile;

                DATRow row = featureDATFile.GetRow(originalIdString);
                if (row != null)
                {
                    // The added feature has a DATRow, check if this GENFile has a DATFile, if not create it
                    if (datFile == null)
                    {
                        // Create a new DATfile with AddDATFile. This wil add DATRows for existing features, so create DATfile before adding new feature
                        datFile = new DATFile(this);
                        datFile.ColumnNames = featureDATFile.ColumnNames.ToList();
                    }

                    // Add new feature to this GENFile
                    Features.Add(genFeature);

                    // Add columns of added feature to existing columns of this genfile
                    datFile.AddColumns(featureDATFile.ColumnNames.ToList());

                    // prepare list of values, add empty string for all old and new columns 
                    List<string> valueList = new List<string>();
                    for (int colIdx = 0; colIdx < datFile.ColumnNames.Count; colIdx++)
                    {
                        valueList.Add(string.Empty);
                    }
                    // Set id to id of new feature
                    valueList[0] = row[0];

                    // Find new columnindices for values of added feature
                    for (int colIdx = 1; colIdx < row.Count; colIdx++)
                    {
                        string colName = featureDATFile.ColumnNames[colIdx];
                        int datFileColIdx = datFile.GetColIdx(colName);
                        valueList[datFileColIdx] = row[colIdx];
                    }
                    row = new DATRow(valueList);

                    if (hasRecalculatedId)
                    {
                        row[0] = recalculatedId;
                    }
                    datFile.AddRow(row);
                }
                else
                {
                    // No DATRow present, simply add new feature
                    Features.Add(genFeature);
                }
            }
            else
            {
                Features.Add(genFeature);
            }
            if (isExtentUpdated)
            {
                UpdateExtent(genFeature);
            }
            genFeature.GENFile = this;
        }

        /// <summary>
        /// Retrieve feature with specified ID
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public GENFeature GetFeature(string id)
        {
            for (int featureIdx = 0; featureIdx < Features.Count; featureIdx++)
            {
                GENFeature feature = Features[featureIdx];
                if (feature.ID.Equals(id))
                {
                    return feature;
                }
            }
            return null;
        }

        /// <summary>
        /// Add feature (as defined by a list of points and an ID) to GEN-file
        /// </summary>
        /// <param name="points"></param>
        /// <param name="id">id value</param>
        /// <param name="isIDRecalculated"></param>
        /// <param name="startID"></param>
        /// <returns></returns>
        public GENFeature AddFeature(List<Point> points, int id, bool isIDRecalculated = false, int startID = 1)
        {
            return AddFeature(points, id.ToString(), isIDRecalculated, startID);
        }

        /// <summary>
        /// Add feature (as defined by a list of points and an ID) to GEN-file
        /// </summary>
        /// <param name="points"></param>
        /// <param name="id">id string</param>
        /// <param name="isIDRecalculated"></param>
        /// <param name="startID"></param>
        /// <returns></returns>
        public GENFeature AddFeature(List<Point> points, string id, bool isIDRecalculated = false, int startID = 1)
        {
            GENFeature addedFeature = null;
            if (points.Count > 0)
            {
                if (points.Count == 1)
                {
                    // A single point was defined
                    addedFeature = new GENPoint(this, id, points[0]);
                }
                else if (points[0].Equals(points[points.Count - 1]))
                {
                    // A polygon was defined
                    addedFeature = new GENPolygon(this, id, points);
                }
                else
                {
                    // A line was defined
                    addedFeature = new GENLine(this, id, points);
                }

                AddFeature(addedFeature, isIDRecalculated, startID);
                UpdateExtent(Features[Features.Count - 1]);
            }

            return addedFeature;
        }

        /// <summary>
        /// Write GEN-file with filename stored in this object
        /// </summary>
        /// <param name="metadata"></param>
        public override void WriteFile(Metadata metadata = null)
        {
            WriteFile(this.Filename, metadata);
        }

        /// <summary>
        /// Write GEN-file with specified filename
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="metadata"></param>
        public override void WriteFile(string filename, Metadata metadata = null)
        {
            StreamWriter sw = null;
            this.Filename = filename;
            try
            {
                if ((filename == null) || filename.Equals(string.Empty))
                {
                    throw new Exception("No filename specified for GENFile.WriteFile()");
                }

                if (!Path.GetDirectoryName(filename).Equals(string.Empty) && !Directory.Exists(Path.GetDirectoryName(filename)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filename));
                }

                if (Path.GetExtension(filename).Equals(string.Empty))
                {
                    filename += ".GEN";
                }

                if (Path.GetExtension(filename).ToLower().Equals(".gen"))
                {
                    // Use uppercase file extension
                    filename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".GEN");
                }

                sw = new StreamWriter(filename);

                // Write GEN-features
                for (int featureIdx = 0; featureIdx < Count; featureIdx++)
                {
                    WriteFeature(sw, Features[featureIdx]);
                }

                sw.WriteLine("END");
            }
            catch (IOException ex)
            {
                if (ex.Message.ToLower().Contains("access") || ex.Message.ToLower().Contains("toegang"))
                {
                    throw new ToolException(Extension + "-file cannot be written, because it is being used by another process: " + filename);
                }
                else
                {
                    throw new Exception("Unexpected error while writing " + Extension + "-file: " + filename, ex);
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while writing " + Extension + "-file: " + filename, ex);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }

            if (datFile != null)
            {
                datFile.WriteFile();
            }

            if (metadata != null)
            {
                // force metadata to refer to this IDF-file
                metadata.IMODFilename = filename;
                metadata.Type = Extension;
                metadata.Resolution = "-";
                metadata.WriteMetaFile();
            }
        }

        /// <summary>
        /// Write single feature to specified StreamWriter
        /// </summary>
        /// <param name="sw"></param>
        /// <param name="feature"></param>
        private void WriteFeature(StreamWriter sw, GENFeature feature)
        {
            // Write idx and id
            if ((feature.GENFile != null) && (feature.GENFile.datFile != null))
            {
                // When a DAT-file is written too, id's should match exactly and in a DAT-file comma's without quotes are not valid
                if (feature.ID.Contains(","))
                {
                    string corrId = GENUtils.CorrectString(feature.ID);
                    sw.Write(corrId);
                }
                else
                {
                    sw.Write(feature.ID.ToString());
                }
            }
            else
            {
                sw.Write(feature.ID.ToString());
            }
            sw.WriteLine();

            // Write vertices
            for (int pointIdx = 0; pointIdx < feature.Points.Count; pointIdx++)
            {
                Point point = feature.Points[pointIdx];
                sw.Write(" " + point.XString + ", " + point.YString);
                if ((point is Point3D) && !((Point3D)point).Z.Equals(double.NaN))
                {
                    sw.WriteLine(", " + ((Point3D)point).ZString);
                }
                else
                {
                    sw.WriteLine();
                }
            }
            sw.WriteLine("END");
        }

        /// <summary>
        /// Create an iMOD Legend object for this GEN-file. TODO.
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public override Legend CreateLegend(string description)
        {
            GENLegend legend = new GENLegend();
            legend.Description = description;
            return legend;
        }

        /// <summary>
        /// Returns number of features in this GEN-file
        /// </summary>
        /// <returns></returns>
        public override long RetrieveElementCount()
        {
            return Features.Count;
        }

        /// <summary>
        /// Create (deep) copy of this GEN-file object, including GEN-features
        /// </summary>
        /// <param name="newFilename"></param>
        /// <returns>IMODFile object</returns>
        public override IMODFile Copy(string newFilename = null)
        {
            return CopyGEN(newFilename);
        }

        /// <summary>
        /// Create (deep) copy of this GEN-file object, including GEN-features
        /// </summary>
        /// <param name="newFilename"></param>
        /// <returns>GENFile object</returns>
        public GENFile CopyGEN(string newFilename = null)
        {
            GENFile copiedGENFile = new GENFile();
            for (int featureIdx = 0; featureIdx < Features.Count; featureIdx++)
            {
                GENFeature feature = Features[featureIdx];
                GENFeature featureCopy = feature.Copy();
                copiedGENFile.AddFeature(featureCopy);
            }
            copiedGENFile.Filename = newFilename;
            if (datFile != null)
            {
                copiedGENFile.datFile = datFile.Copy(copiedGENFile);
            }

            if (Legend != null)
            {
                copiedGENFile.Legend = Legend.Copy();
            }
            if (Metadata != null)
            {
                copiedGENFile.Metadata = Metadata.Copy();
            }

            return copiedGENFile;
        }


        /// <summary>
        /// Retrieve string with GEN-file type: point, line, polygon or mixed
        /// </summary>
        /// <returns></returns>
        public string GetFileType()
        {
            int pointCount = RetrieveGENPoints().Count;
            int lineCount = RetrieveGENLines().Count;
            int polygonCount = RetrieveGENPolygons().Count;
            int featureCount = pointCount + lineCount + polygonCount;

            if (pointCount == featureCount)
            {
                return "Point";
            }
            if (lineCount == featureCount)
            {
                return "Line";
            }
            if (polygonCount == featureCount)
            {
                return "Polygon";
            }

            return "Mixed";
        }

        /// <summary>
        /// Return all GENPoint features in this GEN-file
        /// </summary>
        /// <returns></returns>
        public List<GENPoint> RetrieveGENPoints()
        {
            List<GENPoint> genPoints = new List<GENPoint>();
            for (int featureIdx = 0; featureIdx < Features.Count; featureIdx++)
            {
                if (Features[featureIdx] is GENPoint)
                {
                    genPoints.Add((GENPoint)Features[featureIdx]);
                }
            }
            return genPoints;
        }

        /// <summary>
        /// Return all GENLine features in this GEN-file
        /// </summary>
        /// <returns></returns>
        public List<GENLine> RetrieveGENLines()
        {
            List<GENLine> genLines = new List<GENLine>();
            for (int featureIdx = 0; featureIdx < Features.Count; featureIdx++)
            {
                if (Features[featureIdx] is GENLine)
                {
                    genLines.Add((GENLine)Features[featureIdx]);
                }
            }
            return genLines;
        }

        /// <summary>
        /// Return all GENPolygon features in this GEN-file
        /// </summary>
        /// <returns></returns>
        public List<GENPolygon> RetrieveGENPolygons()
        {
            List<GENPolygon> genPolygons = new List<GENPolygon>();
            for (int featureIdx = 0; featureIdx < Features.Count; featureIdx++)
            {
                if (Features[featureIdx] is GENPolygon)
                {
                    genPolygons.Add((GENPolygon)Features[featureIdx]);
                }
            }
            return genPolygons;
        }

        /// <summary>
        /// Add DAT-file with IDs for all current features. Except for ID-columns no other columns are added.
        /// </summary>
        public void AddDATFile()
        {
            DATFile datFile = new DATFile(this);
            datFile.AddIDColumn();

            for (int featureIdx = 0; featureIdx < Features.Count; featureIdx++)
            {
                GENFeature genFeature = Features[featureIdx];
                List<string> values = new List<string>();
                values.Add(genFeature.ID);
                datFile.AddRow(new DATRow(values));
            }

            this.datFile = datFile;
        }

        /// <summary>
        /// Update extent of bounding box around features in this GEN-file with specified feature
        /// </summary>
        /// <param name="feature"></param>
        protected void UpdateExtent(GENFeature feature)
        {
            for (int pointIdx = 0; pointIdx < feature.Points.Count; pointIdx++)
            {
                Point point = feature.Points[pointIdx];
                UpdateExtent(point);
            }
        }

        /// <summary>
        /// Update extent of bounding box around features in this GEN-file with specified point
        /// </summary>
        /// <param name="point"></param>
        protected void UpdateExtent(Point point)
        {
            if (extent == null)
            {
                extent = new Extent((float)point.X, (float)point.Y, (float)point.X, (float)point.Y);
            }
            else
            {
                if (point.X < extent.llx)
                {
                    extent.llx = (float)point.X;
                }
                if (point.Y < extent.lly)
                {
                    extent.lly = (float)point.Y;
                }
                if (point.X > extent.urx)
                {
                    extent.urx = (float)point.X;
                }
                if (point.Y > extent.ury)
                {
                    extent.ury = (float)point.Y;
                }
            }
        }

        /// <summary>
        /// Recalcalate extent of bounding box around features in this GEN-file
        /// </summary>
        protected void UpdateExtent()
        {
            extent = null;
            for (int featureIdx = 0; featureIdx < Features.Count; featureIdx++)
            {
                GENFeature feature = Features[featureIdx];
                UpdateExtent(feature);
            }
        }

        /// <summary>
        /// Release memory (e.g. from DAT-files) when lazy loading is defined
        /// </summary>
        /// <param name="isMemoryCollected"></param>
        public override void ReleaseMemory(bool isMemoryCollected = true)
        {
            if (UseLazyLoading)
            {
                // Note: DAT-file is not checked for modifications. Saving modifications is a responsibility of the caller.
                datFile = null;
            }
        }

        /// <summary>
        /// Determine equality up to the level of the contents
        /// </summary>
        /// <param name="otherIMODFile"></param>
        /// <param name="comparedExtent"></param>
        /// <param name="isNoDataCompared"></param>
        /// <param name="isContentComparisonForced"></param>
        /// <returns></returns>
        public override bool HasEqualContent(IMODFile otherIMODFile, Extent comparedExtent, bool isNoDataCompared, bool isContentComparisonForced = false)
        {
            throw new NotImplementedException();
        }
    }
}
