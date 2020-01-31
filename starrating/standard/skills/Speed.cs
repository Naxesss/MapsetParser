using MapsetParser.objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapsetParser.starrating.standard
{
    public class Speed : Skill
    {
        protected override double SkillMultiplier => 1400;
        protected override double StrainDecay      => 0.3;

        // constants for thresholds
        private const double singleSpacingThreshold    = 125;
        private const double streamSpacingThreshold    = 110;
        private const double almostDiameter            = 90;

        protected override double StrainValueOf(HitObject hitObject)
        {
            double distance = hitObject.GetPrevStartDistance();

            // determines speed based on distance to the note, where the further spaced the streams are the more speed is given
            // basically describes how fast you're forced to move your cursor at the same time as clicking
            double speedValue;
            if (distance > singleSpacingThreshold)
                speedValue = 2.5;
            else if (distance > streamSpacingThreshold)
                speedValue = 1.6 + 0.9 * (distance - streamSpacingThreshold) / (singleSpacingThreshold - streamSpacingThreshold);
            else if (distance > almostDiameter)
                speedValue = 1.2 + 0.4 * (distance - almostDiameter) / (streamSpacingThreshold - almostDiameter);
            else if (distance > almostDiameter / 2)
                speedValue = 0.95 + 0.25 * (distance - almostDiameter / 2) / (almostDiameter / 2);
            else
                speedValue = 0.95;

            return speedValue / hitObject.GetPrevDeltaStartTime();
        }
    }
}
