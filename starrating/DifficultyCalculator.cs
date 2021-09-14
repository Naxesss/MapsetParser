// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MapsetParser.objects;
using MapsetParser.starrating;
using MapsetParser.starrating.preprocessing;
using MapsetParser.starrating.skills;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MapsetParser.starrating
{
    public abstract class DifficultyCalculator
    {
        /// <summary>
        /// The length of each strain section.
        /// </summary>
        protected virtual int SectionLength => 400;

        private readonly Beatmap beatmap;

        protected DifficultyCalculator(Beatmap beatmap)
        {
            this.beatmap = beatmap;
        }

        /// <summary>
        /// Calculates the difficulty of the beatmap using a specific mod combination.
        /// </summary>
        /// <param name="mods">The mods that should be applied to the beatmap.</param>
        /// <returns>A structure describing the difficulty of the beatmap.</returns>
        public DifficultyAttributes Calculate()
        {
            return Calculate(beatmap);
        }

        /// <summary>
        /// Calculates the difficulty of the beatmap using all mod combinations applicable to the beatmap.
        /// </summary>
        /// <returns>A collection of structures describing the difficulty of the beatmap for each mod combination.</returns>
        public IEnumerable<DifficultyAttributes> CalculateAll()
        {
            yield return Calculate();
        }

        private DifficultyAttributes Calculate(Beatmap beatmap)
        {
            var skills = CreateSkills(beatmap);

            if (!beatmap.hitObjects.Any())
                return CreateDifficultyAttributes(beatmap, skills);

            var difficultyHitObjects = SortObjects(CreateDifficultyHitObjects(beatmap)).ToList();

            double sectionLength = SectionLength;

            // The first object doesn't generate a strain, so we begin with an incremented section end
            double currentSectionEnd = Math.Ceiling(beatmap.hitObjects.First().time / sectionLength) * sectionLength;

            foreach (DifficultyHitObject h in difficultyHitObjects)
            {
                while (h.BaseObject.time > currentSectionEnd)
                {
                    foreach (Skill s in skills)
                    {
                        s.SaveCurrentPeak();
                        s.StartNewSectionFrom(currentSectionEnd);
                    }

                    currentSectionEnd += sectionLength;
                }

                foreach (Skill s in skills)
                    s.Process(h);
            }

            // The peak strain will not be saved for the last section in the above loop
            foreach (Skill s in skills)
                s.SaveCurrentPeak();

            return CreateDifficultyAttributes(beatmap, skills);
        }

        /// <summary>
        /// Sorts a given set of <see cref="DifficultyHitObject"/>s.
        /// </summary>
        /// <param name="input">The <see cref="DifficultyHitObject"/>s to sort.</param>
        /// <returns>The sorted <see cref="DifficultyHitObject"/>s.</returns>
        protected virtual IEnumerable<DifficultyHitObject> SortObjects(IEnumerable<DifficultyHitObject> input)
            => input.OrderBy(h => h.BaseObject.time);

        /// <summary>
        /// Creates <see cref="DifficultyAttributes"/> to describe beatmap's calculated difficulty.
        /// </summary>
        /// <param name="beatmap">The <see cref="IBeatmap"/> whose difficulty was calculated.</param>
        /// <param name="mods">The <see cref="Mod"/>s that difficulty was calculated with.</param>
        /// <param name="skills">The skills which processed the beatmap.</param>
        /// <param name="clockRate">The rate at which the gameplay clock is run at.</param>
        protected abstract DifficultyAttributes CreateDifficultyAttributes(Beatmap beatmap, Skill[] skills);

        /// <summary>
        /// Enumerates <see cref="DifficultyHitObject"/>s to be processed from <see cref="HitObject"/>s in the <see cref="IBeatmap"/>.
        /// </summary>
        /// <param name="beatmap">The <see cref="IBeatmap"/> providing the <see cref="HitObject"/>s to enumerate.</param>
        /// <param name="clockRate">The rate at which the gameplay clock is run at.</param>
        /// <returns>The enumerated <see cref="DifficultyHitObject"/>s.</returns>
        protected abstract IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(Beatmap beatmap);

        /// <summary>
        /// Creates the <see cref="Skill"/>s to calculate the difficulty of an <see cref="IBeatmap"/>.
        /// </summary>
        /// <param name="beatmap">The <see cref="IBeatmap"/> whose difficulty will be calculated.</param>
        /// <returns>The <see cref="Skill"/>s.</returns>
        protected abstract Skill[] CreateSkills(Beatmap beatmap);
    }
}