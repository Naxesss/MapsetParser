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
            Tick,
            Unknown
        }

        public readonly int                customIndex;
        public readonly Beatmap.Sampleset? sampleset;
        public readonly HitSound?          hitSound;
        public readonly HitSource          hitSource;
        public readonly bool               taiko;

        public readonly double time;

        public HitSample(int aCustomIndex, Beatmap.Sampleset? aSampleset, HitSound? aHitSound, HitSource aHitSource, double aTime,
            bool aTaiko = false)
        {
            customIndex = aCustomIndex;
            sampleset = aSampleset;
            hitSound = aHitSound;
            hitSource = aHitSource;
            taiko = aTaiko;

            time = aTime;
        }

        public HitSample(string aFileName)
        {
            Regex regex = new Regex(@"(?i)^(taiko-)?(soft|normal|drum)-(hit(whistle|normal|finish)|slider(slide|whistle|tick))(\d+)?");
            Match match = regex.Match(aFileName);
            GroupCollection groups = match.Groups;

            taiko = groups[1].Success;
            sampleset = ParseSampleset(groups[2].Value);
            hitSource = ParseHitSource(groups[3].Value);

            // Can either be part of "hit/.../" or "slider/.../"
            if (groups[4].Success)
                hitSound = ParseHitSound(groups[4].Value);
            else if (groups[5].Success)
                hitSound = ParseHitSound(groups[5].Value);
            else
                hitSound = null;

            customIndex = ParseCustomIndex(groups[6].Value);
        }

        /// <summary> Returns the sampleset corresponding to the given text representation, e.g. "drum" or "soft".
        /// Unrecognized representation returns null. </summary>
        private Beatmap.Sampleset? ParseSampleset(string aText)
        {
            string lowerText = aText.ToLower();
            return
                lowerText == "soft" ? Beatmap.Sampleset.Soft :
                lowerText == "normal" ? Beatmap.Sampleset.Normal :
                lowerText == "drum" ? Beatmap.Sampleset.Drum :
                (Beatmap.Sampleset?)null;
        }

        /// <summary> Returns the hit source corresponding to the given text representation, e.g. "hitnormal" or "sliderslide".
        /// Unrecognized representation returns a hit source of type unknown. </summary>
        private HitSource ParseHitSource(string aText)
        {
            string lowerText = aText.ToLower();
            return
                lowerText.StartsWith("hit") ? HitSource.Edge :
                lowerText.StartsWith("slidertick") ? HitSource.Tick :
                lowerText.StartsWith("slider") ? HitSource.Body :
                HitSource.Unknown;
        }

        /// <summary> Returns the hit sound corresponding to the given text representation, e.g. "whistle", "clap" or "finish".
        /// Unrecognized representation, or N/A (e.g. sliderslide/tick), returns null. </summary>
        private HitSound? ParseHitSound(string aText)
        {
            string lowerText = aText.ToLower();
            return
                lowerText == "normal" ? HitSound.Normal :
                lowerText == "clap" ? HitSound.Clap :
                lowerText == "whistle" ? HitSound.Whistle :
                lowerText == "finish" ? HitSound.Finish :
                (HitSound?)null;
        }

        /// <summary> Returns the given text as an integer if possible, else 1 (i.e. implicit custom index). </summary>
        private int ParseCustomIndex(string aText)
        {
            try
            { return int.Parse(aText); }
            catch
            { return 1; }
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
                        if (hitSource == HitSource.Edge && individualHitSound != HitSound.None)
                            hitSoundString = "hit" + individualHitSound.ToString().ToLower();
                        else if (hitSource == HitSource.Body)
                            hitSoundString = "slider" + (individualHitSound == HitSound.Whistle ? "whistle" : "slide");
                    }
                }
            }

            if (hitSource == HitSource.Tick)
                hitSoundString = "slidertick";

            string customIndexString = customIndex == 1 ? "" : customIndex.ToString();

            if (hitSoundString != null && samplesetString != null)
                return taikoString + samplesetString + "-" + hitSoundString + customIndexString;
            else
                return null;
        }

        /// <summary> Returns whether the sample file name is the same as the given file name (i.e. same sample file).
        /// Ignores case sensitivity. </summary>
        public bool SameFileName(string aFileNameWithExtension) =>
            aFileNameWithExtension.ToLower().StartsWith(GetFileName() + ".");
    }
}
