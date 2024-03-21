// IFFSHPconvert is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IFFSHPconvert.
// 
// IFFSHPconvert is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IFFSHPconvert is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IFFSHPconvert. If not, see <https://www.gnu.org/licenses/>.
using EGIS.ShapeFileLib;
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.IFF;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IFFSHPconvert
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
            AddAuthor("Koen Jansen");
            AddAuthor("Koen van der Hauw");
            ToolPurpose = "SIF-tool for converting IFF-files to shapefiles";
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

            // Create output path if not yet existing
            if (!Directory.Exists(settings.OutputPath))
            {
                Directory.CreateDirectory(settings.OutputPath);
            }

            //plus
            // Delete output if specified
            if (settings.IsOutputDeleted)
            {
                // Remove previous results when present. Request path length >= 6 to avoid deletion of rootfolders. Minimum path is something like "c:\tmp"
                if (Directory.Exists(settings.OutputPath) && (settings.OutputPath.Length >= 6) && !settings.OutputPath.StartsWith("\\\\"))
                {
                    // try to remove old output files
                    try
                    {
                        Directory.Delete(Path.Combine(settings.OutputPath), true);
                    }
                    catch (Exception)
                    {
                        throw new ToolException("Could not delete previous results: " + Path.Combine(settings.OutputPath));
                    }

                    Directory.CreateDirectory(settings.OutputPath);
                }
            }

            // An example for reading files from a path and creating a new file...
            string[] inputFilenames = Directory.GetFiles(settings.InputPath, settings.InputFilter);

            Log.AddInfo("Processing input files ...");
            int fileCount = 0;

            if ((inputFilenames.Length > 1) && (settings.OutputFilename != null))
            {
                throw new ToolException("An output filename is specified, but more than one ISG-file is found for current filter: " + settings.InputFilter);
            }

            foreach (string inputFilename in inputFilenames)
            {
                string newSHPFilename = null;
                int logIndentLevel = 1;
                if (settings.OutputFilename == null)
                {
                    newSHPFilename = Path.Combine(settings.OutputPath, Path.GetFileNameWithoutExtension(inputFilename), ".shp");
                }
                else
                {
                    newSHPFilename = Path.Combine(settings.OutputPath, settings.OutputFilename);
                }

                Log.AddInfo("Reading IFF-file " + Path.GetFileName(inputFilename) + " ...", logIndentLevel);
                IFFFile iffFile = IFFFile.ReadFile(inputFilename);

                Log.AddInfo("Converting " + Path.GetFileName(inputFilename) + " ...", 1);
                int iffPointIdx = 0;
                ShapeFileWriter sfw = null;

                try
                {
                    string shapeFilename = (settings.OutputFilename != null) ? settings.OutputFilename : inputFilename;
                    shapeFilename = Path.Combine(settings.OutputPath, Path.GetFileNameWithoutExtension(shapeFilename) + ".shp");
                    List<DbfFieldDesc> fieldDescs = CreateIFFFieldDescs();
                    sfw = ShapeFileWriter.CreateWriter(settings.OutputPath, Path.GetFileNameWithoutExtension(shapeFilename), ShapeType.PolyLine, fieldDescs.ToArray());

                    IFFPoint prevIFFPoint = iffFile.ParticlePoints[0];
                    int currentParticleNr = prevIFFPoint.ParticleNumber;
                    short lineCount = 0;
                    for (iffPointIdx = 1; iffPointIdx < iffFile.ParticlePoints.Count; iffPointIdx++)
                    {
                        IFFPoint iffPoint = iffFile.ParticlePoints[iffPointIdx];
                        if (iffPoint.ParticleNumber == currentParticleNr)
                        {
                            lineCount++;
                            AddShapeRecord(sfw, prevIFFPoint, iffPoint);
                        }
                        else
                        {
                            currentParticleNr = iffPoint.ParticleNumber;
                        }
                        prevIFFPoint = iffPoint;
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Unexpected error at point " + (iffPointIdx + 1) + " when reading " + Path.GetFileName(inputFilename), ex);
                }
                finally
                {
                    if (sfw != null)
                    {
                        sfw.Close();
                    }
                }

                fileCount++;
            }

            if (fileCount == 0)
            {
                Log.AddWarning("No files found for filter '" + settings.InputFilter + "' in path: " + settings.InputPath, 1);
            }

            ToolSuccessMessage = "Finished processing " + fileCount + " file(s)";

            return exitcode;
        }

        private void AddShapeRecord(ShapeFileWriter sfw, IFFPoint prevIFFPoint, IFFPoint iffPoint)
        {
            PointD[] points = new PointD[2];
            points[0] = new PointD(prevIFFPoint.X, prevIFFPoint.Y);
            points[1] = new PointD(iffPoint.X, iffPoint.Y);
            sfw.AddRecord(points, 2, new string[] { prevIFFPoint.X.ToString("F3", EnglishCultureInfo), prevIFFPoint.Y.ToString("F3", EnglishCultureInfo), prevIFFPoint.Z.ToString("F3", EnglishCultureInfo),
                iffPoint.X.ToString("F3", EnglishCultureInfo), iffPoint.Y.ToString("F3", EnglishCultureInfo), iffPoint.Z.ToString("F3", EnglishCultureInfo),
                iffPoint.ParticleNumber.ToString(), iffPoint.Time.ToString("F3", EnglishCultureInfo), iffPoint.Velocity.ToString("F3", EnglishCultureInfo), iffPoint.IRow.ToString(), iffPoint.ICol.ToString() });
        }

        private List<DbfFieldDesc> CreateIFFFieldDescs()
        {
            List<DbfFieldDesc> fieldDescs = new List<DbfFieldDesc>();
            fieldDescs.Add(CreateIFFFieldDescs(DbfFieldType.FloatingPoint, "X1", 12, 3));
            fieldDescs.Add(CreateIFFFieldDescs(DbfFieldType.FloatingPoint, "Y1", 12, 3));
            fieldDescs.Add(CreateIFFFieldDescs(DbfFieldType.FloatingPoint, "Z1", 12, 3));
            fieldDescs.Add(CreateIFFFieldDescs(DbfFieldType.FloatingPoint, "X2", 12, 3));
            fieldDescs.Add(CreateIFFFieldDescs(DbfFieldType.FloatingPoint, "Y2", 12, 3));
            fieldDescs.Add(CreateIFFFieldDescs(DbfFieldType.FloatingPoint, "Z2", 12, 3));
            fieldDescs.Add(CreateIFFFieldDescs(DbfFieldType.Number, "ParticleNr", 10));
            fieldDescs.Add(CreateIFFFieldDescs(DbfFieldType.FloatingPoint, "Traveltime", 12, 3));
            fieldDescs.Add(CreateIFFFieldDescs(DbfFieldType.FloatingPoint, "Velocity", 12, 3));
            fieldDescs.Add(CreateIFFFieldDescs(DbfFieldType.Number, "IRow", 8));
            fieldDescs.Add(CreateIFFFieldDescs(DbfFieldType.Number, "ICol", 8));
            return fieldDescs;
        }

        private DbfFieldDesc CreateIFFFieldDescs(DbfFieldType fieldType, string fieldName, int fieldLength, int decimalCount = -1)
        {
            DbfFieldDesc dbfFieldDesc = new DbfFieldDesc();
            dbfFieldDesc.FieldType = fieldType;
            dbfFieldDesc.FieldName = fieldName;
            dbfFieldDesc.FieldLength = fieldLength;
            if (decimalCount >= 0)
            {
                dbfFieldDesc.DecimalCount = decimalCount;
            }
            return dbfFieldDesc;
        }
    }
}
