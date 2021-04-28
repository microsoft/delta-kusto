using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel
{
    /// <summary>
    /// Model an entity name in Kusto (cf
    /// <see cref="https://docs.microsoft.com/en-us/azure/data-explorer/kusto/query/schema-entities/entity-names"/>).
    /// </summary>
    public class EntityName : IComparable<EntityName>
    {
        private readonly char[] SPECIAL_CHARACTERS = new[] { ' ', '.', '-', '"' };

        public EntityName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("Entity name can't be null or empty");
            }
            Name = name;

            foreach (var c in name)
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                {
                    //  Nothing special
                }
                else if (SPECIAL_CHARACTERS.Contains(c))
                {
                    NeedEscape = true;
                }
                else
                {
                    throw new DeltaException($"Unsuppored character for an entity:  '{c}'");
                }
            }
        }

        public static EntityName FromCode(SyntaxElement element)
        {
            switch (element)
            {
                case NameDeclaration nameDeclaration:
                    return new EntityName(nameDeclaration.Name.SimpleName);
                case TokenName tokenName:
                    return new EntityName(tokenName.Name.Text);
                case LiteralExpression literal:
                    return new EntityName((string)literal.LiteralValue);

                default:
                    return new EntityName(element.ToString());
            }
        }

        public string Name { get; }

        public bool NeedEscape { get; }

        public string ToScript()
        {
            return NeedEscape
                ? $"['{EscapeName()}']"
                : Name;
        }

        int IComparable<EntityName>.CompareTo(EntityName? other)
        {
            return other != null
                ? Name.CompareTo(other.Name)
                : -1;
        }

        #region object methods
        public override string ToString()
        {
            return ToScript();
        }

        public override bool Equals(object? obj)
        {
            var other = obj as EntityName;

            return other != null
                && other.Name == Name;
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }
        #endregion

        private string EscapeName()
        {
            var escapedName = Name
                .Replace("\"", "\\\"");

            return escapedName;
        }
    }
}