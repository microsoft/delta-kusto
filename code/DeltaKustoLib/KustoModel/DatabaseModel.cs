using DeltaKustoLib.CommandModel;
using DeltaKustoLib.SchemaObjects;
using Kusto.Language.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace DeltaKustoLib.KustoModel
{
    public class DatabaseModel
    {
        private static readonly IImmutableSet<Type> INPUT_COMMANDS = new[]
        {
            typeof(CreateFunctionCommand)
        }.ToImmutableHashSet();

        private IImmutableList<CreateFunctionCommand> _functionCommands;

        private DatabaseModel(
            IEnumerable<CreateFunctionCommand> functionCommands)
        {
            _functionCommands = functionCommands.ToImmutableArray();
        }

        public static DatabaseModel FromCommands(
            IEnumerable<CommandBase> commands)
        {
            ValidateCommandTypes(commands.Select(c => (c.GetType(), c.ObjectFriendlyTypeName)).Distinct());

            var functions = commands
                .OfType<CreateFunctionCommand>()
                .ToImmutableArray();

            ValidateDuplicates("Functions", functions);

            return new DatabaseModel(functions);
        }

        public static DatabaseModel FromDatabaseSchema(DatabaseSchema databaseSchema)
        {
            var functions = databaseSchema
                .Functions
                .Values
                .Select(s => CreateFunctionCommand.FromFunctionSchema(s));

            return new DatabaseModel(functions);
        }

        public IImmutableList<CommandBase> ComputeDelta(DatabaseModel targetModel)
        {
            var functions =
                CreateFunctionCommand.ComputeDelta(_functionCommands, targetModel._functionCommands);
            var deltaCommands = functions;

            return deltaCommands.ToImmutableArray();
        }

        private static void ValidateCommandTypes(IEnumerable<(Type type, string friendlyName)> commandTypes)
        {
            var extraCommandTypes = commandTypes
                .Select(p => p.type)
                .Except(INPUT_COMMANDS);

            if (extraCommandTypes.Any())
            {
                var typeToNameMap = commandTypes
                    .ToImmutableDictionary(p => p.type, p => p.friendlyName);

                throw new DeltaException(
                    "Unsupported command types:  "
                    + $"{string.Join(", ", extraCommandTypes.Select(t => typeToNameMap[t]))}");
            }
        }

        private static void ValidateDuplicates<T>(
            string friendlyObjectType,
            IEnumerable<T> dbObjects)
            where T : CommandBase
        {
            var functionDuplicates = dbObjects
                .GroupBy(o => o.ObjectName)
                .Where(g => g.Count() > 1)
                .Select(g => new { Name = g.Key, Objects = g.ToArray(), Count = g.Count() });

            if (functionDuplicates.Any())
            {
                var duplicateText = string.Join(
                    ", ",
                    functionDuplicates.Select(d => $"(Name = '{d.Name}', Count = {d.Count})"));

                throw new DeltaException(
                    $"{friendlyObjectType} have duplicates:  {{ {duplicateText} }}");
            }
        }
    }
}