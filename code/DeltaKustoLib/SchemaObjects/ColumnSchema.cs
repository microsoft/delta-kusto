namespace DeltaKustoLib.SchemaObjects
{
    public class ColumnSchema
    {
        public string Name { get; set; } = string.Empty;

        public string CslType { get; set; } = string.Empty;

        public string DocString { get; set; } = string.Empty;
    }
}