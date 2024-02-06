// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using MapsetParser.objects;

namespace MapsetParser.starrating.preprocessing
{
    /// <summary>
    /// Wraps a <see cref="HitObject"/> and provides additional information to be used for difficulty calculation.
    /// </summary>
    public class DifficultyHitObject
    {
        private readonly IReadOnlyList<DifficultyHitObject> difficultyHitObjects;

        /// <summary>
        /// The index of this <see cref="DifficultyHitObject"/> in the list of all <see cref="DifficultyHitObject"/>s.
        /// </summary>
        public int Index;

        /// <summary>
        /// The <see cref="HitObject"/> this <see cref="DifficultyHitObject"/> wraps.
        /// </summary>
        public readonly HitObject BaseObject;

        /// <summary>
        /// The last <see cref="HitObject"/> which occurs before <see cref="BaseObject"/>.
        /// </summary>
        public readonly HitObject LastObject;

        /// <summary>
        /// Amount of time elapsed between <see cref="BaseObject"/> and <see cref="LastObject"/>.
        /// </summary>
        public readonly double DeltaTime;

        /// <summary>
        /// Start time of <see cref="BaseObject"/>.
        /// </summary>
        public readonly double StartTime;

        /// <summary>
        /// End time of <see cref="BaseObject"/>.
        /// </summary>
        public readonly double EndTime;

        /// <summary>
        /// Creates a new <see cref="DifficultyHitObject"/>.
        /// </summary>
        /// <param name="hitObject">The <see cref="HitObject"/> which this <see cref="DifficultyHitObject"/> wraps.</param>
        /// <param name="lastObject">The last <see cref="HitObject"/> which occurs before <paramref name="hitObject"/> in the beatmap.</param>
        /// <param name="objects">The list of <see cref="DifficultyHitObject"/>s in the current beatmap.</param>
        /// <param name="index">The index of this <see cref="DifficultyHitObject"/> in <paramref name="objects"/> list.</param>
        public DifficultyHitObject(HitObject hitObject, HitObject lastObject, List<DifficultyHitObject> objects, int index)
        {
            difficultyHitObjects = objects;
            Index = index;
            BaseObject = hitObject;
            LastObject = lastObject;
            DeltaTime = (hitObject.time - lastObject.time);
            StartTime = hitObject.time;
            EndTime = hitObject.GetEndTime();
        }

        public DifficultyHitObject Previous(int backwardsIndex) => difficultyHitObjects.ElementAtOrDefault(Index - (backwardsIndex + 1));

        public DifficultyHitObject Next(int forwardsIndex) => difficultyHitObjects.ElementAtOrDefault(Index + (forwardsIndex + 1));
    }
}
