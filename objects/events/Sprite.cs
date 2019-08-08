using MapsetParser.statics;
using System.Globalization;
using System.Numerics;

namespace MapsetParser.objects.events
{
    public class Sprite
    {
        // Sprite,Foreground,Centre,"SB\whitenamebar.png",320,240
        // Sprite, layer, origin, filename, x offset, y offset

        public readonly int     layer;
        public readonly int     origin;
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
        private int GetLayer(string[] anArgs)
        {
            string argument = anArgs[1];
            int id =
                argument == "Background"    ? 0 :
                argument == "Fail"          ? 1 :
                argument == "Pass"          ? 2 :
                argument == "Foreground"    ? 3 :
                -1;

            // Throws an exception if not a number.
            if (id == -1)
                return int.Parse(argument);

            return id;
        }

        // origin
        private int GetOrigin(string[] anArgs)
        {
            string argument = anArgs[2];
            int id =
                argument == "TopLeft"      ? 0 :
                argument == "Centre"       ? 1 :
                argument == "CentreLeft"   ? 2 :
                argument == "TopRight"     ? 3 :
                argument == "BottomCentre" ? 4 :
                argument == "TopCentre"    ? 5 :
                argument == "Custom"       ? 6 :
                argument == "CentreRight"  ? 7 :
                argument == "BottomLeft"   ? 8 :
                argument == "BottomRight"  ? 9 :
                -1;

            if (id == -1)
                return int.Parse(argument);

            return id;
        }

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
