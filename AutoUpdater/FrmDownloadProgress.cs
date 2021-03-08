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
        /// ���캯��
        /// </summary>
        /// <param name="downloadFileListTemp"></param>
        public FrmDownloadProgress(List<DownloadFileInfo> downloadFileListTemp)
        {
            InitializeComponent();

            //��ʼ��Ҫ���ص��ļ� 
            this.downloadFileList = downloadFileListTemp;
            allFileList = new List<DownloadFileInfo>();
            foreach (DownloadFileInfo file in downloadFileListTemp)
            {
                allFileList.Add(file);
            }
        }

        /// <summary>
        /// ���ڹر��¼�
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
        /// ���ڼ����¼�
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
        /// �����ļ�
        /// </summary>
        /// <param name="o"></param>
        private void ProcDownload(object o)
        {
            try
            {
                //��ȡ��ʱ�ļ���Ŀ¼
                string tempFolderPath = Path.Combine(CommonUnitity.SystemBinUrl, ConstFile.TEMPFOLDERNAME);
                if (!Directory.Exists(tempFolderPath))
                {
                    Directory.CreateDirectory(tempFolderPath);
                }

                evtPerDonwload = new ManualResetEvent(false);

                //��ȡ�ļ��ܴ�С
                foreach (DownloadFileInfo file in this.downloadFileList)
                {
                    total += file.Size;
                }


                //ѭ������
                while (!evtDownload.WaitOne(0, false))
                {
                    if (this.downloadFileList.Count == 0)
                        break;

                    //ȡ�������ļ���Ϣ
                    DownloadFileInfo file = this.downloadFileList[0];
                    //��UI���ֵ�ǰ������Ϣ
                    this.ShowCurrentDownloadFileName(file.FileName, file.Size);
                    //������������
                    clientDownload = new WebClient();
                    //ʹ��ϵͳ����
                    clientDownload.Proxy = WebRequest.GetSystemWebProxy();
                    //ʹ��Ӧ�ó���ƾ��
                    clientDownload.Proxy.Credentials = CredentialCache.DefaultCredentials;
                    //ʹ��Ӧ�ó���ƾ��
                    clientDownload.Credentials = System.Net.CredentialCache.DefaultCredentials;
                    //�ļ����ؽ��ȸ���
                    clientDownload.DownloadProgressChanged += (object sender, DownloadProgressChangedEventArgs e) =>
                    {
                        try
                        {
                            this.SetProcessBar(e.ProgressPercentage, (int)((nDownloadedTotal + e.BytesReceived) * 100 / total));
                        }
                        catch { }

                    };

                    //�ļ��������
                    clientDownload.DownloadFileCompleted += (object sender, AsyncCompletedEventArgs e) =>
                    {
                        try
                        {
                            //��֤�����Ƿ�����
                            DealWithDownloadErrors();
                            DownloadFileInfo dfile = e.UserState as DownloadFileInfo;
                            nDownloadedTotal += dfile.Size;
                            this.SetProcessBar(0, (int)(nDownloadedTotal * 100 / total));
                            //��������
                            evtPerDonwload.Set();
                        }
                        catch { }

                    };
                    //����߳�
                    evtPerDonwload.Reset();
                    //�����ļ�����·��
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

                    //��ʼ�����ļ�
                    clientDownload.DownloadFileAsync(new Uri(file.DownloadUrl), Path.Combine(tempFolderPath, file.FileName), file);
                    //�ȴ��������
                    evtPerDonwload.WaitOne();
                    //�������ض���
                    clientDownload.Dispose();
                    clientDownload = null;
                    //�Ƴ������ص��ļ�
                    this.downloadFileList.Remove(file);
                }

                //���û�������ļ�������
                if (downloadFileList.Count > 0)
                    return;

                //�����������
                //DealWithDownloadErrors();
                //�����ļ�������Ŀ¼
                foreach (DownloadFileInfo file in this.allFileList)
                {
                    string tempUrlPath = CommonUnitity.GetFolderUrl(file);
                    string destFileName = string.Empty;
                    string sourceFileName = string.Empty;
                    try
                    {
                        //����Դ·����Ŀ��·��
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

                        //��֤�ļ��Ƿ���
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
        /// ��������ɵ��ļ��ƶ���ָ��λ��
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
        /// ��ʾ�������ص��ļ�
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
                this.lblFileName.Text = $"�ļ���Ϣ:{name}({size / 1024}KB)";
            }
        }

        /// <summary>
        /// ���½�����
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
        /// �����������
        /// </summary>
        private void DealWithDownloadErrors()
        {
            try
            {
                //��������Ƿ���ͨ
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
        /// ��������������ʾ����
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