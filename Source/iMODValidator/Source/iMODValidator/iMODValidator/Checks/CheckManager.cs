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
using Sweco.SIF.iMODValidator.Actions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Checks
{
    /// <summary>
    /// Class for managing available iMODValidator checks, i.e. registering checks
    /// </summary>
    public class CheckManager : ActionManager
    {
        protected static CheckManager instance = null;
        public static CheckManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new CheckManager();
                }
                return instance;
            }
        }

        public List<ValidatorAction> Checks
        {
            get { return Actions; }
        }

        public void SortChecks()
        {
            base.SortActions();
        }

        public Check RetrieveCheck(Type type)
        {
            ValidatorAction action = RetrieveAction(type);
            if ((action != null) && (action is Check))
            {
                return (Check)action;
            }
            return null;
        }

        public override void RegisterDefaultActions()
        {
            RegisterAction(new OLFCheck());
            RegisterAction(new TOPBOTCheck());
            RegisterAction(new KVVKHVCheck());
            RegisterAction(new KDCCheck());
            RegisterAction(new ANICheck());
        }
    }
}
