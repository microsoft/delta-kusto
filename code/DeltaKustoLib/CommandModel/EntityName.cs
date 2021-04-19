using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel
{
    public class EntityName
    {
        public EntityName(string name)
        {
            if(string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException("Entity name can't be null or empty");
            }
            Name = name;
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

        public string ToScript()
        {
            return $"['{Name}']";
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
    }
}