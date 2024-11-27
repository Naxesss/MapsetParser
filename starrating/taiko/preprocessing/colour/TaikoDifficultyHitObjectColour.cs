﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MapsetParser.starrating.taiko.preprocessing.Colour.Data;

namespace MapsetParser.starrating.taiko.preprocessing.Colour
{
    /// <summary>
    /// Stores colour compression information for a <see cref="TaikoDifficultyHitObject"/>.
    /// </summary>
    public class TaikoDifficultyHitObjectColour
    {
        /// <summary>
        /// The <see cref="MonoStreak"/> that encodes this note.
        /// </summary>
        public MonoStreak? MonoStreak;

        /// <summary>
        /// The <see cref="AlternatingMonoPattern"/> that encodes this note.
        /// </summary>
        public AlternatingMonoPattern? AlternatingMonoPattern;

        /// <summary>
        /// The <see cref="RepeatingHitPattern"/> that encodes this note.
        /// </summary>
        public RepeatingHitPatterns? RepeatingHitPattern;

        /// <summary>
        /// The closest past <see cref="TaikoDifficultyHitObject"/> that's not the same colour.
        /// </summary>
        public TaikoDifficultyHitObject? PreviousColourChange => MonoStreak?.FirstHitObject.PreviousNote(0);

        /// <summary>
        /// The closest future <see cref="TaikoDifficultyHitObject"/> that's not the same colour.
        /// </summary>
        public TaikoDifficultyHitObject? NextColourChange => MonoStreak?.LastHitObject.NextNote(0);
    }
}
