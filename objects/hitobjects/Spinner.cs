using System.Globalization;

namespace MapsetParser.objects.hitobjects
{
    public class Spinner : HitObject
    {
        public readonly double endTime;

        public Spinner(string[] anArgs, Beatmap aBeatmap)
            : base(anArgs, aBeatmap)
        {
            endTime = GetEndTime(anArgs);
        }

        private double GetEndTime(string[] anArgs)
        {
            return double.Parse(anArgs[5], CultureInfo.InvariantCulture);
        }
    }
}
