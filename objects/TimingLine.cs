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
        public readonly bool    kiai;

        public readonly Beatmap.Sampleset sampleset;
        public readonly int               customIndex;
        public readonly float             volume;

        // might not be explicit (depending on inherited or not)
        public float svMult;

        public TimingLine(string[] anArgs)
        {
            code = String.Join(',', anArgs);
            
            offset         = GetOffset(anArgs);
            meter          = GetMeter(anArgs);
            sampleset      = GetSampleset(anArgs);
            customIndex    = GetCustomIndex(anArgs);
            volume         = GetVolume(anArgs);
            uninherited    = IsUninherited(anArgs);
            kiai           = IsKiai(anArgs);

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

        // kiai
        private bool IsKiai(string[] anArgs)
        {
            return anArgs[7] == "1";
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
