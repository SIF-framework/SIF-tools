// IDFmath is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IDFmath.
// 
// IDFmath is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IDFmath is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IDFmath. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using Sweco.SIF.iMOD.IDF;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.IDFmath
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

        protected enum OperatorEnum
        {
            Unknown,
            Add,
            Substract,
            Divide,
            Multiply,
            Average
        }

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
            Authors = new string[] { "Koen van der Hauw" };
            ToolPurpose = "SIF-tool for simple math operations on IDF-files\n"
                + "  the extent of the output IDF-file is the union of the input extents\n"
                + "  the cellsize of the output IDF-file is the minimum cell size";
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

            bool isOverwritten = settings.IsOverwrite;
            bool useNodataAsValue = settings.UseNodataAsValue;
            float[] noDataValues = settings.NoDataValues;

            IDFFile idfFile1 = IDFFile.ReadFile(settings.IDF1Filename);
            float par2Value = float.NaN;
            IDFFile idfFile2 = null;
            if (!float.TryParse(settings.IDF2Filename, NumberStyles.Float, EnglishCultureInfo, out par2Value))
            {
                idfFile2 = IDFFile.ReadFile(settings.IDF2Filename);
            }

            OperatorEnum operatorType = ParseOperatorString(settings.OperatorString);

            if (useNodataAsValue)
            {
                if (noDataValues[0].Equals(float.NaN))
                {
                    idfFile1.NoDataCalculationValue = idfFile1.NoDataValue;
                }
                else
                {
                    idfFile1.NoDataCalculationValue = noDataValues[0];
                }
                if (idfFile2 != null)
                {
                    if (noDataValues[1].Equals(float.NaN))
                    {
                        idfFile2.NoDataCalculationValue = idfFile2.NoDataValue;
                    }
                    else
                    {
                        idfFile2.NoDataCalculationValue = noDataValues[1];
                    }
                }
            }

            IDFFile idfFile3 = Calculate(idfFile1, operatorType, idfFile2, par2Value, settings);

            if (!File.Exists(settings.IDF3Filename) || settings.IsOverwrite)
            {
                idfFile3.WriteFile(settings.IDF3Filename);
            }
            else
            {
                throw new ToolException("Output IDF-file already exists and overwrite option has not been specified: " + settings.IDF3Filename);
            }

            ToolSuccessMessage = "Finished IDF-calculation: " + Path.GetFileName(settings.IDF3Filename) + " = " + Path.GetFileName(settings.IDF1Filename) + " " + settings.OperatorString + " " + Path.GetFileName(settings.IDF2Filename);

            return exitcode;
        }

        /// <summary>
        /// Calculate new IDF-file for idfFile1 and idfFile2 or par2Value and defined settings
        /// </summary>
        /// <param name="idfFile1"></param>
        /// <param name="operatorType"></param>
        /// <param name="idfFile2"></param>
        /// <param name="par2Value">when idfFile2 is null, this float value will be used</param>
        /// <param name="settings"></param>
        /// <returns></returns>
        protected virtual IDFFile Calculate(IDFFile idfFile1, OperatorEnum operatorType, IDFFile idfFile2, float par2Value, SIFToolSettings settings)
        {
            IDFFile idfFile3 = null;
            switch (operatorType)
            {
                case OperatorEnum.Add:
                    idfFile3 = (idfFile2 == null) ? idfFile1 + par2Value : idfFile1 + idfFile2;
                    break;
                case OperatorEnum.Substract:
                    idfFile3 = (idfFile2 == null) ? idfFile1 - par2Value : idfFile1 - idfFile2;
                    break;
                case OperatorEnum.Multiply:
                    idfFile3 = (idfFile2 == null) ? idfFile1 * par2Value : idfFile1 * idfFile2;
                    break;
                case OperatorEnum.Divide:
                    idfFile3 = (idfFile2 == null) ? idfFile1 / par2Value : idfFile1 / idfFile2; ;
                    break;
                case OperatorEnum.Average:
                    idfFile3 = (idfFile2 == null) ? (idfFile1 + par2Value) * 0.5f : (idfFile1 + idfFile2) * 0.5f; ;
                    break;
                default:
                    throw new Exception("Invalid operator");
            }
            return idfFile3;
        }

        protected OperatorEnum ParseOperatorString(string operatorString)
        {
            switch (operatorString)
            {
                case "+":
                    return OperatorEnum.Add;
                case "-":
                    return OperatorEnum.Substract;
                case "*":
                    return OperatorEnum.Multiply;
                case "/":
                    return OperatorEnum.Divide;
                case "avg":
                    return OperatorEnum.Average;
                default:
                    throw new Exception("Invalid operator found: " + operatorString);
            }
        }
    }
}
