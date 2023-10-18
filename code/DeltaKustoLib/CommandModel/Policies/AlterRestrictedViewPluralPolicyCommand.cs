using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;
using Kusto.Language.Syntax;

namespace DeltaKustoLib.CommandModel.Policies
{
    /// <summary>
    /// Models <see cref="https://learn.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-table-restricted-view-access-policy-command"/>
    /// </summary>
    [Command(19200, "Alter tables restricted view Policy")]
    public class AlterRestrictedViewPluralPolicyCommand : PolicyCommandBase
    {
        public IImmutableList<EntityName> TableNames { get; }

        public bool AreEnabled { get; }

        public override string CommandFriendlyName => ".alter table policy restricted_view_access";

        public override string SortIndex => TableNames.First().Name;

        public override string ScriptPath =>
            "tables/policies/restricted_view_access/create-many";

        public AlterRestrictedViewPluralPolicyCommand(
            IEnumerable<EntityName> tableNames,
            bool areEnabled)
        {
            if (!tableNames.Any())
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tableNames),
                    "Must have at least one table name");
            }

            TableNames = tableNames.ToImmutableArray();
            AreEnabled = areEnabled;
        }

        public override string ToScript(ScriptingContext? context = null)
        {
            var builder = new StringBuilder();

            builder.Append(".alter tables (");
            builder.Append(string.Join(", ", TableNames.Select(t => t.ToScript())));
            builder.Append($") policy restricted_view_access {AreEnabled}");
            builder.AppendLine();

            return builder.ToString();
        }

        internal static CommandBase? FromCode(CommandBlock commandBlock)
        {
            var nameReferences = commandBlock.GetDescendants<NameReference>();
            var booleanToken = commandBlock.GetUniqueDescendant<SyntaxToken>(
                "boolean",
                t => t.Kind == SyntaxKind.BooleanLiteralToken);

            return new AlterRestrictedViewPluralPolicyCommand(
                nameReferences.Select(r => EntityName.FromCode(r)),
                (bool)booleanToken.Value);
        }
    }
}