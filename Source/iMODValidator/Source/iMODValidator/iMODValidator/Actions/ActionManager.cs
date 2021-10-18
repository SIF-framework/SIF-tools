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
using Sweco.SIF.iMODValidator.Exceptions;
using Sweco.SIF.iMODValidator.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.iMODValidator.Actions
{
    /// <summary>
    /// Base class for managing available iMODValidator actions, i.e. registering actions
    /// </summary>
    public abstract class ActionManager
    {
        protected bool isAbortRequested;
        public bool IsAbortRequested
        {
            get { return isAbortRequested; }
        }

        protected List<ValidatorAction> actions = null;
        public List<ValidatorAction> Actions
        {
            get
            {
                if (actions == null)
                {
                    actions = new List<ValidatorAction>();

                    try
                    {
                        RegisterDefaultActions();
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Could not register one or more actions", ex);
                    }
                }
                return actions;
            }
        }

        public void RegisterAction(ValidatorAction action)
        {
            Actions.Add(action);
        }

        public void RegisterActions(List<ValidatorAction> actions)
        {
            Actions.AddRange(actions);
        }

        public void UnRegisterAction(ValidatorAction action)
        {
            Actions.Remove(action);
        }

        public ValidatorAction RetrieveAction(Type type)
        {
            foreach (ValidatorAction action in Actions)
            {
                if (action.GetType().Equals(type))
                {
                    return action;
                }
            }
            return null;
        }

        protected virtual void SortActions()
        {
            try
            {
                if (Actions != null)
                {
                    Actions.Sort();
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Could not sort checks", ex);
            }
        }

        public void AbortActions()
        {
            isAbortRequested = true;
        }

        public void ResetAbortActions()
        {
            isAbortRequested = false;
        }

        public void CheckForAbort()
        {
            if (isAbortRequested)
            {
                isAbortRequested = false;
                throw new AbortException();
            }
        }

        public virtual string GetiMODFilesPath(Model model)
        {
            return ((Actions != null) && (Actions.Count > 0)) ? Actions[0].GetIMODFilesPath(model) : model.ToolOutputPath;
        }

        public abstract void RegisterDefaultActions();
    }
}
