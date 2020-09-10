using MapsetParser.objects.hitobjects;
using MapsetParser.statics;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Text.RegularExpressions;

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

        public readonly Beatmap beatmap;
        public readonly string code;
        private int hitObjectIndex;

        public virtual Vector2 Position { get; private set; }

        public readonly double   time;
        public readonly Type     type;
        public readonly HitSound hitSound;

        // extras
        // not all file versions have these, so they need to be nullable
        public readonly Beatmap.Sampleset sampleset;
        public readonly Beatmap.Sampleset addition;
        public readonly int?              customIndex;
        public readonly int?              volume;
        public readonly string            filename = null;

        public List<HitSample> usedHitSamples = new List<HitSample>();

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
            Circle        = 1,
            Slider        = 2,
            NewCombo      = 4,
            Spinner       = 8,
            ComboSkip1    = 16,
            ComboSkip2    = 32,
            ComboSkip3    = 64,
            ManiaHoldNote = 128
        }

        public HitObject(string[] args, Beatmap beatmap)
        {
            this.beatmap = beatmap;
            code = String.Join(",", args);

            Position = GetPosition(args);

            time = GetTime(args);
            type = GetTypeFlags(args);
            hitSound = GetHitSound(args);

            // extras
            Tuple<Beatmap.Sampleset, Beatmap.Sampleset, int?, int?, string> extras = GetExtras(args);
            if (extras != null)
            {
                // custom index and volume are by default 0 if there are edge hitsounds or similar
                sampleset   = extras.Item1;
                addition    = extras.Item2;
                customIndex = extras.Item3 == 0 ? null : extras.Item3;
                volume      = extras.Item4 == 0 ? null : extras.Item4;

                // hitsound filenames only apply to circles and hold notes
                string hitSoundFile = extras.Item5;
                if (hitSoundFile.Trim() != "" && (HasType(Type.Circle) || HasType(Type.ManiaHoldNote)))
                    filename = PathStatic.ParsePath(hitSoundFile, false, true);
            }

            // Sliders and spinners include additional edges which support hit sounding, so we
            // should handle that after those edges are initialized in Slider/Spinner instead.
            if (!(this is Slider) && !(this is Spinner))
                usedHitSamples = GetUsedHitSamples().ToList();
        }

        /*
         *  Parsing
         */

        private Vector2 GetPosition(string[] args)
        {
            float x = float.Parse(args[0], CultureInfo.InvariantCulture);
            float y = float.Parse(args[1], CultureInfo.InvariantCulture);

            return new Vector2(x, y);
        }

        private double GetTime(string[] args)
        {
            return double.Parse(args[2], CultureInfo.InvariantCulture);
        }

        private Type GetTypeFlags(string[] args)
        {
            return (Type)int.Parse(args[3]);
        }

        private HitSound GetHitSound(string[] args)
        {
            return (HitSound)int.Parse(args[4]);
        }

        private Tuple<Beatmap.Sampleset, Beatmap.Sampleset, int?, int?, string> GetExtras(string[] args)
        {
            string extras = args.Last();

            // Hold notes have "endTime:extras" as format.
            int index = HasType(Type.ManiaHoldNote) ? 1 : 0;
            if (extras.Contains(":"))
            {
                Beatmap.Sampleset samplesetValue = (Beatmap.Sampleset)int.Parse(extras.Split(':')[index]);
                Beatmap.Sampleset additionsValue = (Beatmap.Sampleset)int.Parse(extras.Split(':')[index + 1]);
                int? customIndexValue = int.Parse(extras.Split(':')[index + 2]);

                // Does not exist in file v11.
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
         *  Next / Prev
         */

        /// <summary> Returns the index of this hit object in the beatmap's hit object list, O(1). </summary>
        public int GetHitObjectIndex() => hitObjectIndex;
        /// <summary> Sets the index of this hit object. This should reflect the index in the hit object list of the beatmap.
        /// Only use this if you're changing the order of objects or adding new ones after parsing. </summary>
        public void SetHitObjectIndex(int index) => hitObjectIndex = index;

        /// <summary> Returns the next hit object in the hit objects list, if any,
        /// otherwise null, O(1). Optionally skips concurrent objects. </summary>
        public HitObject Next(bool skipConcurrent = false)
        {
            HitObject next = null;
            for (int i = hitObjectIndex + 1; i < beatmap.hitObjects.Count; ++i)
            {
                next = beatmap.hitObjects[i];
                if (!skipConcurrent || next.time != time)
                    break;
            }

            return next;
        }

        /// <summary> Returns the previous hit object in the hit objects list, if any,
        /// otherwise null, O(1). Optionally skips concurrent objects. </summary>
        public HitObject Prev(bool skipConcurrent = false)
        {
            HitObject prev = null;
            for (int i = hitObjectIndex - 1; i >= 0; --i)
            {
                prev = beatmap.hitObjects[i];
                if (!skipConcurrent || prev.time != time)
                    break;
            }

            return prev;
        }

        /// <summary> Returns the previous hit object in the hit objects list, if any,
        /// otherwise the first, O(1). Optionally skips concurrent objects. </summary>
        public HitObject PrevOrFirst(bool skipConcurrent = false) =>
            Prev(skipConcurrent) ?? beatmap.hitObjects.FirstOrDefault();

        /*
         *  Star Rating
         */

        /// <summary> <para>Returns the difference in time between the start of this object and the start of the previous object.</para>
        /// Note: This always returns at least 50 ms, to mimic the star rating algorithm.</summary>
        public double GetPrevDeltaStartTime()
        {
            // Smallest value is 50 ms for pp calc as a safety measure apparently,
            // it's equivalent to 375 BPM streaming speed.
            return Math.Max(50, time - PrevOrFirst().time);
        }

        /// <summary> <para>Returns the distance between the edges of the hit circles for the start of this object and the start of the previous object.</para>
        /// Note: This adds a bonus scaling factor for small circle sizes, to mimic the star rating algorithm.</summary>
        public double GetPrevStartDistance()
        {
            double radius = beatmap.difficultySettings.GetCircleRadius();

            // We will scale distances by this factor, so we can assume a uniform CircleSize among beatmaps.
            double scalingFactor = 52 / radius;

            // small circle bonus
            if (radius < 30)
                scalingFactor *= 1 + Math.Min(30 - radius, 5) / 50;

            Vector2 prevPosition = PrevOrFirst().Position;
            double prevDistance = (Position - prevPosition).Length();

            return prevDistance * scalingFactor;
        }

        /*
         *  Utility
         */

        /// <summary> Returns whether a hit object code has the given type. </summary>
        public static bool HasType(string[] args, Type type) =>
            ((Type)int.Parse(args[3]) & type) != 0;

        public bool HasType(Type type) =>
            (this.type & type) != 0;

        /// <summary> Returns whether the hit object has a hit sound, or optionally a certain type of hit sound. </summary>
        public bool HasHitSound(HitSound? hitSound = null) =>
            hitSound == null ?
                this.hitSound > 0 :
                (this.hitSound & hitSound) != 0;

        /// <summary> Returns the difference in time between the start of this object and the end of the previous object. </summary>
        public double GetPrevDeltaTime() =>
            time - beatmap.GetPrevHitObject(time).GetEndTime();

        /// <summary> Returns the difference in distance between the start of this object and the end of the previous object. </summary>
        public double GetPrevDistance()
        {
            HitObject prevObject = beatmap.GetPrevHitObject(time);

            Vector2 prevPosition = prevObject.Position;
            if (prevObject is Slider slider)
                prevPosition = slider.EndPosition;

            return (Position - prevPosition).Length();
        }

        /// <summary> Returns the points in time where heads, tails or reverses exist (i.e. the start, end or reverses of any object). </summary>
        public IEnumerable<double> GetEdgeTimes()
        {
            // Head counts as an edge.
            yield return time;

            if (this is Slider slider)
                for (int i = 0; i < slider.edgeAmount; ++i)
                    yield return time + slider.GetCurveDuration() * (i + 1);

            if (this is Spinner spinner)
                yield return spinner.endTime;

            if (this is HoldNote holdNote)
                yield return holdNote.endTime;
        }

        /// <summary> Returns the custom index for the object, if any, otherwise for the line, if any, otherwise 1. </summary>
        public int GetCustomIndex(TimingLine line = null)
        {
            if (line == null)
                line = beatmap.GetTimingLine(time);

            return customIndex ?? line?.customIndex ?? 1;
        }

        /// <summary> Returns the effective sampleset of the hit object (body for sliders), optionally prioritizing the addition. </summary>
        public Beatmap.Sampleset GetSampleset(bool additionOverrides = false, double? specificTime = null)
        {
            if (additionOverrides && addition != Beatmap.Sampleset.Auto)
                return addition;

            // Inherits from timing line if auto.
            return sampleset == Beatmap.Sampleset.Auto ?
                beatmap.GetTimingLine(specificTime ?? time, true).sampleset : sampleset;
        }

        /// <summary> Returns the effective sampleset of the head of the object, if applicable, otherwise null, optionally prioritizing the addition.
        /// Spinners have no start sample. </summary>
        public Beatmap.Sampleset? GetStartSampleset(bool additionOverrides = false) =>
            (this as Slider)?.GetStartSampleset(additionOverrides) ??
                ((this is Spinner) ? null : (Beatmap.Sampleset?)GetSampleset(additionOverrides));

        /// <summary> Returns the effective sampleset of the tail of the object, if applicable, otherwise null, optionally prioritizing the addition.
        /// Spinners have no start sample. </summary>
        public virtual Beatmap.Sampleset? GetEndSampleset(bool additionOverrides = false) =>
            (this as Slider)?.GetEndSampleset(additionOverrides) ??
                ((this is Spinner) ? (Beatmap.Sampleset?)GetSampleset(additionOverrides) : null);

        /// <summary> Returns the hit sound(s) of the head of the object, if applicable, otherwise null. 
        /// Spinners have no start sample. </summary>
        public HitSound? GetStartHitSound() =>
            (this as Slider)?.startHitSound ??
                ((this is Spinner) ? null : (HitSound?)hitSound);

        /// <summary> Returns the hit sound(s) of the tail of the object, if it applicable, otherwise null.
        /// Circles and hold notes have no end sample.</summary>
        public HitSound? GetEndHitSound() =>
            (this as Slider)?.endHitSound ?? (this as Spinner)?.hitSound ?? null;

        /// <summary> Returns the hit sound(s) of the slide of the object, if applicable, otherwise null.
        /// Circles, hold notes and spinners have no sliderslide. </summary>
        public HitSound? GetSliderSlide() =>
            (this as Slider)?.hitSound ?? null;

        /// <summary> Returns all individual hit sounds used by a specific hit sound instnace,
        /// excluding <see cref="HitSound.None"/>. </summary>
        private IEnumerable<HitSound> SplitHitSound(HitSound hitSound)
        {
            foreach (HitSound individualHitSound in Enum.GetValues(typeof(HitSound)))
                if ((hitSound & individualHitSound) != 0 && individualHitSound != HitSound.None)
                    yield return individualHitSound;
        }

        private HitSample GetEdgeSample(double time, Beatmap.Sampleset? sampleset, HitSound? hitSound)
        {
            TimingLine line = beatmap.GetTimingLine(time, hitSoundLeniency: true);
            return
                new HitSample(
                    line.customIndex,
                    sampleset ?? line.sampleset,
                    hitSound,
                    HitSample.HitSource.Edge,
                    time);
        }

        /// <summary> Returns all used combinations of customs, samplesets and hit sounds for this object. </summary>
        protected IEnumerable<HitSample> GetUsedHitSamples()
        {
            if (beatmap == null)
                // Without a beatmap, we don't know which samples are going to be used, so leave this empty.
                yield break;

            Beatmap.Mode mode = beatmap.generalSettings.mode;

            // Standard can be converted into taiko, so taiko samples could be used there too.
            if (mode == Beatmap.Mode.Taiko ||
                mode == Beatmap.Mode.Standard)
            {
                foreach (HitSample sample in GetUsedHitSamplesTaiko())
                    yield return sample;
            }
            
            if (mode != Beatmap.Mode.Taiko)
            {
                foreach (HitSample sample in GetUsedHitSamplesNonTaiko())
                    yield return sample;
            }
        }

        /// <summary> Returns all used combinations of customs, samplesets and hit sounds for this object.
        /// This assumes the game mode is not taiko (special rules apply to taiko only). </summary>
        private IEnumerable<HitSample> GetUsedHitSamplesNonTaiko()
        {
            // Spinners have no impact sound.
            if (!(this is Spinner))
            {
                // Head
                foreach (HitSound splitStartHitSound in SplitHitSound(GetStartHitSound().GetValueOrDefault()))
                    yield return GetEdgeSample(time, GetStartSampleset(true), splitStartHitSound);
                yield return GetEdgeSample(time, GetStartSampleset(false), HitSound.Normal);
            }

            // Hold notes can not have a hit sounds on their tails.
            if (!(this is HoldNote))
            {
                // Tail
                foreach (HitSound splitEndHitSound in SplitHitSound(GetEndHitSound().GetValueOrDefault()))
                    yield return GetEdgeSample(GetEndTime(), GetEndSampleset(true), splitEndHitSound);
                yield return GetEdgeSample(GetEndTime(), GetEndSampleset(false), HitSound.Normal);
            }

            if (this is Slider slider)
            {
                // Reverse
                for (int i = 0; i < slider.reverseHitSounds.Count; ++i)
                {
                    HitSound? reverseHitSound = slider.reverseHitSounds.ElementAt(i);

                    double theoreticalStart = time - beatmap.GetTheoreticalUnsnap(time);
                    double reverseTime = Timestamp.Round(theoreticalStart + slider.GetCurveDuration() * (i + 1));

                    foreach (HitSound splitReverseHitSound in SplitHitSound(reverseHitSound.GetValueOrDefault()))
                        yield return GetEdgeSample(reverseTime, slider.GetReverseSampleset(i, true), splitReverseHitSound);
                    yield return GetEdgeSample(reverseTime, slider.GetReverseSampleset(i), HitSound.Normal);
                }

                List<TimingLine> lines =
                    beatmap.timingLines.Where(line =>
                        line.offset > slider.time &&
                        line.offset <= slider.endTime).ToList();
                lines.Add(beatmap.GetTimingLine(slider.time, hitSoundLeniency: true));

                // Body, only applies to standard. Catch has droplets instead of body. Taiko and mania have a body but play no background sound.
                if (beatmap.generalSettings.mode == Beatmap.Mode.Standard)
                {
                    foreach (TimingLine line in lines)
                    {
                        // Priority: object sampleset > line sampleset
                        // The addition is ignored for sliderslides, it seems.
                        Beatmap.Sampleset effectiveSampleset =
                            sampleset != Beatmap.Sampleset.Auto ?
                                sampleset :
                                line.sampleset;

                        // Additions are not ignored for sliderwhistles, however.
                        if (slider.hitSound == HitSound.Whistle)
                            effectiveSampleset = addition != Beatmap.Sampleset.Auto ? addition : effectiveSampleset;

                        // The regular sliderslide will always play regardless of using sliderwhistle.
                        yield return new HitSample(
                            line.customIndex,
                            effectiveSampleset,
                            HitSound.None,
                            HitSample.HitSource.Body,
                            line.offset);

                        if (hitSound != HitSound.None)
                        {
                            yield return new HitSample(
                                line.customIndex,
                                effectiveSampleset,
                                hitSound,
                                HitSample.HitSource.Body,
                                line.offset);
                        }
                    }
                }

                // Tick, only applies to standard and catch. Mania has no ticks, taiko sliders play regular impacts.
                if (beatmap.generalSettings.mode == Beatmap.Mode.Standard ||
                    beatmap.generalSettings.mode == Beatmap.Mode.Catch)
                {
                    foreach (double tickTime in slider.sliderTickTimes)
                    {
                        TimingLine line = beatmap.GetTimingLine(tickTime);

                        // If no line exists, we use the default settings.
                        int customIndex = line?.customIndex ?? 1;

                        // Unlike the slider body (for sliderwhistles) and edges, slider ticks are unaffected by additions.
                        Beatmap.Sampleset sampleset = GetSampleset(false, tickTime);

                        // Defaults to normal if none is set (before any timing line).
                        if (sampleset == Beatmap.Sampleset.Auto)
                            sampleset = Beatmap.Sampleset.Normal;

                        yield return new HitSample(customIndex, sampleset, null, HitSample.HitSource.Tick, tickTime);
                    }
                }
            }
        }

        /// <summary> Returns all used combinations of customs, samplesets and hit sounds for this object.
        /// Assumes the game mode is taiko (special rules apply).
        /// <br></br><br></br>
        /// Special Rules:<br></br>
        /// - taiko-hitwhistle plays on big kat <br></br>
        /// - taiko-hitfinish plays on big don <br></br>
        /// - taiko-hitclap and taiko-hitnormal are always used as they play whenever the user presses keys
        /// </summary>
        public IEnumerable<HitSample> GetUsedHitSamplesTaiko()
        {
            TimingLine line = beatmap.GetTimingLine(time, hitSoundLeniency: true);

            yield return new HitSample(line?.customIndex ?? 1, line.sampleset, HitSound.Clap, HitSample.HitSource.Edge, line.offset, true);
            yield return new HitSample(line?.customIndex ?? 1, line.sampleset, HitSound.Normal, HitSample.HitSource.Edge, line.offset, true);

            bool isKat = HasHitSound(HitSound.Clap) || HasHitSound(HitSound.Whistle);
            bool isBig = HasHitSound(HitSound.Finish);

            HitSound hitSound;
            if (isBig)
                if (isKat)  hitSound = HitSound.Whistle;
                else        hitSound = HitSound.Finish;
            else
                if (isKat)  hitSound = HitSound.Clap;
                else        hitSound = HitSound.Normal;

            // In case the hit object's custom index/sampleset/additions are different from the timing line's.
            yield return new HitSample(GetCustomIndex(line), GetSampleset(true), hitSound, HitSample.HitSource.Edge, time, true);
        }

        /// <summary> Returns all potentially used hit sound file names (should they be
        /// in the song folder) for this object without extension. </summary>
        public IEnumerable<string> GetUsedHitSoundFileNames()
        {
            // If you supply a specific hit sound file to the object, this file will replace all
            // other hit sounds, customs, etc, including the hit normal.
            string specificHsFileName = null;
            if (filename != null)
            {
                if (filename.Contains("."))
                    specificHsFileName = filename.Substring(0, filename.IndexOf("."));
                else
                    specificHsFileName = filename;
            }

            if (specificHsFileName != null)
                return new List<string>() { specificHsFileName };

            IEnumerable<string> usedHitSoundFileNames =
                usedHitSamples
                    .Select(sample => sample.GetFileName())
                    .Where(name => name != null)
                    .Distinct();

            return usedHitSoundFileNames;
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

        /// <summary> Returns the name of the object part at the given time, for example "Slider head", "Slider reverse", "Circle" or "Spinner tail". </summary>
        public string GetPartName(double time)
        {
            // Checks within 2 ms leniency in case of decimals or unsnaps.
            bool isClose(double edgeTime, double otherTime) =>
                edgeTime <= otherTime + 2 &&
                edgeTime >= otherTime - 2;

            string edgeType =
                isClose(this.time, time)                                            ? "head" :
                isClose(GetEndTime(), time) || isClose(GetEdgeTimes().Last(), time) ? "tail" :
                GetEdgeTimes().Any(edgeTime => isClose(edgeTime, time))             ? "reverse" :
                "body";

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
