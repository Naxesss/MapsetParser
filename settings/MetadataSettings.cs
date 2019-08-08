using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MapsetParser.settings
{
    public class MetadataSettings
    {
        /*
            Title:Yuumeikyou o Wakatsu Koto
            TitleUnicode:幽明境を分かつこと
            Artist:Diao Ye Zong feat. Kushi
            ArtistUnicode:凋叶棕 feat. Φ串Φ
            Creator:Sakurauchi Riko
            Version:Sakura no Hana
            Source:東方妖々夢　～ Perfect Cherry Blossom.
            Tags:phyloukz 凋叶棕 さいぎょうじ ゆゆこ Saigyouji Yuyuko 幽雅に咲かせ、墨染の桜　～ Border of Life 東方妖々梦 ～ Perfect Cherry Blossom.
            BeatmapID:1541385
            BeatmapSetID:730355
         */
        // key:value

        public string title;
        public string titleUnicode;
        public string artist;
        public string artistUnicode;

        public string creator;
        public string version;
        public string source;
        public string tags;

        public ulong? beatmapId;
        public ulong? beatmapSetId;

        public MetadataSettings(string[] aLines)
        {
            // unlike hitobjects metadata settings gets the whole section and not line by line as code
            
            title          = GetValue(aLines, "Title");
            titleUnicode   = GetValue(aLines, "TitleUnicode");
            artist         = GetValue(aLines, "Artist");
            artistUnicode  = GetValue(aLines, "ArtistUnicode");

            creator    = GetValue(aLines, "Creator");
            version    = GetValue(aLines, "Version");
            source     = GetValue(aLines, "Source");
            tags       = GetValue(aLines, "Tags");

            // check to see if the ids are even there (don't exist in lower osu file versions, and aren't set on non-published maps)
            beatmapId      = GetValue(aLines, "BeatmapID")      == null || GetValue(aLines, "BeatmapID")      == "0" 
                                ? (ulong?)null  : ulong.Parse(GetValue(aLines, "BeatmapID"));
            beatmapSetId   = GetValue(aLines, "BeatmapSetID")   == null || GetValue(aLines, "BeatmapSetID")   == "-1"
                                ? (ulong?)null  : ulong.Parse(GetValue(aLines, "BeatmapSetID"));
        }

        private string GetValue(string[] aLines, string aKey)
        {
            string line = aLines.FirstOrDefault(aLine => aLine.StartsWith(aKey));
            if (line == null)
                return null;

            return line.Substring(line.IndexOf(":") + 1).Trim();
        }

        /// <summary> Returns the same string lowercase and filtered from characters disabled in file names. </summary>
        public string GetFileNameFiltered(string aString)
        {
            return aString
                .Replace("/", "")
                .Replace("\\", "")
                .Replace("?", "")
                .Replace("*", "")
                .Replace(":", "")
                .Replace("|", "")
                .Replace("\"", "")
                .Replace("<", "")
                .Replace(">", "");
        }
    }
}
