using Microsoft.AspNetCore.Http;
using Shirhatti.WebLogger;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Builder
{
    internal class WebLoggingMiddleware : IObserver<LogMessageEntry>
    {
        private IDisposable _unsubscriber;
        private TaskCompletionSource<bool> _tcs;
        private HttpContext _context;
        private volatile bool _isCancelled;

        public async Task Invoke(HttpContext context, WebLoggerProcessor processor)
        {
            _isCancelled = false;
            _tcs = new TaskCompletionSource<bool>();
            _context = context;

            CancellationToken? token = context?.RequestAborted;
            token.Value.Register(() =>
            {
                _isCancelled = true;
                _tcs.TrySetCanceled();
            });

            context.Response.Headers.Add("Content-Type", "text/event-stream");
            await context.Response.Body.FlushAsync();
            Subscribe(processor);
            try
            {
                await _tcs.Task;
            }
            catch (OperationCanceledException)
            {

            }
            finally
            {
                Unsubscribe();
            }
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
            _isCancelled = true;
            _tcs.TrySetResult(true);
        }

        public void OnError(Exception error)
        {
            // Do nothing.
        }

        public async void OnNext(LogMessageEntry message)
        {
            if (_isCancelled || _context==null)
            {
                return;
            }
            if (message.TimeStamp != null)
            {
                await _context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes(message.TimeStamp));
            }

            if (message.LevelString != null)
            {
                await _context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes(message.LevelString));
            }

            await _context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes(message.Message));
        }
    }
}
