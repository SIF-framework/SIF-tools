// Sweco.SIF.Common is part of SIF-basis, a framework by Sweco for iMOD-modelling
// Copyright(C) 2021 Sweco Nederland B.V.
// 
// All rights to this software and documentation, including intellectual
// property rights, are owned by Sweco Nederland B.V., except for third
// party code or libraries which are governed by their own license.
// 
// This file is part of Sweco.SIF.Common.
// 
// Sweco.SIF.Common is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// Sweco.SIF.Common is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with Sweco.SIF.Common. If not, see <https://www.gnu.org/licenses/>.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sweco.SIF.Common
{
    /// <summary>
    /// SIF utilities for file processing
    /// </summary>
    public class FileUtils
    {
        /// <summary>
        /// Adds a trailing backslash to a path string if not yet present in the path string
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string EnsureTrailingSlash(string path)
        {
            if (!(path.EndsWith(Path.DirectorySeparatorChar.ToString())))
            {
                path += Path.DirectorySeparatorChar;
            }
            return path;
        }

        /// <summary>
        /// Adds specified postfix at the end of the filename, before the extension. No underscores or other characters are added by this function
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="postfix"></param>
        /// <param name="maxFilenameLength"></param>
        /// <returns></returns>
        public static string AddFilePostFix(string filename, string postfix, int maxFilenameLength = 0)
        {
            if (filename != null)
            {
                string shortName = Path.GetFileNameWithoutExtension(filename) + postfix;
                if ((maxFilenameLength > 0) && (shortName.Length > maxFilenameLength))
                {
                    shortName = shortName.Substring(shortName.Length - 50);
                }

                string newFilename = Path.Combine(Path.GetDirectoryName(filename), shortName + Path.GetExtension(filename));
                return newFilename;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieve folder path from a path string that refers to either a directory or a file
        /// </summary>
        /// <param name="pathString"></param>
        /// <returns>"." for an input string without a directory part, or null for null-string input</returns>
        public static string GetFolderPath(string pathString)
        {
            if (pathString == null)
            {
                return null;
            }

            // First check if path is an already existing Directory or File
            if (Directory.Exists(pathString))
            {
                return pathString;
            }
            else if (File.Exists(pathString))
            {
                return Path.GetDirectoryName(pathString);
            }

            // Not an existing directory or file, analyse for file extension
            string outputFolder;
            string extension = Path.GetExtension(pathString);
            if (!extension.Equals(string.Empty) || ((pathString.IndexOfAny(Path.GetInvalidPathChars()) == -1) && File.Exists(pathString)))
            {
                outputFolder = Path.GetDirectoryName(pathString);
            }
            else
            {
                outputFolder = pathString;
            }

            if (outputFolder.Equals(string.Empty))
            {
                outputFolder = ".";
            }

            return outputFolder;
        }

        /// <summary>
        /// Retrieve filename from a path string that refers to either a directory or a file
        /// </summary>
        /// <param name="pathString"></param>
        /// <returns>null for null/empty-string input</returns>
        public static string GetFilename(string pathString)
        {
            if ((pathString == null) || pathString.Equals(string.Empty))
            {
                return null;
            }

            // First check if path is an already existing Directory or File
            if (Directory.Exists(pathString))
            {
                return null;
            }
            else if (File.Exists(pathString))
            {
                return Path.GetFileName(pathString);
            }

            // Not an existing directory or file, analyse for file extension
            string outputFilename = null;
            string extension = Path.GetExtension(pathString);
            if (!extension.Equals(string.Empty))
            {
                outputFilename = Path.GetFileName(pathString);
            }
            else if ((pathString.IndexOfAny(Path.GetInvalidPathChars()) == -1) && File.Exists(pathString))
            {
                outputFilename = Path.GetFileName(pathString);
            }
            return outputFilename;
        }

        /// <summary>
        /// Adds postfix "_#i" to specified filename if it exists already, where i is the lowest available index
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static string GetUniqueFilename(string filename)
        {
            int idx = 1;
            string corrFilename = filename;
            while (File.Exists(corrFilename))
            {
                idx++;
                corrFilename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename) + "_#" + idx + Path.GetExtension(filename));
            }
            return corrFilename;
        }

        /// <summary>
        /// Creates the folders in the specified path if not yet existing, otherwise nothing is done
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string EnsureFolderExists(string path)
        {
            if (!Directory.Exists(Path.GetDirectoryName(path)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
            }
            return path;
        }

        /// <summary>
        /// Creates the folders in the specified path and the specified subdirectory in that path if not yet existing, otherwise nothing is done
        /// </summary>
        /// <param name="path"></param>
        /// <param name="subdirname"></param>
        /// <returns></returns>
        public static string EnsureFolderExists(string path, string subdirname)
        {
            string subdirPath = Path.Combine(path, subdirname);
            if (!Directory.Exists(Path.GetDirectoryName(subdirPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(subdirPath));
            }
            return subdirPath;
        }

        /// <summary>
        /// Returns true if the specified path is a root path (e.g. "C:\") or a network path (e.g. "\\someserver\...")
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsRootOrNetworkPath(string path)
        {
            return IsRootPath(path) || IsNetworkPath(path);
        }

        /// <summary>
        /// Returns true if the specified path is a root path (e.g. "C:\" or "\")
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsRootPath(string path)
        {
            return ((path.Length == 3) && path.Substring(1, 2).Equals(":\\")) || ((path.Length == 1) && path.Equals("\\"));
        }

        /// <summary>
        /// Returns true if the specified path is a network path (e.g. "\\someserver\...")
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsNetworkPath(string path)
        {
            return path.StartsWith("\\\\");
        }

        /// <summary>
        /// Deletes the specified directory and optionally all subdirectories as well
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isrecursive"></param>
        public static void DeleteDirectory(string path, bool isrecursive)
        {
            string[] filenames = Directory.GetFiles(path);
            foreach (string filename in filenames)
            {
                try
                {
                    File.Delete(filename);
                }
                catch (Exception ex)
                {
                    throw new Exception("Could not delete " + Path.GetFileName(filename) + " from " + path, ex);
                }
            }

            if (isrecursive)
            {
                string[] subdirpaths = Directory.GetDirectories(path);
                foreach (string subdirpath in subdirpaths)
                {
                    DeleteDirectory(subdirpath, isrecursive);
                }
            }

            Directory.Delete(path);
        }

        /// <summary>
        /// Check if specified path string contains invalid symbols according to Path-class
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool HasInvalidPathCharacters(string path)
        {
            foreach (char c in Path.GetInvalidPathChars())
            {
                if (path.Contains(c))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Check if specified filename contains invalid symbols according to Path-class
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static bool HasInvalidFilenameCharacters(string filename)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                if (filename.Contains(c))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Retrieves the relative path for a given absolute filename, relative to the specified basePath
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="basePath"></param>
        /// <param name="isMatchCase">if true search for an exact match in case</param> 
        /// <returns></returns>
        public static string GetRelativePath(string filename, string basePath, bool isMatchCase = true)
        {
            string path;
            string matchingPath;
            int slashIdx;
            string leftOverBasePath;

            filename = Path.GetFullPath(filename);
            basePath = Path.GetFullPath(basePath);
            if (!isMatchCase)
            {
                filename = filename.ToLower();
                basePath = basePath.ToLower();
            }

            // Retrieve maximum first part of both paths that is equal
            slashIdx = -1;
            matchingPath = string.Empty;
            path = string.Empty;
            while ((path.Equals(string.Empty) || (slashIdx >= 0)) && basePath.StartsWith(path))
            {
                slashIdx = filename.IndexOf(Path.DirectorySeparatorChar, slashIdx + 1);
                matchingPath = path;
                if (slashIdx >= 0)
                {
                    path = filename.Substring(0, slashIdx);
                }
            }
            if (basePath.StartsWith(path))
            {
                matchingPath = path;
            }

            string relativeFilename = string.Empty;
            leftOverBasePath = basePath.Replace(matchingPath, string.Empty);
            slashIdx = leftOverBasePath.IndexOf(Path.DirectorySeparatorChar, 0);
            while (slashIdx >= 0)
            {
                relativeFilename += "..\\";
                slashIdx = leftOverBasePath.IndexOf(Path.DirectorySeparatorChar, slashIdx + 1);
            }

            relativeFilename += filename.Substring(matchingPath.Length + 1);

            return relativeFilename;
        }

        /// <summary>
        /// Read string from text file
        /// </summary>
        /// <param name="filename">full filename of file to read</param>
        /// <returns></returns>
        public static string ReadFile(string filename)
        {
            if (string.IsNullOrWhiteSpace(filename))
            {
                return null;
            }

            try
            {
                return File.ReadAllText(filename);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not read file: " + filename, ex);
            }
        }

        /// <summary>
        /// Write some text to specified. Creates new file or overwrite or appends to an existing file.
        /// </summary>
        /// <param name="filename">filename of file to write</param>
        /// <param name="text">text to write to file (an EOL is added)</param>
        /// <param name="append">true to append text to the file, false to overwrite an existing file</param>
        public static void WriteFile(string filename, string text, bool append = false)
        {
            if (!Directory.Exists(Path.GetDirectoryName(filename)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filename));
            }

            StreamWriter sw = null;
            try
            {
                sw = new StreamWriter(filename, append);
                sw.WriteLine(text);
            }
            catch (Exception ex)
            {
                throw new Exception("Could not write text to file '" + Path.GetFileName(filename) + "'", ex);
            }
            finally
            {
                if (sw != null)
                {
                    sw.Close();
                }
            }
        }

        /// <summary>
        /// Remove all empty subdirectories recursively from specified basepath
        /// </summary>
        /// <param name="basePath"></param>
        public static void RemoveEmptySubdirectories(string basePath)
        {
            foreach (string subdirectoryPath in Directory.GetDirectories(basePath))
            {
                RemoveEmptySubdirectories(subdirectoryPath);

                if ((Directory.GetFiles(subdirectoryPath).Length == 0) && (Directory.GetDirectories(subdirectoryPath).Length == 0))
                {
                    Directory.Delete(subdirectoryPath, false);
                }
            }
        }
    }
}
