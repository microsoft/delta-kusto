using System;
using System.Collections.Generic;
using System.Text;

namespace DeltaKustoLib.SchemaObjects
{
    public class RootSchema
    {
        public DatabaseSchema[] Database { get; set; } = new DatabaseSchema[0];
    }
}