using System.Globalization;

namespace MapsetParser.objects.timinglines
{
    public class UninheritedLine : TimingLine
    {
        public readonly double msPerBeat;
        public readonly double bpm;

        public UninheritedLine(string[] args, Beatmap beatmap)
            : base(args, beatmap)
        {
            msPerBeat = GetMsPerBeat(args);

            bpm = GetBPM();
        }

        /// <summary> Returns the miliseconds per beat of the uninherited line. </summary>
        private double GetMsPerBeat(string[] args) =>
            double.Parse(args[1], CultureInfo.InvariantCulture);

        /// <summary> Returns the beats per minute (BPM) of the uninherited line. </summary>
        private double GetBPM() => 60000 / msPerBeat;
    }
}
