using System;

namespace H.Utilities.Messages
{
    [Serializable]
    public class GetTypesMessage : Message
    {
        public GetTypesMessage()
        {
            Text = "get_types";
        }
    }
}
