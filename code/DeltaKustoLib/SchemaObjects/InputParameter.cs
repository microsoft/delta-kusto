namespace DeltaKustoLib.SchemaObjects
{
    public class InputParameter
    {
        public string Name { get; set; } = string.Empty;

        public string? CslType { get; set; } = null;
        
        public ColumnParameter[] Columns { get; set; } = new ColumnParameter[0];
    }
}