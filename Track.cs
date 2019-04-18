using MapsetParser.statics;
using System;
using System.Collections.Generic;
using System.Text;

namespace MapsetParser
{
    internal class Track
    {
        private string message;

        public Track(string aMessage)
        {
            message = aMessage;
            EventStatic.onLoadStart(message);
        }

        public void Complete()
        {
            EventStatic.onLoadComplete(message);
        }
    }
}
