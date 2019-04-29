using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MapsetParserTest
{
    public class HitObjectTest
    {
        private static string relativePath = Directory.GetCurrentDirectory();
        private static BeatmapSet beatmapSet = new BeatmapSet(relativePath + @"\testcases\Rita - dorchadas");
        private static Beatmap beatmap = beatmapSet.beatmaps.First();
        
        [Theory]
        [InlineData(159, 22153)]
        [InlineData(319, 23749)]
        public void GetPrevDeltaTimeTest(double aDeltaTime, double aTime) =>
            Assert.Equal(aDeltaTime, beatmap.GetHitObject(aTime).GetPrevDeltaTime(), 2);

        [Theory]
        [InlineData(141.7, 22153)]
        [InlineData(5.95, 23749)]
        public void GetPrevDistanceTest(double aDistance, double aTime) =>
            Assert.Equal(aDistance, beatmap.GetHitObject(aTime).GetPrevDistance(), 2);

        [Theory]
        [InlineData(25026, 25026, 0)]
        [InlineData(25345.15, 25026, 1)]
        [InlineData(25664.3, 25026, 2)]
        public void GetEdgeTimesTest(double anEdgeTime, double aTime, int anIndex) =>
            Assert.Equal(anEdgeTime, beatmap.GetHitObject(aTime).GetEdgeTimes().ElementAt(anIndex), 2);

        [Theory]
        [InlineData(Beatmap.Sampleset.Soft, 22792)]
        [InlineData(Beatmap.Sampleset.Drum, 56558)]
        [InlineData(Beatmap.Sampleset.Normal, 73683)]
        public void GetSamplesetTest(Beatmap.Sampleset aSample, double aTime) =>
            Assert.Equal(aSample, beatmap.GetHitObject(aTime).GetSampleset());

        [Theory]
        [InlineData(HitObject.HitSound.None, 30292)]
        [InlineData(HitObject.HitSound.Finish, 18324)]
        [InlineData(HitObject.HitSound.Whistle | HitObject.HitSound.Finish, 76933)]
        [InlineData(null, 268707)]
        public void GetStartHitSoundTest(HitObject.HitSound? aHitSound, double aTime) =>
            Assert.Equal(aHitSound, beatmap.GetHitObject(aTime).GetStartHitSound());

        [Theory]
        [InlineData(HitObject.HitSound.Whistle, 64558)]
        [InlineData(null, 30292)]
        public void GetSliderSlideTest(HitObject.HitSound? aHitSound, double aTime) =>
            Assert.Equal(aHitSound, beatmap.GetHitObject(aTime).GetSliderSlide());

        [Fact]
        public void GetUsedHitSoundFileNamesTest()
        {
            IEnumerable<string> fileNames = beatmap.GetHitObject(76933).GetUsedHitSoundFileNames();
            Assert.Equal(
                new List<string>()
                {
                    "soft-hitwhistle52",
                    "soft-hitfinish52",
                    "normal-hitnormal52",
                    "soft-sliderslide52"
                },
                fileNames);
        }

        [Theory]
        [InlineData(62808, 62558)]
        [InlineData(19919, 19281)]
        [InlineData(61308, 61308)]
        [InlineData(270457, 268707)]
        public void GetEndTimeTest(double aEndTime, double aTime) =>
            Assert.Equal(aEndTime, beatmap.GetHitObject(aTime).GetEndTime(), 2);
    }
}
