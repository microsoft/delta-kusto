using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal class CommandTypeOrderAttribute : Attribute
    {
        public CommandTypeOrderAttribute(int order)
        {
            Order = order;
        }

        public int Order { get; }
    }
}