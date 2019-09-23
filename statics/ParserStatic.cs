using MapsetParser.objects.events;
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

        /// <summary> Returns the layer id from the given storyboard line arguments.
        /// This converts identifiers like "Background", "Fail", and "Pass" to their
        /// respective numerical value. </summary>
        public static Sprite.Layer GetStoryboardLayer(string[] anArgs)
        {
            string layerArg = anArgs[1];
            foreach (Sprite.Layer layer in Enum.GetValues(typeof(Sprite.Layer)))
            {
                string layerName = Enum.GetName(typeof(Sprite.Layer), layer);
                if (layerName == layerArg)
                    return layer;
            }

            return Sprite.Layer.Unknown;
        }

        /// <summary> Returns the origin id from the given storyboard line arguments.
        /// This converts identifiers like "TopLeft", "Centre", and "BottomRight" to
        /// their respective numerical value. </summary>
        public static Sprite.Origin GetStoryboardOrigin(string[] anArgs)
        {
            string originArg = anArgs[2];
            foreach (Sprite.Origin origin in Enum.GetValues(typeof(Sprite.Origin)))
            {
                string originName = Enum.GetName(typeof(Sprite.Origin), origin);
                if (originName == originArg)
                    return origin;
            }

            return Sprite.Origin.Unknown;
        }
    }
}
