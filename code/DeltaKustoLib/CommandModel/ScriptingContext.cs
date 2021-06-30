using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel
{
    public record ScriptingContext
    {
        public EntityName? CurrentDatabaseName { get; init; } = null;
    }
}