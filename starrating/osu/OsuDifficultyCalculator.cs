// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.starrating.osu.preprocessing;
using MapsetParser.starrating.osu.skills;
using MapsetParser.starrating.preprocessing;
using MapsetParser.starrating.skills;
using System;
using System.Collections.Generic;
using System.Linq;
using static MapsetParser.settings.DifficultySettings;

namespace MapsetParser.starrating.osu
{
    public class OsuDifficultyCalculator : DifficultyCalculator
    {
        private const double difficulty_multiplier = 0.0675;

        public OsuDifficultyCalculator(Beatmap beatmap)
            : base(beatmap)
        {
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(Beatmap beatmap, Skill[] skills)
        {
            if (beatmap.hitObjects.Count == 0)
                return new OsuDifficultyAttributes { Skills = skills };

            double aimRating = Math.Sqrt(skills[0].DifficultyValue()) * difficulty_multiplier;
            double speedRating = Math.Sqrt(skills[1].DifficultyValue()) * difficulty_multiplier;
            double starRating = aimRating + speedRating + Math.Abs(aimRating - speedRating) / 2;

            // Todo: These int casts are temporary to achieve 1:1 results with osu!stable, and should be removed in the future
            double hitWindowGreat = (int)beatmap.difficultySettings.GetHitWindow(HitType.Great300);
            double preempt = (int)beatmap.difficultySettings.GetPreemptTime();

            int maxCombo = beatmap.hitObjects.Count;
            maxCombo += beatmap.hitObjects.OfType<Slider>().Sum(s => s.GetSliderTickTimes().Count);

            int hitCirclesCount = beatmap.hitObjects.Count(h => h is Circle);

            return new OsuDifficultyAttributes
            {
                StarRating = starRating,
                AimStrain = aimRating,
                SpeedStrain = speedRating,
                ApproachRate = preempt > 1200 ? (1800 - preempt) / 120 : (1200 - preempt) / 150 + 5,
                OverallDifficulty = (80 - hitWindowGreat) / 6,
                MaxCombo = maxCombo,
                HitCircleCount = hitCirclesCount,
                Skills = skills
            };
        }

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(Beatmap beatmap)
        {
            // The first jump is formed by the first two hitobjects of the map.
            // If the map has less than two OsuHitObjects, the enumerator will not return anything.
            for (int i = 1; i < beatmap.hitObjects.Count; i++)
            {
                var lastLast = i > 1 ? beatmap.hitObjects[i - 2] : null;
                var last = beatmap.hitObjects[i - 1];
                var current = beatmap.hitObjects[i];

                yield return new OsuDifficultyHitObject(current, lastLast, last);
            }
        }

        protected override Skill[] CreateSkills(Beatmap beatmap) => new Skill[]
        {
            new Aim(),
            new Speed()
        };
    }
}