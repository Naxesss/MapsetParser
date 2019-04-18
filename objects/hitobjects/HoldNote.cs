using System.Globalization;

namespace MapsetParser.objects.hitobjects
{
    public class HoldNote : HitObject
    {
        // 448,192,243437,128,2,247861:0:0:0:0:
        // x, y, time, typeFlags, hitsound, endTime:extras
        
        public double endTime;

        public HoldNote(string aCode, Beatmap aBeatmap)
            : base(aCode, aBeatmap)
        {
            endTime = GetEndTime(aCode);
        }
        
        private double GetEndTime(string aCode)
        {
            return double.Parse(aCode.Split(',')[5].Split(':')[0], CultureInfo.InvariantCulture);
        }
    }
}
