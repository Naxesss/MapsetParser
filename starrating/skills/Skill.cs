// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using MapsetParser.starrating.preprocessing;
using MapsetParser.starrating.utils;

namespace MapsetParser.starrating.skills
{
    /// <summary>
    /// A bare minimal abstract skill for fully custom skill implementations.
    /// </summary>
    /// <remarks>
    /// This class should be considered a "processing" class and not persisted.
    /// </remarks>
    public abstract class Skill
    {
        protected Skill()
        {
        }

        /// <summary>
        /// Process a <see cref="DifficultyHitObject"/>.
        /// </summary>
        /// <param name="current">The <see cref="DifficultyHitObject"/> to process.</param>
        public abstract void Process(DifficultyHitObject current);

        public abstract string SkillName();
        public override string ToString() => SkillName();

        public virtual bool useInStarRating => true;

        public override bool Equals(object obj)
        {
            if (!(obj is Skill skill))
                return false;

            return skill.SkillName() == this.SkillName();
        }

        public override int GetHashCode()
        {
            return SkillName().GetHashCode();
        }

        /// <summary>
        /// Returns the calculated difficulty value representing all <see cref="DifficultyHitObject"/>s that have been processed up to this point.
        /// </summary>
        public abstract double DifficultyValue();
    }
}
