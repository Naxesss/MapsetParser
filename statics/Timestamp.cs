using MapsetParser.objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapsetParser.statics
{
    public static class Timestamp
    {
        public static string Get<T>(params T[] anObject)
        {
            if (anObject is HitObject[])
                return GetTimestamp((anObject as HitObject[])[0].beatmap, anObject as HitObject[]);
            else if (anObject is double[])
                return GetTimestamp(Math.Round(Convert.ToDouble((anObject as double[])[0])));
            else
                return "";
        }

        private static string GetTimestamp(double aTime)
        {
            double time = Math.Floor(aTime);

            if (time < 0)
                return time.ToString();

            double minutes = 0;
            while (time >= 60000)
            {
                time -= 60000;
                ++minutes;
            }

            double seconds = 0;
            while (time >= 1000)
            {
                time -= 1000;
                ++seconds;
            }

            string minuteString = minutes >= 10 ? minutes.ToString() : "0" + minutes;
            string secondString = seconds >= 10 ? seconds.ToString() : "0" + seconds;
            string milisecondsString =
                time >= 100 ? time.ToString() :
                time >= 10 ? "0" + time :
                "00" + time;

            return minuteString + ":" + secondString + ":" + milisecondsString + " - ";
        }

        private static string GetTimestamp(Beatmap aMap, params HitObject[] aHitObjects)
        {
            string timestamp = GetTimestamp(aHitObjects[0].time);
            timestamp = timestamp.Substring(0, timestamp.Length - 3);

            string objects = "";
            foreach (HitObject hitObject in aHitObjects)
                objects += (objects.Length > 0 ? "," : "") + aMap.GetCombo(hitObject);
            
            return timestamp + " (" + objects + ") - ";
        }
    }
}
