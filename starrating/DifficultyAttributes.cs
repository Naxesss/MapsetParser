// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MapsetParser.starrating.skills;

namespace MapsetParser.starrating
{
    public class DifficultyAttributes
    {
        public Skill[] Skills;

        public double StarRating;
        public int MaxCombo;

        public DifficultyAttributes()
        {
        }

        public DifficultyAttributes(Skill[] skills, double starRating)
        {
            Skills = skills;
            StarRating = starRating;
        }
    }
}