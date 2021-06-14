﻿using DeltaKustoLib;
using DeltaKustoLib.CommandModel;
using DeltaKustoLib.KustoModel;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace DeltaKustoUnitTest.Delta
{
    public class UpdatePolicyTest : ParsingTestBase
    {
        [Fact]
        public void FromEmptyToSomething()
        {
            var currentCommands = new CommandBase[0];
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetCommands = Parse(
                ".create table A (a:int)"
                + "\n\n"
                + ".alter table A policy update @'[{ \"Source\" : \"A\", \"Query\" : \"A\" }]");
            var targetDatabase = DatabaseModel.FromCommands(targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Equal(2, delta.Count);
            Assert.IsType<AlterUpdatePolicyCommand>(delta[1]);

            var policyCommand = (AlterUpdatePolicyCommand)delta[1];
            var policies = policyCommand.UpdatePolicies;

            Assert.Single(policies);
            Assert.Equal("A", policies[0].Source);
        }

        [Fact]
        public void FromSomethingToEmpty()
        {
            var currentCommands = Parse(
                ".create table A (a:int)"
                + "\n\n"
                + ".alter table A policy update @'[{ \"Source\" : \"A\", \"Query\" : \"A\" }]");
            var currentDatabase = DatabaseModel.FromCommands(currentCommands);
            var targetCommands = Parse(".create table A (a:int)");
            var targetDatabase = DatabaseModel.FromCommands(targetCommands);
            var delta = currentDatabase.ComputeDelta(targetDatabase);

            Assert.Single(delta);
            Assert.IsType<AlterUpdatePolicyCommand>(delta[0]);

            var policyCommand = (AlterUpdatePolicyCommand)delta[0];
            var policies = policyCommand.UpdatePolicies;

            Assert.Empty(policies);
        }
    }
}