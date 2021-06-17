using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DeltaKustoLib.CommandModel
{
    internal class DeleteCachingPolicyCommand : CommandBase
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

        public override bool Equals(CommandBase? other)
        {
            throw new NotImplementedException();
        }

        public override string ToScript()
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