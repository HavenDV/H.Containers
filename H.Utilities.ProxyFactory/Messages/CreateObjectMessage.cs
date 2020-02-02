using System;

namespace H.Utilities.Messages
{
    [Serializable]
    public class CreateObjectMessage : Message
    {
        public string? TypeName { get; set; }
        public Guid? Guid { get; set; }

        public CreateObjectMessage()
        {
            Text = "create_object";
        }
    }
}
