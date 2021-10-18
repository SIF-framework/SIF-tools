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
using Sweco.SIF.iMOD;
using Sweco.SIF.iMODValidator.Results;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Checks.CheckResults
{
    /// <summary>
    /// Class for reporting some details about a check
    /// </summary>
    public class CheckDetail : Result
    {
        private List<string> filenames;
        private string timeseriesFilename = null;
        private Timeseries timeseries = null;
        public const string TYPENAME = "Detail";

        public override string TypeName
        {
            get { return TYPENAME; }
        }

        public string ClassName
        {
            get { return ShortDescription; }
            set { ShortDescription = value; }
        }

        public int ClassNumber
        {
            get { return ResultValue; }
            set { ResultValue = value; }
        }

        public List<string> Filenames
        {
            get { return filenames; }
            set { filenames = value; }
        }

        public string Detail
        {
            get { return DetailedDescription; }
            set { DetailedDescription = value; }
        }

        private string moreDetail;
        public string MoreDetail
        {
            get { return moreDetail; }
            set { moreDetail = value; }
        }

        public string TimeseriesFilename
        {
            get { return timeseriesFilename; }
            set { timeseriesFilename = value; }
        }

        public Timeseries Timeseries
        {
            get { return timeseries; }
            set { timeseries = value; }
        }

        public CheckDetail(int classNumber, string className, IMODFile iMODFile, string detail, string moreDetail = null, string timeseriesFilename = null, Timeseries timeseries = null)
            : base(classNumber, className, detail)
        {
            this.filenames = new List<string>();
            if (iMODFile != null)
            {
                this.filenames.Add(Path.GetFileName(iMODFile.Filename));
            }
            this.timeseriesFilename = timeseriesFilename;
            this.timeseries = timeseries;
            this.moreDetail = moreDetail;
        }

        public CheckDetail(string className, IMODFile iMODFile, string detail, string moreDetail = null, string timeseriesFilename = null, Timeseries timeseries = null)
            : this(0, className, iMODFile, detail, moreDetail, timeseriesFilename, timeseries)
        {
        }

        public CheckDetail(CheckResult checkResult, IMODFile iMODFile, string detail, string moreDetail = null, string timeseriesFilename = null, Timeseries timeseries = null)
            : this(checkResult.ResultValue, checkResult.ShortDescription, iMODFile, detail, moreDetail, timeseriesFilename, timeseries)
        {
        }

        public CheckDetail(CheckDetail checkDetail, IMODFile iMODFile, string detail, string moreDetail = null, string timeseriesFilename = null, Timeseries timeseries = null)
            : this(checkDetail.ClassNumber, checkDetail.ClassName, iMODFile, detail, moreDetail, timeseriesFilename, timeseries)
        {
        }

        public CheckDetail(int classNumber, string className, IMODFile iMODFile, string detail, string moreDetail, float value)
            : base(classNumber, className, detail)
        {
            this.filenames = new List<string>();
            if ((iMODFile != null) && (iMODFile.Filename != null))
            {
                this.filenames.Add(Path.GetFileName(iMODFile.Filename));
            }
            this.timeseriesFilename = value.ToString("F3", englishCultureInfo);
            this.timeseries = null;
            this.moreDetail = moreDetail;
        }

        public CheckDetail(string className, IMODFile iMODFile, string detail, string moreDetail, float value)
            : this(0, className, iMODFile, detail, moreDetail, value)
        {
        }

        public CheckDetail(CheckResult checkResult, IMODFile iMODFile, string detail, string moreDetail, float value)
            : this(checkResult.ResultValue, checkResult.ShortDescription, iMODFile, detail, moreDetail, value)
        {
        }

        public CheckDetail(CheckDetail checkDetail, IMODFile iMODFile, string detail, string moreDetail, float value)
            : this(checkDetail.ClassNumber, checkDetail.ClassName, iMODFile, detail, moreDetail, value)
        {
        }

        public void AddIMODFile(IMODFile imodFile)
        {
            filenames.Add(Path.GetFileName(imodFile.Filename));
        }

        public void AddFilename(string filename)
        {
            filenames.Add(filename);
        }

        public void AddFilenames(List<string> filenames)
        {
            this.filenames.AddRange(filenames);
        }
    }
}
