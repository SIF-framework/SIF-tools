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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Settings
{
    /// <summary>
    /// Class for storing Metadata settings
    /// </summary>
    public class MetadataSettings
    {
        public string GeneralInformationHeaderString;
        public string DatasetDescriptionHeaderString;
        public string AdministrationHeaderString;
        public string FilenameString;
        public string LocationString;
        public string PublicationDateString;
        public string FileVersionString;
        public string ModelVersionString;
        public string DescriptionString;
        public string ProducerString;
        public string TypeString;
        public string UnitString;
        public string ResolutionString;
        public string SourceString;
        public string ProcessDescriptionString;
        public string ScaleString;
        public string OrganisationString;
        public string WebsiteString;
        public string ContactString;
        public string EmailString;
        public string FieldFormat;

        public MetadataSettings()
        {
            this.GeneralInformationHeaderString = "Algemene informatie";
            this.DatasetDescriptionHeaderString = "Beschrijving dataset";
            this.AdministrationHeaderString = "Administratie";
            this.FilenameString = "Bestandsnaam";
            this.LocationString = "Locatie";
            this.PublicationDateString = "Publicatie datum";
            this.FileVersionString = "Versienr bestand";
            this.ModelVersionString = "Versienr model";
            this.DescriptionString = "Beschrijving";
            this.ProducerString = "Producent";
            this.TypeString = "Type";
            this.UnitString = "Eenheid";
            this.ResolutionString = "Resolutie";
            this.SourceString = "Herkomst/Bron";
            this.ProcessDescriptionString = "Procesbeschrijving";
            this.ScaleString = "Toepassingsschaal";
            this.OrganisationString = "Organisatie";
            this.WebsiteString = "Website";
            this.ContactString = "Contactpersoon";
            this.EmailString = "E-mail adres";
            this.FieldFormat = "- {0,-18}: ";
        }
    }
}
