using DeltaKustoLib.CommandModel;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
{
    public abstract class ParsingTestBase
    {
        protected IImmutableList<CommandBase> Parse(string script)
        {
            var commands = CommandBase.FromScript("mydb", script);
            var reformedScript = string.Join("\n", commands.Select(c => c.ToScript()));
            var reformedCommands = CommandBase.FromScript("mydb", reformedScript);
            var commandEquality = commands
                .Zip(reformedCommands, (c, rc) => c.Equals(rc));

            //  Make sure we can go from script to command and vise versa without losing anything
            Assert.DoesNotContain(commandEquality, r => r == false);

            return commands;
        }

        protected CommandBase ParseOneCommand(string script)
        {
            var commands = Parse(script);

            Assert.Single(commands);

            return commands.First();
        }
    }
}