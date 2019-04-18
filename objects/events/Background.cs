using MapsetParser.statics;
using System.Globalization;
using System.Numerics;

namespace MapsetParser.objects.events
{
    public class Background
    {
        // 0,0,"apple is oral.jpg",0,0
        // Background, layer, filename, x offset, y offset

        public int          layer;
        public string       path;
        public Vector2?     offset;

        /// <summary> The path in lowercase without extension or quotationmarks. </summary>
        public string strippedPath;

        public Background(string aCode)
        {
            layer  = GetLayer(aCode);
            path   = GetPath(aCode);
            offset = GetOffset(aCode);

            strippedPath = PathStatic.ParsePath(path, true);
        }

        // layer
        private int GetLayer(string aCode)
        {
            return int.Parse(aCode.Split(',')[1]);
        }

        // filename
        private string GetPath(string aCode)
        {
            // remove quotes for consistency, no way to add quotes manually anyway
            return PathStatic.ParsePath(aCode.Split(',')[2], false, true);
        }

        // offset
        private Vector2? GetOffset(string aCode)
        {
            // doesn't exist in file version 9, for example
            if (aCode.Split(',').Length > 4)
                return new Vector2(
                    float.Parse(aCode.Split(',')[3], CultureInfo.InvariantCulture),
                    float.Parse(aCode.Split(',')[4], CultureInfo.InvariantCulture));
            else
                return null;
        }
    }
}
