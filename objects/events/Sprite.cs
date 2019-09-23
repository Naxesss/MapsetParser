using MapsetParser.statics;
using System.Globalization;
using System.Numerics;

namespace MapsetParser.objects.events
{
    public class Sprite
    {
        // Sprite,Foreground,Centre,"SB\whitenamebar.png",320,240
        // Sprite, layer, origin, filename, x offset, y offset

        public enum Layer
        {
            Background,
            Fail,
            Pass,
            Foreground,
            Overlay,
            Unknown
        }

        public enum Origin
        {
            TopLeft,
            Centre,
            CentreLeft,
            TopRight,
            BottomCentre,
            TopCentre,
            Custom,
            CentreRight,
            BottomLeft,
            BottomRight,
            Unknown
        }

        public readonly Layer   layer;
        public readonly Origin  origin;
        public readonly string  path;
        public readonly Vector2 offset;

        /// <summary> The path in lowercase without extension or quotationmarks. </summary>
        public readonly string strippedPath;

        public Sprite(string[] anArgs)
        {
            layer  = GetLayer(anArgs);
            origin = GetOrigin(anArgs);
            path   = GetPath(anArgs);
            offset = GetOffset(anArgs);

            strippedPath = PathStatic.ParsePath(path, true);
        }

        // layer
        private Layer GetLayer(string[] anArgs) =>
            ParserStatic.GetStoryboardLayer(anArgs);

        // origin
        private Origin GetOrigin(string[] anArgs) =>
            ParserStatic.GetStoryboardOrigin(anArgs);

        // filename
        private string GetPath(string[] anArgs)
        {
            // remove quotes for consistency, no way to add quotes manually anyway
            return PathStatic.ParsePath(anArgs[3], false, true);
        }

        // offset
        private Vector2 GetOffset(string[] anArgs)
        {
            if (anArgs.Length > 4)
                return new Vector2(
                    float.Parse(anArgs[4], CultureInfo.InvariantCulture),
                    float.Parse(anArgs[5], CultureInfo.InvariantCulture));
            else
                // default coordinates
                return new Vector2(320, 240);
        }
    }
}
