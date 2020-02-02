using System;

namespace H.Utilities.Messages
{
    [Serializable]
    public class RaiseEventMessage : Message
    {
        public Guid? ObjectGuid { get; set; }
        public string? EventName { get; set; }
        public Guid? EventGuid { get; set; }

        public RaiseEventMessage()
        {
            Text = "raise_event";
        }

        public string ConnectionName => $"H.Containers.Process_{ObjectGuid}_{EventName}_Event_{EventGuid}";
    }
}
