using System;
using System.Collections.Generic;
using System.Numerics;
using MapsetParser.starrating.utils;

namespace MapsetParser.objects.hitobjects.osu
{
    public static class OsuExtensions
    {
        private const float OBJECT_RADIUS = 64;

        public static int GetMaxCombo(this Beatmap beatmap)
        {
            int combo = 0;
            foreach (var h in beatmap.hitObjects)
                addCombo(h, ref combo);
            return combo;

            static void addCombo(HitObject hitObject, ref int combo)
            {
                if (hitObject is Circle)
                {
                    combo++;
                }
                else if (hitObject is Slider)
                {
                    combo += ((Slider)hitObject).GetSliderTickTimes().Count;
                    combo += ((Slider)hitObject).edgeAmount;
                }
                else if (hitObject is Spinner)
                {
                    combo++;
                }
            }
        }

        public static double Radius(this HitObject hitObject)
        {
            return OBJECT_RADIUS * hitObject.Scale();
        }

        public static Vector2 StackedPosition(this HitObject hitObject)
        {
            return hitObject.Position + hitObject.StackOffset();
        }

        public static Vector2 StackOffset(this HitObject hitObject)
        {
            var stackable = hitObject as Stackable;
            var stackHeight = 0;
            if (stackable != null)
            {
                stackHeight = stackable.stackIndex;
            }
            return new Vector2(stackHeight * hitObject.Scale() * -6.4f);
        }

        public static float Scale(this HitObject hitObject)
        {
            var circleSize = hitObject.beatmap.difficultySettings.circleSize;
            return CalculateScaleFromCircleSize(circleSize, true);
        }

        public static List<HitObject> NestedHitObjects(this Slider slider)
        {
            var nestedObjects = new List<HitObject>();
            foreach (var sliderTickTime in slider.GetSliderTickTimes())
            {
                var position = slider.GetPathPosition(sliderTickTime);
                var args = new String[] { position.X.ToString(), position.Y.ToString(), sliderTickTime.ToString() };
                var sliderTick = new SliderTick(args, slider.beatmap);
                nestedObjects.Add(sliderTick);
            }
            foreach (var sliderEdgeTime in slider.GetEdgeTimes())
            {
                if (sliderEdgeTime == slider.time || sliderEdgeTime == slider.endTime)
                {
                    // Ignore slider head and tail. Only take repeats.
                    continue;
                }
                var position = slider.GetPathPosition(sliderEdgeTime);
                var args = new String[] { position.X.ToString(), position.Y.ToString(), sliderEdgeTime.ToString() };
                var sliderRepeat = new SliderRepeat(args, slider.beatmap);
                nestedObjects.Add(sliderRepeat);
            }
            return nestedObjects;
        }

        public static double Duration(this Slider slider)
        {
            return slider.endTime - slider.time;
        }

        public static int RepeatCount(this Slider slider)
        {
            return Math.Max(slider.edgeAmount - 2, 0);
        }

        public static double SpanDuration(this Slider slider)
        {
            return slider.RepeatCount() + 1;
        }

        public static double SpanCount(this Slider slider)
        {
            return slider.Duration() / slider.SpanCount();
        }

        private static float CalculateScaleFromCircleSize(float circleSize, bool applyFudge = false)
        {
            // The following comment is copied verbatim from osu-stable:
            //
            //   Builds of osu! up to 2013-05-04 had the gamefield being rounded down, which caused incorrect radius calculations
            //   in widescreen cases. This ratio adjusts to allow for old replays to work post-fix, which in turn increases the lenience
            //   for all plays, but by an amount so small it should only be effective in replays.
            //
            // To match expectations of gameplay we need to apply this multiplier to circle scale. It's weird but is what it is.
            // It works out to under 1 game pixel and is generally not meaningful to gameplay, but is to replay playback accuracy.
            const float broken_gamefield_rounding_allowance = 1.00041f;

            return (float)(1.0f - 0.7f * DifficultyUtils.DifficultyRange(circleSize)) / 2 * (applyFudge ? broken_gamefield_rounding_allowance : 1);
        }
    }
}
