namespace DeltaKustoLib.SchemaModel
{
    public class TableModel
    {
        public string Name { get; }

        private TableModel(string name)
        {
            Name = name;
        }
    }
}