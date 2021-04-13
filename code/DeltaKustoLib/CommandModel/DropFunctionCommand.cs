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
        public string FunctionName { get; }

        public override string CommandFriendlyName => ".drop function";

        public DropFunctionCommand(string functionName)
        {
            FunctionName = functionName;
        }

        internal static CommandBase FromCode(CustomCommand customCommand)
        {
            var customNode = customCommand.GetUniqueImmediateDescendant<CustomNode>("Custom node");
            var nameReference = customNode.GetUniqueImmediateDescendant<NameReference>("Name reference");

            return new DropFunctionCommand(nameReference.Name.SimpleName);
        }

        public override bool Equals(CommandBase? other)
        {
            var otherFunction = other as CreateFunctionCommand;
            var areEqualed = otherFunction != null
                && otherFunction.FunctionName == FunctionName;

            return areEqualed;
        }

        public override string ToScript()
        {
            return $".drop function ['{FunctionName}']";
        }
    }
}