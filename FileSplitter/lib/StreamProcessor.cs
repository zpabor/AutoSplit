using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using System.Security.Cryptography;

namespace FileSplitter
{   
    internal class test { } 
    abstract class StreamProcessorBase
    {
        private long _length;
        public long LengthToProcess { set { _length = value; _lengthIsSet = true; } }
        private bool _lengthIsSet = false;
        public bool lenghthIsSet { get { return _lengthIsSet; } }        
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
                
                for (long ia = 0; ia < (_length / buff.Length) - 1; ia++)
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
        private Stream _inputStream;
        public Stream inputStream { set { _inputStream = value;} }
        List<Task> _taskList = new List<Task>();
       
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
                    bytesread = _inputStream.Read(_buff, 0, _buff.Length);
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
        public void addProcessor(StreamProcessorBase newStreamProc)
        {                     
            newStreamProc.ringbuffer = _rbuff;
            newStreamProc.buffer = _buff;
            if (!newStreamProc.lenghthIsSet)
                newStreamProc.LengthToProcess = _inputStream.Length;             
            _taskList.Add(newStreamProc.proctask);
        }
    }    
    class MD5_StreamProcessor : StreamProcessorBase
    {
        MD5 md5;
        public byte[] result { get { return md5.Hash; } }
        public MD5_StreamProcessor()
        {
            md5 = MD5.Create();
            md5.Initialize();
        }
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
