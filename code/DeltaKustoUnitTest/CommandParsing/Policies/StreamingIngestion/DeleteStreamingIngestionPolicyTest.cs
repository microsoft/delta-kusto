﻿using DeltaKustoLib.CommandModel;
using DeltaKustoLib.CommandModel.Policies;
using DeltaKustoLib.CommandModel.Policies.StreamingIngestion;
using System;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing.Policies.StreamingIngestion
{
    public class DeleteStreamingIngestionPolicyTest : ParsingTestBase
    {
        [Fact]
        public void SimpleTable()
        {
            TestStreamingIngestionPolicy(EntityType.Table, "A");
        }

        [Fact]
        public void FunkyTable()
        {
            TestStreamingIngestionPolicy(EntityType.Table, "A- 1");
        }

        [Fact]
        public void SimpleDatabase()
        {
            TestStreamingIngestionPolicy(EntityType.Database, "Db");
        }

        [Fact]
        public void FunkyDatabase()
        {
            TestStreamingIngestionPolicy(EntityType.Database, "db.mine");
        }

        private void TestStreamingIngestionPolicy(EntityType type, string name)
        {
            var commandText = new DeleteStreamingIngestionPolicyCommand(type, new EntityName(name))
                .ToScript(null);
            var command = ParseOneCommand(commandText);

            Assert.IsType<DeleteStreamingIngestionPolicyCommand>(command);

            var realCommand = (DeleteStreamingIngestionPolicyCommand)command;

            Assert.Equal(type, realCommand.EntityType);
            Assert.Equal(name, realCommand.EntityName.Name);
        }
    }
}