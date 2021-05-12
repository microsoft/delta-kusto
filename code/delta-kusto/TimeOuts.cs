using System;

namespace delta_kusto
{
    internal class TimeOuts
    {
        public static TimeSpan API = TimeSpan.FromSeconds(10);
        
        public static TimeSpan RETRIEVE_DB = TimeSpan.FromSeconds(15);
        
        public static TimeSpan ACTION = TimeSpan.FromSeconds(40);
    }
}