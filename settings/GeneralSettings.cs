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

        public GeneralSettings(string aCode)
        {
            audioFileName      = GetValue(aCode, "AudioFilename");
            audioLeadIn        = float.Parse(GetValue(aCode, "AudioLeadIn"), CultureInfo.InvariantCulture);
            previewTime        = float.Parse(GetValue(aCode, "PreviewTime"), CultureInfo.InvariantCulture);
            countdown          = (Countdown)int.Parse(GetValue(aCode, "Countdown"));
            stackLeniency      = float.Parse(GetValue(aCode, "StackLeniency"), CultureInfo.InvariantCulture) * 10;
            mode               = (Beatmap.Mode)int.Parse(GetValue(aCode, "Mode"));
            letterbox          = GetValue(aCode, "LetterboxInBreaks") == "1";
            widescreenSupport  = GetValue(aCode, "WidescreenStoryboard") == "1";

            // optional
            countdownBeatOffset    = GetValue(aCode, "CountdownOffset") != null ?
                                        int.Parse(GetValue(aCode, "CountdownOffset")) : 0;
            skinPreference         = GetValue(aCode, "SkinPreference");

            storyInFrontOfFire     = GetValue(aCode, "StoryFireInFront") != null
                                        && GetValue(aCode, "StoryFireInFront") == "1";
            specialN1Style         = GetValue(aCode, "SpecialStyle") != null
                                        && GetValue(aCode, "SpecialStyle") == "1";
            epilepsyWarning        = GetValue(aCode, "EpilepsyWarning") != null
                                        && GetValue(aCode, "EpilepsyWarning") == "1";
            useSkinSprites         = GetValue(aCode, "UseSkinSprites") != null
                                        && GetValue(aCode, "UseSkinSprites") == "1";
        }

        private string GetValue(string aCode, string aKey)
        {
            string line = aCode.Split(new string[] { "\n" }, StringSplitOptions.None).FirstOrDefault(aLine => aLine.StartsWith(aKey));
            if (line == null)
                return null;

            return line.Substring(line.IndexOf(":") + 1).Trim();
        }
    }
}
