using Kusto.Language.Syntax;
using System;
using System.Text;

namespace DeltaKustoLib.CommandModel.Policies
{
    /// <summary>
    /// Models <see cref="https://learn.microsoft.com/en-us/azure/data-explorer/kusto/management/delete-table-partitioning-policy-command"/>
    /// </summary>
    [Command(18000, "Delete Streaming Ingestion Policy")]
    public class DeletePartitioningPolicyCommand : TableOnlyPolicyCommandBase
    {
        public override string CommandFriendlyName => ".delete <entity> policy partitioning";

        public override string ScriptPath =>
            "tables/policies/streamingingestion/delete";

        public DeletePartitioningPolicyCommand(EntityName tableName)
            : base(tableName)
        {
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var entityName = rootElement.GetFirstDescendant<NameReference>();

            return new DeletePartitioningPolicyCommand(EntityName.FromCode(entityName.Name));
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();

            builder.AppendLine($".delete table {TableName} policy partitioning");

            return builder.ToString();
        }
    }
}