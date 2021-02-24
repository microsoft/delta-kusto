using DeltaKustoLib.CommandModel;
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
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }
            Name = name;
            Folder = folder;
            DocString = docString;
            Tables = tableModels.ToImmutableArray();
        }

        public static DatabaseModel FromCommands(IEnumerable<CommandBase> commands)
        {
            throw new NotImplementedException();
        }

        public IImmutableList<CommandBase> ComputeDelta(DatabaseModel targetModel)
        {
            throw new NotImplementedException();
        }
    }
}