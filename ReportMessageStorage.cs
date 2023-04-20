using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Terra
{   
    public class ReportMessageStorage
    {
        private static List<KeyValuePair<QueueStateEnum, string>> _messageList = new()
        {
            new KeyValuePair<QueueStateEnum, string>(QueueStateEnum.dispose, "Dispose"),
            new KeyValuePair<QueueStateEnum, string>(QueueStateEnum.bufferReady, "BufferReady"),
            new KeyValuePair<QueueStateEnum, string>(QueueStateEnum.endSending, "End sending documents"),
            new KeyValuePair<QueueStateEnum, string>(QueueStateEnum.startSending, "Start sending documents"),
            new KeyValuePair<QueueStateEnum, string>(QueueStateEnum.cancel, "Cancel sending messages"),
            new KeyValuePair<QueueStateEnum, string>(QueueStateEnum.onTimer, "Time process activated"),
            new KeyValuePair<QueueStateEnum, string>(QueueStateEnum.bufferOverflow, "Buffer overflow we gather more message than connector can send by one time."),
            new KeyValuePair<QueueStateEnum, string>(QueueStateEnum.exception, "An exception occurred during document sending process."),
            new KeyValuePair<QueueStateEnum, string>(QueueStateEnum.warning, "Reach undesire condition. Code and task progress is not perfect."),
        };
        
        internal static ConcurrentDictionary<QueueStateEnum, string> MessageDictionary = new ConcurrentDictionary<QueueStateEnum, string>(_messageList);
        public ReportMessageStorage()
        {
            
        }     
    }
}
