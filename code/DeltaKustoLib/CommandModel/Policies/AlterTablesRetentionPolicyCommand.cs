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
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/cache-policy#altering-the-cache-policy"/>
    /// </summary>
    public class AlterTablesRetentionPolicyCommand : CommandBase
    {
        #region Inner Types
        private record RetentionPolicy
        {
            public static RetentionPolicy Create(TimeSpan softDelete, bool recoverability)
            {
                return new RetentionPolicy
                {
                    SoftDelete = softDelete.ToString(),
                    Recoverability = recoverability
                    ? EnableBoolean.Enabled.ToString()
                    : EnableBoolean.Disabled.ToString()
                };
            }

            public string SoftDelete { get; init; } = TimeSpan.FromDays(36500).ToString();

            public string Recoverability { get; init; } = EnableBoolean.Enabled.ToString();

            public TimeSpan GetSoftDelete()
            {
                TimeSpan time;

                if (TimeSpan.TryParse(SoftDelete, out time))
                {
                    return time;
                }
                else
                {
                    throw new DeltaException(
                        $"Can't parse 'SoftDelete' value '{SoftDelete}'");
                }
            }

            public bool GetRecoverability()
            {
                EnableBoolean flag;

                if (Enum.TryParse<EnableBoolean>(Recoverability, out flag))
                {
                    return flag == EnableBoolean.Enabled;
                }
                else
                {
                    throw new DeltaException(
                        $"Can't parse 'Recoverability' flag value '{Recoverability}'");
                }
            }
        }
        #endregion

        private static readonly JsonSerializerOptions _policiesSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public IImmutableList<EntityName> TableNames { get; }

        public TimeSpan SoftDelete { get; }

        public bool Recoverability { get; }

        public override string CommandFriendlyName => ".alter tables policy retention";

        public AlterTablesRetentionPolicyCommand(
            IEnumerable<EntityName> tableNames,
            TimeSpan softDelete,
            bool recoverability)
        {
            TableNames = tableNames.ToImmutableArray();
            SoftDelete = softDelete;
            Recoverability = recoverability;

            if(!TableNames.Any())
            {
                throw new ArgumentOutOfRangeException(
                    nameof(tableNames),
                    "Should contain at least one table name");
            }
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
            var policy = JsonSerializer.Deserialize<RetentionPolicy>(policyText.Text);

            if (policy == null)
            {
                throw new DeltaException(
                    $"Can't extract policy objects from {policyText.ToScript()}");
            }

            return new AlterTablesRetentionPolicyCommand(
                tableNames,
                policy.GetSoftDelete(),
                policy.GetRecoverability());
        }

        public override bool Equals(CommandBase? other)
        {
            var otherFunction = other as AlterTablesRetentionPolicyCommand;
            var areEqualed = otherFunction != null
                && otherFunction.TableNames.SequenceEqual(TableNames)
                && otherFunction.SoftDelete.Equals(SoftDelete)
                && otherFunction.Recoverability.Equals(Recoverability);

            return areEqualed;
        }

        public override string ToScript()
        {
            var builder = new StringBuilder();
            var policy = RetentionPolicy.Create(SoftDelete, Recoverability);

            builder.Append(".alter tables (");
            builder.Append(string.Join(", ", TableNames.Select(t => t.ToScript())));
            builder.Append(") policy retention");
            builder.AppendLine();
            builder.Append("```");
            builder.Append(JsonSerializer.Serialize(policy, _policiesSerializerOptions));
            builder.AppendLine();
            builder.Append("```");

            return builder.ToString();
        }
    }
}