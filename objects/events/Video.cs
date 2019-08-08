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

        public Video(string[] anArgs)
        {
            offset = GetOffset(anArgs);
            path   = GetPath(anArgs);

            strippedPath = PathStatic.ParsePath(path, true);
        }

        // offset
        private int GetOffset(string[] anArgs)
        {
            return int.Parse(anArgs[1]);
        }

        // filename
        private string GetPath(string[] anArgs)
        {
            // remove quotes for consistency, no way to add quotes manually anyway
            return PathStatic.ParsePath(anArgs[2], false, true);
        }
    }
}
