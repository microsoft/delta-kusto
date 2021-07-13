using Kusto.Language.Syntax;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace DeltaKustoLib.CommandModel.Policies
{
    [CommandTypeOrder(11000, "Delete Caching Policies")]
    public class DeleteCachingPolicyCommand : EntityPolicyCommandBase
    {
        public override string CommandFriendlyName => throw new NotImplementedException();

        public override string ScriptPath => EntityType == EntityType.Database
            ? $"tables/policies/caching/delete/{EntityName}"
            : $"db/policies/delete";

        public DeleteCachingPolicyCommand(EntityType entityType, EntityName entityName)
            : base(entityType, entityName)
        {
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
            if (EntityType == EntityType.Database && context?.CurrentDatabaseName != null)
            {
                builder.Append(context.CurrentDatabaseName.ToScript());
            }
            else
            {
                builder.Append(EntityName.ToScript());
            }
            builder.Append(" policy caching");

            return builder.ToString();
        }
    }
}