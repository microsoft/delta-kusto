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
    /// Models <see cref="https://learn.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-ingestion-time-policy-command"/>
    /// </summary>
    [Command(21200, "Alter (plural) Ingestion Time Policies")]
    public class AlterIngestionTimePluralPolicyCommand : PolicyCommandBase
    {
        public IImmutableList<EntityName> TableNames { get; }

        public bool AreEnabled { get; }

        public override string CommandFriendlyName => ".alter tables policy ingestiontime";

        public override string SortIndex => TableNames.First().Name;

        public override string ScriptPath => "tables/policies/ingestiontime/create-many/";

        public AlterIngestionTimePluralPolicyCommand(
            IEnumerable<EntityName> tableNames,
            bool areEnabled)
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

            AreEnabled = areEnabled;
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var tableNames = rootElement.GetDescendants<NameReference>();
            var enabledToken = rootElement.GetUniqueDescendant<SyntaxToken>(
                "Ingestion time",
                t => t.Kind == SyntaxKind.BooleanLiteralToken);
            var areEnabled = (bool)enabledToken.Value;

            return new AlterIngestionTimePluralPolicyCommand(
                tableNames.Select(n => EntityName.FromCode(n)),
                areEnabled);
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();

            builder.Append(".alter tables (");
            builder.Append(string.Join(", ", TableNames.Select(n => n.ToScript())));
            builder.Append(") policy ingestiontime ");
            builder.AppendLine(AreEnabled.ToString().ToLower());

            return builder.ToString();
        }

        internal static IEnumerable<CommandBase> ComputeDelta(
            AlterIngestionTimePolicyCommand? currentCommand,
            AlterIngestionTimePolicyCommand? targetCommand)
        {
            var hasCurrent = currentCommand != null;
            var hasTarget = targetCommand != null;

            if (hasCurrent && !hasTarget)
            {   //  No target, we remove the current policy
                yield return new DeleteIngestionTimePolicyCommand(currentCommand!.EntityName);
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