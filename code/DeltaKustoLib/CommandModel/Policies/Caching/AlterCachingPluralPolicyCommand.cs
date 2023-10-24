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
    [Command(11200, "Alter (plural) Caching Policies")]
    public class AlterCachingPluralPolicyCommand : PolicyCommandBase, ISingularToPluralCommand
    {
        public IImmutableList<EntityName> TableNames { get; }

        public KustoTimeSpan HotData { get; }

        public KustoTimeSpan HotIndex { get; }

        public IImmutableList<HotWindow> HotWindows { get; }

        public override string CommandFriendlyName => ".alter tables policy caching";

        public override string SortIndex => TableNames.First().Name;

        public override string ScriptPath => "tables/policies/caching/create-many";

        public AlterCachingPluralPolicyCommand(
            IEnumerable<EntityName> tableNames,
            TimeSpan hotData,
            TimeSpan hotIndex,
            IEnumerable<HotWindow> hotWindows)
        {
            TableNames = tableNames
                .OrderBy(t => t.Name)
                .ToImmutableArray();

            HotData = new KustoTimeSpan(hotData);
            HotIndex = new KustoTimeSpan(hotIndex);
            HotWindows = hotWindows.ToImmutableArray();
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var tableNames = rootElement.GetDescendants<NameReference>();
            var (hotData, hotIndex) = ExtractHotDurations(rootElement);
            var hotWindowTimes = rootElement.GetDescendants<SyntaxToken>(
                t => t.Kind == SyntaxKind.DateTimeLiteralToken);

            if (hotWindowTimes.Count() % 2 == 1)
            {
                throw new DeltaException(
                    "Hot Window date times should come in even numbers, "
                    + $"not '{hotWindowTimes.Count()}'");
            }

            var hotWindows = hotWindowTimes
                .Chunk(2)
                .Select(c => new HotWindow((DateTime)c[0].Value, (DateTime)c[1].Value));

            return new AlterCachingPluralPolicyCommand(
                tableNames.Select(n => EntityName.FromCode(n)),
                hotData,
                hotIndex,
                hotWindows);
        }

        public override bool Equals(CommandBase? other)
        {
            var otherPolicy = other as AlterCachingPluralPolicyCommand;
            var areEqualed = otherPolicy != null
                && base.Equals(otherPolicy)
                && Enumerable.SequenceEqual(TableNames, otherPolicy.TableNames)
                && otherPolicy.HotData.Equals(HotData)
                && otherPolicy.HotIndex.Equals(HotIndex)
                && otherPolicy.HotWindows.SequenceEqual(HotWindows);

            return areEqualed;
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();

            builder.Append(".alter tables (");
            builder.Append(string.Join(", ", TableNames.Select(n => n.ToScript())));
            builder.Append(") policy caching ");
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
            foreach (var hotWindow in HotWindows)
            {
                builder.AppendLine();
                builder.Append(", ");
                builder.Append(hotWindow);
            }
            builder.AppendLine();

            return builder.ToString();
        }

        IEnumerable<CommandBase> ISingularToPluralCommand.ToPlural(
            IEnumerable<CommandBase> singularCommands)
        {
            var singularPolicyCommands = singularCommands
                .Cast<AlterCachingPolicyCommand>();

            //  We might want to cap batches to a maximum size?
            var pluralCommands = singularPolicyCommands
                .GroupBy(c => (HotIndex: c.HotIndex, HotData: c.HotData, HotWindows: c.HotWindows))
                .Select(g => new AlterCachingPluralPolicyCommand(
                    g.Select(c => c.EntityName),
                    g.Key.HotData.Duration!.Value,
                    g.Key.HotIndex.Duration!.Value,
                    g.Key.HotWindows));

            return pluralCommands.ToImmutableArray();
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