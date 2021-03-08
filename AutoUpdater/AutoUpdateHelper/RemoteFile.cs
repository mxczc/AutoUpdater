using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace AutoUpdater
{
    public class RemoteFile
    {
        public string Path { get; } = "";

        public string Url { get; } = "";

        public string LastVer { get; } = "";

        public int Size { get; } = 0;

        //public bool NeedRestart { get; } = false;

        public string Verison { get; } = "";

        public RemoteFile(XmlNode node)
        {
            this.Path = node.Attributes["path"].Value;
            this.Url = node.Attributes["url"].Value;
            this.LastVer = node.Attributes["lastver"].Value;
            this.Size = Convert.ToInt32(node.Attributes["size"].Value);
            //this.NeedRestart = Convert.ToBoolean(node.Attributes["needRestart"].Value);
            this.Verison = node.Attributes["version"].Value;
        }
    }
}
