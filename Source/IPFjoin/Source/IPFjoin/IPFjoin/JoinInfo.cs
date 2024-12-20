// IPFjoin is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of IPFjoin.
// 
// IPFjoin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// IPFjoin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with IPFjoin. If not, see <https://www.gnu.org/licenses/>.
using System.Collections.Generic;

namespace Sweco.SIF.IPFjoin
{
    public class JoinInfo
    {
        // list of key-values for each point in ipfFile1
        public IDictionary<string, List<int>> keyDictionary1;

        // list of key-values for each point in joinFile2
        public IDictionary<string, List<int>> keyDictionary2;
        public List<int> selectedColIndices2;

        public JoinInfo()
        {
            keyDictionary1 = null;
            keyDictionary2 = null;
            selectedColIndices2 = null;
        }
    }
}
