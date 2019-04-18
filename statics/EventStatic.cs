using System;
using System.Collections.Generic;
using System.Text;

namespace MapsetParser.statics
{
    public class EventStatic
    {
        /// <summary> Called whenever loading of something is started. </summary>
        public static Action<string> onLoadStart = (aMessage) => { };

        /// <summary> Called whenever loading of something is completed. </summary>
        public static Action<string> onLoadComplete = (aMessage) => { };
    }
}
