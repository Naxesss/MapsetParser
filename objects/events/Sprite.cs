using MapsetParser.statics;
using System.Globalization;
using System.Numerics;

namespace MapsetParser.objects.events
{
    public class Sprite
    {
        // Sprite,Foreground,Centre,"SB\whitenamebar.png",320,240
        // Sprite, layer, origin, filename, x offset, y offset

        public int      layer;
        public int      origin;
        public string   path;
        public Vector2  offset;

        /// <summary> The path in lowercase without extension or quotationmarks. </summary>
        public string strippedPath;

        public Sprite(string aCode)
        {
            layer  = GetLayer(aCode);
            origin = GetOrigin(aCode);
            path   = GetPath(aCode);
            offset = GetOffset(aCode);

            strippedPath = PathStatic.ParsePath(path, true);
        }

        // layer
        private int GetLayer(string aCode)
        {
            string arg = aCode.Split(',')[1];
            int id = arg == "Background"    ? 0
                     : arg == "Fail"          ? 1
                     : arg == "Pass"          ? 2
                     : arg == "Foreground"    ? 3
                                                : -1;
            
            if(id == -1)
                try { return int.Parse(arg); } catch { }

            return id;
        }

        // origin
        private int GetOrigin(string aCode)
        {
            int id =
                aCode.Split(',')[2] == "TopLeft"         ? 0 :
                aCode.Split(',')[2] == "Centre"          ? 1 :
                aCode.Split(',')[2] == "CentreLeft"      ? 2 :
                aCode.Split(',')[2] == "TopRight"        ? 3 :
                aCode.Split(',')[2] == "BottomCentre"    ? 4 :
                aCode.Split(',')[2] == "TopCentre"       ? 5 :
                aCode.Split(',')[2] == "Custom"          ? 6 :
                aCode.Split(',')[2] == "CentreRight"     ? 7 :
                aCode.Split(',')[2] == "BottomLeft"      ? 8 :
                aCode.Split(',')[2] == "BottomRight"     ? 9 :
                                                           -1;

            if (id == -1)
                try { return int.Parse(aCode.Split(',')[2]); } catch { }

            return id;
        }

        // filename
        private string GetPath(string aCode)
        {
            // remove quotes for consistency, no way to add quotes manually anyway
            return PathStatic.ParsePath(aCode.Split(',')[3], false, true);
        }

        // offset
        private Vector2 GetOffset(string aCode)
        {
            if (aCode.Split(',').Length > 4)
                return new Vector2(
                    float.Parse(aCode.Split(',')[4], CultureInfo.InvariantCulture),
                    float.Parse(aCode.Split(',')[5], CultureInfo.InvariantCulture));
            else
                // default coordinates
                return new Vector2(320, 240);
        }
    }
}
