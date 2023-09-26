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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.IFF
{
    /// <summary>
    /// Class for storing a list of IFF-particles
    /// </summary>
    public class ParticleList : List<int>
    {
        protected Dictionary<int, string> particleSourceIdDictionary;
        protected Dictionary<int, int> particleSourceIdxDictionary;

        /// <summary>
        /// Particlenumbers with corresponding source GEN-file polygon id
        /// </summary>
        public Dictionary<int, string> ParticleSourceIdDictionary
        {
            get { return particleSourceIdDictionary; }
            set { particleSourceIdDictionary = value; }
        }

        /// <summary>
        /// Particlenumbers with corresponding source polygon (one-based) index in GEN-file
        /// </summary>
        public Dictionary<int, int> ParticleSourceIdxDictionary
        {
            get { return particleSourceIdxDictionary; }
            set { particleSourceIdxDictionary = value; }
        }

        /// <summary>
        /// Constructor for new ParticleList objects
        /// </summary>
        public ParticleList() : base()
        {
            this.particleSourceIdDictionary = new Dictionary<int, string>();
            this.particleSourceIdxDictionary = new Dictionary<int, int>();
        }

        /// <summary>
        /// Constructor for new ParticleList objects
        /// </summary>
        /// <param name="particleList"></param>
        public ParticleList(IEnumerable<int> particleList) : base(particleList)
        {
            this.particleSourceIdDictionary = new Dictionary<int, string>();
            this.particleSourceIdxDictionary = new Dictionary<int, int>();
        }

        /// <summary>
        /// Constructor for new ParticleList objects
        /// </summary>
        /// <param name="particleList"></param>
        /// <param name="particleSourceIdxDictionary"></param>
        /// <param name="particleSourceIdDictionary"></param>
        public ParticleList(IEnumerable<int> particleList, Dictionary<int, int> particleSourceIdxDictionary, Dictionary<int, string> particleSourceIdDictionary) : base(particleList)
        {
            this.particleSourceIdDictionary = particleSourceIdDictionary;
            this.particleSourceIdxDictionary = particleSourceIdxDictionary;
        }
    }
}
