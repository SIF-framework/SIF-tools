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
using Sweco.SIF.iMOD;
using Sweco.SIF.iMODValidator.Models;
using Sweco.SIF.iMODValidator.Models.Packages;
using Sweco.SIF.iMODValidator.Results;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Actions
{
    /// <summary>
    /// Base class for definition of iMODValidator actions, eg. checking
    /// </summary>
    public abstract class ValidatorAction : IEquatable<ValidatorAction>, IComparable<ValidatorAction>
    {
        protected static CultureInfo EnglishCultureInfo = new CultureInfo("en-GB", false);
        protected const string DivIMODFilesSubDir = "div-imodfiles";

        public virtual string ActionType
        {
            get { return "Action"; }
        }

        protected bool isActive;
        public bool IsActive
        {
            get { return isActive; }
            set { isActive = value; }
        }

        public virtual string Name
        {
            get { return Abbreviation + "-" + ActionType.ToLower(); }
        }

        public abstract string Abbreviation { get; }

        abstract public string Description { get; }

        abstract public void Run(Model model, ResultHandler resultHandler, Log log);

        protected void ReleaseMemory(IMODFile[] imodFiles, bool isMemoryCollected = true)
        {
            foreach (IMODFile imodFile in imodFiles)
            {
                if (imodFile != null)
                {
                    imodFile.ReleaseMemory();
                }
            }
            if (isMemoryCollected)
            {
                GC.Collect();
            }
        }

        protected void ReleaseMemory(Package[] packages, bool isMemoryCollected = true)
        {
            foreach (Package package in packages)
            {
                if (package != null)
                {
                    package.ReleaseMemory();
                }
            }
            if (isMemoryCollected)
            {
                GC.Collect();
            }
        }

        /// <summary>
        /// Resets parameters to initial values before starting a new run
        /// </summary>
        public virtual void Reset()
        {
        }

        public virtual string GetIMODFilesPath(Model model)
        {
            return Path.Combine(model.ToolOutputPath, "imodfiles");
        }

        public virtual string GetDivIMODFilesPath(Model model)
        {
            return Path.Combine(model.ToolOutputPath, DivIMODFilesSubDir);
        }

        public bool Equals(ValidatorAction other)
        {
            return this.Abbreviation.Equals(other.Abbreviation);
        }

        public int CompareTo(ValidatorAction other)
        {
            return this.Abbreviation.CompareTo(other.Abbreviation);
        }
    }
}
