using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FileSplitter
{
    abstract class StreamProcessor
    {
        RingBuffer rbuff;
        private Task t;
        private Stream outputStream;
        public void CreateTask()
        {
            Task processorTask = new Task(() =>
            {
                byte[] tmpbuff = new byte[1];
                
                for (long ia = 0; ia < (outputStream.Length / tmpbuff.Length) - 1; ia++)
                {
                    tmpbuff = rbuff.readNext();
                    buffworker(ref tmpbuff);                   
                }
                tmpbuff = rbuff.readNext();
                buffworker(ref tmpbuff);                
            });

        }
        public abstract void buffworker(ref byte[] objectivebuffer);
    }
    class StreamProcessorController
    {
        Task[] taskset;

        public void Start()
        {
            RingBuffer rbuff = new RingBuffer(1024, taskset.Length);
            foreach (Task t in taskset)
                t.Start();      
        }
    }
}
