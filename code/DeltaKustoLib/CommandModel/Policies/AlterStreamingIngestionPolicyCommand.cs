using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Kusto.Language.Syntax;

namespace DeltaKustoLib.CommandModel.Policies
{
    /// <summary>
    /// Models <see cref="https://learn.microsoft.com/en-us/azure/data-explorer/kusto/management/streamingingestionpolicy"/>
    /// </summary>
    [Command(17100, "Alter Streaming Ingestion Policy")]
    public class AlterStreamingIngestionPolicyCommand : EntityPolicyCommandBase
    {
        public override string CommandFriendlyName => ".alter <entity> policy streamingingestion";

        public override string ScriptPath => EntityType == EntityType.Database
            ? $"tables/policies/streamingingestion/create/{EntityName}"
            : $"databases/policies/streamingingestion/create";

        public AlterStreamingIngestionPolicyCommand(
            EntityType entityType,
            EntityName entityName,
            JsonDocument policy) : base(entityType, entityName, policy)
        {
        }

        public AlterStreamingIngestionPolicyCommand(
            EntityType entityType,
            EntityName entityName,
            bool isEnabled,
            double? hintAllocatedRated)
            : this(
                  entityType,
                  entityName,
                  ToJsonDocument(
                    new
                    {
                        IsEnabled = isEnabled,
                        HintAllocatedRated = hintAllocatedRated
                    }))
        {
        }

        public override string ToScript(ScriptingContext? context = null)
        {
            var builder = new StringBuilder();

            builder.Append(".alter ");
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
            builder.Append(" policy streamingingestion ");
            builder.AppendLine();
            builder.AppendLine("```");
            builder.AppendLine(SerializePolicy());
            builder.AppendLine("```");

            return builder.ToString();
        }

        internal static CommandBase? FromCode(CommandBlock commandBlock)
        {
            var entityType = ExtractEntityType(commandBlock);
            var entityNameReference = commandBlock.GetDescendants<NameReference>().Last();

            var policyText = QuotedText.FromLiteral(
                commandBlock.GetUniqueDescendant<LiteralExpression>(
                    "StreamingIngestion",
                    e => e.NameInParent == "StreamingIngestionPolicy"));
            var policy = Deserialize<JsonDocument>(policyText.Text);

            if (policy == null)
            {
                throw new DeltaException(
                    $"Can't extract policy objects from {policyText.ToScript()}");
            }

            return new AlterStreamingIngestionPolicyCommand(
                entityType,
                EntityName.FromCode(entityNameReference.Name),
                policy);
        }

        internal static IEnumerable<CommandBase> ComputeDelta(
            AlterStreamingIngestionPolicyCommand? currentCommand,
            AlterStreamingIngestionPolicyCommand? targetCommand)
        {
            var hasCurrent = currentCommand != null;
            var hasTarget = targetCommand != null;

            if (hasCurrent && !hasTarget)
            {
                // No target, we remove the current policy
                yield return new DeleteStreamingIngestionPolicyCommand(
                    currentCommand!.EntityType,
                    currentCommand!.EntityName);
            }
            else if (hasTarget)
            {
                if (!hasCurrent || !currentCommand!.Equals(targetCommand!))
                {
                    // There is a target and either no current or the current is different
                    yield return targetCommand!;
                }
            }
            else
            {   // Both target and current are null: no delta
            }
        }
    }
}