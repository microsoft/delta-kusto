using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal class CommandAttribute : Attribute
    {
        public CommandAttribute(int order, string headerComment)
        {
            Order = order;
            HeaderComment = headerComment;
        }

        public int Order { get; }

        public string HeaderComment { get; }
    }
}