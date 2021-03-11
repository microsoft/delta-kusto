using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace DeltaKustoLib.SchemaObjects
{
    public class RootSchema
    {
        public IDictionary<string, DatabaseSchema> Databases { get; set; } = new Dictionary<string, DatabaseSchema>();

        public static RootSchema FromJson(string json)
        {
            var schema = JsonSerializer.Deserialize<RootSchema>(json);

            if (schema == null)
            {
                throw new DeltaException("JSON payload doesn't look like a JSON object");
            }

            return schema;
        }
    }
}