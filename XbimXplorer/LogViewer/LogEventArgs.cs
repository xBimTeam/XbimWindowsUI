using System;
using System.Collections.Generic;
using Serilog.Events;

namespace XbimXplorer.LogViewer
{
    public class LogEventArgs : EventArgs
    {
        public LogEventArgs(IEnumerable<LogEvent> loggingEvents)
        {
            // Validate parameters.
            if (loggingEvents == null)
                throw new ArgumentNullException(nameof(loggingEvents));

            // Assign values.
            LoggingEvents = loggingEvents;
        }

        // Poor-man's immutability.
        public IEnumerable<LogEvent> LoggingEvents { get; private set; }
       
    }
}
