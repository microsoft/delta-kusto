using System;

namespace delta_kusto
{
    internal class TimeOuts
    {
        public static TimeSpan API = TimeSpan.FromSeconds(4);
        
        public static TimeSpan FILE = TimeSpan.FromSeconds(.2);

        public static TimeSpan RETRIEVE_DB = TimeSpan.FromSeconds(2);
        
        public static TimeSpan ACTION = TimeSpan.FromSeconds(10);
    }
}