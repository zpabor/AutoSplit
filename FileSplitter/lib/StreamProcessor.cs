using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using System.Security.Cryptography;

namespace FileSplitter
{   
    class StreamProcessorBase
    {

    }   
    abstract class StreamProcessor
    {              
        public RingBuffer ringbuffer { set { _rbuff = value; } }
        private RingBuffer _rbuff;
        protected byte[] _buff;
        public byte[] buffer { set { _buff = value; } }     
        public Stream outputStream { set { _outputStream = value; } }
        private Stream _outputStream;
        private Task _task;
        public Task proctask { get { if (_task == null) CreateTask(); return _task; } }                             
        public void CreateTask()
        {
          
            Task processorTask = new Task(() =>
            {
                byte[] buff = new byte[1];
                
                for (long ia = 0; ia < (_outputStream.Length / buff.Length) - 1; ia++)
                {
                    buff = _rbuff.readNext();
                    buffWorker();
                    _outputStream.Write(buff, 0, 0);                  
                }
                buff = _rbuff.readNext();
                buffWorker();                
                buffFinalize();
                _outputStream.Close();               
            });
            _task = processorTask;
        }
        abstract protected void buffWorker();
        virtual protected void buffFinalize(){return;}        
    }    


    class StreamProcessorController
    {
        protected RingBuffer _rbuff;
        private Stream inputStream;
        List<Task> _taskList;
       
        private bool _isComplete = false;
        public bool isComplete { get { return _isComplete; } }
        private byte[] _buff = new byte[1];
        protected long bytesread;
        public void Start()
        {
            
                        
            _rbuff = new RingBuffer(1024, _taskList.Count);
            Task.Run(() =>
            {
                do
                {
                    bytesread = inputStream.Read(_buff, 0, _buff.Length);
                    _rbuff.writeNext(_buff);
                    //sleepcounter++;
                    //if (sleepcounter == 16)
                    //    await Task.Delay(1000);
                } while (bytesread > 0);
            });
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
            newStreamProc.buffer = _buff;              
            _taskList.Add(newStreamProc.proctask);
        }
    }    
    class MD5_StreamProcessor : StreamProcessor
    {             
        MD5 md5 = MD5.Create();
        protected override void buffWorker()
        {   
                     
            md5.TransformBlock(_buff, 0, _buff.Length, null, 0);
        }
        protected override void buffFinalize()
        {
            md5.TransformFinalBlock(_buff, _buff.Length, 0);
        }
    }
}
