using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MapsetParser.settings
{
    public class DifficultySettings
    {
        /*
            HPDrainRate:6
            CircleSize:4.2
            OverallDifficulty:9
            ApproachRate:9.7
            SliderMultiplier:2.5
            SliderTickRate:2
         */
        // key:value

        public float hpDrain;
        public float circleSize;
        public float overallDifficulty;
        public float approachRate;

        public float sliderMultiplier;
        public float sliderTickRate;

        public DifficultySettings(string aCode)
        {
            hpDrain            = GetValue(aCode, "HPDrainRate");
            circleSize         = GetValue(aCode, "CircleSize");
            overallDifficulty  = GetValue(aCode, "OverallDifficulty");
            approachRate       = GetValue(aCode, "ApproachRate");

            sliderMultiplier   = GetValue(aCode, "SliderMultiplier");
            sliderTickRate     = GetValue(aCode, "SliderTickRate");
        }

        private float GetValue(string aCode, string aKey)
        {
            string line = aCode.Split(new string[] { "\n" }, StringSplitOptions.None).FirstOrDefault(aLine => aLine.StartsWith(aKey));
            if (line == null)
                return 0;

            return float.Parse(line.Substring(line.IndexOf(":") + 1).Trim(), CultureInfo.InvariantCulture);
        }

        /// <summary> Returns the radius of a circle or slider from the circle size. </summary>
        public float GetCircleRadius() =>
            32.0f * (1.0f - 0.7f * (circleSize - 5) / 5);
    }
}
