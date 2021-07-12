using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

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

        public string SerializePolicy()
        {
            return JsonSerializer.Serialize(Policy, _policiesSerializerOptions);
        }

        public T DeserializePolicy<T>()
        {
            var obj = JsonSerializer.Deserialize<T>(SerializePolicy());

            if (obj == null)
            {
                throw new DeltaException("Can't deserialize a policy");
            }

            return obj;
        }

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

        protected bool PolicyEquals(PolicyCommandBase other)
        {
            return ElementEquals(Policy.RootElement, other.Policy.RootElement);
        }

        private bool ElementEquals(JsonElement element1, JsonElement element2)
        {
            if (!element1.ValueKind.Equals(element2.ValueKind))
            {
                return false;
            }
            else
            {
                switch (element1.ValueKind)
                {
                    case JsonValueKind.Array:
                        return ArrayElementEquals(element1, element2);
                    case JsonValueKind.Object:
                        return ObjectElementEquals(element1, element2);
                    case JsonValueKind.Undefined:
                    case JsonValueKind.Null:
                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        return true;
                    case JsonValueKind.Number:
                        return element1.GetDecimal().Equals(element2.GetDecimal());
                    case JsonValueKind.String:
                        return string.Equals(element1.GetString(), element2.GetString());
                    default:
                        throw new NotSupportedException($"JSON value kind not supported:  {element1.ValueKind}");
                }
            }
        }

        private bool ArrayElementEquals(JsonElement element1, JsonElement element2)
        {
            if (element1.GetArrayLength() != element2.GetArrayLength())
            {
                return false;
            }
            else
            {
                var zippedSubElements = element1.EnumerateArray().Zip(
                    element2.EnumerateArray(),
                    (s1, s2) => (s1, s2));
                var arrayEqual =
                    zippedSubElements.Aggregate(true, (b, s) => b && ElementEquals(s.s1, s.s2));

                return arrayEqual;
            }
        }

        private bool ObjectElementEquals(JsonElement element1, JsonElement element2)
        {
            var lookup2 = element2.EnumerateObject().ToLookup(p => p.Name, p => p.Value);

            if (element1.EnumerateObject().Count() != lookup2.Count)
            {
                return false;
            }
            else
            {
                foreach (var g in element1.EnumerateObject())
                {
                    if (!lookup2.Contains(g.Name)
                        || !ElementEquals(g.Value, lookup2[g.Name].First()))
                    {
                        return false;
                    }
                }

                return true;
            }
        }
    }
}