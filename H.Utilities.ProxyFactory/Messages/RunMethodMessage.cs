using System;

namespace H.Utilities.Messages
{
    [Serializable]
    public class RunMethodMessage : MethodMessage
    {
        public RunMethodMessage()
        {
            Text = "run_method";
        }

        public string ConnectionPrefix => $"H.Containers.Process_{ObjectGuid}_{MethodName}_{MethodGuid}_";
    }
}
