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
            circleSize         = GetValue(lines, "CircleSize", 0f, 18f);
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

        struct DiffRange
        {
            public readonly double lower;
            public readonly double middle;
            public readonly double upper;

            public DiffRange(double lower, double middle, double upper)
            {
                this.lower = lower;
                this.middle = middle;
                this.upper = upper;
            }
        }

        private double DifficultyRange(double difficulty, DiffRange range) =>
            DifficultyRange(difficulty, range.lower, range.middle, range.upper);

        private double DifficultyRange(double difficulty, double lower, double middle, double upper) =>
            difficulty < 5
                ? middle + (upper - middle) * (5 - difficulty) / 5
                : middle - (middle - lower) * (difficulty - 5) / 5;

        /// <summary> Returns the time from where the object begins fading in to where it is fully opaque. </summary>
        public double GetFadeInTime() =>
            DifficultyRange(approachRate, 450, 1200, 1800);

        /// <summary> Returns the time from where the object is fully opaque to where it is on the timeline. </summary>
        public double GetPreemptTime() =>
            DifficultyRange(approachRate, 300, 800, 1200);

        public enum HitType
        {
            Great300,
            Ok100,
            Meh50
        }

        private readonly Dictionary<HitType, DiffRange> hitRanges = new Dictionary<HitType, DiffRange>()
        {
            { HitType.Great300, new DiffRange(20, 50, 80)    },
            { HitType.Ok100,    new DiffRange(60, 100, 140)  },
            { HitType.Meh50,    new DiffRange(100, 150, 200) }
        };

        public double GetHitWindow(HitType hitType) =>
            DifficultyRange(overallDifficulty, hitRanges[hitType]);
    }
}
