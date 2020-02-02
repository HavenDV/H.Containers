using System;

namespace H.Utilities.Messages
{
    [Serializable]
    public class ExceptionMessage : Message
    {
        public Exception? Exception { get; set; }

        public ExceptionMessage()
        {
            Text = "exception";
        }
    }
}
