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
        public RingBuffer ringbuffer { set { _rbuff = value; } }
        private RingBuffer _rbuff;      
        public Stream outputStream { set { _outputStream = value; } }
        private Stream _outputStream;
        private Task _task;
        public Task proctask { get { if (_task == null) CreateTask(); return _task; } }
        public StreamProcessor(Stream outstream, ref RingBuffer rbuffinit)
        {
            outputStream = outstream;
            _rbuff = rbuffinit;
        }  
        public void CreateTask()
        {
            Task processorTask = new Task(() =>
            {
                byte[] tmpbuff = new byte[1];
                
                for (long ia = 0; ia < (_outputStream.Length / tmpbuff.Length) - 1; ia++)
                {
                    tmpbuff = _rbuff.readNext();
                    buffworker(ref tmpbuff);                   
                }
                tmpbuff = _rbuff.readNext();
                buffworker(ref tmpbuff);                
            });
            _task = processorTask;
        }
        public abstract void buffworker(ref byte[] objectivebuffer);
    }


    class StreamProcessorController
    {
        List<Task> _taskList;
        private RingBuffer _rbuff;
        private bool _isComplete = false;
        public bool isComplete { get { return _isComplete; } }
        public void Start()
        {
            _rbuff = new RingBuffer(1024, _taskList.Count);

            foreach (Task t in _taskList)            
                t.Start();
            Wait();                                                              
        }
        private void Wait() 
        {
            Task.Run(() =>
            {
                Task.WaitAll(_taskList.ToArray());
                _isComplete = true;
            });            
        }
        public void addProcessor(StreamProcessor newStreamProc)
        {
            newStreamProc.ringbuffer = _rbuff;            
            _taskList.Add(newStreamProc.proctask);
        }
    }
}
