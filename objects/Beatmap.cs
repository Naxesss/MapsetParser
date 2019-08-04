using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using MapsetParser.settings;
using MapsetParser.objects.events;
using MapsetParser.objects.hitobjects;
using MapsetParser.objects.timinglines;
using MapsetParser.starrating.standard;
using System.Numerics;
using MapsetParser.statics;

namespace MapsetParser.objects
{
    public class Beatmap
    {
        public readonly string code;
        public string songPath;
        public string mapPath;

        // star rating
        public float? starRating;

        // settings
        public GeneralSettings      generalSettings;
        public MetadataSettings     metadataSettings;
        public DifficultySettings   difficultySettings;
        public ColourSettings       colourSettings;

        // events
        public List<Background>     backgrounds;
        public List<Video>          videos;
        public List<Break>          breaks;
        public List<Sprite>         sprites;
        public List<StoryHitSound>  storyHitSounds;
        public List<Animation>      animations;

        // objects
        public List<TimingLine>     timingLines;
        public List<HitObject>      hitObjects;

        /// <summary> Which type of hit sounds are used, does not affect hitnormal if addition. </summary>
        public enum Sampleset
        {
            Auto,
            Normal,
            Soft,
            Drum
        }

        /// <summary> Which type of game mode the beatmap is for. </summary>
        public enum Mode
        {
            Standard,
            Taiko,
            Catch,
            Mania
        }

        /// <summary> Which type of difficulty level the beatmap is considered. </summary>
        public enum Difficulty
        {
            Easy,
            Normal,
            Hard,
            Insane,
            Expert,
            Ultra
        }

        public Beatmap(string aCode, float? aStarRating = null, string aSongPath = null, string aMapPath = null)
        {
            code       = aCode;
            songPath   = aSongPath;
            mapPath    = aMapPath;

            generalSettings    = ParserStatic.GetSettings(aCode, "General",     aSectionCode => new GeneralSettings(aSectionCode));
            metadataSettings   = ParserStatic.GetSettings(aCode, "Metadata",    aSectionCode => new MetadataSettings(aSectionCode));
            difficultySettings = ParserStatic.GetSettings(aCode, "Difficulty",  aSectionCode => new DifficultySettings(aSectionCode));
            colourSettings     = ParserStatic.GetSettings(aCode, "Colours",     aSectionCode => new ColourSettings(aSectionCode));

            // event type 3 seems to be "background colour transformation" https://i.imgur.com/Tqlz3s5.png
            
            backgrounds    = GetEvents(aCode, new List<string>() { "Background",   "0" }, aLine => new Background(aLine));
            videos         = GetEvents(aCode, new List<string>() { "Video",        "1" }, aLine => new Video(aLine));
            breaks         = GetEvents(aCode, new List<string>() { "Break",        "2" }, aLine => new Break(aLine));
            sprites        = GetEvents(aCode, new List<string>() { "Sprite",       "4" }, aLine => new Sprite(aLine));
            storyHitSounds = GetEvents(aCode, new List<string>() { "Sample",       "5" }, aLine => new StoryHitSound(aLine));
            animations     = GetEvents(aCode, new List<string>() { "Animation",    "6" }, aLine => new Animation(aLine));
            
            timingLines        = GetTimingLines(aCode);
            hitObjects         = GetHitobjects(aCode);

            if (generalSettings.mode == Mode.Standard)
            {
                // Stacking is standard-only.
                ApplyStacking();

                starRating = aStarRating ?? (float)StandardDifficultyCalculator.Calculate(this).Item3;
            }
        }

        /*
         *  Stacking Methods
        */

        /// <summary> Applies stacking for objects in the beatmap, updating the stack index and position values. </summary>
        private void ApplyStacking()
        {
            bool wasChanged;
            do
            {
                wasChanged = false;

                // Only hit objects that can be stacked can cause other objects to be stacked.
                List<Stackable> iteratedObjects = new List<Stackable>();
                foreach (Stackable hitObject in hitObjects.OfType<Stackable>())
                {
                    iteratedObjects.Add(hitObject);
                    foreach (Stackable otherHitObject in hitObjects.OfType<Stackable>().Except(iteratedObjects))
                    {
                        if (!MeetsStackTime(hitObject, otherHitObject))
                            break;

                        if ((hitObject is Circle || otherHitObject is Circle) &&
                            ShouldStack(hitObject, otherHitObject))
                        {
                            if (hitObject.stackIndex < 0)
                            {
                                // Objects stacked under slider tails will continue to stack downwards.
                                --otherHitObject.stackIndex;
                                wasChanged = true;
                                break;
                            }
                            else
                            {
                                ++hitObject.stackIndex;
                                wasChanged = true;
                                break;
                            }
                        }
                        
                        if (hitObject is Slider && ShouldStackTail(hitObject as Slider, otherHitObject))
                        {
                            // Slider tail on circle means the circle moves down,
                            // whereas slider tail on slider head means the first slider moves up.
                            if (otherHitObject is Circle)
                                --otherHitObject.stackIndex;
                            else
                                ++hitObject.stackIndex;
                            wasChanged = true;
                            break;
                        }
                    }
                }
            }
            while (wasChanged);
        }

        /// <summary> Returns whether two stackable objects should be stacked, but currently are not. </summary>
        private bool ShouldStack(Stackable anObject, Stackable anOtherObject)
        {
            bool isNearInTime = MeetsStackTime(anObject, anOtherObject);
            bool isNearInSpace = MeetsStackDistance(anObject, anOtherObject);
            bool wouldStackCorrectly =
                anObject.stackIndex == anOtherObject.stackIndex ||
                anObject.stackIndex < 0 && anObject.stackIndex < anOtherObject.stackIndex; // Allows negative stacks to line up.
            
            return isNearInTime && isNearInSpace && wouldStackCorrectly;
        }

        /// <summary> Returns whether a stackable following a slider should be stacked under the slider tail 
        /// (or slider over the head in case of slider and slider), but currently is not. </summary>
        private bool ShouldStackTail(Slider aSlider, Stackable anOtherObject)
        {
            double distanceSq =
                Vector2.DistanceSquared(
                    anOtherObject.UnstackedPosition,
                    aSlider.edgeAmount % 2 == 0 ?
                        aSlider.UnstackedPosition :
                        aSlider.UnstackedEndPosition);
            
            bool isNearInTime = MeetsStackTime(aSlider, anOtherObject);
            bool isNearInSpace = distanceSq < 3 * 3;
            bool wouldStackCorrectly = aSlider.stackIndex == anOtherObject.stackIndex;

            return isNearInTime && isNearInSpace && wouldStackCorrectly && aSlider.time < anOtherObject.time;
        }

        /// <summary> Returns whether two stackable objects are close enough in time to be stacked. Measures from end to start Hitsoundtime. </summary>
        private bool MeetsStackTime(Stackable anObject, Stackable anOtherObject) =>
            anOtherObject.time - anObject.GetEndTime() <= StackTimeThreshold();

        /// <summary> Returns whether two stackable objects are close enough in space to be stacked. Measures from head to head. </summary>
        private bool MeetsStackDistance(Stackable anObject, Stackable anOtherObject) =>
            Vector2.DistanceSquared(anObject.UnstackedPosition, anOtherObject.UnstackedPosition) < 3 * 3;

        /// <summary> Returns how far apart in time two objects can be and still be able to stack. </summary>
        private double StackTimeThreshold() =>
            difficultySettings.GetFadeInTime() * generalSettings.stackLeniency * 0.1;

        /*
         *  Helper Methods 
        */

        /// <summary> Returns the timing line currently in effect at the given time, optionally with a 5 ms backward leniency. </summary>
        public TimingLine GetTimingLine(double aTime, bool aHitSoundLeniency = false) => GetTimingLine<TimingLine>(aTime, aHitSoundLeniency);
        /// <summary> Same as <see cref="GetTimingLine"/> except only considers objects of a given type. </summary>
        public T GetTimingLine<T>(double aTime, bool aHitSoundLeniency = false) where T : TimingLine
        {
            return
                timingLines.OfType<T>().LastOrDefault( aLine =>
                    aLine.offset <= aTime + (aHitSoundLeniency ? 5 : 0)) ??
                GetNextTimingLine<T>(aTime);
        }

        /// <summary> Returns the next timing line after the current if any. </summary>
        public TimingLine GetNextTimingLine(double aTime) => GetNextTimingLine<TimingLine>(aTime);
        /// <summary> Same as <see cref="GetNextTimingLine"/> except only considers objects of a given type. </summary>
        public T GetNextTimingLine<T>(double aTime) where T : TimingLine
        {
            return
                timingLines.OfType<T>().FirstOrDefault(aLine =>
                    aLine.offset > aTime);
        }

        /// <summary> Returns the current or previous hit object if any, otherwise the next hit object. </summary>
        public HitObject GetHitObject(double aTime) => GetHitObject<HitObject>(aTime);
        /// <summary> Same as <see cref="GetHitObject"/> except only considers objects of a given type. </summary>
        public T GetHitObject<T>(double aTime) where T : HitObject
        {
            return
                hitObjects.OfType<T>().LastOrDefault(anObject =>
                    (anObject.GetEndTime() <= aTime || anObject.time == aTime)) ??
                GetNextHitObject<T>(aTime);
        }
        
        /// <summary> Returns the previous hit object if any, otherwise the first. </summary>
        public HitObject GetPrevHitObject(double aTime) => GetPrevHitObject<HitObject>(aTime);
        /// <summary> Same as <see cref="GetPrevHitObject"/> except only considers objects of a given type. </summary>
        public T GetPrevHitObject<T>(double aTime) where T : HitObject
        {
            return
                hitObjects.OfType<T>().LastOrDefault(aHitObject =>
                    aHitObject.time < aTime) ??
                hitObjects.OfType<T>().FirstOrDefault();
        }

        /// <summary> Returns the next hit object after the current if any. </summary>
        public HitObject GetNextHitObject(double aTime) => GetNextHitObject<HitObject>(aTime);
        /// <summary> Same as <see cref="GetNextHitObject"/> except only considers objects of a given type. </summary>
        public T GetNextHitObject<T>(double aTime) where T : HitObject
        {
            return
                hitObjects.OfType<T>().FirstOrDefault(anObject =>
                    anObject.time > aTime);
        }

        /// <summary> Returns the unsnap in ms of notes unsnapped by 2 ms or more, otherwise null. </summary>
        public double? GetUnsnapIssue(double aTime)
        {
            int thresholdUnrankable = 2;

            double unsnap      = GetPracticalUnsnap(aTime);
            double roundUnsnap = Math.Abs(unsnap);

            if (roundUnsnap >= thresholdUnrankable)
                return unsnap;

            return null;
        }

        /// <summary> Returns the current combo colour number, starts at 0. </summary>
        public int GetComboColourIndex(double aTime)
        {
            int combo = 0;
            foreach (HitObject hitObject in hitObjects)
            {
                if (hitObject.time > aTime)
                    break;

                // ignore spinners
                if (!hitObject.type.HasFlag(HitObject.Type.Spinner))
                {
                    int reverses = 0;

                    // has new combo
                    if (hitObject.type.HasFlag(HitObject.Type.NewCombo))
                        reverses += 1;

                    // accounts for the combo colour skips
                    for (int bit = 0x10; bit < 0x80; bit <<= 1)
                        if (((int)hitObject.type & bit) > 0)
                            reverses += (int)Math.Floor(bit / 16.0f);

                    // counts up and wraps around
                    for (int l = 0; l < reverses; l++)
                    {
                        combo += 1;
                        if (combo >= colourSettings.combos.Count)
                            combo = 0;
                    }
                }
            }
            return combo;
        }

        /// <summary> Same as <see cref="GetComboColourIndex"/>, except accounts for a bug which makes the last registered colour in
        /// the code the first number in the editor. Basically use for display purposes.</summary>
        public int GetDisplayedComboColourIndex(double aTime)
        {
            int colourIndex = GetComboColourIndex(aTime);
            if (colourIndex == 0)
                return colourSettings.combos.Count;
            else
                return colourIndex;
        }

        /// <summary> Returns whether a difficulty-specific storyboard is present, does not care about .osb files. </summary>
        public bool HasDifficultySpecificStoryboard()
        {
            if (sprites.Count > 0 || animations.Count > 0)
                return true;

            return false;
        }

        /// <summary> Returns the interpreted difficulty level based on the star rating of the beatmap
        /// (may be inaccurate since recent sr reworks were done), can optionally consider diff names. </summary>
        public Difficulty GetDifficulty(bool aConsiderDiffName = false)
        {
            Difficulty difficulty;

            if (starRating < 2.0f)      difficulty =  Difficulty.Easy;
            else if (starRating < 2.7f) difficulty = Difficulty.Normal;
            else if (starRating < 4.0f) difficulty = Difficulty.Hard;
            else if (starRating < 5.3f) difficulty = Difficulty.Insane;
            else if (starRating < 6.5f) difficulty = Difficulty.Expert;
            else                        difficulty = Difficulty.Ultra;

            if(!aConsiderDiffName)
                return difficulty;

            return GetDifficultyFromName() ?? difficulty;
        }

        /// <summary> A list of aliases for difficulty levels. Can't be ambigious with named top diffs, so something
        /// like "Lunatic", "Another", or "Special" which could be either Insane or top diff is no good.</summary>
        private readonly Dictionary<Difficulty, IEnumerable<string>> nameDiffPairs = new Dictionary<Difficulty, IEnumerable<string>>()
        {
            { Difficulty.Easy,   new List<string>(){ "Easy", "Beginner", "Basic", "Kantan", "Cup", "EZ" } },
            { Difficulty.Normal, new List<string>(){ "Normal", "Intermediate", "Medium", "Futsuu", "Salad", "NM" } },
            { Difficulty.Hard,   new List<string>(){ "Hard", "Advanced", "Muzukashii", "Platter", "HD" } },
            { Difficulty.Insane, new List<string>(){ "Insane", "Hyper", "Oni", "Rain", "MX" } },
            { Difficulty.Expert, new List<string>(){ "Expert", "Extra", "Extreme", "Inner Oni", "Ura Oni", "Overdose", "SC" } }
        };

        public Difficulty? GetDifficultyFromName()
        {
            string name = metadataSettings.version;

            foreach (var pair in nameDiffPairs)
                if (pair.Value.Any(
                    aValue =>
                        name.ToLower() == aValue.ToLower() ||
                        name.ToLower().StartsWith(aValue.ToLower() + " ") ||
                        name.ToLower().EndsWith(" " + aValue.ToLower()) ||
                        name.ToLower().Contains(" " + aValue.ToLower() + " ")))
                    return pair.Key;

            return null;
        }

        /// <summary> Returns the name of the difficulty in a gramatically correct way, for example "an Easy" and "a Normal".
        /// Mostly useful for adding in the middle of sentences.</summary>
        public string GetDifficultyName(Difficulty? aDifficulty = null)
        {
            switch (aDifficulty ?? GetDifficulty())
            {
                case Difficulty.Easy:   return "an Easy";
                case Difficulty.Normal: return "a Normal";
                case Difficulty.Hard:   return "a Hard";
                case Difficulty.Insane: return "an Insane";
                case Difficulty.Expert: return "an Expert";
                default:                return "an Ultra";
            }
        }

        /// <summary> Returns the complete drain time of the beatmap, accounting for breaks. </summary>
        public double GetDrainTime()
        {
            if (hitObjects.Count > 0)
            {
                double startTime = hitObjects.First().time;
                double endTime = hitObjects.Last().GetEndTime();

                // remove breaks
                double breakReduction = 0;
                foreach (Break @break in breaks)
                    breakReduction += @break.GetDuration(this);

                return endTime - startTime - breakReduction;
            }
            return 0;
        }

        /// <summary> Returns the play time of the beatmap, starting from the first object and ending at the end of the last object. </summary>
        public double GetPlayTime()
        {
            if (hitObjects.Count > 0)
            {
                double startTime = hitObjects.First().time;
                double endTime = hitObjects.Last().GetEndTime();

                return endTime - startTime;
            }
            return 0;
        }

        /// <summary> Returns the beat number from offset 0 at which the countdown would start, accounting for
        /// countdown offset and speed. No countdown if less than 0. </summary>
        public double GetCountdownStartBeat()
        {
            // If there are no objects, this does not apply.
            if (GetHitObject(0) == null)
                return 0;

            // always 6 beats before the first, but the first beat can be cut by having the first beat 5 ms after 0.
            UninheritedLine line = GetTimingLine<UninheritedLine>(0);

            double firstBeatTime = line.offset;
            while (firstBeatTime - line.msPerBeat > 0)
                firstBeatTime -= line.msPerBeat;

            double firstObjectTime = GetHitObject(0).time;
            int firstObjectBeat = Timestamp.Round((firstObjectTime - firstBeatTime) / line.msPerBeat);

            // Apparently double does not result in the countdown needing half as much time, but rather closer to 0.45 times as much.
            double countdownMultiplier =
                generalSettings.countdown == GeneralSettings.Countdown.None ? 1 :
                generalSettings.countdown == GeneralSettings.Countdown.Half ? 2 :
                0.45;

            return firstObjectBeat -
                ((firstBeatTime > 5 ? 5 : 6) + generalSettings.countdownBeatOffset) * countdownMultiplier;
        }

        /// <summary> Returns how many ms into a beat the given time is. </summary>
        public double GetOffsetIntoBeat(double aTime)
        {
            UninheritedLine line = GetTimingLine<UninheritedLine>(aTime);

            // gets how many miliseconds into a beat we are
            double time        = aTime - line.offset;
            double division    = time / line.msPerBeat;
            double fraction    = division - (float)Math.Floor(division);
            double beatOffset  = fraction * line.msPerBeat;

            return beatOffset;
        }

        private readonly int[] divisors = new int[] { 1, 2, 3, 4, 6, 8, 12, 16 };
        /// <summary> Returns the lowest possible beat snap divisor to get to the given time with less than 2 ms of unsnap, 0 if unsnapped. </summary>
        public int GetLowestDivisor(double aTime)
        {
            foreach (int divisor in divisors)
            {
                double unsnap = Math.Abs(GetPracticalUnsnap(aTime, divisor, 1));
                if (unsnap < 2)
                    return divisor;
            }
            
            return 0;
        }

        /// <summary> Returns the unsnap ignoring all of the game's rounding and other approximations. </summary>
        public double GetTheoreticalUnsnap(double aTime, int aSecondDivisor = 16, int aThirdDivisor = 12)
        {
            UninheritedLine line = GetTimingLine<UninheritedLine>(aTime);

            double beatOffset      = GetOffsetIntoBeat(aTime);
            double currentFraction = beatOffset / line.msPerBeat;

            // 1/16
            double desiredFractionSecond = (float)Math.Round(currentFraction * aSecondDivisor) / aSecondDivisor;
            double differenceFractionSecond = currentFraction - desiredFractionSecond;
            double theoreticalUnsnapSecond = differenceFractionSecond * line.msPerBeat;

            // 1/12
            double desiredFractionThird = (float)Math.Round(currentFraction * aThirdDivisor) / aThirdDivisor;
            double differenceFractionThird = currentFraction - desiredFractionThird;
            double theoreticalUnsnapThird = differenceFractionThird * line.msPerBeat;

            // picks the smaller of the two as unsnap
            return Math.Abs(theoreticalUnsnapThird) > Math.Abs(theoreticalUnsnapSecond)
                ? theoreticalUnsnapSecond : theoreticalUnsnapThird;
        }

        /// <summary> Returns the unsnap accounting for the way the game rounds (or more accurately doesn't round) snapping. <para/>
        /// The value returned is in terms of how much the object needs to be moved forwards in time to be snapped. </summary>
        public double GetPracticalUnsnap(double aTime, int aSecondDivisor = 16, int aThirdDivisor = 12)
        {
            double theoreticalUnsnap = GetTheoreticalUnsnap(aTime, aSecondDivisor, aThirdDivisor);
            
            double desiredTime = Timestamp.Round(aTime - theoreticalUnsnap);
            double practicalUnsnap = desiredTime - aTime;

            return practicalUnsnap;
        }

        /// <summary> Returns the combo number (the number you see on the notes), of a given hit object. </summary>
        public int GetCombo(HitObject aHitObject)
        {
            int combo = 1;

            // add a combo number for each object before this that isn't a new combo
            HitObject lastHitObject = aHitObject;
            while (true)
            {
                // if either there are no more objects behind it or it's a new combo, break
                if (lastHitObject == hitObjects.FirstOrDefault() || lastHitObject.type.HasFlag(HitObject.Type.NewCombo))
                    break;

                lastHitObject = GetPrevHitObject(lastHitObject.time);

                ++combo;
            }

            return combo;
        }

        /// <summary> Returns the full audio file path the beatmap uses if any such file exists, otherwise null. </summary>
        public string GetAudioFilePath()
        {
            if (songPath != null)
            {
                // read the mp3 file tags, if an audio file is specified
                string audioFileName = generalSettings.audioFileName;
                string mp3Path = songPath + "\\" + audioFileName;

                if (audioFileName.Length > 0 && File.Exists(mp3Path))
                    return mp3Path;
            }

            // no audio file
            return null;
        }

        /// <summary> Returns the expected file name of the .osu based on the beatmap's metadata. </summary>
        public string GetOsuFileName()
        {
            string songArtist     = metadataSettings.GetFileNameFiltered(metadataSettings.artist);
            string songTitle      = metadataSettings.GetFileNameFiltered(metadataSettings.title);
            string songCreator    = metadataSettings.GetFileNameFiltered(metadataSettings.creator);
            string version        = metadataSettings.GetFileNameFiltered(metadataSettings.version);

            return songArtist + " - " + songTitle + " (" + songCreator + ") [" + version + "].osu";
        }

        /*
         *  Parser Methods
        */
        
        private List<T> GetEvents<T>(string aCode, List<string> aTypes, Func<string, T> aFunc)
        {
            // find all lines starting with any of aTypes in the event section
            List<T> types = new List<T>();
            ParserStatic.GetSettings(aCode, "Events", aSection =>
            {
                foreach (string line in aSection.Split(new string[] { "\n" }, StringSplitOptions.None))
                    if (aTypes.Any(aType => line.StartsWith(aType + ",")))
                        types.Add(aFunc(line));
                return aSection;
            });
            return types;
        }

        private List<TimingLine> GetTimingLines(string aCode)
        {
            // find the [TimingPoints] section and parse each timing line
            return ParserStatic.ParseSection(aCode, "TimingPoints", aLine =>
            {
                return TimingLine.IsUninherited(aLine) ? new UninheritedLine(aLine) : (TimingLine)new InheritedLine(aLine);
            }).ToList();
        }

        private List<HitObject> GetHitobjects(string aCode)
        {
            // find the [Hitobjects] section and parse each hitobject until empty line or end of file
            return ParserStatic.ParseSection(aCode, "HitObjects", aLine =>
            {
                return
                    HitObject.HasType(aLine, HitObject.Type.Circle)        ? new Circle(aLine, this) :
                    HitObject.HasType(aLine, HitObject.Type.Slider)        ? new Slider(aLine, this) :
                    HitObject.HasType(aLine, HitObject.Type.ManiaHoldNote) ? new HoldNote(aLine, this) :
                    (HitObject)new Spinner(aLine, this);
            }).ToList();
        }

        /// <summary> Returns the beatmap as a string in the format "[Insane]", if the difficulty is called "Insane", for example. </summary>
        public override string ToString()
        {
            return "[" + metadataSettings.version + "]";
        }
    }
}
