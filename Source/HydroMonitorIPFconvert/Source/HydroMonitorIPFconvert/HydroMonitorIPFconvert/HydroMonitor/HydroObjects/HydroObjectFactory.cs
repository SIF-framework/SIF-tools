// HydroMonitorIPFconvert is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of HydroMonitorIPFconvert.
// 
// HydroMonitorIPFconvert is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// HydroMonitorIPFconvert is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with HydroMonitorIPFconvert. If not, see <https://www.gnu.org/licenses/>.
using System;

namespace Sweco.SIF.HydroMonitorIPFconvert
{
    /// <summary>
    /// Static class for creation of specific HydroObjects
    /// </summary>
    public class HydroObjectFactory
    {
        public static HydroObject CreateObject(HydroMonitorFile hydroMonitorFile, HydroObjectType objectType, string id)
        {
            switch (objectType)
            {
                case HydroObjectType.ObservationWell:
                    return new ObservationWell(hydroMonitorFile, id);
                case HydroObjectType.PumpingWell:
                    return new PumpingWell(hydroMonitorFile, id);
                case HydroObjectType.WeatherStation:
                    return new WeatherStation(hydroMonitorFile, id);
                case HydroObjectType.SurfaceWaterLevelGauge:
                    return new SurfaceWaterLevelGauge(hydroMonitorFile, id);
                default:
                    throw new Exception("Not supported object type: " + objectType.ToString());
            }
        }
    }
}
