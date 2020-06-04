using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MapsetParser.objects
{
    public class TimingLine
    {
        // 440,476.190476190476,4,2,1,40,1,0
        // offset, msPerBeat, meter, sampleset, customIndex, volume, inherited, kiai

        public Beatmap beatmap;
        public string code;
        private int timingLineIndex;

        public readonly double  offset;
        public readonly int     meter;         // this exists for both green and red lines but only red uses it
        public readonly bool    uninherited;

        private readonly Type   type;
        public readonly bool    kiai;
        public readonly bool    omitsBarLine;

        public readonly Beatmap.Sampleset sampleset;
        public readonly int               customIndex;
        public readonly float             volume;

        // might not be explicit (depending on inherited or not)
        public float svMult;

        [Flags]
        public enum Type
        {
            Kiai = 1,
            OmitBarLine = 8
        }

        public TimingLine(string[] args, Beatmap beatmap)
        {
            this.beatmap = beatmap;
            code = String.Join(",", args);
            
            offset       = GetOffset(args);
            meter        = GetMeter(args);
            sampleset    = GetSampleset(args);
            customIndex  = GetCustomIndex(args);
            volume       = GetVolume(args);
            uninherited  = IsUninherited(args);

            type         = GetType(args);
            kiai         = type.HasFlag(Type.Kiai);
            omitsBarLine = type.HasFlag(Type.OmitBarLine);

            // may not be explicit
            svMult = GetSvMult(args);
        }

        /// <summary> Returns the offset of the line. </summary>
        private double GetOffset(string[] args) =>
            double.Parse(args[0], CultureInfo.InvariantCulture);

        /// <summary> Returns the meter (i.e. timing signature) of the line. </summary>
        private int GetMeter(string[] args) =>
            int.Parse(args[2]);

        /// <summary> Returns the sampleset which this line applies to any sample set to Auto sampleset. </summary>
        private Beatmap.Sampleset GetSampleset(string[] args) =>
            (Beatmap.Sampleset)int.Parse(args[3]);

        /// <summary> Returns the custom sample index of the line. </summary>
        private int GetCustomIndex(string[] args) =>
            int.Parse(args[4]);

        /// <summary> Returns the sample volume of the line. </summary>
        private float GetVolume(string[] args) =>
            float.Parse(args[5], CultureInfo.InvariantCulture);
        
        /// <summary> Returns whether a line of code representing a timing line is uninherited or inherited. </summary>
        public static bool IsUninherited(string[] args)
        {
            // Does not exist in file version 5.
            if (args.Length > 6)
                return args[6] == "1";
            return true;
        }

        /// <summary> Returns whether kiai is enabled for this line. </summary>
        private Type GetType(string[] args)
        {
            // Does not exist in file version 5.
            if (args.Length > 7)
                return (Type)int.Parse(args[7]);
            return 0;
        }

        /// <summary> Returns the slider velocity multiplier (1 for uninherited lines). Fit into range 0.1 - 10 before returning. </summary>
        public float GetSvMult(string[] args)
        {
            if (!IsUninherited(args))
            {
                float svMult = 1 / (float.Parse(args[1], CultureInfo.InvariantCulture) * -0.01f);

                // Min 0.1x, max 10x.
                if (svMult > 10f)  svMult = 10f;
                if (svMult < 0.1f) svMult = 0.1f;

                return svMult;
            }
            else
                return 1;
        }

        /*
         *  Next / Prev
         */

        /// <summary> Returns the index of this timing line in the beatmap's timing line list, O(1). </summary>
        public int GetTimingLineIndex() => timingLineIndex;
        /// <summary> Sets the index of this timing line. This should reflect the index in the timing line list of the beatmap.
        /// Only use this if you're changing the order of lines or adding new ones after parsing. </summary>
        public void SetTimingLineIndex(int index) => timingLineIndex = index;

        /// <summary> Returns the next timing line in the timing line list, if any,
        /// otherwise null, O(1). Optionally skips concurrent lines. </summary>
        public TimingLine Next(bool skipConcurrent = false)
        {
            TimingLine next = null;
            for (int i = timingLineIndex; i < beatmap.timingLines.Count; ++i)
            {
                next = beatmap.timingLines[i];
                if (!skipConcurrent || next.offset != offset)
                    break;
            }

            return next;
        }

        /// <summary> Returns the previous timing line in the timing line list, if any,
        /// otherwise null, O(1). Optionally skips concurrent objects. </summary>
        public TimingLine Prev(bool skipConcurrent = false)
        {
            TimingLine prev = null;
            for (int i = timingLineIndex; i >= 0; --i)
            {
                prev = beatmap.timingLines[i];
                if (!skipConcurrent || prev.offset != offset)
                    break;
            }

            return prev;
        }

        /// <summary> Returns the previous timing line in the timing line list, if any,
        /// otherwise the first, O(1). Optionally skips concurrent objects. </summary>
        public TimingLine PrevOrFirst(bool skipConcurrent = false) =>
            Prev(skipConcurrent) ?? beatmap.timingLines.FirstOrDefault();
    }
}
