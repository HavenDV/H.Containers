﻿using System;

namespace H.Containers.Args
{
    /// <summary>
    /// 
    /// </summary>
    public class EventEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public string Hash { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string EventName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string PipeName { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public object? Args { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public EventEventArgs(string hash, string eventName, string pipeName, object? args)
        {
            Hash = hash ?? throw new ArgumentNullException(nameof(hash));
            EventName = eventName ?? throw new ArgumentNullException(nameof(eventName));
            PipeName = pipeName ?? throw new ArgumentNullException(nameof(pipeName));
            Args = args;
        }
    }
}
