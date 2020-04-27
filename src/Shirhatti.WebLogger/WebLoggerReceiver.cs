using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;

namespace Shirhatti.WebLogger
{
    internal class WebLoggerReceiver : IObserver<LogMessageEntry>, IDisposable
    {
        private const int _maxQueuedMessages = 10;
        private IDisposable _unsubscriber;
        private readonly Channel<LogMessageEntry> _channel = Channel.CreateBounded<LogMessageEntry>(_maxQueuedMessages);
        private readonly WebLoggerProcessor _processor;

        public WebLoggerReceiver(WebLoggerProcessor processor)
        {
            _processor = processor;
        }

        public async IAsyncEnumerable<LogMessageEntry> FetchLogs([EnumeratorCancellation] CancellationToken token = default)
        {
            Subscribe(_processor);
            while (await _channel.Reader.WaitToReadAsync(token))
            {
                var logMessage = await _channel.Reader.ReadAsync();
                yield return logMessage;
            }
            Unsubscribe();
        }

        public virtual void Subscribe(IObservable<LogMessageEntry> provider)
        {
            _unsubscriber = provider.Subscribe(this);
        }

        public virtual void Unsubscribe()
        {
            _unsubscriber?.Dispose();
        }

        public void OnCompleted()
        {
            _ = _channel.Writer.TryComplete();
        }

        public void OnError(Exception error)
        {
            _ = _channel.Writer.TryComplete();
        }

        public void OnNext(LogMessageEntry message)
        {
            _ = _channel.Writer.TryWrite(message);
        }

        public void Dispose()
        {
            _unsubscriber?.Dispose();
        }
    }
}
