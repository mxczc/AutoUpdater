using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace AutoUpdater
{
    [Serializable]
    public class LocalFile
    {
        [XmlAttribute("path")]
        public string Path { get; set; } = "";

        [XmlAttribute("lastver")]
        public string LastVer { get; set; } = "";

        [XmlAttribute("size")]
        public int Size { get; set; } = 0;
        [XmlAttribute("version")]
        public string Version { get; set; } = "";

        public LocalFile(string path, string ver, int size, string versionid)
        {
            this.Path = path;
            this.LastVer = ver;
            this.Size = size;
            this.Version = versionid;
        }
        public LocalFile() { }
    }
}
