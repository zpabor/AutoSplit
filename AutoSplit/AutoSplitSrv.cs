using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using SpliceUtilities;
using System.Runtime.Serialization.Formatters.Binary;

namespace AutoSplitSrv
{
    public partial class AutoSplitSrv : ServiceBase
    {
        private string resumeFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\AutoSplit\resume.dat";
        private FileSystemWatcher fswSplitDir;
        private List<string> jobs = new List<string>();
        public AutoSplitSrv()
        {
            InitializeComponent();
            fswSplitDir = new FileSystemWatcher("C:\\Users\\paborza.AUTH\\OneDrive for Business");
            fswSplitDir.Created += new FileSystemEventHandler(this.OnFileCreated);            
            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\AutoSplit");
    

        }

        protected override void OnStart(string[] args)
        {
            //System.Threading.Thread.Sleep(20000);
            FileStream fs = new FileStream(resumeFile, FileMode.OpenOrCreate, FileAccess.Read);
            BinaryFormatter bf = new BinaryFormatter();
            if(fs.Length > 0)
                jobs = (List<string>)bf.Deserialize(fs);
            fswSplitDir.EnableRaisingEvents = true;
            
            if (jobs.Count > 0 & jobs != null)
            {
                foreach (string job in jobs)
                {
                    SplitTask(job).Start();
                }
            }
            fs.Close();
        }

        protected override void OnStop()
        {
            FileStream fs = new FileStream(resumeFile, FileMode.Create, FileAccess.Write);
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(fs, jobs);
            fswSplitDir.EnableRaisingEvents = false;
            fs.Close();
        }
        public void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            Task.Run(()=> SplitTask(e.FullPath));
        }
        private async Task SplitTask(string path)
        {
            FileStream fsTest = await getFileStream(path);
            jobs.Add(path);
            fsTest.Lock(0, fsTest.Length);
            if (fsTest.Length > FileSplitter.MAX_FILE_SIZE)
            {
                FileSplitter fsplit = new FileSplitter(fsTest, path);
                fsplit.Split();
            }
            jobs.RemoveAt(jobs.IndexOf(path));
        } 
        private async Task<FileStream> getFileStream(string filepath)
        {
            FileStream fsTest = null;
            while (fsTest == null)
            {
                try
                {
                    fsTest = new FileStream(filepath, FileMode.Open, FileAccess.ReadWrite);                                                                                                               
                }
                catch (Exception)
                {
                    await Task.Delay(100);                    
                }                               
            }
            return fsTest;          
        }        
    }
}
