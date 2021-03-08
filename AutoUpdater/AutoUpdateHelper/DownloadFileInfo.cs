using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace AutoUpdater
{
    /// <summary>
    /// 下载文件信息
    /// </summary>
    public class DownloadFileInfo
    {

        public string DownloadUrl { get; } = string.Empty;
        public string FileFullName { get; } = string.Empty;

        public string FileName { get { return Path.GetFileName(FileFullName); } }
        public string LastVer { get; set; } = string.Empty;

        public int Size { get; } = 0;
        public string Version { get; set; } = string.Empty;

        public DownloadFileInfo(string url, string name, string ver, int size, string versionid)
        {
            this.DownloadUrl = url;
            this.FileFullName = name;
            this.LastVer = ver;
            this.Size = size;
            this.Version = versionid;
        }
    }
}
