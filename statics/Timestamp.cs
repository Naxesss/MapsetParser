using MapsetParser.objects;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapsetParser.statics
{
    public static class Timestamp
    {
        /// <summary> Returns the given time as an integer in the way the game rounds time values. </summary>
        /// <remarks>
        ///     Interestingly, the game currently does not round, but rather cast to integer. This may
        ///     change in future versions of the game to fix issues such as 1 ms rounding errors when
        ///     copying objects, however.
        /// </remarks>
        public static int Round(double aTime) => (int)aTime;

        /// <summary> Returns the timestamp of a given time. If decimal, is rounded in the same way the game rounds. </summary>
        public static string Get(double aTime) =>
            GetTimestamp(aTime);

        /// <summary> Returns the timestamp of given hit objects, so the timestamp includes the object(s). </summary>
        public static string Get(params HitObject[] anObjects) =>
            GetTimestamp(anObjects[0].beatmap, anObjects);

        private static string GetTimestamp(double aTime)
        {
            double time = Round(aTime);

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
            {
                string objectRef;
                if (aMap.generalSettings.mode == Beatmap.Mode.Mania)
                {
                    int row =
                        hitObject.Position.X == 64 ? 0 :
                        hitObject.Position.X == 192 ? 1 :
                        hitObject.Position.X == 320 ? 2 :
                        hitObject.Position.X == 448 ? 3 :
                        -1;

                    objectRef = hitObject.time + "|" + row;
                }
                else
                    objectRef = aMap.GetCombo(hitObject).ToString();

                objects += (objects.Length > 0 ? "," : "") + objectRef;
            }
            
            return timestamp + " (" + objects + ") - ";
        }
    }
}
