using System;
using log4net.Appender;
using log4net.Core;
using log4net;

namespace XbimXplorer.LogViewer
{
    public class EventAppender : AppenderSkeleton
    {
        /// <summary>
        /// Pauses processing events for EventsQuantityInterval if their number reached EventsQuantityLimit within the time.
        /// </summary>
        public int EventsLimit = Int32.MaxValue;

        /// <summary>
        /// resets events after EventsResetInterval without events.
        /// </summary>
        public int EventsResetInterval = 60;

        private int _eventsTally = 0;
        private DateTime _lastEvent = DateTime.Now;



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
            if (DateTime.Now.Subtract(_lastEvent).TotalSeconds > EventsResetInterval)
                _eventsTally = 0;
        
            // logging was already suspended
            if (_eventsTally == -1)
                return;

            // Validate parameters.
            if (loggingEvents == null)
                throw new ArgumentNullException(nameof(loggingEvents));

            _eventsTally += loggingEvents.Length;
            // determine what to 
            var eventsToReport = loggingEvents;

            if (_eventsTally > EventsLimit)
            {
                // todo: log warning then suspend
                LoggingEventData d = new LoggingEventData();
                d.Level = Level.Warn;
                d.LoggerName = "XbimXplorer.LogViewer.EventAppender";
                d.Message = $"Message limit reached, logging suspended for {EventsResetInterval} seconds.";
                d.TimeStampUtc = DateTime.Now.ToUniversalTime();
                LoggingEvent l = new LoggingEvent(d);
                _eventsTally = -1;
                eventsToReport = new[] { l };
            }
            
            // define and get the event handlers.
            EventHandler<LogEventArgs> handlers;
            lock (_eventLock) handlers = _loggedEventHandlers;

            // Fire if not null.
            handlers?.Invoke(this, new LogEventArgs(eventsToReport));
        }
    }
}

