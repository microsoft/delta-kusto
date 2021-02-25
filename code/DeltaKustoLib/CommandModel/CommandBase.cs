using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace DeltaKustoLib.CommandModel
{
    public abstract class CommandBase : IEquatable<CommandBase>
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

        public static IImmutableList<CommandBase> FromScript(string databaseName, string script)
        {
            throw new NotImplementedException();
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
    }
}