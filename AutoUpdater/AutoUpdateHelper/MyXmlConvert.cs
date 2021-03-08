using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace AutoUpdater.AutoUpdateHelper
{
    public class MyXmlConvert
    {
        /// <summary>
        /// xml序列化
        /// </summary>
        /// <param name="strobj"></param>
        public static string SerializeObject<T>(T obj)
        {
            try
            {
                var _namespace = new XmlSerializerNamespaces();
                _namespace.Add(string.Empty, string.Empty);

                XmlSerializer serializer = new XmlSerializer(obj.GetType());
                var _xmlSetting = new XmlWriterSettings
                {
                    Encoding = Encoding.UTF8,//文本编码， 
                    Indent = false, //是否缩进
                    OmitXmlDeclaration = true,//是否省略Xml声明
                    NewLineHandling = NewLineHandling.Replace,//是否压缩
                };


                using (StringWriter _writer = new Utf8StringWriter())
                {
                    using (var _xmlWriter = XmlWriter.Create(_writer, _xmlSetting))
                    {
                        serializer.Serialize(_xmlWriter, obj, _namespace);
                        return _writer.ToString();
                    }
                }

            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// 反序列化
        /// </summary>
        /// <returns></returns>
        public static T DeserializeObject<T>(string _xml)
        {
            try
            {
                T obj;
                using (MemoryStream _ms = new MemoryStream(Encoding.UTF8.GetBytes(_xml)))
                {
                    using (XmlReader _reader = XmlReader.Create(_ms))
                    {
                        XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                        obj = (T)xmlSerializer.Deserialize(_reader);
                    }
                }
                return obj;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }

    /// <summary>
    /// XML序列化指定编码类
    /// </summary>
    public class Utf8StringWriter : StringWriter
    {
        public override Encoding Encoding
        {
            get
            {
                return Encoding.UTF8;
            }
        }
    }
}
