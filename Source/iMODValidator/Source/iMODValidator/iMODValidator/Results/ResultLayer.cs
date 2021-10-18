// iMODValidator is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of iMODValidator.
// 
// iMODValidator is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// iMODValidator is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with iMODValidator. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD;
using Sweco.SIF.iMOD.IDF;
using Sweco.SIF.iMOD.IPF;
using Sweco.SIF.iMOD.Legends;
using Sweco.SIF.iMODPlus.IDF;
using Sweco.SIF.iMODValidator.Models;
using Sweco.SIF.iMODValidator.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Results
{
    /// <summary>
    /// A ResultLayer can hold any kind of result(s) for a specific layer and timestep, stored in an iMOD-file
    /// </summary>
    public abstract class ResultLayer : IEquatable<ResultLayer>
    {
        /// <summary>
        /// The maximum distance between two points to be assumed identical 
        /// </summary>
        protected const float IPFXYMargin = 0.01f;

        /// <summary>
        /// String that identifies and describes this type of result layer
        /// </summary>
        public abstract string ResultType { get; }

        protected string id;
        /// <summary>
        /// String that defines the id of this resultlayer as used in it's filename/path and in tables
        /// </summary>
        public string Id
        {
            get { return id; }
            set { id = value; }
        }

        protected string id2;
        /// <summary>
        /// String that defines the id of this resultlayer at a lower level (e.g. RIV-package or condunctancefile within RIV-package
        /// </summary>
        public string Id2
        {
            get { return id2; }
            set
            {
                id2 = value;
                if (this.ResultFile != null)
                {
                    this.ResultFile.Filename = CreateResultFilename();
                }
            }
        }

        protected int ilay;
        public int ILay
        {
            get { return ilay; }
            set { ilay = value; }
        }

        protected int kper;
        public int KPER
        {
            get { return kper; }
            set { kper = value; }
        }

        protected DateTime? startDate;
        /// <summary>
        /// Model startdate (or null for steady-state)
        /// </summary>
        public DateTime? StartDate
        {
            get { return startDate; }
            set { startDate = value; }
        }

        protected IMODFile resultFile;
        public IMODFile ResultFile
        {
            set { resultFile = value; }
            get { return resultFile; }
        }

        public Legend Legend
        {
            get { return ResultFile.Legend; }
            set
            {
                if (value != null)
                {
                    ResultFile.Legend = value.Copy();
                }
                else
                {
                    ResultFile.Legend = null;
                }
            }   // make a copy to ensure that a distinct legend object is used for different resultfiles
        }

        /// <summary>
        /// Description of the result layer, used for the metadata file
        /// </summary>
        protected string description;
        public string Description
        {
            get { return description; }
            set { description = value; }
        }

        /// <summary>
        /// Description of the process for creation this result layer, used for the metadata file
        /// </summary>
        protected string processDescription;
        public string ProcessDescription
        {
            get { return processDescription; }
            set { processDescription = value; }
        }

        protected string resolution;
        public string Resolution
        {
            get { return resolution; }
        }

        protected List<IMODFile> sourceFiles;
        /// <summary>
        /// A list of related iMOD sourcefiles
        /// </summary>
        public List<IMODFile> SourceFiles
        {
            get { return sourceFiles; }
        }

        protected string outputPath;
        public string OutputPath
        {
            get { return outputPath; }
            set { outputPath = value; }
        }

        protected long resultCount;
        public long ResultCount
        {
            get { return resultCount; }
            set { resultCount = value; }
        }

        protected long value1;
        public long Value1
        {
            get { return value1; }
            set { value1 = value; }
        }

        protected long value2;
        public long Value2
        {
            get { return value2; }
            set { value2 = value; }
        }

        protected bool isResultReAddingAllowed;
        /// <summary>
        /// Specifies if more than one result can be added at the same location
        /// </summary>
        protected bool IsResultReAddingAllowed
        {
            get { return isResultReAddingAllowed; }
            set { isResultReAddingAllowed = value; }
        }

        protected SortedDictionary<string, long> messageDictionary = null;
        /// <summary>
        /// Sorted Dictionary with resultmessages and number of occurrences: (shortDescription, count)
        /// </summary>
        public SortedDictionary<string, long> MessageDictionary
        {
            get { return messageDictionary; }
            set { messageDictionary = value; }
        }

        /// <summary>
        /// General constructor, for inheritance
        /// </summary>
        protected ResultLayer()
        {
            Initialize(null, null, 1, 0, null, string.Empty);
        }

        /// <summary>
        /// General constructor, for inheritance
        /// </summary>
        protected ResultLayer(string id, string id2, int kper, int ilay, DateTime? startDate, string outputPath)
        {
            Initialize(id, id2, kper, ilay, startDate, outputPath);
        }

        /// <summary>
        /// Constuctor for a ResultLayer with an underlying IDF ResultFile object
        /// </summary>
        /// <param name="id"></param>
        /// <param name="id2"></param>
        /// <param name="kper"></param>
        /// <param name="ilay"></param>
        /// <param name="startDate"></param>
        /// <param name="extent"></param>
        /// <param name="cellsize"></param>
        /// <param name="noDataValue"></param>
        /// <param name="outputPath"></param>
        /// <param name="legend"></param>
        /// <param name="useSparseGrid"></param>
        public ResultLayer(string id, string id2, int kper, int ilay, DateTime? startDate, Extent extent, float cellsize, float noDataValue, string outputPath, Legend legend = null, bool useSparseGrid = false)
            : this(id, id2, kper, ilay, startDate, outputPath)
        {
            InitializeIDF(extent, cellsize, noDataValue, legend, useSparseGrid);
        }

        /// <summary>
        /// General initialization
        /// </summary>
        /// <param name="id"></param>
        /// <param name="id2"></param>
        /// <param name="kper"></param>
        /// <param name="ilay"></param>
        /// <param name="startDate"></param>
        /// <param name="outputPath"></param>
        private void Initialize(string id, string id2, int kper, int ilay, DateTime? startDate, string outputPath)
        {
            this.id = id;
            this.id2 = id2;
            this.kper = kper;
            this.ilay = ilay;
            this.startDate = startDate;
            this.outputPath = outputPath;
            this.messageDictionary = new SortedDictionary<string, long>();
            this.sourceFiles = new List<IMODFile>();
            this.ResultCount = 0;
            this.Value1 = 0;
            this.Value2 = 0;
            this.isResultReAddingAllowed = false;
        }

        private void InitializeIDF(Extent extent, float cellsize, float noDataValue, Legend legend, bool useSparseGrid = false)
        {
            if (useSparseGrid)
            {
                this.resultFile = new SparseIDFFile(string.Empty, extent, cellsize, noDataValue, true);
                this.resultFile.ResetValues();
            }
            else
            {
                IDFFile idfFile = new IDFFile(string.Empty, extent, cellsize, noDataValue);
                idfFile.SetValues(0);
                this.resultFile = idfFile;
            }
            this.Legend = (legend != null) ? legend.Copy() : null;
            this.resultFile.Filename = CreateResultFilename();
            this.resultFile.UseLazyLoading = true;
            this.resolution = cellsize.ToString() + "m";
        }

        public bool HasResults()
        {
            return (ResultCount != 0);
        }

        protected virtual void AddResult(Result result)
        {
            ResultCount += result.ResultCount;
            Value1 += result.Value1;
            Value2 += result.Value2;

            // Store specified resultmessage in the messagedictionary of this resultlayer
            if (result.ShortDescription != null)
            {
                if (!messageDictionary.ContainsKey(result.ShortDescription))
                {
                    messageDictionary.Add(result.ShortDescription, 0);
                }
                messageDictionary[result.ShortDescription] += result.ResultCount;
            }
        }

        public void ResetResultValues()
        {
            if (resultFile is IDFFile)
            {
                ((IDFFile)resultFile).SetValues(0);
            }
            else if (resultFile is IPFFile)
            {
                ((IPFFile)resultFile).ResetValues();
            }
            else
            {
                throw new Exception("Unsupported iMOD-file '" + resultFile.GetType().ToString() + "' for ResultLayer");
            }
        }

        /// <summary>
        /// Remove unused legend classes, for which no values are present in the iMOD result file
        /// </summary>
        /// <param name="classLabelSubString">if specified, only classes with the specified substring in its label are checked for remova;</param>
        public virtual void CompressLegend(string classLabelSubString = null)
        {
            if (resultFile is IDFFile)
            {
                ((IDFLegend) resultFile.Legend).CompressLegend((IDFFile) resultFile, classLabelSubString);
            }
        }

        /// <summary>
        /// Writes this resultfile to disk 
        /// </summary>
        /// <param name="log">if specified, a note will be added to the log</param>
        public void WriteResultFile(Log log)
        {
            if (log != null)
            {
                log.AddInfo(resultCount.ToString() + " " + id2 + " " + ResultType.ToLower() + "s found." + " Writing " + ResultType.ToLower() + "file " + Path.GetFileName(ResultFile.Filename) + " ...", 2);
            }
            resultFile.WriteFile(CreateResultFileMetadata());
            resultFile.UseLazyLoading = iMODValidatorSettingsManager.Settings.UseLazyLoading;
            resultFile.ReleaseMemory(resultFile.UseLazyLoading);
        }

        protected Metadata CreateResultFileMetadata()
        {
            Metadata metadata = Model.CreateDefaultMetadata();
            metadata.Description = Description;
            string resultFilePath = string.Empty;
            if (ResultFile != null)
            {
                resultFilePath = resultFile.Filename;
                if (ResultFile.Legend != null)
                {
                    metadata.Description += "\r\n" + "\r\n" + ResultFile.Legend.ToLongString();
                }
            }
            if (ProcessDescription != null)
            {
                metadata.ProcessDescription = ProcessDescription;
            }
            metadata.Resolution = Resolution;
            metadata.Source = string.Empty;
            foreach (IMODFile sourceFile in sourceFiles)
            {
                if (metadata.Source.Length > 0)
                {
                    metadata.Source += "; ";
                }
                if (sourceFile != null)
                {
                    if (sourceFile.Filename != null)
                    {
                        double someDouble;
                        if (double.TryParse(sourceFile.Filename, out someDouble))
                        {
                            metadata.Source += "Constant value " + sourceFile.Filename;
                        }
                        else
                        {
                            string commonStringLeft = CommonUtils.GetCommonLeftSubstring(resultFilePath, sourceFile.Filename);
                            if ((commonStringLeft != null) && !commonStringLeft.Equals(string.Empty))
                            {
                                metadata.Source += sourceFile.Filename.Replace(commonStringLeft, string.Empty);
                            }
                        }
                    }
                }
            }

            return metadata;
        }

        protected string CreateResultFilename()
        {
            if (ResultFile == null)
            {
                throw new Exception("ResultLayer.CreateResultFilename cannot be called before ResultFile is defined.");
            }

            string kperString = string.Empty;
            if ((kper > 0) && (startDate != null))
            {
                kperString = "_" + Model.GetStressPeriodString(startDate, kper);
            }

            if (Id == null)
            {
                if (id2 == null)
                {
                    return Path.Combine(outputPath, "L" + ilay + "_" + ResultType.ToLower() + "s" + kperString + "." + ResultFile.Extension);
                }
                else
                {
                    return Path.Combine(outputPath, id2 + "_L" + ilay + "_" + ResultType.ToLower() + "s" + kperString + "." + ResultFile.Extension);
                }
            }
            else
            {
                if (id2 == null)
                {
                    return Path.Combine(FileUtils.EnsureFolderExists(outputPath, Id), "L" + ilay + "_" + ResultType.ToLower() + "s" + kperString + "." + ResultFile.Extension);
                }
                else
                {
                    return Path.Combine(FileUtils.EnsureFolderExists(outputPath, Id), id2 + "_L" + ilay + "_" + ResultType.ToLower() + "s" + kperString + "." + ResultFile.Extension);
                }
            }
        }

        /// <summary>
        /// Adds an imodfile to the list of sourcefiles, which is used for creating metadata
        /// If imodfile is null or already present, it is not added.
        /// </summary>
        /// <param name="sourceFile"></param>
        public void AddSourceFile(IMODFile sourceFile)
        {
            if (sourceFile != null)
            {
                if (!sourceFiles.Contains(sourceFile))
                {
                    sourceFiles.Add(sourceFile);
                }
            }
        }

        /// <summary>
        /// Adds imodfiles to the list of sourcefiles, which is used for creating metadata
        /// </summary>
        /// <param name="sourceFiles"></param>
        public virtual void AddSourceFiles(List<IMODFile> sourceFiles)
        {
            if (sourceFiles != null)
            {
                foreach (IMODFile sourcefile in sourceFiles)
                {
                    AddSourceFile(sourcefile);
                }
            }
        }

        /// <summary>
        /// Adds IDF-files to the list of sourcefiles, which is used for creating metadata
        /// </summary>
        /// <param name="sourceFiles"></param>
        public void AddSourceFiles(List<IDFFile> sourceFiles)
        {
            if (sourceFiles != null)
            {
                this.sourceFiles.AddRange(sourceFiles);
            }
        }

        /// <summary>
        /// Adds IPF-files to the list of sourcefiles, which is used for creating metadata
        /// </summary>
        /// <param name="sourceFiles"></param>
        public void AddSourceFiles(List<IPFFile> sourceFiles)
        {
            if (sourceFiles != null)
            {
                this.sourceFiles.AddRange(sourceFiles);
            }
        }

        public override string ToString()
        {
            return "(" + this.Id2 + "," + this.Id + "," + this.ResultType.ToString() + ",Ilay=" + this.ILay + ",KPER=" + this.kper + ")";
        }

        public bool Equals(ResultLayer other)
        {
            return ((this.ResultFile == null) || (other.ResultFile == null)
                        || (Path.GetFileName(this.ResultFile.Filename).ToUpper().Equals(Path.GetFileName(other.ResultFile.Filename).ToUpper())))
                   && this.kper.Equals(other.kper) && this.Id.Equals(other.Id) && this.id2.Equals(other.id2) && (this.ilay == other.ilay) && (this.KPER.Equals(other.KPER));
        }


        public void ReleaseMemory(bool isMemoryCollected = true)
        {
            if (resultFile != null)
            {
                resultFile.ReleaseMemory();
                if (isMemoryCollected)
                {
                    GC.Collect();
                }
            }
        }

        public virtual ResultLayerStatistics CreateResultLayerStatistics(string resultFilename, long resultLocationCount)
        {
            ResultLayerStatistics resultLayerStatistics = new ResultLayerStatistics(this.ResultType, resultFilename, this.resultCount, resultLocationCount, this.value1, this.value2);
            resultLayerStatistics.MessageCountDictionary = this.MessageDictionary;
            return resultLayerStatistics;
        }

        public abstract ResultLayer Copy();
    }
}
