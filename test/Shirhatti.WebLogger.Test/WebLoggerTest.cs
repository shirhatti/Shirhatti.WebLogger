using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Shirhatti.WebLogger.Test
{
    public class WebLoggerTest
    {
        [Fact]
        public async Task ReceiveMessageTest()
        {
            var logMessage = new LogMessageEntry();
            var proccesor = new WebLoggerProcessor();
            using var receiver = new WebLoggerReceiver(proccesor);
            receiver.Subscribe(proccesor);
            proccesor.EnqueueMessage(logMessage);
            proccesor.EnqueueMessage(logMessage);
            proccesor.Dispose();
            await foreach(var expectedMessage in receiver.FetchLogs())
            {
                Assert.Equal(logMessage, expectedMessage);
            }
        }

        [Fact]
        public async Task CancelReceiveTest()
        {
            var logMessage = new LogMessageEntry();
            using var proccesor = new WebLoggerProcessor();
            using var receiver = new WebLoggerReceiver(proccesor);
            receiver.Subscribe(proccesor);
            proccesor.EnqueueMessage(logMessage);
            proccesor.EnqueueMessage(logMessage);
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            proccesor.Dispose();

            await foreach (var expectedMessage in receiver.FetchLogs(token))
            {
                Assert.Equal(logMessage, expectedMessage);
                Assert.Throws<TaskCanceledException>(() =>
                {
                    cts.Cancel();
                });
            }
        }
    }
}
