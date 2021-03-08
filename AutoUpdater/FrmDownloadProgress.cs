using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Net;
using System.IO;
using System.Diagnostics;
using System.Xml;

namespace AutoUpdater
{
    public partial class FrmDownloadProgress : Form
    {
        private List<DownloadFileInfo> downloadFileList = null;
        private List<DownloadFileInfo> allFileList = null;
        private ManualResetEvent evtDownload = null;
        private ManualResetEvent evtPerDonwload = null;
        private WebClient clientDownload = null;
        private long total = 0;
        private long nDownloadedTotal = 0;
        private delegate void ShowCurrentDownloadFileNameCallBack(string name,long size);
        private delegate void SetProcessBarCallBack(int current, int total);

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="downloadFileListTemp"></param>
        public FrmDownloadProgress(List<DownloadFileInfo> downloadFileListTemp)
        {
            InitializeComponent();

            //初始化要下载的文件 
            this.downloadFileList = downloadFileListTemp;
            allFileList = new List<DownloadFileInfo>();
            foreach (DownloadFileInfo file in downloadFileListTemp)
            {
                allFileList.Add(file);
            }
        }

        /// <summary>
        /// 窗口关闭事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (clientDownload != null)
                clientDownload.CancelAsync();

            evtDownload.Set();
            evtPerDonwload.Set();
        }

        /// <summary>
        /// 窗口加载事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnFormLoad(object sender, EventArgs e)
        {
            evtDownload = new ManualResetEvent(true);
            evtDownload.Reset();
            ThreadPool.QueueUserWorkItem(new WaitCallback(this.ProcDownload));
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="o"></param>
        private void ProcDownload(object o)
        {
            try
            {
                //获取临时文件夹目录
                string tempFolderPath = Path.Combine(CommonUnitity.SystemBinUrl, ConstFile.TEMPFOLDERNAME);
                if (!Directory.Exists(tempFolderPath))
                {
                    Directory.CreateDirectory(tempFolderPath);
                }

                evtPerDonwload = new ManualResetEvent(false);

                //获取文件总大小
                foreach (DownloadFileInfo file in this.downloadFileList)
                {
                    total += file.Size;
                }


                //循环下载
                while (!evtDownload.WaitOne(0, false))
                {
                    if (this.downloadFileList.Count == 0)
                        break;

                    //取得下载文件信息
                    DownloadFileInfo file = this.downloadFileList[0];
                    //在UI呈现当前任务信息
                    this.ShowCurrentDownloadFileName(file.FileName, file.Size);
                    //创建网络连接
                    clientDownload = new WebClient();
                    //使用系统代理
                    clientDownload.Proxy = WebRequest.GetSystemWebProxy();
                    //使用应用程序凭据
                    clientDownload.Proxy.Credentials = CredentialCache.DefaultCredentials;
                    //使用应用程序凭据
                    clientDownload.Credentials = System.Net.CredentialCache.DefaultCredentials;
                    //文件下载进度更新
                    clientDownload.DownloadProgressChanged += (object sender, DownloadProgressChangedEventArgs e) =>
                    {
                        try
                        {
                            this.SetProcessBar(e.ProgressPercentage, (int)((nDownloadedTotal + e.BytesReceived) * 100 / total));
                        }
                        catch { }

                    };

                    //文件下载完成
                    clientDownload.DownloadFileCompleted += (object sender, AsyncCompletedEventArgs e) =>
                    {
                        try
                        {
                            //验证网络是否正常
                            DealWithDownloadErrors();
                            DownloadFileInfo dfile = e.UserState as DownloadFileInfo;
                            nDownloadedTotal += dfile.Size;
                            this.SetProcessBar(0, (int)(nDownloadedTotal * 100 / total));
                            //标记已完成
                            evtPerDonwload.Set();
                        }
                        catch { }

                    };
                    //阻隔线程
                    evtPerDonwload.Reset();
                    //创建文件保存路径
                    string tempFolderPath1 = CommonUnitity.GetFolderUrl(file);
                    if (!string.IsNullOrEmpty(tempFolderPath1))
                    {
                        tempFolderPath = Path.Combine(CommonUnitity.SystemBinUrl, ConstFile.TEMPFOLDERNAME);
                        tempFolderPath += tempFolderPath1;
                    }
                    else
                    {
                        tempFolderPath = Path.Combine(CommonUnitity.SystemBinUrl, ConstFile.TEMPFOLDERNAME);
                    }

                    //开始下载文件
                    clientDownload.DownloadFileAsync(new Uri(file.DownloadUrl), Path.Combine(tempFolderPath, file.FileName), file);
                    //等待任务完成
                    evtPerDonwload.WaitOne();
                    //销毁下载对象
                    clientDownload.Dispose();
                    clientDownload = null;
                    //移除已下载的文件
                    this.downloadFileList.Remove(file);
                }

                //如果没有下载文件，返回
                if (downloadFileList.Count > 0)
                    return;

                //处理网络错误
                //DealWithDownloadErrors();
                //拷贝文件到程序目录
                foreach (DownloadFileInfo file in this.allFileList)
                {
                    string tempUrlPath = CommonUnitity.GetFolderUrl(file);
                    string destFileName = string.Empty;
                    string sourceFileName = string.Empty;
                    try
                    {
                        //生成源路径与目标路径
                        if (!string.IsNullOrEmpty(tempUrlPath))
                        {
                            destFileName = Path.Combine(CommonUnitity.SystemBinUrl + tempUrlPath.Substring(1), file.FileName);
                            sourceFileName = Path.Combine(CommonUnitity.SystemBinUrl + ConstFile.TEMPFOLDERNAME + tempUrlPath, file.FileName);
                        }
                        else
                        {
                            destFileName = Path.Combine(CommonUnitity.SystemBinUrl, file.FileName);
                            sourceFileName = Path.Combine(CommonUnitity.SystemBinUrl + ConstFile.TEMPFOLDERNAME, file.FileName);
                        }

                        //验证文件是否损坏
                        System.IO.FileInfo f = new FileInfo(sourceFileName);
                        if (!file.Size.ToString().Equals(f.Length.ToString()) && !file.FileName.ToString().EndsWith(".xml"))
                        {
                            ShowErrorAndRestartApplication();
                        }

                        string newfilepath = string.Empty;
                        if (sourceFileName.Substring(sourceFileName.LastIndexOf(".") + 1).Equals(ConstFile.CONFIGFILEKEY))
                        {
                            if (System.IO.File.Exists(sourceFileName))
                            {
                                if (sourceFileName.EndsWith("_"))
                                {
                                    newfilepath = sourceFileName;
                                    sourceFileName = sourceFileName.Substring(0, sourceFileName.Length - 1);
                                    destFileName = destFileName.Substring(0, destFileName.Length - 1);
                                }
                                File.Move(newfilepath, sourceFileName);
                            }
                        }

                        if (File.Exists(destFileName))
                        {
                            MoveFolderToOld(destFileName, sourceFileName);
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(tempUrlPath))
                            {
                                if (!Directory.Exists(CommonUnitity.SystemBinUrl + tempUrlPath.Substring(1)))
                                {
                                    Directory.CreateDirectory(CommonUnitity.SystemBinUrl + tempUrlPath.Substring(1));


                                    MoveFolderToOld(destFileName, sourceFileName);
                                }
                                else
                                {
                                    MoveFolderToOld(destFileName, sourceFileName);
                                }
                            }
                            else
                            {
                                MoveFolderToOld(destFileName, sourceFileName);
                            }

                        }
                    }
                    catch
                    {
                        ShowErrorAndRestartApplication();
                    }

                }
                this.allFileList.Clear();
                if (this.downloadFileList.Count == 0)
                    this.DialogResult = DialogResult.OK;
                else
                    this.DialogResult = DialogResult.Cancel;
                evtDownload.Set();
            }
            catch
            {
                ShowErrorAndRestartApplication();
            }
        }

        /// <summary>
        /// 将下载完成的文件移动到指定位置
        /// </summary>
        /// <param name="destFileName"></param>
        /// <param name="sourceFileName"></param>
        void MoveFolderToOld(string destFileName, string sourceFileName)
        {
            if (File.Exists(destFileName + ".old"))
                File.Delete(destFileName + ".old");

            if (File.Exists(destFileName))
                File.Move(destFileName, destFileName + ".old");

            File.Move(sourceFileName, destFileName);
            //File.Delete(oldPath + ".old");
        }

        /// <summary>
        /// 显示正在下载的文件
        /// </summary>
        /// <param name="name"></param>
        private void ShowCurrentDownloadFileName(string name,long size)
        {
            if (this.lblFileName.InvokeRequired)
            {
                ShowCurrentDownloadFileNameCallBack cb = new ShowCurrentDownloadFileNameCallBack(ShowCurrentDownloadFileName);
                this.Invoke(cb, new object[] { name,size });
            }
            else
            {
                this.lblFileName.Text = $"文件信息:{name}({size / 1024}KB)";
            }
        }

        /// <summary>
        /// 更新进度条
        /// </summary>
        /// <param name="current"></param>
        /// <param name="total"></param>
        private void SetProcessBar(int current, int total)
        {
            if (this.progressBarCurrent.InvokeRequired)
            {
                SetProcessBarCallBack cb = new SetProcessBarCallBack(SetProcessBar);
                this.Invoke(cb, new object[] { current, total });
            }
            else
            {
                this.progressBarCurrent.Value = current;
                this.progressBarTotal.Value = total;
            }
        }

        /// <summary>
        /// 处理网络错误
        /// </summary>
        private void DealWithDownloadErrors()
        {
            try
            {
                //检测网络是否联通
                Config config = Config.LoadConfig(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConstFile.FILENAME));
                WebClient client = new WebClient();
                client.DownloadString(config.ServerUrl);
            }
            catch 
            {
                ShowErrorAndRestartApplication();
            }
        }

        /// <summary>
        /// 更新遇到问题提示重试
        /// </summary>
        private void ShowErrorAndRestartApplication()
        {
            MessageBox.Show(ConstFile.NOTNETWORK, ConstFile.MESSAGETITLE, MessageBoxButtons.OK, MessageBoxIcon.Information);
            this.DialogResult = DialogResult.Cancel;
        }

        private void lblSdgx_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            Process.Start($"http://www.cisharp.com/packet/mainApp.zip");
        }
    }
}