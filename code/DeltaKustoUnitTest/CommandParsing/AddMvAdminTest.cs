using DeltaKustoLib.CommandModel;
using System;
using System.Linq;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
{
    public class AddMvAdminTest : ParsingTestBase
    {
        public void AddMvAdmin()
        {
            var commands = Parse(
                ".add materialized view SampleView admins ('aaduser=imikeoein@fabrikam.com')");

            Assert.Empty(commands);
        }
    }
}