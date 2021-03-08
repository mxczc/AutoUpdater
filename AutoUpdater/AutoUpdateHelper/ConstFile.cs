using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoUpdater
{
    public class ConstFile
    {
        /// <summary>
        /// 主程序名称
        /// </summary>
        public const string SOFT_NAME = "MianApp";
        public const string TEMPFOLDERNAME = "TempFolder";
        public const string CONFIGFILEKEY = "config_";
        public const string FILENAME = "acfg.config";
        public const string ROOLBACKFILE = "MianApp.exe";
        public const string MESSAGETITLE = "自动更新程序";
        public const string CANCELORNOT = "正在更新中，确定取消?";
        public const string APPLYTHEUPDATE = "程序需要重新启动才能应用更新，请点击“确定”重新启动程序！";
        public const string NOTNETWORK = "程序更新未能成功，请重启更新程序重试！";
    }
}
