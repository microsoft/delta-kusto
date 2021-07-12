using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel.Policies
{
    public abstract class TableOnlyPolicyCommandBase : PolicyCommandBase
    {
        public EntityName TableName { get; }

        public TableOnlyPolicyCommandBase(EntityName tableName, JsonDocument policy)
            : base(policy)
        {
            TableName = tableName;
        }

        public TableOnlyPolicyCommandBase(EntityName tableName)
            : this(tableName, ToJsonDocument(new object()))
        {
        }
    }
}