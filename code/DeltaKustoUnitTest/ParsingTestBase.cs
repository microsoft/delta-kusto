using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Xunit;

namespace DeltaKustoUnitTest
{
    public abstract class ParsingTestBase : TestBase
    {
        protected IImmutableList<CommandBase> Parse(string script)
        {
            var commands = CommandBase.FromScript(script);
            var reformedScript = string.Join("\n\n", commands.Select(c => c.ToScript()));
            var reformedCommands = CommandBase.FromScript(reformedScript);

            //  Make sure we can go from script to command and vise versa without losing anything
            Assert.Equal(commands, reformedCommands);

            return commands;
        }

        protected CommandBase ParseOneCommand(string script)
        {
            var commands = Parse(script);

            Assert.Single(commands);

            return commands.First();
        }

        protected static string GetActualTableName(string tableName)
        {
            var actualTableName = tableName.Split('.').Last();

            if (actualTableName.StartsWith('['))
            {
                return actualTableName.Substring(2, actualTableName.Length - 4);
            }
            else
            {
                return actualTableName;
            }
        }
    }
}