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
using Sweco.SIF.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sweco.SIF.iMODValidator.Exceptions
{
    public static class ExceptionUtils
    {
        public static void HandleUnexpectedException(Exception ex, Log log, int logIndentLevel = 0)
        {
            if (ex.GetBaseException() is AbortException)
            {
                HandleAbortException(log, logIndentLevel);
            }
            else
            {
                string msg = Common.ExceptionHandler.GetExceptionChainString(ex);
                log.AddError("\r\n" + "Unexpected error: " + msg);
                log.AddInfo(Common.ExceptionHandler.GetStacktraceString(ex, true, logIndentLevel));

                MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            Environment.ExitCode = 1;
        }

        public static void HandleValidatorException(Exception ex, Log log, int logIndentLevel = 0, bool showMessageBox = true)
        {
            string msg = Common.ExceptionHandler.GetExceptionChainString(ex);
            log.AddError("\r\n" + msg);
            if (showMessageBox)
            {
                MessageBox.Show(msg, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            Environment.ExitCode = 1;
        }

        public static void HandleAbortException(Log log, int logIndentLevel = 0, bool showMessageBox = false)
        {
            string msg = "Abort was requested. Checks are cancelled.";
            log.AddInfo("\r\n" + msg);
            if (showMessageBox)
            {
                MessageBox.Show(msg, "Abort", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

    }
}
