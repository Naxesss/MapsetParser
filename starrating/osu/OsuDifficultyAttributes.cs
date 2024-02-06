// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MapsetParser.starrating.osu
{
    public class OsuDifficultyAttributes : DifficultyAttributes
    {
        /// <summary>
        /// The difficulty corresponding to the aim skill.
        /// </summary>
        public double AimDifficulty;

        /// <summary>
        /// The difficulty corresponding to the speed skill.
        /// </summary>
        public double SpeedDifficulty;

        /// <summary>
        /// The number of clickable objects weighted by difficulty.
        /// Related to <see cref="SpeedDifficulty"/>
        /// </summary>
        public double SpeedNoteCount;

        /// <summary>
        /// Describes how much of <see cref="AimDifficulty"/> is contributed to by hitcircles or sliders.
        /// A value closer to 1.0 indicates most of <see cref="AimDifficulty"/> is contributed by hitcircles.
        /// A value closer to 0.0 indicates most of <see cref="AimDifficulty"/> is contributed by sliders.
        /// </summary>
        public double SliderFactor;

        /// <summary>
        /// The perceived approach rate inclusive of rate-adjusting mods (DT/HT/etc).
        /// </summary>
        /// <remarks>
        /// Rate-adjusting mods don't directly affect the approach rate difficulty value, but have a perceived effect as a result of adjusting audio timing.
        /// </remarks>
        public double ApproachRate;

        /// <summary>
        /// The perceived overall difficulty inclusive of rate-adjusting mods (DT/HT/etc).
        /// </summary>
        /// <remarks>
        /// Rate-adjusting mods don't directly affect the overall difficulty value, but have a perceived effect as a result of adjusting audio timing.
        /// </remarks>
        public double OverallDifficulty;

        /// <summary>
        /// The beatmap's drain rate. This doesn't scale with rate-adjusting mods.
        /// </summary>
        public double DrainRate;

        /// <summary>
        /// The number of hitcircles in the beatmap.
        /// </summary>
        public int HitCircleCount;
        /// <summary>
        /// The number of sliders in the beatmap.
        /// </summary>
        public int SliderCount;

        /// <summary>
        /// The number of spinners in the beatmap.
        /// </summary>
        public int SpinnerCount;
    }
}
