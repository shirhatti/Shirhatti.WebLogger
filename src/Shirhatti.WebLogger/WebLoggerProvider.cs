using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Shirhatti.WebLogger
{
    internal class WebLoggerProvider : ILoggerProvider
    {
        private readonly ConcurrentDictionary<string, WebLogger> _loggers = new ConcurrentDictionary<string, WebLogger>();
        private readonly WebLoggerProcessor _messageQueue;

        public WebLoggerProvider(WebLoggerProcessor loggerProcessor)
        {
            _messageQueue = loggerProcessor;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, CreateLoggerImplementation);
        }

        private WebLogger CreateLoggerImplementation(string name)
        {
            return new WebLogger(name, null, null, _messageQueue);
        }

        public void Dispose()
        {
            _messageQueue.Dispose();
        }
    }
}
