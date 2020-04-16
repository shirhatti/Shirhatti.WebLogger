using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Configuration;
using Shirhatti.WebLogger;
using System;

namespace Microsoft.Extensions.Logging
{
    public static class ILoggingBuilderExtensions
    {
        public static ILoggingBuilder AddWebLogger(this ILoggingBuilder builder)
        {
            builder.Services.TryAddSingleton<WebLoggerProcessor>();
            builder.Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, WebLoggerProvider>());
            return builder;
        }
    }
}
