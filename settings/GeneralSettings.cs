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

        public GeneralSettings(string[] lines)
        {
            audioFileName   = GetValue(lines, "AudioFilename") ?? "audio.mp3";
            audioLeadIn     = float.Parse(GetValue(lines, "AudioLeadIn") ?? "0", CultureInfo.InvariantCulture);
            previewTime     = float.Parse(GetValue(lines, "PreviewTime") ?? "-1", CultureInfo.InvariantCulture);
            countdown       = (Countdown)int.Parse(GetValue(lines, "Countdown") ?? "0");

            // don't exist in file version 5
            stackLeniency       = float.Parse(GetValue(lines, "StackLeniency") ?? "0.7", CultureInfo.InvariantCulture) * 10;
            mode                = (Beatmap.Mode)int.Parse(GetValue(lines, "Mode") ?? "0");
            letterbox           = GetValue(lines, "LetterboxInBreaks") == "1";
            widescreenSupport   = GetValue(lines, "WidescreenStoryboard") == "1";

            // optional
            countdownBeatOffset    = int.Parse(GetValue(lines, "CountdownOffset") ?? "0");
            skinPreference         = GetValue(lines, "SkinPreference") ?? "";
            storyInFrontOfFire     = GetValue(lines, "StoryFireInFront") == "1";
            specialN1Style         = GetValue(lines, "SpecialStyle") == "1";
            epilepsyWarning        = GetValue(lines, "EpilepsyWarning") == "1";
            useSkinSprites         = GetValue(lines, "UseSkinSprites") == "1";
        }

        private string GetValue(string[] lines, string key)
        {
            string line = lines.FirstOrDefault(otherLine => otherLine.StartsWith(key));
            if (line == null)
                return null;

            return line.Substring(line.IndexOf(":") + 1).Trim();
        }
    }
}
