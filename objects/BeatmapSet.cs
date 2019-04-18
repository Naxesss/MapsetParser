using MapsetParser.objects.events;
using MapsetParser.objects.hitobjects;
using MapsetParser.settings;
using MapsetParser.statics;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MapsetParser.objects
{
    public class BeatmapSet
    {
        public List<Beatmap>   beatmaps;
        public Osb             osb;

        public string               songPath;
        public List<string>         songFilePaths  = new List<string>();
        public IEnumerable<string>  hitsoundFiles;

        private struct BeatmapFile
        {
            public string name;
            public string code;

            public BeatmapFile(string aName, string aCode)
            {
                name = aName;
                code = aCode;
            }
        }

        public BeatmapSet(string aBeatmapSetPath)
        {
            Track mapsetTrack = new Track("Parsing mapset \"" + PathStatic.CutPath(aBeatmapSetPath) + "\"...");

            beatmaps   = new List<Beatmap>();
            osb        = null;
            songPath   = aBeatmapSetPath;

            Initalize(aBeatmapSetPath);
            
            hitsoundFiles = GetHitsoundFiles().ToList();
            beatmaps = beatmaps.OrderBy(aBeatmap => aBeatmap.starRating).ToList();

            mapsetTrack.Complete();
        }

        public void Initalize(string aBeatmapSetPath)
        {
            if (!Directory.Exists(aBeatmapSetPath))
                throw new DirectoryNotFoundException("The folder \"" + aBeatmapSetPath + "\" does not exist.");

            string[] filePaths = Directory.GetFiles(aBeatmapSetPath, "*.*", SearchOption.AllDirectories);

            List<BeatmapFile> beatmapFiles = new List<BeatmapFile>();
            for (int i = 0; i < filePaths.Length; ++i)
            {
                songFilePaths.Add(filePaths[i]);
                if (filePaths[i].EndsWith(".osu"))
                {
                    string fileName = filePaths[i].Substring(songPath.Length + 1);
                    string code = File.ReadAllText(filePaths[i]);

                    beatmapFiles.Add(new BeatmapFile(fileName, code));
                }
            }

            ConcurrentBag<Beatmap> concurrentBeatmaps = new ConcurrentBag<Beatmap>();
            Parallel.ForEach(beatmapFiles, aBeatmapFile =>
            {
                Track beatmapTrack = new Track("Parsing " + aBeatmapFile.name + "...");
                concurrentBeatmaps.Add(new Beatmap(aBeatmapFile.code, null, songPath, aBeatmapFile.name));
                beatmapTrack.Complete();
            });

            foreach (Beatmap beatmap in concurrentBeatmaps)
                beatmaps.Add(beatmap);

            string expectedOsbFileName = GetOsbFileName();
            for (int i = 0; i < filePaths.Length; ++i)
            {
                string currentFileName = filePaths[i].Substring(songPath.Length + 1);
                if (filePaths[i].EndsWith(".osb") && currentFileName.ToLower() == expectedOsbFileName)
                {
                    Track osbTrack = new Track("Parsing " + currentFileName + "...");
                    osb = new Osb(File.ReadAllText(filePaths[i]));
                    osbTrack.Complete();
                }
            }
        }

        /// <summary> Returns the expected .osb file name based on the metadata of the first beatmap if any exists, otherwise null. </summary>
        public string GetOsbFileName()
        {
            MetadataSettings settings = beatmaps.FirstOrDefault()?.metadataSettings;
            if (settings == null)
                return null;

            string songArtist     = settings.GetFileNameFiltered(settings.artist);
            string songTitle      = settings.GetFileNameFiltered(settings.title);
            string songCreator    = settings.GetFileNameFiltered(settings.creator);
                
            return songArtist + " - " + songTitle + " (" + songCreator + ").osb";
        }

        /// <summary> Returns the full audio file path of the first beatmap in the set if one exists, otherwise null. </summary>
        public string GetAudioFilePath()
        {
            return beatmaps.FirstOrDefault(aBeatmap => aBeatmap != null)?.GetAudioFilePath() ?? null;
        }

        /// <summary> Returns the audio file name of the first beatmap in the set if one exists, otherwise null. </summary>
        public string GetAudioFileName()
        {
            return beatmaps.FirstOrDefault(aBeatmap => aBeatmap != null)?.generalSettings.audioFileName ?? null;
        }

        /// <summary> Returns whether or not a hit sound file is used based on its file name. </summary>
        public bool IsHitsoundFileUsed(string aFileName)
        {
            Regex hitsoundRegex = new Regex("(soft|normal|drum)-(slider(slide|whistle|tick)|hit(clap|finish|whistle|normal))(\\d+)?");
            if (hitsoundRegex.IsMatch(aFileName))
            {
                Match match = hitsoundRegex.Match(aFileName);

                string sample = match.Groups[1].ToString();
                string slide = match.Groups[3].ToString();
                string hitsound = match.Groups[4].ToString();
                int customIndex = match.Groups[5].ToString().Length > 0 ? int.Parse(match.Groups[5].ToString()) : 1;

                bool isTick = slide == "tick";

                HitObject.Hitsound parsedHitsound =
                    hitsound == "normal"    ? HitObject.Hitsound.Normal :
                    hitsound == "whistle"   ? HitObject.Hitsound.Whistle :
                    hitsound == "finish"    ? HitObject.Hitsound.Finish :
                    hitsound == "clap"      ? HitObject.Hitsound.Clap :
                                              HitObject.Hitsound.None;

                HitObject.Hitsound parsedSlide = 
                    slide == "slide"    ? HitObject.Hitsound.Normal :
                    slide == "whistle"  ? HitObject.Hitsound.Whistle :
                                          HitObject.Hitsound.None;

                Beatmap.Sampleset parsedSample = 
                    sample == "normal"  ? Beatmap.Sampleset.Normal :
                    sample == "soft"    ? Beatmap.Sampleset.Soft :
                    sample == "drum"    ? Beatmap.Sampleset.Drum :
                                          Beatmap.Sampleset.Auto;

                // if neither slide nor hs is set, or sample isn't set, and it's not a slidertick, then the format is wrong and it's going to be unused
                if (!((parsedHitsound == HitObject.Hitsound.None && parsedSlide == HitObject.Hitsound.None)
                    || parsedSample == Beatmap.Sampleset.Auto) || isTick)
                {
                    foreach (Beatmap beatmap in beatmaps)
                    {
                        foreach (HitObject hitObject in beatmap.hitObjects)
                        {
                            if (parsedHitsound == HitObject.Hitsound.Normal)
                            {
                                // hitnormals trigger every time and are unaffected by additions
                                IEnumerable<Tuple<int, Beatmap.Sampleset?, HitObject.Hitsound?>> usedHitnormals = hitObject.GetUsedHitsounds();
                                if (usedHitnormals.Any(aTuple => aTuple.Item1 == customIndex
                                                                && aTuple.Item2 == parsedSample))
                                    return true;
                            }
                            else if (parsedHitsound != HitObject.Hitsound.None)
                            {
                                // regular hitsounds, affected by additions which inherit samplesets
                                IEnumerable<Tuple<int, Beatmap.Sampleset?, HitObject.Hitsound?>> usedHitsounds = hitObject.GetUsedHitsounds(true);
                                if (usedHitsounds.Any(aTuple => aTuple.Item1 == customIndex
                                                                && aTuple.Item2 == parsedSample
                                                                && aTuple.Item3.GetValueOrDefault().HasFlag(parsedHitsound)))
                                    return true;
                            }
                            else if (hitObject is Slider)
                            {
                                Slider slider = hitObject as Slider;

                                // get all the new timing lines while the slider is in effect
                                // has a leniency of 5 ms behind the timingline, forward leniency doens't matter due to priority
                                IEnumerable<TimingLine> lines = beatmap.timingLines
                                    .Where(aLine => aLine.offset > slider.time && aLine.offset - 5 <= slider.endTime);

                                // sliderwhistle needs a whistle hitsound on the slider, also check so it's not none since that's for slider ticks
                                if (((HitObject.Hitsound)slider.hitsound).HasFlag(parsedSlide) || parsedSlide != HitObject.Hitsound.Whistle)
                                {
                                    // if any of them has the right custom then it's used
                                    // no need to do leniency on the first one since the lines already does that for us
                                    if (beatmap.GetTimingLine(slider.time).customIndex == customIndex
                                        || lines.Any(aLine => aLine.customIndex == customIndex))
                                        return true;
                                }
                                
                                if (isTick)
                                {
                                    IEnumerable<double> tickTimes = slider.GetSliderTickTimes();
                                    foreach (double tickTime in tickTimes)
                                    {
                                        TimingLine line = beatmap.GetTimingLine(tickTime);
                                        if (line.sampleset == parsedSample)
                                            return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        /// <summary> Returns whether the given full file path is used by the beatmapset. </summary>
        public bool IsFileUsed(string aFilePath)
        {
            string fileName       = aFilePath.Split(new char[] { '/', '\\' }).Last().ToLower();
            string parsedPath     = PathStatic.ParsePath(aFilePath);
            string strippedPath   = PathStatic.ParsePath(aFilePath, true);

            if (beatmaps.Any(aBeatmap => aBeatmap.generalSettings.audioFileName.ToLower() == parsedPath))
                return true;

            string firstStripped =
                PathStatic.ParsePath(
                    Directory.GetFiles(songPath, strippedPath + ".*").Last()
                ).Substring(songPath.Length + 1);

            // these are always used, but you won't be able to update them unless they have the right format
            if (fileName.EndsWith(".osu"))
                return true;
            
            if (beatmaps.Any(aBeatmap =>
                aBeatmap.sprites        .Any(anElement => anElement.path.ToLower() == parsedPath) ||
                aBeatmap.videos         .Any(anElement => anElement.path.ToLower() == parsedPath) ||
                aBeatmap.backgrounds    .Any(anElement => anElement.path.ToLower() == parsedPath) ||
                aBeatmap.animations     .Any(anElement => anElement.path.ToLower() == parsedPath) ||
                aBeatmap.storyHitsounds .Any(anElement => anElement.path.ToLower() == parsedPath)))
                return true;

            // if mPath is stripped, and go.png is over go.jpg, then if we're looking at go.jpg, it doesn't count

            // animations cannot be stripped of their extension
            if (beatmaps.Any(aBeatmap =>
                aBeatmap.sprites         .Any(anElement => anElement.strippedPath == strippedPath && firstStripped.StartsWith(anElement.path)) ||
                aBeatmap.videos          .Any(anElement => anElement.strippedPath == strippedPath && firstStripped.StartsWith(anElement.path)) ||
                aBeatmap.backgrounds     .Any(anElement => anElement.strippedPath == strippedPath && firstStripped.StartsWith(anElement.path)) ||
                aBeatmap.storyHitsounds  .Any(anElement => anElement.strippedPath == strippedPath && firstStripped.StartsWith(anElement.path))))
            {
                if(parsedPath == firstStripped)
                    return true;
            }

            if (osb != null && (
                osb.sprites       .Any(anElement => anElement.path.ToLower() == parsedPath) || 
                osb.videos        .Any(anElement => anElement.path.ToLower() == parsedPath) ||
                osb.backgrounds   .Any(anElement => anElement.path.ToLower() == parsedPath) ||
                osb.animations    .Any(anElement => anElement.path.ToLower() == parsedPath) ||
                osb.storyHitsounds.Any(anElement => anElement.path.ToLower() == parsedPath)))
                return true;

            if (osb != null && (
                osb.sprites       .Any(anElement => anElement.strippedPath == strippedPath && firstStripped.StartsWith(anElement.path)) ||
                osb.videos        .Any(anElement => anElement.strippedPath == strippedPath && firstStripped.StartsWith(anElement.path)) ||
                osb.backgrounds   .Any(anElement => anElement.strippedPath == strippedPath && firstStripped.StartsWith(anElement.path)) ||
                osb.storyHitsounds.Any(anElement => anElement.strippedPath == strippedPath && firstStripped.StartsWith(anElement.path))))
            {
                if (parsedPath == firstStripped)
                    return true;
            }

            if (beatmaps.Any(aBeatmap => aBeatmap.hitObjects.Any(anObject =>
                (anObject.filename != null ? PathStatic.ParsePath(anObject.filename, true) : null) == strippedPath)))
                return true;

            if (hitsoundFiles.Any(aHitsoundPath => PathStatic.ParsePath(aHitsoundPath) == parsedPath))
                return true;

            if (SkinStatic.IsUsed(fileName, this))
                return true;

            if (fileName == GetOsbFileName() && osb.IsUsed())
                return true;

            foreach (Beatmap beatmap in beatmaps)
                if (IsAnimationUsed(parsedPath, beatmap.animations))
                    return true;

            if (osb != null && IsAnimationUsed(parsedPath, osb.animations))
                return true;

            return false;
        }

        private bool IsAnimationUsed(string aFilePath, List<Animation> anAnimationList)
        {
            foreach (Animation animation in anAnimationList)
                foreach (string framePath in animation.framePaths)
                    if (framePath.ToLower() == aFilePath.ToLower())
                        return true;

            return false;
        }

        private IEnumerable<string> GetHitsoundFiles()
        {
            foreach (string filePath in songFilePaths)
            {
                string fileSongPath = filePath.Substring(songPath.Length + 1);
                string fileName = fileSongPath.Split(new char[] { '/', '\\' }).Last().ToLower();

                if (IsHitsoundFileUsed(fileName))
                    yield return fileSongPath;
            }
        }

        /// <summary> Returns the beatmapset as a string in the format "Artist - Title (Creator)". </summary>
        public override string ToString()
        {
            if (beatmaps.Count > 0)
            {
                MetadataSettings settings = beatmaps.First().metadataSettings;

                string songArtist     = settings.GetFileNameFiltered(settings.artist);
                string songTitle      = settings.GetFileNameFiltered(settings.title);
                string songCreator    = settings.GetFileNameFiltered(settings.creator);

                return songArtist + " - " + songTitle + " (" + songCreator + ")";
            }
            return "No beatmaps in set.";
        }
    }
}
