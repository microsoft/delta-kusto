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
            var functions = new List<CreateFunctionCommand>();

            foreach (var command in commands)
            {
                var function = command as CreateFunctionCommand;

                if (function != null)
                {
                    functions.Add(function);
                }
                else
                {
                    throw new NotSupportedException(
                        $"Command of type {command.GetType().FullName} are currently unsupported");
                }
            }

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
                CreateFunctionCommand.ComputeDelta(FunctionCommands,targetModel.FunctionCommands);
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
    }
}