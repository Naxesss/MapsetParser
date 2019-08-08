using MapsetParser.objects.events;
using MapsetParser.statics;
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

            string[] lines = aCode.Split(new string[] { "\n" }, StringSplitOptions.None);

            // substitute variables in the code before looking at the event part
            List<KeyValuePair<string, string>> substitutions = new List<KeyValuePair<string, string>>();
            ParserStatic.ApplySettings(lines, "Variables", aSectionLines =>
            {
                foreach (string line in aSectionLines)
                    if (line.StartsWith("$"))
                        substitutions.Add(new KeyValuePair<string, string>(
                            line.Split('=')[0].Trim(),
                            line.Split('=')[1].Trim()));
            });

            string substitutedCode = aCode;
            foreach (KeyValuePair<string, string> substitution in substitutions)
                substitutedCode = substitutedCode.Replace(substitution.Key, substitution.Value);

            string codeResult = substitutedCode.ToString();
            string[] linesResult = codeResult.Split(new string[] { "\n" }, StringSplitOptions.None);

            backgrounds    = GetEvents(linesResult, new List<string>() { "Background",   "0" }, anArgs => new Background(anArgs));
            videos         = GetEvents(linesResult, new List<string>() { "Video",        "1" }, anArgs => new Video(anArgs));
            breaks         = GetEvents(linesResult, new List<string>() { "Break",        "2" }, anArgs => new Break(anArgs));
            sprites        = GetEvents(linesResult, new List<string>() { "Sprite",       "4" }, anArgs => new Sprite(anArgs));
            storyHitSounds = GetEvents(linesResult, new List<string>() { "Sample",       "5" }, anArgs => new StoryHitSound(anArgs));
            animations     = GetEvents(linesResult, new List<string>() { "Animation",    "6" }, anArgs => new Animation(anArgs));
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

        private List<T> GetEvents<T>(string[] aLines, List<string> aTypes, Func<string[], T> aFunc)
        {
            // find all lines starting with any of aTypes in the event section
            List<T> types = new List<T>();
            ParserStatic.ApplySettings(aLines, "Events", aSectionLines =>
            {
                foreach (string line in aSectionLines)
                    if (aTypes.Any(aType => line.StartsWith(aType + ",")))
                        types.Add(aFunc(line.Split(',')));
            });
            return types;
        }
    }
}
