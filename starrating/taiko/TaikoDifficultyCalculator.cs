// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using MapsetParser.scoring;
using MapsetParser.starrating.preprocessing;
using MapsetParser.starrating.skills;
using MapsetParser.starrating.taiko.preprocessing;
using MapsetParser.starrating.taiko.preprocessing.Colour;
using MapsetParser.starrating.taiko.scoring;
using MapsetParser.starrating.taiko.skills;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MapsetParser.starrating.taiko
{
    public class TaikoDifficultyCalculator : DifficultyCalculator
    {
        private const double difficulty_multiplier = 1.35;

        public TaikoDifficultyCalculator(Beatmap beatmap)
            : base(beatmap)
        {
        }

        protected override Skill[] CreateSkills(Beatmap beatmap)
        {
            return new Skill[]
            {
                new Peaks(),
                new ScrollSpeed(),
            };
        }

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(Beatmap beatmap)
        {
            List<DifficultyHitObject> difficultyHitObjects = new List<DifficultyHitObject>();
            List<TaikoDifficultyHitObject> centreObjects = new List<TaikoDifficultyHitObject>();
            List<TaikoDifficultyHitObject> rimObjects = new List<TaikoDifficultyHitObject>();
            List<TaikoDifficultyHitObject> noteObjects = new List<TaikoDifficultyHitObject>();

            for (int i = 2; i < beatmap.hitObjects.Count; i++)
            {
                difficultyHitObjects.Add(
                    new TaikoDifficultyHitObject(
                        beatmap.hitObjects[i], beatmap.hitObjects[i - 1], beatmap.hitObjects[i - 2], difficultyHitObjects,
                        centreObjects, rimObjects, noteObjects, difficultyHitObjects.Count)
                );
            }

            TaikoColourDifficultyPreprocessor.ProcessAndAssign(difficultyHitObjects);

            return difficultyHitObjects;
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(Beatmap beatmap, Skill[] skills)
        {
            if (beatmap.hitObjects.Count == 0)
                return new TaikoDifficultyAttributes {};

            var combined = (Peaks)skills[0];

            double colourRating = combined.ColourDifficultyValue * difficulty_multiplier;
            double rhythmRating = combined.RhythmDifficultyValue * difficulty_multiplier;
            double staminaRating = combined.StaminaDifficultyValue * difficulty_multiplier;

            double combinedRating = combined.DifficultyValue() * difficulty_multiplier;
            double starRating = rescale(combinedRating * 1.4);

            HitWindows hitWindows = new TaikoHitWindows();
            hitWindows.SetDifficulty(beatmap.difficultySettings.overallDifficulty);

            TaikoDifficultyAttributes attributes = new TaikoDifficultyAttributes
            {
                StarRating = starRating,
                StaminaDifficulty = staminaRating,
                RhythmDifficulty = rhythmRating,
                ColourDifficulty = colourRating,
                PeakDifficulty = combinedRating,
                GreatHitWindow = hitWindows.WindowFor(HitResult.Great),
                MaxCombo = beatmap.hitObjects.Count(h => h is Circle),
                Skills = skills
            };

            return attributes;
        }

        /// <summary>
        /// Applies a final re-scaling of the star rating.
        /// </summary>
        /// <param name="sr">The raw star rating value before re-scaling.</param>
        private double rescale(double sr)
        {
            if (sr < 0) return sr;

            return 10.43 * Math.Log(sr / 8 + 1);
        }
    }
}