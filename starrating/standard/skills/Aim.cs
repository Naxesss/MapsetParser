using MapsetParser.objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapsetParser.starrating.standard
{
    public class Aim : Skill
    {
        protected override double SkillMultiplier => 26.25;
        protected override double StrainDecay => 0.15;

        protected override double StrainValueOf(HitObject anObject)
        {
            double distance   = anObject.GetPrevStartDistance();
            double time       = anObject.GetPrevDeltaStartTime();

            double strainValue = Math.Pow(distance, 0.99) / time;
            return strainValue;
        }
    }
}
