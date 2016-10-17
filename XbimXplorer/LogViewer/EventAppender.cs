using System;
using log4net.Appender;
using log4net.Core;

namespace XbimXplorer.LogViewer
{
    public class EventAppender : AppenderSkeleton
    {
        // Note, you will probably have to override other things here.

        public string Tag;

        // The lock for the event.
        private readonly object _eventLock = new object();

        // The backing field for the event.
        private EventHandler<LogEventArgs> _loggedEventHandlers;
            
        // Add and remove methods.
        public event EventHandler<LogEventArgs> Logged 
        {
            add
            {
                lock (_eventLock) 
                    _loggedEventHandlers += value;
            }
            remove
            {
                lock (_eventLock)
                {
                    // ReSharper disable once DelegateSubtraction // warning does not apply to this case
                    _loggedEventHandlers -= value;
                }
            }
        }
    

        // Singular case.
        protected override void Append(LoggingEvent loggingEvent)
        {
            // Validate parameters.
            if (loggingEvent == null)
                throw new ArgumentNullException(nameof(loggingEvent));

            // Call the override that processes these in bulk.
            Append(new[] {loggingEvent});
        }

        // Multiple case.
        protected override void Append(LoggingEvent[] loggingEvents)
        {
            // Validate parameters.
            if (loggingEvents == null)
                throw new ArgumentNullException(nameof(loggingEvents));

            // The event handlers.
            EventHandler<LogEventArgs> handlers;

            // Get the handlers.
            lock (_eventLock) handlers = _loggedEventHandlers;

            // Fire if not null.
            handlers?.Invoke(this, new LogEventArgs(loggingEvents));
        }
    }
}

