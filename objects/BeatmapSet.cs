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
        public IEnumerable<string>  hitSoundFiles;

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

            Track hsTrack = new Track("Finding hit sound files...");
            hitSoundFiles = GetUsedHitSoundFiles().ToList();
            hsTrack.Complete();

            beatmaps = beatmaps.OrderBy(aBeatmap => aBeatmap.starRating).ToList();

            mapsetTrack.Complete();
        }

        private void Initalize(string aBeatmapSetPath)
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

            string expectedOsbFileName = GetOsbFileName()?.ToLower();
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

        /// <summary> Returns whichever of the given file names are unused. </summary>
        public List<string> GetUsedHitSoundFilesOf(IEnumerable<string> aFileNames)
        {
            List<string> usedFilesNames = new List<string>();

            foreach (Beatmap beatmap in beatmaps)
                foreach (HitObject hitObject in beatmap.hitObjects)
                    foreach (string usedFileName in hitObject.GetUsedHitSoundFiles())
                        foreach (string fileName in aFileNames)
                            if (fileName.StartsWith(usedFileName + ".") && !usedFilesNames.Contains(fileName))
                                usedFilesNames.Add(fileName);

            return usedFilesNames;
        }

        /// <summary> Returns whether the given full file path is used by the beatmapset. </summary>
        public bool IsFileUsed(string aFilePath)
        {
            string relativePath = PathStatic.RelativePath(aFilePath, songPath);
            string fileName     = relativePath.Split(new char[] { '/', '\\' }).Last().ToLower();
            string parsedPath   = PathStatic.ParsePath(relativePath);
            string strippedPath = PathStatic.ParsePath(relativePath, true);

            if (beatmaps.Any(aBeatmap => aBeatmap.generalSettings.audioFileName.ToLower() == parsedPath))
                return true;
            
            // When the path is "go", and "go.png" is over "go.jpg" in order, then "go.jpg" will be the one used.
            // So we basically want to find the last path which matches the name.
            string lastStripped =
                PathStatic.ParsePath(
                    Directory.GetFiles(songPath, strippedPath + ".*").LastOrDefault()
                )?.Substring(songPath.Length + 1);

            if (lastStripped == null)
                return false;

            // these are always used, but you won't be able to update them unless they have the right format
            if (fileName.EndsWith(".osu"))
                return true;
            
            if (beatmaps.Any(aBeatmap =>
                aBeatmap.sprites        .Any(anElement => anElement.path.ToLower() == parsedPath) ||
                aBeatmap.videos         .Any(anElement => anElement.path.ToLower() == parsedPath) ||
                aBeatmap.backgrounds    .Any(anElement => anElement.path.ToLower() == parsedPath) ||
                aBeatmap.animations     .Any(anElement => anElement.path.ToLower() == parsedPath) ||
                aBeatmap.storyHitSounds .Any(anElement => anElement.path.ToLower() == parsedPath)))
                return true;

            // animations cannot be stripped of their extension
            if (beatmaps.Any(aBeatmap =>
                aBeatmap.sprites         .Any(anElement => anElement.strippedPath == strippedPath && lastStripped.StartsWith(anElement.path)) ||
                aBeatmap.videos          .Any(anElement => anElement.strippedPath == strippedPath && lastStripped.StartsWith(anElement.path)) ||
                aBeatmap.backgrounds     .Any(anElement => anElement.strippedPath == strippedPath && lastStripped.StartsWith(anElement.path)) ||
                aBeatmap.storyHitSounds  .Any(anElement => anElement.strippedPath == strippedPath && lastStripped.StartsWith(anElement.path))) &&
                parsedPath == lastStripped)
            {
                return true;
            }

            if (osb != null && (
                osb.sprites       .Any(anElement => anElement.path.ToLower() == parsedPath) || 
                osb.videos        .Any(anElement => anElement.path.ToLower() == parsedPath) ||
                osb.backgrounds   .Any(anElement => anElement.path.ToLower() == parsedPath) ||
                osb.animations    .Any(anElement => anElement.path.ToLower() == parsedPath) ||
                osb.storyHitSounds.Any(anElement => anElement.path.ToLower() == parsedPath)))
                return true;

            if (osb != null && (
                osb.sprites       .Any(anElement => anElement.strippedPath == strippedPath && lastStripped.StartsWith(anElement.path)) ||
                osb.videos        .Any(anElement => anElement.strippedPath == strippedPath && lastStripped.StartsWith(anElement.path)) ||
                osb.backgrounds   .Any(anElement => anElement.strippedPath == strippedPath && lastStripped.StartsWith(anElement.path)) ||
                osb.storyHitSounds.Any(anElement => anElement.strippedPath == strippedPath && lastStripped.StartsWith(anElement.path))) &&
                parsedPath == lastStripped)
            {
                return true;
            }

            if (beatmaps.Any(aBeatmap => aBeatmap.hitObjects.Any(anObject =>
                (anObject.filename != null ? PathStatic.ParsePath(anObject.filename, true) : null) == strippedPath)))
                return true;

            if (hitSoundFiles.Any(aHitSoundPath => PathStatic.ParsePath(aHitSoundPath) == parsedPath))
                return true;

            if (SkinStatic.IsUsed(fileName, this))
                return true;

            if (fileName == GetOsbFileName().ToLower() && osb.IsUsed())
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

        /// <summary> Returns all used hit sound files in the folder. </summary>
        private IEnumerable<string> GetUsedHitSoundFiles()
        {
            IEnumerable<string> hitSoundFilePaths =
                songFilePaths.Select(aPath => aPath.Substring(songPath.Length + 1));

            IEnumerable<string> usedHitSoundFiles =
                GetUsedHitSoundFilesOf(hitSoundFilePaths.Select(aPath =>
                    aPath.Split(new char[] { '/', '\\' }).Last().ToLower()));

            return usedHitSoundFiles;
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
