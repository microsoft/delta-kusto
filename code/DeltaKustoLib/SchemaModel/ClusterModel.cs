using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace DeltaKustoLib.SchemaModel
{
    public class ClusterModel
    {
        public IImmutableList<DatabaseModel> Databases { get; }

        private ClusterModel(IEnumerable<DatabaseModel> databaseModels)
        {
            Databases = databaseModels.ToImmutableArray();
        }

        public static ClusterModel MergeModels(IEnumerable<DatabaseModel> databaseModels)
        {
            throw new NotImplementedException();
        }
    }
}