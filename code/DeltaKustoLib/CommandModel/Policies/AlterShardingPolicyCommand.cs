using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel.Policies
{
    /// <summary>
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/sharding-policy#alter-policy"/>
    /// </summary>
    [Command(15100, "Alter Sharding Policies")]
    public class AlterShardingPolicyCommand : EntityPolicyCommandBase
    {
        private static readonly ShardingPolicySerializerContext _serializerContext
            = new ShardingPolicySerializerContext(
                new JsonSerializerOptions
                {
                    WriteIndented = true
                });

        public int? MaxRowCount { get; }

        public int? MaxExtentSizeInMb { get; }

        public int? MaxOriginalSizeInMb { get; }

        public override string CommandFriendlyName => ".alter <entity> policy sharding";

        public override string ScriptPath => EntityType == EntityType.Database
            ? $"tables/policies/sharding/create/{EntityName}"
            : $"databases/policies/sharding/create";

        public AlterShardingPolicyCommand(
            EntityType entityType,
            EntityName entityName,
            int? maxRowCount,
            int? maxExtentSizeInMb,
            int? maxOriginalSizeInMb)
            : base(
                  entityType,
                  entityName,
                  CreatePolicyText(maxRowCount, maxExtentSizeInMb, maxOriginalSizeInMb))
        {
            MaxRowCount = maxRowCount;
            MaxExtentSizeInMb = maxExtentSizeInMb;
            MaxOriginalSizeInMb = maxOriginalSizeInMb;
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var entityType = ExtractEntityType(rootElement);
            var entityName = rootElement.GetDescendants<NameReference>().Last();
            var policyText = QuotedText.FromLiteral(
                rootElement.GetUniqueDescendant<LiteralExpression>(
                    "Sharding",
                    e => e.NameInParent == "ShardingPolicy"));

            return CreateFromPolicyText(entityType, entityName, policyText);
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();
            var policy = new
            {
                MaxRowCount = MaxRowCount,
                MaxExtentSizeInMb = MaxExtentSizeInMb,
                MaxOriginalSizeInMb = MaxOriginalSizeInMb
            };

            builder.Append(".alter ");
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
            builder.AppendLine(" policy sharding");
            builder.AppendLine("```");
            builder.AppendLine(SerializePolicy());
            builder.AppendLine("```");

            return builder.ToString();
        }

        internal static IEnumerable<CommandBase> ComputeDelta(
            AlterShardingPolicyCommand? currentCommand,
            AlterShardingPolicyCommand? targetCommand)
        {
            var hasCurrent = currentCommand != null;
            var hasTarget = targetCommand != null;

            if (hasCurrent && !hasTarget)
            {   //  No target, we remove the current policy
                yield return new DeleteShardingPolicyCommand(
                    currentCommand!.EntityType,
                    currentCommand!.EntityName);
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

        private static JsonDocument CreatePolicyText(
            int? maxRowCount,
            int? maxExtentSizeInMb,
            int? maxOriginalSizeInMb)
        {
            var map = new Dictionary<string, int>();

            if (maxRowCount != null)
            {
                map[nameof(maxRowCount)] = maxRowCount.Value;
            }
            if (maxExtentSizeInMb != null)
            {
                map[nameof(maxExtentSizeInMb)] = maxExtentSizeInMb.Value;
            }
            if (maxOriginalSizeInMb != null)
            {
                map[nameof(maxOriginalSizeInMb)] = maxOriginalSizeInMb.Value;
            }

            var text = Serialize(map, _serializerContext);
            var doc = Deserialize<JsonDocument>(text);

            return doc!;
        }

        private static CommandBase CreateFromPolicyText(
            EntityType entityType,
            NameReference entityName,
            QuotedText policyText)
        {
            var policy = Deserialize<JsonDocument>(policyText.Text);

            if (policy == null)
            {
                throw new DeltaException(
                    $"Can't extract policy objects from {policyText.ToScript()}");
            }
            else
            {
                int? maxRowCount = null;
                int? maxExtentSizeInMb = null;
                int? maxOriginalSizeInMb = null;
                var validProperties = new[]
                {
                    (Name:nameof(maxRowCount), Action:(Action<int>)((value) => maxRowCount = value)),
                    (Name:nameof(maxExtentSizeInMb), Action:(Action<int>)((value) => maxExtentSizeInMb = value)),
                    (Name:nameof(maxOriginalSizeInMb), Action:(Action<int>)((value) => maxOriginalSizeInMb = value))
                };

                foreach (var property in policy.RootElement.EnumerateObject())
                {
                    foreach (var validProperty in validProperties)
                    {
                        if (property.Name.Equals(
                            validProperty.Name,
                            StringComparison.InvariantCultureIgnoreCase)
                            && property.Value.ValueKind != JsonValueKind.Null)
                        {
                            int value;

                            if (property.Value.TryGetInt32(out value))
                            {
                                validProperty.Action(value);
                            }
                            else
                            {
                                throw new DeltaException($"{validProperty.Name} should be an integer but is "
                                    + $"a {property.Value.ValueKind}");
                            }
                        }
                    }
                }

                return new AlterShardingPolicyCommand(
                    entityType,
                    EntityName.FromCode(entityName.Name),
                    maxRowCount,
                    maxExtentSizeInMb,
                    maxOriginalSizeInMb);
            }
        }
    }

    [JsonSerializable(typeof(Dictionary<string, int>))]
    internal partial class ShardingPolicySerializerContext : JsonSerializerContext
    {
    }
}