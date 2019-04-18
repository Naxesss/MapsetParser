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

        public MetadataSettings(string aCode)
        {
            // unlike hitobjects metadata settings gets the whole section and not line by line as code

            title          = GetValue(aCode, "Title");
            titleUnicode   = GetValue(aCode, "TitleUnicode");
            artist         = GetValue(aCode, "Artist");
            artistUnicode  = GetValue(aCode, "ArtistUnicode");

            creator    = GetValue(aCode, "Creator");
            version    = GetValue(aCode, "Version");
            source     = GetValue(aCode, "Source");
            tags       = GetValue(aCode, "Tags");

            // check to see if the ids are even there (don't exist in lower osu file versions, and aren't set on non-published maps)
            beatmapId      = GetValue(aCode, "BeatmapID")      == null || GetValue(aCode, "BeatmapID")      == "0" 
                                ? (ulong?)null  : ulong.Parse(GetValue(aCode, "BeatmapID"));
            beatmapSetId   = GetValue(aCode, "BeatmapSetID")   == null || GetValue(aCode, "BeatmapSetID")   == "-1"
                                ? (ulong?)null  : ulong.Parse(GetValue(aCode, "BeatmapSetID"));
        }

        private string GetValue(string aCode, string aKey)
        {
            string line = aCode.Split(new string[] { "\n" }, StringSplitOptions.None).FirstOrDefault(aLine => aLine.StartsWith(aKey));
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
                .Replace(">", "")
                .ToLower();
        }
    }
}
