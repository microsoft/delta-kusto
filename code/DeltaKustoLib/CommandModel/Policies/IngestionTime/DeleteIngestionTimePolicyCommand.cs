using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel.Policies.IngestionTime
{
    /// <summary>
    /// Models <see cref="https://learn.microsoft.com/en-us/azure/data-explorer/kusto/management/delete-ingestion-time-policy-command"/>
    /// </summary>
    [Command(21000, "Delete Ingestion Time Policies")]
    public class DeleteIngestionTimePolicyCommand : TableOnlyPolicyCommandBase
    {
        public override string CommandFriendlyName => ".delete <entity> policy auto_delete";

        public override string ScriptPath => $"tables/policies/auto_delete/delete";

        public DeleteIngestionTimePolicyCommand(EntityName tableName)
            : base(tableName)
        {
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var entityName = rootElement.GetFirstDescendant<NameReference>();

            return new DeleteIngestionTimePolicyCommand(EntityName.FromCode(entityName.Name));
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();

            builder.Append(".delete table ");
            builder.Append(TableName.ToScript());
            builder.AppendLine(" policy ingestiontime");

            return builder.ToString();
        }
    }
}