using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel.Policies
{
    public abstract class EntityPolicyCommandBase : PolicyCommandBase
    {
        public EntityType EntityType { get; }

        public EntityName EntityName { get; }

        public EntityPolicyCommandBase(
            EntityType entityType,
            EntityName entityName,
            JsonDocument policy)
            : base(policy)
        {
            if (entityType != EntityType.Database && entityType != EntityType.Table)
            {
                throw new NotSupportedException(
                    $"Entity type {entityType} isn't supported in this context");
            }
            EntityType = entityType;
            EntityName = entityName;
        }

        public EntityPolicyCommandBase(EntityType entityType, EntityName entityName)
            : this(entityType, entityName, ToJsonDocument(new object()))
        {
        }

        public override string SortIndex =>
            $"{(EntityType == EntityType.Database ? 0 : 1)}_{EntityName}";

        public override bool Equals(CommandBase? other)
        {
            var otherPolicy = other as EntityPolicyCommandBase;
            var areEqualed = otherPolicy != null
                && base.Equals(other)
                && otherPolicy.EntityType.Equals(EntityType)
                && (EntityType == EntityType.Database || otherPolicy.EntityName.Equals(EntityName));

            return areEqualed;
        }

        protected static EntityType ExtractEntityType(SyntaxElement rootElement)
        {
            var dbEntityType = rootElement.GetAtMostOneDescendant<SyntaxToken>(
                "database",
                t => t.Kind == SyntaxKind.DatabaseKeyword);
            var tableEntityType = rootElement.GetAtMostOneDescendant<SyntaxToken>(
                "table",
                t => t.Kind == SyntaxKind.IdentifierToken && t.Text.ToLower() == "table");
            var entityType = dbEntityType != null
                ? EntityType.Database
                : tableEntityType != null
                ? EntityType.Table
                : throw new DeltaException("Can't figure out entity type");

            return entityType;
        }
    }
}