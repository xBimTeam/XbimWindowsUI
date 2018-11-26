using Serilog.Core;
using Serilog.Events;
using System;


namespace XbimXplorer.LogViewer
{
    // A 'Sink' is Serilog's term for a log4net 'appender'

    /// <summary>
    /// A Serilog 'Sink' which lets us intercept all logs a relay to an event handler so we can see logs in the application
    /// </summary>
    public class InMemoryLogSink : ILogEventSink
    {
        /// <summary>
        /// Pauses processing events for EventsQuantityInterval if their number reached EventsQuantityLimit within the time.
        /// </summary>
        public int EventsLimit = int.MaxValue;

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
                    _loggedEventHandlers -= value;
                }
            }
        }


        public void Emit(LogEvent logEvent)
        {
            if (DateTime.Now.Subtract(_lastEvent).TotalSeconds > EventsResetInterval)
            {
                _lastEvent = DateTime.Now;
                _eventsTally = 0;
            }
        
            // logging was already suspended
            if (_eventsTally == -1)
                return;

            // Validate parameters.
            if (logEvent == null)
                throw new ArgumentNullException(nameof(logEvent));

            _eventsTally ++;

            // define and get the event handlers.
            EventHandler<LogEventArgs> handlers;
            lock (_eventLock) handlers = _loggedEventHandlers;

            // Send the log event
            handlers?.Invoke(this, new LogEventArgs(new[] { logEvent }));

            if (_eventsTally > EventsLimit)
            {
                // Fake an event to notify user we're stopping.

                var messageTemplate = new MessageTemplate(new[] {
                    new Serilog.Parsing.TextToken($"Message limit reached, logging suspended for {EventsResetInterval} seconds.") });
                LogEvent overLimit = new LogEvent(DateTime.UtcNow, LogEventLevel.Warning, null, messageTemplate, new LogEventProperty[0]);

                _eventsTally = -1;
                // Send the warning of overlimit
                handlers?.Invoke(this, new LogEventArgs(new[] { overLimit }));
            }
        }

        
    }

}

