using System;
using System.Collections.Generic;
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
                while (ex != null)
                {
                    sb.AppendLine(ex.Message);
                    ex = ex.InnerException;
                }
                return sb.ToString();
            }
        }
        public string TimeStamp {
            get { return loggingEvent.TimeStamp.ToShortTimeString(); }
        }
    }
}
