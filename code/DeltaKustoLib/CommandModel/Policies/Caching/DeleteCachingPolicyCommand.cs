using Kusto.Language.Syntax;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace DeltaKustoLib.CommandModel.Policies.Caching
{
    [Command(11000, "Delete Caching Policies")]
    public class DeleteCachingPolicyCommand : EntityPolicyCommandBase
    {
        public override string CommandFriendlyName => throw new NotImplementedException();

        public override string ScriptPath => EntityType == EntityType.Database
            ? $"tables/policies/caching/delete"
            : $"db/policies/caching/delete";

        public DeleteCachingPolicyCommand(EntityType entityType, EntityName entityName)
            : base(entityType, entityName)
        {
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var entityType = ExtractEntityType(rootElement);
            var entityName = rootElement
                .GetAtLeastOneDescendant<NameReference>("Name reference")
                .First();

            return new DeleteCachingPolicyCommand(
                entityType,
                EntityName.FromCode(entityName.Name));
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