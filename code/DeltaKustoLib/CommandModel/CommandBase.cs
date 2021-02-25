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
        public string DatabaseName { get; }

        protected CommandBase(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new ArgumentNullException(nameof(databaseName));
            }
            DatabaseName = databaseName;
        }

        public static IImmutableList<CommandBase> FromScript(string databaseName, string script)
        {
            var scripts = SplitCommandScripts(script);
            var commands = scripts
                .Select(s => CreateCommand(databaseName, s, KustoCode.Parse(s)))
                .ToImmutableArray();

            return commands;
        }

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
            return DatabaseName.GetHashCode()
                | base.GetHashCode();
        }
        #endregion

        private static CommandBase CreateCommand(string databaseName, string script, KustoCode code)
        {
            var commandBlock = code.Syntax as CommandBlock;

            if (commandBlock == null)
            {
                throw new DeltaException("Script isn't a command", script);
            }

            var customCommands = commandBlock.GetDescendants<CustomCommand>();

            if (customCommands.Count != 1)
            {
                throw new DeltaException(
                    $"There should be one custom command but there are {customCommands.Count}",
                    script);
            }

            //customCommand.Kind
            //  Show all elements (for debug purposes)
            //var list = new List<SyntaxElement>();

            //commandBlock.WalkElements(e => list.Add(e));

            var customCommand = customCommands.First();

            switch (customCommand.CommandKind)
            {
                case "CreateFunction":
                case "CreateOrAlterFunction":
                    return CreateFunctionCommand.FromCode(databaseName, script, commandBlock);

                default:
                    throw new DeltaException(
                        $"Can't handle CommandKind '{customCommand.CommandKind}'",
                        script);
            }
        }

        private static IEnumerable<string> SplitCommandScripts(string script)
        {
            var lines = script
                .Split('\n')
                .Select(l => l.Trim());
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