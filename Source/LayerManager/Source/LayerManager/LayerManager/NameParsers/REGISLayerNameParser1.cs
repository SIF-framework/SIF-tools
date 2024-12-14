// LayerManager is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of LayerManager.
// 
// LayerManager is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// LayerManager is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with LayerManager. If not, see <https://www.gnu.org/licenses/>.
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.LayerManager.NameParsers
{
    public class REGISLayerNameParser1 : LayerNameParser
    {
        private const string LayerStringSeperator = "-";

        public override bool IsValidLayerFilename(string regisFilename)
        {
            return Path.GetFileNameWithoutExtension(regisFilename).Contains("-");
        }

        public override string GetLayerPrefix(string regisLayerFilename)
        {
            if (regisLayerFilename == null)
            {
                return null;
            }

            int charIdx = Path.GetFileNameWithoutExtension(regisLayerFilename).IndexOf(LayerStringSeperator);
            if (charIdx < 0)
            {
                throw new ToolException("Unexpected format for REGIS-filename: " + Path.GetFileName(regisLayerFilename));
            }

            string prefix = Path.GetFileNameWithoutExtension(regisLayerFilename);
            int prefixIdx = prefix.IndexOf(LayerStringSeperator);
            prefix = prefix.Substring(0, prefixIdx);
            return prefix;
        }

        /// <summary>
        /// Parse REGIS filename with format: (LAYER(z|k|v|c|q)[_SUB]-(t|b|kh|kv|kd|c)-(ck|sk).IDF
        /// where _SUB is optional and SUB can be a substring of any length
        /// </summary>
        /// <param name="regisFilename"></param>
        /// <param name="lithologyCode"></param>
        /// <param name="index"></param>
        /// <param name="isAquifer"></param>
        /// <param name="isAquitard"></param>
        public override void ParseLayerFilename(string regisFilename, out string lithologyCode, out int index, out bool isAquifer, out bool isAquitard, out string layerName, out string substring)
        {
            index = 0;
            lithologyCode = null;
            substring = null;
            regisFilename = Path.GetFileName(regisFilename);

            int dashIndex1 = regisFilename.IndexOf("-");
            if (dashIndex1 > 0)
            {
                // Check for substring after layername
                int underscoreIndex = regisFilename.IndexOf("_");
                if (underscoreIndex > 0)
                {
                    substring = regisFilename.Substring(underscoreIndex + 1, (dashIndex1 - underscoreIndex - 1));
                    regisFilename = regisFilename.Remove(underscoreIndex, substring.Length + 1);
                    dashIndex1 = regisFilename.IndexOf("-");
                }

                layerName = regisFilename.Substring(0, dashIndex1);
                int dashIndex2 = regisFilename.IndexOf("-", dashIndex1 + 1);
                if (dashIndex2 > 0)
                {
                    string postfix = string.Empty;
                    if (ContainsDigits(layerName))
                    {
                        // check for postfix after layer unit index, e.g.KIk1a or KIz2b
                        int postfixIdx = dashIndex1 - 1;
                        while (postfixIdx > 0)
                        {
                            if (!int.TryParse(regisFilename.Substring(postfixIdx, 1), out int digit))
                            {
                                postfix = regisFilename.Substring(postfixIdx, 1) + postfix;
                            }
                            else
                            {
                                postfixIdx = 0;
                            }
                            postfixIdx--;
                        }
                    }
                    else
                    {
                        postfix = string.Empty;
                    }

                    index = 0;
                    int charIdx = dashIndex1 - 1 - postfix.Length;
                    int factor = 1;
                    while (charIdx > 0)
                    {
                        if (int.TryParse(regisFilename.Substring(charIdx, 1), out int digit))
                        {
                            index += factor * digit;
                            factor *= 10;
                        }
                        else
                        {
                            lithologyCode = regisFilename.Substring(charIdx, 1);
                            charIdx = 0;
                        }
                        charIdx--;
                    }
                }
                else
                {
                    throw new Exception("Filename doesn't have REGIS format: " + Path.GetFileName(regisFilename));
                }
            }
            else
            {
                throw new Exception("Filename doesn't have REGIS format: " + Path.GetFileName(regisFilename));
            }

            isAquifer = false;
            isAquitard = false;
            if (lithologyCode != null)
            {
                switch (lithologyCode.ToLower())
                {
                    case "z":
                        // zand
                        isAquifer = true;
                        break;
                    case "k":
                        // klei
                        isAquitard = true;
                        break;
                    case "c":
                        // complex
                        isAquifer = true;
                        isAquitard = true;
                        break;
                    case "q":
                        // kalk
                        isAquifer = true;
                        isAquitard = true;
                        break;
                    case "v":
                        // veen
                        isAquitard = true;
                        break;
                    case "b":
                        // bruinkool
                        isAquitard = true;
                        break;
                    default:
                        throw new Exception("Unknown REGIS lithology code in filename: " + regisFilename);
                }
            }
        }

        public override string GetTopFilePatternString(string layerTopFilename)
        {
            string[] patterns = CommonUtils.SplitQuoted(Properties.Settings.Default.REGISTopFilePatternsString, ',');
            foreach (string pattern in patterns)
            {
                if (layerTopFilename.Contains(pattern))
                {
                    return pattern;
                }
            }
            throw new Exception("REGIS layerfilename format not recognized: " + layerTopFilename);
        }
    }
}
