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
using Sweco.SIF.iMOD.GEN;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.IPF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.IFF
{
    /// <summary>
    /// Class for storing and manipulating iMOD flowlines. Flowlines are stored as a list of IFF points that they pass through. 
    /// Each point has an id of the particle that follows this flowline.
    /// </summary>
    public class IFFFile
    {
        protected const int ColumnCount = 9;

        protected List<IFFPoint> particlePoints;
        protected List<string> columnNames;
        protected string filename;
        protected DateTime lastWriteTime;
        protected Dictionary<int, string> particleSourceIdDictionary;
        protected Dictionary<int, int> particleSourceIdxDictionary;

        protected static CultureInfo englishCultureInfo = new CultureInfo("en-GB", false);

        /// <summary>
        /// File extension of IFF-files
        /// </summary>
        public string Extension
        {
            get { return "IFF"; }
        }

        /// <summary>
        /// List of particle points inside this IFF-file
        /// </summary>
        public List<IFFPoint> ParticlePoints
        {
            get { return particlePoints; }
            set { particlePoints = value; }
        }

        /// <summary>
        /// List of column names for this IFF-file
        /// </summary>
        public List<string> ColumnNames
        {
            get { return columnNames; }
            set { columnNames = value; }
        }

        /// <summary>
        /// Filename of this IFF-file
        /// </summary>
        public string Filename
        {
            get { return filename; }
            set { filename = value; }
        }

        /// <summary>
        /// Last write time of this IFF-file
        /// </summary>
        public DateTime LastWriteTime
        {
            get { return lastWriteTime; }
            set { lastWriteTime = value; }
        }

        /// <summary>
        /// Particlenumbers with corresponding source GEN-file polygon id
        /// </summary>
        public Dictionary<int, string> ParticleSourceIdDictionary
        {
            get { return particleSourceIdDictionary; }
            set { particleSourceIdDictionary = value; }
        }

        /// <summary>
        /// Particlenumbers with corresponding source polygon (one-based) index in GEN-file
        /// </summary>
        public Dictionary<int, int> ParticleSourceIdxDictionary
        {
            get { return particleSourceIdxDictionary; }
            set { particleSourceIdxDictionary = value; }
        }

        /// <summary>
        /// Constructor for new IFFFile objects
        /// </summary>
        public IFFFile()
        {
            particlePoints = new List<IFFPoint>();
            this.particleSourceIdDictionary = new Dictionary<int, string>();
            this.particleSourceIdxDictionary = new Dictionary<int, int>();
            columnNames = new List<string>();
        }

        /// <summary>
        /// Check if the specified filename has an IFF-file extension
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static bool HasIFFExtension(string filename)
        {
            return (Path.GetExtension(filename).ToLower().Equals(".iff"));
        }

        public Extent RetrieveExtent()
        {
            double xll = double.MaxValue;
            double yll = double.MaxValue;
            double xur = double.MinValue;
            double yur = double.MinValue;

            double x;
            double y;
            foreach (IFFPoint iffPoint in particlePoints)
            {
                x = iffPoint.X;
                y = iffPoint.Y;
                if (x < xll)
                {
                    xll = x;
                }
                else if (x > xur)
                {
                    xur = x;
                }
                if (y < yll)
                {
                    yll = y;
                }
                else if (y > yur)
                {
                    yur = y;
                }
            }
            return new Extent((float)xll, (float)yll, (float)xur, (float)yur);
        }

        /// <summary>
        /// Read IFFFile object from IFF-file with specified filename
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static IFFFile ReadFile(string filename)
        {
            Stream stream = null;
            StreamReader sr = null;

            IFFFile iffFile = new IFFFile();
            iffFile.filename = filename;
            long lineIdx = 0;
            try
            {
                iffFile.filename = filename;
                iffFile.lastWriteTime = File.GetLastWriteTime(filename);

                stream = File.OpenRead(filename);
                sr = new StreamReader(stream);

                // Read and check column count
                string line = sr.ReadLine().Trim();
                lineIdx++;
                int colCount = int.Parse(line);
                if (colCount != ColumnCount)
                {
                    throw new Exception("Unexpected columncount (" + colCount + ") in IFF-file, expected " + ColumnCount + " columns");
                }

                // Read column names
                for (int colIdx = 0; colIdx < colCount; colIdx++)
                {
                    line = sr.ReadLine().Trim();
                    lineIdx++;
                    iffFile.columnNames.Add(line);
                }

                // Read partice lines
                int currentParticleNumber = -1;
                while (!sr.EndOfStream)
                {
                    line = sr.ReadLine().Trim();
                    lineIdx++;

                    // Replace iMOD-string for infinity by string that is recognize by float.Parse()
                    line = line.Replace("Infinity", float.PositiveInfinity.ToString(englishCultureInfo));

                    string[] lineValues = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    int particleNumber = int.Parse(lineValues[0]);
                    IFFPointEnum pointType = IFFPointEnum.Undefined;
                    if (particleNumber != currentParticleNumber)
                    {
                        // A new particle and pathline is started
                        pointType = IFFPointEnum.StartPoint;

                        // modify pointType of last IFF point, which should be an endpoint
                        if (currentParticleNumber > 0)
                        {
                            iffFile.particlePoints[iffFile.particlePoints.Count - 1].PointType = IFFPointEnum.EndPoint;
                        }

                        currentParticleNumber = particleNumber;
                    }
                    else
                    {
                        pointType = IFFPointEnum.MidPoint;
                    }

                    int ilay = int.Parse(lineValues[1]);
                    double x = double.Parse(lineValues[2], NumberStyles.Float, englishCultureInfo);
                    double y = double.Parse(lineValues[3], NumberStyles.Float, englishCultureInfo);
                    double z = double.Parse(lineValues[4], NumberStyles.Float, englishCultureInfo);
                    float time = float.Parse(lineValues[5], NumberStyles.Float, englishCultureInfo);
                    float velocity = float.Parse(lineValues[6], NumberStyles.Float, englishCultureInfo);
                    int irow = int.Parse(lineValues[7]);
                    int icol = int.Parse(lineValues[8]);
                    IFFPoint iffPoint = new IFFPoint(particleNumber, ilay, x, y, z, time, velocity, irow, icol, pointType);
                    iffFile.particlePoints.Add(iffPoint);
                }
                // modify pointType of last IFF point, which should be an endpoint
                iffFile.particlePoints[iffFile.particlePoints.Count - 1].PointType = IFFPointEnum.EndPoint;
            }
            catch (Exception ex)
            {
                if (lineIdx > 0)
                {
                    throw new Exception("Could not read IFF-file line " + lineIdx + ": " + filename, ex);
                }
                else
                {
                    throw new Exception("Could not read IFF-file: " + filename, ex);
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

            return iffFile;
        }

        /// <summary>
        /// Reverts traveltime per flowline
        /// </summary>
        public void ReverseTravelTime()
        {
            if (particlePoints.Count > 0)
            {
                int currentParticleNumber = particlePoints[0].ParticleNumber;
                int currentParticleStartIdx = 0;
                float currentMaxTraveltime = 0;
                for (int pointIdx = 0; pointIdx < particlePoints.Count; pointIdx++)
                {
                    IFFPoint iffPoint = particlePoints[pointIdx];

                    if ((iffPoint.ParticleNumber == currentParticleNumber) && (iffPoint.Time > currentMaxTraveltime))
                    {
                        currentMaxTraveltime = iffPoint.Time;
                    }

                    if ((iffPoint.ParticleNumber != currentParticleNumber) || (pointIdx == (particlePoints.Count - 1)))
                    {
                        int pointIdx2 = pointIdx - 1;
                        if (pointIdx == (particlePoints.Count - 1))
                        {
                            pointIdx2 = pointIdx;
                        }
                        // A new particle is started,  use max found traveltime to reverse points of current particle
                        for (; pointIdx2 >= currentParticleStartIdx; pointIdx2--)
                        {
                            IFFPoint iffPoint2 = particlePoints[pointIdx2];
                            iffPoint2.Time = currentMaxTraveltime - iffPoint2.Time;
                        }

                        // Reset stats for new particle
                        currentParticleNumber = iffPoint.ParticleNumber;
                        currentParticleStartIdx = pointIdx;
                        currentMaxTraveltime = 0;
                    }
                }
            }
        }

        public ParticleList SelectParticles(SelectPointType selectPointType = SelectPointType.All)
        {
            if (selectPointType == SelectPointType.Undefined)
            {
                throw new Exception("Undefined select method: " + selectPointType.ToString());
            }

            HashSet<int> particleNumbers = new HashSet<int>();
            foreach (IFFPoint iffPoint in particlePoints)
            {
                if (((selectPointType == SelectPointType.Start) && (iffPoint.PointType == IFFPointEnum.StartPoint)) ||
                    ((selectPointType == SelectPointType.Mid) && (iffPoint.PointType == IFFPointEnum.MidPoint)) ||
                    ((selectPointType == SelectPointType.End) && (iffPoint.PointType == IFFPointEnum.EndPoint)) ||
                    (selectPointType == SelectPointType.All))
                {
                    if (!particleNumbers.Contains(iffPoint.ParticleNumber))
                    {
                        particleNumbers.Add(iffPoint.ParticleNumber);
                    }
                }
            }

            return new ParticleList(particleNumbers.ToList<int>());
        }

        /// <summary>
        /// Selects all particle numbers that are in the specified polygons, so even if only one point of the flowline is in the polygon, 
        /// the particle is selected/unselected (when parameter isInside is set to true/false). Only points of the specified type are
        /// evaluated.
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="selectPointType">defines which points are evaluated for the selection of particles</param>
        /// <param name="selectMethod">specify if specified points should be inside/outside the specified extent</param>
        /// <returns>List of selected particle numbers</returns>
        public ParticleList SelectParticles(GENFile genFile, SelectPointType selectPointType = SelectPointType.All, SelectPointMethod selectMethod = SelectPointMethod.Inside)
        {
            if (selectPointType == SelectPointType.Undefined)
            {
                throw new Exception("Undefined select method: " + selectPointType.ToString());
            }

            List<string> seperatorsList = new List<string>() { ",", ";", " ", "	" };
            List<GENPolygon> genPolygons = genFile.RetrieveGENPolygons();
            HashSet<int> particleNumbers = new HashSet<int>();
            Dictionary<int, string> particleSourceIdDictionary = new Dictionary<int, string>();
            Dictionary<int, int> particleSourceIdxDictionary = new Dictionary<int, int>();
            for (int genPolygonIdx = 0; genPolygonIdx < genPolygons.Count; genPolygonIdx++)
            {
                GENPolygon genPolygon = genPolygons[genPolygonIdx];
                string genPolygonId = CommonUtils.RemoveStrings(genPolygon.ID, seperatorsList);
                Extent boundingBox = new Extent(genPolygon.Points);
                foreach (IFFPoint iffPoint in particlePoints)
                {
                    if (((selectPointType == SelectPointType.Start) && (iffPoint.PointType == IFFPointEnum.StartPoint)) ||
                        ((selectPointType == SelectPointType.Mid) && (iffPoint.PointType == IFFPointEnum.MidPoint)) ||
                        ((selectPointType == SelectPointType.End) && (iffPoint.PointType == IFFPointEnum.EndPoint)) ||
                        (selectPointType == SelectPointType.All))
                    {
                        // First do fast check for proximity of point to polygon
                        if (iffPoint.IsContainedBy(boundingBox))
                        {
                            // Point is inside bounding box, do further checks to see if point is inside/outside polygon
                            bool isContainedByPolygon = genPolygon.Contains(iffPoint);
                            if ((isContainedByPolygon && (selectMethod == SelectPointMethod.Inside)) || (!isContainedByPolygon && (selectMethod == SelectPointMethod.Outside)))
                            {
                                if (!particleNumbers.Contains(iffPoint.ParticleNumber))
                                {
                                    particleNumbers.Add(iffPoint.ParticleNumber);
                                    if (!particleSourceIdDictionary.ContainsKey(iffPoint.ParticleNumber))
                                    {
                                        particleSourceIdDictionary.Add(iffPoint.ParticleNumber, genPolygonId);
                                        particleSourceIdxDictionary.Add(iffPoint.ParticleNumber, genPolygonIdx + 1);
                                    }
                                }
                            }
                        }
                        else if ((selectMethod == SelectPointMethod.Outside))
                        {
                            // point is outside bounding box. If points outside the polygon have to be selected, this point should be added
                            if (!particleNumbers.Contains(iffPoint.ParticleNumber))
                            {
                                particleNumbers.Add(iffPoint.ParticleNumber);
                                if (!particleSourceIdDictionary.ContainsKey(iffPoint.ParticleNumber))
                                {
                                    particleSourceIdDictionary.Add(iffPoint.ParticleNumber, genPolygonId);
                                    particleSourceIdxDictionary.Add(iffPoint.ParticleNumber, genPolygonIdx + 1);
                                }
                            }
                        }
                    }
                }
            }

            ParticleList particleList = new ParticleList(particleNumbers, particleSourceIdxDictionary, particleSourceIdDictionary);
            return particleList;
        }

        /// <summary>
        /// Selects all particle numbers that are in the specified polygons, within levels and within time. 
        /// So even if only one point of the flowline is in the polygon, the particle is selected/unselected (when parameter isInside is set to true/false). 
        /// Only points of the specified type are evaluated.
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="topLevelIDFFile"></param>
        /// <param name="botLevelIDFFile"></param>
        /// <param name="minTime"></param>
        /// <param name="maxTime"></param>
        /// <param name="selectPointType">defines which points are evaluated for the selection of particles</param>
        /// <param name="selectMethod">specify if specified points should be inside/outside the specified extent</param>
        /// <returns>List of selected particle numbers</returns>
        public ParticleList SelectParticles(GENFile genFile, IDFFile topLevelIDFFile, IDFFile botLevelIDFFile = null, float minTime = float.NaN, float maxTime = float.NaN, SelectPointType selectPointType = SelectPointType.All, SelectPointMethod selectMethod = SelectPointMethod.Inside)
        {
            if (selectPointType == SelectPointType.Undefined)
            {
                throw new Exception("Undefined select method: " + selectPointType.ToString());
            }

            float maxTopLevelValue = (topLevelIDFFile != null) ? topLevelIDFFile.MaxValue : float.NaN;
            float minBotLevelValue = (botLevelIDFFile != null) ? botLevelIDFFile.MinValue : float.NaN;

            bool isSelectInside = (selectMethod == SelectPointMethod.Inside);
            bool isSelectOutside = (selectMethod == SelectPointMethod.Outside);
            bool isTimeIntervalDefined = (!minTime.Equals(float.NaN) && !maxTime.Equals(float.NaN));

            List<string> seperatorsList = new List<string>() { ",", ";", " ", "	" };
            List<GENPolygon> genPolygons = genFile.RetrieveGENPolygons();
            Dictionary<int, string> particleSourceIdDictionary = new Dictionary<int, string>();
            Dictionary<int, int> particleSourceIdxDictionary = new Dictionary<int, int>();
            HashSet<int> particleNumbers = new HashSet<int>();
            for (int genPolygonIdx = 0; genPolygonIdx < genPolygons.Count; genPolygonIdx++)
            {
                GENPolygon genPolygon = genPolygons[genPolygonIdx];
                string genPolygonId = CommonUtils.RemoveStrings(genPolygon.ID, seperatorsList);
                Extent boundingBox = new Extent(genPolygon.Points);
                foreach (IFFPoint iffPoint in particlePoints)
                {
                    if (((selectPointType == SelectPointType.Start) && (iffPoint.PointType == IFFPointEnum.StartPoint)) ||
                    ((selectPointType == SelectPointType.Mid) && (iffPoint.PointType == IFFPointEnum.MidPoint)) ||
                    ((selectPointType == SelectPointType.End) && (iffPoint.PointType == IFFPointEnum.EndPoint)) ||
                    (selectPointType == SelectPointType.All))
                    {
                        bool isInsideTimeInterval = !(iffPoint.Time < minTime) && !(iffPoint.Time > maxTime);
                        if (!isTimeIntervalDefined || ((isInsideTimeInterval && isSelectInside) || (!isInsideTimeInterval && isSelectOutside)))
                        {
                            float iffPointZ = (float)iffPoint.Z;

                            // First do fast check for proximity of point to specified volume (by polygon and levels)
                            bool isContainedInBoundingBox = iffPoint.IsContainedBy(boundingBox) && (!(iffPointZ > maxTopLevelValue) && !(iffPointZ < minBotLevelValue)); // Use inverse expressions for top and bot to cope with possible NaN-value
                            if (isContainedInBoundingBox)
                            {
                                float topValue = (topLevelIDFFile != null) ? topLevelIDFFile.GetValue((float)iffPoint.X, (float)iffPoint.Y) : float.NaN;
                                float botValue = (botLevelIDFFile != null) ? botLevelIDFFile.GetValue((float)iffPoint.X, (float)iffPoint.Y) : float.NaN;

                                // Point is inside bounding box, do further checks to see if point is inside/outside polygon
                                bool isContainedByPolygon = genPolygon.Contains(iffPoint);
                                bool isContainedByVolume = isContainedByPolygon && (!(iffPointZ > topValue) && !(iffPointZ < botValue));
                                if ((isContainedByVolume && (selectMethod == SelectPointMethod.Inside)) || (!isContainedByVolume && (selectMethod == SelectPointMethod.Outside)))
                                {
                                    if (!particleNumbers.Contains(iffPoint.ParticleNumber))
                                    {
                                        particleNumbers.Add(iffPoint.ParticleNumber);
                                        if (!particleSourceIdDictionary.ContainsKey(iffPoint.ParticleNumber))
                                        {
                                            particleSourceIdDictionary.Add(iffPoint.ParticleNumber, genPolygonId);
                                            particleSourceIdxDictionary.Add(iffPoint.ParticleNumber, genPolygonIdx + 1);
                                        }
                                    }
                                }
                            }
                            else if (selectMethod == SelectPointMethod.Outside)
                            {
                                if (!particleNumbers.Contains(iffPoint.ParticleNumber))
                                {
                                    particleNumbers.Add(iffPoint.ParticleNumber);
                                    if (!particleSourceIdDictionary.ContainsKey(iffPoint.ParticleNumber))
                                    {
                                        particleSourceIdDictionary.Add(iffPoint.ParticleNumber, genPolygonId);
                                        particleSourceIdxDictionary.Add(iffPoint.ParticleNumber, genPolygonIdx + 1);
                                    }
                                }
                            }
                        }
                        else if (selectMethod == SelectPointMethod.Outside)
                        {
                            if (!particleNumbers.Contains(iffPoint.ParticleNumber))
                            {
                                particleNumbers.Add(iffPoint.ParticleNumber);
                                if (!particleSourceIdDictionary.ContainsKey(iffPoint.ParticleNumber))
                                {
                                    particleSourceIdDictionary.Add(iffPoint.ParticleNumber, genPolygonId);
                                    particleSourceIdxDictionary.Add(iffPoint.ParticleNumber, genPolygonIdx + 1);
                                }
                            }
                        }
                    }
                }
            }

            return new ParticleList(particleNumbers.ToList<int>(), particleSourceIdxDictionary, particleSourceIdDictionary);
        }

        /// <summary>
        /// Selects all particle numbers that are between the specified levels, so even if only one point of the flowline is within, 
        /// the particle is selected/unselected (when parameter isInside is set to true/false). 
        /// </summary>
        /// <param name="topLevelIDFFile"></param>
        /// <param name="botLevelIDFFile"></param>
        /// <param name="selectPointType">defines which points are evaluated for the selection of particles</param>
        /// <returns>List of selected particle numbers</returns>
        public ParticleList SelectParticles(IDFFile topLevelIDFFile, IDFFile botLevelIDFFile = null, SelectPointType selectPointType = SelectPointType.All)
        {
            float maxTopLevelValue = (topLevelIDFFile != null) ? topLevelIDFFile.MaxValue : float.NaN;
            float minBotLevelValue = (botLevelIDFFile != null) ? botLevelIDFFile.MinValue : float.NaN;

            HashSet<int> particleNumbers = new HashSet<int>();
            foreach (IFFPoint iffPoint in particlePoints)
            {
                if (((selectPointType == SelectPointType.Start) && (iffPoint.PointType == IFFPointEnum.StartPoint)) ||
                    ((selectPointType == SelectPointType.Mid) && (iffPoint.PointType == IFFPointEnum.MidPoint)) ||
                    ((selectPointType == SelectPointType.End) && (iffPoint.PointType == IFFPointEnum.EndPoint)) ||
                    (selectPointType == SelectPointType.All))
                {
                    float iffPointZ = (float)iffPoint.Z;
                    if (!(iffPointZ > maxTopLevelValue) && !(iffPointZ < minBotLevelValue))
                    {
                        float topValue = (topLevelIDFFile != null) ? topLevelIDFFile.GetValue((float)iffPoint.X, (float)iffPoint.Y) : float.NaN;
                        float botValue = (botLevelIDFFile != null) ? botLevelIDFFile.GetValue((float)iffPoint.X, (float)iffPoint.Y) : float.NaN;
                        // Select points between specified levels
                        if (!(iffPointZ > topValue) && !(iffPointZ < botValue)) // Use inverse expressions for top and bot to cope with possible NaN-value
                        {
                            if (!particleNumbers.Contains(iffPoint.ParticleNumber))
                            {
                                particleNumbers.Add(iffPoint.ParticleNumber);
                            }
                        }
                    }
                }
            }

            return new ParticleList(particleNumbers.ToList<int>());
        }

        /// <summary>
        /// Selects all particle numbers that are in the specified extent, so even if only one point of the flowline is in the extent, 
        /// the particle is selected/unselected (when parameter isInside is set to true/false)
        /// </summary>
        /// <param name="extent"></param>
        /// <param name="selectPointType">defines which points are evaluated for the selection of particles</param>
        /// <param name="selectMethod">specify if specified points should be inside/outside the specified extent</param>
        /// <returns>List of selected particle numbers</returns>
        public ParticleList SelectParticles(Extent extent, SelectPointType selectPointType = SelectPointType.All, SelectPointMethod selectMethod = SelectPointMethod.Inside)
        {
            HashSet<int> particleNumbers = new HashSet<int>();
            foreach (IFFPoint iffPoint in particlePoints)
            {
                if (((selectPointType == SelectPointType.Start) && (iffPoint.PointType == IFFPointEnum.StartPoint)) ||
                    ((selectPointType == SelectPointType.Mid) && (iffPoint.PointType == IFFPointEnum.MidPoint)) ||
                    ((selectPointType == SelectPointType.End) && (iffPoint.PointType == IFFPointEnum.EndPoint)) ||
                    (selectPointType == SelectPointType.All))
                {
                    bool isContained = iffPoint.IsContainedBy(extent);
                    if ((isContained && (selectMethod == SelectPointMethod.Inside)) || (!isContained && (selectMethod == SelectPointMethod.Outside)))
                    {
                        if (!particleNumbers.Contains(iffPoint.ParticleNumber))
                        {
                            particleNumbers.Add(iffPoint.ParticleNumber);
                        }
                    }
                }
            }

            return new ParticleList(particleNumbers.ToList<int>());
        }

        // Select flowlines of specified particles
        public IFFFile SelectFlowLines(ParticleList particleList)
        {
            HashSet<int> particleNumbersHashset = new HashSet<int>(particleList);

            IFFFile newIFFFile = new IFFFile();
            newIFFFile.filename = null;
            foreach (IFFPoint iffPoint in particlePoints)
            {
                if (particleNumbersHashset.Contains(iffPoint.ParticleNumber))
                {
                    newIFFFile.particlePoints.Add(iffPoint);
                    if (particleList.ParticleSourceIdDictionary.Count > 0)
                    {
                        if (particleList.ParticleSourceIdDictionary.ContainsKey(iffPoint.ParticleNumber))
                        {
                            if (!newIFFFile.particleSourceIdDictionary.ContainsKey(iffPoint.ParticleNumber))
                            {
                                newIFFFile.particleSourceIdDictionary.Add(iffPoint.ParticleNumber, particleList.ParticleSourceIdDictionary[iffPoint.ParticleNumber]);
                                newIFFFile.particleSourceIdxDictionary.Add(iffPoint.ParticleNumber, particleList.ParticleSourceIdxDictionary[iffPoint.ParticleNumber]);
                            }
                        }
                    }
                }
            }
            return newIFFFile;
        }

        /// <summary>
        /// Select start- and endpoints from pathlines with specified particlenumber
        /// </summary>
        /// <param name="particleList"></param>
        /// <returns></returns>
        public IPFFile SelectPoints(ParticleList particleList = null)
        {
            HashSet<int> particleNumbersHashset = (particleList != null) ? new HashSet<int>(particleList) : null;

            IPFFile ipfFile = new IPFFile();
            ipfFile.ColumnNames = new List<string> {"SP_X", "SP_Y", "SP_Z", "SP_ILAY", "SP_IROW", "SP_ICOL",
                "EP_X", "EP_Y", "EP_Z", "EP_ILAY", "EP_IROW", "EP_ICOL",
                "MinLayer", "MaxLayer", "MinVelocity", "MaxVelocity", "Time", "Distance", "ParticleId" };
            Dictionary<int, string> particleSourceIdDictionary = null;
            Dictionary<int, int> particleSourceIdxDictionary = null;
            if ((particleList != null) && (particleList.ParticleSourceIdDictionary.Count > 0))
            {
                particleSourceIdDictionary = particleList.ParticleSourceIdDictionary;
                particleSourceIdxDictionary = particleList.ParticleSourceIdxDictionary;
            }
            else if (this.particleSourceIdDictionary.Count > 0)
            {
                particleSourceIdDictionary = this.particleSourceIdDictionary;
                particleSourceIdxDictionary = this.particleSourceIdxDictionary;
            }
            if (particleSourceIdDictionary != null)
            {
                ipfFile.ColumnNames.Add("GENPolygonIdx");
                ipfFile.ColumnNames.Add("GENPolygonId");
            }

            // Store current point info
            int currentParticleNumber = -1;
            double cpX = double.NaN;
            double cpY = double.NaN;
            double cpZ = double.NaN;
            double currentDistance = double.NaN;
            double minVelocity = double.NaN;
            double maxVelocity = double.NaN;
            double time = double.NaN;
            int minILAY = 0;
            int maxILAY = 0;
            // store startpoint info
            double spX = double.NaN;
            double spY = double.NaN;
            double spZ = double.NaN;
            int spILAY = 0;
            int spIRow = 0;
            int spICol = 0;

            foreach (IFFPoint iffPoint in particlePoints)
            {
                if ((particleList == null) || particleNumbersHashset.Contains(iffPoint.ParticleNumber))
                {
                    if (iffPoint.PointType == IFFPointEnum.StartPoint)
                    {
                        spX = iffPoint.X;
                        spY = iffPoint.Y;
                        spZ = iffPoint.Z;
                        spILAY = iffPoint.ILAY;
                        spIRow = iffPoint.IRow;
                        spICol = iffPoint.ICol;
                        minVelocity = double.MaxValue;
                        maxVelocity = 0;
                        time = iffPoint.Time;
                        minILAY = iffPoint.ILAY;
                        maxILAY = iffPoint.ILAY;

                        currentParticleNumber = iffPoint.ParticleNumber;
                        currentDistance = 0;
                        cpX = spX;
                        cpY = spY;
                        cpZ = spZ;
                    }
                    else
                    {
                        // Startpoint always has velocity 0, skip it, but process other velocities
                        if (iffPoint.Velocity < minVelocity)
                        {
                            minVelocity = iffPoint.Velocity;
                        }
                        if (iffPoint.Velocity > maxVelocity)
                        {
                            maxVelocity = iffPoint.Velocity;
                        }
                    }
                    if (iffPoint.ILAY < minILAY)
                    {
                        minILAY = iffPoint.ILAY;
                    }
                    if (iffPoint.ILAY > maxILAY)
                    {
                        maxILAY = iffPoint.ILAY;
                    }

                    currentDistance += Math.Sqrt((iffPoint.X - cpX) * (iffPoint.X - cpX) + (iffPoint.Y - cpY) * (iffPoint.Y - cpY) + (iffPoint.Z - cpZ) * (iffPoint.Z - cpZ));
                    cpX = iffPoint.X;
                    cpY = iffPoint.Y;
                    cpZ = iffPoint.Z;

                    // Note: flowlines that only have one point (which will be a startpoint) will currently be ignored
                    if (iffPoint.PointType == IFFPointEnum.EndPoint)
                    {
                        Point3D xyzPoint = new DoublePoint3D(iffPoint.X, iffPoint.Y, iffPoint.Z);
                        List<string> valueList = new List<string>() { spX.ToString("F3", englishCultureInfo), spY.ToString("F3", englishCultureInfo), spZ.ToString("F3", englishCultureInfo), spILAY.ToString(), spIRow.ToString(), spICol.ToString(),
                            iffPoint.X.ToString("F3", englishCultureInfo), iffPoint.Y.ToString("F3", englishCultureInfo), iffPoint.Z.ToString("F3", englishCultureInfo), iffPoint.ILAY.ToString(), iffPoint.IRow.ToString(), iffPoint.ICol.ToString(),
                            minILAY.ToString(), maxILAY.ToString(), minVelocity.ToString("F3", englishCultureInfo), maxVelocity.ToString("F3", englishCultureInfo), Math.Max(time, iffPoint.Time).ToString("F3", englishCultureInfo), currentDistance.ToString("F3", englishCultureInfo), iffPoint.ParticleNumber.ToString()};
                        if (particleSourceIdDictionary != null)
                        {
                            int sourceIdx = 0;
                            string sourceId = "0";
                            if (particleSourceIdDictionary.ContainsKey(iffPoint.ParticleNumber))
                            {
                                sourceIdx = particleSourceIdxDictionary[iffPoint.ParticleNumber];
                                sourceId = particleSourceIdDictionary[iffPoint.ParticleNumber];
                            }
                            valueList.Add(sourceIdx.ToString());
                            valueList.Add(sourceId);
                        }
                        string[] columnValues = valueList.ToArray();
                        IPFPoint ipfPoint = new IPFPoint(ipfFile, xyzPoint, columnValues);
                        ipfFile.AddPoint(ipfPoint);
                    }
                }
            }

            return ipfFile;
        }

        /// <summary>
        /// Select/clip particle points (so parts of the flowline) within specified extent
        /// </summary>
        /// <param name="extent"></param>
        /// <param name="selectMethod">method for selecting flowlines</param>
        /// <returns></returns>
        public IFFFile SelectFlowLines(Extent extent, SelectFlowLinesMethod selectMethod = SelectFlowLinesMethod.Inside)
        {
            IFFFile newIFFFile = new IFFFile();
            newIFFFile.filename = null;

            bool isSelectInside = (selectMethod == SelectFlowLinesMethod.Inside);
            bool isSelectOutside = (selectMethod == SelectFlowLinesMethod.Outside);
            bool isSelectBefore = (selectMethod == SelectFlowLinesMethod.Before);

            // Add IFF-points either before, inside or outside specified volume
            // Store IFF-points of each particle, until it enters the specified volume. Then add IFF-points up to there and skip following points
            List<IFFPoint> currentIFFPoints = null;
            bool isAdding = true;
            int currentParticleNumber = -1;
            IFFPoint previousPoint = null;
            foreach (IFFPoint iffPoint in particlePoints)
            {
                bool isContained = iffPoint.IsContainedBy(extent);
                if (isSelectBefore || (isContained && isSelectInside) || (!isContained && isSelectOutside))
                {
                    // A new particle is started, reset stats, make last point of previous particle an endpoint, make this point a startpoint
                    if (iffPoint.ParticleNumber != currentParticleNumber)
                    {
                        previousPoint = (newIFFFile.particlePoints.Count > 0) ? newIFFFile.particlePoints[newIFFFile.particlePoints.Count - 1] : null;
                        if ((previousPoint != null) && (previousPoint.ParticleNumber == currentParticleNumber) && (previousPoint.PointType != IFFPointEnum.StartPoint))
                        {
                            previousPoint.PointType = IFFPointEnum.EndPoint;
                        }

                        // A new particle is started, reset stats
                        currentParticleNumber = iffPoint.ParticleNumber;
                        currentIFFPoints = new List<IFFPoint>();
                        isAdding = true;

                        iffPoint.PointType = IFFPointEnum.StartPoint;
                    }

                    if (isSelectBefore)
                    {
                        if (isAdding)
                        {
                            // Store points until the volume is entered
                            currentIFFPoints.Add(iffPoint);
                            if (isContained)
                            {
                                // The specified volume is entered, add flowline up to here to result, but stop adding points
                                foreach (IFFPoint selIFFPoint in currentIFFPoints)
                                {
                                    newIFFFile.particlePoints.Add(selIFFPoint);
                                }
                                isAdding = false;
                            }
                        }
                    }
                    else
                    {
                        newIFFFile.particlePoints.Add(iffPoint);
                    }
                }
            }
            previousPoint = (newIFFFile.particlePoints.Count > 0) ? newIFFFile.particlePoints[newIFFFile.particlePoints.Count - 1] : null;
            if ((previousPoint != null) && (previousPoint.ParticleNumber == currentParticleNumber) && (previousPoint.PointType != IFFPointEnum.StartPoint))
            {
                previousPoint.PointType = IFFPointEnum.EndPoint;
            }

            return newIFFFile;
        }

        /// <summary>
        /// Select/clip particle points (so parts of the flowline) within specified polygon(s), levels AND time
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="topLevelIDFFile"></param>
        /// <param name="botLevelIDFFile"></param>
        /// <param name="minTime"></param>
        /// <param name="maxTime"></param>
        /// <param name="selectMethod">method for selecting flowlines</param>
        /// <returns></returns>
        public IFFFile SelectFlowLines(GENFile genFile, IDFFile topLevelIDFFile, IDFFile botLevelIDFFile = null, float minTime = float.NaN, float maxTime = float.NaN, float minVelocity = float.NaN, float maxVelocity = float.NaN, SelectFlowLinesMethod selectMethod = SelectFlowLinesMethod.Inside)
        {
            IFFFile newIFFFile = new IFFFile();
            newIFFFile.filename = null;

            List<string> seperatorsList = new List<string>() { ",", ";", " ", "	" };
            float maxTopLevelValue = (topLevelIDFFile != null) ? topLevelIDFFile.MaxValue : float.NaN;
            float minBotLevelValue = (botLevelIDFFile != null) ? botLevelIDFFile.MinValue : float.NaN;

            bool isSelectInside = (selectMethod == SelectFlowLinesMethod.Inside);
            bool isSelectOutside = (selectMethod == SelectFlowLinesMethod.Outside);
            bool isSelectBefore = (selectMethod == SelectFlowLinesMethod.Before) || (selectMethod == SelectFlowLinesMethod.BeforeAndInside);
            bool isSelectBeforeAndInside = (selectMethod == SelectFlowLinesMethod.BeforeAndInside);
            bool isSelectBeforeOnly = (selectMethod == SelectFlowLinesMethod.Before);

            if (genFile == null)
            {
                Extent extent = RetrieveExtent();
                extent = extent.Enlarge(1.1f);
                genFile = new GENFile();
                GENPolygon polygon = new GENPolygon(genFile, "0", extent.ToPointList());
                genFile.AddFeature(polygon);
            }

            List<GENPolygon> genPolygons = genFile.RetrieveGENPolygons();
            long p = 0;
            for (int genPolygonIdx = 0; genPolygonIdx < genPolygons.Count; genPolygonIdx++)
            {
                GENPolygon genPolygon = genPolygons[genPolygonIdx];
                string genPolygonId = CommonUtils.RemoveStrings(genPolygon.ID, seperatorsList);
                Extent polygonBoundingBox = new Extent(genPolygon.Points);

                // Add IFF-points either before, inside or outside specified volume
                // Store IFF-points of each particle, until it enters the specified volume. Then add IFF-points up to there and skip following points
                List<IFFPoint> currentIFFPoints = new List<IFFPoint>(); ;
                bool isAdding = false;
                bool isAddingBefore = false;
                bool isAddingInside = false;
                int currentParticleNumber = -1;
                IFFPoint previousPoint = null;
                for (int iffPointIdx = 0; iffPointIdx < particlePoints.Count; iffPointIdx++)
                {
                    IFFPoint iffPoint = particlePoints[iffPointIdx].CopyIFFPoint();

                    if (isAdding || (currentParticleNumber != iffPoint.ParticleNumber))
                    {
                        //  0. check velocity constraints
                        bool isInsideVelolocityInterval = (minVelocity.Equals(float.NaN) || (iffPoint.Velocity >= minVelocity)) && (maxVelocity.Equals(float.NaN) || (iffPoint.Velocity <= maxVelocity));
                        if (isSelectBefore || (isInsideVelolocityInterval && isSelectInside) || (!isInsideVelolocityInterval && isSelectOutside))
                        {
                            //  1. check time constraints
                            bool isInsideTimeInterval = (minTime.Equals(float.NaN) || (iffPoint.Time >= minTime)) && (maxTime.Equals(float.NaN) || (iffPoint.Time <= maxTime));
                            if (isSelectBefore || (isInsideTimeInterval && isSelectInside) || (!isInsideTimeInterval && isSelectOutside))
                            {
                                // 2a. Now do fast check for proximity of point to specified polygon
                                bool isInsidePolygonBoundingBox = iffPoint.IsContainedBy(polygonBoundingBox);
                                if (isSelectBefore || (isInsidePolygonBoundingBox && isSelectInside) || (!isInsidePolygonBoundingBox && isSelectOutside))
                                {
                                    float iffPointZ = (float)iffPoint.Z;
                                    // 3a. Now do fast check for proximity of point to specified levels
                                    bool isInsideLevelBoundingBox = !(iffPointZ > maxTopLevelValue) && !(iffPointZ < minBotLevelValue);
                                    if (isSelectBefore || (isInsideLevelBoundingBox && isSelectInside) || (!isInsidePolygonBoundingBox && isSelectOutside))     // Use inverse expressions for top and bot to cope with possible NaN-value
                                    {
                                        // 3b. Check if point Z-value is between specified levels
                                        float topValue = (topLevelIDFFile != null) ? topLevelIDFFile.GetValue((float)iffPoint.X, (float)iffPoint.Y) : float.NaN;
                                        float botValue = (botLevelIDFFile != null) ? botLevelIDFFile.GetValue((float)iffPoint.X, (float)iffPoint.Y) : float.NaN;
                                        bool isInsideLevels = isInsideLevelBoundingBox && !(iffPointZ > topValue) && !(iffPointZ < botValue); // Use inverse expressions for top and bot to cope with possible NaN-value
                                        if (isSelectBefore || (isInsideLevels && isSelectInside) && (!isInsideLevels && isSelectOutside))
                                        {
                                            // 2b.Selection Before or Point is inside bounding box, do further checks to see if point is inside/outside polygon
                                            bool isInsidePolygon = isInsidePolygonBoundingBox && genPolygon.Contains(iffPoint);
                                            if (isSelectBefore || (isInsidePolygon && isSelectInside) || (!isInsidePolygon && isSelectOutside))
                                            {
                                                if (iffPoint.ParticleNumber != currentParticleNumber)
                                                {
                                                    // A new particle is started, add stored flowpath up to here when the flowline finished inside the specified volume
                                                    if (isAddingInside && (isSelectBeforeAndInside || isSelectInside))
                                                    {
                                                        foreach (IFFPoint selIFFPoint in currentIFFPoints)
                                                        {
                                                            newIFFFile.particlePoints.Add(selIFFPoint);
                                                            if (!newIFFFile.particleSourceIdDictionary.ContainsKey(selIFFPoint.ParticleNumber))
                                                            {
                                                                newIFFFile.particleSourceIdDictionary.Add(selIFFPoint.ParticleNumber, genPolygonId);
                                                                newIFFFile.particleSourceIdxDictionary.Add(selIFFPoint.ParticleNumber, genPolygonIdx + 1);
                                                            }
                                                        }
                                                        currentIFFPoints.Clear();
                                                    }

                                                    // make last point of previous particle an endpoint, make this point a startpoint
                                                    previousPoint = (newIFFFile.particlePoints.Count > 0) ? newIFFFile.particlePoints[newIFFFile.particlePoints.Count - 1] : null;
                                                    if ((previousPoint != null) && (previousPoint.ParticleNumber == currentParticleNumber) && (previousPoint.PointType != IFFPointEnum.StartPoint))
                                                    {
                                                        previousPoint.PointType = IFFPointEnum.EndPoint;
                                                    }

                                                    // reset stats for new particle
                                                    currentParticleNumber = iffPoint.ParticleNumber;
                                                    currentIFFPoints = new List<IFFPoint>();
                                                    isAdding = true;
                                                    isAddingBefore = true;
                                                    isAddingInside = false;

                                                    iffPoint.PointType = IFFPointEnum.StartPoint;
                                                }

                                                if (isSelectBefore)
                                                {
                                                    if (isAdding)
                                                    {
                                                        bool isCurrentIFFPointAdded = false;
                                                        if (isAddingBefore && (isInsidePolygon && isInsideLevels && isInsideTimeInterval && isInsideVelolocityInterval))
                                                        {
                                                            isCurrentIFFPointAdded = true;

                                                            isAddingBefore = false;
                                                            isAddingInside = true;
                                                        }

                                                        if (isAddingInside && (isSelectBeforeOnly || !(isInsidePolygon && isInsideLevels && isInsideTimeInterval && isInsideVelolocityInterval)))
                                                        {
                                                            // The specified volume is entered (beforeonly) or left (before and inside)
                                                            if (isCurrentIFFPointAdded)
                                                            {
                                                                currentIFFPoints.Add(iffPoint);
                                                            }

                                                            // write flowline up to here to result but stop adding points
                                                            foreach (IFFPoint selIFFPoint in currentIFFPoints)
                                                            {
                                                                newIFFFile.particlePoints.Add(selIFFPoint);
                                                                if (!newIFFFile.particleSourceIdDictionary.ContainsKey(selIFFPoint.ParticleNumber))
                                                                {
                                                                    newIFFFile.particleSourceIdDictionary.Add(selIFFPoint.ParticleNumber, genPolygonId);
                                                                    newIFFFile.particleSourceIdxDictionary.Add(selIFFPoint.ParticleNumber, genPolygonIdx + 1);
                                                                }
                                                            }
                                                            isAdding = false;
                                                            isAddingBefore = false;
                                                            isAddingInside = false;
                                                        }
                                                        else
                                                        {
                                                            currentIFFPoints.Add(iffPoint);
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    newIFFFile.particlePoints.Add(iffPoint);
                                                    if (!newIFFFile.particleSourceIdDictionary.ContainsKey(iffPoint.ParticleNumber))
                                                    {
                                                        newIFFFile.particleSourceIdDictionary.Add(iffPoint.ParticleNumber, genPolygonId);
                                                        newIFFFile.particleSourceIdxDictionary.Add(iffPoint.ParticleNumber, genPolygonIdx + 1);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else if (isSelectOutside)
                                    {
                                        newIFFFile.particlePoints.Add(iffPoint);
                                        if (!newIFFFile.particleSourceIdDictionary.ContainsKey(iffPoint.ParticleNumber))
                                        {
                                            newIFFFile.particleSourceIdDictionary.Add(iffPoint.ParticleNumber, genPolygonId);
                                            newIFFFile.particleSourceIdxDictionary.Add(iffPoint.ParticleNumber, genPolygonIdx + 1);
                                        }
                                    }
                                }
                                else if (isSelectOutside)
                                {
                                    newIFFFile.particlePoints.Add(iffPoint);
                                    if (!newIFFFile.particleSourceIdDictionary.ContainsKey(iffPoint.ParticleNumber))
                                    {
                                        newIFFFile.particleSourceIdDictionary.Add(iffPoint.ParticleNumber, genPolygonId);
                                        newIFFFile.particleSourceIdxDictionary.Add(iffPoint.ParticleNumber, genPolygonIdx + 1);
                                    }
                                }
                            }
                        }
                    }
                }
                previousPoint = (newIFFFile.particlePoints.Count > 0) ? newIFFFile.particlePoints[newIFFFile.particlePoints.Count - 1] : null;
                if ((previousPoint != null) && (previousPoint.ParticleNumber == currentParticleNumber) && (previousPoint.PointType != IFFPointEnum.StartPoint))
                {
                    previousPoint.PointType = IFFPointEnum.EndPoint;
                }
            }

            return newIFFFile;
        }

        /// <summary>
        /// Select/clip particle points (so parts of the flowline) within specified polygon(s)
        /// </summary>
        /// <param name="genFile"></param>
        /// <param name="selectMethod">method for selecting flowlines</param>
        /// <returns></returns>
        public IFFFile SelectFlowLines(GENFile genFile, SelectFlowLinesMethod selectMethod = SelectFlowLinesMethod.Inside)
        {
            IFFFile newIFFFile = new IFFFile();
            newIFFFile.filename = null;

            List<string> seperatorsList = new List<string>() { ",", ";", " ", "	" };
            bool isSelectInside = (selectMethod == SelectFlowLinesMethod.Inside);
            bool isSelectOutside = (selectMethod == SelectFlowLinesMethod.Outside);
            bool isSelectBefore = (selectMethod == SelectFlowLinesMethod.Before);

            List<GENPolygon> genPolygons = genFile.RetrieveGENPolygons();
            for (int genPolygonIdx = 0; genPolygonIdx < genPolygons.Count; genPolygonIdx++)
            {
                GENPolygon genPolygon = genPolygons[genPolygonIdx];
                string genPolygonId = CommonUtils.RemoveStrings(genPolygon.ID, seperatorsList);
                Extent boundingBox = new Extent(genPolygon.Points);

                // Add IFF-points either before, inside or outside specified volume
                // Store IFF-points of each particle, until it enters the specified volume. Then add IFF-points up to there and skip following points
                List<IFFPoint> currentIFFPoints = null;
                bool isAdding = true;
                int currentParticleNumber = -1;
                IFFPoint previousPoint = null;
                foreach (IFFPoint iffPoint in particlePoints)
                {
                    // First do fast check for proximity of point to polygon
                    if (isSelectBefore || iffPoint.IsContainedBy(boundingBox))
                    {
                        // SelectionBefore or Point is inside bounding box, do further checks to see if point is inside/outside polygon
                        bool isContainedByPolygon = genPolygon.Contains(iffPoint);
                        if (isSelectBefore || (isContainedByPolygon && isSelectInside) || (!isContainedByPolygon && isSelectOutside))
                        {
                            if (iffPoint.ParticleNumber != currentParticleNumber)
                            {
                                // A new particle is started, reset stats, make last point of previous particle an endpoint, make this point a startpoint
                                previousPoint = (newIFFFile.particlePoints.Count > 0) ? newIFFFile.particlePoints[newIFFFile.particlePoints.Count - 1] : null;
                                if ((previousPoint != null) && (previousPoint.ParticleNumber == currentParticleNumber) && (previousPoint.PointType != IFFPointEnum.StartPoint))
                                {
                                    previousPoint.PointType = IFFPointEnum.EndPoint;
                                }

                                // A new particle is started, reset stats
                                currentParticleNumber = iffPoint.ParticleNumber;
                                currentIFFPoints = new List<IFFPoint>();
                                isAdding = true;

                                iffPoint.PointType = IFFPointEnum.StartPoint;
                            }

                            if (isSelectBefore)
                            {
                                if (isAdding)
                                {
                                    currentIFFPoints.Add(iffPoint);
                                    if (isContainedByPolygon)
                                    {
                                        // The specified volume is entered, write flowline up to here to result but stop adding points
                                        foreach (IFFPoint selIFFPoint in currentIFFPoints)
                                        {
                                            newIFFFile.particlePoints.Add(selIFFPoint);
                                            if (!newIFFFile.particleSourceIdDictionary.ContainsKey(iffPoint.ParticleNumber))
                                            {
                                                newIFFFile.particleSourceIdDictionary.Add(selIFFPoint.ParticleNumber, genPolygonId);
                                                newIFFFile.particleSourceIdxDictionary.Add(selIFFPoint.ParticleNumber, genPolygonIdx + 1);
                                            }
                                        }
                                        isAdding = false;
                                    }
                                }
                            }
                            else
                            {
                                newIFFFile.particlePoints.Add(iffPoint);
                                if (!newIFFFile.particleSourceIdDictionary.ContainsKey(iffPoint.ParticleNumber))
                                {
                                    newIFFFile.particleSourceIdDictionary.Add(iffPoint.ParticleNumber, genPolygonId);
                                    newIFFFile.particleSourceIdxDictionary.Add(iffPoint.ParticleNumber, genPolygonIdx + 1);
                                }
                            }
                        }
                    }
                    else if (isSelectOutside)
                    {
                        newIFFFile.particlePoints.Add(iffPoint);
                        if (!newIFFFile.particleSourceIdDictionary.ContainsKey(iffPoint.ParticleNumber))
                        {
                            newIFFFile.particleSourceIdDictionary.Add(iffPoint.ParticleNumber, genPolygonId);
                            newIFFFile.particleSourceIdxDictionary.Add(iffPoint.ParticleNumber, genPolygonIdx + 1);
                        }
                    }
                }
                previousPoint = (newIFFFile.particlePoints.Count > 0) ? newIFFFile.particlePoints[newIFFFile.particlePoints.Count - 1] : null;
                if ((previousPoint != null) && (previousPoint.ParticleNumber == currentParticleNumber) && (previousPoint.PointType != IFFPointEnum.StartPoint))
                {
                    previousPoint.PointType = IFFPointEnum.EndPoint;
                }

            }

            return newIFFFile;
        }

        public IFFFile SelectFlowLines(string topIDFFilename)
        {
            return SelectFlowLines(topIDFFilename, null);
        }

        public IFFFile SelectFlowLines(string topLevelDefinition, string botLevelDefinition = null)
        {
            IDFFile topLevelIDFFile = ParseIDFDefinition(topLevelDefinition);
            IDFFile botLevelIDFFile = (botLevelDefinition != null) ? ParseIDFDefinition(botLevelDefinition) : null;
            return SelectFlowLines(topLevelIDFFile, botLevelIDFFile);
        }

        /// <summary>
        /// Select/clip particle points (so parts of the flowline) within specified levels
        /// </summary>
        /// <param name="topLevelIDFFile"></param>
        /// <param name="botLevelIDFFile"></param>
        /// <param name="selectMethod">method for selecting flowlines</param>
        /// <returns></returns>
        public IFFFile SelectFlowLines(IDFFile topLevelIDFFile, IDFFile botLevelIDFFile = null, SelectFlowLinesMethod selectMethod = SelectFlowLinesMethod.Inside)
        {
            IFFFile newIFFFile = new IFFFile();
            newIFFFile.filename = null;

            float maxTopLevelValue = (topLevelIDFFile != null) ? topLevelIDFFile.MaxValue : float.NaN;
            float minBotLevelValue = (botLevelIDFFile != null) ? botLevelIDFFile.MinValue : float.NaN;

            bool isSelectInside = (selectMethod == SelectFlowLinesMethod.Inside);
            bool isSelectOutside = (selectMethod == SelectFlowLinesMethod.Outside);
            bool isSelectBefore = (selectMethod == SelectFlowLinesMethod.Before);

            // Add IFF-points either before, inside or outside specified volume
            // Store IFF-points of each particle, until it enters the specified volume. Then add IFF-points up to there and skip following points
            List<IFFPoint> currentIFFPoints = null;
            bool isAdding = true;
            int currentParticleNumber = -1;
            IFFPoint previousPoint = null;
            foreach (IFFPoint iffPoint in particlePoints)
            {
                float iffPointZ = (float)iffPoint.Z;
                // Do fast check against min/max levels
                if (!(iffPointZ > maxTopLevelValue) && !(iffPointZ < minBotLevelValue))     // Use inverse expressions for top and bot to cope with possible NaN-value
                {
                    // Check if point Z-value is between specified levels
                    float topValue = (topLevelIDFFile != null) ? topLevelIDFFile.GetValue((float)iffPoint.X, (float)iffPoint.Y) : float.NaN;
                    float botValue = (botLevelIDFFile != null) ? botLevelIDFFile.GetValue((float)iffPoint.X, (float)iffPoint.Y) : float.NaN;
                    bool isContained = !(iffPointZ > topValue) && !(iffPointZ < botValue); // Use inverse expressions for top and bot to cope with possible NaN-value
                    if (isSelectBefore || isContained)
                    {
                        if (iffPoint.ParticleNumber != currentParticleNumber)
                        {
                            // A new particle is started, reset stats, make last point of previous particle an endpoint, make this point a startpoint
                            previousPoint = (newIFFFile.particlePoints.Count > 0) ? newIFFFile.particlePoints[newIFFFile.particlePoints.Count - 1] : null;
                            if ((previousPoint != null) && (previousPoint.ParticleNumber == currentParticleNumber) && (previousPoint.PointType != IFFPointEnum.StartPoint))
                            {
                                previousPoint.PointType = IFFPointEnum.EndPoint;
                            }

                            // A new particle is started, reset stats
                            currentParticleNumber = iffPoint.ParticleNumber;
                            currentIFFPoints = new List<IFFPoint>();
                            isAdding = true;

                            iffPoint.PointType = IFFPointEnum.StartPoint;
                        }

                        if (isSelectBefore)
                        {
                            if (isAdding)
                            {
                                currentIFFPoints.Add(iffPoint);
                                if (isContained)
                                {
                                    // The specified volume is entered, write flowline up to here to result but stop adding points
                                    foreach (IFFPoint selIFFPoint in currentIFFPoints)
                                    {
                                        newIFFFile.particlePoints.Add(selIFFPoint);
                                    }
                                    isAdding = false;
                                }
                            }
                        }
                        else
                        {
                            newIFFFile.particlePoints.Add(iffPoint);
                        }
                    }
                }
                else if (isSelectOutside)
                {
                    newIFFFile.particlePoints.Add(iffPoint);
                }
            }
            previousPoint = (newIFFFile.particlePoints.Count > 0) ? newIFFFile.particlePoints[newIFFFile.particlePoints.Count - 1] : null;
            if ((previousPoint != null) && (previousPoint.ParticleNumber == currentParticleNumber) && (previousPoint.PointType != IFFPointEnum.StartPoint))
            {
                previousPoint.PointType = IFFPointEnum.EndPoint;
            }

            return newIFFFile;
        }

        public IFFFile SelectFlowLinesByTravelTime(float minTravelTime, float maxTravelTime)
        {
            IFFFile newIFFFile = new IFFFile();
            newIFFFile.filename = null;
            int currentParticleNumber = -1;
            IFFPoint previousPoint = null;
            foreach (IFFPoint iffPoint in particlePoints)
            {
                if ((minTravelTime.Equals(float.NaN) || (iffPoint.Time >= minTravelTime)) && (maxTravelTime.Equals(float.NaN) || (iffPoint.Time <= maxTravelTime)))
                {
                    // A new particle is started, reset stats, make last point of previous particle an endpoint, make this point a startpoint
                    if (iffPoint.ParticleNumber != currentParticleNumber)
                    {
                        previousPoint = (newIFFFile.particlePoints.Count > 0) ? newIFFFile.particlePoints[newIFFFile.particlePoints.Count - 1] : null;
                        if ((previousPoint != null) && (previousPoint.ParticleNumber == currentParticleNumber) && (previousPoint.PointType != IFFPointEnum.StartPoint))
                        {
                            previousPoint.PointType = IFFPointEnum.EndPoint;
                        }

                        // A new particle is started, reset stats
                        currentParticleNumber = iffPoint.ParticleNumber;

                        iffPoint.PointType = IFFPointEnum.StartPoint;
                    }

                    newIFFFile.particlePoints.Add(iffPoint);
                }
            }
            previousPoint = (newIFFFile.particlePoints.Count > 0) ? newIFFFile.particlePoints[newIFFFile.particlePoints.Count - 1] : null;
            if ((previousPoint != null) && (previousPoint.ParticleNumber == currentParticleNumber) && (previousPoint.PointType != IFFPointEnum.StartPoint))
            {
                previousPoint.PointType = IFFPointEnum.EndPoint;
            }
            return newIFFFile;
        }

        public IFFFile SelectFlowLinesByVelocity(float minVelocity, float maxVelocity)
        {
            IFFFile newIFFFile = new IFFFile();
            newIFFFile.filename = null;
            int currentParticleNumber = -1;
            IFFPoint previousPoint = null;
            foreach (IFFPoint iffPoint in particlePoints)
            {
                if ((minVelocity.Equals(float.NaN) || (iffPoint.Velocity >= minVelocity)) && (maxVelocity.Equals(float.NaN) || (iffPoint.Velocity <= maxVelocity)))
                {
                    // A new particle is started, reset stats, make last point of previous particle an endpoint, make this point a startpoint
                    if (iffPoint.ParticleNumber != currentParticleNumber)
                    {
                        previousPoint = (newIFFFile.particlePoints.Count > 0) ? newIFFFile.particlePoints[newIFFFile.particlePoints.Count - 1] : null;
                        if ((previousPoint != null) && (previousPoint.ParticleNumber == currentParticleNumber) && (previousPoint.PointType != IFFPointEnum.StartPoint))
                        {
                            previousPoint.PointType = IFFPointEnum.EndPoint;
                        }

                        // A new particle is started, reset stats
                        currentParticleNumber = iffPoint.ParticleNumber;

                        iffPoint.PointType = IFFPointEnum.StartPoint;
                    }

                    newIFFFile.particlePoints.Add(iffPoint);
                }
            }
            previousPoint = (newIFFFile.particlePoints.Count > 0) ? newIFFFile.particlePoints[newIFFFile.particlePoints.Count - 1] : null;
            if ((previousPoint != null) && (previousPoint.ParticleNumber == currentParticleNumber) && (previousPoint.PointType != IFFPointEnum.StartPoint))
            {
                previousPoint.PointType = IFFPointEnum.EndPoint;
            }
            return newIFFFile;
        }

        private IDFFile ParseIDFDefinition(string idfDefinitionString)
        {
            float value;
            IDFFile idfFile = null;
            if (float.TryParse(idfDefinitionString, NumberStyles.Float, englishCultureInfo, out value))
            {
                idfFile = new ConstantIDFFile(value);
            }
            else
            {
                if (File.Exists(idfDefinitionString))
                {
                    idfFile = IDFFile.ReadFile(idfDefinitionString);
                }
                else
                {
                    throw new Exception("Invalid value or filename: " + idfDefinitionString);
                }
            }
            return idfFile;
        }

        public List<int> InvertParticleSelection(List<int> particleNumbers)
        {
            List<int> inverseSelectionParticleNumbers = new List<int>();
            foreach (IFFPoint iffPoint in particlePoints)
            {
                if (!particleNumbers.Contains(iffPoint.ParticleNumber) && !inverseSelectionParticleNumbers.Contains(iffPoint.ParticleNumber))
                {
                    inverseSelectionParticleNumbers.Add(iffPoint.ParticleNumber);
                }
            }
            return inverseSelectionParticleNumbers;
        }

        public void WriteFile(string iffFilename, bool copyLastWriteTime = false)
        {
            filename = iffFilename;
            WriteFile(copyLastWriteTime);
        }

        public void WriteFile(bool copyLastWriteTime = false)
        {
            StreamWriter sw = null;

            try
            {
                if (!Path.GetDirectoryName(filename).Equals(string.Empty) && !Directory.Exists(Path.GetDirectoryName(filename)))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(filename));
                }

                if (Path.GetExtension(filename).Equals(string.Empty))
                {
                    filename += ".IFF";
                }

                sw = new StreamWriter(filename, false);

                sw.WriteLine(ColumnCount);

                sw.WriteLine("PARTICLE_NUMBER");
                sw.WriteLine("ILAY");
                sw.WriteLine("XCRD.");
                sw.WriteLine("YCRD.");
                sw.WriteLine("ZCRD.");
                sw.WriteLine("TIME(YEARS)");
                sw.WriteLine("VELOCITY(M/DAY)");
                sw.WriteLine("IROW");
                sw.WriteLine("ICOL");

                foreach (IFFPoint iffPoint in particlePoints)
                {
                    sw.Write(iffPoint.ParticleNumber.ToString().PadLeft(10));
                    sw.Write(iffPoint.ILAY.ToString().PadLeft(11));
                    sw.Write(iffPoint.X.ToString("0.0000000E+00", englishCultureInfo).PadLeft(16));
                    sw.Write(iffPoint.Y.ToString("0.0000000E+00", englishCultureInfo).PadLeft(16));
                    sw.Write(iffPoint.Z.ToString("0.0000000E+00", englishCultureInfo).PadLeft(16));
                    sw.Write(iffPoint.Time.ToString("0.0000000E+00", englishCultureInfo).PadLeft(16));
                    sw.Write(iffPoint.Velocity.ToString("0.0000000E+00", englishCultureInfo).PadLeft(16));
                    sw.Write(iffPoint.IRow.ToString().PadLeft(11));
                    sw.Write(iffPoint.ICol.ToString().PadLeft(11));
                    sw.WriteLine();
                }

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

            if (copyLastWriteTime)
            {
                File.SetLastWriteTime(filename, this.lastWriteTime);
            }

        }
    }

    public class IFFPoint : DoublePoint3D
    {
        public int ParticleNumber;
        public int ILAY;
        public float Time;
        public float Velocity;
        public int IRow;
        public int ICol;
        public IFFPointEnum PointType;

        public IFFPoint()
        {
        }

        public IFFPoint(int particleNumber, int ilay, double x, double y, double z, float time, float velocity, int irow, int icol, IFFPointEnum pointType)
        {
            this.ParticleNumber = particleNumber;
            this.ILAY = ilay;
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.Time = time;
            this.Velocity = velocity;
            this.IRow = irow;
            this.ICol = icol;
            this.PointType = pointType;
        }

        public override Point Copy()
        {
            return CopyIFFPoint();
        }

        public IFFPoint CopyIFFPoint()
        {
            return new IFFPoint(ParticleNumber, ILAY, X, Y, Z, Time, Velocity, IRow, ICol, PointType);
        }
    }

    public enum IFFPointEnum
    {
        Undefined,
        StartPoint,
        MidPoint,
        EndPoint,
    }

    public enum SelectPointMethod
    {
        Undefined,
        Inside,
        Outside
    }

    public enum SelectPointType
    {
        Undefined,
        /// <summary>
        /// Selection is based on startpoints only
        /// </summary>
        Start,
        /// <summary>
        /// Selection is based on midpoints only
        /// </summary>
        Mid,
        /// <summary>
        /// Selection is based on endpoints only
        /// </summary>
        End,
        /// <summary>
        /// Selection is based on all points
        /// </summary>
        All,
    }

    /// <summary>
    ///  
    /// </summary>
    public enum SelectFlowLinesMethod
    {
        Undefined,
        /// <summary>
        /// /Flowlines before volume, from startpoints to just inside, are selected
        /// </summary>
        Before,
        /// <summary>
        /// /Flowlines before and inside volume, from startpoints to completely inside, are selected
        /// </summary>
        BeforeAndInside,
        /// <summary>
        /// Flowlines inside volume are selected
        /// </summary>
        Inside,
        /// <summary>
        /// Flowlines outside volume are selected
        /// </summary>
        Outside,
    }
}
