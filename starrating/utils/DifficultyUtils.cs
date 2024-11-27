// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace MapsetParser.starrating.utils
{
    public static class DifficultyUtils
    {
        public static double DifficultyRange(double difficulty, double min, double mid, double max)
        {
            if (difficulty > 5)
                return mid + (max - mid) * DifficultyRange(difficulty);
            if (difficulty < 5)
                return mid + (mid - min) * DifficultyRange(difficulty);

            return mid;
        }

        public static double DifficultyRange(double difficulty) => (difficulty - 5) / 5;

        public static double DifficultyRange(double difficulty, (double od0, double od5, double od10) range) => DifficultyRange(difficulty, range.od0, range.od5, range.od10);

        public static double InverseDifficultyRange(double difficultyValue, double diff0, double diff5, double diff10)
        {
            return Math.Sign(difficultyValue - diff5) == Math.Sign(diff10 - diff5)
                ? (difficultyValue - diff5) / (diff10 - diff5) * 5 + 5
                : (difficultyValue - diff5) / (diff5 - diff0) * 5 + 5;
        }
    }
}
