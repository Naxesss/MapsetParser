using System.Globalization;

namespace MapsetParser.objects.events
{
    public class Break
    {
        // 2,66281,71774

        // 2, start, end

        /*  Notes:
         *  - Assuming no extensions of pre- and post break times or rounding errors,
         *      - pre break is 200 ms long
         *      - post break is 525 ms long
         *  - Saving the beatmap corrects any abnormal break times
         *  - Abnormal break times do not show up in the editor, but do in gameplay.
        */

        public readonly double time;
        public readonly double endTime;

        public Break(string[] anArgs)
        {
            time       = GetTime(anArgs);
            endTime    = GetEndTime(anArgs);
        }

        // start
        private double GetTime(string[] anArgs)
        {
            return double.Parse(anArgs[1], CultureInfo.InvariantCulture);
        }

        // end
        private double GetEndTime(string[] anArgs)
        {
            return double.Parse(anArgs[2], CultureInfo.InvariantCulture);
        }

        /// <summary> Returns the end time of the object before the break. </summary>
        public double GetRealStart(Beatmap aBeatmap)
        {
            return aBeatmap.GetPrevHitObject(time).GetEndTime();
        }

        /// <summary> Returns the start time of the object after the break. </summary>
        public double GetRealEnd(Beatmap aBeatmap)
        {
            return aBeatmap.GetNextHitObject(endTime).time;
        }

        /// <summary> Returns the duration between the end of the object before the break and the start of the
        /// object after it. During this time, no health will be drained. </summary>
        public double GetDuration(Beatmap aBeatmap)
        {
            return GetRealEnd(aBeatmap) - GetRealStart(aBeatmap);
        }
    }
}
