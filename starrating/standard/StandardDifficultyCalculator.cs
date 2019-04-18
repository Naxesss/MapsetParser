using MapsetParser.objects;
using MapsetParser.starrating.standard;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapsetParser.starrating.standard
{
    class StandardDifficultyCalculator
    {
        // constants
        private const int       sectionLength          = 400;
        private const double    difficultyMultiplier   = 0.0675; // 0.0675

        // returns aim, speed and star rating respectively
        // note that star rating isn't just the sum of aim and speed
        public static Tuple<double, double, double> Calculate(Beatmap aBeatmap)
        {
            // if there are no objects in the map it's star rating will be 0
            if (aBeatmap.hitObjects.Count == 0)
                return new Tuple<double, double, double>(0, 0, 0);

            Skill[] skills =
            {
                new Aim(),
                new Speed()
            };

            // "the first object doesn't generate a strain, so we begin with an incremented section end"
            double currentSectionEnd = sectionLength;

            // calc ignores the first object
            foreach (HitObject hitObject in aBeatmap.hitObjects.Skip(1))
            {
                // perform all this on the previous object, which is why it's before Process()
                while (hitObject.time > currentSectionEnd)
                {
                    foreach (Skill skill in skills)
                    {
                        skill.SaveCurrentPeak();                          // adds new peak
                        skill.StartNewSectionFrom(currentSectionEnd);   // decays from prev object's start to the start of the new section
                    }

                    currentSectionEnd += sectionLength;
                }

                foreach (Skill skill in skills) // todo figure out why 2 objects don't measure correctly using this whole thing
                    skill.Process(hitObject);
            }

            // calculating the actual rating of each skill and the sum
            double aimRating      = Math.Sqrt(skills[0].DifficultyValue()) * difficultyMultiplier;
            double speedRating    = Math.Sqrt(skills[1].DifficultyValue()) * difficultyMultiplier;
            double starRating     = aimRating + speedRating + Math.Abs(aimRating - speedRating) / 2;

            return new Tuple<double, double, double>(aimRating, speedRating, starRating);
        }
    }
}
