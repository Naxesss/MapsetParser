using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace MapsetParser.objects
{
    public class TimingLine
    {
        // 440,476.190476190476,4,2,1,40,1,0
        // offset, msPerBeat, meter, sampleset, customIndex, volume, inherited, kiai

        public string code;

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

        public TimingLine(string[] anArgs)
        {
            code = String.Join(",", anArgs);
            
            offset       = GetOffset(anArgs);
            meter        = GetMeter(anArgs);
            sampleset    = GetSampleset(anArgs);
            customIndex  = GetCustomIndex(anArgs);
            volume       = GetVolume(anArgs);
            uninherited  = IsUninherited(anArgs);

            type         = GetType(anArgs);
            kiai         = type.HasFlag(Type.Kiai);
            omitsBarLine = type.HasFlag(Type.OmitBarLine);

            // may not be explicit
            svMult = GetSvMult(anArgs);
        }

        // offset
        private double GetOffset(string[] anArgs)
        {
            return double.Parse(anArgs[0], CultureInfo.InvariantCulture);
        }

        // meter
        private int GetMeter(string[] anArgs)
        {
            return int.Parse(anArgs[2]);
        }

        // sampleset
        private Beatmap.Sampleset GetSampleset(string[] anArgs)
        {
            return (Beatmap.Sampleset)int.Parse(anArgs[3]);
        }

        // customIndex
        private int GetCustomIndex(string[] anArgs)
        {
            return int.Parse(anArgs[4]);
        }

        // volume
        private float GetVolume(string[] anArgs)
        {
            return float.Parse(anArgs[5], CultureInfo.InvariantCulture);
        }
        
        /// <summary> Returns whether a line of code representing a timing line is uninherited or inherited. </summary>
        public static bool IsUninherited(string[] anArgs)
        {
            return anArgs[6] == "1";
        }

        // kiai (does not exist in file version 5)
        private Type GetType(string[] anArgs)
        {
            if(anArgs.Length > 7)
                return (Type)int.Parse(anArgs[7]);
            return 0;
        }

        /// <summary> Returns the slider velocity multiplier (1 for uninherited lines). </summary>
        public float GetSvMult(string[] anArgs)
        {
            if (!IsUninherited(anArgs))
                return 1 / (float.Parse(anArgs[1], CultureInfo.InvariantCulture) * -0.01f);
            else
                return 1;
        }
    }
}
