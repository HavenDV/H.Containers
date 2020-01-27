using System;
using System.Reflection;

namespace H.Utilities.Args
{
    /// <summary>
    /// 
    /// </summary>
    public class EventEventArgs : EventArgs
    {
        /// <summary>
        /// 
        /// </summary>
        public object? Args { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public EventInfo EventInfo { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public EmptyProxyFactory ProxyFactory { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public bool IsCanceled { get; set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        /// <param name="eventInfo"></param>
        /// <param name="proxyFactory"></param>
        public EventEventArgs(object? args, EventInfo eventInfo, EmptyProxyFactory proxyFactory)
        {
            Args = args ?? throw new ArgumentNullException(nameof(args));
            EventInfo = eventInfo ?? throw new ArgumentNullException(nameof(eventInfo));
            ProxyFactory = proxyFactory ?? throw new ArgumentNullException(nameof(proxyFactory));
        }
    }
}
