﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MapsetParser.objects.hitobjects;
using MapsetParser.starrating.osu.preprocessing;
using MapsetParser.starrating.preprocessing;
using MapsetParser.starrating.skills;
using System;

namespace MapsetParser.starrating.osu.skills
{
    /// <summary>
    /// Represents the skill required to correctly aim at every object in the map with a uniform CircleSize and normalized distances.
    /// </summary>
    public class Aim : Skill
    {
        private const double angle_bonus_begin = Math.PI / 3;
        private const double timing_threshold = 107;

        protected override double SkillMultiplier => 26.25;
        protected override double StrainDecayBase => 0.15;

        public override string SkillName() => "Aim";

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            if (current.BaseObject is Spinner)
                return 0;

            var osuCurrent = (OsuDifficultyHitObject)current;

            double result = 0;

            if (Previous.Count > 0)
            {
                var osuPrevious = (OsuDifficultyHitObject)Previous[0];

                if (osuCurrent.Angle != null && osuCurrent.Angle.Value > angle_bonus_begin)
                {
                    const double scale = 90;

                    var angleBonus = Math.Sqrt(
                        Math.Max(osuPrevious.JumpDistance - scale, 0)
                        * Math.Pow(Math.Sin(osuCurrent.Angle.Value - angle_bonus_begin), 2)
                        * Math.Max(osuCurrent.JumpDistance - scale, 0));
                    result = 1.5 * ApplyDiminishingExp(Math.Max(0, angleBonus)) / Math.Max(timing_threshold, osuPrevious.StrainTime);
                }
            }

            double jumpDistanceExp = ApplyDiminishingExp(osuCurrent.JumpDistance);
            double travelDistanceExp = ApplyDiminishingExp(osuCurrent.TravelDistance);

            return Math.Max(
                result + (jumpDistanceExp + travelDistanceExp + Math.Sqrt(travelDistanceExp * jumpDistanceExp)) / Math.Max(osuCurrent.StrainTime, timing_threshold),
                (Math.Sqrt(travelDistanceExp * jumpDistanceExp) + jumpDistanceExp + travelDistanceExp) / osuCurrent.StrainTime
            );
        }

        private double ApplyDiminishingExp(double val) => Math.Pow(val, 0.99);
    }
}