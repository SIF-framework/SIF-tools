// GENbuffer is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of GENbuffer.
// 
// GENbuffer is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// GENbuffer is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with GENbuffer. If not, see <https://www.gnu.org/licenses/>.
using ClipperLib;
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.GEN;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.GENbuffer
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
            ToolPurpose = "SIF-tool for buffering GEN-lines or -polygons";
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

            // Place worker code here
            string outputPath = settings.OutputPath;

            // Create output path if not yet existing
            if (!Directory.Exists(outputPath))
            {
                Directory.CreateDirectory(outputPath);
            }

            // An example for reading files from a path and creating a new file...
            string[] inputFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter, settings.IsRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly );
            if ((inputFilenames.Length > 1) && (settings.OutputFilename != null))
            {
                throw new ToolException("An output filename is specified, but more than one input file is found for current filter '" + settings.InputFilter + "'. " +
				                        "Specify just an output path or modify input filter to select only a single file.");
            }

            Log.AddInfo("Processing input files ...");
            int fileCount = 0;
            foreach (string inputFilename in inputFilenames)
            {
                Log.AddInfo("Processing GEN-file " + Path.GetFileName(inputFilename) + " ...", 1);

                Log.AddInfo("Reading GEN-file ...", 2);
                GENFile genFile = GENFile.ReadFile(inputFilename);

                Log.AddInfo("Adding buffer of " + settings.BufferSize + "m to GEN-file ...", 2);
                GENFile newGENFile = ProcessBuffer(genFile, settings);

                string outputFilename = RetrieveOutputFilename(inputFilename, settings.OutputPath, settings.InputPath, settings.OutputFilename, "GEN");
                outputFilename = FileUtils.AddFilePostFix(outputFilename, "_buffer" + (int)(settings.BufferSize));
                Log.AddInfo("Writing GEN-file " + Path.GetFileName(outputFilename) + " ...", 2);
                newGENFile.WriteFile(outputFilename);

                fileCount++;
            }

            ToolSuccessMessage = "Finished processing " + fileCount + " file(s)";

            return exitcode;
        }

        protected GENFile ProcessBuffer(GENFile genFile, SIFToolSettings settings)
        {
            GENFile bufferGENFile = new GENFile();
            int polygonCount = 0;

            if (genFile.HasDATFile())
            {
                // Copy DAT-file columns
                bufferGENFile.AddDATFile();
                for (int colIdx = 1; colIdx < genFile.DATFile.ColumnNames.Count; colIdx++)
                {
                    bufferGENFile.DATFile.AddColumn(genFile.DATFile.ColumnNames[colIdx]);
                }
            }

            // Process source GEN-polygons
            List<GENPolygon> genPolygons = genFile.RetrieveGENPolygons();

            // Convert lines to polygons with area 0
            List<GENLine> genLines = genFile.RetrieveGENLines();
            if (genLines.Count > 0)
            {
                for (int lineIdx = 0; lineIdx < genLines.Count; lineIdx++)
                {
                    GENLine genLine = genLines[lineIdx];

                    if (genLine.Points.Count > 0)
                    {
                        List<Point> pointList = new List<Point>(genLine.Points);
                        genLine.ReversePoints();
                        genLine.Points.RemoveAt(0);
                        pointList.AddRange(genLine.Points);
                        GENPolygon genPolygon = new GENPolygon(genFile, "L" + genLine.ID, pointList);
                        genPolygons.Add(genPolygon);
                    }
                }
            }

            foreach (GENPolygon genPolygon in genPolygons)
            {
                ClipperOffset co = new ClipperOffset();

                // Convert GEN-polygons to ClipperLib datastructure
                List<IntPoint> intPointList = new List<IntPoint>();
                foreach (Point point in genPolygon.Points)
                {
                    intPointList.Add(new IntPoint(point.X, point.Y));
                }
                JoinType joinType = JoinType.jtRound;
                switch (settings.BufferJoinType)
                {
                    case BufferJoinType.Round:
                        joinType = JoinType.jtRound;
                        break;
                    case BufferJoinType.Miter:
                        joinType = JoinType.jtMiter;
                        break;
                    case BufferJoinType.Square:
                        joinType = JoinType.jtSquare;
                        break;
                    default:
                        throw new ToolException("Invalid BufferJoinType: " + settings.BufferJoinType.ToString());
                }

                // Buffer with ClipperLib
                co.AddPath(intPointList, joinType, EndType.etClosedPolygon);
                List<List<IntPoint>> intPointLists = new List<List<IntPoint>>();
                co.Execute(ref intPointLists, settings.BufferSize);

                // Convert ClipperLib result to GEN-file
                DATRow datRow = null;
                if (genFile.HasDATFile())
                {
                    datRow = genFile.DATFile.GetRow(genPolygon.ID);
                }

                foreach (List<IntPoint> intPointList2 in intPointLists)
                {
                    polygonCount++;
                    GENPolygon genPolygon2 = new GENPolygon(bufferGENFile, polygonCount.ToString());
                    foreach (IntPoint intPoint in intPointList2)
                    {
                        Point point = new GIS.DoublePoint(intPoint.X, intPoint.Y);
                        genPolygon2.Points.Add(point);
                    }
                    if (!genPolygon2.Points[0].Equals(genPolygon2.Points[genPolygon2.Points.Count - 1]))
                    {
                        genPolygon2.Points.Add(genPolygon2.Points[0]);
                    }
                    bufferGENFile.AddFeature(genPolygon2);
                    if (datRow != null)
                    {

                        DATRow newRow = datRow.Copy();
                        newRow[0] = genPolygon2.ID;
                        bufferGENFile.DATFile.AddRow(newRow);
                    }
                }
            }

            return bufferGENFile;
        }
    }
}
