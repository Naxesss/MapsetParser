using MapsetParser.objects;
using System;
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

        [Fact]
        public void LoadedBeatmapTest() =>
            Assert.Single(dorchadasSet.beatmaps);

        [Fact]
        public void LoadedOsbTest() =>
            Assert.NotNull(dorchadasSet.osb);

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
        public void IsHitSoundFileUsedTest()
        {
            foreach(string hitSoundFile in dorchadasSet.hitsoundFiles)
                Assert.True(dorchadasSet.IsHitSoundFileUsed(hitSoundFile));

            Assert.True(dorchadasSet.IsHitSoundFileUsed("soft-hitclap.wav"));
            Assert.False(dorchadasSet.IsHitSoundFileUsed("soft-hitclap99.wav"));
        }

        [Fact]
        public void IsOsuUsedTest() =>
            Assert.True(dorchadasSet.IsFileUsed("Rita - dorchadas (Delis) [Mirash's Insane].osu"));

        [Fact]
        public void IsOsbUsedTest()
        {
            // The .osb is only used if it actually contains something.
            Assert.False(dorchadasSet.IsFileUsed("Rita - dorchadas (Delis).osb"));
            Assert.True(realityDistortionSet.IsFileUsed("Camellia Vs Akira Complex - Reality Distortion (rrtyui).osb"));
        }

        [Fact]
        public void IsAnimationUsedTest()
        {
            Assert.True(realityDistortionSet.IsFileUsed(@"sb\element\realnoise\noise_3.png"));
            Assert.False(realityDistortionSet.IsFileUsed(@"sb\element\realnoise\noise_4.png"));
        }
    }
}
