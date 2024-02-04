// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using MapsetParser.objects;
using MapsetParser.objects.timinglines;
using MapsetParser.starrating.preprocessing;
using MapsetParser.starrating.skills;

namespace MapsetParser.starrating.taiko.skills
{
    public class ScrollSpeed : StrainSkill
    {
        protected override int SectionLength => 20;
        public override bool useInStarRating => false;
        public override string SkillName() => "Scroll Speed";

        public ScrollSpeed()
            : base()
        {
        }

        public override double CalculateInitialStrain(double time, DifficultyHitObject current)
        {
            return getScrollSpeed(current.BaseObject);
        }

        public override double StrainValueAt(DifficultyHitObject current)
        {
            return getScrollSpeed(current.BaseObject);
        }

        private double getScrollSpeed(HitObject hitObject)
        {
            var beatmap = hitObject.beatmap;
            var bpm = beatmap.GetTimingLine<UninheritedLine>(hitObject.time).bpm;
            var svMult = beatmap.GetTimingLine(hitObject.time).svMult;
            var baseSvMult = beatmap.difficultySettings.sliderMultiplier / 1.4;
            return bpm * svMult * baseSvMult;
        }
    }
}
