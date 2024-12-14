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
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sweco.SIF.GIS;
using Sweco.SIF.iMOD.Legends;

namespace Sweco.SIF.iMOD
{
    /// <summary>
    /// Base class for iMOD-files. Ensure to create a static ReadFile(string filename, ...) method in subclasses. 
    /// See iMOD-manual for details of iMOD-files: https://oss.deltares.nl/nl/web/imod/user-manual.
    /// </summary>
    public abstract class IMODFile : IEquatable<IMODFile>
    {
        /// <summary>
        /// Encoding used for writing iMOD-files with text/ASCI-format
        /// </summary>
        public static Encoding Encoding = Encoding.Default;

        /// <summary>
        /// Formatting and other cultureInfo of English (UK) language
        /// </summary>
        public static CultureInfo EnglishCultureInfo { get; set; } = new CultureInfo("en-GB", false);

        /// <summary>
        /// Full filename of this iMOD-file
        /// </summary>
        public virtual string Filename { get; set; }

        /// <summary>
        /// Legend for this iMOD-file
        /// </summary>
        public virtual Legend Legend { get; set; }

        /// <summary>
        /// Metadata for this iMOD-file
        /// </summary>
        public virtual Metadata Metadata { get; set; }

        /// <summary>
        /// NoData-value for this iMOD-file (or NaN if not defined)
        /// </summary>
        public virtual float NoDataValue { get; set; }

        /// <summary>
        /// Minimum value for this iMOD-file (or NaN if not defined)
        /// </summary>
        public virtual float MinValue { get; set; }

        /// <summary>
        /// Maximum value for this iMOD-file (or NaN if not defined)
        /// </summary>
        public virtual float MaxValue { get; set; }

        /// <summary>
        /// Extent of the actual values in file or memory (if it difffers from the modified extent, the modification still has to be performed).
        /// Note: this extent can differ from the other two defined extents for this object, which are used to implement lazy loading.
        /// </summary>
        protected Extent extent;

        /// <summary>
        /// Extent as defined for modifying (clipping/enlarging) the fileExtent, or null if no modification is defined
        /// Note: this extent can differ from the other two defined extents for this object, which are all used to implement lazy loading.
        /// </summary>
        protected Extent modifiedExtent;

        /// <summary>
        /// Extent as defined in the corresponding iMOD-file, or null if no file yet exists
        /// Note: this extent can differ from the other two defined extents for this object, which are used to implement lazy loading.
        /// </summary>
        protected Extent fileExtent;

        /// <summary>
        /// Create empty iMOD-file
        /// </summary>
        protected IMODFile()
        {
            extent = null;
            fileExtent = null;
            modifiedExtent = null;
        }

        /// <summary>
        /// Create metadata for this iMOD-file, based on a specified metadata object
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="description">this description overwrites the description in the specified metadata object</param>
        /// <returns></returns>
        public virtual Metadata CreateMetadata(Metadata metadata, string description)
        {
            Metadata metadataCopy = metadata.Copy();
            metadataCopy.IMODFilename = Path.GetFileName(Filename);
            metadataCopy.METFilename = null;
            metadataCopy.Location = Path.GetDirectoryName(Filename);
            metadataCopy.PublicationDate = DateTime.Now;
            metadataCopy.Description = description;
            return metadataCopy;
        }

        /// <summary>
        /// Create simple metadata for this iMOD-file with the filename, current data and the specified description
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public virtual Metadata CreateMetadata(string description)
        {
            return new Metadata(Filename, DateTime.Now, description);
        }

        /// <summary>
        /// Determine equality up to the level of the filename
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool Equals(IMODFile other)
        {
            if (other == null)
            {
                return false;
            }

            // If the objects are equal, there's no need to check the rest
            if (object.Equals(this, other))
            {
                return true;
            }

            // If the objects are of a different type, there not equal
            if (!this.GetType().Equals(other.GetType()))
            {
                return false;
            }

            // Equality on this level is determined by the filename, not the contents
            return ((this.Filename != null) && this.Filename.Equals(other.Filename));
        }

        /// <summary>
        /// Return file extension of this iMOD-file without dot-prefix
        /// </summary>
        public abstract string Extension { get; }

        /// <summary>
        /// Returns extent of the values in file or memory (which may differ from the file extent)
        /// </summary>
        public virtual Extent Extent { get { return extent; } }

        /// <summary>
        /// Property that defines if lazy loading should be used
        /// </summary>
        public abstract bool UseLazyLoading { get; set; }

        /// <summary>
        /// Reset iMOD-file to empty/initial state
        /// </summary>
        public abstract void ResetValues();

        /// <summary>
        /// Release memory of this (lazy-loadable) object
        /// </summary>
        /// <param name="isMemoryCollected">if true, force system to collect memory (which may reduce performance)</param>
        public abstract void ReleaseMemory(bool isMemoryCollected = true);

        /// <summary>
        /// Create a legend for this iMOD-file with the specified description
        /// </summary>
        /// <param name="description"></param>
        /// <returns></returns>
        public abstract Legend CreateLegend(string description);

        /// <summary>
        /// Retrieve the number of actual (non-NoData) elements/items/features in this iMOD-file
        /// </summary>
        /// <returns></returns>
        public abstract long RetrieveElementCount();

        /// <summary>
        /// Creates a (deep) copy of this iMOD-file
        /// </summary>
        /// <param name="newFilename"></param>
        /// <returns></returns>
        public abstract IMODFile Copy(string newFilename = null);

        /// <summary>
        /// Clip this iMOD-file to the specified extent
        /// </summary>
        /// <param name="clipExtent"></param>
        /// <returns></returns>
        public abstract IMODFile Clip(Extent clipExtent);

        /// <summary>
        /// Write this iMOD-file to the filename that is defined in this object 
        /// </summary>
        /// <param name="metadata"></param>
        public abstract void WriteFile(Metadata metadata = null);

        /// <summary>
        /// Write this iMOD-file to the specified filename
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="metadata"></param>
        public abstract void WriteFile(string filename, Metadata metadata = null);

        /// <summary>
        /// Check if this iMOD-file is equal to another iMOD-file. If filenames are equal, the actual content is not compared as a default, except when specified so
        /// </summary>
        /// <param name="otherIMODFile"></param>
        /// <param name="comparedExtent"></param>
        /// <param name="isNoDataCompared">if true, the actual NoData-values are compared as well; otherwise different NoData-value definitions do not influence equality</param>
        /// <param name="isContentComparisonForced">if true the actual contents are compared</param>
        /// <returns></returns>
        public abstract bool HasEqualContent(IMODFile otherIMODFile, Extent comparedExtent, bool isNoDataCompared, bool isContentComparisonForced = false);

        /// <summary>
        /// Create a new iMOD-file object that represents the difference between specified other iMOD-file and this iMOD-file
        /// </summary>
        /// <param name="otherIMODFile"></param>
        /// <param name="outputPath"></param>
        /// <param name="isNoDataCompared">if true, NoData-values are compared using the defined NoDataCalculationValues</param>
        /// <param name="comparedExtent">the extent for which the difference should be calculated</param>
        /// <returns></returns>
        public abstract IMODFile CreateDifferenceFile(IMODFile otherIMODFile, string outputPath, float noDataCalculationValue = float.NaN, Extent comparedExtent = null);

        /// <summary>
        /// Return a legend with absolute difference that corresponds with this kind of iMOD-file
        /// </summary>
        /// <returns></returns>
        public abstract Legend CreateDifferenceLegend(Color? noDifferenceColor = null, bool isColorReversed = false);

        /// <summary>
        /// Return a legend with relative (factor) difference that corresponds with this kind of iMOD-file
        /// </summary>
        /// <returns></returns>
        public abstract Legend CreateDivisionLegend(Color? noDifferenceColor = null, bool isColorReversed = false);
    }
}
