using MapsetParser.statics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

namespace MapsetParser.objects.events
{
    public class Animation
    {
        // Animation,Fail,Centre,"spr\scn1_spr3_2_b.png",320,280,2,40,LoopForever
        // Animation, layer, origin, filename, x offset, y offset, frame count, frame delay, loop type

        /// <summary> Whether the animation repeats or ends after going through all frames. </summary>
        public enum LoopType
        {
            LoopForever,
            LoopOnce
        }

        public readonly int      layer;
        public readonly int      origin;
        public readonly string   path;
        public readonly Vector2  offset;

        // animation-specific
        public readonly int      frameCount;
        public readonly double   frameDelay;
        public readonly LoopType loopType;

        public readonly List<string> framePaths;

        public Animation(string[] anArgs)
        {
            layer  = GetLayer(anArgs);
            origin = GetOrigin(anArgs);
            path   = GetPath(anArgs);
            offset = GetOffset(anArgs);

            // animation-specific
            frameCount = GetFrameCount(anArgs);
            frameDelay = GetFrameDelay(anArgs);
            loopType   = GetLoopType(anArgs);

            framePaths = GetFramePaths().ToList();
        }

        // layer
        private int GetLayer(string[] anArgs)
        {
            string argument = anArgs[1];
            int id =
                argument == "Background" ? 0 :
                argument == "Fail"       ? 1 :
                argument == "Pass"       ? 2 :
                argument == "Foreground" ? 3 :
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
                return new Vector2(float.Parse(anArgs[4], CultureInfo.InvariantCulture),
                                   float.Parse(anArgs[5], CultureInfo.InvariantCulture));
            else
                // default coordinates
                return new Vector2(320, 240);
        }

        // frame count
        private int GetFrameCount(string[] anArgs)
        {
            return int.Parse(anArgs[6]);
        }

        // frame delay
        private double GetFrameDelay(string[] anArgs)
        {
            return double.Parse(anArgs[7], CultureInfo.InvariantCulture);
        }

        // loop type
        private LoopType GetLoopType(string[] anArgs)
        {
            return anArgs[8] == "LoopForever"
                ? LoopType.LoopForever
                : LoopType.LoopOnce;
        }

        /// <summary> Returns all relative file paths for all frames used. </summary>
        public IEnumerable<string> GetFramePaths()
        {
            for (int i = 0; i < frameCount; ++i)
                yield return path.Insert(path.LastIndexOf("."), i.ToString());
        }
    }
}
