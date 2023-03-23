using System;
using System.Threading.Tasks;

namespace DeltaKustoAdxIntegrationTest
{
    public class DbNameHolder : IDisposable
    {
        private readonly Action _onDispose;

        public DbNameHolder(string name, Action onDispose)
        {
            Name = name;
            _onDispose = onDispose;
        }

        public string Name { get; }

        void IDisposable.Dispose()
        {
            _onDispose();
        }
    }
}