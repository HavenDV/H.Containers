using System;

namespace H.Utilities.Messages
{
    [Serializable]
    public class LoadAssemblyMessage : Message
    {
        public string? Path { get; set; }

        public LoadAssemblyMessage()
        {
            Text = "load_assembly";
        }
    }
}
