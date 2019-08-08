using System.Globalization;

namespace MapsetParser.objects.timinglines
{
    public class UninheritedLine : TimingLine
    {
        public readonly double msPerBeat;
        public readonly double bpm;

        // red lines
        public UninheritedLine(string[] anArgs)
            : base(anArgs)
        {
            msPerBeat = GetMsPerBeat(anArgs);

            bpm = GetBPM();
        }

        // msPerBeat (uninherited) / svMult (inherited)
        private double GetMsPerBeat(string[] anArgs)
        {
            return double.Parse(anArgs[1], CultureInfo.InvariantCulture);
        }

        private double GetBPM()
        {
            return 60000 / msPerBeat;
        }
    }
}
