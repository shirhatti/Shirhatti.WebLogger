using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Shirhatti.WebLogger
{
    internal class WebLoggerProcessor : IDisposable, IObservable<LogMessageEntry>
    {
        private const int _maxQueuedMessages = 1024;
        private readonly List<IObserver<LogMessageEntry>> _observers;
        private readonly BlockingCollection<LogMessageEntry> _messageQueue = new BlockingCollection<LogMessageEntry>(_maxQueuedMessages);
        private readonly Thread _outputThread;

        public WebLoggerProcessor()
        {
            _observers = new List<IObserver<LogMessageEntry>>();

            _outputThread = new Thread(ProcessLogQueue)
            {
                IsBackground = true,
                Name = "Web logger queue processing thread"
            };
            _outputThread.Start();
        }

        internal virtual void EnqueueMessage(LogMessageEntry message)
        {
            if (!_messageQueue.IsAddingCompleted)
            {
                try
                {
                    _messageQueue.Add(message);
                    return;
                }
                catch (InvalidOperationException) { }
            }
        }

        private void ProcessLogQueue()
        {
            try
            {
                foreach (LogMessageEntry message in _messageQueue.GetConsumingEnumerable())
                {
                    foreach (IObserver<LogMessageEntry> observer in _observers)
                    {
                        observer.OnNext(message);
                    }
                }
            }
            catch
            {
                try
                {
                    _messageQueue.CompleteAdding();
                }
                catch { }
            }
        }

        public void Dispose()
        {
            _messageQueue.CompleteAdding();
            foreach (IObserver<LogMessageEntry> observer in _observers)
            {
                observer.OnCompleted();
            }
            try
            {
                _outputThread.Join(1500);
            }
            catch (ThreadStateException) { }
        }

        public IDisposable Subscribe(IObserver<LogMessageEntry> observer)
        {
            if (!_observers.Contains(observer))
            {
                _observers.Add(observer);
            }
            return new Unsubscriber<LogMessageEntry>(_observers, observer);
        }
    }
}
