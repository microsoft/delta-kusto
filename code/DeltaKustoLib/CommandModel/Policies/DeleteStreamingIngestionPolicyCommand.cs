using Kusto.Language.Syntax;
using System;
using System.Text;

namespace DeltaKustoLib.CommandModel.Policies
{
    /// <summary>
    /// Models <see cref="https://learn.microsoft.com/en-us/azure/data-explorer/kusto/management/streamingingestionpolicy"/>
    /// </summary>
    [Command(17000, "Delete Streaming Ingestion Policy")]
    public class DeleteStreamingIngestionPolicyCommand : EntityPolicyCommandBase
    {
        public override string CommandFriendlyName => ".delete <entity> policy streamingingestion";

        public override string ScriptPath => EntityType == EntityType.Database
            ? $"tables/policies/streamingingestion/delete"
            : $"db/policies/delete";

        public DeleteStreamingIngestionPolicyCommand(EntityType entityType, EntityName entityName)
            : base(entityType, entityName)
        {
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var entityType = ExtractEntityType(rootElement);
            var entityName = rootElement.GetFirstDescendant<NameReference>();

            return new DeleteStreamingIngestionPolicyCommand(entityType, EntityName.FromCode(entityName.Name));
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
            builder.Append(" policy streamingingestion");

            return builder.ToString();
        }
    }
}