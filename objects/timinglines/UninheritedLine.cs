using System.Globalization;

namespace MapsetParser.objects.timinglines
{
    public class UninheritedLine : TimingLine
    {
        public readonly double msPerBeat;
        public readonly double bpm;

        // red lines
        public UninheritedLine(string aCode)
            : base(aCode)
        {
            msPerBeat = GetMsPerBeat(aCode);

            bpm = GetBPM();
        }

        // msPerBeat (uninherited) / svMult (inherited)
        private double GetMsPerBeat(string aCode)
        {
            return double.Parse(aCode.Split(',')[1], CultureInfo.InvariantCulture);
        }

        private double GetBPM()
        {
            return 60000 / msPerBeat;
        }
    }
}
