// Del2Bin is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Del2Bin.
// 
// Del2Bin is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Del2Bin is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Del2Bin. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Del2Bin
{
    public class DeleteUtils
    {
        // from: https://www.fluxbytes.com/csharp/delete-files-or-folders-to-recycle-bin-in-c/
        // see: https://docs.microsoft.com/en-us/dotnet/api/microsoft.visualbasic.fileio.filesystem.deletefile?view=netframework-4.8
        private const int FO_DELETE = 0x0003;
        private const int FOF_ALLOWUNDO = 0x0040;           // Preserve undo information, if possible. 
        private const int FOF_NOCONFIRMATION = 0x0010;      // Show no confirmation dialog box to the user

        // Struct which contains information that the SHFileOperation function uses to perform file operations. 
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            [MarshalAs(UnmanagedType.U4)]
            public int wFunc;
            public string pFrom;
            public string pTo;
            public short fFlags;
            [MarshalAs(UnmanagedType.Bool)]
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern int SHFileOperation(ref SHFILEOPSTRUCT FileOp);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns>Returns zero if successful; otherwise nonzero. Applications normally should simply check for zero or nonzero.</returns>
        public static int DeleteFileOrFolder(string path)
        {
            // A pointer to an SHFILEOPSTRUCT structure that contains information this function needs to carry out the specified operation. This parameter must contain a valid value that is not NULL. You are responsible for validating the value. If you do not validate it, you will experience unexpected results.
            // It is good practice to examine the value of the fAnyOperationsAborted member of the SHFILEOPSTRUCT. SHFileOperation can return 0 for success if the user cancels the operation. If you do not check fAnyOperationsAborted as well as the return value, you cannot know that the function accomplished the full task you asked of it and you might proceed under incorrect assumptions.
            // see: https://docs.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shfileoperationa
            SHFILEOPSTRUCT fileop = new SHFILEOPSTRUCT();
            fileop.wFunc = FO_DELETE;
            fileop.pFrom = path + '\0' + '\0';
            fileop.fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION;
            return SHFileOperation(ref fileop);
        }
    }
}
