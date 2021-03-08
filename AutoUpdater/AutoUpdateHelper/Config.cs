using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using AutoUpdater.AutoUpdateHelper;

namespace AutoUpdater
{
    [Serializable]
    [XmlRoot("local")]
    public class Config
    {
        [XmlElement("serverUrl")]
        public string ServerUrl { get; set; } = string.Empty;
        [XmlArray("files")]
        [XmlArrayItem("file")]
        public List<LocalFile> UpdateFileList { get; set; }
        public static void SaveConfig(string path, Config cfg)
        {
            var xml = MyXmlConvert.SerializeObject(cfg);
            File.WriteAllText(path, xml, Encoding.UTF8);
        }
        public static Config LoadConfig(string path)
        {
            var xml = File.ReadAllText(path, Encoding.UTF8);
            return MyXmlConvert.DeserializeObject<Config>(xml);
        }

    }

}
