using ConsoleApp2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Terra
{
    internal class ProgressReportFactory<T> 
    {
        private Type tType;
        public ProgressReportFactory()
        {
           tType = typeof(T);
        }
        public dynamic Report(QueueStateEnum state)
        {
            
            if (tType == typeof(string))
            {
                switch (state) 
                {                   
                    case QueueStateEnum.onTimer:
                        return "On timer event";
                    case QueueStateEnum.dispose:
                        return "Dispose";
                    case QueueStateEnum.cancel:
                        return "Cancel";
                    case QueueStateEnum.startSending:
                        return "start sending";
                    case QueueStateEnum.endSending:
                        return "end sending";                   
                    case QueueStateEnum.exception:
                        return "exception";
                    case QueueStateEnum.bufferReady:
                        return "bufferReady";
                    case QueueStateEnum.bufferOverflow:
                        return "buffer overflow";
                    default:
                        return "unhandled state";
                }
            
            }
            else if (tType == typeof(int)) 
            {
                // TODO:
                return 777;
            }

            return default;
        }
         
    }  
}
