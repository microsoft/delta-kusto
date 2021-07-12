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
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/merge-policy#alter-policy"/>
    /// </summary>
    [CommandTypeOrder(13100)]
    public class AlterMergePolicyCommand : EntityPolicyCommandBase
    {
        public override string CommandFriendlyName => ".alter <entity> policy merge";

        public AlterMergePolicyCommand(
            EntityType entityType,
            EntityName entityName,
            JsonDocument policy) : base(entityType, entityName, policy)
        {
        }

        public AlterMergePolicyCommand(
            EntityType entityType,
            EntityName entityName,
            int rowCountUpperBoundForMerge,
            int maxExtentsToMerge,
            TimeSpan loopPeriod)
            : this(
                  entityType,
                  entityName,
                  ToJsonDocument(new
                  {
                      RowCountUpperBoundForMerge = rowCountUpperBoundForMerge,
                      MaxExtentsToMerge = maxExtentsToMerge,
                      LoopPeriod = loopPeriod.ToString()
                  }))
        {
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var entityKinds = rootElement
                .GetDescendants<SyntaxElement>(s => s.Kind == SyntaxKind.TableKeyword
                || s.Kind == SyntaxKind.DatabaseKeyword)
                .Select(s => s.Kind);

            if (!entityKinds.Any())
            {
                throw new DeltaException("Alter merge policy requires to act on a table or database (cluster isn't supported)");
            }
            var entityKind = entityKinds.First();
            var entityType = entityKind == SyntaxKind.TableKeyword
                ? EntityType.Table
                : EntityType.Database;
            var entityName = rootElement.GetDescendants<NameReference>().Last();
            var policyText = QuotedText.FromLiteral(
                rootElement.GetUniqueDescendant<LiteralExpression>(
                    "Merge",
                    e => e.NameInParent == "MergePolicy"));
            var policy = JsonSerializer.Deserialize<JsonDocument>(policyText.Text);

            if (policy == null)
            {
                throw new DeltaException(
                    $"Can't extract policy objects from {policyText.ToScript()}");
            }

            return new AlterMergePolicyCommand(
                entityType,
                EntityName.FromCode(entityName.Name),
                policy);
        }

        public override bool Equals(CommandBase? other)
        {
            var otherFunction = other as AlterMergePolicyCommand;
            var areEqualed = otherFunction != null
                && otherFunction.EntityType.Equals(EntityType)
                && otherFunction.EntityName.Equals(EntityName)
                && PolicyEquals(otherFunction);

            return areEqualed;
        }

        public override string ToScript(ScriptingContext? context)
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
            builder.Append(" policy merge");
            builder.AppendLine();
            builder.Append("```");
            builder.Append(SerializePolicy());
            builder.AppendLine();
            builder.Append("```");

            return builder.ToString();
        }

        internal static IEnumerable<CommandBase> ComputeDelta(
            AlterMergePolicyCommand? currentCommand,
            AlterMergePolicyCommand? targetCommand)
        {
            var hasCurrent = currentCommand != null;
            var hasTarget = targetCommand != null;

            if (hasCurrent && !hasTarget)
            {   //  No target, we remove the current policy
                yield return new DeleteMergePolicyCommand(
                    currentCommand!.EntityType,
                    currentCommand!.EntityName);
            }
            else if (hasTarget)
            {
                if (!hasCurrent || !currentCommand!.Equals(targetCommand!))
                {   //  There is a target and either no current or the current is different
                    yield return targetCommand!;
                }
            }
            else
            {   //  Both target and current are null:  no delta
            }
        }
    }
}