using MapsetParser.statics;

namespace MapsetParser.objects.events
{
    public class Video
    {
        // Video,-320,"aragoto.avi"
        // Video, offset, filename

        public readonly int    offset;
        public readonly string path;

        /// <summary> The path in lowercase without extension or quotationmarks. </summary>
        public readonly string strippedPath;

        public Video(string aCode)
        {
            offset = GetOffset(aCode);
            path   = GetPath(aCode);

            strippedPath = PathStatic.ParsePath(path, true);
        }

        // offset
        private int GetOffset(string aCode)
        {
            return int.Parse(aCode.Split(',')[1]);
        }

        // filename
        private string GetPath(string aCode)
        {
            // remove quotes for consistency, no way to add quotes manually anyway
            return PathStatic.ParsePath(aCode.Split(',')[2], false, true);
        }
    }
}
