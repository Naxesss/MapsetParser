using System.Globalization;

namespace MapsetParser.objects.hitobjects
{
    public class HoldNote : HitObject
    {
        // 448,192,243437,128,2,247861:0:0:0:0:
        // x, y, time, typeFlags, hitsound, endTime:extras
        
        public readonly double endTime;

        public HoldNote(string[] anArgs, Beatmap aBeatmap)
            : base(anArgs, aBeatmap)
        {
            endTime = GetEndTime(anArgs);
        }
        
        private double GetEndTime(string[] anArgs)
        {
            return double.Parse(anArgs[5].Split(':')[0], CultureInfo.InvariantCulture);
        }
    }
}
