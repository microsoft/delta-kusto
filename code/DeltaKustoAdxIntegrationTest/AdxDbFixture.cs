using System;
using System.Threading;
using System.Threading.Tasks;

namespace DeltaKustoAdxIntegrationTest
{
    public class AdxDbFixture : IDisposable
    {
        private readonly Lazy<string> _dbPrefix;
        private volatile int _dbCount;

        public AdxDbFixture()
        {
            _dbPrefix = new Lazy<string>(
                () =>
                {
                    var dbPrefix = Environment.GetEnvironmentVariable("deltaKustoDbPrefix");

                    if (string.IsNullOrWhiteSpace(dbPrefix))
                    {
                        throw new ArgumentNullException(nameof(dbPrefix));
                    }

                    return dbPrefix;
                },
                true);
        }

        public string GetDbName()
        {
            var dbCount = Interlocked.Increment(ref _dbCount);
            var name = $"{_dbPrefix.Value}{dbCount}";

            return name;
        }

        public /*async*/ Task InitializeDbAsync(string dbName)
        {
            if (!dbName.StartsWith(_dbPrefix.Value))
            {
                throw new ArgumentException("Wrong prefix", nameof(dbName));
            }

            return Task.CompletedTask;
        }

        void IDisposable.Dispose()
        {
        }
    }
}