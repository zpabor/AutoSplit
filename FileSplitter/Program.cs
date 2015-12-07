using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace FileSplitter
{
    class Program
    {
        static void Main(string[] args)
        {
            /*
            FileSystemWatcher fswOneDriveFolder = new FileSystemWatcher("C:\\Users\\paborza.AUTH\\OneDrive for Business");
            fswOneDriveFolder.Created += new FileSystemEventHandler(handler.OnCreation);
            fswOneDriveFolder.EnableRaisingEvents = true;
            while (true)
            {
                
                Thread.Sleep(1000);
            }
            */


            //FileSplitter fs = new FileSplitter("C:\\Users\\paborza.AUTH\\OneDrive for Business\\test2.iso");
            //fs.Split();
            FileCombiner fc = new FileCombiner(@"C:\test.xml");
            fc.combine();

            Console.ReadLine();
            /*
            FileStream xfs = new FileStream("C:\\Users\\paborza.AUTH\\OneDrive for Business\\test2.iso", FileMode.Open);
            byte[] a = new byte[4096];
            byte[] ba1 = new byte[4096];
            byte[] ba2 = new byte[4096];
            List<byte> listA = new List<byte>();
            List<byte> listB = new List<byte>();
            List<byte> master = new List<byte>();
            //ba[0] = 0;
            //ba2[0] = 22;
            RingBuffer x = new RingBuffer(32, 2);
            long ticks = 0;
            int tickcycle = 1;
            System.Diagnostics.Stopwatch sw = new System.Diagnostics.Stopwatch();

            Task read1 = new Task(() =>
           {
               for (int ix = 0; ix < 100000; ix++)
               {
                   //Console.WriteLine("READ Main " + Task.CurrentId);
                   ba1 = x.readNext();
                   listA.Add(ba1[0]);

                   //Console.WriteLine("READ Main " + Task.CurrentId + " --- iteration: " + ix + " bytes read: " + ba1.Length);                                                      
                }                               
            });
            Task read2 = new Task(() =>
            {
                for (int ix = 0; ix < 100000; ix++)
                {
                    //Console.WriteLine("READ Main " + Task.CurrentId);

                    //sw.Start();
                    ba2 = x.readNext();
                    //sw.Stop();
                    //ticks = (ticks + sw.ElapsedTicks) / tickcycle;
                    //tickcycle++;
                    listB.Add(ba2[0]);
                    //Console.WriteLine("READ Main " + Task.CurrentId +  " --- iteration: " + ix + " bytes read: " + ba2.Length);
                    //Console.Clear();                
                }

            });



            Task.Run(() =>
            {
                int bytesread = 0;
                do
                {
                    bytesread = xfs.Read(a, 0, a.Length);
                    master.Add(a[0]);
                    sw.Start();
                    x.writeNext(a);
                    sw.Stop();
                    ticks = (ticks + sw.ElapsedTicks) / tickcycle;
                    tickcycle++;
                    //xfs.Position += a.Length;
                    //Console.WriteLine("WRITE --- bytes read: " + bytesread + "position: " + xfs.Position + " file length: " + xfs.Length + " data: " + a[0]);
                } while (bytesread > 0);
            });
            
            read2.Start();
            read1.Start();
            read1.Wait();
            read2.Wait();
           
            Console.WriteLine(ticks);
            for(int i = 0; i < listA.Count; i++ )
            {
                if (!(listA[i] == master[i]))
                    Console.WriteLine(i +" err");
                //Console.WriteLine(i + "master " + (int)master[i] + " listA " + listA[i] + " listB " + listB[i]);
            }
            
            x.debugOutput();
            Console.ReadLine();
            
            Thread.Sleep(10000);
            
            //xfs.Read(ba2, 0, 0);
            */            
        }                
    }    
}
