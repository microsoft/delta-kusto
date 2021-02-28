using DeltaKustoLib.CommandModel;
using DeltaKustoLib.SchemaObjects;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace DeltaKustoLib.SchemaModel
{
    public class DatabaseModel
    {
        public string DatabaseName { get; }

        public IImmutableList<CreateFunctionCommand> FunctionCommands { get; }

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
            throw new NotImplementedException();
        }

        public IImmutableList<CommandBase> ComputeDelta(DatabaseModel targetModel)
        {
            throw new NotImplementedException();
        }
    }
}