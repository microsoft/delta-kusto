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
    public class DropFunctionCommand : CommandBase
    {
        public EntityName FunctionName { get; }

        public override string CommandFriendlyName => ".drop function";

        public DropFunctionCommand(EntityName functionName)
        {
            FunctionName = functionName;
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var nameReference = rootElement.GetUniqueDescendant<NameReference>(
                "FunctionName",
                n => n.NameInParent == "FunctionName");

            return new DropFunctionCommand(new EntityName(nameReference.Name.SimpleName));
        }

        public override bool Equals(CommandBase? other)
        {
            var otherFunction = other as DropFunctionCommand;
            var areEqualed = otherFunction != null
                && otherFunction.FunctionName.Equals(FunctionName);

            return areEqualed;
        }

        public override string ToScript()
        {
            return $".drop function {FunctionName}";
        }
    }
}