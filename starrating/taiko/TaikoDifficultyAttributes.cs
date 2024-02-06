// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace MapsetParser.starrating.taiko
{
    public class TaikoDifficultyAttributes : DifficultyAttributes
    {
        /// <summary>
        /// The difficulty corresponding to the stamina skill.
        /// </summary>
        public double StaminaDifficulty;

        /// <summary>
        /// The difficulty corresponding to the rhythm skill.
        /// </summary>
        public double RhythmDifficulty;

        /// <summary>
        /// The difficulty corresponding to the colour skill.
        /// </summary>
        public double ColourDifficulty;

        /// <summary>
        /// The difficulty corresponding to the hardest parts of the map.
        /// </summary>
        public double PeakDifficulty;

        /// <summary>
        /// The perceived hit window for a GREAT hit inclusive of rate-adjusting mods (DT/HT/etc).
        /// </summary>
        public double GreatHitWindow;
    }
}
