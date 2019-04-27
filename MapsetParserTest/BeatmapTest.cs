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
    public class BeatmapTest
    {
        private static string relativePath = Directory.GetCurrentDirectory();
        private static BeatmapSet beatmapSet = new BeatmapSet(relativePath + @"\testcases\Rita - dorchadas");
        private static Beatmap beatmap = beatmapSet.beatmaps.First();
        
        [Fact]
        public void HitObjectsPopulatedTest() =>
            Assert.NotEmpty(beatmap.hitObjects);
        
        [Fact]
        public void TimingLinesPopulatedTest() =>
            Assert.NotEmpty(beatmap.timingLines);
        
        [Fact]
        public void MapPathTest() =>
            Assert.Equal(beatmap.GetOsuFileName(), beatmap.mapPath);
        
        [Theory]
        [InlineData(0, 74058)]
        [InlineData(2, 30770)]
        [InlineData(-1, 223019)]
        [InlineData(-2, 223082)]
        public void StackingTest(int aStackIndex, double aTime) =>
            Assert.Equal(aStackIndex, (beatmap.hitObjects.First(anObject => anObject.time == aTime) as Stackable).stackIndex);

        [Theory]
        [InlineData(36515, 36515)]
        [InlineData(36515, 36994)]
        public void GetTimingLineTest(double anObjectTime, double aTime) =>
            Assert.Equal(anObjectTime, beatmap.GetTimingLine(aTime).offset);

        [Theory]
        [InlineData(37472, 36515)]
        [InlineData(37472, 36994)]
        public void GetNextTimingLineTest(double anObjectTime, double aTime) =>
            Assert.Equal(anObjectTime, beatmap.GetNextTimingLine(aTime).offset);

        [Theory]
        [InlineData(36515, 36515)]
        [InlineData(36834, 36994)]
        public void GetHitObjectTest(double anObjectTime, double aTime) =>
            Assert.Equal(anObjectTime, beatmap.GetHitObject(aTime).time);

        [Theory]
        [InlineData(36834, 36515)]
        [InlineData(37153, 36994)]
        public void GetNextHitObjectTest(double anObjectTime, double aTime) =>
            Assert.Equal(anObjectTime, beatmap.GetNextHitObject(aTime).time);

        [Theory]
        [InlineData(36036, 36515)]
        [InlineData(36834, 36994)]
        public void GetPrevHitObjectTest(double anObjectTime, double aTime) =>
            Assert.Equal(anObjectTime, beatmap.GetPrevHitObject(aTime).time);

        [Fact]
        public void GetUnsnapIssueTest() =>
            Assert.Equal(3, beatmap.GetUnsnapIssue(56555));

        [Fact]
        public void GetMinorUnsnapIssueTest() =>
            Assert.Null(beatmap.GetUnsnapIssue(56557));

        [Theory]
        [InlineData(0, 105808)]
        [InlineData(3, 55433)]
        public void GetComboColourIndexTest(int anIndex, double aTime) =>
            Assert.Equal(anIndex, beatmap.GetComboColourIndex(aTime));

        [Theory]
        [InlineData(6, 105808)]
        [InlineData(3, 55433)]
        public void GetDisplayedComboColourIndexTest(int anIndex, double aTime) =>
            Assert.Equal(anIndex, beatmap.GetDisplayedComboColourIndex(aTime));
        
        [Fact]
        public void GetDraintimeTest() =>
            Assert.Equal(249590, beatmap.GetDraintime());

        [Fact]
        public void NoCountdownTest() =>
            Assert.True(beatmap.GetCountdownStartBeat() < 0);

        [Theory]
        [InlineData(158.96, 3483)]
        [InlineData(0.96, 3325)]
        public void GetOffsetIntoBeatTest(double anOffset, double aTime) =>
            Assert.Equal(anOffset, beatmap.GetOffsetIntoBeat(aTime), 2);

        [Theory]
        [InlineData(6, 3058)]
        [InlineData(6, 3057)]
        [InlineData(1, 3004)]
        [InlineData(0, 3167)]
        public void GetLowestDivisorTest(int aDivisor, double aTime) =>
            Assert.Equal(aDivisor, beatmap.GetLowestDivisor(aTime));

        [Theory]
        [InlineData(0.91, 3059)]
        [InlineData(-0.81, 3536)]
        public void GetTheoreticalUnsnapTest(double anUnsnap, double aTime) =>
            Assert.Equal(anUnsnap, beatmap.GetTheoreticalUnsnap(aTime), 2);
        
        [Theory]
        [InlineData(-1, 3059)]
        [InlineData(0, 3536)]
        [InlineData(1, 3535)]
        public void GetPracticalUnsnapTest(double anUnsnap, double aTime) =>
            Assert.Equal(anUnsnap, beatmap.GetPracticalUnsnap(aTime));

        public static IEnumerable<object[]> GetComboNumberData =>
            new List<object[]>
            {
                new object[] { 4, beatmap.GetHitObject(3962) },
                new object[] { 1, beatmap.GetHitObject(6834) },
                new object[] { 6, beatmap.GetHitObject(23749) }
            };

        [Theory]
        [MemberData(nameof(GetComboNumberData))]
        public void GetComboNumberTest(double aNumber, HitObject anObject) =>
            Assert.Equal(aNumber, beatmap.GetCombo(anObject));

        [Fact]
        public void GetOsuFileNameTest() =>
            Assert.Equal("Rita - dorchadas (Delis) [Mirash's Insane].osu", beatmap.GetOsuFileName());
    }
}
