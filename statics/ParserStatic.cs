using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapsetParser.statics
{
    public static class ParserStatic
    {
        /// <summary> Returns the given function for each line in this section. </summary>
        public static IEnumerable<T> ParseSection<T>(string[] aLines, string aSectionName, Func<string, T> aFunc)
        {
            // Find the section, always from a line starting with [ and ending with ]
            // then ending on either end of file or an empty line.
            bool read = false;
            foreach (string line in aLines)
            {
                if (line.Trim().Length == 0)
                    read = false;

                if (read)
                    yield return aFunc(line.Replace("\r", ""));

                if (line.StartsWith("[" + aSectionName + "]"))
                    read = true;
            }
        }

        /// <summary> Returns all the lines in this section ran through the given function, excluding the section identifier (e.g. [HitObjects]). </summary>
        public static T GetSettings<T>(string[] aLines, string aSection, Func<string[], T> aFunc)
        {
            IEnumerable<string> lines = ParseSection(aLines, aSection, aLine => aLine);

            return aFunc(lines.ToArray());
        }

        /// <summary> Same as <see cref="GetSettings"/> except does not return. </summary>
        public static void ApplySettings(string[] aLines, string aSection, Action<string[]> anAction)
        {
            IEnumerable<string> lines = ParseSection(aLines, aSection, aLine => aLine);

            anAction(lines.ToArray());
        }
    }
}
