using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.IO;
using System.Diagnostics;
using System.Security.Cryptography;

namespace CreateXmlTools
{
    public partial class FormMain : Form
    {
        public FormMain()
        {
            InitializeComponent();
            txtWebUrl.Text = "localhost:80";
            txtWebUrl.ForeColor = Color.Gray;
        }

        //获取当前目录
        //string currentDirectory = AppDomain.CurrentDomain.BaseDirectory;
        string currentDirectory = System.Environment.CurrentDirectory;
        //服务端xml文件名称
        string serverXmlName = "Remote.xml";
        //更新文件URL前缀
        string url = string.Empty;

        void CreateXml()
        {
            //创建文档对象
            XmlDocument doc = new XmlDocument();
            //创建根节点
            XmlElement root = doc.CreateElement("updateFiles");
            //头声明
            XmlDeclaration xmldecl = doc.CreateXmlDeclaration("1.0", "utf-8", null);
            doc.AppendChild(xmldecl);
            DirectoryInfo dicInfo = new DirectoryInfo(currentDirectory);
            
            //调用递归方法组装xml文件
            PopuAllDirectory(doc, root, dicInfo);
            //追加节点
            doc.AppendChild(root);
            //保存文档
            doc.Save(serverXmlName);
        }

        //递归组装xml文件方法
        private void PopuAllDirectory(XmlDocument doc, XmlElement root, DirectoryInfo dicInfo)
        {
            foreach (FileInfo f in dicInfo.GetFiles())
            {
                //排除当前目录中生成xml文件的工具文件
                if (f.Name != "CreateXmlTools.exe" && f.Name != serverXmlName && !f.Name.ToLower().EndsWith(".pdb"))
                {
                    string path = dicInfo.FullName.Replace(currentDirectory, "").Replace("\\", "/");
                    string folderPath=string.Empty;
                    if (path != string.Empty)
                    {
                        folderPath = path.TrimStart('/') + "/";
                    }
                    XmlElement child = doc.CreateElement("file");
                    //文件路径
                    child.SetAttribute("path", folderPath + f.Name);
                    //文件下载地址
                    child.SetAttribute("url", url + path + "/" + f.Name);
                    //文件版本
                    child.SetAttribute("lastver", FileVersionInfo.GetVersionInfo(f.FullName).FileVersion);
                    //文件大小
                    child.SetAttribute("size", f.Length.ToString());
                    //child.SetAttribute("needRestart", "false");
                    //唯一码
                    child.SetAttribute("version", GetFileMD5(f.FullName));
                    root.AppendChild(child);
                }
            }

            foreach (DirectoryInfo di in dicInfo.GetDirectories())
                PopuAllDirectory(doc, root, di);
        }
        /// <summary>
        /// 获取文件的MD5码
        /// </summary>
        /// <param name="fileName">传入的文件名（含路径及后缀名）</param>
        /// <returns></returns>
        public string GetFileMD5(string filePath)
        {
            try
            {
                using (FileStream file = new FileStream(filePath, System.IO.FileMode.Open))
                {
                    using (MD5 md5 = new MD5CryptoServiceProvider())
                    {
                        byte[] retVal = md5.ComputeHash(file);
                        file.Close();
                        StringBuilder sb = new StringBuilder();
                        for (int i = 0; i < retVal.Length; i++)
                        {
                            sb.Append(retVal[i].ToString("x2"));
                        }
                        return sb.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
            }
        }
        private void btnCreate_Click(object sender, EventArgs e)
        {
            url = "http://" + txtWebUrl.Text.Trim();
            CreateXml();
            ReadXml();
        }

        private void ReadXml()
        {
            rtbXml.ReadOnly = true;
            if (File.Exists(serverXmlName))
            {
                rtbXml.Text = File.ReadAllText(serverXmlName);
            }
        }

        private void txtWebUrl_Enter(object sender, EventArgs e)
        {
            txtWebUrl.ForeColor = Color.Black;
            if (txtWebUrl.Text.Trim() == "localhost:8011")
            {
                txtWebUrl.Text = string.Empty;
            }
        }
        
    }
}
