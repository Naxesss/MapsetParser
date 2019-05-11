using System;
using System.Collections.Generic;
using System.Text;

namespace MapsetParser.statics
{
    public static class ParserStatic
    {
        /// <summary> Returns the given function for each line in this section. </summary>
        public static IEnumerable<T> ParseSection<T>(string aCode, string aSectionName, Func<string, T> aFunc)
        {
            // Find the section, always from a line starting with [ and ending with ]
            // then ending on either end of file or an empty line.
            IEnumerable<string> lines = aCode.Split(new string[] { "\n" }, StringSplitOptions.None);

            bool read = false;
            foreach (string line in lines)
            {
                if (line.Trim().Length == 0)
                    read = false;

                if (read)
                    yield return aFunc(line);

                if (line.StartsWith("[" + aSectionName + "]"))
                    read = true;
            }
        }

        /// <summary> Returns all the lines in this section ran through the given function, excluding the section identifier (e.g. [HitObjects]). </summary>
        public static T GetSettings<T>(string aCode, string aSection, Func<string, T> aFunc)
        {
            StringBuilder stringBuilder = new StringBuilder("");

            IEnumerable<string> lines = ParseSection(aCode, aSection, aLine => aLine);
            foreach (string line in lines)
                stringBuilder.Append((stringBuilder.Length > 0 ? "\n" : "") + line);

            return aFunc(stringBuilder.ToString());
        }

        /// <summary> Same as <see cref="GetSettings"/> except does not return. </summary>
        public static void ApplySettings(string aCode, string aSection, Action<string> anAction)
        {
            StringBuilder stringBuilder = new StringBuilder("");

            IEnumerable<string> lines = ParseSection(aCode, aSection, aLine => aLine);
            foreach (string line in lines)
                stringBuilder.Append((stringBuilder.Length > 0 ? "\n" : "") + line);

            anAction(stringBuilder.ToString());
        }
    }
}
