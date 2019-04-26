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
        public int      type;
        public int      hitsound;

        // extras
        // not all file versions have these, so they need to be nullable
        public Beatmap.Sampleset    sampleset;
        public Beatmap.Sampleset    addition;
        public int?                 customIndex;
        public int?                 volume;
        public string               filename       = null;

        /// <summary> Determines which sounds will be played as feedback (can be combined, thus bitflags). </summary>
        public enum Hitsound
        {
            None    = 0,
            Normal  = 1,
            Whistle = 2,
            Finish  = 4,
            Clap    = 8
        }

        /// <summary> Determines the properties of the hit object (can be combined, thus bitflags). </summary>
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
            hitsound   = GetHitsound(aCode);

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
                if(((type & (int)Type.Circle)           > 0 ||
                    (type & (int)Type.ManiaHoldNote)    > 0) &&
                    extras.Item5.Trim() != "")
                {
                    filename = PathStatic.ParsePath(extras.Item5);
                }
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
        
        private int GetTypeFlags(string aCode)
        {
            return int.Parse(aCode.Split(',')[3]);
        }
        
        private int GetHitsound(string aCode)
        {
            return int.Parse(aCode.Split(',')[4]);
        }
        
        private Tuple<Beatmap.Sampleset, Beatmap.Sampleset, int? ,int?, string> GetExtras(string aCode)
        {
            string extras = aCode.Split(',').Last();

            // hold notes have "endTime:extras" as format
            int index = ((Type)type).HasFlag(Type.ManiaHoldNote) ? 1 : 0;
            if (extras.Contains(":"))
            {
                Beatmap.Sampleset sampleset   = (Beatmap.Sampleset)int.Parse(extras.Split(':')[index]);
                Beatmap.Sampleset additions   = (Beatmap.Sampleset)int.Parse(extras.Split(':')[index + 1]);
                int? customIndex              = int.Parse(extras.Split(':')[index + 2]);

                // does not exist in file v11
                int? volume = null;
                if (extras.Split(':').Count() > index + 3)
                    volume = int.Parse(extras.Split(':')[index + 3]);

                string filename = "";
                if (extras.Split(':').Count() > index + 4)
                    filename = extras.Split(':')[index + 4];

                return Tuple.Create(sampleset, additions, customIndex, volume, filename);
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

        /// <summary> Returns whether the hit object has the given type. </summary>
        public bool HasType(Type aType)
        {
            return (type & (int)aType) != 0;
        }

        /// <summary> Returns whether a hit object code has the given type. </summary>
        public static bool HasType(string aCode, Type aType)
        {
            return ((Type)int.Parse(aCode.Split(',')[3])).HasFlag(aType);
        }

        /// <summary> Returns whether the hit object has a hit sound, or optionally a certain type of hit sound. </summary>
        public bool HasHitsound(Hitsound? aHitsound = null)
        {
            return aHitsound == null
                ? hitsound > 0
                : ((Hitsound)hitsound).HasFlag(aHitsound);
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
            if (prevObject is Slider)
                prevPosition = ((Slider)prevObject).EndPosition;

            return (Position - prevPosition).Length();
        }

        /// <summary> Returns the points in time where heads, tails or repeats exist (i.e. the start, end or reverses of any object). </summary>
        public IEnumerable<double> GetEdgeTimes()
        {
            yield return time;

            if (this is Slider)
                for(int i = 0; i <  ((Slider)this).edgeAmount; ++i)
                    yield return time + ((Slider)this).GetCurveDuration() * (i + 1);

            if (this is Spinner)
                yield return ((Spinner)this).endTime;

            if (this is HoldNote)
                yield return ((HoldNote)this).endTime;
        }

        /// <summary> Returns the sampleset of the hit object, optionally prioritizing the addition. </summary>
        public Beatmap.Sampleset GetSampleset(bool anAddition = false)
        {
            if (anAddition && addition != Beatmap.Sampleset.Auto)
                return addition;

            // inherits from timing line if auto
            return sampleset == Beatmap.Sampleset.Auto
                ? beatmap.GetTimingLine(time, false, true).sampleset : sampleset;
        }

        /// <summary> Returns the hit sound of the head of the object, if applicable, otherwise null. </summary>
        public Hitsound? GetHitsoundStart()
        {
            // spinners have no start
            return (this as Slider)?.startHitsound
                ?? ((this is Spinner) ? null
                 : (Hitsound?)hitsound);
        }

        /// <summary> Returns the hit sound of the tail of the object, if it applicable, otherwise null. </summary>
        public Hitsound? GetHitsoundEnd()
        {
            // circles and hold notes have no end
            return (this as Slider)?.endHitsound
                ?? (Hitsound?)(this as Spinner)?.hitsound
                ?? null;
        }

        /// <summary> Returns the hit sound of the slide of the object, if applicable, otherwise null. </summary>
        public Hitsound? GetSliderslide()
        {
            // circles, hold notes and spinners have no sliderslide
            return (Hitsound?)(this as Slider)?.hitsound
                 ?? null;
        }

        /// <summary> Returns all used combinations of customs, samplesets and hit sounds for this object. </summary>
        public IEnumerable<Tuple<int, Beatmap.Sampleset?, Hitsound?>> GetUsedHitsounds(bool anAddition = false)
        {
            yield return new Tuple<int, Beatmap.Sampleset?, Hitsound?>(
                beatmap.GetTimingLine(time, false, true).customIndex,
                GetSampleStart(anAddition), GetHitsoundStart());

            yield return new Tuple<int, Beatmap.Sampleset?, Hitsound?>(
                beatmap.GetTimingLine(GetEndTime(), false, true).customIndex,
                GetSampleEnd(anAddition), GetHitsoundEnd());

            // only runs if it's a slider with repeats
            for (int i = 0; i < ((this as Slider)?.repeatHitsounds.Count() ?? 0); ++i)
            {
                // functions as list pairs
                Hitsound?           hitsound  = (this as Slider)?.repeatHitsounds.ElementAt(i);
                Beatmap.Sampleset?  sampleset = (this as Slider)?.GetRepeatSampleset(i);
                Beatmap.Sampleset?  addition  = (this as Slider)?.repeatAdditions.Count() > 0 ?   // not a thing in file version 9
                                                (this as Slider)?.repeatAdditions.ElementAt(i) :
                                                Beatmap.Sampleset.Auto;

                yield return new Tuple<int, Beatmap.Sampleset?, Hitsound?>(
                    beatmap.GetTimingLine((this as Slider).GetCurveDuration() * (i + 1), false, true).customIndex,
                    anAddition && addition != Beatmap.Sampleset.Auto ? addition : sampleset, hitsound);
            }
        }

        /// <summary> Returns the sampleset of the head of the object, if applicable, otherwise null, optionally prioritizing the addition. </summary>
        public Beatmap.Sampleset? GetSampleStart(bool anAddition = false)
        {
            return (this as Slider)?.GetStartSampleset(anAddition) ?? ((this is Spinner) ? null : (Beatmap.Sampleset?)GetSampleset(anAddition));
        }

        /// <summary> Returns the sampleset of the tail of the object, if applicable, otherwise null, optionally prioritizing the addition. </summary>
        public Beatmap.Sampleset? GetSampleEnd(bool anAddition = false)
        {
            return (this as Slider)?.GetEndSampleset(anAddition) ?? ((this is Spinner) ? (Beatmap.Sampleset?)GetSampleset(anAddition) : null);
        }

        /// <summary> Returns the end time of the hit object, or the start time if no end time exists. </summary>
        public double GetEndTime()
        {
            // regardless of circle/slider/spinner/hold note, finds the end of the object
            return (this as Slider)?.endTime
                ?? (this as Spinner)?.endTime
                ?? (this as HoldNote)?.endTime
                ?? time;
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
                this is Slider   ? "Slider" :
                this is Circle   ? "Circle" :
                this is Spinner  ? "Spinner" :
                this is HoldNote ? "Hold note" :
                                   "Unknown object";

        public override string ToString() =>
            time + " ms: " + GetObjectType() + " at (" + Position.X + "; " + Position.Y + ")";
    }
}
