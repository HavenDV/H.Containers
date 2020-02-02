using System;

namespace H.Utilities.Messages
{
    [Serializable]
    public class CancelMethodMessage : MethodMessage
    {
        public CancelMethodMessage()
        {
            Text = "cancel_method";
        }
    }
}
