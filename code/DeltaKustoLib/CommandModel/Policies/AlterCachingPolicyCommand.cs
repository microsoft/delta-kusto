using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel.Policies
{
    /// <summary>
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/cache-policy#altering-the-cache-policy"/>
    /// </summary>
    [Command(11100, "Alter Caching Policies")]
    public class AlterCachingPolicyCommand : EntityPolicyCommandBase
    {
        public KustoTimeSpan HotData { get; }

        public KustoTimeSpan HotIndex { get; }

        public override string CommandFriendlyName => ".alter <entity> policy caching";

        public override string ScriptPath => EntityType == EntityType.Database
            ? $"tables/policies/caching/create/{EntityName}"
            : $"databases/policies/caching/create";

        public AlterCachingPolicyCommand(
            EntityType entityType,
            EntityName entityName,
            TimeSpan hotData,
            TimeSpan hotIndex) : base(entityType, entityName)
        {
            HotData = new KustoTimeSpan(hotData);
            HotIndex = new KustoTimeSpan(hotIndex);
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            //var entityKinds = rootElement
            //    .GetDescendants<SyntaxElement>(s => s.Kind == SyntaxKind.TableKeyword
            //    || s.Kind == SyntaxKind.DatabaseKeyword)
            //    .Select(s => s.Kind);

            //if (!entityKinds.Any())
            //{
            //    throw new DeltaException("Alter caching policy requires to act on a table or database (cluster isn't supported)");
            //}
            //var entityKind = entityKinds.First();
            //var entityType = entityKind == SyntaxKind.TableKeyword
            //    ? EntityType.Table
            //    : EntityType.Database;
            //var entityName = rootElement
            //    .GetDescendants<NameReference>(n => n.NameInParent == "TableName"
            //    || n.NameInParent == "DatabaseName"
            //    || n.NameInParent == "Selector")
            //    .Last();
            //var (hotData, hotIndex) = ExtractHotDurations(rootElement);

            //return new AlterCachingPolicyCommand(
            //    entityType,
            //    EntityName.FromCode(entityName.Name),
            //    hotData,
            //    hotIndex);
            throw new NotImplementedException();
        }

        public override bool Equals(CommandBase? other)
        {
            var otherPolicy = other as AlterCachingPolicyCommand;
            var areEqualed = otherPolicy != null
                && base.Equals(otherPolicy)
                && otherPolicy.HotData.Equals(HotData)
                && otherPolicy.HotIndex.Equals(HotIndex);

            return areEqualed;
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();

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
            builder.Append(" policy caching ");
            if (HotData.Equals(HotIndex))
            {
                builder.Append("hot = ");
                builder.Append(HotData);
            }
            else
            {
                builder.Append("hotdata = ");
                builder.Append(HotData);
                builder.Append(" hotindex = ");
                builder.Append(HotIndex);
            }

            return builder.ToString();
        }

        internal static IEnumerable<CommandBase> ComputeDelta(
            AlterCachingPolicyCommand? currentCommand,
            AlterCachingPolicyCommand? targetCommand)
        {
            var hasCurrent = currentCommand != null;
            var hasTarget = targetCommand != null;

            if (hasCurrent && !hasTarget)
            {   //  No target, we remove the current policy
                yield return new DeleteCachingPolicyCommand(
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

        private static (TimeSpan hotData, TimeSpan hotIndex) ExtractHotDurations(
            SyntaxElement rootElement)
        {
            var durations = GetHotDurations(rootElement);

            //if (durations.Count == 1 && durations.ContainsKey(SyntaxKind.HotKeyword))
            //{
            //    var duration = durations.First().Value;

            //    return (duration, duration);
            //}
            //else if (durations.Count == 2
            //    && durations.ContainsKey(SyntaxKind.HotDataKeyword)
            //    && durations.ContainsKey(SyntaxKind.HotIndexKeyword))
            //{
            //    var dataDuration = durations[SyntaxKind.HotDataKeyword];
            //    var indexDuration = durations[SyntaxKind.HotIndexKeyword];

            //    return (dataDuration, indexDuration);
            //}
            //else
            //{
            //    throw new DeltaException("Caching policy expect either a 'hot' parameter or a 'hotdata' and 'hotindex'");
            //}
            throw new NotImplementedException();
        }

        private static IImmutableDictionary<SyntaxKind, TimeSpan> GetHotDurations(SyntaxElement rootElement)
        {
            var elements = rootElement.GetDescendants<SyntaxElement>();
            var builder = ImmutableDictionary<SyntaxKind, TimeSpan>.Empty.ToBuilder();
            //SyntaxKind? kind = null;

            //foreach (var e in elements)
            //{
                //if (kind == null)
                //{
                //    if (e.Kind == SyntaxKind.HotKeyword
                //        || e.Kind == SyntaxKind.HotDataKeyword
                //        || e.Kind == SyntaxKind.HotIndexKeyword)
                //    {
                //        if (!e.IsMissing)
                //        {
                //            kind = e.Kind;
                //        }
                //    }
                //}
                //else
                //{
                //    if (e.Kind == SyntaxKind.TimespanLiteralToken)
                //    {
                //        var token = (SyntaxToken)e;
                //        var t = (TimeSpan)token.Value;

                //        builder.Add(kind.Value, t);
                //        kind = null;
                //    }
                //}
            //}

            //return builder.ToImmutable();
                throw new NotImplementedException();
        }
    }
}