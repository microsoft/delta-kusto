using System.Collections.Generic;

namespace DeltaKustoLib.SchemaObjects
{
    public class DatabaseSchema
    {
        public string Name { get; set; } = string.Empty;

        public IDictionary<string, FunctionSchema> Functions { get; set; } = new Dictionary<string, FunctionSchema>();
    }
}