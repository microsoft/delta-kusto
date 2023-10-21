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
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/retention-policy#alter-retention-policy"/>
    /// </summary>
    [Command(14200, "Alter Retention Policies")]
    public class AlterTablesRetentionPolicyCommand : PolicyCommandBase
    {
        public IImmutableList<EntityName> TableNames { get; }

        public override string CommandFriendlyName => ".alter tables policy retention";

        public override string SortIndex => TableNames.First().Name;

        public override string ScriptPath => "tables/policies/retention/create-many";

        public AlterTablesRetentionPolicyCommand(
            IEnumerable<EntityName> tableNames,
            JsonDocument policy) : base(policy)
        {
            TableNames = tableNames
                .OrderBy(t => t.Name)
                .ToImmutableArray();

            if (!TableNames.Any())
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tableNames),
                    "Should contain at least one table name");
            }
        }

        public AlterTablesRetentionPolicyCommand(
            IEnumerable<EntityName> tableNames,
            TimeSpan softDeletePeriod,
            bool recoverability)
            : this(
                  tableNames,
                  ToJsonDocument(new
                  {
                      SoftDeletePeriod = softDeletePeriod.ToString(),
                      Recoverability = recoverability ? "Enabled" : "Disabled"
                  }))
        {
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var tableNames = rootElement
                .GetDescendants<SyntaxElement>(n => n.NameInParent == "Name"
                //  Different type depending if it's a simple name or not
                && (n is TokenName || n is LiteralExpression))
                //.GetDescendants<LiteralExpression>(n => n.NameInParent == "Name")
                //.GetDescendants<TokenName>(n => n.NameInParent == "Name")
                .Select(t => EntityName.FromCode(t));
            var policyText = QuotedText.FromLiteral(
                rootElement.GetUniqueDescendant<LiteralExpression>(
                    "RetentionPolicy",
                    e => e.NameInParent == "RetentionPolicy"));
            var policy = Deserialize<JsonDocument>(policyText.Text);

            if (policy == null)
            {
                throw new DeltaException(
                    $"Can't extract policy objects from {policyText.ToScript()}");
            }

            return new AlterTablesRetentionPolicyCommand(tableNames, policy);
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();

            builder.Append(".alter tables (");
            builder.Append(string.Join(", ", TableNames.Select(t => t.ToScript())));
            builder.Append(") policy retention");
            builder.AppendLine();
            builder.Append("```");
            builder.Append(SerializePolicy());
            builder.AppendLine();
            builder.Append("```");

            return builder.ToString();
        }
    }
}