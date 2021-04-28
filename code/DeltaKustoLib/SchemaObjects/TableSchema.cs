namespace DeltaKustoLib.SchemaObjects
{
    public class TableSchema
    {
        public string Name { get; set; } = string.Empty;

        public string Folder { get; set; } = string.Empty;

        public string DocString { get; set; } = string.Empty;

        public ColumnSchema[] OrderedColumns { get; set; } = new ColumnSchema[0];
    }
}