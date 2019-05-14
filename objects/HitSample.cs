using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using static MapsetParser.objects.HitObject;

namespace MapsetParser.objects
{
    public class HitSample
    {
        public enum HitSource
        {
            Edge,
            Body,
            Tick
        }

        public readonly int                customIndex;
        public readonly Beatmap.Sampleset? sampleset;
        public readonly HitSound?          hitSound;
        public readonly HitSource          hitSource;
        public readonly bool               taiko;

        public readonly double time;

        public HitSample(int aCustomIndex, Beatmap.Sampleset? aSampleset, HitSound? aHitSound, HitSource aHitSource, double aTime)
        {
            customIndex = aCustomIndex;
            sampleset = aSampleset;
            hitSound = aHitSound;
            hitSource = aHitSource;

            time = aTime;
        }

        /// <summary> Returns the file name of this sample without extension, or null if no file is associated. </summary>
        public string GetFileName()
        {
            string taikoString = taiko ? "taiko-" : "";
            string samplesetString = sampleset?.ToString().ToLower();
            string hitSoundString = null;

            if (hitSound != null)
            {
                foreach (HitSound individualHitSound in Enum.GetValues(typeof(HitSound)))
                {
                    if (hitSound.GetValueOrDefault().HasFlag(individualHitSound))
                    {
                        if (hitSource == HitSource.Edge)
                            hitSoundString = "hit" + individualHitSound.ToString().ToLower();
                        else if (hitSource == HitSource.Body)
                            hitSoundString = "slider" + (individualHitSound == HitSound.Whistle ? "whistle" : "slide");
                    }
                    if (hitSource == HitSource.Tick)
                        hitSoundString = "slidertick";
                }
            }

            string customIndexString = customIndex == 1 ? "" : customIndex.ToString();

            if (hitSoundString != null && samplesetString != null)
                return taikoString + samplesetString + "-" + hitSoundString + customIndexString;
            else
                return null;
        }
    }
}
