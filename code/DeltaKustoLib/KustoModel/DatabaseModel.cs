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

        public string DatabaseName { get; }

        internal IImmutableList<CreateFunctionCommand> FunctionCommands { get; }

        private DatabaseModel(
            string databaseName,
            IEnumerable<CreateFunctionCommand> functionCommands)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new ArgumentNullException(nameof(databaseName));
            }
            DatabaseName = databaseName;
            FunctionCommands = functionCommands.ToImmutableArray();
        }

        public static DatabaseModel FromCommands(
            string databaseName,
            IEnumerable<CommandBase> commands)
        {
            var commandGroups = commands
                .GroupBy(c => c.GetType())
                .ToImmutableDictionary(g => g.Key);

            ValidateCommandTypes(commandGroups.Select(c => c.Key));

            var functions = commandGroups.ContainsKey(typeof(CreateFunctionCommand))
                ? commandGroups[typeof(CreateFunctionCommand)].Cast<CreateFunctionCommand>()
                : new CreateFunctionCommand[0];

            ValidateDuplicates("Functions", functions, f => f.FunctionName);

            return new DatabaseModel(databaseName, functions.ToImmutableArray());
        }

        public static DatabaseModel FromDatabaseSchema(DatabaseSchema databaseSchema)
        {
            var functions = databaseSchema
                .Functions
                .Values
                .Select(s => FromFunctionSchema(s));

            return new DatabaseModel(databaseSchema.Name, functions);
        }

        public IImmutableList<CommandBase> ComputeDelta(DatabaseModel targetModel)
        {
            var functions =
                CreateFunctionCommand.ComputeDelta(FunctionCommands, targetModel.FunctionCommands);
            var deltaCommands = functions;

            return deltaCommands.ToImmutableArray();
        }

        private static CreateFunctionCommand FromFunctionSchema(FunctionSchema schema)
        {
            var parameters = schema
                .InputParameters
                .Select(i => FromParameterSchema(i));

            return new CreateFunctionCommand(
                schema.Name,
                parameters,
                schema.Body,
                schema.Folder,
                schema.DocString,
                true);
        }

        private static TypedParameterModel FromParameterSchema(InputParameterSchema input)
        {
            return input.CslType == null
                ? new TypedParameterModel(
                    input.Name,
                    new TableParameterModel(input.Columns.Select(c => new ColumnModel(c.Name, c.CslType))))
                : new TypedParameterModel(input.Name, input.CslType);
        }

        private static void ValidateCommandTypes(IEnumerable<Type> commandTypes)
        {
            var extraCommandTypes = commandTypes
                .Except(INPUT_COMMANDS)
                .ToArray();

            if (extraCommandTypes.Any())
            {
                throw new DeltaException(
                    "Unsupported command types:  "
                    + $"{string.Join(", ", extraCommandTypes.Select(t => t.Name))}");
            }
        }

        private static void ValidateDuplicates<T>(
            string objectName,
            IEnumerable<T> functions,
            Func<T, string> nameFinder)
        {
            var functionDuplicates = functions
                .GroupBy(f => nameFinder(f))
                .Where(g => g.Count() > 1)
                .Select(g => new { Name = g.Key, Objects = g.ToArray(), Count = g.Count() });

            if (functionDuplicates.Any())
            {
                var duplicateText = string.Join(
                    ", ",
                    functionDuplicates.Select(d => $"(Name = '{d.Name}', Count = {d.Count})"));

                throw new DeltaException($"{objectName} have duplicates:  {{ {duplicateText} }}");
            }
        }
    }
}