using Microsoft.Extensions.Logging;
using System;
using System.Text;

namespace Shirhatti.WebLogger
{
    public class WebLogger : ILogger
    {
        private static readonly string _loglevelPadding = ": ";
        private static readonly string _messagePadding;
        private static readonly string _newLineWithMessagePadding;

        private readonly WebLoggerProcessor _queueProcessor;
        private Func<string, LogLevel, bool> _filter;

        [ThreadStatic]
        private static StringBuilder _logBuilder;

        static WebLogger()
        {
            string logLevelString = GetLogLevelString(LogLevel.Information);
            _messagePadding = new string(' ', logLevelString.Length + _loglevelPadding.Length);
            _newLineWithMessagePadding = Environment.NewLine + _messagePadding;
        }

        public WebLogger(string name, Func<string, LogLevel, bool> filter, bool includeScopes)
            : this(name, filter, includeScopes ? new LoggerExternalScopeProvider() : null, new WebLoggerProcessor())
        {
        }

        public WebLogger(string name, Func<string, LogLevel, bool> filter, IExternalScopeProvider scopeProvider)
            : this(name, filter, scopeProvider, new WebLoggerProcessor())
        {
        }

        internal WebLogger(string name, Func<string, LogLevel, bool> filter, IExternalScopeProvider scopeProvider, WebLoggerProcessor loggerProcessor)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Filter = filter ?? ((category, logLevel) => true);
            ScopeProvider = scopeProvider;
            _queueProcessor = loggerProcessor;
        }

        public Func<string, LogLevel, bool> Filter
        {
            get { return _filter; }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                _filter = value;
            }
        }

        public string Name { get; }

        internal IExternalScopeProvider ScopeProvider { get; set; }

        internal string TimestampFormat { get; set; }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }

            string message = formatter(state, exception);

            if (!string.IsNullOrEmpty(message) || exception != null)
            {
                WriteMessage(logLevel, Name, eventId.Id, message, exception);
            }
        }

        public virtual void WriteMessage(LogLevel logLevel, string logName, int eventId, string message, Exception exception)
        {
            StringBuilder logBuilder = _logBuilder;
            _logBuilder = null;

            if (logBuilder == null)
            {
                logBuilder = new StringBuilder();
            }

            string logLevelString = GetLogLevelString(logLevel);
            // category and event id
            logBuilder.Append(_loglevelPadding);
            logBuilder.Append(logName);
            logBuilder.Append("[");
            logBuilder.Append(eventId);
            logBuilder.AppendLine("]");

            // scope information
            GetScopeInformation(logBuilder);

            if (!string.IsNullOrEmpty(message))
            {
                // message
                logBuilder.Append(_messagePadding);

                int len = logBuilder.Length;
                logBuilder.AppendLine(message);
                logBuilder.Replace(Environment.NewLine, _newLineWithMessagePadding, len, message.Length);
            }

            // Example:
            // System.InvalidOperationException
            //    at Namespace.Class.Function() in File:line X
            if (exception != null)
            {
                // exception message
                logBuilder.AppendLine(exception.ToString());
            }

            bool hasLevel = !string.IsNullOrEmpty(logLevelString);
            string timestampFormat = TimestampFormat;
            // Queue log message
            _queueProcessor.EnqueueMessage(new LogMessageEntry()
            {
                TimeStamp = timestampFormat != null ? DateTime.Now.ToString(timestampFormat) : null,
                Message = logBuilder.ToString(),
                LevelString = hasLevel ? logLevelString : null
            });

            logBuilder.Clear();
            if (logBuilder.Capacity > 1024)
            {
                logBuilder.Capacity = 1024;
            }
            _logBuilder = logBuilder;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            if (logLevel == LogLevel.None)
            {
                return false;
            }

            return Filter(Name, logLevel);
        }

        public IDisposable BeginScope<TState>(TState state) => ScopeProvider?.Push(state);

        private static string GetLogLevelString(LogLevel logLevel)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    return "trce";
                case LogLevel.Debug:
                    return "dbug";
                case LogLevel.Information:
                    return "info";
                case LogLevel.Warning:
                    return "warn";
                case LogLevel.Error:
                    return "fail";
                case LogLevel.Critical:
                    return "crit";
                default:
                    throw new ArgumentOutOfRangeException(nameof(logLevel));
            }
        }

        private void GetScopeInformation(StringBuilder stringBuilder)
        {
            IExternalScopeProvider scopeProvider = ScopeProvider;
            if (scopeProvider != null)
            {
                int initialLength = stringBuilder.Length;

                scopeProvider.ForEachScope((scope, state) =>
                {
                    (StringBuilder builder, int length) = state;
                    bool first = length == builder.Length;
                    builder.Append(first ? "=> " : " => ").Append(scope);
                }, (stringBuilder, initialLength));

                if (stringBuilder.Length > initialLength)
                {
                    stringBuilder.Insert(initialLength, _messagePadding);
                    stringBuilder.AppendLine();
                }
            }
        }
    }
}
