using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel.Policies.Caching
{
    /// <summary>
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/cache-policy#altering-the-cache-policy"/>
    /// </summary>
    [Command(11100, "Alter Caching Policies")]
    public class AlterCachingPolicyCommand : EntityPolicyCommandBase
    {
        public KustoTimeSpan HotData { get; }

        public KustoTimeSpan HotIndex { get; }

        public IImmutableList<HotWindow> HotWindows { get; }

        public override string CommandFriendlyName => ".alter <entity> policy caching";

        public override string ScriptPath => EntityType == EntityType.Database
            ? $"tables/policies/caching/create/{EntityName}"
            : $"databases/policies/caching/create";

        public AlterCachingPolicyCommand(
            EntityType entityType,
            EntityName entityName,
            TimeSpan hotData,
            TimeSpan hotIndex,
            IEnumerable<HotWindow> hotWindows) : base(entityType, entityName)
        {
            HotData = new KustoTimeSpan(hotData);
            HotIndex = new KustoTimeSpan(hotIndex);
            HotWindows = hotWindows.ToImmutableArray();
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var entityType = ExtractEntityType(rootElement);
            var entityNames = rootElement.GetDescendants<NameReference>();
            var entityName = entityNames.LastOrDefault();
            var (hotData, hotIndex) = ExtractHotDurations(rootElement);
            var hotWindowTimes = rootElement.GetDescendants<SyntaxToken>(
                t => t.Kind == SyntaxKind.DateTimeLiteralToken);

            if (entityName == null)
            {
                throw new DeltaException("No entity name found");
            }
            if (hotWindowTimes.Count() % 2 == 1)
            {
                throw new DeltaException(
                    "Hot Window date times should come in even numbers, "
                    + $"not '{hotWindowTimes.Count()}'");
            }

            var hotWindows = hotWindowTimes
                .Chunk(2)
                .Select(c => new HotWindow((DateTime)c[0].Value, (DateTime)c[1].Value));

            return new AlterCachingPolicyCommand(
                entityType,
                EntityName.FromCode(entityName.Name),
                hotData,
                hotIndex,
                hotWindows);
        }

        public override bool Equals(CommandBase? other)
        {
            var otherPolicy = other as AlterCachingPolicyCommand;
            var areEqualed = otherPolicy != null
                && base.Equals(otherPolicy)
                && otherPolicy.HotData.Equals(HotData)
                && otherPolicy.HotIndex.Equals(HotIndex)
                && otherPolicy.HotWindows.SequenceEqual(HotWindows);

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
            builder.Append(" policy caching ");
            if (HotData.Equals(HotIndex))
            {
                builder.Append("hot = ");
                builder.Append(HotData);
            }
            else
            {
                builder.Append("hotdata = ");
                builder.Append(HotData);
                builder.Append(" hotindex = ");
                builder.Append(HotIndex);
            }
            foreach(var hotWindow in HotWindows)
            {
                builder.AppendLine();
                builder.Append(", ");
                builder.Append(hotWindow);
            }

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

        private static (TimeSpan hotData, TimeSpan hotIndex) ExtractHotDurations(
            SyntaxElement rootElement)
        {
            var hotExpression = rootElement.GetUniqueDescendant<LiteralExpression>(
                "hot",
                e => e.NameInParent == "HotData" || e.NameInParent == "Timespan");
            var hotValue = (TimeSpan)hotExpression.LiteralValue;
            var hotIndexExpression = rootElement.GetAtMostOneDescendant<LiteralExpression>(
                "hotindex",
                e => e.NameInParent == "HotIndex");

            if (hotIndexExpression == null)
            {
                return (hotValue, hotValue);
            }
            else
            {
                var hotIndexValue = (TimeSpan)hotIndexExpression.LiteralValue;

                return (hotValue, hotIndexValue);
            }
        }
    }
}