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

        public DifficultySettings(string[] lines)
        {
            hpDrain            = GetValue(lines, "HPDrainRate", 0f, 10f);
            circleSize         = GetValue(lines, "CircleSize", 0f, 10f);
            overallDifficulty  = GetValue(lines, "OverallDifficulty", 0f, 10f);
            approachRate       = GetValue(lines, "ApproachRate", 0f, 10f);

            sliderMultiplier   = GetValue(lines, "SliderMultiplier", 0.4f, 3.6f);
            sliderTickRate     = GetValue(lines, "SliderTickRate", 0.5f, 8f);
        }

        private float GetValue(string[] lines, string key, float? min = null, float? max = null)
        {
            string line = lines.FirstOrDefault(otherLine => otherLine.StartsWith(key));
            if (line == null)
                return 0;

            float value = float.Parse(line.Substring(line.IndexOf(":") + 1).Trim(), CultureInfo.InvariantCulture);

            if (value < min) value = min.GetValueOrDefault();
            if (value > max) value = max.GetValueOrDefault();

            return value;
        }

        /// <summary> Returns the radius of a circle or slider from the circle size. </summary>
        public float GetCircleRadius() =>
            32.0f * (1.0f - 0.7f * (circleSize - 5) / 5);

        /// <summary> Returns the time from where the object begins fading in to where it is fully opaque.  </summary>
        public double GetFadeInTime() =>
            approachRate < 5
                ? 1200 + 600 * (5 - approachRate) / 5
                : 1200 - 750 * (approachRate - 5) / 5;

        /// <summary> Returns the time from where the object is fully opaque to where it is on the timeline.  </summary>
        public double GetPreemptTime() =>
            approachRate < 5
                ? 800 + 400 * (5 - approachRate) / 5
                : 800 - 500 * (approachRate - 5) / 5;
    }
}
