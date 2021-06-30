using Kusto.Language.Syntax;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace DeltaKustoLib.CommandModel.Policies
{
    public class DeleteCachingPolicyCommand : CommandBase
    {
        public EntityType EntityType { get; }

        public EntityName EntityName { get; }

        public override string CommandFriendlyName => throw new NotImplementedException();

        public DeleteCachingPolicyCommand(EntityType entityType, EntityName entityName)
        {
            if (entityType != EntityType.Database && entityType != EntityType.Table)
            {
                throw new NotSupportedException(
                    $"Entity type {entityType} isn't supported in this context");
            }
            EntityType = entityType;
            EntityName = entityName;
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
            var entityName = rootElement
                .GetAtLeastOneDescendant<NameReference>("Name reference")
                .First();

            return new DeleteCachingPolicyCommand(
                entityType,
                EntityName.FromCode(entityName.Name));
        }

        public override bool Equals(CommandBase? other)
        {
            var otherFunction = other as DeleteCachingPolicyCommand;
            var areEqualed = otherFunction != null
                && otherFunction.EntityType.Equals(EntityType)
                && otherFunction.EntityName.Equals(EntityName);

            return areEqualed;
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();

            builder.Append(".delete ");
            builder.Append(EntityType == EntityType.Table ? "table" : "database");
            builder.Append(" ");
            builder.Append(EntityName.ToScript());
            builder.Append(" policy caching");

            return builder.ToString();
        }
    }
}