using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib
{
    public class KustoTimeSpan
    {
        public KustoTimeSpan(TimeSpan? duration)
        {
        }

        public TimeSpan? Duration { get; }

        public override string ToString()
        {
            if (Duration == null)
            {
                return "time(null)";
            }
            else
            {
                TimeSpan duration = Duration.Value;

                if (IsSimpleDecimal(duration.TotalDays))
                {
                    return MakeLiteral(duration.TotalDays, "d");
                }
                else if (IsSimpleDecimal(duration.Hours))
                {
                    return MakeLiteral(duration.Hours, "h");
                }
                else if (IsSimpleDecimal(duration.Minutes))
                {
                    return MakeLiteral(duration.Minutes, "m");
                }
                else if (IsSimpleDecimal(duration.Seconds))
                {
                    return MakeLiteral(duration.Seconds, "s");
                }
                else if (IsSimpleDecimal(duration.Milliseconds))
                {
                    return MakeLiteral(duration.Milliseconds, "ms");
                }
                else
                {
                    var time = duration.ToString("G");

                    return $"time({time})";
                }
            }
        }

        private static string MakeLiteral(double number, string suffix)
        {
            if (number == (int)number)
            {
                return number + suffix;
            }
            else
            {
                return $"timespan({number}{suffix})";
            }
        }

        private static bool IsSimpleDecimal(double number)
        {
            return number * 10 == (int)(number * 10);
        }
    }
}