using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace DeltaKustoLib.CommandModel.Policies
{
    public abstract class PolicyCommandBase : CommandBase
    {
        private static readonly JsonSerializerOptions _policiesSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public JsonDocument Policy { get; }

        public PolicyCommandBase(JsonDocument policy)
        {
            Policy = policy;
        }

        public PolicyCommandBase() : this(ToJsonDocument(new object()))
        {
        }

        public override bool Equals(CommandBase? other)
        {
            var otherPolicy = other as PolicyCommandBase;
            var areEqualed = otherPolicy != null
                && SerializePolicy().Equals(otherPolicy.SerializePolicy());

            return areEqualed;
        }

        [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
        public string SerializePolicy()
        {
            return JsonSerializer.Serialize(Policy, _policiesSerializerOptions);
        }

        [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
        public T DeserializePolicy<T>()
        {
            var obj = JsonSerializer.Deserialize<T>(SerializePolicy());

            if (obj == null)
            {
                throw new DeltaException("Can't deserialize a policy");
            }

            return obj;
        }

        [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
        public static JsonDocument ToJsonDocument(object obj)
        {
            var text = JsonSerializer.Serialize(obj);
            var doc = JsonSerializer.Deserialize<JsonDocument>(text);

            if (doc == null)
            {
                throw new InvalidOperationException("Can't JSON format an object");
            }

            return doc;
        }

        [UnconditionalSuppressMessage("AssemblyLoadTrimming", "IL2026:RequiresUnreferencedCode")]
        protected static T Deserialize<T>(string text)
        {
            var document = JsonSerializer.Deserialize<T>(text);

            if (document == null)
            {
                throw new DeltaException($"Can't deserialize '{text}'");
            }

            return document;
        }

        protected static string Serialize<T>(T obj, JsonSerializerContext serializerContext)
        {
            var buffer = JsonSerializer.SerializeToUtf8Bytes(obj, typeof(T), serializerContext);

            return UTF8Encoding.ASCII.GetString(buffer);
        }
    }
}