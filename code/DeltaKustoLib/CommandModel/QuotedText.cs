using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel
{
    public class QuotedText : IComparable<QuotedText>
    {
        static QuotedText()
        {
            Empty = new QuotedText(string.Empty);
        }

        public QuotedText(string text)
        {
            Text = text;
        }

        public static QuotedText FromText(string? text)
        {
            return string.IsNullOrWhiteSpace(text)
                ? Empty
                : new QuotedText(text);
        }

        public static QuotedText FromToken(SyntaxToken token)
        {
            //  Remove surrounding quotes
            var text = token.Text.Substring(1, token.Text.Length - 2);
            var unescapedText = text
                .Replace("\\r", "\r")
                .Replace("\\n", "\n")
                .Replace("\\\"", "\"")
                .Replace("\\\\", "\\");

            return new QuotedText(unescapedText);
        }

        public static QuotedText FromLiteral(LiteralExpression literal)
        {
            return new QuotedText((string)literal.LiteralValue);
        }

        public static QuotedText Empty { get; }

        public string Text { get; }

        public string ToScript()
        {
            var escape = Text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "\\r");

            return $"\"{escape}\"";
        }

        int IComparable<QuotedText>.CompareTo(QuotedText? other)
        {
            return other != null
                ? Text.CompareTo(other.Text)
                : -1;
        }

        #region object methods
        public override string ToString()
        {
            return ToScript();
        }

        public override bool Equals(object? obj)
        {
            var other = obj as QuotedText;

            return other != null
                && other.Text == Text;
        }

        public override int GetHashCode()
        {
            return Text.GetHashCode();
        }
        #endregion
    }
}