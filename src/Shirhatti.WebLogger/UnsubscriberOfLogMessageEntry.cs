using System;
using System.Collections.Generic;

namespace Shirhatti.WebLogger
{
    internal class Unsubscriber<LogMessageEntry> : IDisposable
    {
        private readonly List<IObserver<LogMessageEntry>> _observers;
        private readonly IObserver<LogMessageEntry> _observer;

        internal Unsubscriber(List<IObserver<LogMessageEntry>> observers, IObserver<LogMessageEntry> observer)
        {
            _observers = observers;
            _observer = observer;
        }

        public void Dispose()
        {
            if (_observers.Contains(_observer))
            {
                _observers.Remove(_observer);
            }
        }
    }
}
