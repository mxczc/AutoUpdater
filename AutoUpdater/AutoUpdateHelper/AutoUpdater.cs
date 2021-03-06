using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Xml;
using System.Xml.Serialization;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using AutoUpdater;

namespace AutoUpdater
{

    /// <summary>
    /// 自动更新程序
    /// </summary>
    public class AutoUpdater : IAutoUpdater
    {
        //显示委托
        public delegate void ShowHandler();
        //显示事件
        public event ShowHandler OnShow;
        //基础配置
        private Config config = null;
        //是否需要重启主程序
        //private bool bNeedRestart = false;
        //是否下载
        private bool bDownload = false;
        //下载文件列表
        List<DownloadFileInfo> downloadFileList = null;
        public AutoUpdater()
        {
            if (!File.Exists(ConstFile.FILENAME))
            {
                CreateCfg();
            }
            config = Config.LoadConfig(ConstFile.FILENAME);
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        private void CreateCfg()
        {
            try
            {
                Config config = new Config()
                {
                    ServerUrl = $"https://www.cisharp.com/packet/Remote.xml",
                    UpdateFileList = new List<LocalFile>()
                };

                Config.SaveConfig("acfg.config", config);
            }

            catch (Exception ex)
            {
                MessageBox.Show("配置文件生成失败，请检查程序目录读写权限！", "提示");
            }
        }

        /// <summary>
        /// 开始验证更新
        /// </summary>
        public void Update()
        {
            //程序版本文件
            Dictionary<string, RemoteFile> listRemotFile = ParseRemoteXml(config.ServerUrl);
            //程序文件列表
            List<DownloadFileInfo> downloadList = new List<DownloadFileInfo>();
            //与本地文件MD5校验
            foreach (LocalFile file in config.UpdateFileList)
            {
                if (listRemotFile.ContainsKey(file.Path))
                {
                    RemoteFile rf = listRemotFile[file.Path];
                    string v1 = rf.Verison;
                    string v2 = file.Version;
                    if (v1 != v2)
                    {
                        downloadList.Add(new DownloadFileInfo(rf.Url, rf.Path, rf.LastVer, rf.Size, rf.Verison));
                        file.Path = rf.Path;
                        file.LastVer = rf.LastVer;
                        file.Size = rf.Size;
                        file.Version = rf.Verison;
                        bDownload = true;
                    }

                    listRemotFile.Remove(file.Path);
                }
            }

            foreach (RemoteFile file in listRemotFile.Values)
            {
                downloadList.Add(new DownloadFileInfo(file.Url, file.Path, file.LastVer, file.Size, file.Verison));
                bDownload = true;
                config.UpdateFileList.Add(new LocalFile(file.Path, file.LastVer, file.Size, file.Verison));
            }
            downloadFileList = downloadList;

            //判断是否需要下载
            if (bDownload)
            {
                //关闭主程序进程
                OperProcess op = new OperProcess();
                op.InitUpdateEnvironment();

                OnShow?.Invoke();

                //调用窗体开始更新
                StartDownload(downloadList);
            }
        }

        /// <summary>
        /// 回滚下载
        /// </summary>
        public void RollBack()
        {
            foreach (DownloadFileInfo file in downloadFileList ?? new List<DownloadFileInfo>())
            {
                string tempUrlPath = CommonUnitity.GetFolderUrl(file);
                string source = string.Empty;
                try
                {
                    if (!string.IsNullOrEmpty(tempUrlPath))
                    {
                        source = Path.Combine(CommonUnitity.SystemBinUrl + tempUrlPath.Substring(1), file.FileName);
                    }
                    else
                    {
                        source = Path.Combine(CommonUnitity.SystemBinUrl, file.FileName);
                    }
                    if (source.EndsWith("_"))
                        source = source.Substring(0, source.Length - 1);

                    MoveFolderToOld(source + ".old", source);
                }
                catch
                {

                }
            }
        }

        /// <summary>
        /// 文件拷贝到目标文件夹
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        private void MoveFolderToOld(string source, string dest)
        {
            if (File.Exists(source) && File.Exists(dest))
            {
                //还原文件
                File.Copy(source, dest, true);
                //删除备份
                File.Delete(source);
            }
        }

        /// <summary>
        /// 开始下载
        /// </summary>
        /// <param name="downloadList"></param>
        private void StartDownload(List<DownloadFileInfo> downloadList)
        {
            FrmDownloadProgress dp = new FrmDownloadProgress(downloadList);
            if (dp.ShowDialog() == DialogResult.OK)
            {
                if (DialogResult.Cancel == dp.ShowDialog())
                {
                    //更新失败，回滚
                    RollBack();
                    return;
                }
                //更新成功
                Config.SaveConfig(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConstFile.FILENAME), config);
                //删除更新临时文件
                Directory.Delete(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConstFile.TEMPFOLDERNAME), true);
                //删除备份文件
                DeleteOld(AppDomain.CurrentDomain.BaseDirectory);
                //弹出提示
                MessageBox.Show(ConstFile.APPLYTHEUPDATE, ConstFile.MESSAGETITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
                //重启程序
                CommonUnitity.RestartApplication();
            }
        }

        /// <summary>
        /// 删除备份文件
        /// </summary>
        /// <param name="path"></param>
        private void DeleteOld(string path)
        {
            try
            {
                DirectoryInfo di = new DirectoryInfo(path);
                //遍历该路径下的所有文件
                foreach (FileSystemInfo fi in di.GetFileSystemInfos())
                {
                    if (fi is DirectoryInfo)
                    {
                        DeleteOld(fi.FullName);
                    }
                    else
                    {
                        string exname = fi.Name.Substring(fi.Name.LastIndexOf(".") + 1);//得到后缀名
                                                                                        //判断当前文件后缀名是否与给定后缀名一样
                        if (exname.ToLower() == "old")
                        {
                            File.Delete(path + "\\" + fi.Name);//删除当前文件
                        }
                    }
                }

            }
            catch
            {
                return;
            }
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        private Dictionary<string, RemoteFile> ParseRemoteXml(string url)
        {
            XmlDocument document = new XmlDocument();
            document.Load(url);

            Dictionary<string, RemoteFile> list = new Dictionary<string, RemoteFile>();
            foreach (XmlNode node in document.DocumentElement.ChildNodes)
            {
                list.Add(node.Attributes["path"].Value, new RemoteFile(node));
            }

            return list;
        }

    }

}