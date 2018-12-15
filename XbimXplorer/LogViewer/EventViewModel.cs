using Serilog.Events;
using System.Text;

namespace XbimXplorer.LogViewer
{
    public class EventViewModel
    {
        private LogEvent loggingEvent;

        public EventViewModel(LogEvent loggingEvent)
        {
            this.loggingEvent = loggingEvent;
        }


        public string Logger
        {
            get { return loggingEvent.Properties.TryGetValue("SourceContext", out LogEventPropertyValue value) 
                    ? GetTextValue(value) 
                    : "<Global>"; }
        }

        public int ThreadId
        {
            get
            {
                return loggingEvent.Properties.TryGetValue("ThreadId", out LogEventPropertyValue value)
                  ? GetIntValue(value)
                  : 0;
            }
        }

        private string GetTextValue(LogEventPropertyValue value)
        {
            if (value is ScalarValue scalar)
            {
                return scalar.Value.ToString();
            }
            return "";
        }

        private int GetIntValue(LogEventPropertyValue value)
        {
            if (value is ScalarValue scalar)
            {
                return (int)scalar.Value;
            }
            return 0;
        }

        public string Message
        {
            get { return loggingEvent.RenderMessage(); }
            
        }

        public string Level
        {
            get { return loggingEvent.Level.ToString(); }
        }

        public string ErrorMessage
        {
            get
            {
                if (loggingEvent.Exception == null)
                    return "";
                var sb = new StringBuilder();
                var ex = loggingEvent.Exception;
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
            get { return loggingEvent.Timestamp.ToString("t"); }
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
