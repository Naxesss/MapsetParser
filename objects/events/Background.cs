using MapsetParser.statics;
using System.Globalization;
using System.Numerics;

namespace MapsetParser.objects.events
{
    public class Background
    {
        // 0,0,"apple is oral.jpg",0,0
        // Background, offset (unused), filename, x offset, y offset

        public readonly string   path;
        public readonly Vector2? offset;

        /// <summary> The path in lowercase without extension or quotationmarks. </summary>
        public readonly string strippedPath;

        public Background(string[] anArgs)
        {
            path   = GetPath(anArgs);
            offset = GetOffset(anArgs);

            strippedPath = PathStatic.ParsePath(path, true);
        }

        // filename
        private string GetPath(string[] anArgs)
        {
            // remove quotes for consistency, no way to add quotes manually anyway
            return PathStatic.ParsePath(anArgs[2], false, true);
        }

        // offset
        private Vector2? GetOffset(string[] anArgs)
        {
            // doesn't exist in file version 9, for example
            if (anArgs.Length > 4)
                return new Vector2(
                    float.Parse(anArgs[3], CultureInfo.InvariantCulture),
                    float.Parse(anArgs[4], CultureInfo.InvariantCulture));
            else
                return null;
        }
    }
}
