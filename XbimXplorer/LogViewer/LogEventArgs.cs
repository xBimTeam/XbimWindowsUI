using System;
using System.Collections.Generic;
using log4net.Core;

namespace XbimXplorer.LogViewer
{
    public class LogEventArgs : EventArgs
    {
        public LogEventArgs(IEnumerable<LoggingEvent> loggingEvents)
        {
            // Validate parameters.
            if (loggingEvents == null)
                throw new ArgumentNullException("loggingEvents");

            // Assign values.
            LoggingEvents = loggingEvents;
        }

        // Poor-man's immutability.
        public IEnumerable<LoggingEvent> LoggingEvents { get; private set; }
    }
}
