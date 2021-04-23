using Kusto.Language.Parsing;
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
    /// Models <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/management/alter-column"/>
    /// </summary>
    public class AlterColumnTypeCommand : CommandBase
    {
        #region Inner Types
        public class ColumnDocString : IEquatable<ColumnDocString>
        {
            public EntityName ColumnName { get; }

            public QuotedText DocString { get; }

            public ColumnDocString(EntityName columnName, QuotedText docString)
            {
                ColumnName = columnName;
                DocString = docString;
            }

            public bool Equals([AllowNull] ColumnDocString other)
            {
                return other != null
                    && ColumnName.Equals(other.ColumnName)
                    && DocString.Equals(other.DocString);
            }

            public override string ToString()
            {
                return $"{ColumnName}:{DocString}";
            }
        }
        #endregion

        public EntityName TableName { get; }

        public EntityName ColumnName { get; }

        public string Type { get; }

        public override string CommandFriendlyName => ".alter column type";

        internal AlterColumnTypeCommand(
            EntityName tableName,
            EntityName columnName,
            string type)
        {
            TableName = tableName;
            ColumnName = columnName;
            Type = type;
        }

        internal static CommandBase FromCode(SyntaxElement rootElement)
        {
            var nameReferences = rootElement.GetDescendants<NameReference>();

            if (nameReferences.Count != 2)
            {
                throw new DeltaException($"Expected 2 names but got {nameReferences.Count}");
            }

            var tableName = new EntityName(nameReferences[0].Name.SimpleName);
            var columnName = new EntityName(nameReferences[1].Name.SimpleName);
            var typeExpression = rootElement.GetUniqueDescendant<PrimitiveTypeExpression>("Primitive type");
            var type = typeExpression.Type.ValueText;

            return new AlterColumnTypeCommand(tableName, columnName, type);
        }

        public override bool Equals(CommandBase? other)
        {
            var otherCommand = other as AlterColumnTypeCommand;
            var areEqualed = otherCommand != null
                && otherCommand.TableName.Equals(TableName)
                && otherCommand.ColumnName.Equals(ColumnName)
                && otherCommand.Type.Equals(Type);

            return areEqualed;
        }

        public override string ToScript()
        {
            var builder = new StringBuilder();

            builder.Append(".alter column ");
            builder.Append(TableName);
            builder.Append('.');
            builder.Append(ColumnName);
            builder.Append(" type = ");
            builder.Append(Type);

            return builder.ToString();
        }
    }
}