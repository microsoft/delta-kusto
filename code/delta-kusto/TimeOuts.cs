using System;

namespace delta_kusto
{
    internal class TimeOuts
    {
        public static TimeSpan API = TimeSpan.FromSeconds(8);
        
        public static TimeSpan FILE = TimeSpan.FromSeconds(.5);

        public static TimeSpan RETRIEVE_DB = TimeSpan.FromSeconds(5);
        
        public static TimeSpan ACTION = TimeSpan.FromSeconds(20);
    }
}