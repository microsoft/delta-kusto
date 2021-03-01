using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace DeltaKustoLib.CommandModel
{
    internal class DropFunctionCommand : CommandBase
    {
        public string FunctionName { get; }

        public DropFunctionCommand(string functionName)
        {
            FunctionName = functionName;
        }

        internal static CommandBase FromCode(
            string databaseName,
            CustomCommand customCommand)
        {
            throw new NotSupportedException(".drop function not supported in this context");
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