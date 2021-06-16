using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel
{
    /// <summary>
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/cache-policy#altering-the-cache-policy"/>
    /// </summary>
    public class AlterCachingPolicyCommand : CommandBase
    {
        public EntityType EntityType { get; }

        public EntityName EntityName { get; }

        public TimeSpan Duration { get; }

        public string DurationText { get; }

        public override string CommandFriendlyName => ".alter <entity> policy caching";

        public AlterCachingPolicyCommand(
            EntityType entityType,
            EntityName entityName,
            TimeSpan duration,
            string durationText)
        {
            if (entityType != EntityType.Database && entityType != EntityType.Table)
            {
                throw new NotSupportedException(
                    $"Entity type {entityType} isn't supported in this context");
            }
            EntityType = entityType;
            EntityName = entityName;
            Duration = duration;
            DurationText = durationText;
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var entityKinds = rootElement
                .GetDescendants<SyntaxElement>(s => s.Kind == SyntaxKind.TableKeyword
                || s.Kind == SyntaxKind.DatabaseKeyword)
                .Select(s => s.Kind);

            if (!entityKinds.Any())
            {
                throw new DeltaException("Alter caching policy requires to act on a table or database (cluster isn't supported)");
            }
            var entityKind = entityKinds.First();
            var entityType = entityKind == SyntaxKind.TableKeyword
                ? EntityType.Table
                : EntityType.Database;
            var entityName = rootElement.GetUniqueDescendant<NameReference>("Name reference");
            var durationExpression = rootElement.GetUniqueDescendant<LiteralExpression>(
                "Duration",
                ex => ex.NameInParent == "Timespan");
            var duration = (TimeSpan)durationExpression.LiteralValue;
            var durationText = durationExpression.Token.Text;

            return new AlterCachingPolicyCommand(
                entityType,
                EntityName.FromCode(entityName.Name),
                duration,
                durationText);
        }

        public override bool Equals(CommandBase? other)
        {
            var otherFunction = other as AlterCachingPolicyCommand;
            var areEqualed = otherFunction != null
                && otherFunction.EntityType.Equals(EntityType)
                && otherFunction.EntityName.Equals(EntityName)
                && otherFunction.Duration.Equals(Duration)
                && otherFunction.DurationText.Equals(DurationText);

            return areEqualed;
        }

        public override string ToScript()
        {
            var builder = new StringBuilder();

            builder.Append(".alter ");
            builder.Append(EntityType == EntityType.Table ? "table" : "database");
            builder.Append(" ");
            builder.Append(EntityName.ToScript());
            builder.Append(" policy caching hot = ");
            builder.Append(DurationText);

            return builder.ToString();
        }

        internal static IEnumerable<CommandBase> ComputeDelta(
            AlterCachingPolicyCommand? currentCommand,
            AlterCachingPolicyCommand? targetCommand)
        {
            var hasCurrent = currentCommand != null;
            var hasTarget = targetCommand != null;

            if (hasCurrent && !hasTarget)
            {   //  No target, we remove the current policy
                yield return new DeleteCachingPolicyCommand(
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