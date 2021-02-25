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

        public string? Folder { get; }

        public string? DocString { get; }

        public bool? SkipValidation { get; }

        private CreateFunctionCommand(
            string databaseName,
            string functionName,
            string functionBody,
            string? folder,
            string? docString,
            bool? skipValidation)
            : base(databaseName)
        {
            FunctionName = functionName;
            FunctionBody = functionBody;
            Folder = folder;
            DocString = docString;
            SkipValidation = skipValidation;
        }

        public override bool Equals([AllowNull] CommandBase other)
        {
            var otherFunction = other as CreateFunctionCommand;

            return otherFunction != null
                && otherFunction.FunctionName == FunctionName
                && otherFunction.FunctionBody == FunctionBody
                && otherFunction.Folder == Folder
                && otherFunction.DocString == DocString
                && otherFunction.SkipValidation == SkipValidation;
        }

        public override string ToScript()
        {
            var builder = new StringBuilder();
            var properties = new[]
            {
                Folder!=null ? $"folder=\"{Folder}\"" : null,
                DocString!=null ? $"docstring=\"{DocString}\"" : null,
                SkipValidation!=null ? $"skipvalidation=\"{SkipValidation}\"" : null
            };
            var nonEmptyProperties = properties.Where(p => p != null);

            builder.Append(".create-or-alter function ");
            if (nonEmptyProperties.Any())
            {
                builder.Append("with (");
                builder.AppendJoin(", ", nonEmptyProperties);
                builder.Append(") ");
            }
            builder.Append(FunctionName);
            builder.Append(" {");
            builder.AppendLine();
            builder.Append(FunctionBody);
            builder.AppendLine();
            builder.Append("}");

            return builder.ToString();
        }

        internal static CommandBase FromCode(
            string databaseName,
            string script,
            CommandBlock commandBlock)
        {
            var functionNameDeclarations = commandBlock.GetDescendants<NameDeclaration>(
                n => n.NameInParent == "FunctionName");
            var functionBodies = commandBlock.GetDescendants<FunctionBody>();

            if (functionNameDeclarations.Count < 1)
            {
                throw new DeltaException(
                    "There should be at least one function name declaration but there are none",
                    script);
            }
            if (functionBodies.Count != 1)
            {
                throw new DeltaException(
                    $"There should be one function body but there are {functionBodies.Count}",
                    script);
            }

            var functionNameDeclaration = functionNameDeclarations.First();
            var functionName = functionNameDeclaration.Name.SimpleName;
            var functionBody = functionBodies.First();
            var bodyText = script.Substring(
                functionBody.OpenBrace.TextStart + 1,
                functionBody.CloseBrace.TextStart - functionBody.OpenBrace.TextStart - 1);
            var folder = GetPropertyValue(script, "folder", commandBlock);
            var docString = GetPropertyValue(script, "docstring", commandBlock);
            var skipValidation = GetSkipValidation(script, commandBlock);

            return new CreateFunctionCommand(
                databaseName,
                functionName,
                bodyText.Trim(),
                folder,
                docString,
                skipValidation);
        }

        private static bool? GetSkipValidation(string script, CommandBlock commandBlock)
        {
            var skipValidationText = GetPropertyValue(script, "skipvalidation", commandBlock);

            if (skipValidationText == null)
            {
                return null;
            }
            else
            {
                if (skipValidationText.ToLower() != "true"
                && skipValidationText.ToLower() != "false")
                {
                    throw new DeltaException($"skipvalidation must be 'true' or 'false', it can't be '{skipValidationText}'");
                }

                var skipValidation = bool.Parse(skipValidationText);

                return skipValidation;
            }
        }

        private static string? GetPropertyValue(
            string script,
            string propertyName,
            CommandBlock commandBlock)
        {
            var propertyNameDeclaration = commandBlock.GetDescendants<NameDeclaration>(
                n => n.NameInParent == "PropertyName" && n.SimpleName == propertyName)
                .FirstOrDefault();

            if (propertyNameDeclaration == null)
            {
                return null;
            }
            else
            {
                var literalExpression = propertyNameDeclaration
                    .Parent
                    .GetDescendants<LiteralExpression>()
                    .FirstOrDefault();

                if (literalExpression == null)
                {
                    throw new DeltaException(
                        $"Can't find literal expression for {propertyName}",
                        script);
                }

                return (string)literalExpression.LiteralValue;
            }
        }
    }
}