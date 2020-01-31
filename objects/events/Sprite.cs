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
            Background = 0,
            Fail = 1,
            Pass = 2,
            Foreground = 3,
            Overlay = 4,
            Unknown
        }

        public enum Origin
        {
            TopLeft = 0,
            Centre = 1,
            CentreLeft = 2,
            TopRight = 3,
            BottomCentre = 4,
            TopCentre = 5,
            Custom = 6,
            CentreRight = 7,
            BottomLeft = 8,
            BottomRight = 9,
            Unknown
        }

        public readonly Layer   layer;
        public readonly Origin  origin;
        public readonly string  path;
        public readonly Vector2 offset;

        /// <summary> The path in lowercase without extension or quotationmarks. </summary>
        public readonly string strippedPath;

        public Sprite(string[] args)
        {
            layer  = GetLayer(args);
            origin = GetOrigin(args);
            path   = GetPath(args);
            offset = GetOffset(args);

            strippedPath = PathStatic.ParsePath(path, true);
        }

        // layer
        private Layer GetLayer(string[] anArgs) =>
            ParserStatic.GetStoryboardLayer(anArgs);

        // origin
        private Origin GetOrigin(string[] anArgs) =>
            ParserStatic.GetStoryboardOrigin(anArgs);

        /// <summary> Returns the file path which this sprite uses. Retains case sensitivity and extension. </summary>
        private string GetPath(string[] args) =>
            PathStatic.ParsePath(args[3], retainCase: true);

        /// <summary> Returns the positional offset from the top left corner of the screen, if specified,
        /// otherwise default (320, 240). </summary>
        private Vector2 GetOffset(string[] args)
        {
            if (args.Length > 4)
                return new Vector2(
                    float.Parse(args[4], CultureInfo.InvariantCulture),
                    float.Parse(args[5], CultureInfo.InvariantCulture));
            else
                // default coordinates
                return new Vector2(320, 240);
        }
    }
}
