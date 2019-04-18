using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapsetParser.statics
{
    public class PathStatic
    {
        /// <summary> Returns the file path in its base form as seen by the game, optionally allowing
        /// extensions to be stripped or maintaining case. </summary>
        public static string ParsePath(string aFilePath, bool aCanStrip = false, bool aKeepCase = false)
        {
            string trimmedPath = aFilePath.Replace("\"", "").Replace("\\", "/").Trim();
            if (!aKeepCase)
                trimmedPath = trimmedPath.ToLower();
            if (!aCanStrip)
                return trimmedPath;

            string strippedPath = trimmedPath.LastIndexOf(".") != -1 ? trimmedPath.Substring(0, trimmedPath.LastIndexOf(".")) : trimmedPath;
            return strippedPath;
        }

        /// <summary> Returns the file or folder name rather than its path. Takes the last split of "\\" and "/". </summary>
        public static string CutPath(string aFilePath) =>
            aFilePath.Split(new char[] { '\\', '/' }).Last();
    }
}
