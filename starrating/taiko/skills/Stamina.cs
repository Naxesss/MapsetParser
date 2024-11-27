// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MapsetParser.starrating.preprocessing;
using MapsetParser.starrating.skills;
using MapsetParser.starrating.taiko.evaluators;

namespace MapsetParser.starrating.taiko.skills
{
    /// <summary>
    /// Calculates the stamina coefficient of taiko difficulty.
    /// </summary>
    public class Stamina : StrainDecaySkill
    {
        protected override double SkillMultiplier => 1.1;
        protected override double StrainDecayBase => 0.4;

        public override string SkillName() => "Stamina";

        /// <summary>
        /// Creates a <see cref="Stamina"/> skill.
        /// </summary>
        public Stamina()
            : base()
        {
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            return StaminaEvaluator.EvaluateDifficultyOf(current);
        }
    }
}
