﻿using System;
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
        private string resumeFile = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @".AutoSplit\resume.dat";
        private FileSystemWatcher fswSplitDir;
        private List<string> incompleteSplits = new List<string>();
        public AutoSplitSrv()
        {
            InitializeComponent();
            fswSplitDir = new FileSystemWatcher("C:\\Users\\paborza.AUTH\\OneDrive for Business");
            fswSplitDir.Created += new FileSystemEventHandler(this.OnFileCreated);            
            Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @".AutoSplit");
    

        }

        protected override void OnStart(string[] args)
        {
            fswSplitDir.EnableRaisingEvents = true;
        }

        protected override void OnStop()
        {
            FileStream fs = new FileStream(resumeFile, FileMode.Create, FileAccess.Write);
            BinaryFormatter bf = new BinaryFormatter();
            bf.Serialize(fs, incompleteSplits);
            fswSplitDir.EnableRaisingEvents = false;
        }
        public void OnFileCreated(object sender, FileSystemEventArgs e)
        {
            SplitTask(e.FullPath).Start();
        }
        private async Task SplitTask(string path)
        {
            FileStream fsTest = await getFileStream(path);
            incompleteSplits.Add(path);
            fsTest.Lock(0, fsTest.Length);
            if (fsTest.Length > 1470)
            {
                FileSplitter fs = new FileSplitter(fsTest, path);
                fs.Split();
            }
            incompleteSplits.RemoveAt(incompleteSplits.IndexOf(path));
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
