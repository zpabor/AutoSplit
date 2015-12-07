using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FileSplitter
{

    internal class FileMetaData
    {

        #region Embedded classes
        [Serializable()]
        internal class PartCollection : System.Collections.ICollection, IEnumerable
        {
            private List<PartMeta> _part = new List<PartMeta>();
            private ConcurrentDictionary<int, PartMeta> _partDict = new ConcurrentDictionary<int, PartMeta>();
            private object addlock = new object();
            private List<uint?> partNumbers = new List<uint?>();
            XmlDocument XmlDoc;
            public PartMeta this[int index]
            {
                get
                {
                    return _part[index];
                }
            }
            public PartCollection(XmlDocument xmldoc)
            {
                XmlDoc = xmldoc;
            }
            public void Sort()
            {
                lock (addlock)
                {
                    _part.Sort();
                }
            }
            public bool Exists(uint? number)
            {
                if (_part.Contains(new PartMeta(XmlDoc, number)))
                    return true;
                return false;
            }
            private void _add(PartMeta newpart)
            {
                lock (addlock)
                {
                    if (_part.Contains(newpart))
                        _part.Add(newpart);
                }
            }

            public void Add(uint number)
            {
                lock (addlock)
                {
                    long i = (long)number - _part.Count + 1;

                    if (i < 1)
                        return;

                    while (i > 1)
                    {
                        _part.Add(new PartMeta(XmlDoc, (uint)_part.Count));
                        i = (long)number - _part.Count + 1;
                    }

                    _part.Add(new PartMeta(XmlDoc, number));
                }
            }

            #region ICollection
            public int Count
            {
                get
                {
                    return ((ICollection)_part).Count;
                }
            }

            public bool IsSynchronized
            {
                get
                {
                    return ((ICollection)_part).IsSynchronized;
                }
            }

            public object SyncRoot
            {
                get
                {
                    return ((ICollection)_part).SyncRoot;
                }
            }

            public void CopyTo(Array array, int index)
            {
                ((ICollection)_part).CopyTo(array, index);
            }

            public IEnumerator GetEnumerator()
            {
                return ((ICollection)_part).GetEnumerator();
            }
            #endregion
        }
        [Serializable()]
        internal class PartMeta : IComparable
        {
            private XmlElement partXML;

            private uint? _partNumber;
            public uint? PartNumber
            {
                get
                {
                    return _partNumber;
                }
                set
                {
                    _partNumber = value;
                    partXML.SetAttribute(partnumber_Xmlatt, _partNumber.ToString());
                }
            }

            byte[] _md5hash;
            public byte[] Md5hash
            {
                get
                {
                    return _md5hash;
                }
                set
                {
                    _md5hash = value;
                    partXML.SetAttribute(md5hash_Xmlatt, Convert.ToBase64String(_md5hash));
                }
            }

            private uint _chunkSize;
            public uint ChunkSize
            {
                get
                {
                    return _chunkSize;
                }
                set
                {
                    _chunkSize = value;
                    partXML.SetAttribute(chuncksize_Xmlatt, _chunkSize.ToString());
                }
            }

            private string _partpath;
            public string Path
            {
                get
                {
                    return _partpath;
                }
                set
                {
                    _partpath = value;
                    partXML.SetAttribute(path_Xmlatt, _partpath);
                }
            }

            private string _filename;
            public string FileName
            {
                get
                {
                    return _filename;
                }
                set
                {
                    _filename = value;
                    partXML.SetAttribute(name_Xmlatt, _filename);
                }
            }

            public PartMeta(XmlDocument XmlDoc)
            {
                partXML = XmlDoc.CreateElement("part");
            }
            public PartMeta(XmlDocument XmlDoc, uint? number)
            {
                partXML = XmlDoc.CreateElement("part");
                PartNumber = number;
            }
            public PartMeta(XmlDocument XmlDoc, uint? number, byte[] md5hash)
            {
                partXML = XmlDoc.CreateElement("part");
                PartNumber = number;
                Md5hash = md5hash;

            }



            public int CompareTo(object obj)
            {
                PartMeta tmppart = (PartMeta)obj;
                return (int)(_partNumber - tmppart.PartNumber);
            }

            public XmlElement getXmlElement()
            {
                return partXML;
            }
        } 
        #endregion
        #region Constants

        //main file attributes
        const string chuncksize_Xmlatt = "chunksize";
        const string filename_Xmlatt = "filename";
        const string filesize_Xmlatt = "filesize";
        const string md5hash_Xmlatt = "md5";
        const string name_Xmlatt = "name";
        const string numparts_Xmlatt = "numparts";
        const string path_Xmlatt = "path";
        //file part attributes
        const string partnumber_Xmlatt = "number";

        #endregion Constants

        #region Properties and private members

        public PartCollection Part;

        private XmlDocument FileMetaXmlDoc = new XmlDocument();
        private XmlElement root;

        private string _filepath = "";
        public string Path
        {
            get { return _filepath; }
            set
            {
                _filepath = value;
                root.SetAttribute(path_Xmlatt, _filepath);
            }
        }

        private ulong _filesize;
        public ulong FileSize
        {
            get
            {
                return _filesize;
            }
            set
            {
                _filesize = value;
                root.SetAttribute(filesize_Xmlatt, _filesize.ToString());
            }
        }

        private uint _chunksize;
        public uint ChunkSize
        {
            get
            {
                return _chunksize;
            }
            set
            {
                _chunksize = value;
                root.SetAttribute(chuncksize_Xmlatt, _chunksize.ToString());
            }
        }

        private string _path = "";
        public string FilePath
        {
            get
            {
                return _path;
            }
            set
            {
                _path = value;
                root.SetAttribute(path_Xmlatt, _path);
            }
        }

        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                root.SetAttribute(name_Xmlatt, _name);
            }
        }

        private byte[] _md5hash;
        public byte[] MD5Hash
        {
            get
            {
                return _md5hash;
            }
            set
            {
                _md5hash = value;
                root.SetAttribute(md5hash_Xmlatt, Convert.ToBase64String(_md5hash));
            }
        }

        private ushort _numParts;
        public ushort NumParts
        {
            get
            {
                return _numParts;
            }
            set
            {
                _numParts = value;
                root.SetAttribute(numparts_Xmlatt, _numParts.ToString());
            }
        }


        #endregion
        #region Constructors
        public FileMetaData()
        {
            //FileMetaXmlDoc = new XmlDocument();
            XmlDeclaration dec = FileMetaXmlDoc.CreateXmlDeclaration("1.0", null, null);
            FileMetaXmlDoc.AppendChild(dec);
            root = FileMetaXmlDoc.CreateElement("SplitFile");
            FileMetaXmlDoc.AppendChild(root);
            XmlElement partsElement = FileMetaXmlDoc.CreateElement("parts");

            Part = new PartCollection(FileMetaXmlDoc);
        }
        public FileMetaData(string xmlfile)
        {
            Load(xmlfile);
        }
        #endregion Constructors
        #region Methods
        
        public void Save(string path)
        {
            UpdateXml();
            FileMetaXmlDoc.Save(path);
        }

        private void UpdateXml()
        {
            Part.Sort();
            foreach (XmlElement e in root)
                root.RemoveChild(e);

            foreach (PartMeta i in Part)
            {
                root.AppendChild(i.getXmlElement());
            }
        }

        public void Load(string xmlpath)
        {            
            FileMetaXmlDoc.Load(xmlpath);
            root = FileMetaXmlDoc.DocumentElement;
            Part = new PartCollection(FileMetaXmlDoc);


            _chunksize = Convert.ToUInt32(root.GetAttribute(chuncksize_Xmlatt));
            //_filepath = root.GetAttribute(filename_Xmlatt);
            _filesize = Convert.ToUInt64(root.GetAttribute(filesize_Xmlatt));
            _name = root.GetAttribute(name_Xmlatt);
            _md5hash = Convert.FromBase64String(root.GetAttribute(md5hash_Xmlatt));
            _numParts = Convert.ToUInt16(root.GetAttribute(numparts_Xmlatt));
            _path = root.GetAttribute(path_Xmlatt);

            int n;
            foreach (XmlElement e in root.ChildNodes)
            {
                Part.Add(Convert.ToUInt32(e.GetAttribute(partnumber_Xmlatt)));
                n = Convert.ToInt32(e.GetAttribute(partnumber_Xmlatt));
                Part[n].ChunkSize = Convert.ToUInt32(e.GetAttribute(chuncksize_Xmlatt));
                //Part[n].Path = e.GetAttribute(filename_Xmlatt);
                Part[n].FileName = e.GetAttribute(name_Xmlatt);
                Part[n].Md5hash = Convert.FromBase64String(e.GetAttribute(md5hash_Xmlatt));
                Part[n].PartNumber = (uint)n;
            }
        }
        public static string getFilenameFromPath(string path)
        {
            char[] d = { '\\' };
            String[] f = path.Split(d);
            return f[f.Length - 1];
        }
        #endregion
    }
}
