using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileSplitter
{
    class FileCombiner
    {
        FileMetaData FileMeta;
        public FileCombiner(string metaXml)
        {
            FileMeta = new FileMetaData(metaXml);
        }
        public void combine()
        {

            RingBuffer ringbuff = new RingBuffer(32, 1);
            Task.Run(() =>
            {
                int bytesread;
                int sleepcounter = 0;
                byte[] buffer = new Byte[4096 * 1024];
                FileStream fsChunk;
                FileStream test = new FileStream("C:\\Users\\paborza.AUTH\\OneDrive for Business\\test2a.iso", FileMode.Create, FileAccess.Write);
                foreach (FileMetaData.PartMeta part in FileMeta.Part)
                {
                    fsChunk = new FileStream("C:\\Users\\paborza.AUTH\\OneDrive for Business\\" + part.FileName, FileMode.Open, FileAccess.Read);

                    do
                    {
                        bytesread = fsChunk.Read(buffer, 0, buffer.Length);
                        ringbuff.writeNext(buffer);
                        test.Write(buffer, 0, buffer.Length);
                        sleepcounter++;
                        //if (sleepcounter == 16)
                        //    await Task.Delay(1000);

                    } while (bytesread > 0);
                    fsChunk.Flush();
                    fsChunk.Close();
                }
            });

            Task combiner = new Task(() =>
            {
                byte[] tmpbuff = new byte[4096 * 1024];
                FileStream reformed = new FileStream("C:\\Users\\paborza.AUTH\\OneDrive for Business\\test2.iso", FileMode.Create, FileAccess.Write);
                ulong byteswritten = 0;
                do
                {
                    tmpbuff = ringbuff.readNext();
                    reformed.Write(tmpbuff, 0, tmpbuff.Length);
                    byteswritten += (ulong)tmpbuff.Length;
                } while (byteswritten < FileMeta.FileSize);
                reformed.Flush();
                reformed.Close();
            });
            combiner.Start();
            combiner.Wait();
        }

    }
}
