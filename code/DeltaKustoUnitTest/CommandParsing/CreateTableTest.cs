using DeltaKustoLib.CommandModel;
using System;
using Xunit;

namespace DeltaKustoUnitTest.CommandParsing
{
    public class CreateTableTest : ParsingTestBase
    {
        [Fact]
        public void ThreeColumns()
        {
            var command = ParseOneCommand(
                ".create-merge table demo_make_series1 (TimeStamp:datetime, BrowserVer:string, OsVer:string, Country:string)");
        }
    }
}