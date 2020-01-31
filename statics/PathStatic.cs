using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MapsetParser.statics
{
    public static class PathStatic
    {
        /// <summary> Returns the file path in its base form as seen by the game, optionally allowing
        /// extensions to be stripped or maintaining case. </summary>
        public static string ParsePath(string filePath, bool withoutExtension = false, bool retainCase = false)
        {
            if (filePath == null)
                return null;

            string trimmedPath = filePath.Replace("\"", "").Replace("\\", "/").Trim();
            if (!retainCase)
                trimmedPath = trimmedPath.ToLower();
            if (!withoutExtension)
                return trimmedPath;

            string strippedPath = trimmedPath.LastIndexOf(".") != -1 ? trimmedPath.Substring(0, trimmedPath.LastIndexOf(".")) : trimmedPath;
            return strippedPath;
        }

        /// <summary> Returns the file or folder name rather than its path. Takes the last split of "\\" and "/". </summary>
        public static string CutPath(string filePath) =>
            filePath.Split(new char[] { '\\', '/' }).Last();

        /// <summary> Returns the file path relative to another path, usually song path in most cases. </summary>
        public static string RelativePath(string filePath, string songPath) =>
            filePath.Replace("\\", "/").Replace(songPath.Replace("\\", "/") + "/", "");
    }
}
