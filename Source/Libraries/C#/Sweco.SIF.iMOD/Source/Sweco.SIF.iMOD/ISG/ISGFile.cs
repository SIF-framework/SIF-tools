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
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.ISG;
using Sweco.SIF.iMOD.Legends;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.ISG
{
    /// <summary>
    /// Class to read and analyze ISG-files. See iMOD-manual for details of ISG-files: https://oss.deltares.nl/nl/web/imod/user-manual.
    /// </summary>
    public class ISGFile : IMODFile, IEquatable<ISGFile>
    {
        // ISG-files contain the following objects (according to iMOD-manual):
        // - Segments:           lines that define the watercoarses. Each segment contains zero or more of the following:
        // - Nodes:              points on a segment that define the XY-position and orientation of each segment. Each segment contains at least two nodes.
        // - Calculation points: point on a segment that define attributes of a segment at that position: water level, bottom, resistance and infiltration factor
        // - Structures:         Structures are the weirs on a segment where a (fixed) water level is maintained
        // - Cross-sections:     Cross-sections are points on a segment where a cross-section is defined;
        // - QWD relationships:  Q-Width-Depth relationships are points on a segment where the relation between the discharge and the width and depth of the water level is defined.

        public const float DistanceErrorMargin = 0.25f;

        /// <summary>
        /// Extension string for this type of iMOD-file
        /// </summary>
        public override string Extension
        {
            get { return "ISG"; }
        }

        /// <summary>
        /// List of segments that are actually loaded in memory or null if an existing ISG-file is lazy-loaded
        /// </summary>
        protected List<ISGSegment> segments;

        /// <summary>
        /// List of segments of this ISG-file; in case of lazy-loading, calling this property will ensure segments are loaded from file.
        /// </summary>
        public List<ISGSegment> Segments
        {
            get
            {
                if (segments == null)
                {
                    LoadSegments();
                }

                return segments;
            }
            set { segments = value; }
        }

        /// <summary>
        /// Number of segments in ISG-file which is (only) set after reading an ISG-file (with or without lazy-loading)
        /// </summary>
        private int segmentCount;

        /// <summary>
        /// Number of segments that are available for this ISG-file in memory or on disk (in case of lazy-loading)
        /// </summary>
        public int SegmentCount
        {
            get
            {
                if (segments != null)
                {
                    return segments.Count;
                }
                else
                {
                    return segmentCount;
                }
            }
        }

        protected Log log;
        public Log Log
        {
            get { return log; }
            set { log = value; }
        }
        protected int logIndentLevel;
        public int LogIndentLevel
        {
            get { return logIndentLevel; }
            set { logIndentLevel = value; }
        }

        /// <summary>
        /// Specifies that the actual segments/data are only loaded at first access
        /// </summary>
        protected bool useLazyLoading;

        /// <summary>
        /// Specifies that the actual segments/data are only loaded at first access
        /// </summary>
        public override bool UseLazyLoading
        {
            get { return useLazyLoading; }
            set { useLazyLoading = value; }
        }

        /// <summary>
        /// Creates empty ISGFile object
        /// </summary>
        /// <param name="filename"></param>
        public ISGFile(string filename = null)
        {
            this.Filename = filename;
            this.segments = new List<ISGSegment>();
            this.segmentCount = -1;
            this.UseLazyLoading = false;
        }

        /// <summary>
        /// Read ISG-file from file into memory
        /// </summary>
        /// <param name="Filename"></param>
        /// <param name="useLazyLoading"></param>
        /// <param name="log"></param>
        /// <param name="logIndentLevel"></param>
        /// <returns></returns>
        /// <exception cref="ToolException"></exception>
        /// <exception cref="Exception"></exception>
        public static ISGFile ReadFile(string Filename, bool useLazyLoading = false, Log log = null, int logIndentLevel = 0)
        {
            if (!File.Exists(Filename))
            {
                throw new ToolException("ISG-file doesn't exist: " + Filename);
            }

            if (!Path.GetExtension(Filename).ToLower().Equals(".isg"))
            {
                throw new Exception("ISG-file has invalid extension: " + Path.GetExtension(Filename));
            }

            ISGFile isgFile;
            try
            {
                isgFile = new ISGFile(Filename);
                isgFile.useLazyLoading = useLazyLoading;
                if (useLazyLoading)
                {
                    isgFile.ReadISGSegmentCount();

                    // Set segments to null to indicate current object is lazy loaded and segments are not yet loaded
                    isgFile.segments = null;
                }
                else
                {
                    isgFile.LoadSegments();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not read ISG-file " + Filename, ex);
            }

            return isgFile;
        }

        /// <summary>
        /// Ensure that all ISG-data is loaded, which is postponed for lazy-loaded ISG-files
        /// </summary>
        public virtual void EnsureLoadedSegments()
        {
            if (segments == null)
            {
                LoadSegments();
            }
        }

        private void LoadSegments()
        {
            if (log != null)
            {
                log.AddMessage(LogLevel.Trace, "Lazy load of ISG-segments for file: " + Filename, 1);
            }

            try
            {
                ReadSegments();
                ReadISP();

                ReadISD1();
                ReadISD2();

                ReadISC1();
                ReadISC2();

                ReadIST1();
                ReadIST2();
                
                UpdateExtent();

            }
            catch (Exception ex)
            {
                throw new Exception("Could not read ISG-file " + Filename, ex);
            }
        }

        protected void ReadISGSegmentCount()
        {
            Stream stream = null;
            StreamReader sr = null;
            string line = null;
            try
            {
                stream = File.OpenRead(Filename);
                sr = new StreamReader(stream);

                // Parse first line with number of points
                segmentCount = 0;
                try
                {
                    line = sr.ReadLine().Trim();
                    string[] lineParts = line.Split(new char[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lineParts.Length > 0)
                    {
                        line = lineParts[0];
                    }
                    // check for erroneous extra zero or other numbers and remove
                    segmentCount = int.Parse(line);
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not read number of segments in line \"" + line + "\"", ex);
                }
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
        }

        /// <summary>
        /// Read ISG-file with names of segments
        /// Warning: Care must be taken that only loaded data is referenced. Use EnsureLoadedSegment() to load all data if necessary (for lazy loading)
        /// </summary>
        /// <exception cref="Exception"></exception>
        /// <exception cref="ToolException"></exception>
        public void ReadSegments()
        {
            Stream stream = null;
            StreamReader sr = null;
            string line = null;
            try
            {
                stream = File.OpenRead(Filename);
                sr = new StreamReader(stream);

                // Parse first line with number of points
                segmentCount = 0;
                try
                {
                    line = sr.ReadLine().Trim();
                    string[] lineParts = line.Split(new char[] { ',', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                    if (lineParts.Length > 0)
                    {
                        line = lineParts[0];
                    }
                    // check for erroneous extra zero or other numbers and remove
                    segmentCount = int.Parse(line);
                }
                catch (Exception)
                {
                    throw new Exception("Could not read number of segments in line \"" + line + "\"");
                }

                // Parse segments
                segments = new List<ISGSegment>();
                extent = new Extent();
                try
                {
                    while (!sr.EndOfStream)
                    {
                        line = sr.ReadLine();
                        segments.Add(ParseSegmentLine(line, ","));
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not read segment " + (segments.Count() + 1) + ": " + line, ex);
                }

                if (segments.Count() != segmentCount)
                {
                    throw new ToolException("Invalid number of segments (" + segmentCount + ") in ISG-file: " + segments.Count + " found");
                }
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
        }

        public void UpdateExtent()
        {
            extent = null;

            if ((segments != null) && (segments.Count() > 0))
            {
                extent = segments[0].GetExtent().Copy();
                for (int segmentIdx = 0; segmentIdx < segments.Count(); segmentIdx++)
                {
                    ISGSegment segment = segments[segmentIdx];
                    if (segment.NSEG > 0)
                    {
                        for (int nodeIdx = 0; nodeIdx < segment.NSEG; nodeIdx++)
                        {
                            float x = segment.Nodes[nodeIdx].X;
                            float y = segment.Nodes[nodeIdx].Y;
                            if (x < extent.llx)
                            {
                                extent.llx = x;
                            }
                            if (x > extent.urx)
                            {
                                extent.urx = x;
                            }
                            if (y < extent.lly)
                            {
                                extent.lly = y;
                            }
                            if (y > extent.ury)
                            {
                                extent.ury = y;
                            }
                        }
                    }
                }
            }
        }

        private static ISGSegment ParseSegmentLine(string line, string listSeperators)
        {
            string[] values = line.Split(listSeperators.ToCharArray());
            if (values.Length != 11)
            {
                throw new Exception("Segmentline has invalid number of values (11 values expected): " + values.Length);
            }
            // Expected parameters for each line: Label ISEG NSEG ICLC NCLC ICRS NCRS ISTW NSTW IQHR NQHR
            return new ISGSegment(values[0].Replace("\"", "").Trim(), int.Parse(values[1]), int.Parse(values[2]), int.Parse(values[3]), int.Parse(values[4]), int.Parse(values[5]), int.Parse(values[6]), int.Parse(values[7]), int.Parse(values[8]), int.Parse(values[9]), int.Parse(values[10]));
        }

        /// <summary>
        /// Read ISP-file with ISG-nodes; this is the minimum requirement to retrieve the extent.
        /// Warning: Care must be taken that only loaded data is referenced. Use EnsureLoadedSegment() to load all data if necessary (for lazy loading)
        /// </summary>
        public void ReadISP()
        {
            if (segments == null)
            {
                throw new Exception("segments is null, ensure data is loaded before calling ReadXXX()-methods");
            }

            string ispFilename = Path.Combine(Path.GetDirectoryName(Filename), Path.GetFileNameWithoutExtension(Filename) + ".ISP");
            if (!File.Exists(ispFilename))
            {
                throw new ToolException("ISP-file does not exist: " + ispFilename);
            }

            Stream stream = null;
            BinaryReader br = null;
            try
            {
                stream = File.OpenRead(ispFilename);
                br = new BinaryReader(stream);
                int recordLength = br.ReadInt32();   // is always 2295 for ISP-files
                int byteLength;
                bool isDoublePrecision;
                switch (recordLength)
                {
                    case 2295:
                        isDoublePrecision = false;
                        byteLength = ISGNode.SingleByteLength;
                        break;
                    case 4343:
                        isDoublePrecision = true;
                        byteLength = ISGNode.DoubleByteLength;
                        break;
                    default:
                        throw new Exception("Unknown record length for ISP-file: " + recordLength);
                }
                foreach (ISGSegment segment in segments)
                {
                    br.BaseStream.Seek(segment.ISEG * byteLength, SeekOrigin.Begin);
                    List<ISGNode> isgNodeList = new List<ISGNode>();
                    for (int subIdx = 0; subIdx < segment.NSEG; subIdx++)
                    {
                        try
                        {
                            ISGNode node = null;
                            if (isDoublePrecision)
                            {
                                node = new ISGNode(segment, (float)br.ReadDouble(), (float)br.ReadDouble());
                            }
                            else
                            {
                                node = new ISGNode(segment, br.ReadSingle(), br.ReadSingle());
                            }
                            isgNodeList.Add(node);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Unexpected error while reading ISP-record " + segment.ISEG + subIdx + " for segment: " + segment.Label, ex);
                        }
                    }
                    segment.Nodes = isgNodeList;
                }
            }
            catch (EndOfStreamException ex)
            {
                throw new Exception("Unexpected end of file reading ISP-file " + ispFilename, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while reading ISP-file " + Filename, ex);
            }
            finally
            {
                if (br != null)
                {
                    br.Close();
                }
                if (stream != null)
                {
                    stream.Close();
                }
            }
        }

        /// <summary>
        /// Read ISD1-file with calculation point locations and names
        /// Warning: Care must be taken that only loaded data is referenced. Use EnsureLoadedSegment() to load all ISG-data if necessary (for lazy loading)
        /// </summary>
        public void ReadISD1()
        {
            if (segments == null)
            {
                throw new Exception("segments is null, ensure data is loaded before calling ReadXXX()-methods");
            }

            string isd1Filename = Path.Combine(Path.GetDirectoryName(Filename), Path.GetFileNameWithoutExtension(Filename) + ".ISD1");
            if (!File.Exists(isd1Filename))
            {
                throw new ToolException("ISD1-file does not exist: " + isd1Filename);
            }

            Stream stream = null;
            BinaryReader br = null;
            try
            {
                stream = File.OpenRead(isd1Filename);
                br = new BinaryReader(stream);
                int recordLength = br.ReadInt32();   // is always 11511 for ISD1-files
                foreach (ISGSegment segment in segments)
                {
                    br.BaseStream.Seek(segment.ICLC * ISGCalculationPoint.ByteLength, SeekOrigin.Begin);
                    List<ISGCalculationPoint> isgCalculationPointList = new List<ISGCalculationPoint>();
                    for (int subIdx = 0; subIdx < segment.NCLC; subIdx++)
                    {
                        try
                        {
                            ISGCalculationPoint calculationPoint = new ISGCalculationPoint();
                            calculationPoint.N = br.ReadInt32();
                            calculationPoint.IREF = br.ReadInt32();
                            calculationPoint.DIST = br.ReadSingle();
                            calculationPoint.CNAME = new string(br.ReadChars(32)).Trim();
                            isgCalculationPointList.Add(calculationPoint);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Unexpected error while reading ISD1-record " + (segment.ICLC + subIdx) + " for segment: " + segment.Label, ex);
                        }
                    }
                    segment.CalculationPoints = isgCalculationPointList;
                }
            }
            catch (EndOfStreamException ex)
            {
                throw new Exception("Unexpected end of file reading ISD1-file " + Filename, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while reading ISD1-file " + Filename, ex);
            }
            finally
            {
                if (br != null)
                {
                    br.Close();
                }
                if (stream != null)
                {
                    stream.Close();
                }
            }
        }

        /// <summary>
        /// Read ISD2-file with calculation calculation point details (level, etc.) 
        /// Warning: Care must be taken that only loaded data is referenced. Use EnsureLoadedSegment() to load all ISG-data if necessary (for lazy loading)
        /// </summary>
        public void ReadISD2()
        {
            if (segments == null)
            {
                throw new Exception("segments is null, ensure data is loaded before calling ReadXXX()-methods");
            }

            string isd2Filename = Path.Combine(Path.GetDirectoryName(Filename), Path.GetFileNameWithoutExtension(Filename) + ".ISD2");
            if (!File.Exists(isd2Filename))
            {
                throw new ToolException("ISD2-file does not exist: " + isd2Filename);
            }

            Stream stream = null;
            BinaryReader br = null;
            try
            {
                stream = File.OpenRead(isd2Filename);
                br = new BinaryReader(stream);
                int recordLength = br.ReadInt32();   // is always 5367 for ISD2-files
                foreach (ISGSegment segment in segments)
                {
                    foreach (ISGCalculationPoint cp in segment.CalculationPoints)
                    {
                        br.BaseStream.Seek(cp.IREF * ISGCalculationPointData.ByteLength, SeekOrigin.Begin);
                        List<ISGCalculationPointData> isd2RecordList = new List<ISGCalculationPointData>();
                        for (int subIdx = 0; subIdx < cp.N; subIdx++)
                        {
                            try
                            {
                                ISGCalculationPointData isd2Record = new ISGCalculationPointData();
                                int IDATE = br.ReadInt32();
                                isd2Record.DATE = new DateTime(IDATE / 10000, (IDATE / 100) % 100, IDATE % 100);
                                isd2Record.WLVL = br.ReadSingle();
                                isd2Record.BTML = br.ReadSingle();
                                isd2Record.RESIS = br.ReadSingle();
                                isd2Record.INFF = br.ReadSingle();
                                isd2RecordList.Add(isd2Record);
                            }
                            catch (Exception ex)
                            {
                                //                              string s = ex.GetBaseException().Message;
                                //                              long mem = GC.GetTotalMemory(true) / 1000000;
                                throw new Exception("Unexpected error while reading ISD2-record " + (cp.IREF + subIdx) + " for segment: " + segment.Label + ", calculation point: " + cp.CNAME, ex);
                            }


                        }
                        cp.cpDataArray = isd2RecordList.ToArray();
                    }
                }
            }
            catch (EndOfStreamException ex)
            {
                throw new Exception("Unexpected end of file reading ISD2-file " + Filename, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while reading ISD2-file " + Filename, ex);
            }
            finally
            {
                if (br != null)
                {
                    br.Close();
                }
                if (stream != null)
                {
                    stream.Close();
                }
            }
        }

        /// <summary>
        /// Read ISC1-file with cross section locations and names
        /// Warning: Care must be taken that only loaded data is referenced. Use EnsureLoadedSegment() to load all ISG-data if necessary (for lazy loading)
        /// </summary>
        public void ReadISC1()
        {
            if (segments == null)
            {
                throw new Exception("segments is null, ensure data is loaded before calling ReadXXX()-methods");
            }

            string isc1Filename = Path.Combine(Path.GetDirectoryName(Filename), Path.GetFileNameWithoutExtension(Filename) + ".ISC1");
            if (!File.Exists(isc1Filename))
            {
                throw new ToolException("ISC1-file does not exist: " + isc1Filename);
            }

            Stream stream = null;
            BinaryReader br = null;
            try
            {
                stream = File.OpenRead(isc1Filename);
                br = new BinaryReader(stream);
                int recordLength = br.ReadInt32();   // is always 11511 for ISC1-files
                foreach (ISGSegment segment in segments)
                {
                    br.BaseStream.Seek(segment.ICRS * ISGCrossSection.ByteLength, SeekOrigin.Begin);
                    List<ISGCrossSection> isgCrossSectionList = new List<ISGCrossSection>();
                    for (int subIdx = 0; subIdx < segment.NCRS; subIdx++)
                    {
                        try
                        {
                            ISGCrossSection crosssection = new ISGCrossSection();
                            crosssection.N = br.ReadInt32();
                            crosssection.IREF = br.ReadInt32();
                            crosssection.DIST = br.ReadSingle();
                            crosssection.CNAME = new string(br.ReadChars(32)).Trim();
                            isgCrossSectionList.Add(crosssection);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Unexpected error while reading ISC1-record " + (segment.ICRS + subIdx) + " for segment: " + segment.Label, ex);
                        }
                    }
                    segment.CrossSections = isgCrossSectionList;
                }
            }
            catch (EndOfStreamException ex)
            {
                throw new Exception("Unexpected end of file reading ISC1-file " + Filename, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while reading ISC1-file " + Filename, ex);
            }
            finally
            {
                if (br != null)
                {
                    br.Close();
                }
                if (stream != null)
                {
                    stream.Close();
                }
            }
        }

        /// <summary>
        /// Read ISC2-file with cross section details (levels, etc.) are loaded
        /// Warning: Care must be taken that only loaded data is referenced. Use EnsureLoadedSegment() to load all ISG-data if necessary (for lazy loading)
        /// </summary>
        public void ReadISC2()
        {
            if (segments == null)
            {
                throw new Exception("segments is null, ensure data is loaded before calling ReadXXX()-methods");
            }

            string isc2Filename = Path.Combine(Path.GetDirectoryName(Filename), Path.GetFileNameWithoutExtension(Filename) + ".ISC2");
            if (!File.Exists(isc2Filename))
            {
                throw new ToolException("ISC2-file does not exist: " + isc2Filename);
            }

            Stream stream = null;
            BinaryReader br = null;
            try
            {
                stream = File.OpenRead(isc2Filename);
                br = new BinaryReader(stream);
                int recordLength = br.ReadInt32();   // is always 3319 for ISC2-files
                foreach (ISGSegment segment in segments)
                {
                    foreach (ISGCrossSection cs in segment.CrossSections)
                    {
                        br.BaseStream.Seek(cs.IREF * ISGCrossSectionData.ByteLength, SeekOrigin.Begin);
                        List<ISGCrossSectionData> isc2RecordList = new List<ISGCrossSectionData>();
                        if (cs.N >= 0)
                        {
                            for (int subIdx = 0; subIdx < cs.N; subIdx++)
                            {
                                try
                                {
                                    ISGCrossSectionData1 isc2Record = new ISGCrossSectionData1();
                                    isc2Record.DISTANCE = br.ReadSingle();
                                    isc2Record.BOTTOM = br.ReadSingle();
                                    isc2Record.KM = br.ReadSingle();
                                    isc2RecordList.Add(isc2Record);
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception("Unexpected error while reading ISC2-record " + (cs.IREF + subIdx) + " for segment: " + segment.Label + ", cross section: " + cs.CNAME, ex);
                                }
                            }
                            cs.Definitions = isc2RecordList.ToArray();
                        }
                        else
                        {
                            float dx = br.ReadSingle();
                            float dy = br.ReadSingle();
                            br.BaseStream.Seek(4, SeekOrigin.Current);
                            for (int subIdx = 1; subIdx < (-cs.N); subIdx++)
                            {
                                try
                                {
                                    ISGCrossSectionData2 isc2Record = new ISGCrossSectionData2();
                                    isc2Record.DX = dx;
                                    isc2Record.DY = dy;
                                    isc2Record.X = br.ReadSingle();
                                    isc2Record.Y = br.ReadSingle();
                                    isc2Record.Z = br.ReadSingle();
                                    isc2RecordList.Add(isc2Record);
                                }
                                catch (Exception ex)
                                {
                                    throw new Exception("Unexpected error while reading ISC2-record " + (cs.IREF + subIdx) + " for segment: " + segment.Label + ", cross section: " + cs.CNAME, ex);
                                }
                            }
                            cs.Definitions = isc2RecordList.ToArray();
                        }
                    }
                }
            }
            catch (EndOfStreamException ex)
            {
                throw new Exception("Unexpected end of file reading ISC2-file " + Filename, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while reading ISC2-file " + Filename, ex);
            }
            finally
            {
                if (br != null)
                {
                    br.Close();
                }
                if (stream != null)
                {
                    stream.Close();
                }
            }
        }

        /// <summary>
        /// Read IST1-file with structure locations and names 
        /// Warning: Care must be taken that only loaded data is referenced. Use EnsureLoadedSegment() to load all ISG-data if necessary (for lazy loading)
        /// </summary>
        public void ReadIST1()
        {
            if (segments == null)
            {
                throw new Exception("segments is null, ensure data is loaded before calling ReadXXX()-methods");
            }

            string ist1Filename = Path.Combine(Path.GetDirectoryName(Filename), Path.GetFileNameWithoutExtension(Filename) + ".IST1");
            if (!File.Exists(ist1Filename))
            {
                throw new ToolException("IST1-file does not exist: " + ist1Filename);
            }

            Stream stream = null;
            BinaryReader br = null;
            try
            {
                stream = File.OpenRead(ist1Filename);
                br = new BinaryReader(stream);
                int recordLength = br.ReadInt32();   // is always 11511 for IST1-files
                foreach (ISGSegment segment in segments)
                {
                    br.BaseStream.Seek(segment.ISTW * ISGStructure.ByteLength, SeekOrigin.Begin);
                    List<ISGStructure> isgStructureList = new List<ISGStructure>();
                    for (int subIdx = 0; subIdx < segment.NSTW; subIdx++)
                    {
                        try
                        {
                            ISGStructure structure = new ISGStructure();
                            structure.N = br.ReadInt32();
                            structure.IREF = br.ReadInt32();
                            structure.DIST = br.ReadSingle();
                            structure.CNAME = new string(br.ReadChars(32));
                            isgStructureList.Add(structure);
                        }
                        catch (Exception ex)
                        {
                            throw new Exception("Unexpected error while reading IST1-record " + (segment.ISTW + subIdx) + " for segment: " + segment.Label, ex);
                        }
                    }
                    segment.Structures = isgStructureList;
                }
            }
            catch (EndOfStreamException ex)
            {
                throw new Exception("Unexpected end of file reading IST1-file " + Filename, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while reading IST1-file " + Filename, ex);
            }
            finally
            {
                if (br != null)
                {
                    br.Close();
                }
                if (stream != null)
                {
                    stream.Close();
                }
            }
        }

        /// <summary>
        /// Read IST2-file with structure details
        /// Warning: Care must be taken that only loaded data is referenced. Use EnsureLoadedSegment() to load all ISG-data if necessary (for lazy loading)
        /// </summary>
        public void ReadIST2()
        {
            if (segments == null)
            {
                throw new Exception("segments is null, ensure data is loaded before calling ReadXXX()-methods");
            }

            string ist2Filename = Path.Combine(Path.GetDirectoryName(Filename), Path.GetFileNameWithoutExtension(Filename) + ".IST2");
            if (!File.Exists(ist2Filename))
            {
                throw new ToolException("IST2-file does not exist: " + ist2Filename);
            }

            Stream stream = null;
            BinaryReader br = null;
            try
            {
                stream = File.OpenRead(ist2Filename);
                br = new BinaryReader(stream);
                int recordLength = br.ReadInt32();   // is always 3319 for IST2-files
                foreach (ISGSegment segment in segments)
                {
                    foreach (ISGStructure structure in segment.Structures)
                    {
                        br.BaseStream.Seek(structure.IREF * ISGStructureData.ByteLength, SeekOrigin.Begin);
                        List<ISGStructureData> ist2RecordList = new List<ISGStructureData>();
                        for (int subIdx = 0; subIdx < structure.N; subIdx++)
                        {
                            try
                            {
                                ISGStructureData ist2Record = new ISGStructureData();
                                int IDATE = br.ReadInt32();
                                ist2Record.DATE = new DateTime(IDATE / 10000, (IDATE % 10000) / 100, IDATE % 100);
                                ist2Record.WLVL_UP = br.ReadSingle();
                                ist2Record.WLVL_DOWN = br.ReadSingle();
                                ist2RecordList.Add(ist2Record);
                            }
                            catch (Exception ex)
                            {
                                throw new Exception("Unexpected error while reading IST2-record " + (structure.IREF + subIdx) + " for segment: " + segment.Label + ", structure: " + structure.CNAME, ex);
                            }
                        }
                        structure.structureDataArray = ist2RecordList.ToArray();
                    }
                }
            }
            catch (EndOfStreamException ex)
            {
                throw new Exception("Unexpected end of file reading IST2-file " + Filename, ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Unexpected error while reading IST2-file " + Filename, ex);
            }
            finally
            {
                if (br != null)
                {
                    br.Close();
                }
                if (stream != null)
                {
                    stream.Close();
                }
            }
        }

        /// <summary>
        /// Copy contents (segments) of this ISGFile object to new object
        /// </summary>
        /// <param name="copiedFilename"></param>
        /// <returns></returns>
        public ISGFile CopyISG(string copiedFilename)
        {
            ISGFile isgFileCopy = new ISGFile(this.Filename);

            if (segments != null)
            {
                for (int segmentIdx = 0; segmentIdx < this.segments.Count(); segmentIdx++)
                {
                    ISGSegment segment = segments[segmentIdx];
                    isgFileCopy.AddSegment(segment.Copy());
                }
            }

            return isgFileCopy;
        }

        /// <summary>
        /// Clips IMODFile instance to given extent
        /// </summary>
        /// <param name="clipExtent"></param>
        /// <returns></returns>
        public override IMODFile Clip(Extent clipExtent)
        {
            return ClipISG(clipExtent);
        }

        /// <summary>
        /// Clip ISG-file segmentwise: If one of the segmentnodes is inside the specified extent, add the whole segment
        /// </summary>
        /// <param name="extent"></param>
        /// <returns></returns>
        public ISGFile ClipISG(Extent extent)
        {
            ISGFile clippedISGFile = new ISGFile(this.Filename);

            EnsureLoadedSegments();

            for (int segmentIdx = 0; segmentIdx < this.segments.Count(); segmentIdx++)
            {
                ISGSegment segment = segments[segmentIdx];
                for (int nodeIdx = 0; nodeIdx < segment.NSEG; nodeIdx++)
                {
                    ISGNode node = segment.Nodes[nodeIdx];
                    // If one of the segmentnodes is inside the specified extent, add the whole segment
                    if (extent.Contains(node.X, node.Y))
                    {
                        // Add complete segment ad skip other nodes
                        clippedISGFile.AddSegment(segment.Copy());

                        nodeIdx = segment.NSEG + 1;
                    }
                }
            }
            clippedISGFile.UpdateExtent();
            return clippedISGFile;
        }

        /// <summary>
        /// Add specified ISG-segment to this ISG-file
        /// </summary>
        /// <param name="isgSegment"></param>
        public void AddSegment(ISGSegment isgSegment)
        {
            // Check via Property if segments are existing
            if (Segments == null)
            {
                // No Segments available, not in memory, not in file. Create empty list.
                segments = new List<ISGSegment>();
            }
            segments.Add(isgSegment);
        }

        /// <summary>
        /// Add specified List of ISG-segments to this ISG-file
        /// </summary>
        /// <param name="isgSegments"></param>
        public void AddSegments(List<ISGSegment> isgSegments)
        {
            // Check via Property if segments are existing
            if (Segments == null)
            {
                // No Segments available, not in memory, not in file. Create empty list.
                isgSegments = new List<ISGSegment>();
            }
            isgSegments.AddRange(isgSegments);
        }

        /// <summary>
        /// Determine equality up to the level of the filename
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public virtual bool Equals(ISGFile other)
        {
            return base.Equals(other);
        }

        /// <summary>
        /// Currently not implemented
        /// </summary>
        public override void ResetValues()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Release memory for lazy loaded objects
        /// </summary>
        /// <param name="isMemoryCollected"></param>
        public override void ReleaseMemory(bool isMemoryCollected = true)
        {
            if (useLazyLoading)
            {
                segments = null;
                if (isMemoryCollected)
                {
                    GC.Collect();
                }
            }
        }

        /// <summary>
        /// Create legend for ISG-file with specified description
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public override Legend CreateLegend(string description)
        {
            ISGLegend isgLegend = new ISGLegend(description, 1, Color.LightBlue);
            return isgLegend;
        }

        /// <summary>
        /// Retrieves number of segments in this ISGFile object
        /// </summary>
        /// <returns></returns>
        public override long RetrieveElementCount()
        {
            return SegmentCount;
        }

        /// <summary>
        /// Currently not implemented
        /// </summary>
        /// <param name="newFilename"></param>
        /// <returns></returns>
        public override IMODFile Copy(string newFilename = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Currently not implemented
        /// </summary>
        /// <param name="metadata"></param>
        public override void WriteFile(Metadata metadata = null)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Currently not implemented
        /// </summary>
        /// <param name="Filename"></param>
        /// <param name="metadata"></param>
        public override void WriteFile(string Filename, Metadata metadata = null)
        {
            throw new NotImplementedException();
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
            if (!(otherIMODFile is ISGFile))
            {
                return false;
            }

            ISGFile otherISGFile = (ISGFile)otherIMODFile;
            if (!isContentComparisonForced && this.Equals(otherISGFile))
            {
                return true;
            }

            EnsureLoadedSegments();
            otherISGFile.EnsureLoadedSegments();

            // Determine comparison extent
            if (!((this.extent != null) && (otherISGFile.Extent != null)))
            {
                // One or both files have a null extent
                if ((this.extent == null) && (otherISGFile.Extent == null))
                {
                    // both files have no content, so equal...
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
                    if (!this.extent.Equals(otherISGFile.Extent))
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
                    Extent comparedExtent2 = otherISGFile.Extent.Clip(comparedExtent);
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
            ISGFile diffISGFile = this.CreateDifferenceFile((ISGFile) otherIMODFile, string.Empty, 0, comparedExtent);
            return (diffISGFile == null);
        }

        /// <summary>
        /// Create a new ISGFile object that represents the difference between specified other ISG-file and this ISG-file.
        /// </summary>
        /// <param name="otherISGFile"></param>
        /// <param name="outputPath"></param>
        /// <param name="noDataCalculationValue">Currently ignored for ISG-files</param>
        /// <param name="comparedExtent"></param>
        /// <returns></returns>
        public override IMODFile CreateDifferenceFile(IMODFile otherISGFile, string outputPath, float noDataCalculationValue = float.NaN, Extent comparedExtent = null)
        {
            if (otherISGFile is ISGFile)
            {
                return CreateDifferenceFile((ISGFile)otherISGFile, outputPath, noDataCalculationValue, comparedExtent);
            }
            else
            {
                throw new Exception("Difference between ISG and " + otherISGFile.GetType().Name + " is not implemented");
            }
        }

        /// <summary>
        /// Create a new ISGFile object that represents the difference between specified other ISG-file and this ISG-file.
        /// All different segments (deleted, different or added) are (completely) returned.
        /// Note: currently only the segments are compared, not the underlying data. 
        /// </summary>
        /// <param name="otherISGFile"></param>
        /// <param name="outputPath"></param>
        /// <param name="noDataCalculationValue">currently ignored for ISG-files</param>
        /// <param name="comparedExtent"></param>
        /// <returns>ISG-file with different segment (without data), or null if ISG-files are equal</returns>
        public ISGFile CreateDifferenceFile(ISGFile otherISGFile, string outputPath, float noDataCalculationValue, Extent comparedExtent = null)
        {
            // If the objects are equal, there's no need to check the actual contents
            if (object.Equals(this, otherISGFile))
            {
                return null;
            }

            if (otherISGFile == null)
            {
                // When other file is missing, the result is a copy of this file
                return CopyISG(Path.Combine(outputPath, "DIFF_" + Path.GetFileNameWithoutExtension(Filename) + "-null" + Path.GetExtension(Filename)));
            }

            string diffFilename = Path.Combine(outputPath, "DIFF_" + Path.GetFileNameWithoutExtension(Filename)
                + "-" + Path.GetFileNameWithoutExtension(otherISGFile.Filename) + Path.GetExtension(Filename));
            ISGFile diffISGFile = new ISGFile(diffFilename);

            this.EnsureLoadedSegments();
            otherISGFile.EnsureLoadedSegments();

            Extent thisISGExtent = this.Extent;
            Extent otherISGExtent = otherISGFile.Extent;
            if (!thisISGExtent.Intersects(otherISGExtent))
            {
                // No overlap, return all segments
                diffISGFile.AddSegments(segments);
                diffISGFile.AddSegments(otherISGFile.segments);
            }

            HashSet<int> leftOverIndices2 = new HashSet<int>();
            for (int segmentIdx2 = 0; segmentIdx2 < otherISGFile.segmentCount; segmentIdx2++)
            {
                leftOverIndices2.Add(segmentIdx2);
            }

            for (int segmentIdx1 = 0; segmentIdx1 < segments.Count; segmentIdx1++)
            {
                ISGSegment isgSegment1 = segments[segmentIdx1];
                int segmentIdx2 = otherISGFile.RetrieveSegmentIndex(isgSegment1, leftOverIndices2);
                if (segmentIdx2 >= 0)
                {
                    leftOverIndices2.Remove(segmentIdx2);
                }
                else
                {
                    diffISGFile.AddSegment(isgSegment1);
                }
            }

            foreach (int segmentIdx2 in leftOverIndices2)
            {
                diffISGFile.AddSegment(otherISGFile.segments[segmentIdx2]);
            }

            return (diffISGFile.SegmentCount > 0) ? diffISGFile : null;
        }

        /// <summary>
        /// Retrive index of specified segment or -1 if not found
        /// </summary>
        /// <param name="searchedSegment"></param>
        /// <returns></returns>
        private int RetrieveSegmentIndex(ISGSegment searchedSegment)
        {
            Extent searchedExtent = searchedSegment.GetExtent();

            for (int segmentIdx = 0; segmentIdx < segments.Count; segmentIdx++)
            {
                ISGSegment isgSegment = segments[segmentIdx];
                if (isgSegment.HasOverlap(searchedExtent))
                {
                    if (isgSegment.Equals(searchedSegment))
                    {
                        return segmentIdx;
                    }
                }
            }

            return -1;
        }

        /// <summary>
        /// Retrieve index of specified segment or -1 if not found
        /// </summary>
        /// <param name="searchedSegment"></param>
        /// <param name="checkedSegmentIndices">list of segment indices to check, to speed up search</param>
        /// <returns></returns>
        private int RetrieveSegmentIndex(ISGSegment searchedSegment, ICollection<int> checkedSegmentIndices)
        {
            Extent searchedExtent = searchedSegment.GetExtent();

            foreach (int segmentIdx in checkedSegmentIndices)
            {
                ISGSegment isgSegment = segments[segmentIdx];
                if (isgSegment.HasOverlap(searchedExtent))
                {
                    if (isgSegment.Equals(searchedSegment))
                    {
                        return segmentIdx;
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
        /// Create legend with single color that is used for methods that need a difference legend
        /// </summary>
        /// <param name="color">single color to use for legend, default is orange</param>
        /// <param name="isColorReversed">ignored</param>
        /// <returns></returns>
        public override Legend CreateDifferenceLegend(System.Drawing.Color? color = null, bool isColorReversed = false)
        {
            ISGLegend legend = new ISGLegend();
            legend.Description = "ISG-file legend";
            legend.Color = (color != null) ? (Color)color : Color.Orange;
            legend.Thickness = 2;
            return legend;
        }

        /// <summary>
        /// Create legend with single color that is used for methods that need a factor difference legend
        /// </summary>
        /// <param name="color">single color to use for legend, default is orange</param>
        /// <param name="isColorReversed">ignored</param>
        /// <returns></returns>
        public override Legend CreateDivisionLegend(System.Drawing.Color? color = null, bool isColorReversed = false)
        {
            return CreateDifferenceLegend(null);
        }
    }
}
