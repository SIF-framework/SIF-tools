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
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD
{
    /// <summary>
    /// Class for reading, merging or writing iMOD MET-files
    /// </summary>
    public class Metadata
    {
        /// <summary>
        /// Encoding used for writing metadata files
        /// </summary>
        public static Encoding Encoding = Encoding.Default;

        /// <summary>
        /// Full Filename for MET-file with this metadata. If not defined, it is derived from IMODFilename
        /// </summary>
        public string METFilename { get; set; }

        /// <summary>
        /// Filename (without path) of the iMOD-file that this metadata describes
        /// </summary>
        public string IMODFilename { get; set; }

        /// <summary>
        /// Path to the iMOD-file that this metadata describes
        /// </summary>
        public string Location { get; set; }

        /// <summary>
        /// Date that the iMOD-file is published. If not defined the current date is used.
        /// </summary>
        public DateTime? PublicationDate { get; set; }

        /// <summary>
        /// Version of the iMOD-file 
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Version (and/or name) of the model that the iMOD-file is used for
        /// </summary>
        public string Modelversion { get; set; }

        /// <summary>
        /// Description about the contents of the iMOD-file
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Name of producer of the iMOD-file 
        /// </summary>
        public string Producer { get; set; }

        /// <summary>
        /// Type of the iMOD-file, e.g. IDF, IPF, GEN, ISG.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Unit of the contents of the iMOD-file 
        /// </summary>
        public string Unit { get; set; }

        /// <summary>
        /// Resolution of the iMOD-file (or cellsize if equal for x and y)
        /// </summary>
        public string Resolution { get; set; }

        /// <summary>
        /// Source file(s) that were used for the production of this iMOD-file
        /// </summary>
        public string Source { get; set; }

        /// <summary>
        /// Description about the process for creation of the iMOD-file 
        /// </summary>
        public string ProcessDescription { get; set; }

        /// <summary>
        /// Scale that the iMOD-file is produced for
        /// </summary>
        public string Scale { get; set; }

        /// <summary>
        /// Name of the organisation that can be contacted about the iMOD-file 
        /// </summary>
        public string Organisation { get; set; }

        /// <summary>
        /// Website URL of the organisation that can be contacted about the iMOD-file 
        /// </summary>
        public string Website { get; set; }

        /// <summary>
        /// Name(s) of the person(s) that can be contacted about the iMOD-file 
        /// </summary>
        public string Contact { get; set; }

        /// <summary>
        /// Email address(es) of the person(s) that can be contacted about the iMOD-file  
        /// </summary>
        public string Emailaddress { get; set; }

        /// <summary>
        /// Other information about the iMOD-file
        /// </summary>
        public string Other { get; set; }

        /// <summary>
        /// The language that is used for writing the fieldnames of this metadata file
        /// </summary>
        public MetadataLanguage MetadataLanguage { get; set; }

        /// <summary>
        /// Settings for reading/writing metadata, i.e. formats, field names per language 
        /// </summary>
        public MetadataSettings MetadataSettings { get; set; }

        /// <summary>
        /// Creates an empty metadata object 
        /// </summary>
        public Metadata()
        {
            METFilename = string.Empty;
            IMODFilename = string.Empty;
            Location = string.Empty;
            PublicationDate = null;
            Version = string.Empty;
            Modelversion = string.Empty;
            Description = string.Empty;
            Producer = string.Empty;
            Type = string.Empty;
            Unit = string.Empty;
            Resolution = string.Empty;
            Source = string.Empty;
            ProcessDescription = string.Empty;
            Scale = string.Empty;
            Organisation = string.Empty;
            Website = string.Empty;
            Contact = string.Empty;
            Emailaddress = string.Empty;
            Other = string.Empty;
            MetadataLanguage = MetadataLanguage.English;
            MetadataSettings = new MetadataSettings();
        }

        /// <summary>
        /// Creates a metadata object with the spefied description
        /// </summary>
        /// <param name="description"></param>
        public Metadata(string description)
            : this()
        {
            this.Description = description;
        }

        /// <summary>
        /// Creates a metadata object for the spefied filename, date and description
        /// </summary>
        /// <param name="iMODFilename"></param>
        /// <param name="publicationDate"></param>
        /// <param name="description"></param>
        public Metadata(string iMODFilename, DateTime? publicationDate, string description)
            : this()
        {
            this.IMODFilename = Path.GetFileName(iMODFilename);
            this.Location = Path.GetDirectoryName(iMODFilename);
            this.PublicationDate = publicationDate;
            this.Description = description;
        }

        /// <summary>
        /// Write a MET-file for the defined METFilename and metadata of the specified object. An existing file is overwritten.
        /// </summary>
        /// <param name="metadata"></param>
        public static void WriteMetaFile(Metadata metadata)
        {
            metadata.WriteMetaFile();
        }

        /// <summary>
        /// Write a MET-file for the defined METFilename and metadata of this object. An existing file is overwritten.
        /// </summary>
        public void WriteMetaFile()
        {
            string publicationDateString = (PublicationDate == null) ? DateTime.Now.ToShortDateString() : PublicationDate.Value.ToShortDateString();
            string fieldFormat = GetFieldstring(MetadataSettings.FieldFormats, false);

            if ((METFilename == null) || METFilename.Equals(string.Empty))
            {
                if (IMODFilename != null)
                {
                    METFilename = Path.ChangeExtension(IMODFilename, "MET");
                }
                else
                {
                    throw new Exception("iMOD- or Metadata filename has not been set, metadata cannot be written");
                }
            }
            if ((IMODFilename == null) || IMODFilename.Equals(string.Empty))
            {
                IMODFilename = Path.GetFileNameWithoutExtension(METFilename);
            }

            string metString = "# " + GetFieldstring(MetadataSettings.GeneralInformationHeaderStrings) + "\r\n";
            metString += string.Format(fieldFormat + "{1}\r\n", GetFieldstring(MetadataSettings.FilenameStrings), Path.GetFileName(IMODFilename));
            metString += string.Format(fieldFormat + "{1}\r\n", GetFieldstring(MetadataSettings.LocationStrings), Location);
            metString += string.Format(fieldFormat + "{1}\r\n", GetFieldstring(MetadataSettings.PublicationDateStrings), publicationDateString);
            metString += string.Format(fieldFormat + "{1}\r\n", GetFieldstring(MetadataSettings.FileVersionStrings), Version);
            metString += string.Format(fieldFormat + "{1}\r\n", GetFieldstring(MetadataSettings.ModelVersionStrings), Modelversion);
            metString += string.Format(fieldFormat + "{1}\r\n", GetFieldstring(MetadataSettings.DescriptionStrings), Description.Replace("\n", "\n               "));
            metString += string.Format(fieldFormat + "{1}\r\n", GetFieldstring(MetadataSettings.ProducerStrings), Producer);
            metString += string.Format(fieldFormat + "{1}\r\n", GetFieldstring(MetadataSettings.TypeStrings), Type);
            metString += "\r\n" + "# " + GetFieldstring(MetadataSettings.DatasetDescriptionHeaderStrings) + "\r\n";
            metString += string.Format(fieldFormat + "{1}\r\n", GetFieldstring(MetadataSettings.UnitStrings), Unit);
            metString += string.Format(fieldFormat + "{1}\r\n", GetFieldstring(MetadataSettings.ResolutionStrings), Resolution);
            metString += string.Format(fieldFormat + "{1}\r\n", GetFieldstring(MetadataSettings.SourceStrings), Source);
            metString += string.Format(fieldFormat + "{1}\r\n", GetFieldstring(MetadataSettings.ProcessDescriptionStrings), ProcessDescription);
            metString += string.Format(fieldFormat + "{1}\r\n", GetFieldstring(MetadataSettings.ScaleStrings), Scale);
            metString += "\r\n" + "# " + GetFieldstring(MetadataSettings.AdministrationHeaderStrings) + "\r\n";
            metString += string.Format(fieldFormat + "{1}\r\n", GetFieldstring(MetadataSettings.OrganisationStrings), Organisation);
            metString += string.Format(fieldFormat + "{1}\r\n", GetFieldstring(MetadataSettings.WebsiteStrings), Website);
            metString += string.Format(fieldFormat + "{1}\r\n", GetFieldstring(MetadataSettings.ContactStrings), Contact);
            metString += string.Format(fieldFormat + "{1}\r\n", GetFieldstring(MetadataSettings.EmailStrings), Emailaddress);
            metString += "\r\n" + ((Other == null) ? "" : (Other + "\r\n"));

            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(METFilename, false, Encoding);
                sw.Write(metString);
            }
            catch (Exception ex)
            {
                throw new Exception("Error while writing metadatafile " + METFilename, ex);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                    sw = null;
                }
            }
        }

        /// <summary>
        /// Read metadata from an existing metadata file
        /// </summary>
        /// <param name="filename">Filename of MET-file</param>
        /// <returns>null, if MET-file does not exist</returns>
        public static Metadata ReadMetaFile(string filename)
        {
            Metadata metadata = new Metadata();

            if (Path.GetExtension(filename).ToLower().Equals(".met"))
            {
                metadata.METFilename = filename;
            }
            else
            {
                metadata.METFilename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + ".MET"); ;
            }

            if (!File.Exists(metadata.METFilename))
            {
                return null;
            }

            Stream stream = null;
            StreamReader sr = null;
            try
            {
                stream = File.OpenRead(metadata.METFilename);
                sr = new StreamReader(stream);

                string metString = sr.ReadToEnd();
                metadata.IMODFilename = GetMETStringValue(metString, MetadataSettings.Instance.FilenameStrings);
                metadata.Location = GetMETStringValue(metString, MetadataSettings.Instance.LocationStrings);

                string dateString = GetMETStringValue(metString, MetadataSettings.Instance.PublicationDateStrings);
                if ((dateString != null) && !dateString.Trim().Equals(string.Empty))
                {
                    metadata.PublicationDate = DateTime.Parse(dateString);
                }
                metadata.Version = GetMETStringValue(metString, MetadataSettings.Instance.FileVersionStrings);
                metadata.Modelversion = GetMETStringValue(metString, MetadataSettings.Instance.ModelVersionStrings);
                metadata.Description = GetMETStringValue(metString, MetadataSettings.Instance.DescriptionStrings, true);
                metadata.Producer = GetMETStringValue(metString, MetadataSettings.Instance.ProducerStrings);
                metadata.Type = GetMETStringValue(metString, MetadataSettings.Instance.TypeStrings);

                metadata.Unit = GetMETStringValue(metString, MetadataSettings.Instance.UnitStrings);
                metadata.Resolution = GetMETStringValue(metString, MetadataSettings.Instance.ResolutionStrings);
                metadata.Source = GetMETStringValue(metString, MetadataSettings.Instance.SourceStrings, true);
                metadata.ProcessDescription = GetMETStringValue(metString, MetadataSettings.Instance.ProcessDescriptionStrings, true);
                metadata.Scale = GetMETStringValue(metString, MetadataSettings.Instance.ScaleStrings);

                metadata.Organisation = GetMETStringValue(metString, MetadataSettings.Instance.OrganisationStrings);
                metadata.Website = GetMETStringValue(metString, MetadataSettings.Instance.WebsiteStrings);
                metadata.Contact = GetMETStringValue(metString, MetadataSettings.Instance.ContactStrings);
                metadata.Emailaddress = GetMETStringValue(metString, MetadataSettings.Instance.EmailStrings);

                metadata.Other = GetMETStringOtherValue(metString);

                return metadata;
            }
            catch (Exception ex)
            {
                throw new Exception("Error while reading metadatafile: " + metadata.METFilename, ex);
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
        /// Add metadata from another metdatadata object to this metadata object, identical metadata is not added
        /// </summary>
        /// <param name="otherMetadata"></param>
        public void MergeMetadata(Metadata otherMetadata)
        {
            // Overwrite following fields with new data
            IMODFilename = otherMetadata.IMODFilename;
            METFilename = otherMetadata.METFilename;
            Location = otherMetadata.Location;
            PublicationDate = otherMetadata.PublicationDate;
            if ((otherMetadata.Version == null) || otherMetadata.Version.Trim().Equals(string.Empty))
            {
                if ((Version != null) && !Version.Trim().Equals(string.Empty))
                {
                    // If new version is not defined and current version is an integer, increase with 1
                    if (int.TryParse(Version, out int versionInteger))
                    {
                        Version = (versionInteger + 1).ToString();
                    }
                }
                else
                {
                    Version = "1";
                }
            }
            else
            {
                Version = RemovePrefixes(otherMetadata.Version);
            }
            Modelversion = RemovePrefixes(otherMetadata.Modelversion);

            // merge other fields to olddata
            Description = AddMetadataValue(Description, otherMetadata.Description);
            Producer = AddMetadataValue(Producer, otherMetadata.Producer);
            Type = AddMetadataValue(Type, otherMetadata.Type);

            Unit = AddMetadataValue(Unit, otherMetadata.Unit);
            Resolution = AddMetadataValue(Resolution, otherMetadata.Resolution);
            Source = AddMetadataValue(Source, otherMetadata.Source);
            ProcessDescription = AddMetadataValue(ProcessDescription, otherMetadata.ProcessDescription);
            Scale = AddMetadataValue(Scale, otherMetadata.Scale);

            Organisation = AddMetadataValue(Organisation, otherMetadata.Organisation);
            Website = AddMetadataValue(Website, otherMetadata.Website);
            Contact = AddMetadataValue(Contact, otherMetadata.Contact);
            Emailaddress = AddMetadataValue(Emailaddress, otherMetadata.Emailaddress);

            Other = AddMetadataValue(Other, otherMetadata.Other);
        }

        /// <summary>
        /// Copy metadata of this object to a new Metadata object=
        /// </summary>
        /// <returns></returns>
        public Metadata Copy()
        {
            Metadata copiedMetadata = (Metadata)this.MemberwiseClone();

            // Do deep copy of underlying objects
            if (PublicationDate != null)
            {
                copiedMetadata.PublicationDate = new DateTime?(PublicationDate.Value);
            }

            return copiedMetadata;
        }

        private string GetFieldstring(List<string> list, bool isTrimmed = true)
        {
            int languageIndex = (int)MetadataLanguage;
            if (languageIndex >= list.Count)
            {
                languageIndex = list.Count - 1;
            }
            if (isTrimmed)
            {
                return list[languageIndex].Trim();
            }
            else
            {
                return list[languageIndex];
            }
        }

        private static string GetMETStringOtherValue(string metString)
        {
            string value = string.Empty;
            int idx = metString.LastIndexOf("\r\n\r\n");
            if (idx > 0)
            {
                value = metString.Substring(idx + "\r\n\r\n".Length);
            }

            if (value.Equals(string.Empty))
            {
                value = string.Empty;
            }

            return value;
        }

        private static string GetMETStringValue(string metString, List<string> fieldNames, bool allowMultiLines = false)
        {
            List<string> fieldformats = MetadataSettings.Instance.FieldFormats;
            string value = null;
            for (int idx = 0; idx < fieldNames.Count; idx++)
            {
                value = GetMETStringValue(metString, fieldNames[idx], fieldformats[idx], allowMultiLines);
                if (value != null)
                {
                    return value;
                }
            }
            return value;
        }

        private static string GetMETStringValue(string metString, string fieldName, string fieldformat, bool allowMultiLines = false)
        {
            string value = null;

            string fieldnameString = string.Format(fieldformat, fieldName);
            int idx = metString.IndexOf(fieldnameString);
            if (idx > 0)
            {
                int startIdx = idx + fieldnameString.Length - 1;
                // read until end-of-line (when multilines are not allowed)
                int endIdx = (metString.IndexOf("\r\n", startIdx + 1) < metString.IndexOf("\n", startIdx + 1)) ? metString.IndexOf("\r\n", startIdx + 1) : metString.IndexOf("\n", startIdx + 1);
                if (allowMultiLines)
                {
                    endIdx = metString.IndexOf("\r\n", startIdx + 1);
                    int endIdx2 = metString.IndexOf("\r\n\r\n", startIdx + 1);
                    if (endIdx2 > 0)
                    {
                        if ((endIdx <= 0) || ((endIdx > 0) && (endIdx2 < endIdx)))
                        {
                            endIdx = endIdx2;
                        }
                    }
                }

                if (endIdx <= 0)
                {
                    if (endIdx <= 0)
                    {
                        endIdx = metString.Length;
                    }
                }
                value = metString.Substring(startIdx, endIdx - startIdx).Trim();
            }

            return value;
        }

        private string AddMetadataValue(string value1, string value2)
        {
            // Overwrite value1 string if '=' symbol is prefixed before value2
            if (value2.StartsWith(MetadataSettings.OverwritePrefix) && (value2.Length >= 1))
            {
                return value2.Substring(1);
            }

            if ((value1 != null) && (!value1.Trim().Equals(string.Empty)))
            {
                if ((value2 != null) && (!value2.Trim().Equals(string.Empty)))
                {
                    if (value1.Trim().ToLower().Contains(value2.Trim().ToLower()))
                    {
                        return value1;
                    }
                    else
                    {
                        if (value1.Substring(value1.Length - 1).IndexOfAny(new char[] { '.', ';', ',' }) >= 0)
                        {
                            return value1.Trim() + " " + value2;
                        }
                        else
                        {
                            if (value2.Substring(0, 1).IndexOfAny(new char[] { ',', '/', '\\', '+' }) >= 0)
                            {
                                return value1 + value2;
                            }
                            else
                            {
                                return value1.Trim() + "; " + value2;
                            }
                        }
                    }
                }
                else
                {
                    return value1;
                }
            }
            else
            {
                return value2;
            }
        }

        /// <summary>
        /// Remove prefix 
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string RemovePrefixes(string value)
        {
            // remove dash (-) symbol when text is started with it
            if ((value.Length >= 1) && value.StartsWith(MetadataSettings.Instance.OverwritePrefix))
            {
                return value.Substring(1);
            }
            else
            {
                return value;
            }
        }

    }

    /// <summary>
    /// Available language for writing metadata fieldnames
    /// </summary>
    public enum MetadataLanguage
    {
        /// <summary>
        /// Dutch Language
        /// </summary>
        Dutch,

        /// <summary>
        /// English language
        /// </summary>
        English
    }

    /// <summary>
    /// Class for storing language and other settings for representation and visualisation of Metadata files
    /// </summary>
    public class MetadataSettings
    {
        // List with language strings per defined language, for each of the the headers in a Metadata file
        public List<string> GeneralInformationHeaderStrings;
        public List<string> DatasetDescriptionHeaderStrings;
        public List<string> AdministrationHeaderStrings;
        public List<string> FilenameStrings;
        public List<string> LocationStrings;
        public List<string> PublicationDateStrings;
        public List<string> FileVersionStrings;
        public List<string> ModelVersionStrings;
        public List<string> DescriptionStrings;
        public List<string> ProducerStrings;
        public List<string> TypeStrings;
        public List<string> UnitStrings;
        public List<string> ResolutionStrings;
        public List<string> SourceStrings;
        public List<string> ProcessDescriptionStrings;
        public List<string> ScaleStrings;
        public List<string> OrganisationStrings;
        public List<string> WebsiteStrings;
        public List<string> ContactStrings;
        public List<string> EmailStrings;

        /// <summary>
        /// Field format for each of the language string for formatting line in Metadata file
        /// </summary>
        public List<string> FieldFormats;

        /// <summary>
        /// Prefix before string to define that it existing metadata should be overwritten for that field
        /// </summary>
        public string OverwritePrefix { get; set; }

        /// <summary>
        /// Retrieve single instance object for MetadataSettings class
        /// </summary>
        protected static MetadataSettings instance = null;

        /// <summary>
        /// Retrieve single instance object for MetadataSettings class
        /// </summary>
        public static MetadataSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new MetadataSettings();
                }
                return instance;
            }
        }

        /// <summary>
        /// Create MetadataSettings object with default settings
        /// </summary>
        public MetadataSettings()
        {
            this.GeneralInformationHeaderStrings = new List<string>() { "Algemene informatie", "General Information" };
            this.DatasetDescriptionHeaderStrings = new List<string>() { "Beschrijving dataset", "Description Data" };
            this.AdministrationHeaderStrings = new List<string>() { "Administratie", "Administration" };
            this.FilenameStrings = new List<string>() { "Bestandsnaam", "Filename" };
            this.LocationStrings = new List<string>() { "Locatie", "Location" };
            this.PublicationDateStrings = new List<string>() { "Publicatie datum", "Publication Date" };
            this.FileVersionStrings = new List<string>() { "Versienr bestand", "Version Number" };
            this.ModelVersionStrings = new List<string>() { "Versienr model", "Modelversion" };
            this.DescriptionStrings = new List<string>() { "Beschrijving", "Comment" };
            this.ProducerStrings = new List<string>() { "Producent", "Producer" };
            this.TypeStrings = new List<string>() { "Type", "Type" };
            this.UnitStrings = new List<string>() { "Eenheid", "Unit" };
            this.ResolutionStrings = new List<string>() { "Resolutie", "Resolution" };
            this.SourceStrings = new List<string>() { "Herkomst/Bron", "Source" };
            this.ProcessDescriptionStrings = new List<string>() { "Procesbeschrijving", "Process description" };
            this.ScaleStrings = new List<string>() { "Toepassingsschaal", "Scale" };
            this.OrganisationStrings = new List<string>() { "Organisatie", "Organisation" };
            this.WebsiteStrings = new List<string>() { "Website", "Website" };
            this.ContactStrings = new List<string>() { "Contactpersoon", "Contactpersoon" };
            this.EmailStrings = new List<string>() { "E-mail adres", "Email address" };
            this.FieldFormats = new List<string>() { "- {0,-18}: ", "- {0,-19}: " };

            this.OverwritePrefix = "=";
        }
    }
}
