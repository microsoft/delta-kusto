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
    /// Models <see cref="https://learn.microsoft.com/en-us/azure/data-explorer/kusto/management/delete-table-restricted-view-access-policy-command"/>
    /// </summary>
    [Command(19000, "Delete restricted view Policies")]
    public class DeleteRestrictedViewPolicyCommand : TableOnlyPolicyCommandBase
    {
        public override string CommandFriendlyName => ".delete table policy restricted_view_access";

        public override string ScriptPath => "tables/policies/restricted_view_access/delete";

        public DeleteRestrictedViewPolicyCommand(EntityName tableName) : base(tableName)
        {
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var entityName = rootElement.GetFirstDescendant<NameReference>();

            return new DeleteRestrictedViewPolicyCommand(EntityName.FromCode(entityName.Name));
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();

            builder.Append(".delete table ");
            builder.Append(TableName.ToScript());
            builder.Append(" policy restricted_view_access");

            return builder.ToString();
        }
    }
}