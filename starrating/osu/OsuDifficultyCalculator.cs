// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.hitobjects.osu;
using MapsetParser.scoring;
using MapsetParser.starrating.preprocessing;
using MapsetParser.starrating.skills;
using MapsetParser.starrating.osu.preprocessing;
using MapsetParser.starrating.osu.scoring;
using MapsetParser.starrating.osu.skills;
using MapsetParser.starrating.utils;

namespace MapsetParser.starrating.osu
{
    public class OsuDifficultyCalculator : DifficultyCalculator
    {
        private const double PERFORMANCE_BASE_MULTIPLIER = 1.14; // Taken from https://github.com/ppy/osu/blob/master/osu.Game.Rulesets.Osu/Difficulty/OsuPerformanceCalculator.cs#L16
        private const double difficulty_multiplier = 0.0675;

        public OsuDifficultyCalculator(Beatmap beatmap)
            : base(beatmap)
        {
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(Beatmap beatmap, Skill[] skills)
        {
            if (beatmap.hitObjects.Count == 0)
                return new OsuDifficultyAttributes { };

            double aimRating = Math.Sqrt(skills[0].DifficultyValue()) * difficulty_multiplier;
            double aimRatingNoSliders = Math.Sqrt(skills[1].DifficultyValue()) * difficulty_multiplier;
            double speedRating = Math.Sqrt(skills[2].DifficultyValue()) * difficulty_multiplier;
            double speedNotes = ((Speed)skills[2]).RelevantNoteCount();

            double sliderFactor = aimRating > 0 ? aimRatingNoSliders / aimRating : 1;

            double baseAimPerformance = Math.Pow(5 * Math.Max(1, aimRating / 0.0675) - 4, 3) / 100000;
            double baseSpeedPerformance = Math.Pow(5 * Math.Max(1, speedRating / 0.0675) - 4, 3) / 100000;

            double basePerformance =
                Math.Pow(
                    Math.Pow(baseAimPerformance, 1.1) +
                    Math.Pow(baseSpeedPerformance, 1.1), 1.0 / 1.1
                );

            double starRating = basePerformance > 0.00001
                ? Math.Cbrt(PERFORMANCE_BASE_MULTIPLIER) * 0.027 * (Math.Cbrt(100000 / Math.Pow(2, 1 / 1.1) * basePerformance) + 4)
                : 0;

            double preempt = DifficultyUtils.DifficultyRange(beatmap.difficultySettings.approachRate, 1800, 1200, 450);
            double drainRate = beatmap.difficultySettings.hpDrain;
            int maxCombo = beatmap.GetMaxCombo();

            int hitCirclesCount = beatmap.hitObjects.Count(h => h is Circle);
            int sliderCount = beatmap.hitObjects.Count(h => h is Slider);
            int spinnerCount = beatmap.hitObjects.Count(h => h is Spinner);

            HitWindows hitWindows = new OsuHitWindows();
            hitWindows.SetDifficulty(beatmap.difficultySettings.overallDifficulty);

            double hitWindowGreat = hitWindows.WindowFor(HitResult.Great);

            OsuDifficultyAttributes attributes = new OsuDifficultyAttributes
            {
                StarRating = starRating,
                AimDifficulty = aimRating,
                SpeedDifficulty = speedRating,
                SpeedNoteCount = speedNotes,
                SliderFactor = sliderFactor,
                ApproachRate = preempt > 1200 ? (1800 - preempt) / 120 : (1200 - preempt) / 150 + 5,
                OverallDifficulty = (80 - hitWindowGreat) / 6,
                DrainRate = drainRate,
                MaxCombo = maxCombo,
                HitCircleCount = hitCirclesCount,
                SliderCount = sliderCount,
                SpinnerCount = spinnerCount,
                Skills = skills
            };

            return attributes;
        }

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(Beatmap beatmap)
        {
            List<DifficultyHitObject> objects = new List<DifficultyHitObject>();

            // The first jump is formed by the first two hitobjects of the map.
            // If the map has less than two OsuHitObjects, the enumerator will not return anything.
            for (int i = 1; i < beatmap.hitObjects.Count; i++)
            {
                var lastLast = i > 1 ? beatmap.hitObjects[i - 2] : null;
                objects.Add(new OsuDifficultyHitObject(beatmap.hitObjects[i], beatmap.hitObjects[i - 1], lastLast, objects, objects.Count));
            }

            return objects;
        }

        protected override Skill[] CreateSkills(Beatmap beatmap)
        {
            return new Skill[]
            {
                new Aim(true),
                new Aim(false),
                new Speed()
            };
        }
    }
}
