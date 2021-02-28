using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace DeltaKustoLib.KustoModel
{
    public class ClusterModel
    {
        public IImmutableList<DatabaseModel> Databases { get; }

        private ClusterModel(IEnumerable<DatabaseModel> databaseModels)
        {
            Databases = databaseModels.ToImmutableArray();
        }

        public static ClusterModel FromCommands(IEnumerable<CommandBase> commands)
        {
            throw new NotImplementedException();
        }
    }
}