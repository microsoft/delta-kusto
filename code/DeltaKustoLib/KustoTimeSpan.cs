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
            Duration = duration;
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
                else if (duration.TotalHours <= 120 && IsSimpleDecimal(duration.TotalHours))
                {
                    return MakeLiteral(duration.TotalHours, "h");
                }
                else if (duration.TotalMinutes <= 120 && IsSimpleDecimal(duration.TotalMinutes))
                {
                    return MakeLiteral(duration.TotalMinutes, "m");
                }
                else if (duration.TotalSeconds <= 120 && IsSimpleDecimal(duration.TotalSeconds))
                {
                    return MakeLiteral(duration.TotalSeconds, "s");
                }
                else if (duration.TotalMilliseconds <= 1000 && IsSimpleDecimal(duration.TotalMilliseconds))
                {
                    return MakeLiteral(duration.TotalMilliseconds, "ms");
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
            return number != 0 && number * 10 == (int)(number * 10);
        }
    }
}