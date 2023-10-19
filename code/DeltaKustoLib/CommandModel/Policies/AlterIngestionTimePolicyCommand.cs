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
    /// Models <see cref="https://learn.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-ingestion-time-policy-command"/>
    /// </summary>
    [Command(21100, "Alter Ingestion Time Policies")]
    public class AlterIngestionTimePolicyCommand : TableOnlyPolicyCommandBase
    {
        public bool IsEnabled { get; }

        public override string CommandFriendlyName => ".alter <entity> policy ingestiontime";

        public override string ScriptPath => $"tables/policies/ingestiontime/create/{TableName}";

        public AlterIngestionTimePolicyCommand(EntityName tableName, bool isEnabled)
            : base(tableName)
        {
            IsEnabled = isEnabled;
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var tableName = rootElement.GetDescendants<NameReference>().Last();

            throw new NotImplementedException();
            //return new AlterIngestionTimePolicyCommand(EntityName.FromCode(tableName.Name), policy);
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();

            builder.Append(".alter table ");
            builder.Append(TableName);
            builder.Append(" policy ingestiontime ");
            builder.AppendLine(IsEnabled.ToString().ToLower());

            return builder.ToString();
        }

        internal static IEnumerable<CommandBase> ComputeDelta(
            AlterAutoDeletePolicyCommand? currentCommand,
            AlterAutoDeletePolicyCommand? targetCommand)
        {
            var hasCurrent = currentCommand != null;
            var hasTarget = targetCommand != null;

            if (hasCurrent && !hasTarget)
            {   //  No target, we remove the current policy
                yield return new DeleteAutoDeletePolicyCommand(currentCommand!.TableName);
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
    }
}