using System;

namespace DeltaKustoLib.CommandModel.Policies
{
    internal record IngestionBatchingPolicy
    {
        public static IngestionBatchingPolicy Create(
            TimeSpan maximumBatchingTimeSpan,
            int maximumNumberOfItems,
            int maximumRawDataSizeMb)
        {
            return new IngestionBatchingPolicy
            {
                MaximumBatchingTimeSpan = maximumBatchingTimeSpan.ToString(),
                MaximumNumberOfItems = maximumNumberOfItems,
                MaximumRawDataSizeMb = maximumRawDataSizeMb
            };
        }

        public string MaximumBatchingTimeSpan { get; init; } = TimeSpan.FromMinutes(5).ToString();

        public int MaximumNumberOfItems { get; init; } = 1000;
        
        public int MaximumRawDataSizeMb { get; init; } = 1024;

        public TimeSpan GetMaximumBatchingTimeSpan()
        {
            TimeSpan time;

            if (TimeSpan.TryParse(MaximumBatchingTimeSpan, out time))
            {
                return time;
            }
            else
            {
                throw new DeltaException(
                    $"Can't parse 'MaximumBatchingTimeSpan' value '{MaximumBatchingTimeSpan}'");
            }
        }
    }
}