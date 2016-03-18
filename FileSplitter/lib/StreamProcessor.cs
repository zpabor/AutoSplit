using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using System.Security.Cryptography;
using FileSplitter;
namespace StreamProcessorNS
{   
    internal class StreamProcessorBase
    {
        protected RingBuffer _rbuff;         
        protected void _rbuffSetInputToLocal(StreamProcessorBase input) { input._rbuff = _rbuff; }

        protected byte[] _buff;
        protected void _buffSetInputToLocal(StreamProcessorBase input) { input._buff = _buff; }

        protected Stream _IOStream;
        protected void _IOStreamInputToLocal(StreamProcessorBase input) { input._IOStream = _IOStream}
    } 
    abstract class StreamProcessor:StreamProcessorBase
    {
        private long _length;
        public long LengthToProcess { set { _length = value; _lengthIsSet = true; } }
        private bool _lengthIsSet = false;
        public bool lenghthIsSet { get { return _lengthIsSet; } }                             
        
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
                    _IOStream.Write(buff, 0, 0);                  
                }
                buff = _rbuff.readNext();
                buffWorker();                
                buffFinalize();
                _IOStream.Close();               
            });           
            _task = processorTask;
            
        }
        virtual protected void buffWorker(){return;}
        virtual protected void buffFinalize(){return;}        
    }    


    class StreamProcessorController:StreamProcessorBase
    {
        //protected RingBuffer _rbuff;
        //private Stream _inputStream;
       // public Stream inputStream { set { _inputStream = value;} }
        List<Task> _taskList = new List<Task>();
       
        private bool _isComplete = false;
        public bool isComplete { get { return _isComplete; } }
        //private byte[] _buff = new byte[1];
        protected long bytesread;
        public void Start()
        {
            
                        
            _rbuff = new RingBuffer(1024, _taskList.Count);
            Task.Run(() =>
            {
                do
                {
                    bytesread = _IOStream.Read(_buff, 0, _buff.Length);
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
            base._rbuffSetInputToLocal(newStreamProc);            
            if (!newStreamProc.lenghthIsSet)
                newStreamProc.LengthToProcess = _IOStream.Length;             
            _taskList.Add(newStreamProc.proctask);
        }
    }    
    class MD5_StreamProcessor : StreamProcessor
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
    class StreamProcessorSplit : StreamProcessor
    {
        List<Task> _TaskList = new List<Task>();
        void addnew()
        {
            CreateTask();
            Task t = new Task()
            _TaskList.Add(proctask);
            t.ContinueWith( ()=> t);      
        }
    }
}
