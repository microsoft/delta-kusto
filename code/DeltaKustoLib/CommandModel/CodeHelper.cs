using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography;
using System.Text;

namespace DeltaKustoLib.CommandModel
{
    internal static class CodeHelper
    {
        public static IEnumerable<TElement> GetAtLeastOneDescendant<TElement>(
            this SyntaxElement parent,
            string descendantNameForExceptionMessage,
            Func<TElement, bool>? predicate = null)
            where TElement : SyntaxElement
        {
            var descendants = parent.GetDescendants<TElement>(predicate);

            if (!descendants.Any())
            {
                throw new DeltaException(
                    $"There should be at least one {descendantNameForExceptionMessage} but there are none",
                    parent.Root.ToString(IncludeTrivia.All));
            }

            return descendants;
        }

        public static TElement GetUniqueDescendant<TElement>(
            this SyntaxElement parent,
            string descendantNameForExceptionMessage,
            Func<TElement, bool>? predicate = null)
            where TElement : SyntaxElement
        {
            var descendants = parent.GetDescendants<TElement>(predicate);

            if (descendants.Count != 1)
            {
                throw new DeltaException(
                    $"There should be one-and-only-one {descendantNameForExceptionMessage} but there are {descendants.Count}",
                    parent.Root.ToString(IncludeTrivia.All));
            }

            return descendants.First();
        }

        public static TElement? GetAtMostOneDescendant<TElement>(
            this SyntaxElement parent,
            string descendantNameForExceptionMessage,
            Func<TElement, bool>? predicate = null)
            where TElement : SyntaxElement
        {
            var descendants = parent.GetDescendants<TElement>(predicate);

            if (descendants.Count > 1)
            {
                throw new DeltaException(
                    $"There should be at most one {descendantNameForExceptionMessage} but there are {descendants.Count}",
                    parent.Root.ToString(IncludeTrivia.All));
            }

            return descendants.FirstOrDefault();
        }

        public static TElement GetUniqueImmediateDescendant<TElement>(
            this SyntaxElement parent,
            string descendantNameForExceptionMessage,
            Func<TElement, bool>? predicate = null)
            where TElement : SyntaxElement
        {
            var descendants = parent.GetDescendants<TElement>(
                e => e.Parent == parent
                && (predicate == null || predicate(e)));

            if (descendants.Count != 1)
            {
                throw new DeltaException(
                    $"There should be one-and-only-one {descendantNameForExceptionMessage} but there are {descendants.Count}",
                    parent.Root.ToString(IncludeTrivia.All));
            }

            return descendants.First();
        }

        public static IReadOnlyList<TElement> GetImmediateDescendants<TElement>(
            this SyntaxElement parent,
            Func<TElement, bool>? predicate = null)
            where TElement : SyntaxElement
        {
            var descendants = parent.GetDescendants<TElement>(
                e => e.Parent == parent
                && (predicate == null || predicate(e)));

            return descendants;
        }

        #region Extract Children
        public static (C1, C2) ExtractChildren<C1, C2>(
            this IReadOnlyList<SyntaxElement> children,
            string childrenNameForExceptionMessage)
            where C1 : SyntaxElement
            where C2 : SyntaxElement
        {
            if (children.Count != 2)
            {
                throw new DeltaException(
                    $"Expected 2 children in '{childrenNameForExceptionMessage}' "
                    + $"but found {children.Count}");
            }
            var child1 = children[0] as C1;
            var child2 = children[1] as C2;

            if (child1 == null)
            {
                throw new DeltaException(
                    $"First child in '{childrenNameForExceptionMessage}' "
                    + "has unexpected type",
                    children[0].Root.ToString(IncludeTrivia.All));
            }
            if (child2 == null)
            {
                throw new DeltaException(
                    $"Second child in '{childrenNameForExceptionMessage}' "
                    + "has unexpected type",
                    children[1].Root.ToString(IncludeTrivia.All));
            }

            return (child1, child2);
        }

        public static (C1, C2, C3) ExtractChildren<C1, C2, C3>(
            this IReadOnlyList<SyntaxElement> children,
            string childrenNameForExceptionMessage)
            where C1 : SyntaxElement
            where C2 : SyntaxElement
            where C3 : SyntaxElement
        {
            if (children.Count != 3)
            {
                throw new DeltaException(
                    $"Expected 3 children in '{childrenNameForExceptionMessage}' "
                    + "but found {children.Count}");
            }
            var child1 = children[0] as C1;
            var child2 = children[1] as C2;
            var child3 = children[2] as C3;

            if (child1 == null)
            {
                throw new DeltaException(
                    $"First child in '{childrenNameForExceptionMessage}' "
                    + "has unexpected type",
                    children[0].Root.ToString(IncludeTrivia.All));
            }
            if (child2 == null)
            {
                throw new DeltaException(
                    $"Second child in '{childrenNameForExceptionMessage}' "
                    + "has unexpected type",
                    children[1].Root.ToString(IncludeTrivia.All));
            }
            if (child3 == null)
            {
                throw new DeltaException(
                    $"Third child in '{childrenNameForExceptionMessage}' "
                    + "has unexpected type",
                    children[2].Root.ToString(IncludeTrivia.All));
            }

            return (child1, child2, child3);
        }

        public static (C1, C2, C3, C4) ExtractChildren<C1, C2, C3, C4>(
            this IReadOnlyList<SyntaxElement> children,
            string childrenNameForExceptionMessage)
            where C1 : SyntaxElement
            where C2 : SyntaxElement
            where C3 : SyntaxElement
            where C4 : SyntaxElement
        {
            if (children.Count != 4)
            {
                throw new DeltaException(
                    $"Expected 4 children in '{childrenNameForExceptionMessage}' "
                    + "but found {children.Count}");
            }
            var child1 = children[0] as C1;
            var child2 = children[1] as C2;
            var child3 = children[2] as C3;
            var child4 = children[3] as C4;

            if (child1 == null)
            {
                throw new DeltaException(
                    $"First child in '{childrenNameForExceptionMessage}' "
                    + "has unexpected type",
                    children[0].Root.ToString(IncludeTrivia.All));
            }
            if (child2 == null)
            {
                throw new DeltaException(
                    $"Second child in '{childrenNameForExceptionMessage}' "
                    + "has unexpected type",
                    children[1].Root.ToString(IncludeTrivia.All));
            }
            if (child3 == null)
            {
                throw new DeltaException(
                    $"Third child in '{childrenNameForExceptionMessage}' "
                    + "has unexpected type",
                    children[2].Root.ToString(IncludeTrivia.All));
            }
            if (child4 == null)
            {
                throw new DeltaException(
                    $"Third child in '{childrenNameForExceptionMessage}' "
                    + "has unexpected type",
                    children[3].Root.ToString(IncludeTrivia.All));
            }

            return (child1, child2, child3, child4);
        }
        #endregion
    }
}