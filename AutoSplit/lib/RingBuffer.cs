using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SpliceUtilities
{
    class RingBuffer :  IDisposable
    {

        private struct Node
        {
            public byte[] data;
            public int readFlags;
        }       
        volatile int currentIndex = -1;
        int[] readerIndex;        
        int allReadFlagsValue = 0;
        object addlock = new object();
        object readCountLock = new object();
        object checkFlagLock = new object();
        int readerCount = 0;
        Task writer;
        Task<byte[]>[] reader;
        Node[] buffer;
        ConcurrentDictionary<int?, int> readerDictionary = new ConcurrentDictionary<int?, int>();
        public RingBuffer(int length, int nReaders)
        {
            //readerTotal = nReaders;
            buffer = new Node[length];
            reader = new Task<byte[]>[nReaders]; // single elemt task array to be waited on while runtime tasks are gathered and put into list then copied back into readers
            readerIndex = new int[nReaders];            
            
            for(int i = 0; i < readerIndex.Length; i++) {setFlag(ref allReadFlagsValue, ref i);}
            
            //initialize readerIndex to starting point of -1
            for (int i = 0;  i < readerIndex.Length; i++) {readerIndex[i] = -1;}
            //set readcount of each buffer to number of readers so the "isWrireReady" func will return true to fill the buffer initially
            for (int i = 0; i < buffer.Length; i++) { buffer[i].readFlags = allReadFlagsValue; }           
            
            //empty tasks for first run            
            writer = Task.Run(() => {});            
            reader[0] = Task<byte[]>.Run(() => { return new byte[1]; });
            for (int i = 1; i < reader.Length; i++)
                reader[i] = reader[0];
           
        }
        private int indexOfCurrentReader()
        {            
            if (!readerDictionary.ContainsKey(Task.CurrentId))
                addReader(); 
            
                return readerDictionary[Task.CurrentId];            
        }
        private void addReader()
        {            
            lock(addlock) //necessary 
            {
                int c = readerCount;
                readerDictionary.TryAdd(Task.CurrentId, c);                
                readerCount++;
            }
        }
        private void nextWriteNode()
        {
            if (currentIndex < buffer.Length -1) { System.Threading.Interlocked.Increment(ref currentIndex); }
            else { currentIndex = 0; }
        }
        private void nextReadNode(int index)
        {
            //int index = indexOfCurrentReader();

            if (readerIndex[index] < buffer.Length - 1) { System.Threading.Interlocked.Increment(ref readerIndex[index]); }
            else { readerIndex[index] = 0; }
        }
        public byte[] readNext()
        {                        
            int index = indexOfCurrentReader();            
            nextReadNode(index);
            while (!isReadReady(ref index))
            {
                writer.Wait();

            }
            reader[index] = Task<byte[]>.Factory.StartNew(() =>
            {
                //Console.WriteLine("Reading from: " + index + " at position " + readerIndex[index] + " with readcount " + buffer[readerIndex[index]].readFlags + " reader[index].Result: " + reader[index].Result[0]);
                TaskCompletionSource<byte[]> tcs = new TaskCompletionSource<byte[]>();
                tcs.SetResult(buffer[readerIndex[index]].data);
                //Console.WriteLine(index + ": " + readerIndex[index]);
                Task.Factory.StartNew(() =>
                {
                    TaskCompletionSource<bool> tcsi = new TaskCompletionSource<bool>();
                    setFlag(ref buffer[readerIndex[index]].readFlags, ref index);
                    tcsi.SetResult(true);
                }, TaskCreationOptions.AttachedToParent);
                return tcs.Task.Result;
            });            
            //Console.WriteLine("Reading from: " + index + " at position " + readerIndex[index] + " with readcount " + buffer[readerIndex[index]].readFlags + " reader[index].Result: " + reader[index].Result[0]);
            return reader[index].Result;            
        }
        public void writeNext(byte[] nextBuffer)
        {
            byte[] lba = (byte[])nextBuffer.Clone();
            writer.Wait(); //wait for previose write task to complete            
            nextWriteNode();            
            while (!isWriteReady())
            {                
                Task.WaitAll(reader);                         
            }            
            //byte[] lba = (byte[])nextBuffer.Clone(); //local copy of byte to add to the buffer          
            writer = Task.Run(() => writeNodeBuffer(lba, currentIndex)); // task to write byte into the buffer. This could be added to a queu            
        }
        private void writeNodeBuffer(byte[] wbuffer, int index)
        {
            
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            //Console.WriteLine("writing to: " + currentIndex);
            buffer[index].data = (byte[])wbuffer.Clone();            
            buffer[index].readFlags = 0;
            tcs.SetResult(true); 
                              
        }
        private bool isWriteReady()
        {
            if (allReadFlagsValue == buffer[currentIndex].readFlags) { return true; }
            return false;
        }
        private bool isReadReady(ref int index)
        {
            //Console.WriteLine(currentIndex);                
            if(currentIndex > -1)      
                if (checkFlag( buffer[currentIndex].readFlags, ref index )) { return true; }           
            return false;            
        }
        private bool bufferIsFull()
        {
            if (buffer[currentIndex].readFlags < readerCount) { return false; }
            return true;
        }
        private void setFlag(ref int data,ref int pos)
        {
            //Console.WriteLine("setFlag");
            int tmp = 1;
            tmp = tmp << pos;
            lock(readCountLock)
            {
                data = tmp | data;
            }                         
        }
        private bool checkFlag(int data, ref int pos)
        {
            lock (checkFlagLock)
            {
                int tmp = data;
                tmp = tmp | (1 << pos);            
                return !(data == tmp);
            }
        }
        public void readerDetatch()
        {
            int index = indexOfCurrentReader();
            //Array.

        }
        public void debugOutput()
        {
            Console.WriteLine(readerDictionary.Keys);
        }
        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                    writer.Dispose();
                    foreach (Task r in reader)
                    {
                        r.Dispose();
                    }
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~RingBuffer() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
