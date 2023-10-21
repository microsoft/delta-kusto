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
    /// Models ??? (unexisting documentation)
    /// </summary>
    [Command(20000, "Delete Row Level Security Policies")]
    public class DeleteRowLevelSecurityPolicyCommand : TableOnlyPolicyCommandBase
    {
        public override string CommandFriendlyName => ".delete <entity> policy row_level_security";

        public override string ScriptPath => "tables/policies/row_level_security/delete";

        public DeleteRowLevelSecurityPolicyCommand(EntityName tableName)
            : base(tableName)
        {
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var tableName = rootElement.GetFirstDescendant<NameReference>();

            return new DeleteRowLevelSecurityPolicyCommand(EntityName.FromCode(tableName.Name));
        }

        public override string ToScript(ScriptingContext? context)
        {
            var builder = new StringBuilder();

            builder.Append(".delete table ");
            builder.Append(TableName.ToScript());
            builder.Append(" policy row_level_security");

            return builder.ToString();
        }
    }
}