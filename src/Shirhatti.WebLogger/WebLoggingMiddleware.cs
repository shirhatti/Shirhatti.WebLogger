using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;
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

        public WebLoggingMiddleware(RequestDelegate _)
        {

        }
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

            context.Response.ContentType = "text/event-stream";
            context.Response.Headers[HeaderNames.CacheControl] = "no-cache";

            // Make sure we disable all response buffering for SSE
            var bufferingFeature = context.Features.Get<IHttpResponseBodyFeature>();
            bufferingFeature.DisableBuffering();

            context.Response.Headers[HeaderNames.ContentEncoding] = "identity";

            // Workaround for a Firefox bug where EventSource won't fire the open event
            // until it receives some data
            await context.Response.WriteAsync("\r\n");
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
