using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel.Policies
{
    /// <summary>
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/retention-policy#delete-retention-policy"/>
    /// </summary>
    [Command(14000, "Delete Retention Policies")]
    public class DeleteRetentionPolicyCommand : EntityPolicyCommandBase
    {
        public override string CommandFriendlyName => ".delete <entity> policy retention";

        public override string ScriptPath => EntityType == EntityType.Database
            ? $"tables/policies/retention/delete"
            : $"db/policies/delete";

        public DeleteRetentionPolicyCommand(EntityType entityType, EntityName entityName)
            : base(entityType, entityName)
        {
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var entityType = ExtractEntityType(rootElement);
            var entityName = rootElement.GetFirstDescendant<NameReference>();

            return new DeleteRetentionPolicyCommand(entityType, EntityName.FromCode(entityName.Name));
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();

            builder.Append(".delete ");
            builder.Append(EntityType == EntityType.Table ? "table" : "database");
            builder.Append(" ");
            if (EntityType == EntityType.Database && context?.CurrentDatabaseName != null)
            {
                builder.Append(context.CurrentDatabaseName.ToScript());
            }
            else
            {
                builder.Append(EntityName.ToScript());
            }
            builder.Append(" policy retention");

            return builder.ToString();
        }
    }
}