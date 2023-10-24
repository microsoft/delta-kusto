using System;
using System.Collections.Immutable;
using System.Text;
using System.Text.Json.Serialization;

namespace DeltaKustoLib.CommandModel.Policies.Update
{
    public record UpdatePolicy
    {
        public bool IsEnabled { get; set; } = false;

        public string? Source { get; set; } = null;

        public string? Query { get; set; } = null;

        public bool IsTransactional { get; set; } = false;

        public bool PropagateIngestionProperties { get; set; } = false;
    }

    [JsonSerializable(typeof(UpdatePolicy))]
    [JsonSerializable(typeof(IImmutableList<UpdatePolicy>))]
    internal partial class UpdatePolicySerializerContext : JsonSerializerContext
    {
    }
}