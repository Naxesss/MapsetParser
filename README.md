# MapsetParser
MapsetParser is a parser library for beatmapsets from the rhythm game osu! made in C# .NET Core. This parser not only makes the data in .osu files more easily accessible, but also keeps track of the entire song folder of a given mapset, including the path to files inside sub-folders.

Figuring out seemingly basic properties like which files are used or what the slider velocity the current object is in, to more advanced ones like how many frames each storyboard animation has, what hitsound the 5th repeat of a slider is using, or where halfway through a catmull slider is on the playfield, are all possible with this parser!

# Examples
## Finding sliders with multiple repeats
```csharp
BeatmapSet beatmapSet = new BeatmapSet(@"C:\...\osu\Songs\580215 Rita - dorchadas");

Beatmap beatmap = beatmapSet.beatmaps.First(aBeatmap =>
    aBeatmap.metadataSettings.version == "Mirash's Insane");

foreach (Slider slider in beatmap.hitObjects.OfType<Slider>())
    if (slider.edgeAmount > 2)
        Console.WriteLine($"Slider at {slider.time} ms has {(slider.edgeAmount - 1)} repeats.");
```
```
Slider at 107808 ms has 2 repeats.
Slider at 108308 ms has 2 repeats.
```

## Seeing which files are used
```csharp
BeatmapSet beatmapSet = new BeatmapSet(@"C:\...\osu\Songs\580215 Rita - dorchadas");

// As an example, let's add an unused hit sound into the files.
List<string> filePaths = new List<string>(beatmapSet.songFilePaths)
{
    "soft-hitwhistle99.wav"
};

foreach (string filePath in filePaths)
{
    string relativePath = PathStatic.RelativePath(filePath, beatmapSet.songPath);

    if (beatmapSet.IsFileUsed(relativePath))
        Console.WriteLine($"File \"{relativePath}\" is used.");
    else
        Console.WriteLine($"File \"{relativePath}\" is not used.");
}
```
```
File "BG.jpg" is used.
File "Dorchadas.mp3" is used.
File "drum-hitclap52.wav" is used.
File "drum-hitfinish52.wav" is used.
File "drum-hitnormal51.wav" is used.
File "drum-sliderslide23.wav" is used.
File "normal-hitclap.wav" is used.
File "Rita - dorchadas (Delis) [alacat's Hard].osu" is used.
File "Rita - dorchadas (Delis) [Insane].osu" is used.
File "Rita - dorchadas (Delis) [Intermediate].osu" is used.
File "Rita - dorchadas (Delis) [Milkshake's Normal].osu" is used.
File "Rita - dorchadas (Delis) [Mirash's Insane].osu" is used.
File "Rita - dorchadas (Delis) [Sharnoth].osu" is used.
File "Rita - dorchadas (Delis) [Trynna's Easy].osu" is used.
File "soft-sliderslide51.wav" is used.
File "soft-hitwhistle99.wav" is not used.
```

## Detecting unsnapped objects
```csharp
BeatmapSet beatmapSet = new BeatmapSet(@"C:\...\osu\Songs\580215 Rita - dorchadas");

foreach (Beatmap beatmap in beatmapSet.beatmaps)
{
    foreach (HitObject hitObject in beatmap.hitObjects)
    {
        foreach (double edgeTime in hitObject.GetEdgeTimes())
        {
            string partName = hitObject.GetPartName(edgeTime);

            // Object needs to be moved forwards by this much to be snapped.
            double unsnap = beatmap.GetPracticalUnsnap(edgeTime);
            if (Math.Abs(unsnap) >= 1)
                Console.WriteLine($"{partName} at {edgeTime} ms is unsnapped by {unsnap} ms.");
        }
    }
}
```
```
Circle at 137932 ms is unsnapped by 1 ms.
Circle at 140646 ms is unsnapped by -1 ms.
Slider head at 164263 ms is unsnapped by -1 ms.
```
