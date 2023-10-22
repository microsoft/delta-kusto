using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel.Policies.IngestionBatching
{
    /// <summary>
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/batching-policy#altering-the-ingestionbatching-policy"/>
    /// </summary>
    [Command(12200, "Alter Ingestion Batching (plural) Policies")]
    public class AlterIngestionBatchingPluralPolicyCommand : PolicyCommandBase, ISingularToPluralCommand
    {
        public IImmutableList<EntityName> TableNames { get; }

        public override string CommandFriendlyName => ".alter tables policy ingestionbatching";

        public override string SortIndex => TableNames.First().Name;

        public override string ScriptPath => "tables/policies/ingestionbatching/create-many";

        public AlterIngestionBatchingPluralPolicyCommand(
            IEnumerable<EntityName> tableNames,
            JsonDocument policy) : base(policy)
        {
            TableNames = tableNames
                .OrderBy(t => t.Name)
                .ToImmutableArray();
        }

        public AlterIngestionBatchingPluralPolicyCommand(
            IEnumerable<EntityName> tableNames,
            TimeSpan maximumBatchingTimeSpan,
            int maximumNumberOfItems,
            int maximumRawDataSizeMb) : this(
                tableNames,
                ToJsonDocument(new
                {
                    MaximumBatchingTimeSpan = maximumBatchingTimeSpan.ToString(),
                    MaximumNumberOfItems = maximumNumberOfItems,
                    MaximumRawDataSizeMb = maximumRawDataSizeMb
                }))
        {
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var tableNames = rootElement.GetDescendants<NameReference>();
            var policyText = QuotedText.FromLiteral(
                rootElement.GetUniqueDescendant<LiteralExpression>(
                    "IngestionBatching",
                    e => e.NameInParent == "IngestionBatchingPolicy"));
            var policy = Deserialize<JsonDocument>(policyText.Text);

            if (policy == null)
            {
                throw new DeltaException(
                    $"Can't extract policy objects from {policyText.ToScript()}");
            }

            return new AlterIngestionBatchingPluralPolicyCommand(
                tableNames.Select(n => EntityName.FromCode(n)),
                policy);
        }

        public override bool Equals(CommandBase? other)
        {
            var otherPolicy = other as AlterIngestionBatchingPluralPolicyCommand;
            var areEqualed = otherPolicy != null
                && base.Equals(otherPolicy)
                && Enumerable.SequenceEqual(TableNames, otherPolicy.TableNames);

            return areEqualed;
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();

            builder.Append(".alter tables (");
            builder.Append(string.Join(", ", TableNames.Select(n => n.ToScript())));
            builder.AppendLine(") policy ingestionbatching");
            builder.AppendLine("```");
            builder.AppendLine(SerializePolicy());
            builder.AppendLine("```");

            return builder.ToString();
        }

        IEnumerable<CommandBase> ISingularToPluralCommand.ToPlural(
            IEnumerable<CommandBase> singularCommands)
        {
            var singularPolicyCommands = singularCommands
                .Cast<AlterIngestionBatchingPolicyCommand>();

            //  We might want to cap batches to a maximum size?
            var pluralCommands = singularPolicyCommands
                .GroupBy(c => c.Policy, JsonDocumentComparer)
                .Select(g => new AlterIngestionBatchingPluralPolicyCommand(
                    g.Select(c => c.EntityName),
                    g.Key));

            return pluralCommands.ToImmutableArray();
        }
    }
}