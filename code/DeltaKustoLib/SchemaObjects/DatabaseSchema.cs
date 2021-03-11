using System.Collections.Generic;
using System.Text.Json;

namespace DeltaKustoLib.SchemaObjects
{
    public class DatabaseSchema
    {
        public IDictionary<string, FunctionSchema> Functions { get; set; } = new Dictionary<string, FunctionSchema>();

        public static DatabaseSchema FromJson(string json)
        {
            var schema = JsonSerializer.Deserialize<DatabaseSchema>(json);

            if (schema == null)
            {
                throw new DeltaException(
                    $"JSON payload doesn't look like a JSON object:  '{json}'");
            }

            return schema;
        }
    }
}