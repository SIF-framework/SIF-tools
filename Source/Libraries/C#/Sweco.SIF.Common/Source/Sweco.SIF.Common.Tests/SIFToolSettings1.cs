// Sweco.SIF.Common is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.Common.
// 
// Sweco.SIF.Common is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.Common is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.Common. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Common.Tests
{
    public class SIFToolSettings1 : SIFToolSettingsBase
    {
        public string Inpath { get; set; }
        public string Filter { get; set; }
        public string OutPath { get; set; }
        public string Excelsheet { get; set; }
        public bool IsOptionASelected { get; set; }
        public bool IsOptionBSelected { get; set; }
        public bool IsOptionCSelected { get; set; }
        public bool IsOptionDSelected { get; set; }

        public SIFToolSettings1(string[] args) : base(args)
        {
            Inpath = null;
            Filter = null;
            OutPath = null;
            Excelsheet = null;
            IsOptionASelected = false;
            IsOptionBSelected = false;
            IsOptionCSelected = false;
            IsOptionDSelected = false;
        }

        protected override void DefineToolSyntax()
        {
            // Define tool usage
            AddToolParameterDescription("inPath",     "Path to search for input files",   "C:\\Test\\Input");
            AddToolParameterDescription("filter",     "Filter to do ...",                 "*.XXX");
            AddToolParameterDescription("excelSheet", "Excelsheet to define input files", "somesheet.xlsx",   2);
            AddToolParameterDescription("outPath",    "Path to write result",             "C:\\Test\\Output", new int[] { 0, 2 });
            AddToolOptionDescription("a", "process action a", "/a:45,3", "Action 'a' is processed with parameters: {0}, {1} and {2}", new string[] { "t1", "t2" }, new string[] { "t3" }, new string[] { "XXX" }, 0);
            AddToolOptionDescription("b", "perform action b", "/b:naam", null,                                                        new string[] { "b1" },       null,                    null, new int[] { 0, 2 });
            AddToolOptionDescription("c", "handle action c",  "/c:cx",   null,                                                        new string[] { "c1" },       null,                    null, 0);
            AddToolOptionDescription("d", "delete d",         "/d:file", null,                                                        new string[] { "d1" },       null,                    null, 0);
        }

        //public override void LogSettings(Log log)
        //{
        //    if (Inpath != null)
        //    {
        //        log.AddMessage("Inpath: " + Inpath);
        //    }
        //    if (Filter != null)
        //    {
        //        log.AddMessage("Filter: " + Filter);
        //    }
        //    if (Excelsheet != null)
        //    {
        //        log.AddMessage("Excelsheet: " + Excelsheet);
        //    }
        //    if (OutPath != null)
        //    {
        //        log.AddMessage("OutPath: " + OutPath);
        //    }
        //    if (Inpath != null)
        //    {
        //        log.AddMessage("Inpath: " + Inpath);
        //    }
        //    if (IsOptionASelected)
        //    {
        //        log.AddMessage("option A specified", 1);
        //    }
        //    if (IsOptionBSelected)
        //    {
        //        log.AddMessage("option B specified", 1);
        //    }
        //    if (IsOptionCSelected)
        //    {
        //        log.AddMessage("option C specified", 1);
        //    }
        //    if (IsOptionDSelected)
        //    {
        //        log.AddMessage("option D specified", 1);
        //    }
        //}

        protected override bool ParseOption(string optionString, bool hasOptionParameters, string optionParameterString = null)
        {
            if (optionString.ToLower().Equals("a"))
            {
                IsOptionASelected = true;
            }
            else if (optionString.ToLower().Equals("b"))
            {
                IsOptionBSelected = true;
            }
            else if (optionString.ToLower().Equals("c"))
            {
                IsOptionDSelected = true;
            }
            else if (optionString.ToLower().Equals("d"))
            {
                IsOptionCSelected = true;
            }
            else if (optionString.ToLower().Equals("i"))
            {
                if (optionParameterString.Length > 0)
                {
                    string[] optionParameters = GetOptionParameters(optionParameterString);
                    if (!int.TryParse(optionParameters[0], out int IdColIdx))
                    {
                        throw new ToolException("Could not parse value for option 'i':" + optionParameters[0]);
                    }
                }
                else
                {
                    throw new ToolException("Please specify Id column index after 'i:': " + optionString);
                }
            }
            else if (optionString.ToLower().Equals("x"))
            {
                if (optionParameterString.Length > 0)
                {
                    string[] optionParameters = GetOptionParameters(optionParameterString);
                    if (optionParameters.Length == 0)
                    {
                        throw new ToolException("Please specify parameters after 'x:', or use only 'x' for defaults:" + optionString);
                    }
                    else
                    {
                        try
                        {
                            // Parse substrings for this option
                            //OptionXValue1 = float.Parse(optionParameters[0], NumberStyles.Float, englishCultureInfo);
                            //if (optionParameters.Length >= 2)
                            //{
                            //    OptionXValue2 = float.Parse(optionParameters[1], NumberStyles.Float, englishCultureInfo);
                            //}
                            //if (optionParameters.Length >= 3)
                            //{
                            //    OptionXValue3 = float.Parse(optionParameters[2], NumberStyles.Float, englishCultureInfo);
                            //}
                        }
                        catch (Exception)
                        {
                            throw new ToolException("Could not parse values for option 'x':" + optionParameterString);
                        }
                    }
                }
                // IsOptionXUsed = true;
            }
            else
            {
                return false;
            }

            return true;
        }

        protected override void ParseParameters(string[] parameters, out int groupIndex)
        {
            if (parameters.Length == 3)
            {
                // Parse syntax 1:
                Inpath = parameters[0];
                Filter = parameters[1];
                OutPath = parameters[2];
                groupIndex = 0;
            }
            else if (parameters.Length == 2)
            {
                // Parse syntax 2:
                Excelsheet = parameters[0];
                OutPath = parameters[1];
                groupIndex = 2;
            }
            else
            {
                throw new ToolException("Invalid number of parameters (" + parameters.Length + "), check tool usage");
            }
        }
    }
}
