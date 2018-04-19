using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;

namespace XbimXplorer.LogViewer
{
    public class EventViewModel
    {
        private log4net.Core.LoggingEvent loggingEvent;

        public EventViewModel(log4net.Core.LoggingEvent loggingEvent)
        {
            this.loggingEvent = loggingEvent;
        }


        public string Logger
        {
            get { return loggingEvent.LoggerName; }
        }

        public string Message
        {
            get { return loggingEvent.RenderedMessage; }
            
        }

        public string Level
        {
            get { return loggingEvent.Level.ToString(); }
        }

        public string ErrorMessage
        {
            get
            {
                if (loggingEvent.ExceptionObject == null)
                    return "";
                var sb = new StringBuilder();
                var ex = loggingEvent.ExceptionObject;
                string stackTrace = ex.StackTrace;
                while (ex != null)
                {
                    sb.AppendLine(ex.Message);
                    ex = ex.InnerException;
                }
                sb.AppendLine(stackTrace);
                return sb.ToString();
            }
        }
        public string TimeStamp {
            get { return loggingEvent.TimeStamp.ToShortTimeString(); }
        }

        public string Summary
        {
            get
            {
                return string.Format("==== {0}\t{1}\t{2}\r\n{3}\r\n{4}\r\n\r\n",
                    TimeStamp,
                    Level,
                    Logger,
                    Message,
                    ErrorMessage
                    );
            }
        }
    }
}
