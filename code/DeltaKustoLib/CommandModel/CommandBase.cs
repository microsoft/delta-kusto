using Kusto.Language;
using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace DeltaKustoLib.CommandModel
{
    public abstract class CommandBase : IEquatable<CommandBase>
    {
        protected CommandBase(string objectName)
        {
            ObjectName = objectName;
        }

        public static IImmutableList<CommandBase> FromScript(string script)
        {
            var scripts = SplitCommandScripts(script);
            var commands = scripts
                .Select(s => CreateCommand(s, KustoCode.Parse(s)))
                .ToImmutableArray();

            return commands;
        }
        public string ObjectName { get; }

        public abstract string ObjectFriendlyTypeName { get; }

        public abstract bool Equals([AllowNull] CommandBase other);

        public abstract string ToScript();

        #region Object methods
        public override string ToString()
        {
            return ToScript();
        }

        public override bool Equals(object? obj)
        {
            var command = obj as CommandBase;

            return command != null && this.Equals(command);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
        #endregion

        protected string EscapeString(string text)
        {
            return text
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("'", "\\'");
        }

        private static CommandBase CreateCommand(string script, KustoCode code)
        {
            try
            {
                var commandBlock = code.Syntax as CommandBlock;

                if (commandBlock == null)
                {
                    throw new DeltaException("Script isn't a command");
                }

                var customCommand = commandBlock.GetUniqueDescendant<CustomCommand>("custom command");

                //  Show all elements (for debug purposes)
                //var list = new List<SyntaxElement>();

                //commandBlock.WalkElements(e => list.Add(e));

                switch (customCommand.CommandKind)
                {
                    case "CreateFunction":
                    case "CreateOrAlterFunction":
                        return CreateFunctionCommand.FromCode(customCommand);
                    case "DropFunction":
                        return DropFunctionCommand.FromCode(customCommand);

                    default:
                        throw new DeltaException(
                            $"Can't handle CommandKind '{customCommand.CommandKind}'");
                }
            }
            catch (DeltaException ex)
            {
                if (string.IsNullOrWhiteSpace(ex.Script))
                {
                    throw new DeltaException(ex.Message, script, ex);
                }
                else
                {
                    throw;
                }
            }
        }

        private static IEnumerable<string> SplitCommandScripts(string script)
        {
            var lines = script
                .Split('\n')
                .Select(l => l.Trim())
                //  Remove comment lines
                .Where(l => !l.StartsWith("//"));
            var currentCommandLines = new List<string>();

            foreach (var line in lines)
            {
                if (line == string.Empty)
                {
                    if (currentCommandLines.Any())
                    {
                        yield return string.Join('\n', currentCommandLines);
                        currentCommandLines.Clear();
                    }
                }
                else
                {
                    currentCommandLines.Add(line);
                }
            }

            if (currentCommandLines.Any())
            {
                yield return string.Join('\n', currentCommandLines);
            }
        }
    }
}