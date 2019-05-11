using MapsetParser.objects.events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapsetParser.objects
{
    public class Osb
    {
        public string code;

        public readonly List<Background>     backgrounds;
        public readonly List<Video>          videos;
        public readonly List<Break>          breaks;
        public readonly List<Sprite>         sprites;
        public readonly List<StoryHitSound>  storyHitSounds;
        public readonly List<Animation>      animations;

        public Osb(string aCode)
        {
            code = aCode;

            // substitute variables in the code before looking at the event part
            List<KeyValuePair<string, string>> substitutions = new List<KeyValuePair<string, string>>();
            GetSettings(aCode, "Variables", aSection =>
            {
                foreach (string line in aSection.Split(new string[] { "\n" }, StringSplitOptions.None))
                    if (line.StartsWith("$"))
                        substitutions.Add(new KeyValuePair<string, string>(
                            line.Split('=')[0].Trim(),
                            line.Split('=')[1].Trim()));
                return aSection;
            });

            string substitutedCode = aCode;
            foreach (KeyValuePair<string, string> substitution in substitutions)
                substitutedCode = substitutedCode.Replace(substitution.Key, substitution.Value);

            string codeResult = substitutedCode.ToString();

            backgrounds    = GetEvents(codeResult, new List<string>() { "Background",   "0" }, aLine => new Background(aLine));
            videos         = GetEvents(codeResult, new List<string>() { "Video",        "1" }, aLine => new Video(aLine));
            breaks         = GetEvents(codeResult, new List<string>() { "Break",        "2" }, aLine => new Break(aLine));
            sprites        = GetEvents(codeResult, new List<string>() { "Sprite",       "4" }, aLine => new Sprite(aLine));
            storyHitSounds = GetEvents(codeResult, new List<string>() { "Sample",       "5" }, aLine => new StoryHitSound(aLine));
            animations     = GetEvents(codeResult, new List<string>() { "Animation",    "6" }, aLine => new Animation(aLine));
        }

        /// <summary> Returns whether the .osb file is actually used as a storyboard (or if it's just empty). </summary>
        public bool IsUsed()
        {
            return backgrounds     .Count > 0
                || videos          .Count > 0
                || breaks          .Count > 0
                || sprites         .Count > 0
                || storyHitSounds  .Count > 0
                || animations      .Count > 0;
        }

        /*
         *  Parser Methods
        */

        private IEnumerable<T> ParseSection<T>(string aCode, string aSectionName, Func<string, T> aFunc)
        {
            // find the section, always from a line starting with [ and ending with ]
            // then ending on either end of file or an empty line
            List<string> lines = aCode.Split(new string[] { "\n" }, StringSplitOptions.None).ToList();

            bool read = false;
            foreach (string line in lines)
            {
                if (line.Trim().Length == 0)
                    read = false;

                if (read)
                    yield return aFunc(line);

                if (line.StartsWith("[" + aSectionName + "]"))
                    read = true;
            }
        }

        private T GetSettings<T>(string aCode, string aSection, Func<string, T> aFunc)
        {
            StringBuilder stringBuilder = new StringBuilder("");
            
            List<string> lines = ParseSection(aCode, aSection, aLine => aLine).ToList();
            foreach (string line in lines)
                stringBuilder.Append((stringBuilder.Length > 0 ? "\n" : "") + line);

            return aFunc(stringBuilder.ToString());
        }

        private List<T> GetEvents<T>(string aCode, List<string> aTypes, Func<string, T> aFunc)
        {
            // find all lines starting with any of aTypes in the event section
            List<T> types = new List<T>();
            GetSettings(aCode, "Events", aSection =>
            {
                foreach (string line in aSection.Split(new string[] { "\n" }, StringSplitOptions.None))
                    if (aTypes.Any(aType => line.StartsWith(aType + ",")))
                        types.Add(aFunc(line));
                return aSection;
            });
            return types;
        }
    }
}
