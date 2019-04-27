using MapsetParser.objects;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Xunit;

namespace MapsetParserTest
{
    public class BeatmapSetTest
    {
        private static string relativePath = Directory.GetCurrentDirectory();

        private static BeatmapSet dorchadasSet         = new BeatmapSet(relativePath + @"\testcases\Rita - dorchadas");
        private static BeatmapSet realityDistortionSet = new BeatmapSet(relativePath + @"\testcases\Camellia Vs Akira Complex - Reality Distortion");

        [Fact]
        public void LoadNonExistingTest() =>
            Assert.Throws<DirectoryNotFoundException>(() => new BeatmapSet(relativePath + @"\testcases\none"));

        public static IEnumerable<object[]> BeatmapSetData =>
            new List<object[]>
            {
                new object[] { dorchadasSet },
                new object[] { realityDistortionSet }
            };

        [Theory]
        [MemberData(nameof(BeatmapSetData))]
        public void LoadedBeatmapTest(BeatmapSet aBeatmapSet) =>
            Assert.Single(aBeatmapSet.beatmaps);

        [Theory]
        [MemberData(nameof(BeatmapSetData))]
        public void LoadedOsbTest(BeatmapSet aBeatmapSet) =>
            Assert.NotNull(aBeatmapSet.osb);

        [Fact]
        public void GetOsbFileNameTest() =>
            Assert.Equal("Rita - dorchadas (Delis).osb", dorchadasSet.GetOsbFileName());

        [Fact]
        public void GetAudioFileNameTest() =>
            Assert.Equal("Dorchadas.mp3", dorchadasSet.GetAudioFileName());

        [Fact]
        public void UsedHitSoundLoadedTest() =>
            Assert.Single(dorchadasSet.hitsoundFiles);

        [Fact]
        public void UsedHitSoundLoadedCorrectlyTest() =>
            Assert.Equal("soft-hitclap.wav", dorchadasSet.hitsoundFiles.FirstOrDefault());

        public static IEnumerable<object[]> IsHitSoundFileUsedData =>
            dorchadasSet.hitsoundFiles.Select(aFile => new object[] { aFile, true });

        [Theory]
        [MemberData(nameof(IsHitSoundFileUsedData))]
        [InlineData("soft-hitclap99.wav", false)]
        public void IsHitSoundFileUsedTest(string aFileName, bool aUsed) =>
            Assert.Equal(aUsed, dorchadasSet.IsHitSoundFileUsed(aFileName));

        [Fact]
        public void IsOsuUsedTest() =>
            Assert.True(dorchadasSet.IsFileUsed("Rita - dorchadas (Delis) [Mirash's Insane].osu"));

        public static IEnumerable<object[]> IsOsbUsedData =>
            new List<object[]>
            {
                new object[] { dorchadasSet, "Rita - dorchadas (Delis).osb", false },
                new object[] { realityDistortionSet, "Camellia Vs Akira Complex - Reality Distortion (rrtyui).osb", true }
            };

        [Theory]
        [MemberData(nameof(IsOsbUsedData))]
        public void IsOsbUsedTest(BeatmapSet aBeatmapSet, string aFileName, bool aUsed) =>
            Assert.Equal(aUsed, aBeatmapSet.IsFileUsed(aFileName));

        [Theory]
        [InlineData(@"sb\element\realnoise\noise_3.png", true)]
        [InlineData(@"sb\element\realnoise\noise_4.png", false)]
        public void IsAnimationUsedTest(string aPath, bool aUsed) =>
            Assert.Equal(aUsed, realityDistortionSet.IsFileUsed(aPath));
    }
}
