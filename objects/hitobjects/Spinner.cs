using System.Globalization;

namespace MapsetParser.objects.hitobjects
{
    public class Spinner : HitObject
    {
        public readonly double endTime;

        public Spinner(string[] args, Beatmap beatmap)
            : base(args, beatmap)
        {
            endTime = GetEndTime(args);
        }

        private double GetEndTime(string[] args) =>
            double.Parse(args[5], CultureInfo.InvariantCulture);
    }
}
