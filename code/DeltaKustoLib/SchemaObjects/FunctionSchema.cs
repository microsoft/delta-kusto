namespace DeltaKustoLib.SchemaObjects
{
    public class FunctionSchema
    {
        public string Name { get; set; } = string.Empty;
 
        public string Body { get; set; } = string.Empty;

        public string Folder { get; set; } = string.Empty;

        public string DocString { get; set; } = string.Empty;

        public InputParameter[] InputParameter { get; set; } = new InputParameter[0];
    }
}