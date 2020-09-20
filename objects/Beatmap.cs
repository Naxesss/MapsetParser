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
using System.Text.RegularExpressions;
using System.Collections.Concurrent;

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
        public List<Sample>         samples;
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

        public Beatmap(string code, float? starRating = null, string songPath = null, string mapPath = null)
        {
            this.code       = code;
            this.songPath   = songPath;
            this.mapPath    = mapPath;

            string[] lines = code.Split(new string[] { "\n" }, StringSplitOptions.None);

            generalSettings    = ParserStatic.GetSettings(lines, "General",     sectionLines => new GeneralSettings(sectionLines));
            metadataSettings   = ParserStatic.GetSettings(lines, "Metadata",    sectionLines => new MetadataSettings(sectionLines));
            difficultySettings = ParserStatic.GetSettings(lines, "Difficulty",  sectionLines => new DifficultySettings(sectionLines));
            colourSettings     = ParserStatic.GetSettings(lines, "Colours",     sectionLines => new ColourSettings(sectionLines));

            // event type 3 seems to be "background colour transformation" https://i.imgur.com/Tqlz3s5.png
            
            backgrounds = GetEvents(lines, new List<string>() { "Background",   "0" }, args => new Background(args));
            videos      = GetEvents(lines, new List<string>() { "Video",        "1" }, args => new Video(args));
            breaks      = GetEvents(lines, new List<string>() { "Break",        "2" }, args => new Break(args));
            sprites     = GetEvents(lines, new List<string>() { "Sprite",       "4" }, args => new Sprite(args));
            samples     = GetEvents(lines, new List<string>() { "Sample",       "5" }, args => new Sample(args));
            animations  = GetEvents(lines, new List<string>() { "Animation",    "6" }, args => new Animation(args));
            
            timingLines = GetTimingLines(lines);
            hitObjects  = GetHitobjects(lines);

            if (generalSettings.mode == Mode.Standard)
            {
                // Stacking is standard-only.
                ApplyStacking();

                this.starRating = starRating ?? (float)StandardDifficultyCalculator.Calculate(this).Item3;
            }
        }

        /*
         *  Optimization Methods
         */

        private static class ThreadSafeCacheHelper<T>
        {
            // Works under the assumption that hit objects and timing lines are immutable per beatmap id, which is the case.
            internal static readonly ConcurrentDictionary<(string, Type), List<T>> cache = new ConcurrentDictionary<(string, Type), List<T>>();
        }

        public static void ClearCache()
        {
            // Includes all types that can be given to `GetTimingLine` methods.
            ThreadSafeCacheHelper<TimingLine>.cache.Clear();
            ThreadSafeCacheHelper<InheritedLine>.cache.Clear();
            ThreadSafeCacheHelper<UninheritedLine>.cache.Clear();

            // Includes all types that can be given to `GetHitObject` methods.
            ThreadSafeCacheHelper<HitObject>.cache.Clear();
            ThreadSafeCacheHelper<Circle>.cache.Clear();
            ThreadSafeCacheHelper<Slider>.cache.Clear();
            ThreadSafeCacheHelper<Spinner>.cache.Clear();
            ThreadSafeCacheHelper<HoldNote>.cache.Clear();
        }

        private List<T> GetOrAdd<T>(Type t, Func<List<T>> func)
        {
            (string, Type) key = (mapPath, t);
            if (!ThreadSafeCacheHelper<T>.cache.ContainsKey(key))
                ThreadSafeCacheHelper<T>.cache[key] = func();
            return ThreadSafeCacheHelper<T>.cache[key];
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
                List<Stackable> stackableHitObjects = hitObjects.OfType<Stackable>().ToList();
                for (int i = 0; i < stackableHitObjects.Count - 1; ++i)
                {
                    for (int j = i + 1; j < stackableHitObjects.Count; ++j)
                    {
                        Stackable hitObject = stackableHitObjects[i];
                        Stackable otherHitObject = stackableHitObjects[j];

                        if (!MeetsStackTime(hitObject, otherHitObject))
                            break;

                        if (hitObject is Circle || otherHitObject is Circle)
                        {
                            if (ShouldStack(hitObject, otherHitObject))
                            {
                                if (otherHitObject is Slider || otherHitObject.isOnSlider)
                                    hitObject.isOnSlider = true;

                                // Sliders are never less than 0 stack index.
                                // Circles go below 0 when stacked under slider tails.
                                if (hitObject.stackIndex < 0 && !hitObject.isOnSlider)
                                {
                                    // Objects stacked under slider tails will continue to stack downwards.
                                    otherHitObject.stackIndex = hitObject.stackIndex - 1;
                                    wasChanged = true;
                                    break;
                                }
                                else
                                {
                                    hitObject.stackIndex = otherHitObject.stackIndex + 1;
                                    wasChanged = true;
                                    break;
                                }
                            }
                            else if (IsStacked(hitObject, otherHitObject))
                                break;
                        }
                        
                        if (hitObject is Slider)
                        {
                            if (ShouldStackTail(hitObject as Slider, otherHitObject))
                            {
                                // Slider tail on circle means the circle moves down,
                                // whereas slider tail on slider head means the first slider moves up.
                                // Only sliders later in time can move sliders earlier in time.
                                if (otherHitObject is Slider || otherHitObject.isOnSlider)
                                {
                                    hitObject.isOnSlider = true;
                                    hitObject.stackIndex = otherHitObject.stackIndex + 1;
                                }
                                else
                                    otherHitObject.stackIndex = hitObject.stackIndex - 1;

                                wasChanged = true;
                                break;
                            }
                            else if (IsStackedTail(hitObject as Slider, otherHitObject))
                                break;
                        }
                    }
                }
            }
            while (wasChanged);
        }

        /// <summary> Returns whether two stackable objects could be stacked. </summary>
        private bool CanStack(Stackable stackable, Stackable otherStackable)
        {
            bool isNearInTime = MeetsStackTime(stackable, otherStackable);
            bool isNearInSpace = MeetsStackDistance(stackable, otherStackable);

            return isNearInTime && isNearInSpace;
        }

        /// <summary> Returns whether two stackable objects are currently stacked. </summary>
        private bool IsStacked(Stackable stackable, Stackable otherStackable)
        {
            bool isAlreadyStacked = stackable.stackIndex == otherStackable.stackIndex + 1;

            return CanStack(stackable, otherStackable) && isAlreadyStacked;
        }

        /// <summary> Returns whether two stackable objects should be stacked, but currently are not. </summary>
        private bool ShouldStack(Stackable stackable, Stackable otherStackable) =>
            CanStack(stackable, otherStackable) && !IsStacked(stackable, otherStackable);

        /// <summary> Returns whether a stackable following a slider could be stacked under the tail
        /// (or over in case of slider and slider). </summary>
        private bool CanStackTail(Slider slider, Stackable stackable)
        {
            double distanceSq =
                Vector2.DistanceSquared(
                    stackable.UnstackedPosition,
                    slider.edgeAmount % 2 == 0 ?
                        slider.UnstackedPosition :
                        slider.UnstackedEndPosition);

            bool isNearInTime = MeetsStackTime(slider, stackable);
            bool isNearInSpace = distanceSq < 3 * 3;

            return isNearInTime && isNearInSpace && slider.time < stackable.time;
        }

        /// <summary> Returns whether a stackable following a slider is stacked under the tail
        /// (or over in case of slider and slider). </summary>
        private bool IsStackedTail(Slider slider, Stackable stackable)
        {
            bool isAlreadyStacked = slider.stackIndex == stackable.stackIndex + 1;

            return CanStackTail(slider, stackable) && isAlreadyStacked;
        }

        /// <summary> Returns whether a stackable following a slider should be stacked under the slider tail 
        /// (or slider over the head in case of slider and slider), but currently is not. </summary>
        private bool ShouldStackTail(Slider slider, Stackable stackable)
        {
            return CanStackTail(slider, stackable) && !IsStackedTail(slider, stackable);
        }

        /// <summary> Returns whether two stackable objects are close enough in time to be stacked. Measures from start to start time. </summary>
        private bool MeetsStackTime(Stackable stackable, Stackable otherStackable) =>
            otherStackable.time - stackable.time <= StackTimeThreshold();

        /// <summary> Returns whether two stackable objects are close enough in space to be stacked. Measures from head to head. </summary>
        private bool MeetsStackDistance(Stackable stackable, Stackable otherStackable) =>
            Vector2.DistanceSquared(stackable.UnstackedPosition, otherStackable.UnstackedPosition) < 3 * 3;

        /// <summary> Returns how far apart in time two objects can be and still be able to stack. </summary>
        private double StackTimeThreshold() =>
            difficultySettings.GetFadeInTime() * generalSettings.stackLeniency * 0.1;

        /*
         *  Helper Methods 
        */

        /// <summary> Returns the element in the sorted list where the given time is greater
        /// than the element time, but less than the next element time (e.g. the line in effect
        /// at some point in time, if we give a list of timing lines).
        /// <br></br><br></br>
        /// Since the list is sorted, we can use the Binary Search algorithm here to get
        /// O(logn) time complexity, instead of O(n), which we would get from linear searching. </summary>
        private int BinaryTimeSearch<T>(List<T> sortedList, Func<T, double> Time, double time, int start=0, int end=int.MaxValue)
        {
            while (true)
            {
                if (start < 0)
                    // Given time is before the list starts, so there is no current element.
                    return -1;

                if (end > sortedList.Count - 1)
                    end = sortedList.Count - 1;
                if (end < 0)
                    end = 0;

                if (start == end)
                    // Given time is after the list ends, so the last element in the list must be the current.
                    return end;

                int i = start + (end - start) / 2;

                T cur = sortedList[i];
                T next = sortedList[i + 1];

                if (time >= Time(cur) && time < Time(next))
                    return i;

                else if (time >= Time(next))
                    // Element is too far back, move forward.
                    start = i + 1;

                else
                    // Element is too far forward, move back.
                    end = i - 1;
            }
        }

        /// <summary> Returns the timing line currently in effect at the given time, if any, otherwise the first, O(logn).
        /// Optionally with a 5 ms backward leniency for hit sounding. </summary>
        public TimingLine GetTimingLine(double time, bool hitSoundLeniency = false) => GetTimingLine<TimingLine>(time, hitSoundLeniency);
        /// <summary> Same as <see cref="GetTimingLine"/> except only considers objects of a given type. </summary>
        public T GetTimingLine<T>(double time, bool hitSoundLeniency = false) where T : TimingLine
        {
            // Cache the results per generic type; timing line and hit object lists are immutable,
            // meaning we always expect the same result from the same input.
            List<T> list = GetOrAdd(typeof(T), () => timingLines.OfType<T>().ToList());
            if (list.Count == 0)
                return null;

            int index = BinaryTimeSearch(list, line => line.offset - (hitSoundLeniency ? 5 : 0), time);
            if (index < 0)
                // Before any timing line starts, so return first line.
                return list[0];

            return list[index];
        }

        /// <summary> Returns the next timing line, if any, otherwise null, O(logn). </summary>
        public TimingLine GetNextTimingLine(double time) => GetNextTimingLine<TimingLine>(time);
        /// <summary> Same as <see cref="GetNextTimingLine"/> except only considers objects of a given type. </summary>
        public T GetNextTimingLine<T>(double time) where T : TimingLine
        {
            List<T> list = GetOrAdd(typeof(T), () => timingLines.OfType<T>().ToList());
            if (list.Count == 0)
                return null;

            int index = BinaryTimeSearch(list, line => line.offset, time);
            if (index < 0)
                // Before any timing line starts, so return first line.
                return list[0];
            if (index + 1 >= list.Count)
                // After last timing line, so there's no next.
                return null;

            return list[index + 1];
        }

        /// <summary> Returns the current or previous hit object if any, otherwise the first, O(logn). </summary>
        public HitObject GetHitObject(double time) => GetHitObject<HitObject>(time);
        /// <summary> Same as <see cref="GetHitObject"/> except only considers objects of a given type. </summary>
        public T GetHitObject<T>(double time) where T : HitObject
        {
            List<T> list = GetOrAdd(typeof(T), () => hitObjects.OfType<T>().ToList());
            if (list.Count == 0)
                return null;

            int index = BinaryTimeSearch(list, obj => obj.time, time);
            if (index < 0)
                // Before first hit object, so return first one.
                return list[0];

            return list[index];
        }

        /// <summary> Returns the previous hit object if any, otherwise the first, O(logn). </summary>
        public HitObject GetPrevHitObject(double time) => GetPrevHitObject<HitObject>(time);
        /// <summary> Same as <see cref="GetPrevHitObject"/> except only considers objects of a given type. </summary>
        public T GetPrevHitObject<T>(double time) where T : HitObject
        {
            List<T> list = GetOrAdd(typeof(T), () => hitObjects.OfType<T>().ToList());
            if (list.Count == 0)
                return null;

            int index = BinaryTimeSearch(list, obj => obj.time, time);
            if (index - 1 < 0)
                // Before the first object, so return the first one.
                return list[0];

            if (list[index].GetEndTime() < time)
                // Directly in front of the previous object.
                return list[index];

            return list[index - 1];
        }

        /// <summary> Returns the next hit object after the current, if any, otherwise null, O(logn). </summary>
        public HitObject GetNextHitObject(double time) => GetNextHitObject<HitObject>(time);
        /// <summary> Same as <see cref="GetNextHitObject"/> except only considers objects of a given type. </summary>
        public T GetNextHitObject<T>(double time) where T : HitObject
        {
            List<T> list = GetOrAdd(typeof(T), () => hitObjects.OfType<T>().ToList());
            if (list.Count == 0)
                return null;

            int index = BinaryTimeSearch(list, obj => obj.time, time);
            if (index < 0)
                // Before first hit object, so return first one.
                return list[0];
            if (index + 1 >= list.Count)
                // After last hit object, so there is no next.
                return null;

            return list[index + 1];
        }

        /// <summary> Returns the unsnap in ms of notes unsnapped by 2 ms or more, otherwise null. </summary>
        public double? GetUnsnapIssue(double time)
        {
            int thresholdUnrankable = 2;

            double unsnap = GetPracticalUnsnap(time);
            if (Math.Abs(unsnap) >= thresholdUnrankable)
                return unsnap;

            return null;
        }

        /// <summary> Returns the current combo colour number, starts at 0. </summary>
        public int GetComboColourIndex(double time)
        {
            int combo = 0;
            foreach (HitObject hitObject in hitObjects)
            {
                if (hitObject.time > time)
                    break;

                // ignore spinners
                if (!hitObject.HasType(HitObject.Type.Spinner))
                {
                    int reverses = 0;

                    // has new combo
                    if (hitObject.HasType(HitObject.Type.NewCombo))
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
        public int GetDisplayedComboColourIndex(double time) =>
            AsDisplayedComboColourIndex(GetComboColourIndex(time));

        /// <summary> Accounts for a bug which makes the last registered colour in
        /// the code the first number in the editor. Basically use for display purposes.</summary>
        public int AsDisplayedComboColourIndex(int zeroBasedIndex) =>
            zeroBasedIndex == 0 ? colourSettings.combos.Count : zeroBasedIndex;

        /// <summary> Returns whether a difficulty-specific storyboard is present, does not care about .osb files. </summary>
        public bool HasDifficultySpecificStoryboard()
        {
            if (sprites.Count > 0 || animations.Count > 0)
                return true;

            return false;
        }

        /// <summary> Returns the interpreted difficulty level based on the star rating of the beatmap
        /// (may be inaccurate since recent sr reworks were done), can optionally consider diff names. </summary>
        public Difficulty GetDifficulty(bool considerName = false)
        {
            Difficulty difficulty;

            if (starRating < 2.0f)      difficulty =  Difficulty.Easy;
            else if (starRating < 2.7f) difficulty = Difficulty.Normal;
            else if (starRating < 4.0f) difficulty = Difficulty.Hard;
            else if (starRating < 5.3f) difficulty = Difficulty.Insane;
            else if (starRating < 6.5f) difficulty = Difficulty.Expert;
            else                        difficulty = Difficulty.Ultra;

            if(!considerName)
                return difficulty;

            return GetDifficultyFromName() ?? difficulty;
        }

        /// <summary> A list of aliases for difficulty levels. Can't be ambigious with named top diffs, so something
        /// like "Lunatic", "Another", or "Special" which could be either Insane or top diff is no good.
        /// See https://osu.ppy.sh/help/wiki/Ranking_Criteria/Difficulty_Naming for reference. </summary>
        private readonly Dictionary<Mode, Dictionary<Difficulty, IEnumerable<string>>> nameDiffPairs =
            new Dictionary<Mode, Dictionary<Difficulty, IEnumerable<string>>>()
        {
            {
                Mode.Standard,
                new Dictionary<Difficulty, IEnumerable<string>>(){
                    //                                       osu!                         Common Variations
                    { Difficulty.Easy,   new List<string>(){ "Beginner", "Easy",          "Novice"                 } },
                    { Difficulty.Normal, new List<string>(){ "Basic", "Normal",           "Medium", "Intermediate" } },
                    { Difficulty.Hard,   new List<string>(){ "Advanced", "Hard"                                    } },
                    { Difficulty.Insane, new List<string>(){ "Hyper", "Insane"                                     } },
                    { Difficulty.Expert, new List<string>(){ "Expert", "Extra", "Extreme"                          } }
                }
            },
            {
                Mode.Taiko,
                new Dictionary<Difficulty, IEnumerable<string>>(){
                    //                                       osu!taiko/Taiko no Tatsujin
                    { Difficulty.Easy,   new List<string>(){ "Kantan"                    } },
                    { Difficulty.Normal, new List<string>(){ "Futsuu"                    } },
                    { Difficulty.Hard,   new List<string>(){ "Muzukashii"                } },
                    { Difficulty.Insane, new List<string>(){ "Oni"                       } },
                    { Difficulty.Expert, new List<string>(){ "Inner Oni", "Ura Oni"      } },
                    { Difficulty.Ultra,  new List<string>(){ "Hell Oni"                  } }
                }
            },
            {
                Mode.Catch,
                new Dictionary<Difficulty, IEnumerable<string>>(){
                    //                                       osu!catch
                    { Difficulty.Easy,   new List<string>(){ "Cup"                } },
                    { Difficulty.Normal, new List<string>(){ "Salad"              } },
                    { Difficulty.Hard,   new List<string>(){ "Platter"            } },
                    { Difficulty.Insane, new List<string>(){ "Rain"               } },
                    { Difficulty.Expert, new List<string>(){ "Overdose", "Deluge" } }
                }
            },
            {
                Mode.Mania,
                new Dictionary<Difficulty, IEnumerable<string>>(){
                    //                                       osu!mania/DJMAX (+EZ2DJ/AC)  Beatmania IIDX    SVDX
                    { Difficulty.Easy,   new List<string>(){ "EZ",                        "Beginner",       "Basic"                           } },
                    { Difficulty.Normal, new List<string>(){ "NM",                        "Normal",         "Novice"                          } },
                    { Difficulty.Hard,   new List<string>(){ "HD",                        "Hyper",          "Advanced"                        } },
                    { Difficulty.Insane, new List<string>(){ "MX",           "SHD",       "Another",        "Exhaust"                         } },
                    { Difficulty.Expert, new List<string>(){ "SC",           "EX",        "Black Another",  "Infinite", "Gravity", "Heavenly" } }
                }
            }
        };

        public Difficulty? GetDifficultyFromName()
        {
            string name = metadataSettings.version;

            // Reverse order allows e.g. "Inner Oni"/"Black Another" to be looked for separately from just "Oni"/"Another".
            var pairs = nameDiffPairs[generalSettings.mode].Reverse();
            foreach (var pair in pairs)
                // Allows difficulty names such as "Normal...!??" and ">{(__HARD;)}" to be detected,
                // but still prevents "Normality" or similar inclusions.
                if (pair.Value.Any(value => new Regex(@$"(?i)(^| )[!-@\[-`{{-~]*{value}[!-@\[-`{{-~]*( |$)").IsMatch(name)))
                    return pair.Key;

            return null;
        }

        /// <summary> Returns the name of the difficulty in a gramatically correct way, for example "an Easy" and "a Normal".
        /// Mostly useful for adding in the middle of sentences.</summary>
        public string GetDifficultyName(Difficulty? difficulty = null)
        {
            switch (difficulty ?? GetDifficulty())
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
        public double GetOffsetIntoBeat(double time)
        {
            UninheritedLine line = GetTimingLine<UninheritedLine>(time);

            // gets how many miliseconds into a beat we are
            double msOffset    = time - line.offset;
            double division    = msOffset / line.msPerBeat;
            double fraction    = division - (float)Math.Floor(division);
            double beatOffset  = fraction * line.msPerBeat;

            return beatOffset;
        }

        private readonly int[] divisors = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 12, 16 };
        /// <summary> Returns the lowest possible beat snap divisor to get to the given time with less than 2 ms of unsnap, 0 if unsnapped. </summary>
        public int GetLowestDivisor(double time)
        {
            UninheritedLine line = GetTimingLine<UninheritedLine>(time);
            foreach (int divisor in divisors)
            {
                double unsnap = Math.Abs(GetPracticalUnsnap(time, divisor, line));
                if (unsnap < 2)
                    return divisor;
            }
            
            return 0;
        }

        /// <summary> Returns the unsnap ignoring all of the game's rounding and other approximations. Can be negative. </summary>
        public double GetTheoreticalUnsnap(double time)
        {
            UninheritedLine line = GetTimingLine<UninheritedLine>(time);
            double[] theoreticalUnsnaps = {
                GetTheoreticalUnsnap(time, 16, line),
                GetTheoreticalUnsnap(time, 12, line),
                GetTheoreticalUnsnap(time, 9, line),
                GetTheoreticalUnsnap(time, 7, line),
                GetTheoreticalUnsnap(time, 5, line),
            };

            // Assume the closest possible snapping & retain signed values.
            double minUnsnap = theoreticalUnsnaps.Min(unsnap => Math.Abs(unsnap));
            return theoreticalUnsnaps.First(unsnap => Math.Abs(unsnap) == minUnsnap);
        }

        /// <summary> Returns the unsnap, from the given snap divisor, ignoring all of the game's rounding and other approximations. 
        /// Optionally supply the uninherited line, instead of the method looking this up itself. The value returned is in terms of
        /// how much the object needs to be moved forwards in time to be snapped. </summary>
        public double GetTheoreticalUnsnap(double time, int divisor, UninheritedLine line = null)
        {
            if (line == null)
                line = GetTimingLine<UninheritedLine>(time);

            double beatOffset      = GetOffsetIntoBeat(time);
            double currentFraction = beatOffset / line.msPerBeat;

            double desiredFraction    = Math.Round(currentFraction * divisor) / divisor;
            double differenceFraction = currentFraction - desiredFraction;
            double theoreticalUnsnap  = differenceFraction * line.msPerBeat;

            return theoreticalUnsnap;
        }

        /// <summary> Returns the unsnap accounting for the way the game rounds (or more accurately doesn't round) snapping. <para/>
        /// The value returned is in terms of how much the object needs to be moved forwards in time to be snapped. </summary>
        public double GetPracticalUnsnap(double time) =>
            GetPracticalUnsnapFromTheoretical(time, GetTheoreticalUnsnap(time));

        /// <summary> Same as <see cref="GetTheoreticalUnsnap(double, int, UninheritedLine)"/>, except accounts for the way
        /// the game rounds ms times, like <see cref="GetPracticalUnsnap(double)"/> does. </summary>
        public double GetPracticalUnsnap(double time, int divisor, UninheritedLine line = null) =>
            GetPracticalUnsnapFromTheoretical(time, GetTheoreticalUnsnap(time, divisor, line));

        /// <summary> Returns the practical unsnap for the given time and theoretical unsnap, by accounting for how the
        /// game rounds (or more accurately casts to int) ms values. </summary>
        private double GetPracticalUnsnapFromTheoretical(double time, double theoreticalUnsnap) =>
            Timestamp.Round(time - theoreticalUnsnap) - time;

        /// <summary> Returns the combo number (the number you see on the notes), of a given hit object.
        public int GetCombo(HitObject hitObject)
        {
            int combo = 1;

            // Adds a combo number for each object before this that isn't a new combo.
            HitObject firstHitObject = hitObjects[0];
            while (hitObject != null)
            {
                HitObject prevHitObject = hitObject.Prev();

                // The first object in the beatmap is always a new combo.
                // Spinners and their following objects are also always new comboed.
                if (hitObject.type.HasFlag(HitObject.Type.NewCombo) || hitObject is Spinner || prevHitObject is Spinner || hitObject == firstHitObject)
                    break;

                hitObject = prevHitObject;

                ++combo;
            }

            return combo;
        }

        /// <summary> Returns the hit object count divided by the drain time. </summary>
        public double GetObjectDensity() =>
            hitObjects.Count / GetDrainTime();

        /// <summary> Returns the full audio file path the beatmap uses if any such file exists, otherwise null. </summary>
        public string GetAudioFilePath()
        {
            if (songPath != null)
            {
                // read the mp3 file tags, if an audio file is specified
                string audioFileName = generalSettings.audioFileName;
                string mp3Path = songPath + Path.DirectorySeparatorChar + audioFileName;

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
        
        private List<T> GetEvents<T>(string[] lines, List<string> types, Func<string[], T> func)
        {
            // find all lines starting with any of aTypes in the event section
            List<T> foundTypes = new List<T>();
            ParserStatic.ApplySettings(lines, "Events", sectionLines =>
            {
                foreach (string line in sectionLines)
                    if (types.Any(type => line.StartsWith(type + ",")))
                        foundTypes.Add(func(line.Split(',')));
            });
            return foundTypes;
        }

        private List<TimingLine> GetTimingLines(string[] lines)
        {
            // find the [TimingPoints] section and parse each timing line
            List<TimingLine> timingLines = ParserStatic.ParseSection(lines, "TimingPoints", line =>
            {
                string[] args = line.Split(',');
                return TimingLine.IsUninherited(args) ? new UninheritedLine(args, this) : (TimingLine)new InheritedLine(args, this);
            }).OrderBy(line => line.offset).ThenBy(line => line is InheritedLine).ToList();

            // Initialize internal indicies for O(1) next/prev access.
            for (int i = 0; i < timingLines.Count; ++i)
                timingLines[i].SetTimingLineIndex(i);

            return timingLines;
        }

        private List<HitObject> GetHitobjects(string[] lines)
        {
            // find the [Hitobjects] section and parse each hitobject until empty line or end of file
            List<HitObject> hitObjects = ParserStatic.ParseSection(lines, "HitObjects", line =>
            {
                string[] args = line.Split(',');
                return
                    HitObject.HasType(args, HitObject.Type.Circle) ? new Circle(args, this) :
                    HitObject.HasType(args, HitObject.Type.Slider) ? new Slider(args, this) :
                    HitObject.HasType(args, HitObject.Type.ManiaHoldNote) ? new HoldNote(args, this) :
                    (HitObject)new Spinner(args, this);
            }).OrderBy(hitObject => hitObject.time).ToList();

            // Initialize internal indicies for O(1) next/prev access.
            for (int i = 0; i < hitObjects.Count; ++i)
                hitObjects[i].SetHitObjectIndex(i);

            return hitObjects;
        }

        /// <summary> Returns the beatmap as a string in the format "[Insane]", if the difficulty is called "Insane", for example. </summary>
        public override string ToString() =>
            "[" + metadataSettings.version + "]";
    }
}
