using MapsetParser.objects;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MapsetParser.settings
{
    public class GeneralSettings
    {
        /*
            AudioFilename: audio.mp3
            AudioLeadIn: 0
            PreviewTime: 160876
            Countdown: 0
            SampleSet: Soft
            StackLeniency: 0.2
            Mode: 0
            LetterboxInBreaks: 0
            WidescreenStoryboard: 0
         */
        // key: value

        public string       audioFileName;
        public float        audioLeadIn;
        public float        previewTime;
        public Countdown    countdown;
        public float        stackLeniency;
        public Beatmap.Mode mode;
        public bool         letterbox;
        public bool         widescreenSupport;

        // optional
        public int      countdownBeatOffset;
        public string   skinPreference;
        public bool     storyInFrontOfFire;
        public bool     specialN1Style;
        public bool     epilepsyWarning;
        public bool     useSkinSprites;

        /// <summary> The speed at which countdown occurs, if any. Normal is 1 per beat. </summary>
        public enum Countdown
        {
            None = 0,
            Normal = 1,
            Half = 2,
            Double = 3
        }

        public GeneralSettings(string[] aLines)
        {
            audioFileName      = GetValue(aLines, "AudioFilename");
            audioLeadIn        = float.Parse(GetValue(aLines, "AudioLeadIn"), CultureInfo.InvariantCulture);
            previewTime        = float.Parse(GetValue(aLines, "PreviewTime"), CultureInfo.InvariantCulture);
            countdown          = (Countdown)int.Parse(GetValue(aLines, "Countdown"));
            stackLeniency      = float.Parse(GetValue(aLines, "StackLeniency"), CultureInfo.InvariantCulture) * 10;
            mode               = (Beatmap.Mode)int.Parse(GetValue(aLines, "Mode"));
            letterbox          = GetValue(aLines, "LetterboxInBreaks") == "1";
            widescreenSupport  = GetValue(aLines, "WidescreenStoryboard") == "1";

            // optional
            countdownBeatOffset    = GetValue(aLines, "CountdownOffset") != null ?
                                        int.Parse(GetValue(aLines, "CountdownOffset")) : 0;
            skinPreference         = GetValue(aLines, "SkinPreference") == "" ?
                                        null : GetValue(aLines, "SkinPreference");
            storyInFrontOfFire     = GetValue(aLines, "StoryFireInFront") != null
                                        && GetValue(aLines, "StoryFireInFront") == "1";
            specialN1Style         = GetValue(aLines, "SpecialStyle") != null
                                        && GetValue(aLines, "SpecialStyle") == "1";
            epilepsyWarning        = GetValue(aLines, "EpilepsyWarning") != null
                                        && GetValue(aLines, "EpilepsyWarning") == "1";
            useSkinSprites         = GetValue(aLines, "UseSkinSprites") != null
                                        && GetValue(aLines, "UseSkinSprites") == "1";
        }

        private string GetValue(string[] aLines, string aKey)
        {
            string line = aLines.FirstOrDefault(aLine => aLine.StartsWith(aKey));
            if (line == null)
                return null;

            return line.Substring(line.IndexOf(":") + 1).Trim();
        }
    }
}
