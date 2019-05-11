using System.Globalization;

namespace MapsetParser.objects.hitobjects
{
    public class Spinner : HitObject
    {
        public readonly double endTime;

        public Spinner(string aCode, Beatmap aBeatmap)
            : base(aCode, aBeatmap)
        {
            endTime = GetEndTime(aCode);
        }

        private double GetEndTime(string aCode)
        {
            return double.Parse(aCode.Split(',')[5], CultureInfo.InvariantCulture);
        }
    }
}
