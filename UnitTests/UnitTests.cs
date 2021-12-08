using System;
using Xunit;
using TickerTracker.Models;
using System.Threading.Tasks;

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
        public async void Connection()
        {
            bool connected = false;

            var exception = await Record.ExceptionAsync(() => Database.Query(conn =>
            {
                connected = true;
                return Task.FromResult(0);
            }));

            // no exceptions thrown
            Assert.Null(exception);

            // query callback executed
            Assert.True(connected);
        }
    }
}
