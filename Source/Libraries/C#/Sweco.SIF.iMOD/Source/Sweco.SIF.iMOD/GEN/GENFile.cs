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
using System.Text.RegularExpressions;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.Legends;
using Sweco.SIF.iMOD.Utils;
using Sweco.SIF.iMOD.Values;

namespace Sweco.SIF.iMOD.GEN
{
    /// <summary>
    /// Format in which a GEN-file can be stored
    /// </summary>
    public enum GENFileFormat
    {
        /// <summary>
        /// Undefined format
        /// </summary>
        Undefined,

        /// <summary>
        /// ASC-format: with a seperate textual GEN- and DAT-file for feature- and fielddata
        /// </summary>
        ASC,

        /// <summary>
        /// Binary-format: a compressed format, with feature and field data in the same file
        /// </summary>
        BIN
    }

    /// <summary>
    /// Class to read, modify and write GEN-files. See iMOD-manual for details of GEN-files: https://oss.deltares.nl/nl/web/imod/user-manual.
    /// </summary>
    public class GENFile : IMODFile
    {
        private const int MaxStringLength = 10000000; // 1000000000;

        /// <summary>
        /// Specifies if an error should be thrown if a duplicate ID is found. Note; the DAT-file has its own setting for reporting this error.
        /// </summary>
        public static bool IsErrorOnDuplicateID = false;

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
        /// File format of the source GEN-file when it was read and/or format that is used for writing
        /// </summary>
        public GENFileFormat FileFormat { get; set; }

        /// <summary>
        /// Reference to DAT-file with columns and column values of features in this GEN-file
        /// </summary>
        protected DATFile datFile;

        /// <summary>
        /// Creates an empty GEN-file
        /// </summary>
        public GENFile()
        {
            Filename = null;
            FileFormat = GENFileFormat.Undefined;
            UseLazyLoading = false;
            extent = null;
            fileExtent = null;
            modifiedExtent = null;
            Features = new List<GENFeature>();
        }

        /// <summary>
        /// Creates an empty GEN-file
        /// </summary>
        /// <param name="capacity"></param>
        public GENFile(int capacity)
        {
            Filename = null;
            FileFormat = GENFileFormat.Undefined;
            UseLazyLoading = false;
            extent = null;
            fileExtent = null;
            modifiedExtent = null;
            Features = new List<GENFeature>(capacity);
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
                return ReadBINGENFile(fname, isIDRecalculated);
            }
            else
            {
                return ReadASCIGENFile(fname, isIDRecalculated);
            }
        }

        /// <summary>
        /// Copies all properties from specified other GENFile object, including column names, but excluding actual features, to this GENFile object.
        /// Note: Filename and Log properties are not copied, this is object specific.
        /// </summary>
        public void CopyProperties(GENFile otherGENFile)
        {
            // Filename = otherGENFile.Filename;
            NoDataValue = otherGENFile.NoDataValue;
            UseLazyLoading = otherGENFile.UseLazyLoading;
            FileFormat = otherGENFile.FileFormat;
            Legend = otherGENFile.Legend;

            fileExtent = otherGENFile.fileExtent;
            // do not copy other extent properties, since these depend on actual points
            // do not copy log properties, this is object specific

            if (otherGENFile.HasDATFile())
            {
                DATFile otherDATFile = otherGENFile.DATFile;

                if (!HasDATFile())
                {
                    AddDATFile();
                }

                DATFile.AddColumns(otherDATFile.ColumnNames);
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
            GENFile genFile = null;
            int lineNumber = 0;
            try
            {
                int currentStartID = 1;

                // Read all lines in GEN-file
                string[] lines = null;
                try
                {
                    lines = File.ReadAllLines(fname);
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not read GEN-file: " + fname, ex);
                }

                genFile = new GENFile();
                genFile.Filename = fname;
                genFile.FileFormat = GENFileFormat.ASC;

                string wholeLine;
                while (lineNumber < lines.Length)
                {
                    wholeLine = lines[lineNumber++].Trim();

                    if (!wholeLine.ToUpper().Equals("END") && !wholeLine.Equals(string.Empty))
                    {
                        string id = wholeLine;
                        string[] lineParts = CommonUtils.SplitQuoted(id, ',', '"', true, true);
                        if (lineParts.Length >= 3)
                        {
                            // line contains a GEN-point definition, read until END-line is found
                            while ((wholeLine != null) && !wholeLine.ToUpper().Equals("END"))
                            {
                                lineParts = CommonUtils.SplitQuoted(wholeLine, ',', '"', true, true);
                                if (lineParts.Length < 3)
                                {
                                    throw new Exception("Invalid line in GEN-file, GEN-point definition expected (IDi, X, Y[, Z]): " + wholeLine);
                                }
                                id = lineParts[0];
                                if (!double.TryParse(lineParts[1], NumberStyles.Float, EnglishCultureInfo, out double x))
                                {
                                    throw new ToolException("Invalid x-coordinate at line " + lineNumber + ": " + lineParts[1]);
                                }
                                if (!double.TryParse(lineParts[2], NumberStyles.Float, EnglishCultureInfo, out double y))
                                {
                                    throw new ToolException("Invalid y-coordinate at line " + lineNumber + ": " + lineParts[2]);
                                }
                                Point point = null;
                                if (lineParts.Length == 4)
                                {
                                    if (!double.TryParse(lineParts[3], NumberStyles.Float, EnglishCultureInfo, out double z))
                                    {
                                        throw new ToolException("Invalid z-coordinate at line " + lineNumber + ": " + lineParts[3]);
                                    }
                                    point = new DoublePoint3D(x, y, z);
                                }
                                else
                                {
                                    point = new DoublePoint(x, y);
                                }

                                genFile.AddFeature(new GENPoint(genFile, id, point), isIDRecalculated, currentStartID);
                                if (isIDRecalculated)
                                {
                                    currentStartID = int.Parse(id) + 1;
                                }

                                wholeLine = (lineNumber < lines.Length) ? lines[lineNumber++].Trim() : null;
                            }
                        }
                        else
                        {
                            List<Point> points = new List<Point>();

                            string[] lineValues; // = wholeLine.Trim().Split(new char[] { ' ', '\t', ',' }, StringSplitOptions.RemoveEmptyEntries);
                            wholeLine = (lineNumber < lines.Length) ? lines[lineNumber++].Trim() : null;
                            while ((wholeLine != null) && !wholeLine.ToUpper().Equals("END"))
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

                                wholeLine = (lineNumber < lines.Length) ? lines[lineNumber++].Trim() : null;
                            }


                            GENFeature addedFeature = genFile.AddFeature(points, id, isIDRecalculated, currentStartID);
                            if (isIDRecalculated)
                            {
                                currentStartID = int.Parse(addedFeature.ID) + 1;
                            }
                        }
                    }
                    else if (lineNumber != lines.Length)
                    {
                        throw new Exception("Unexpected data at line " + lineNumber + " in GEN-file: '" + wholeLine + "'");
                        // lineNumber = lines.Length;
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
        /// Read specified binary GEN-file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="isIDRecalculated">if true, identical ID's are renumbered for the memory instance, also in the DAT-file if present</param>
        /// <returns></returns>
        protected static GENFile ReadBINGENFile(string filename, bool isIDRecalculated = false)
        {
            if (!File.Exists(filename))
            {
                throw new ToolException("GEN-file does not exist: " + filename);
            }

            if (!Path.GetExtension(filename).ToLower().Equals(".gen"))
            {
                throw new Exception("Unsupported extension for GEN-file: " + filename);
            }

            GENFile genFile = null;

            genFile = new GENFile();
            genFile.UseLazyLoading = true;
            genFile.Filename = filename;
            genFile.FileFormat = GENFileFormat.BIN;

            DATFile datfile = new DATFile(genFile);
            genFile.DATFile = datfile;

            Stream stream = null;
            BinaryReader br = null;
            try
            {
                stream = File.OpenRead(filename);
                br = new BinaryReader(stream);

                // Read Definitions
                genFile.ReadBINGENFile(br);

                //if (!genFile.useLazyLoading)
                //{
                //    // When lazy loading is not used, load values immediately
                //    ReadFeatures(br);
                //}
            }
            catch (EndOfStreamException ex)
            {
                throw new Exception("Unexpected end of file while reading header of " + filename, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not read binary GEN-file " + filename, ex);
            }
            finally
            {
                if (br != null)
                {
                    br.Close();
                }
            }

            return genFile;
        }

        /// <summary>
        /// Read data from GEN-file into a BinaryReader object
        /// </summary>
        /// <param name="br"></param>
        protected void ReadBINGENFile(BinaryReader br)
        {
            int controlValue = br.ReadInt32();   // is always 0x20 (32d), ignore
            if (controlValue != 0x20)
            {
                throw new Exception("File is not a binary GEN-file: " + this.Filename);
            }

            // Read dimensions
            float llx = (float)br.ReadDouble();
            float lly = (float)br.ReadDouble();
            float urx = (float)br.ReadDouble();
            float ury = (float)br.ReadDouble();
            this.extent = new Extent(llx, lly, urx, ury);

            // Skip control values
            controlValue = br.ReadInt32();
            controlValue = br.ReadInt32();

            long NPOL = br.ReadInt32();
            long MAXCOL = br.ReadInt32();

            List<int> LWIDTH = null;
            List<string> columnNames = null;
            if (MAXCOL > 0)
            {
                // Skip control values
                controlValue = br.ReadInt32();
                controlValue = br.ReadInt32();

                LWIDTH = new List<int>();
                for (int idx = 0; idx < MAXCOL; idx++)
                {
                    LWIDTH.Add(br.ReadInt32());
                }

                // Skip control values
                controlValue = br.ReadInt32();
                controlValue = br.ReadInt32();

                columnNames = new List<string>();
                for (int idx = 0; idx < MAXCOL; idx++)
                {
                    string columnName = new string(br.ReadChars(11));
                    int nulIdx = columnName.IndexOf("\0");
                    if (nulIdx > 0)
                    {
                        columnName = columnName.Substring(0, nulIdx);
                    }
                    columnNames.Add(columnName.Trim());
                }

                // Add column names and add sequence number if name is present more than once
                DATFile.AddColumns(columnNames, null, false);
            }
            else
            {
                // Add default ID-column
                DATFile.AddIDColumn();
            }

            int pointCount = 0;
            int ITYPE = 0;
            int currentDefaultId = 0;
            for (int featureIdx = 0; featureIdx < NPOL; featureIdx++)
            {
                List<Point> pointList = new List<Point>();

                // Skip control values
                controlValue = br.ReadInt32();
                controlValue = br.ReadInt32();

                pointCount = br.ReadInt32();
                ITYPE = br.ReadInt32();

                List<string> columnValues = new List<string>();
                if (MAXCOL > 0)
                {
                    // Skip control values
                    controlValue = br.ReadInt32();
                    controlValue = br.ReadInt32();

                    for (int idx = 0; idx < MAXCOL; idx++)
                    {
                        string columnValue = new string(br.ReadChars(LWIDTH[idx]));
                        columnValues.Add(columnValue.Trim());
                    }
                }

                // Skip control values
                controlValue = br.ReadInt32();
                controlValue = br.ReadInt32();

                // Skip feature extent
                llx = (float)br.ReadDouble();
                urx = (float)br.ReadDouble();
                lly = (float)br.ReadDouble();
                ury = (float)br.ReadDouble();
                Extent featureExtent = new Extent(llx, lly, urx, ury);

                // Skip control values
                controlValue = br.ReadInt32();
                controlValue = br.ReadInt32();

                // Read feature points
                for (int pointIdx = 0; pointIdx < pointCount; pointIdx++)
                {
                    double x = br.ReadDouble();
                    double y = br.ReadDouble();
                    Point point = new FloatPoint((float)x, (float)y);
                    pointList.Add(point);
                }
                if (ITYPE == 1025) // polygons: 1025; lines (1028); rectangle (1026); circle (1025); points (1027)
                {
                    // Add first point again to close polygon
                    pointList.Add(pointList[0]);
                }
                else if (ITYPE == 1026)
                {
                    // Convert Rectangle to Polygon: rectangle is not suited for storing islands
                    Point ul = pointList[0];
                    Point lr = pointList[1];
                    Point ur = new DoublePoint(lr.X, ul.Y);
                    Point ll = new DoublePoint(ul.X, lr.Y);

                    pointList[1] = ur;
                    pointList.Add(lr);
                    pointList.Add(ll);
                    pointList.Add(ul);
                }

                string id = (columnValues.Count > 0) ? columnValues[0] : string.Empty;
                GENFeature genFeature = null;
                if (id.Equals(string.Empty))
                {
                    genFeature = AddFeature(pointList, ++currentDefaultId, true, currentDefaultId);
                    if (columnValues.Count == 0)
                    {
                        columnValues.Add(currentDefaultId.ToString());
                    }
                    else
                    {
                        columnValues[0] = currentDefaultId.ToString();
                    }
                }
                else
                {
                    genFeature = AddFeature(pointList, id, false);
                }

                // Replace default DATRow-object with specified column values
                DATFile.RemoveRow(id);
                DATFile.AddRow(new DATRow(columnValues));
            }
        }

        /// <summary>
        /// Checks if this genFile object has a corresponding DATFile object in memory or has an existing DAT-file in the same directory as this GEN-file
        /// </summary>
        /// <returns></returns>
        public bool HasDATFile()
        {
            if (DATFile != null)
            {
                return true;
            }
            else if ((Filename != null) && !Filename.Equals(string.Empty))
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
                if (HasDATFile())
                {
                    DATFile.ClearRows();
                }
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
        /// <param name="points">feature points; ensure polygons are closed: equal first and last point</param>
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
        /// <param name="points">feature points; ensure polygons are closed: equal first and last point</param>
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
                else if (points[0].Equals(points[points.Count - 1]) && (points.Count > 3))
                {
                    // A polygon was defined: last point equals first point and at least 3 points (excluding last point, which equals first point)
                    addedFeature = new GENPolygon(this, id, points);
                }
                else
                {
                    // A line was defined
                    addedFeature = new GENLine(this, id, points);
                }

                AddFeature(addedFeature, isIDRecalculated, startID);
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
            WriteFile(filename, metadata, null);
        }

        /// <summary>
        /// Write GEN-file with specified filename and write intermediate logmessages based on size of GEN-file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="metadata">optional metadata to write MET-file</param>
        /// <param name="fileFormat">ASC, binary or undefined (to keep current format)</param>
        /// <param name="log">if specified, intermediate logmessages are added</param>
        /// <param name="logIndentLevel"></param>
        public void WriteFile(string filename, Metadata metadata, GENFileFormat fileFormat, Log log, int logIndentLevel = 0)
        {
            FileFormat = fileFormat;
            WriteFile(filename, metadata, log, logIndentLevel);
        }

        /// <summary>
        /// Write GEN-file with specified filename and write intermediate logmessages based on size of GEN-file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="metadata">optional metadata to write MET-file</param>
        /// <param name="log">if specified, intermediate logmessages are added</param>
        /// <param name="logIndentLevel"></param>
        public void WriteFile(string filename, Metadata metadata, Log log, int logIndentLevel = 0)
        {
            switch (FileFormat)
            {
                case GENFileFormat.Undefined:
                case GENFileFormat.ASC:
                    WriteASCFile(filename, metadata, log, logIndentLevel);
                    break;
                case GENFileFormat.BIN:
                    WriteBINGENFile(filename, metadata, log, logIndentLevel);
                    break;
                default:
                    throw new Exception("Unknown GEN-format: " + FileFormat);
            }
        }

        /// <summary>
        /// Write ASC GEN-file with specified filename and write intermediate logmessages based on size of GEN-file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="metadata">optional metadata to write MET-file</param>
        /// <param name="log">if specified, intermediate logmessages are added</param>
        /// <param name="logIndentLevel"></param>
        public void WriteASCFile(string filename, Metadata metadata, Log log, int logIndentLevel = 0)
        {
            if (log != null)
            {
                log.AddInfo("Writing GEN-file '" + Path.GetFileName(filename) + "'...", logIndentLevel);
            }

            StreamWriter sw = null;
            this.Filename = filename;
            StringBuilder fileStringBuilder = new StringBuilder();
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

                // Calculate number of points between 5% logmessages, use multiple of 50
                int logSnapPointMessageFrequency = (log != null) ? logSnapPointMessageFrequency = Log.GetLogMessageFrequency(this.Count, 5) : 0;

                // Write GEN-features
                sw = new StreamWriter(filename, false, Encoding);
                List<GENPoint> currentGENPoints = new List<GENPoint>();
                for (int featureIdx = 0; featureIdx < Count; featureIdx++)
                {
                    if ((log != null) && (featureIdx % logSnapPointMessageFrequency == 0))
                    {
                        log.AddInfo("Writing features " + (featureIdx + 1) + "-" + (int)Math.Min(this.Count, (featureIdx + logSnapPointMessageFrequency)) + " of " + this.Count + " ...", logIndentLevel + 1);
                    }
                    GENFeature genFeature = Features[featureIdx];

                    if (genFeature is GENPoint)
                    {
                        // Combine consecutive GEN-points in one entry
                        currentGENPoints.Add((GENPoint)genFeature);
                    }
                    else
                    {
                        // Add pending GEN-points
                        AppendGENPoints(sw, fileStringBuilder, currentGENPoints);
                        currentGENPoints.Clear();

                        AppendGENFeature(sw, fileStringBuilder, genFeature);
                    }
                }

                // Add remaining, pending GEN-points
                AppendGENPoints(sw, fileStringBuilder, currentGENPoints);
                currentGENPoints.Clear();

                fileStringBuilder.AppendLine("END");

                sw.Write(fileStringBuilder.ToString());
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
                datFile.WriteFile(log, logIndentLevel);
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
        /// Write binary GEN-file with specified filename and write intermediate logmessages based on size of GEN-file
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="metadata">optional metadata to write MET-file</param>
        /// <param name="log">if specified, intermediate logmessages are added</param>
        /// <param name="logIndentLevel"></param>
        public void WriteBINGENFile(string filename, Metadata metadata = null, Log log = null, int logIndentLevel = 0)
        {
            if (log != null)
            {
                log.AddInfo("Writing binary GEN-file '" + Path.GetFileName(filename) + "'...", logIndentLevel);
            }

            if (!Path.GetExtension(filename).ToLower().Equals(".gen"))
            {
                throw new Exception("Extension for writing GEN-file should be .GEN: " + Path.GetFileName(filename));
            }
            Stream stream = null;
            BinaryWriter bw = null;
            try
            {
                List<int> columnWidths = null;
                DATFile datFile = this.DATFile;
                if (datFile == null)
                {
                    // Create dummy DAT-file with only ID's
                    datFile = new DATFile(this);
                    DATFile.AddIDColumn();
                    for (int featureIdx = 0; featureIdx < Features.Count; featureIdx++)
                    {
                        GENFeature feature = Features[featureIdx];
                        DATRow datRow = new DATRow(new string[] { feature.ID });
                        datFile.AddRow(datRow);
                    }
                }

                stream = File.OpenWrite(filename);
                bw = new BinaryWriter(stream);

                bw.Write((Int32)0x20);   // Write control value, which is always 0x20 (32d) for binary GEN-files; this defines number of bytes (?)

                // Write extent dimensions
                bw.Write((double)Extent.llx);
                bw.Write((double)Extent.lly);
                bw.Write((double)Extent.urx);
                bw.Write((double)Extent.ury);

                // Write control values
                bw.Write((Int32)0x20);
                bw.Write((Int32)0x08); // Write 

                bw.Write((Int32)this.Count);
                bw.Write((datFile != null) ? (Int32)datFile.ColumnNames.Count : (Int32)0);

                // Write control value
                bw.Write((Int32)0x08);

                Int32 MAXCOL = 0;
                Int32 maxColWidth = (Int32)11; // maximum column length seems to be 11 for binary GEN-files
                int columnWidthSum = 0;
                if ((datFile != null) && (datFile.ColumnNames.Count > 0))
                {
                    MAXCOL = (Int32)datFile.ColumnNames.Count;
                    columnWidths = datFile.GetMaxColumnWidths();
                    if (columnWidths[0] < 11)
                    {
                        columnWidths[0] = 11;
                    }

                    // Write control value
                    bw.Write((Int32)(0x04 * MAXCOL));

                    // Write widths columns
                    for (int idx = 0; idx < datFile.ColumnNames.Count; idx++)
                    {
                        bw.Write((Int32)columnWidths[idx]);
                        columnWidthSum += columnWidths[idx];
                    }

                    // Write control values
                    bw.Write((Int32)(0x04 * MAXCOL));
                    bw.Write(MAXCOL * maxColWidth);

                    // Write column names with fixed length of 11 characters
                    for (int idx = 0; idx < datFile.ColumnNames.Count; idx++)
                    {
                        // Limit/extend column name to 11 characters
                        string columnname = datFile.ColumnNames[idx].PadRight(11).Substring(0, 11);
                        for (int idx2 = 0; idx2 < 11; idx2++)
                        {
                            bw.Write(columnname[idx2]);
                        }
                    }

                    // Write control value
                    bw.Write(MAXCOL * maxColWidth);
                }

                for (int featureIdx = 0; featureIdx < Features.Count; featureIdx++)
                {
                    GENFeature feature = Features[featureIdx];
                    int ITYPE = GENUtils.GetITYPE(feature);

                    // Write control values
                    bw.Write((Int32)0x08);

                    // Write point count
                    int pointCount = feature.Points.Count;
                    if (ITYPE == 1025)
                    {
                        // For polygons remove last (extra) point
                        pointCount--;
                    }
                    bw.Write((Int32)pointCount);
                    bw.Write((Int32)ITYPE);

                    // Write control values
                    bw.Write((Int32)0x08);
                    bw.Write((Int32)columnWidthSum);

                    if ((datFile != null) && (datFile.ColumnNames.Count > 0))
                    {
                        DATRow datRow = datFile.GetRow(feature.ID);

                        for (int idx = 0; idx < datFile.ColumnNames.Count; idx++)
                        {
                            string columnValue = datRow[idx];
                            columnValue = columnValue.PadRight(columnWidths[idx]);
                            for (int charIdx = 0; charIdx < columnValue.Length; charIdx++)
                            {
                                bw.Write(columnValue[charIdx]);
                            }
                        }
                    }

                    // Write control values
                    bw.Write((Int32)columnWidthSum);
                    bw.Write((Int32)0x20);

                    // Write feature extent
                    Extent featureExtent = feature.RetrieveExtent();
                    bw.Write((double)featureExtent.llx);
                    bw.Write((double)featureExtent.lly);
                    bw.Write((double)featureExtent.urx);
                    bw.Write((double)featureExtent.ury);

                    // Write control value
                    bw.Write((Int32)0x20);

                    // Write control value
                    bw.Write((Int32)(16 * pointCount));

                    // Write feature points
                    for (int pointIdx = 0; pointIdx < pointCount; pointIdx++)
                    {
                        Point point = feature.Points[pointIdx];
                        bw.Write((double)point.X);
                        bw.Write((double)point.Y);
                    }

                    // Write control value
                    bw.Write((Int32)(16 * pointCount));
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not write binary GEN-file " + filename, ex);
            }
            finally
            {
                if (bw != null)
                {
                    bw.Close();
                }
            }
        }

        private void AppendGENPoints(StreamWriter sw, StringBuilder fileStringBuilder, List<GENPoint> genPoints)
        {
            if (genPoints.Count > 0)
            {
                StringBuilder pointsSB = CreateStringBuilder(genPoints);

                if ((fileStringBuilder.Length + pointsSB.Length) > MaxStringLength)
                {
                    sw.Write(fileStringBuilder.ToString());
                    fileStringBuilder.Clear();
                }
                fileStringBuilder.Append(pointsSB);
            }
        }

        protected void AppendGENFeature(StreamWriter sw, StringBuilder fileStringBuilder, GENFeature genFeature)
        {
            StringBuilder featureSB = CreateStringBuilder(genFeature);

            if ((fileStringBuilder.Length + featureSB.Length) > MaxStringLength)
            {
                sw.Write(fileStringBuilder.ToString());
                fileStringBuilder.Clear();
            }
            fileStringBuilder.Append(featureSB);
        }

        /// <summary>
        /// Create StringBuilder object for specified list of GEN-points
        /// </summary>
        /// <param name="genPoints"></param>
        private StringBuilder CreateStringBuilder(List<GENPoint> genPoints)
        {
            StringBuilder stringBuilder = new StringBuilder();

            for (int idx = 0; idx < genPoints.Count; idx++)
            {
                GENPoint genPoint = genPoints[idx];

                // Retrieve and (if necessary) corect id
                string id = genPoint.ID;
                if ((genPoint.GENFile != null) && (genPoint.GENFile.datFile != null))
                {
                    // When a DAT-file is written too, id's should match exactly and in a DAT-file comma's without quotes are not valid
                    if (genPoint.ID.Contains(","))
                    {
                        id = GENUtils.CorrectString(genPoint.ID);
                    }
                }

                stringBuilder.Append(id + ", ");

                Point point = genPoint.Point;
                stringBuilder.Append(point.XString + ", " + point.YString);
                if ((point is Point3D) && !((Point3D)point).Z.Equals(double.NaN))
                {
                    stringBuilder.AppendLine(", " + ((Point3D)point).ZString);
                }
                else
                {
                    stringBuilder.AppendLine();
                }
            }

            stringBuilder.AppendLine("END");

            return stringBuilder;
        }

        /// <summary>
        /// Create StringBuilder object for specified single feature
        /// </summary>
        /// <param name="feature"></param>
        private StringBuilder CreateStringBuilder(GENFeature feature)
        {
            StringBuilder stringBuilder = new StringBuilder();

            // Write idx and id
            if ((feature.GENFile != null) && (feature.GENFile.datFile != null))
            {
                // When a DAT-file is written too, id's should match exactly and in a DAT-file comma's without quotes are not valid
                if (feature.ID.Contains(","))
                {
                    string corrId = GENUtils.CorrectString(feature.ID);
                    stringBuilder.Append(corrId);
                }
                else
                {
                    stringBuilder.Append(feature.ID.ToString());
                }
            }
            else
            {
                stringBuilder.Append(feature.ID.ToString());
            }
            stringBuilder.AppendLine();

            // Write vertices
            for (int pointIdx = 0; pointIdx < feature.Points.Count; pointIdx++)
            {
                Point point = feature.Points[pointIdx];
                stringBuilder.Append(" " + point.XString + ", " + point.YString);
                if ((point is Point3D) && !((Point3D)point).Z.Equals(double.NaN))
                {
                    stringBuilder.AppendLine(", " + ((Point3D)point).ZString);
                }
                else
                {
                    stringBuilder.AppendLine();
                }
            }
            stringBuilder.AppendLine("END");

            return stringBuilder;
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
            GENFile copiedGENFile = new GENFile(Features.Count);
            for (int featureIdx = 0; featureIdx < Features.Count; featureIdx++)
            {
                GENFeature feature = Features[featureIdx];
                GENFeature featureCopy = feature.Copy();
                copiedGENFile.AddFeature(featureCopy);
            }
            copiedGENFile.Filename = newFilename;
            copiedGENFile.FileFormat = FileFormat;

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
        /// Add DAT-file with IDs for all current features. Except for ID-column no other column is added.
        /// </summary>
        /// <param name="capacity">predefined capacity or leave -1 to ignore</param>
        public void AddDATFile(int capacity = -1)
        {
            DATFile datFile = (capacity >= 0) ? new DATFile(this, capacity) : new DATFile(this);
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
        /// Determine equality up to the level of the filename
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public virtual bool Equals(GENFile other)
        {
            return base.Equals(other);
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
            if (!(otherIMODFile is GENFile))
            {
                return false;
            }

            GENFile otherGENFile = (GENFile)otherIMODFile;
            if (!isContentComparisonForced && this.Equals(otherGENFile))
            {
                return true;
            }

            // Determine comparison extent
            if (!((this.extent != null) && (otherGENFile.Extent != null)))
            {
                // One or both files have a null extent
                if ((this.extent == null) && (otherGENFile.Extent == null))
                {
                    // both files have no content, so they are considered equal
                    return true;
                }
                else
                {
                    // Only one file has some content, so unequal
                    return false;
                }
            }
            else
            {
                if (comparedExtent == null)
                {
                    if (!this.extent.Equals(otherGENFile.Extent))
                    {
                        // Both files have different extents so different content
                        return false;
                    }

                    // Both files have equal extent, continue with actual comparison below
                }
                else
                {
                    // Clip both IDF-files to compared extent
                    Extent comparedExtent1 = this.Extent.Clip(comparedExtent);
                    Extent comparedExtent2 = otherGENFile.Extent.Clip(comparedExtent);
                    if (!comparedExtent1.Equals(comparedExtent2))
                    {
                        // Both files differ in content, even within compared extent
                        return false;
                    }
                    comparedExtent = comparedExtent1;

                    if (!comparedExtent.IsValidExtent())
                    {
                        // The comparison extent has no overlap with the other extents, so within the comparison extent the files are actually equal
                        return true;
                    }

                    // A valid comparison extent remains, continue with actual comparison
                }
            }

            // Compare segments of both files
            GENFile diffGENFile = this.CreateGENDifferenceFile((GENFile)otherIMODFile, string.Empty, 0, comparedExtent);
            return (diffGENFile == null);
        }

        /// <summary>
        /// Clip all features in this GEN-file to specified extent
        /// </summary>
        /// <param name="clipExtent"></param>
        /// <returns>IMODFile object</returns>
        public override IMODFile Clip(Extent clipExtent)
        {
            return ClipGEN(clipExtent);
        }

        /// <summary>
        /// Clip all features in this GEN-file to specified extent
        /// </summary>
        /// <param name="clipExtent"></param>
        /// <param name="isInvertedClip">if true, the part within the extent is clipped away</param>
        /// <returns>GENFile object</returns>
        public GENFile ClipGEN(Extent clipExtent, bool isInvertedClip = false)
        {
            if (clipExtent == null)
            {
                clipExtent = this.extent;
            }
            GENFile clippedGENFile = new GENFile(Features.Count);
            clippedGENFile.Filename = Filename;
            int sourceIDColIdx = -1;
            DATFile clippedDATFile = null;
            bool hasDATFile = this.HasDATFile() || (this.datFile != null);
            if (hasDATFile)
            {
                clippedDATFile = new DATFile(clippedGENFile);
                clippedGENFile.DATFile = clippedDATFile;
                clippedGENFile.DATFile.AddColumns(this.DATFile.ColumnNames);
                string sourceIDColumnName = clippedGENFile.DATFile.GetUniqueColumnName(DATFile.SourceIDColumnName);
                sourceIDColIdx = clippedGENFile.DATFile.AddColumn(sourceIDColumnName);
            }

            int currentClipID = 0;
            for (int genFeatureIdx = 0; genFeatureIdx < Features.Count; genFeatureIdx++)
            {
                GENFeature genFeature = Features[genFeatureIdx];
                List<GENFeature> clippedFeatures = null;
                if (genFeature is GENPoint)
                {
                    clippedFeatures = ((GENPoint)genFeature).Clip(clipExtent);
                }
                else if (genFeature is GENLine)
                {
                    clippedFeatures = ((GENLine)genFeature).Clip(clipExtent);
                }
                else if (genFeature is GENPolygon)
                {
                    clippedFeatures = ((GENPolygon)genFeature).Clip(clipExtent);
                }
                // Simplified version of: clippedGENFile.AddFeatures(clippedFeatures, true, clippedGENFile.Features.Count + 1);
                for (int clippedFeatureIdx = 0; clippedFeatureIdx < clippedFeatures.Count; clippedFeatureIdx++)
                {
                    // Simplified version of: AddFeature(genFeature, true, currentClipID++);
                    GENFeature clippedGENFeature = clippedFeatures[clippedFeatureIdx];
                    // string originalIdString = clippedGENFeature.ID;
                    clippedGENFeature.ID = (++currentClipID).ToString();

                    // Add new feature to this GENFile
                    clippedGENFile.Features.Add(clippedGENFeature);

                    if (hasDATFile)
                    {
                        DATRow datRow = clippedGENFeature.GENFile.DATFile.Rows.ElementAt(clippedFeatureIdx);
                        datRow[0] = clippedGENFeature.ID;
                        datRow[sourceIDColIdx] = genFeature.ID;
                        clippedGENFile.DATFile.AddRow(datRow);
                    }

                    clippedGENFile.UpdateExtent(clippedGENFeature);
                }
            }

            clippedGENFile.fileExtent = (fileExtent != null) ? fileExtent.Copy() : null;
            clippedGENFile.modifiedExtent = (clipExtent != null) ? clipExtent.Copy() : null;
            clippedGENFile.extent = (clipExtent != null) ? clipExtent.Copy() : null;

            if (Legend != null)
            {
                clippedGENFile.Legend = Legend.Copy();
            }

            return clippedGENFile;
        }

        /// <summary>
        /// Retrieves a GENFile object with all features in specified GENFile for which the expression evaluates to true
        /// </summary>
        /// <param name="colIdx"></param>
        /// <param name="valueOperator"></param>
        /// <param name="valueString">string value or a string expression with {colnr}-references, which is only valid for (un)equal-operator</param>
        /// <param name="srcPointIndices">optional (empty, non-null) list to store indices to selected features in source GEN-file</param>
        /// <param name="useRegExp">specify true to use regular expressions for equal or unequal operator on strings</param>
        /// <returns></returns>
        public GENFile Select(int colIdx, ValueOperator valueOperator, string valueString, List<int> srcPointIndices = null, bool useRegExp = false)
        {
            if (!HasDATFile())
            {
                throw new Exception("GEN-selection on column values without a DAT-file is not possible: " + Path.GetFileName(Filename));
            }

            if ((colIdx < 0) || (colIdx > DATFile.ColumnNames.Count))
            {
                throw new Exception("Invalid (zero based) columnindex (" + colIdx + ") for IPF-file: " + Path.GetFileName(Filename));
            }

            if (srcPointIndices == null)
            {
                // When no list is specified, Create dummy list to speed up inner loop, actual list contents will not be returned as list is a value parameter
                srcPointIndices = new List<int>();
            }

            GENFile newGENFile = new GENFile();
            newGENFile.AddDATFile();
            newGENFile.DATFile.ColumnNames = new List<string>(DATFile.ColumnNames);
            if (this.Features.Count == 0)
            {
                return newGENFile;
            }

            // Determine comparison type
            FieldType colFieldType = this.GetFieldType(colIdx);
            FieldType valFieldType = ParseUtils.GetFieldType(valueString);
            FieldType fieldType = FieldType.Undefined;
            if ((colFieldType == FieldType.String) || (valFieldType == FieldType.String))
            {
                // When either col type or value type is a string, then compare values like string values
                fieldType = FieldType.String;

            }
            else if ((colFieldType == FieldType.Double) || (valFieldType == FieldType.Double))
            {
                // When either col type or value type is a double, then compare values like double values
                fieldType = FieldType.Double;
            }
            else
            {
                fieldType = colFieldType;
            }

            bool isColRefExpString = valueString.Contains("{") && valueString.Contains("}");
            object value = ParseUtils.ParseStringValue(valueString, fieldType);

            int featureIdx = 0;
            long longValue;
            double dblValue;
            DateTime datetimeValue;
            List<string> columnNames = DATFile.ColumnNames;

            // For optimization purpose, combinations of operators and fieldtypes are split out
            switch (valueOperator)
            {
                case ValueOperator.Equal:
                    for (; featureIdx < Features.Count; featureIdx++)
                    {
                        GENFeature genFeature = Features[featureIdx];
                        DATRow datRow = DATFile.GetRow(genFeature.ID);
                        if (datRow != null)
                        {
                            object columnValue = ParseUtils.ParseStringValue(datRow[colIdx], fieldType);
                            if (isColRefExpString)
                            {
                                value = ParseUtils.EvaluateStringExpression(columnNames, valueString, datRow);
                            }
                            if (!srcPointIndices.Contains(featureIdx))
                            {
                                if ((useRegExp) && (columnValue is string) && (value is string))
                                {
                                    if (Regex.IsMatch((string)columnValue, (string)value))
                                    {
                                        newGENFile.AddFeature(genFeature);
                                        srcPointIndices.Add(featureIdx);
                                    }
                                }
                                else
                                {
                                    if ((columnValue != null) && columnValue.Equals(value))
                                    {
                                        newGENFile.AddFeature(genFeature);
                                        srcPointIndices.Add(featureIdx);
                                    }
                                }
                            }
                        }
                    }
                    break;
                case ValueOperator.GreaterThan:
                    switch (fieldType)
                    {
                        case FieldType.Long:
                            longValue = (long)value;
                            for (; featureIdx < Features.Count; featureIdx++)
                            {
                                GENFeature genFeature = Features[featureIdx];
                                DATRow datRow = DATFile.GetRow(genFeature.ID);
                                if (datRow != null)
                                {
                                    long pointValue = long.Parse(datRow[colIdx]);
                                    if ((pointValue > longValue) && !srcPointIndices.Contains(featureIdx))
                                    {
                                        newGENFile.AddFeature(genFeature);
                                        srcPointIndices.Add(featureIdx);
                                    }
                                }
                            }
                            break;
                        case FieldType.Double:
                            dblValue = (double)value;
                            for (; featureIdx < Features.Count; featureIdx++)
                            {
                                GENFeature genFeature = Features[featureIdx];
                                DATRow datRow = DATFile.GetRow(genFeature.ID);
                                if (datRow != null)
                                {
                                    double pointValue = double.Parse(datRow[colIdx], EnglishCultureInfo);
                                    if ((pointValue > dblValue) && !srcPointIndices.Contains(featureIdx))
                                    {
                                        newGENFile.AddFeature(genFeature);
                                        srcPointIndices.Add(featureIdx);
                                    }
                                }
                            }
                            break;
                        case FieldType.String:
                            for (; featureIdx < Features.Count; featureIdx++)
                            {
                                GENFeature genFeature = Features[featureIdx];
                                DATRow datRow = DATFile.GetRow(genFeature.ID);
                                if (datRow != null)
                                {
                                    if ((datRow[colIdx].CompareTo(valueString) > 0) && !srcPointIndices.Contains(featureIdx))
                                    {
                                        newGENFile.AddFeature(genFeature);
                                        srcPointIndices.Add(featureIdx);
                                    }
                                }
                            }
                            break;
                        case FieldType.DateTime:
                            datetimeValue = (DateTime)value;
                            for (; featureIdx < Features.Count; featureIdx++)
                            {
                                GENFeature genFeature = Features[featureIdx];
                                DATRow datRow = DATFile.GetRow(genFeature.ID);
                                if (datRow != null)
                                {
                                    DateTime pointValue = DateTime.Parse(datRow[colIdx]);
                                    if ((pointValue > datetimeValue) && !srcPointIndices.Contains(featureIdx))
                                    {
                                        newGENFile.AddFeature(genFeature);
                                        srcPointIndices.Add(featureIdx);
                                    }
                                }
                            }
                            break;
                        case FieldType.Boolean:
                            throw new Exception("Operator GreaterThan is not defined for fieldType Boolean");
                        default:
                            throw new Exception("Select not defined for fieldtype " + fieldType.ToString());
                    }
                    break;
                case ValueOperator.GreaterThanOrEqual:
                    switch (fieldType)
                    {
                        case FieldType.Long:
                            longValue = (long)value;
                            for (; featureIdx < Features.Count; featureIdx++)
                            {
                                GENFeature genFeature = Features[featureIdx];
                                DATRow datRow = DATFile.GetRow(genFeature.ID);
                                if (datRow != null)
                                {
                                    long pointValue = long.Parse(datRow[colIdx]);
                                    if ((pointValue >= longValue) && !srcPointIndices.Contains(featureIdx))
                                    {
                                        newGENFile.AddFeature(genFeature);
                                        srcPointIndices.Add(featureIdx);
                                    }
                                }
                            }
                            break;
                        case FieldType.Double:
                            dblValue = (double)value;
                            for (; featureIdx < Features.Count; featureIdx++)
                            {
                                GENFeature genFeature = Features[featureIdx];
                                DATRow datRow = DATFile.GetRow(genFeature.ID);
                                if (datRow != null)
                                {
                                    double pointValue = double.Parse(datRow[colIdx], EnglishCultureInfo);
                                    if ((pointValue >= dblValue) && !srcPointIndices.Contains(featureIdx))
                                    {
                                        newGENFile.AddFeature(genFeature);
                                        srcPointIndices.Add(featureIdx);
                                    }
                                }
                            }
                            break;
                        case FieldType.String:
                            for (; featureIdx < Features.Count; featureIdx++)
                            {
                                GENFeature genFeature = Features[featureIdx];
                                DATRow datRow = DATFile.GetRow(genFeature.ID);
                                if (datRow != null)
                                {
                                    if ((datRow[colIdx].CompareTo(valueString) >= 0) && !srcPointIndices.Contains(featureIdx))
                                    {
                                        newGENFile.AddFeature(genFeature);
                                        srcPointIndices.Add(featureIdx);
                                    }
                                }
                            }
                            break;
                        case FieldType.DateTime:
                            datetimeValue = (DateTime)value;
                            for (; featureIdx < Features.Count; featureIdx++)
                            {
                                GENFeature genFeature = Features[featureIdx];
                                DATRow datRow = DATFile.GetRow(genFeature.ID);
                                if (datRow != null)
                                {
                                    DateTime pointValue = DateTime.Parse(datRow[colIdx]);
                                    if ((pointValue >= datetimeValue) && !srcPointIndices.Contains(featureIdx))
                                    {
                                        newGENFile.AddFeature(genFeature);
                                        srcPointIndices.Add(featureIdx);
                                    }
                                }
                            }
                            break;
                        case FieldType.Boolean:
                            throw new Exception("Operator GreaterThanOrEqual is not defined for fieldType Boolean");
                        default:
                            throw new Exception("Select not defined for fieldtype " + fieldType.ToString());
                    }
                    break;
                case ValueOperator.LessThan:
                    switch (fieldType)
                    {
                        case FieldType.Long:
                            longValue = (long)value;
                            for (; featureIdx < Features.Count; featureIdx++)
                            {
                                GENFeature genFeature = Features[featureIdx];
                                DATRow datRow = DATFile.GetRow(genFeature.ID);
                                if (datRow != null)
                                {
                                    long pointValue = long.Parse(datRow[colIdx]);
                                    if ((pointValue < longValue) && !srcPointIndices.Contains(featureIdx))
                                    {
                                        newGENFile.AddFeature(genFeature);
                                        srcPointIndices.Add(featureIdx);
                                    }
                                }
                            }
                            break;
                        case FieldType.Double:
                            dblValue = (double)value;
                            for (; featureIdx < Features.Count; featureIdx++)
                            {
                                GENFeature genFeature = Features[featureIdx];
                                DATRow datRow = DATFile.GetRow(genFeature.ID);
                                if (datRow != null)
                                {
                                    double pointValue = double.Parse(datRow[colIdx], EnglishCultureInfo);
                                    if ((pointValue < dblValue) && !srcPointIndices.Contains(featureIdx))
                                    {
                                        newGENFile.AddFeature(genFeature);
                                        srcPointIndices.Add(featureIdx);
                                    }
                                }
                            }
                            break;
                        case FieldType.String:
                            for (; featureIdx < Features.Count; featureIdx++)
                            {
                                GENFeature genFeature = Features[featureIdx];
                                DATRow datRow = DATFile.GetRow(genFeature.ID);
                                if (datRow != null)
                                {
                                    if ((datRow[colIdx].CompareTo(valueString) < 0) && !srcPointIndices.Contains(featureIdx))
                                    {
                                        newGENFile.AddFeature(genFeature);
                                        srcPointIndices.Add(featureIdx);
                                    }
                                }
                            }
                            break;
                        case FieldType.DateTime:
                            datetimeValue = (DateTime)value;
                            for (; featureIdx < Features.Count; featureIdx++)
                            {
                                GENFeature genFeature = Features[featureIdx];
                                DATRow datRow = DATFile.GetRow(genFeature.ID);
                                if (datRow != null)
                                {
                                    DateTime pointValue = DateTime.Parse(datRow[colIdx]);
                                    if ((pointValue < datetimeValue) && !srcPointIndices.Contains(featureIdx))
                                    {
                                        newGENFile.AddFeature(genFeature);
                                        srcPointIndices.Add(featureIdx);
                                    }
                                }
                            }
                            break;
                        case FieldType.Boolean:
                            throw new Exception("Operator LessThan is not defined for fieldType Boolean");
                        default:
                            throw new Exception("Select not defined for fieldtype " + fieldType.ToString());
                    }
                    break;
                case ValueOperator.LessThanOrEqual:
                    switch (fieldType)
                    {
                        case FieldType.Long:
                            longValue = (long)value;
                            for (; featureIdx < Features.Count; featureIdx++)
                            {
                                GENFeature genFeature = Features[featureIdx];
                                DATRow datRow = DATFile.GetRow(genFeature.ID);
                                if (datRow != null)
                                {
                                    long pointValue = long.Parse(datRow[colIdx]);
                                    if ((pointValue <= longValue) && !srcPointIndices.Contains(featureIdx))
                                    {
                                        newGENFile.AddFeature(genFeature);
                                        srcPointIndices.Add(featureIdx);
                                    }
                                }
                            }
                            break;
                        case FieldType.Double:
                            dblValue = (double)value;
                            for (; featureIdx < Features.Count; featureIdx++)
                            {
                                GENFeature genFeature = Features[featureIdx];
                                DATRow datRow = DATFile.GetRow(genFeature.ID);
                                if (datRow != null)
                                {
                                    double pointValue = double.Parse(datRow[colIdx], EnglishCultureInfo);
                                    if ((pointValue <= dblValue) && !srcPointIndices.Contains(featureIdx))
                                    {
                                        newGENFile.AddFeature(genFeature);
                                        srcPointIndices.Add(featureIdx);
                                    }
                                }
                            }
                            break;
                        case FieldType.String:
                            for (; featureIdx < Features.Count; featureIdx++)
                            {
                                GENFeature genFeature = Features[featureIdx];
                                DATRow datRow = DATFile.GetRow(genFeature.ID);
                                if (datRow != null)
                                {
                                    if ((datRow[colIdx].CompareTo(valueString) <= 0) && !srcPointIndices.Contains(featureIdx))
                                    {
                                        newGENFile.AddFeature(genFeature);
                                        srcPointIndices.Add(featureIdx);
                                    }
                                }
                            }
                            break;
                        case FieldType.DateTime:
                            datetimeValue = (DateTime)value;
                            for (; featureIdx < Features.Count; featureIdx++)
                            {
                                GENFeature genFeature = Features[featureIdx];
                                DATRow datRow = DATFile.GetRow(genFeature.ID);
                                if (datRow != null)
                                {
                                    DateTime pointValue = DateTime.Parse(datRow[colIdx]);
                                    if ((pointValue <= datetimeValue) && !srcPointIndices.Contains(featureIdx))
                                    {
                                        newGENFile.AddFeature(genFeature);
                                        srcPointIndices.Add(featureIdx);
                                    }
                                }
                            }
                            break;
                        case FieldType.Boolean:
                            throw new Exception("Operator LessThanThanOrEqual is not defined for fieldType Boolean");
                        default:
                            throw new Exception("Select not defined for fieldtype " + fieldType.ToString());
                    }
                    break;
                case ValueOperator.Unequal:
                    for (; featureIdx < Features.Count; featureIdx++)
                    {
                        GENFeature genFeature = Features[featureIdx];
                        DATRow datRow = DATFile.GetRow(genFeature.ID);
                        if (datRow != null)
                        {
                            object pointValue = ParseUtils.ParseStringValue(datRow[colIdx], fieldType);
                            if (isColRefExpString)
                            {
                                value = ParseUtils.EvaluateStringExpression(DATFile.ColumnNames, valueString, datRow);
                            }
                            if (!srcPointIndices.Contains(featureIdx))
                            {
                                if ((useRegExp) && (pointValue is string) && (value is string))
                                {
                                    if (!Regex.IsMatch((string)pointValue, (string)value))
                                    {
                                        newGENFile.AddFeature(genFeature);
                                        srcPointIndices.Add(featureIdx);
                                    }
                                }
                                else
                                {
                                    if ((pointValue == null) || !pointValue.Equals(value))
                                    {
                                        newGENFile.AddFeature(genFeature);
                                        srcPointIndices.Add(featureIdx);
                                    }
                                }
                            }
                        }
                    }
                    break;
                default:
                    throw new Exception("Undefined operator: " + valueOperator.ToString());
            }

            return newGENFile;
        }

        /// <summary>
        /// Retieves field type (boolean, integer, long, double, string or date) of specified column
        /// </summary>
        /// <param name="fieldColIdx">a (zero based) index for one specific column</param>
        /// <returns></returns>
        public FieldType GetFieldType(int fieldColIdx)
        {
            List<FieldType> fieldTypes = GetFieldTypes(new List<int>() { fieldColIdx });
            return fieldTypes[0];
        }

        /// <summary>
        /// Retieves field types (boolean, integer, long, double, string or date) for current column values
        /// </summary>
        /// <param name="fieldColIndices">null for all columns, or a list of (zero based) indices for specific columns</param>
        /// <returns></returns>
        public List<FieldType> GetFieldTypes(List<int> fieldColIndices = null)
        {
            if (!HasDATFile())
            {
                throw new Exception("Field types cannot be retrieved for GEN-file without DAT-file: " + Path.GetFileName(Filename));
            }

            // Initialize fieldTypes to some type
            List<FieldType> fieldTypes = new List<FieldType>();
            List<bool> isFieldTypeDefined = new List<bool>();

            if (fieldColIndices == null)
            {
                fieldColIndices = new List<int>();
                for (int colIdx = 0; colIdx < DATFile.ColumnNames.Count; colIdx++)
                {
                    fieldColIndices.Add(colIdx);
                }
            }

            foreach (int colIdx in fieldColIndices)
            {
                isFieldTypeDefined.Add(false);
                fieldTypes.Add(FieldType.Boolean);
            }

            for (int featureIdx = 0; featureIdx < Features.Count; featureIdx++)
            {
                GENFeature genFeature = Features[featureIdx];
                DATRow datRow = DATFile.GetRow(genFeature.ID);

                if (datRow != null)
                {
                    for (int fieldColIdxIdx = 0; fieldColIdxIdx < fieldColIndices.Count; fieldColIdxIdx++)
                    {
                        int colIdx = fieldColIndices[fieldColIdxIdx];
                        string value = datRow[colIdx];

                        // Check what type the current value has
                        DateTime dateValue;
                        bool boolValue;
                        double doubleValue;
                        long longValue;
                        if (bool.TryParse(value, out boolValue))
                        {
                            // value is a boolean
                            if (!isFieldTypeDefined[fieldColIdxIdx])
                            {
                                fieldTypes[fieldColIdxIdx] = FieldType.Boolean;
                                isFieldTypeDefined[fieldColIdxIdx] = true;
                            }
                            else if (!fieldTypes[fieldColIdxIdx].Equals(FieldType.Boolean))
                            {
                                fieldTypes[fieldColIdxIdx] = FieldType.String;
                            }
                            else
                            {
                                // leave fieldtype to Bool
                            }
                        }
                        else if (long.TryParse(value, out longValue))
                        {
                            // value is a long (or an integer, etc.)
                            if (!isFieldTypeDefined[fieldColIdxIdx])
                            {
                                fieldTypes[fieldColIdxIdx] = FieldType.Long;
                                isFieldTypeDefined[fieldColIdxIdx] = true;
                            }
                            else if (fieldTypes[fieldColIdxIdx].Equals(FieldType.Double))
                            {
                                // leave fieldType to FloatingPoint
                            }
                            else if (!fieldTypes[fieldColIdxIdx].Equals(FieldType.Long))
                            {
                                // If the current type is other than double or long, a string will be used for this column
                                fieldTypes[fieldColIdxIdx] = FieldType.String;
                            }
                            else
                            {
                                // leave fieldType to shpLong
                            }
                        }
                        else if (double.TryParse(value, NumberStyles.Float, EnglishCultureInfo, out doubleValue))
                        {
                            // value is a double (or a float)
                            if (!isFieldTypeDefined[fieldColIdxIdx])
                            {
                                fieldTypes[fieldColIdxIdx] = FieldType.Double;
                                isFieldTypeDefined[fieldColIdxIdx] = true;
                            }
                            else if (fieldTypes[fieldColIdxIdx].Equals(FieldType.Long))
                            {
                                fieldTypes[fieldColIdxIdx] = FieldType.Double;
                            }
                            else if (!fieldTypes[fieldColIdxIdx].Equals(FieldType.Double))
                            {
                                fieldTypes[fieldColIdxIdx] = FieldType.String;
                            }
                            else
                            {
                                // leave fieldType to FloatingPoint
                            }
                        }
                        else if (DateTime.TryParse(value, out dateValue))
                        {
                            // value is a date
                            if (!isFieldTypeDefined[fieldColIdxIdx])
                            {
                                fieldTypes[fieldColIdxIdx] = FieldType.DateTime;
                                isFieldTypeDefined[fieldColIdxIdx] = true;
                            }
                            else if (!fieldTypes[fieldColIdxIdx].Equals(FieldType.DateTime))
                            {
                                fieldTypes[fieldColIdxIdx] = FieldType.String;
                            }
                            else
                            {
                                // leave fieldType to shpDate
                            }
                        }
                        else
                        {
                            fieldTypes[fieldColIdxIdx] = FieldType.String;
                        }
                    }
                }
            }

            return fieldTypes;
        }

        public GENFeature FindBestMatchingFeature(GENFeature matchedGENFeature, int matchPointIdx, double maxDistance, List<GENFeature> excludedFeatures = null)
        {
            // Call extension method
            return matchedGENFeature.FindBestMatchingFeature(matchPointIdx, Features, maxDistance, excludedFeatures);
        }

        /// <summary>
        /// Find nearest feature to the specified feature in this GENFile 
        /// This is the feature that has a minimum summed distance to all points of the other feature
        /// </summary>
        /// <param name="otherFeature"></param>
        /// <param name="tolerance">Maximum distance to closest point of other feature</param>
        /// <returns></returns>
        public GENFeature FindNearestFeature(GENFeature otherFeature, double tolerance)
        {
            // Call extension method
            return otherFeature.FindNearestFeature(Features, tolerance);
        }

        /// <summary>
        /// Find feature with nearest segment to the specified feature in this GENFile 
        /// This is the feature that has the segment that is closest to the specified point
        /// </summary>
        /// <param name="point"></param>
        /// <param name="tolerance">Maximum distance to closest point of other feature</param>
        /// <param name="excludedFeatures">List of features that should be excluded in the search</param>
        /// <param name="excludedPoints">List of Point that should be excluded in the search</param>
        /// <param name="preferredFeature">Preferred feature in case a choice should be made between points less than Point.Tolerance distance apart. 
        /// Normally this is the feature that was snapped to before.</param>
        /// <param name="preferredFeatureTolerance">Maximum distance to closest point of preferred feature, default (or when NaN is specified) is Point.Tolerance</param>
        /// <returns></returns>
        public GENFeature FindNearestSegmentFeature(Point point, float tolerance, List<GENFeature> excludedFeatures = null,
            List<Point> excludedPoints = null, GENFeature preferredFeature = null, double preferredFeatureTolerance = double.NaN)
        {
            // Call extension method
            return point.FindNearestSegmentFeature(Features, tolerance, excludedFeatures, excludedPoints, preferredFeature, preferredFeatureTolerance);
        }

        /// <summary>
        /// Create a new GENFile object that represents the difference between specified other GEN-file and this GEN-file.
        /// </summary>
        /// <param name="otherGENFile"></param>
        /// <param name="outputPath"></param>
        /// <param name="noDataCalculationValue"></param>
        /// <param name="comparedExtent"></param>
        /// <returns></returns>
        public override IMODFile CreateDifferenceFile(IMODFile otherGENFile, string outputPath, float noDataCalculationValue = float.NaN, Extent comparedExtent = null)
        {
            if (otherGENFile is GENFile)
            {
                return CreateGENDifferenceFile((GENFile)otherGENFile, outputPath, noDataCalculationValue, comparedExtent);
            }
            else
            {
                throw new Exception("Difference between GEN and " + otherGENFile.GetType().Name + " is not implemented");
            }
        }

        /// <summary>
        /// Calculate difference between this GEN-file and another GEN-file. If different the whole feature is returned.
        /// Note: DAT-file is currently not compared. 
        /// </summary>
        /// <param name="otherGENFile"></param>
        /// <param name="outputPath"></param>
        /// <param name="noDataCalculationValue"></param>
        /// <param name="comparedExtent"></param>
        /// <returns>GEN-file with different features (without data), or null if GEN-files are equal</returns>
        public GENFile CreateGENDifferenceFile(GENFile otherGENFile, string outputPath, float noDataCalculationValue = float.NaN, Extent comparedExtent = null)
        {
            // If the objects are equal, there's no need to check the actual contents
            if (object.Equals(this, otherGENFile))
            {
                return null;
            }

            if (otherGENFile == null)
            {
                // When otherFile is missing, the result is a copy of this file
                return CopyGEN(Path.Combine(outputPath, "DIFF_" + Path.GetFileNameWithoutExtension(Filename) + "-null" + Path.GetExtension(Filename)));
            }

            // Create empty difference file to start with
            GENFile diffGENFile = new GENFile();
            string diffFilename = Path.Combine(outputPath, "DIFF_" + Path.GetFileNameWithoutExtension(Filename) + "-"
                + Path.GetFileNameWithoutExtension(otherGENFile.Filename) + Path.GetExtension(Filename));
            diffGENFile.Filename = diffFilename;

            // TODO
            //            diffGENFile.AddColumn("DIFFERENCE");
            //            diffGENFile.textFileColumnIdx = textFileColumnIdx;

            GENFile clippedGENFile = this;
            GENFile clippedOtherGENFile = otherGENFile;
            if (comparedExtent != null)
            {
                clippedGENFile = this.ClipGEN(comparedExtent);
                clippedOtherGENFile = otherGENFile.ClipGEN(comparedExtent);
            }

            Extent clippedGENExtent = clippedGENFile.Extent;
            Extent clippedOtherGENExtent = clippedOtherGENFile.Extent;
            if (!clippedGENExtent.Intersects(clippedOtherGENExtent))
            {
                // No overlap, return all GEN-features
                diffGENFile.AddFeatures(clippedGENFile.Features);
                diffGENFile.AddFeatures(clippedOtherGENFile.Features);
            }

            // Keep track of indices from other GEN-file that haven't been processed yet; to start add indices for all other features
            HashSet<int> leftOverIndices2 = new HashSet<int>();
            for (int featureIdx2 = 0; featureIdx2 < clippedOtherGENFile.Features.Count; featureIdx2++)
            {
                leftOverIndices2.Add(featureIdx2);
            }

            for (int featureIdx1 = 0; featureIdx1 < clippedGENFile.Features.Count; featureIdx1++)
            {
                GENFeature genLine1 = clippedGENFile.Features[featureIdx1];
                int featureIdx2 = clippedOtherGENFile.RetrieveFeatureIndex(genLine1, leftOverIndices2);
                if (featureIdx2 >= 0)
                {
                    leftOverIndices2.Remove(featureIdx2);
                }
                else
                {
                    diffGENFile.AddFeature(genLine1);
                }
            }

            foreach (int featureIdx2 in leftOverIndices2)
            {
                diffGENFile.AddFeature(clippedOtherGENFile.Features[featureIdx2]);
            }

            return (diffGENFile.Count > 0) ? diffGENFile : null;
        }

        /// <summary>
        /// Retrieve index of specified feature or -1 if not found
        /// </summary>
        /// <param name="searchedLine"></param>
        /// <param name="checkedIndices">list of indices to check, to speed up search</param>
        /// <returns></returns>
        public int RetrieveFeatureIndex(GENFeature searchedLine, HashSet<int> checkedIndices)
        {
            Extent searchedExtent = searchedLine.RetrieveExtent();

            foreach (int featureIdx in checkedIndices)
            {
                GENFeature genFeature = Features[featureIdx];
                if (genFeature.RetrieveExtent().Intersects2(searchedExtent))
                {
                    if (genFeature.Equals(searchedLine))
                    {
                        return featureIdx;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Return a difference legend that corresponds with this kind of iMOD-file
        /// </summary>
        /// <returns></returns>
        public Legend CreateDifferenceLegend()
        {
            return CreateDifferenceLegend(null);
        }

        /// <summary>
        /// Creates legend with one class with specified color
        /// </summary>
        /// <param name="color">single color to use for legend, default is orange</param>
        /// <param name="isColorReversed">ignored</param>
        /// <returns></returns>
        public override Legend CreateDifferenceLegend(System.Drawing.Color? color = null, bool isColorReversed = false)
        {
            GENLegend legend = new GENLegend();
            legend.Description = "GEN-file legend";
            legend.Color = (color != null) ? (System.Drawing.Color)color : System.Drawing.Color.Orange;
            legend.Thickness = 2;
            return legend;
        }

        /// <summary>
        /// Create factor difference legend
        /// </summary>
        /// <param name="color">single color to use for legend, default is orange</param>
        /// <param name="isColorReversed">ignored</param>
        /// <returns></returns>
        public override Legend CreateDivisionLegend(System.Drawing.Color? color = null, bool isColorReversed = false)
        {
            return CreateDifferenceLegend(color);
        }
    }
}
