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
    public class AlterRetentionPolicyCommand : CommandBase
    {
        private static readonly JsonSerializerOptions _policiesSerializerOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        public EntityType EntityType { get; }

        public EntityName EntityName { get; }

        public TimeSpan SoftDelete { get; }

        public bool Recoverability { get; }

        public override string CommandFriendlyName => ".alter <entity> policy retention";

        public AlterRetentionPolicyCommand(
            EntityType entityType,
            EntityName entityName,
            TimeSpan softDelete,
            bool recoverability)
        {
            if (entityType != EntityType.Database && entityType != EntityType.Table)
            {
                throw new NotSupportedException(
                    $"Entity type {entityType} isn't supported in this context");
            }
            EntityType = entityType;
            EntityName = entityName;
            SoftDelete = softDelete;
            Recoverability = recoverability;
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var entityKinds = rootElement
                .GetDescendants<SyntaxElement>(s => s.Kind == SyntaxKind.TableKeyword
                || s.Kind == SyntaxKind.DatabaseKeyword)
                .Select(s => s.Kind);

            if (!entityKinds.Any())
            {
                throw new DeltaException("Alter retention policy requires to act on a table or database (cluster isn't supported)");
            }
            var entityKind = entityKinds.First();
            var entityType = entityKind == SyntaxKind.TableKeyword
                ? EntityType.Table
                : EntityType.Database;
            var entityName = rootElement.GetFirstDescendant<NameReference>();
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

            return new AlterRetentionPolicyCommand(
                entityType,
                EntityName.FromCode(entityName.Name),
                policy.GetSoftDelete(),
                policy.GetRecoverability());
        }

        public override bool Equals(CommandBase? other)
        {
            var otherFunction = other as AlterRetentionPolicyCommand;
            var areEqualed = otherFunction != null
                && otherFunction.EntityType.Equals(EntityType)
                && otherFunction.EntityName.Equals(EntityName)
                && otherFunction.SoftDelete.Equals(SoftDelete)
                && otherFunction.Recoverability.Equals(Recoverability);

            return areEqualed;
        }

        public override string ToScript()
        {
            var builder = new StringBuilder();
            var policy = RetentionPolicy.Create(SoftDelete, Recoverability);

            builder.Append(".alter ");
            builder.Append(EntityType == EntityType.Table ? "table" : "database");
            builder.Append(" ");
            builder.Append(EntityName.ToScript());
            builder.Append(" policy retention");
            builder.AppendLine();
            builder.Append("```");
            builder.Append(JsonSerializer.Serialize(policy, _policiesSerializerOptions));
            builder.AppendLine();
            builder.Append("```");

            return builder.ToString();
        }

        internal static IEnumerable<CommandBase> ComputeDelta(
            AlterRetentionPolicyCommand? currentCommand,
            AlterRetentionPolicyCommand? targetCommand)
        {
            throw new NotImplementedException();
        }
    }
}