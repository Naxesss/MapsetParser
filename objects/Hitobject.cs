using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;

namespace MapsetParser.objects
{
    public class HitObject
    {
        // 131,304,1166,1,0,0:0:0:0:                                        circle
        // 319,179,1392,6,0,L|389:160,2,62.5,2|0|0,0:0|0:0|0:0,0:0:0:0:     slider
        // 256,192,187300,12,0,188889,1:0:0:0:                              spinner

        // x, y, time, typeFlags, hitsound, extras                                                                               circle
        // x, y, time, typeFlags, hitsound, (sliderPath, edgeAmount, pixelLength, hitsoundEdges, additionEdges,) extras          slider
        // x, y, time, typeFlags, hitsound, (endTime,) extras                                                                    spinner

        public Beatmap beatmap;
        public string code;
        
        public virtual Vector2 Position { get; private set; }

        public double   time;
        public Type     type;
        public HitSound hitSound;

        // extras
        // not all file versions have these, so they need to be nullable
        public Beatmap.Sampleset    sampleset;
        public Beatmap.Sampleset    addition;
        public int?                 customIndex;
        public int?                 volume;
        public string               filename       = null;

        /// <summary> Determines which sounds will be played as feedback (can be combined, bitflags). </summary>
        [Flags]
        public enum HitSound
        {
            None    = 0,
            Normal  = 1,
            Whistle = 2,
            Finish  = 4,
            Clap    = 8
        }

        /// <summary> Determines the properties of the hit object (can be combined, bitflags). </summary>
        [Flags]
        public enum Type
        {
            Circle          = 1,
            Slider          = 2,
            NewCombo        = 4,
            Spinner         = 8,
            ComboSkip1      = 16,
            ComboSkip2      = 32,
            ComboSkip3      = 64,
            ManiaHoldNote   = 128
        }

        public HitObject(string aCode, Beatmap aBeatmap)
        {
            beatmap = aBeatmap;
            code = aCode;
            
            Position = GetPosition(aCode);

            time       = GetTime(aCode);
            type       = GetTypeFlags(aCode);
            hitSound   = GetHitSound(aCode);

            // extras
            Tuple<Beatmap.Sampleset, Beatmap.Sampleset, int?, int?, string> extras = GetExtras(aCode);
            if (extras != null)
            {
                // custom index and volume are by default 0 if there are edge hitsounds or similar
                sampleset = extras.Item1;
                addition = extras.Item2;
                customIndex = extras.Item3 == 0 ? null : extras.Item3;
                volume = extras.Item4 == 0 ? null : extras.Item4;

                // hitsound filenames only apply to circles and hold notes
                string hitSoundFile = extras.Item5;
                if (hitSoundFile.Trim() != "" && (type.HasFlag(Type.Circle) || type.HasFlag(Type.ManiaHoldNote)))
                    filename = PathStatic.ParsePath(hitSoundFile);
            }
        }

        /*
         *  Parsing
         */
        
        private Vector2 GetPosition(string aCode)
        {
            float x = float.Parse(aCode.Split(',')[0], CultureInfo.InvariantCulture);
            float y = float.Parse(aCode.Split(',')[1], CultureInfo.InvariantCulture);

            return new Vector2(x, y);
        }
        
        private double GetTime(string aCode)
        {
            return double.Parse(aCode.Split(',')[2], CultureInfo.InvariantCulture);
        }
        
        private Type GetTypeFlags(string aCode)
        {
            return (Type)int.Parse(aCode.Split(',')[3]);
        }
        
        private HitSound GetHitSound(string aCode)
        {
            return (HitSound)int.Parse(aCode.Split(',')[4]);
        }
        
        private Tuple<Beatmap.Sampleset, Beatmap.Sampleset, int? ,int?, string> GetExtras(string aCode)
        {
            string extras = aCode.Split(',').Last();

            // hold notes have "endTime:extras" as format
            int index = type.HasFlag(Type.ManiaHoldNote) ? 1 : 0;
            if (extras.Contains(":"))
            {
                Beatmap.Sampleset samplesetValue = (Beatmap.Sampleset)int.Parse(extras.Split(':')[index]);
                Beatmap.Sampleset additionsValue = (Beatmap.Sampleset)int.Parse(extras.Split(':')[index + 1]);
                int? customIndexValue = int.Parse(extras.Split(':')[index + 2]);

                // does not exist in file v11
                int? volumeValue = null;
                if (extras.Split(':').Count() > index + 3)
                    volumeValue = int.Parse(extras.Split(':')[index + 3]);

                string filenameValue = "";
                if (extras.Split(':').Count() > index + 4)
                    filenameValue = extras.Split(':')[index + 4];

                return Tuple.Create(samplesetValue, additionsValue, customIndexValue, volumeValue, filenameValue);
            }
            return null;
        }

        /*
         *  Star Rating
         */

        /// <summary> <para>Returns the difference in time between the start of this object and the start of the previous object.</para>
        /// Note: This always returns at least 50 ms, to mimic the star rating algorithm.</summary>
        public double GetPrevDeltaStartTime()
        {
            // smallest value is 50 ms for pp calc as a safety measure apparently
            // it's equivalent to 375 BPM streaming speed
            return Math.Max(50, time - beatmap.GetPrevHitObject(time).time);
        }

        /// <summary> <para>Returns the distance between the edges of the hit circles for the start of this object and the start of the previous object.</para>
        /// Note: This adds a bonus scaling factor for small circle sizes, to mimic the star rating algorithm.</summary>
        public double GetPrevStartDistance()
        {
            HitObject prevObject = beatmap.GetPrevHitObject(time);

            double radius = beatmap.difficultySettings.GetCircleRadius();

            // We will scale distances by this factor, so we can assume a uniform CircleSize among beatmaps.
            double scalingFactor = 52 / radius;

            // small circle bonus
            if (radius < 30)
                scalingFactor *= 1 + Math.Min(30 - radius, 5) / 50;

            Vector2 prevPosition = prevObject.Position;
            double prevDistance = (Position - prevPosition).Length();

            return prevDistance * scalingFactor;
        }

        /*
         *  Utility
         */

        /// <summary> Returns whether a hit object code has the given type. </summary>
        public static bool HasType(string aCode, Type aType)
        {
            return ((Type)int.Parse(aCode.Split(',')[3])).HasFlag(aType);
        }

        /// <summary> Returns whether the hit object has a hit sound, or optionally a certain type of hit sound. </summary>
        public bool HasHitSound(HitSound? aHitSound = null)
        {
            return aHitSound == null
                ? hitSound > 0
                : hitSound.HasFlag(aHitSound);
        }

        /// <summary> Returns the difference in time between the start of this object and the end of the previous object. </summary>
        public double GetPrevDeltaTime()
        {
            return time - beatmap.GetPrevHitObject(time).GetEndTime();
        }

        /// <summary> Returns the difference in distance between the start of this object and the end of the previous object. </summary>
        public double GetPrevDistance()
        {
            HitObject prevObject = beatmap.GetPrevHitObject(time);

            Vector2 prevPosition = prevObject.Position;
            if (prevObject is Slider slider)
                prevPosition = slider.EndPosition;

            return (Position - prevPosition).Length();
        }

        /// <summary> Returns the points in time where heads, tails or repeats exist (i.e. the start, end or reverses of any object). </summary>
        public IEnumerable<double> GetEdgeTimes()
        {
            yield return time;

            if (this is Slider slider)
                for (int i = 0; i < slider.edgeAmount; ++i)
                    yield return time + slider.GetCurveDuration() * (i + 1);

            if (this is Spinner spinner)
                yield return spinner.endTime;

            if (this is HoldNote holdNote)
                yield return holdNote.endTime;
        }

        /// <summary> Returns the effective sampleset of the hit object (body for sliders), optionally prioritizing the addition. </summary>
        public Beatmap.Sampleset GetSampleset(bool anAddition = false)
        {
            if (anAddition && addition != Beatmap.Sampleset.Auto)
                return addition;

            // inherits from timing line if auto
            return sampleset == Beatmap.Sampleset.Auto
                ? beatmap.GetTimingLine(time, true).sampleset : sampleset;
        }

        /// <summary> Returns the effective sampleset of the head of the object, if applicable, otherwise null, optionally prioritizing the addition. </summary>
        public Beatmap.Sampleset? GetStartSampleset(bool anAddition = false)
        {
            return (this as Slider)?.GetStartSampleset(anAddition) ?? ((this is Spinner) ? null : (Beatmap.Sampleset?)GetSampleset(anAddition));
        }

        /// <summary> Returns the effective sampleset of the tail of the object, if applicable, otherwise null, optionally prioritizing the addition. </summary>
        public virtual Beatmap.Sampleset? GetEndSampleset(bool anAddition = false)
        {
            return (this as Slider)?.GetEndSampleset(anAddition) ?? ((this is Spinner) ? (Beatmap.Sampleset?)GetSampleset(anAddition) : null);
        }

        /// <summary> Returns the hit sound(s) of the head of the object, if applicable, otherwise null. </summary>
        public HitSound? GetStartHitSound()
        {
            // spinners have no start
            return
                (this as Slider)?.startHitSound ??
                ((this is Spinner) ? null : (HitSound?)hitSound);
        }

        /// <summary> Returns the hit sound(s) of the tail of the object, if it applicable, otherwise null. </summary>
        public HitSound? GetEndHitSound()
        {
            // circles and hold notes have no end
            return
                (this as Slider)?.endHitSound ??
                (this as Spinner)?.hitSound ??
                null;
        }

        /// <summary> Returns the hit sound(s) of the slide of the object, if applicable, otherwise null. </summary>
        public HitSound? GetSliderSlide()
        {
            // circles, hold notes and spinners have no sliderslide
            return (this as Slider)?.hitSound ?? null;
        }

        /// <summary> Returns all used combinations of customs, samplesets and hit sounds for this object. </summary>
        private IEnumerable<Tuple<int, Beatmap.Sampleset?, HitSound?>> GetUsedHitSounds(bool anAddition = false)
        {
            yield return new Tuple<int, Beatmap.Sampleset?, HitSound?>(
                beatmap.GetTimingLine(time, false, true).customIndex,
                GetStartSampleset(anAddition), GetStartHitSound());

            yield return new Tuple<int, Beatmap.Sampleset?, HitSound?>(
                beatmap.GetTimingLine(GetEndTime(), false, true).customIndex,
                GetEndSampleset(anAddition), GetEndHitSound());

            for (int i = 0; i < ((this as Slider)?.repeatHitSounds.Count() ?? 0); ++i)
            {
                HitSound?           hitSound  = (this as Slider)?.repeatHitSounds.ElementAt(i);
                Beatmap.Sampleset?  sampleset = (this as Slider)?.GetRepeatSampleset(i);
                Beatmap.Sampleset?  addition  = (this as Slider)?.repeatAdditions.Count() > 0 ?   // not a thing in file version 9
                                                (this as Slider)?.repeatAdditions.ElementAt(i) :
                                                Beatmap.Sampleset.Auto;

                yield return new Tuple<int, Beatmap.Sampleset?, HitSound?>(
                    beatmap.GetTimingLine((this as Slider).GetCurveDuration() * (i + 1), false, true).customIndex,
                    anAddition && addition != Beatmap.Sampleset.Auto ? addition : sampleset, hitSound);
            }
        }

        /// <summary> Returns all used hit sound file names for this object without extension. </summary>
        public IEnumerable<string> GetUsedHitSoundFileNames()
        {
            List<string> usedFiles = new List<string>();

            IEnumerable<Tuple<int, Beatmap.Sampleset?, HitSound?>> usedHitSounds = GetUsedHitSounds(true);
            foreach (var usedHitSound in usedHitSounds)
            {
                foreach (HitSound individualHitSound in Enum.GetValues(typeof(HitSound)))
                {
                    // We only handle the actual hit sounds here, which are affected by additions.
                    if (individualHitSound != HitSound.Normal &&
                        individualHitSound != HitSound.None &&
                        usedHitSound.Item3.GetValueOrDefault().HasFlag(individualHitSound))
                    {
                        string sampleset = usedHitSound.Item2.ToString().ToLower();
                        string hitSound = individualHitSound.ToString().ToLower();
                        string customIndex = usedHitSound.Item1 == 1 ? "" : usedHitSound.Item1.ToString();

                        usedFiles.Add($"{sampleset}-hit{hitSound}{customIndex}");
                    }
                }
            }

            IEnumerable<Tuple<int, Beatmap.Sampleset?, HitSound?>> usedHitNormals = GetUsedHitSounds(false);
            foreach (var usedHitNormal in usedHitNormals)
            {
                // Hit normals are not affected by additions, so we need to do this separately, since otherwise the samplesets would be incorrect.
                // Another reason we're doing this separately is because all places where hit sounds exist (or don't, but can), there is a hit normal.
                // For example, where spinners start, there is no hit normal, since there can't be a hit sound there.
                if (usedHitNormal.Item3 != null)
                {
                    string sampleset = usedHitNormal.Item2.ToString().ToLower();
                    string customIndex = usedHitNormal.Item1 == 1 ? "" : usedHitNormal.Item1.ToString();

                    usedFiles.Add($"{sampleset}-hitnormal{customIndex}");
                }
            }

            if (this is Slider slider)
            {
                List<TimingLine> lines =
                    beatmap.timingLines.Where(aLine =>
                        aLine.offset > slider.time &&
                        aLine.offset <= slider.endTime).ToList();

                lines.Add(beatmap.GetTimingLine(slider.time, true));

                string slide = hitSound.HasFlag(HitSound.Whistle) ? "whistle" : "slide";
                
                foreach (TimingLine line in lines)
                {
                    string samplesetString   = line.sampleset.ToString().ToLower();
                    string customIndexString = line.customIndex == 1 ? "" : line.customIndex.ToString();

                    usedFiles.Add($"{samplesetString}-slider{slide}{customIndexString}");
                }

                IEnumerable<double> tickTimes = slider.GetSliderTickTimes();
                foreach (double tickTime in tickTimes)
                {
                    TimingLine line = beatmap.GetTimingLine(tickTime);
                    string samplesetString   = line.sampleset.ToString().ToLower();
                    string customIndexString = line.customIndex == 1 ? "" : line.customIndex.ToString();

                    usedFiles.Add($"{samplesetString}-slidertick{customIndexString}");
                }
            }

            return usedFiles.Distinct();
        }

        /// <summary> Returns the end time of the hit object, or the start time if no end time exists. </summary>
        public double GetEndTime()
        {
            // regardless of circle/slider/spinner/hold note, finds the end of the object
            return
                (this as Slider)?.endTime ??
                (this as Spinner)?.endTime ??
                (this as HoldNote)?.endTime ??
                time;
        }

        /// <summary> Returns the name of the object part at the given time, for example "Slider head", "Circle" or "Spinner tail". </summary>
        public string GetPartName(double aTime)
        {
            string edgeType =
                GetEndTime() == aTime                ? "tail" :
                aTime > time && aTime < GetEndTime() ? "body" :
                                                       "head";

            return GetObjectType() + (!(this is Circle) ? (" " + edgeType) : "");
        }

        /// <summary> Returns the name of the object in general, for example "Slider", "Circle", "Hold note", etc. </summary>
        public string GetObjectType() =>
            // Creating a hit object instance rather than circle, slider, etc will prevent polymorphism, so we check the type as well.
            this is Slider   || type.HasFlag(Type.Slider)        ? "Slider" :
            this is Circle   || type.HasFlag(Type.Circle)        ? "Circle" :
            this is Spinner  || type.HasFlag(Type.Spinner)       ? "Spinner" :
            this is HoldNote || type.HasFlag(Type.ManiaHoldNote) ? "Hold note" :
            "Unknown object";

        public override string ToString() =>
            time + " ms: " + GetObjectType() + " at (" + Position.X + "; " + Position.Y + ")";
    }
}
