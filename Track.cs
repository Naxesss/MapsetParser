using MapsetParser.statics;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapsetParser
{
    internal class Track
    {
        private readonly string message;

        public Track(string message)
        {
            this.message = message;
            EventStatic.OnLoadStart(this.message);
        }

        public void Complete()
        {
            EventStatic.OnLoadComplete(message);
        }
    }
}
