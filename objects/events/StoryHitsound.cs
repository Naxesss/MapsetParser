

using MapsetParser.statics;
using System.Globalization;

namespace MapsetParser.objects.events
{
    public class StoryHitSound
    {
        // Sample,15707,0,"drum-hitnormal.wav",60
        // type, time, layer, path, volume

        public readonly double time;
        public readonly Layer  layer;
        public readonly string path;
        public readonly float  volume;

        /// <summary> The path in lowercase without extension or quotationmarks. </summary>
        public readonly string strippedPath;

        /// <summary> The layer the hit sound is audible on, for example only when passing a section if "Pass". </summary>
        public enum Layer
        {
            Background = 0,
            Fail = 1,
            Pass = 2,
            Foreground = 3
        }

        public StoryHitSound(string[] anArgs)
        {
            time   = GetTime(anArgs);
            layer  = GetLayer(anArgs);
            path   = GetPath(anArgs);
            volume = GetVolume(anArgs);

            strippedPath = PathStatic.ParsePath(path, true);
        }

        // time
        private double GetTime(string[] anArgs)
        {
            return double.Parse(anArgs[1], CultureInfo.InvariantCulture);
        }

        // layer
        private Layer GetLayer(string[] anArgs)
        {
            return (Layer)int.Parse(anArgs[2]);
        }

        // path
        private string GetPath(string[] anArgs)
        {
            return PathStatic.ParsePath(anArgs[3], false, true);
        }

        // volume (does not exist in file version 5)
        private float GetVolume(string[] anArgs)
        {
            if (anArgs.Length > 4)
                return float.Parse(anArgs[4], CultureInfo.InvariantCulture);

            // 100% volume is default
            return 1.0f;
        }
    }
}
