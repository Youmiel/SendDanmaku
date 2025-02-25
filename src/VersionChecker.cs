﻿using BilibiliDM_PluginFramework;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.IO;
using System.Net;

namespace SendDanmaku
{
    internal class VersionChecker
    {
        /// <summary>
        /// 异步检查版本号
        /// </summary>
        /// <param name="plugin">要执行检查的插件</param>
        public static void Check(DMPlugin plugin)
        {
            new System.Threading.Thread(() =>
            {
                try
                {
                    var versionChecker = new VersionChecker("SendDanmaku");
                    if(versionChecker.FetchInfo())
                    {
                        if(versionChecker.HasNewVersion(plugin.PluginVer))
                        {
                            plugin.Log("插件有新版本了！最新版本：" + versionChecker.Version + "，当前版本" + plugin.PluginVer +
                            "，更新时间：" + versionChecker.UpdateTime.ToString("yyyy.MM.dd") + "\r\n" + "下载地址：" + versionChecker.WebPageUrl);
                        }
                    }
                    else
                    {
                        plugin.Log("版本检查失败：" + versionChecker.LastException.Message);
                    }

                }
                catch(Exception)
                { }// 以防万一
            })
            { Name = "VersionChecker", IsBackground = true }.Start();
        }

        #region -- 常量 --

        /// <summary>
        /// 默认服务器地址
        /// </summary>
        public const string DEFAULT_BASE_URL = "https://www.danmuji.org";

        /// <summary>
        /// API的路径
        /// </summary>
        public const string API_PATH = "/api/v1/";

        /// <summary>
        /// 插件介绍页的路径
        /// </summary>
        public const string WEB_PATH = "/plugins/";

        #endregion

        #region -- 变量 --
        private string BaseUrl;

        /// <summary>
        /// 最后一个出错的Exception
        /// </summary>
        public Exception LastException { get; private set; } = null;

        /// <summary>
        /// 插件ID
        /// </summary>
        public string Id { get; private set; } = "example";

        /// <summary>
        /// API返回的插件名字
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// API返回的插件作者
        /// </summary>
        public string Author { get; private set; }

        /// <summary>
        /// API返回的最新版本号
        /// </summary>
        public Version Version { get; private set; }

        /// <summary>
        /// API返回的插件说明
        /// </summary>
        public string Description { get; private set; }

        /// <summary>
        /// API返回的更新时间
        /// </summary>
        public DateTime UpdateTime { get; private set; }

        /// <summary>
        /// API返回的下载地址
        /// </summary>
        public Uri DownloadUrl { get; private set; }

        /// <summary>
        /// 用户查看用的页面地址
        /// </summary>
        public Uri WebPageUrl
        {
            get
            { return new Uri(this.BaseUrl + WEB_PATH + this.Id); }
        }

        /// <summary>
        /// API返回的下载说明
        /// </summary>
        public string DownloadNote { get; private set; }

        #endregion

        /// <summary>
        /// 新建一个版本检查类
        /// </summary>
        /// <param name="id">插件ID</param>
        public VersionChecker(string id) : this(id, DEFAULT_BASE_URL)
        { }

        /// <summary>
        /// 新建一个指定服务器地址的版本检查类
        /// </summary>
        /// <param name="id">插件ID</param>
        /// <param name="baseurl">服务器地址</param>
        public VersionChecker(string id, string baseurl)
        {
            this.Id = id;
            this.BaseUrl = baseurl;
        }

        /// <summary>
        /// 从服务器获取信息
        /// </summary>
        /// <returns>获取是否成功</returns>
        public bool FetchInfo()
        {
            try
            {
                Uri fullUri = new Uri(BaseUrl + API_PATH + this.Id);
                string json = HttpGet(fullUri);
                JObject j = JObject.Parse(json);

                if(j["id"].ToString() != this.Id)
                { LastException = new Exception("API返回ID不正确"); return false; }
                else
                {
                    this.Name = j["name"].ToString();
                    this.Author = j["author"].ToString();
                    this.Version = new Version(j["version"].ToString());
                    this.Description = j["description"].ToString();
                    this.UpdateTime = DateTime.ParseExact(j["updatetime"].ToString(),
                        "yyyy.MM.dd", CultureInfo.InvariantCulture);
                    this.DownloadUrl = j["dl_url"].ToString().StartsWith("http") ?
                        new Uri(j["dl_url"].ToString()) : new Uri(this.BaseUrl + j["dl_url"].ToString());
                    this.DownloadNote = j["dl_note"].ToString();

                    return true;
                }
            }
            catch(Exception ex)
            { LastException = ex; return false; }

        }

        /// <summary>
        /// 检查是否具有更新的版本
        /// </summary>
        /// <param name="version">当前版本号</param>
        /// <returns>检查结果</returns>
        public bool HasNewVersion(string version)
        { return HasNewVersion(new Version(version)); }

        /// <summary>
        /// 检查是否具有更新的版本
        /// </summary>
        /// <param name="version">当前版本号</param>
        /// <returns>检查结果</returns>
        public bool HasNewVersion(Version version)
        { return (version.CompareTo(this.Version) < 0); }
        // version对象比参数小（之前，older的版本）


        private string HttpGet(Uri uri)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri);
            //request.AutomaticDecompression = DecompressionMethods.GZip;
            using(HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using(Stream stream = response.GetResponseStream())
            using(StreamReader reader = new StreamReader(stream))
            { var text = reader.ReadToEnd(); return text; }
        }
    }
}
