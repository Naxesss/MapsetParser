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
        private const int       sectionLength          = 400;
        private const double    difficultyMultiplier   = 0.0675;
        
        /// <summary> Returns a tuple of aim rating, speed rating, and star rating (calculated from the other two) respectively. </summary>
        public static Tuple<double, double, double> Calculate(Beatmap aBeatmap)
        {
            if (aBeatmap.hitObjects.Count == 0)
                return new Tuple<double, double, double>(0, 0, 0);

            Skill[] skills =
            {
                new Aim(),
                new Speed()
            };

            // First object cannot generate strain, so we offset this to account for that.
            double currentSectionEnd = sectionLength;
            
            foreach (HitObject hitObject in aBeatmap.hitObjects.Skip(1))
            {
                // Performed on the previous object, hence before Process.
                while (hitObject.time > currentSectionEnd)
                {
                    foreach (Skill skill in skills)
                    {
                        skill.SaveCurrentPeak();
                        skill.StartNewSectionFrom(currentSectionEnd);
                    }

                    currentSectionEnd += sectionLength;
                }

                foreach (Skill skill in skills)
                    skill.Process(hitObject);
            }
            
            double aimRating      = Math.Sqrt(skills[0].DifficultyValue()) * difficultyMultiplier;
            double speedRating    = Math.Sqrt(skills[1].DifficultyValue()) * difficultyMultiplier;
            double starRating     = aimRating + speedRating + Math.Abs(aimRating - speedRating) / 2;

            return new Tuple<double, double, double>(aimRating, speedRating, starRating);
        }
    }
}
