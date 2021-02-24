using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaKustoLib.CommandModel
{
    public abstract class CommandBase
    {
        public string DatabaseName { get; }

        private CommandBase(string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new ArgumentNullException(nameof(databaseName));
            }
            DatabaseName = databaseName;
        }

        public static CommandBase FromScript(string databaseName, string script)
        {
            throw new NotImplementedException();
        }
    }
}