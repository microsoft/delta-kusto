using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace DeltaKustoLib.CommandModel
{
    public class CreateFunctionCommand : CommandBase
    {
        public string FunctionName { get; }

        public string FunctionBody { get; }

        private CreateFunctionCommand(
            string databaseName,
            string functionName,
            string functionBody)
            : base(databaseName)
        {
            FunctionName = functionName;
            FunctionBody = functionBody;
        }

        public override bool Equals([AllowNull] CommandBase other)
        {
            var otherFunction = other as CreateFunctionCommand;

            return otherFunction != null
                && otherFunction.FunctionName == FunctionName
                && otherFunction.FunctionBody == FunctionBody;
        }

        public override string ToScript()
        {
            return $".create-or-alter function {FunctionName} {{ {FunctionBody} }}";
        }

        internal static CommandBase FromCode(
            string databaseName,
            string script,
            CommandBlock commandBlock)
        {
            var nameDeclarations = commandBlock.GetDescendants<NameDeclaration>();
            var functionBodies = commandBlock.GetDescendants<FunctionBody>();

            if (nameDeclarations.Count < 1)
            {
                throw new DeltaException(
                    "There should be at least one name declaration but there are none",
                    script);
            }
            if (functionBodies.Count != 1)
            {
                throw new DeltaException(
                    $"There should be one function body but there are {functionBodies.Count}",
                    script);
            }

            var nameDeclaration = nameDeclarations.First();
            var functionName = nameDeclaration.Name.SimpleName;
            var functionBody = functionBodies.First();
            var bodyText = script.Substring(
                functionBody.OpenBrace.TextStart + 1,
                functionBody.CloseBrace.TextStart - functionBody.OpenBrace.TextStart -1 );

            return new CreateFunctionCommand(
                databaseName,
                functionName,
                bodyText.Trim());
        }
    }
}