using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.Cryptography;
using System.Xml.Serialization;
using System.Xml;
using System.Xml.Schema;
using System.Collections;
using System.Collections.Concurrent;

namespace SpliceUtilities
{    
    class FileSplitter
    {
        FileMetaData InputFileMeta = new FileMetaData();
        string _chunkPath;
        const int BUFFER_SIZE = 4096 * 1024;
        private byte[] buffer = new Byte[BUFFER_SIZE];
        private static uint chunksize = 524288000  ;

        private FileStream fsInputFile;

        public FileSplitter(string inputFile, string chunkPath)
        {
            _chunkPath = chunkPath;        
            fsInputFile = new FileStream(inputFile, FileMode.Open);
            InputFileMeta.Name = FileMetaData.getFilenameFromPath(fsInputFile.Name);                                              
            InputFileMeta.FileSize = (ulong)fsInputFile.Length;       
        }
        public FileSplitter(FileStream inputStream, string chunkPath)
        {
            _chunkPath = chunkPath;
            fsInputFile = inputStream;
            InputFileMeta.Name = FileMetaData.getFilenameFromPath(fsInputFile.Name);
            InputFileMeta.FileSize = (ulong)fsInputFile.Length;
        }

        public void Split()
        {            
            RingBuffer ringbuff = new RingBuffer(32, 3);
            int bytesread;            
            ushort chunkCount = (ushort)Math.Ceiling((float)fsInputFile.Length / (float)(chunksize));
            InputFileMeta.NumParts = chunkCount;
            int sleepcounter = 0;
            Task.Run( () =>
            {
                do
                {
                    bytesread = fsInputFile.Read(buffer, 0, buffer.Length);
                    ringbuff.writeNext(buffer);
                    sleepcounter++;
                    //if (sleepcounter == 16)
                    //    await Task.Delay(1000);
                } while (bytesread > 0);
            });

            Task splitter = new Task(() =>
            {
                byte[] tmpbuff;// = new byte[BUFFER_SIZE];
                uint chunkBytesWritten = 0;
                long totalBytesWritten = 0;
                uint currentChunkSize = chunksize;
                FileStream fsChunk;
                for (int i = 0; i < chunkCount; i++)
                {
                    fsChunk = new FileStream("C:\\Users\\paborza.AUTH\\OneDrive for Business\\chunk" + i + ".dat", FileMode.Create, FileAccess.Write);
                    if (i == chunkCount - 1)
                        currentChunkSize = (uint)(fsInputFile.Length - totalBytesWritten);
                        //currentChunkSize = (uint)fsInputFile.Length - (uint)(chunksize * (chunkCount - 1));  
                    tmpbuff = new byte[BUFFER_SIZE];
                    while (chunkBytesWritten < currentChunkSize)
                    {
                        tmpbuff = ringbuff.readNext();
                        fsChunk.Write(tmpbuff, 0, tmpbuff.Length);
                        chunkBytesWritten += (uint)tmpbuff.Length;                    
                    }

                    if (!InputFileMeta.Part.Exists((uint)i))
                        InputFileMeta.Part.Add((uint)i);
                    InputFileMeta.Part[i].ChunkSize = chunkBytesWritten;
                    //InputFileMeta.Part[i].Path = fsChunk.Name;
                    InputFileMeta.Part[i].FileName = FileMetaData.getFilenameFromPath(fsChunk.Name);

                    fsChunk.Flush(true);
                    Console.WriteLine("chunk file length " + fsChunk.Length + " currentchunksize " + currentChunkSize);
                    fsChunk.Close();
                    totalBytesWritten += chunkBytesWritten;
                    chunkBytesWritten = 0;
                }
                InputFileMeta.ChunkSize = InputFileMeta.Part[0].ChunkSize;
            });
            Task chunkMD5Task = new Task(() =>
            {
                MD5 chunkMD5;
                byte[] tmpbuff = new byte[BUFFER_SIZE];
                uint currentChunkSize = chunksize;
                for (int i = 0; i < chunkCount; i++)
                {
                    chunkMD5 = MD5.Create();
                    chunkMD5.Initialize();
                    if (i == chunkCount - 1)
                        currentChunkSize = (uint)fsInputFile.Length - (uint)(chunksize * (chunkCount - 1));
                    for (int ia = 0; ia < (currentChunkSize / tmpbuff.Length) - 1; ia++)
                    {
                        tmpbuff = ringbuff.readNext();
                        chunkMD5.TransformBlock(tmpbuff, 0, tmpbuff.Length, null, 0);
                    }
                    tmpbuff = ringbuff.readNext();
                    chunkMD5.TransformFinalBlock(tmpbuff, 0, tmpbuff.Length);

                    if (!InputFileMeta.Part.Exists((uint)i))
                        InputFileMeta.Part.Add((uint)i);
                    InputFileMeta.Part[i].Md5hash = chunkMD5.Hash;
                    Console.WriteLine(currentChunkSize);
                    Console.WriteLine(BitConverter.ToString(chunkMD5.Hash));
                }

            });
            Task wholeMD5Task = new Task(() =>
            {
                byte[] tmpbuff = new byte[BUFFER_SIZE];
                MD5 wholeMD5 = MD5.Create();
                for (long ia = 0; ia < (fsInputFile.Length / tmpbuff.Length) - 1; ia++)
                {
                    tmpbuff = ringbuff.readNext();
                    wholeMD5.TransformBlock(tmpbuff, 0, tmpbuff.Length, null, 0);
                }
                tmpbuff = ringbuff.readNext();
                wholeMD5.TransformBlock(tmpbuff, 0, tmpbuff.Length, null, 0);
                wholeMD5.TransformFinalBlock(tmpbuff, 0, tmpbuff.Length);
                InputFileMeta.MD5Hash = wholeMD5.Hash;
            });
            chunkMD5Task.Start();
            wholeMD5Task.Start();
            splitter.Start();
            splitter.Wait();
            chunkMD5Task.Wait();
            wholeMD5Task.Wait();
            InputFileMeta.Save(_chunkPath);
        }
        private string arrayToHex(byte[] ar)
        {
            string r = "";
            for (int i = 0; i < ar.Length; i++)
                r = r.Insert(i * 2, ar[i].ToString("X2"));
            return r;
        }
    } 
}
