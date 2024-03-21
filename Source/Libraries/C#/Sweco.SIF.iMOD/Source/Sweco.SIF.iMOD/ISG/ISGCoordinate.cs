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
using Sweco.SIF.GIS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMOD.ISG
{
    public class ISGCoordinate : IEquatable<ISGCoordinate>
    {
        public float X;
        public float Y;

        protected ISGCoordinate() { }

        public ISGCoordinate(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }

        public bool IsContainedBy(Extent extent)
        {
            return ((X >= extent.llx) && (Y >= extent.lly) && (X <= extent.urx) && (Y <= extent.ury));
        }

        public float DistanceTo(ISGCoordinate otherCoordinate)
        {
            return (float)Math.Sqrt((otherCoordinate.X - this.X) * (otherCoordinate.X - this.X) + (otherCoordinate.Y - this.Y) * (otherCoordinate.Y - this.Y));
        }

        public float DistanceTo(float otherX, float otherY)
        {
            return (float)Math.Sqrt((otherX - this.X) * (otherX - this.X) + (otherY - this.Y) * (otherY - this.Y));
        }

        public bool Equals(ISGCoordinate other)
        {
            return X.Equals(other.X) && Y.Equals(other.Y);
        }
    }
}
