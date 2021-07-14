using DeltaKustoLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace DeltaKustoUnitTest
{
    public class KustoTimeSpanTest : TestBase
    {
        [Fact]
        public void Days()
        {
            var time = new KustoTimeSpan(TimeSpan.FromDays(5));

            Assert.Equal("5d", time.ToString());
        }

        [Fact]
        public void Hours()
        {
            var time = new KustoTimeSpan(TimeSpan.FromHours(2));

            Assert.Equal("2h", time.ToString());
        }

        [Fact]
        public void Minutes()
        {
            var time = new KustoTimeSpan(TimeSpan.FromMinutes(7));

            Assert.Equal("7m", time.ToString());
        }

        [Fact]
        public void Seconds()
        {
            var time = new KustoTimeSpan(TimeSpan.FromSeconds(3));

            Assert.Equal("3s", time.ToString());
        }

        [Fact]
        public void MilliSeconds()
        {
            var time = new KustoTimeSpan(TimeSpan.FromMilliseconds(45));

            Assert.Equal("45ms", time.ToString());
        }

        [Fact]
        public void Mix()
        {
            var time = new KustoTimeSpan(TimeSpan.FromDays(32) + TimeSpan.FromHours(5));

            Assert.StartsWith("time(32:05", time.ToString());
        }

        [Fact]
        public void Null()
        {
            var time = new KustoTimeSpan(null);

            Assert.Equal("time(null)", time.ToString());
        }
    }
}