using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeltaKustoLib.CommandModel.Policies.Update
{
    /// <summary>
    /// Models <see cref="https://learn.microsoft.com/en-us/azure/data-explorer/kusto/management/delete-table-update-policy-command"/>
    /// </summary>
    [Command(16000, "Delete Update Policies")]
    public class DeleteUpdatePolicyCommand : TableOnlyPolicyCommandBase
    {
        public override string CommandFriendlyName => ".delete table policy update";

        public override string ScriptPath => "tables/policies/update/delete";

        public DeleteUpdatePolicyCommand(EntityName tableName) : base(tableName)
        {
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var entityName = rootElement.GetFirstDescendant<NameReference>();

            return new DeleteUpdatePolicyCommand(EntityName.FromCode(entityName.Name));
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();

            builder.Append(".delete table ");
            builder.Append(TableName.ToScript());
            builder.AppendLine(" policy update");

            return builder.ToString();
        }
    }
}