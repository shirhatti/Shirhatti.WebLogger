using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Net.Http.Headers;
using Shirhatti.WebLogger;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace Microsoft.AspNetCore.Builder
{
    internal class WebLoggingMiddleware
    {
        public WebLoggingMiddleware(RequestDelegate next)
        {
        }
        public async Task Invoke(HttpContext context, WebLoggerProcessor processor)
        {
            var token = context?.RequestAborted ?? default;

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

            using var receiver = new WebLoggerReceiver(processor);

            await foreach (var message in receiver.FetchLogs(token))
            {
                if (message.TimeStamp != null)
                {
                    await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes(message.TimeStamp), token);
                }

                if (message.LevelString != null)
                {
                    await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes(message.LevelString), token);
                }

                await context.Response.Body.WriteAsync(Encoding.ASCII.GetBytes(message.Message), token);
            }
        }
    }
}
