using System;
using Xunit;
using TickerTracker.Models;

namespace UnitTests
{
    public class UnitTests
    {
        [Fact]
        public void Test1()
        {
            Assert.True(!false);
        }
    }

    public class DatabaseTests
    {
        [Fact]
        public void Connection()
        {
            bool connected = false;

            var exception = Record.Exception(() => Database.Instance().Query(conn => connected = true));

            // no exceptions thrown
            Assert.Null(exception);

            // query callback executed
            Assert.True(connected);
        }
    }
}
