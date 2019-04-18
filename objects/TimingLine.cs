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

        public double               offset;
        public int                  meter;         // this exists for both green and red lines but only red uses it
        public Beatmap.Sampleset    sampleset;
        public int                  customIndex;
        public float                volume;
        public bool                 uninherited;
        public bool                 kiai;

        // might not be explicit (depending on inherited or not)
        public float svMult;

        public TimingLine(string aCode)
        {
            code = aCode;

            offset         = GetOffset(aCode);
            meter          = GetMeter(aCode);
            sampleset      = GetSampleset(aCode);
            customIndex    = GetCustomIndex(aCode);
            volume         = GetVolume(aCode);
            uninherited    = IsUninherited(aCode);
            kiai           = IsKiai(aCode);

            // may not be explicit
            svMult = GetSvMult(aCode);
        }

        // offset
        private double GetOffset(string aCode)
        {
            return double.Parse(aCode.Split(',')[0], CultureInfo.InvariantCulture);
        }

        // meter
        private int GetMeter(string aCode)
        {
            return int.Parse(aCode.Split(',')[2]);
        }

        // sampleset
        private Beatmap.Sampleset GetSampleset(string aCode)
        {
            return (Beatmap.Sampleset)int.Parse(aCode.Split(',')[3]);
        }

        // customIndex
        private int GetCustomIndex(string aCode)
        {
            return int.Parse(aCode.Split(',')[4]);
        }

        // volume
        private float GetVolume(string aCode)
        {
            return float.Parse(aCode.Split(',')[5], CultureInfo.InvariantCulture);
        }
        
        /// <summary> Returns whether a line of code representing a timing line is uninherited or inherited. </summary>
        public static bool IsUninherited(string aCode)
        {
            return aCode.Split(',')[6] == "1";
        }

        // kiai
        private bool IsKiai(string aCode)
        {
            return aCode.Split(',')[7] == "1";
        }

        /// <summary> Returns the slider velocity multiplier (1 for uninherited lines). </summary>
        public float GetSvMult(string aCode)
        {
            if (!IsUninherited(aCode))
                return 1 / (float.Parse(aCode.Split(',')[1], CultureInfo.InvariantCulture) * -0.01f);
            else
                return 1;
        }
    }
}
