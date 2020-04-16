using Microsoft.AspNetCore.Routing;
using System;

namespace Microsoft.AspNetCore.Builder
{
    public static class IEndpointRouteBuilderExtensions
    {
        public static void MapLogs(this IEndpointRouteBuilder builder, string pattern = default)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            string path = pattern ?? "/debug/logs";

            Http.RequestDelegate pipeline = builder.CreateApplicationBuilder()
                                  .UseMiddleware<WebLoggingMiddleware>()
                                  .Build();

            builder.MapGet(path, pipeline);
        }
    }
}
