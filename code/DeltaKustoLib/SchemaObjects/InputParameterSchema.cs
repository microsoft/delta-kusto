namespace DeltaKustoLib.SchemaObjects
{
    public class InputParameterSchema
    {
        public string Name { get; set; } = string.Empty;

        public string? CslType { get; set; } = null;
        
        public ColumnParameterSchema[] Columns { get; set; } = new ColumnParameterSchema[0];
    }
}