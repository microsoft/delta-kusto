using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace DeltaKustoLib.CommandModel
{
    /// <summary>
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/drop-function"/>
    /// </summary>
    [Command(500, "Drop functions")]
    public class DropFunctionsCommand : CommandBase
    {
        public IImmutableList<EntityName> FunctionNames { get; }

        public override string CommandFriendlyName => ".drop function";

        public override string SortIndex => FunctionNames.First().Name;

        public override string ScriptPath => $"functions/drop";
        
        internal DropFunctionsCommand(IImmutableList<EntityName> functionNames)
        {
            if(!functionNames.Any())
            {
                throw new ArgumentException("Empty", nameof(functionNames));
            }
            FunctionNames = functionNames;
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var nameReferences = rootElement.GetDescendants<NameReference>();
            var names = nameReferences
                .Select(n => new EntityName(n.Name.SimpleName))
                .ToImmutableArray();

            return new DropFunctionsCommand(names);
        }

        public override bool Equals(CommandBase? other)
        {
            var otherTables = other as DropFunctionsCommand;
            var areEqualed = otherTables != null
                && otherTables.FunctionNames.ToHashSet().SetEquals(FunctionNames);

            return areEqualed;
        }

        public override string ToScript(ScriptingContext? context)
        {
            return $".drop functions ({string.Join(", ", FunctionNames)})";
        }
    }
}