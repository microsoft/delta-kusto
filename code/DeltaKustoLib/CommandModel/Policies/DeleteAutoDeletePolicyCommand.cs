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
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/auto-delete-policy-command#delete-policy"/>
    /// </summary>
    public class DeleteAutoDeletePolicyCommand : CommandBase
    {
        public EntityName TableName { get; }

        public override string CommandFriendlyName => ".delete <entity> policy auto_delete";

        public DeleteAutoDeletePolicyCommand(EntityName tableName)
        {
            TableName = tableName;
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var entityName = rootElement.GetFirstDescendant<NameReference>();

            return new DeleteAutoDeletePolicyCommand(EntityName.FromCode(entityName.Name));
        }

        public override bool Equals(CommandBase? other)
        {
            var otherFunction = other as DeleteAutoDeletePolicyCommand;
            var areEqualed = otherFunction != null
                && otherFunction.TableName.Equals(TableName);

            return areEqualed;
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();

            builder.Append(".delete table ");
            builder.Append(TableName.ToScript());
            builder.Append(" policy auto_delete");

            return builder.ToString();
        }
    }
}