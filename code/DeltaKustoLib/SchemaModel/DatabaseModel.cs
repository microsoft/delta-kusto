using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace DeltaKustoLib.SchemaModel
{
    public class DatabaseModel
    {
        public string Name { get; }

        public string Folder { get; }

        public string DocString { get; }

        public IImmutableList<TableModel> Tables { get; }

        private DatabaseModel(
            string name,
            string folder,
            string docString,
            IEnumerable<TableModel> tableModels)
        {
            Name = name;
            Folder = folder;
            DocString = docString;
            Tables = tableModels.ToImmutableArray();
        }

        public static DatabaseModel FromScript(string databaseName, string script)
        {
            throw new NotImplementedException();
        }

        public static DatabaseModel MergeModels(IEnumerable<DatabaseModel> databaseModels)
        {
            throw new NotImplementedException();
        }
    }
}