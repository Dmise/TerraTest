using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp2
{
    internal enum QueueStateEnum
    {
        onTimer,
        dispose,
        cancel,
        startSending,
        endSending,
        exception,
        bufferReady,
        bufferOverflow
    }
}
