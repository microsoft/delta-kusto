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
        public EntityName EntityName { get; }

        public TableOnlyPolicyCommandBase(EntityName tableName, JsonDocument policy)
            : base(policy)
        {
            EntityName = tableName;
        }

        public TableOnlyPolicyCommandBase(EntityName tableName)
            : this(tableName, ToJsonDocument(new object()))
        {
        }

        public override string SortIndex => EntityName.Name;

        public override bool Equals(CommandBase? other)
        {
            var otherPolicy = other as TableOnlyPolicyCommandBase;
            var areEqualed = otherPolicy != null
                && base.Equals(other)
                && otherPolicy.EntityName.Equals(EntityName);

            return areEqualed;
        }
    }
}