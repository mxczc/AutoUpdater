using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace AutoUpdater
{
    /// <summary>
    /// 启动进程、关闭进程操作
    /// </summary>
    public class OperProcess
    {
        /// <summary>
        /// 初始化更新
        /// </summary>
        public void InitUpdateEnvironment()
        {
            if (IfExist(ConstFile.SOFT_NAME))
            {
                CloseExe(ConstFile.SOFT_NAME);
            }
        }

        /// <summary>
        /// 完成更新后关闭更新启动主程序
        /// </summary>
        public void StartProcess()
        {
            string path = System.Environment.CurrentDirectory;
            if (!IfExist(ConstFile.SOFT_NAME))
            {
                StartExe(path, $"{ConstFile.SOFT_NAME}.exe");
            }
            //关闭当前进程
            //CloseExe(Process.GetCurrentProcess().ProcessName);
            Process.GetCurrentProcess().Kill();
        }
        
        /// <summary>
        /// 启动主程序
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="fileName"></param>
        private void StartExe(string filePath, string fileName)
        {
            if (File.Exists(fileName))
            {
                Process proc = new Process();
                proc.StartInfo.UseShellExecute = false;//是否使用操作系统外壳程序启动进程

                proc.StartInfo.WorkingDirectory = filePath;//启动进程的初始目录
                proc.StartInfo.FileName = fileName;
                proc.Start();
            }
        }

        /// <summary>
        /// 关闭指定进程
        /// </summary>
        /// <param name="exeName"></param>
        private void CloseExe(string exeName)
        {
            Process[] arrPro = Process.GetProcessesByName(exeName);
            foreach (Process pro in arrPro)
                pro.Kill();
        }

        /// <summary>
        /// 判断进程是否存在
        /// </summary>
        /// <param name="processName"></param>
        /// <returns></returns>
        private bool IfExist(string processName)
        {
            Process[] pro = Process.GetProcessesByName(processName);
            return pro.Count() > 0;
        }
        
    }
}
