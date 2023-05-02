using System;

namespace DeltaKustoLib.CommandModel.Policies
{
    public class HotWindow
    {
        public HotWindow(DateTime from, DateTime to)
        {
            From = from;
            To = to;
        }

        public DateTime From { get; }

        public DateTime To { get; }

        #region Object methods
        public override string ToString()
        {
            return $"datetime({ToString(From)}) .. datetime({ToString(To)})";
        }

        public override bool Equals(object? obj)
        {
            var other = obj as HotWindow;

            return other != null
                && other.From == From
                && other.To == To;
        }

        public override int GetHashCode()
        {
            return From.GetHashCode() ^ To.GetHashCode();
        }
        #endregion

        private static string ToString(DateTime date)
        {
            if (date.TimeOfDay == TimeSpan.Zero)
            {   //  Date only
                return $"{date.Year}-{date.Month}-{date.Day}";
            }
            else
            {   //  Full time
                return $"{date.Year}-{date.Month}-{date.Day} "
                    + $"{date.Hour}:{date.Minute}:{date.Second}:{date.Millisecond}";
            }
        }
    }
}