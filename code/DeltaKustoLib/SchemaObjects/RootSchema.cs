using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace DeltaKustoLib.SchemaObjects
{
    public class RootSchema
    {
        public IDictionary<string, DatabaseSchema> Databases { get; set; } = new Dictionary<string, DatabaseSchema>();

        public RootSchema FromJson(string json)
        {
            var schema = JsonSerializer.Deserialize<RootSchema>(json);

            return schema;
        }
    }
}