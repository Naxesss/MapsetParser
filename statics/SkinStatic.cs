using MapsetParser.objects;
using MapsetParser.objects.hitobjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MapsetParser.statics
{
    internal static class SkinStatic
    {
        private static bool isInitialized = false;
        
        private static string[] skinGeneral = new string[]
        {
            // cursor
            "cursor.png",
            "cursormiddle.png",
            "cursor-smoke.png",
            "cursortrail.png",
            // playfield
            "play-skip.png",
            "play-unranked.png",
            "multi-skipped.png",
            // pause screen
            "pause-overlay.png",    "pause-overlay.jpg",    // according to the wiki page these are the only two which have jpg alternatives
            "fail-background.png",  "fail-background.jpg",
            "pause-back.png",
            "pause-continue.png",
            "pause-replay.png",
            "pause-retry.png",
            // scorebar
            "scorebar-bg.png",
            "scorebar-colour.png",
            // score numbers
            "score-0.png",
            "score-1.png",
            "score-2.png",
            "score-3.png",
            "score-4.png",
            "score-5.png",
            "score-6.png",
            "score-7.png",
            "score-8.png",
            "score-9.png",
            "score-comma.png",
            "score-dot.png",
            "score-percent.png",
            "score-x.png",
            // ranking grades
            "ranking-XH-small.png",
            "ranking-X-small.png",
            "ranking-SH-small.png",
            "ranking-S-small.png",
            "ranking-A-small.png",
            "ranking-B-small.png",
            "ranking-C-small.png",
            "ranking-D-small.png",
            // score entry (used in the leaderboard while in gameplay)
            "scoreentry-0.png",
            "scoreentry-1.png",
            "scoreentry-2.png",
            "scoreentry-3.png",
            "scoreentry-4.png",
            "scoreentry-5.png",
            "scoreentry-6.png",
            "scoreentry-7.png",
            "scoreentry-8.png",
            "scoreentry-9.png",
            "scoreentry-comma.png",
            "scoreentry-dot.png",
            "scoreentry-percent.png",
            "scoreentry-x.png",
            // song selection (used in the in-game leaderboard, among other stuff like kiai)
            "menu-button-background.png",
            "selection-tab.png",
            "star2.png",
            // mod icons (appears in the top right of the screen in gameplay)
            "selection-mod-autoplay.png",
            "selection-mod-cinema.png",
            "selection-mod-doubletime.png",
            "selection-mod-easy.png",
            "selection-mod-flashlight.png",
            "selection-mod-halftime.png",
            "selection-mod-hardrock.png",
            "selection-mod-hidden.png",
            "selection-mod-nightcore.png",
            "selection-mod-nofail.png",
            "selection-mod-perfect.png",
            "selection-mod-suddendeath.png",
            // sounds in gameplay
            "applause.wav", "applause.mp3",
            "comboburst.wav", "comboburst.mp3",
            "combobreak.wav", "combobreak.mp3",
            "failsound.wav", "failsound.mp3",
            // sounds in the pause screen
            "pause-loop.wav", "pause-loop.mp3"
        };

        private static string[] skinStandard = new string[]
        {
            // hit bursts
            "hit0.png",
            "hit50.png",
            "hit100.png",
            "hit100k.png",
            "hit300.png",
            "hit300g.png",
            "hit300k.png",
            // hit burst animations
            "hit0-{n}.png",
            "hit50-{n}.png",
            "hit100-{n}.png",
            "hit100k-{n}.png",
            "hit300-{n}.png",
            "hit300g-{n}.png",
            "hit300k-{n}.png",
            // mod icons exceptions
            "selection-mod-relax2.png",
            "selection-mod-spunout.png",
            "selection-mod-target.png", // currently only cutting edge
            // combo burst
            "comboburst.png",
            // default numbers, used for combos
            "default-0.png",
            "default-1.png",
            "default-2.png",
            "default-3.png",
            "default-4.png",
            "default-5.png",
            "default-6.png",
            "default-7.png",
            "default-8.png",
            "default-9.png",
            // hit circles
            "approachcircle.png",
            "hitcircle.png",
            "hitcircleoverlay.png",
            "hitcircleoverlay-{n}.png",
            "hitcircleselect.png",
            "followpoint.png",
            "followpoint-{n}.png",
            "lighting.png"
        };

        private static string[] skinMania = new string[]
        {
            // mod icons
            "selection-mod-fadein.png",
            "selection-mod-key1.png",
            "selection-mod-key2.png",
            "selection-mod-key3.png",
            "selection-mod-key4.png",
            "selection-mod-key5.png",
            "selection-mod-key6.png",
            "selection-mod-key7.png",
            "selection-mod-key8.png",
            "selection-mod-key9.png",
            "selection-mod-keycoop.png",
            "selection-mod-random.png"
        };

        private static string[] skinCatch = new string[]
        {
            // hit burst exception, appears in both modes' result screens
            "hit0.png",
            // input overlay
            "inputoverlay-background.png",
            "inputoverlay-key.png"
        };

        private static string[] skinNotMania = new string[]
        {
            // scorebar exception, bar is in a different position and excludes this element because of that
            "scorebar-marker.png",
            // mod icons exception, in mania there's no difference between something clicking for you and just using auto
            "selection-mod-relax.png"
        };

        private static string[] skinCountdown = new string[]
        {
            // playfield
            "count1.png",
            "count2.png",
            "count3.png",
            "go.png",
            "ready.png",
            // sounds
            "count1s.wav", "count1s.mp3",
            "count2s.wav", "count2s.mp3",
            "count3s.wav", "count3s.mp3",
            "gos.wav", "gos.mp3",
            "readys.wav", "readys.mp3"
        };

        private static string[] skinStandardSlider = new string[]
        {
            // slider
            "sliderstartcircle.png",
            "sliderstartcircleoverlay.png",
            "sliderstartcircleoverlay-{n}.png",
            "sliderendcircle.png",
            "sliderendcircleoverlay.png",
            "sliderendcircleoverlay-{n}.png",
            "sliderfollowcircle.png",
            "sliderfollowcircle-{n}.png",
            "sliderb.png",
            "sliderb{n}.png",
            "sliderb-nd.png",
            "sliderb-spec.png",
            "sliderscorepoint.png",
            "sliderpoint10.png",
            "sliderpoint30.png"
        };

        private static string[] skinStandardSpinner = new string[]
        {
            // spinner
            "spinner-approachcircle.png",
            "spinner-rpm.png",
            "spinner-clear.png",
            "spinner-spin.png",
            "spinner-glow.png",
            "spinner-bottom.png",
            "spinner-top.png",
            "spinner-middle2.png",
            "spinner-middle.png",
            // "old spinner" but apparently it's used still without needing to be in skin v1
            "spinner-background.png",
            "spinner-circle.png",
            "spinner-metre.png",
            "spinner-osu.png",
            // sounds
            "spinnerspin.wav", "spinnerspin.mp3",
            "spinnerbonus.wav", "spinnerbonus.mp3"
        };

        private static string[] skinNotScorebarMarker = new string[]
        {
            // scorebar marker has higher priority, so if it exists in the folder it will be used instead of these
            "scorebar-ki.png",
            "scorebar-kidanger.png",
            "scorebar-kidanger2.png"
        };

        private static string[] skinNotSliderb = new string[]
        {
            "sliderb-nd.png",
            "sliderb-spec.png"
        };

        private static string[] skinBreak = new string[]
        {
            "section-fail.png",
            "section-pass.png",
            // sounds
            "sectionpass.wav", "sectionpass.mp3",
            "sectionfail.wav", "sectionfail.mp3"
        };

        // here we do skin elements that aren't necessarily used but can be, given a specific condition
        private static List<Tuple<string[], Func<BeatmapSet, bool>>> conditionalTuples = new List<Tuple<string[], Func<BeatmapSet, bool>>>();

        private static void AddElements(string[] anElementList, Func<BeatmapSet, bool> aUseCondition = null) =>
            conditionalTuples.Add(new Tuple<string[], Func<BeatmapSet, bool>>(anElementList, aUseCondition));

        private static void AddElement(string anElement, Func<BeatmapSet, bool> aUseCondition = null) =>
            conditionalTuples.Add(new Tuple<string[], Func<BeatmapSet, bool>>(new string[] { anElement }, aUseCondition));

        private static void Initialize()
        {
            // modes, doing or-gates on standard for everything because conversions
            AddElements(skinGeneral);
            AddElements(skinStandard, aBeatmapSet => aBeatmapSet.beatmaps.Any(
                aBeatmap => aBeatmap.generalSettings.mode == Beatmap.Mode.Standard));
            AddElements(skinCatch, aBeatmapSet => aBeatmapSet.beatmaps.Any(
                aBeatmap => aBeatmap.generalSettings.mode == Beatmap.Mode.Catch
                         || aBeatmap.generalSettings.mode == Beatmap.Mode.Standard));
            AddElements(skinMania, aBeatmapSet => aBeatmapSet.beatmaps.Any(
                aBeatmap => aBeatmap.generalSettings.mode == Beatmap.Mode.Mania
                         || aBeatmap.generalSettings.mode == Beatmap.Mode.Standard));
            AddElements(skinNotMania, aBeatmapSet => aBeatmapSet.beatmaps.Any(
                aBeatmap => aBeatmap.generalSettings.mode != Beatmap.Mode.Mania));

            // TODO: Taiko skin conversion, see issue #6
            /*AddElements(mSkinTaiko, aBeatmapSet => aBeatmapSet.mBeatmaps.Any(
                aBeatmap => aBeatmap.mGeneralSettings.mMode == Beatmap.Mode.Taiko
                         || aBeatmap.mGeneralSettings.mMode == Beatmap.Mode.Standard));*/

            // only used in specific cases
            AddElements(skinCountdown, aBeatmapSet => aBeatmapSet.beatmaps.Any(
                aBeatmap => aBeatmap.generalSettings.countdown > 0));
            AddElements(skinStandardSlider, aBeatmapSet => aBeatmapSet.beatmaps.Any(
                aBeatmap => aBeatmap.hitObjects.Any(anObject => anObject is Slider)));
            AddElement("reversearrow.png", aBeatmapSet => aBeatmapSet.beatmaps.Any(
                aBeatmap => aBeatmap.hitObjects.Any(anObject => (anObject as Slider)?.edgeAmount > 1)));
            AddElements(skinStandardSpinner, aBeatmapSet => aBeatmapSet.beatmaps.Any(
                aBeatmap => aBeatmap.hitObjects.Any(anObject => anObject is Spinner)));
            AddElements(skinBreak, aBeatmapSet => aBeatmapSet.beatmaps.Any(
                aBeatmap => aBeatmap.breaks.Any()));

            // depending on other skin elements
            AddElements(skinNotScorebarMarker, aBeatmapSet => aBeatmapSet.songFilePaths.Any(
                aPath => aPath.EndsWith("scorebar-marker.png")));
            AddElements(skinNotSliderb, aBeatmapSet => aBeatmapSet.songFilePaths.Any(
                aPath => aPath.EndsWith("sliderb.png")));
            AddElement("particle50.png", aBeatmapSet => aBeatmapSet.songFilePaths.Any(
                aPath => aPath.EndsWith("hit50.png")));
            AddElement("particle100.png", aBeatmapSet => aBeatmapSet.songFilePaths.Any(
                aPath => aPath.EndsWith("hit100.png")));
            AddElement("particle300.png", aBeatmapSet => aBeatmapSet.songFilePaths.Any(
                aPath => aPath.EndsWith("hit300.png")));

            isInitialized = true;
        }

        /// <summary> Returns whether the given skin name is used in the given beatmapset (including animations). </summary>
        public static bool IsUsed(string anElementName, BeatmapSet aBeatmapSet)
        {
            if (!isInitialized)
                Initialize();

            // find the tuple that contains the element name
            Tuple<string[], Func<BeatmapSet, bool>> skinCondition = null;
            foreach (Tuple<string[], Func<BeatmapSet, bool>> conditionalTuple in conditionalTuples.ToList())
            {
                foreach (string elementName in conditionalTuple.Item1)
                {
                    if (elementName.ToLower() == anElementName.ToLower())
                        skinCondition = conditionalTuple;

                    // animation frames, i.e. "followpoint-{n}.png"
                    if (elementName.Contains("{n}"))
                    {
                        int startIndex = elementName.IndexOf("{n}");
                        if (   startIndex != -1
                            && anElementName.Length > startIndex
                            && anElementName.IndexOf('.', startIndex) != -1)
                        {
                            int endIndex = anElementName.IndexOf('.', startIndex);
                            string frame = anElementName.Substring(startIndex, endIndex - startIndex);

                            if (elementName.Replace("{n}", frame).ToLower() == anElementName)
                                skinCondition = conditionalTuple;
                        }
                    }
                }
            }

            // if it's use condition matches, it's used
            if(skinCondition != null && (skinCondition.Item2 == null || skinCondition.Item2(aBeatmapSet)))
                return true;
            
            return false;
        }
    }
}
