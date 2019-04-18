

using MapsetParser.statics;
using System.Globalization;

namespace MapsetParser.objects.events
{
    public class StoryHitsound
    {
        // Sample,15707,0,"drum-hitnormal.wav",60
        // type, time, layer, path, volume

        public double   time;
        public Layer    layer;
        public string   path;
        public float    volume;

        /// <summary> The path in lowercase without extension or quotationmarks. </summary>
        public string strippedPath;

        /// <summary> The layer the hit sound is audible on, for example only when passing a section if "Pass". </summary>
        public enum Layer
        {
            Background = 0,
            Fail = 1,
            Pass = 2,
            Foreground = 3
        }

        public StoryHitsound(string aCode)
        {
            time   = GetTime(aCode);
            layer  = GetLayer(aCode);
            path   = GetPath(aCode);
            volume = GetVolume(aCode);

            strippedPath = PathStatic.ParsePath(path, true);
        }

        // time
        private double GetTime(string aCode)
        {
            return double.Parse(aCode.Split(',')[1], CultureInfo.InvariantCulture);
        }

        // layer
        private Layer GetLayer(string aCode)
        {
            return (Layer)int.Parse(aCode.Split(',')[2]);
        }

        // path
        private string GetPath(string aCode)
        {
            return PathStatic.ParsePath(aCode.Split(',')[3], false, true);
        }

        // volume
        private float GetVolume(string aCode)
        {
            return float.Parse(aCode.Split(',')[4], CultureInfo.InvariantCulture);
        }
    }
}
