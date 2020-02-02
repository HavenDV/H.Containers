using System;

namespace H.Utilities.Messages
{
    [Serializable]
    public class MethodMessage : Message
    {
        public string? MethodName { get; set; }
        public Guid? ObjectGuid { get; set; }
        public Guid? MethodGuid { get; set; }
    }
}
