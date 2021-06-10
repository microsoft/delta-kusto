using System;
using System.Text;

namespace DeltaKustoLib.CommandModel
{
    public record UpdatePolicy
    {
        public bool IsEnabled { get; init; } = false;

        public string? Source { get; init; } = null;

        public string? Query { get; init; } = null;

        public bool IsTransactional { get; init; } = false;

        public bool PropagateIngestionProperties { get; init; } = false;
    }
}