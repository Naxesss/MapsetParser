using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapsetParser.starrating.standard
{
    abstract class Skill
    {
        // works as a base class for all skills like aim, speed, etc.

        protected abstract double SkillMultiplier  { get; }    // how much this skill is weighed
        protected abstract double StrainDecay      { get; }    // how quickly strain decays for this skill

        protected abstract double StrainValueOf(HitObject anObject);    // how much an object increases strain

        public double currentStrain        = 0;
        public double currentStrainPeak    = 0;
        
        public double GetStrainDecay(double aDeltaTime) => Math.Pow(StrainDecay, aDeltaTime / 1000);

        public List<HitObject>  previousObjects    = new List<HitObject>();
        public List<double>     strainPeaks        = new List<double>();

        public void Process(HitObject anObject)
        {
            // base mechanics for strain, like decaying over time and increasing for each object provided
            currentStrain *= GetStrainDecay(anObject.GetPrevDeltaStartTime());
            if (!(anObject is Spinner))
                currentStrain += StrainValueOf(anObject) * SkillMultiplier;

            currentStrainPeak = Math.Max(currentStrain, currentStrainPeak);

            previousObjects.Add(anObject);
        }

        public void SaveCurrentPeak()
        {
            if (previousObjects.Count > 0)
                strainPeaks.Add(currentStrainPeak);
        }

        public void StartNewSectionFrom(double anOffset)
        {
            // strain carries over to following sections
            if (previousObjects.Count > 0)
                currentStrainPeak = currentStrain * GetStrainDecay(anOffset - previousObjects.Last().time);
        }

        public double DifficultyValue()
        {
            // sort highest > lowest
            strainPeaks.Sort((aStrain, anOtherStrain) => anOtherStrain.CompareTo(aStrain));

            double difficulty = 0;
            double weight     = 1;
            
            // works similar to pp weighing as in every lower strain being weighed less
            // in this case 90% of the previous
            foreach (double strainPeak in strainPeaks)
            {
                difficulty += strainPeak * weight;
                weight *= 0.9;
            }

            return difficulty;
        }
    }
}
