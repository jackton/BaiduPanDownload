﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BaiduPanDownload.HttpTool.Download
{
    class HttpDownload
    {
        #region 参数
        /// <summary>
        /// 任务ID
        /// </summary>
        public int ID { get; set; }
        /// <summary>
        /// 下载链接
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// 下载路径
        /// </summary>
        public string DownloadPath { get; set; }
        /// <summary>
        /// 线程数
        /// </summary>
        public int ThreadNum { get; set; }
        /// <summary>
        /// 下载速度
        /// </summary>
        public long Speed { get; private set; } = 0L;
        /// <summary>
        /// 下载进度
        /// </summary>
        public float Percentage { get; private set; } = 0F;
        /// <summary>
        /// 是否下载中
        /// </summary>
        public bool Downloading { get; private set; }
        /// <summary>
        /// 是否完成
        /// </summary>
        public bool Completed { get; private set; }
        /// <summary>
        /// 是否终止
        /// </summary>
        public bool Stoped { get; private set; }
        #endregion

        DownloadThread[] Threads;
        DownloadInfo Info;
        /// <summary>
        /// 开始下载
        /// </summary>
        public void Start()
        {
            try
            {
                Downloading = true;
                Stoped = false;
                HttpWebRequest Request = WebRequest.Create(Url) as HttpWebRequest;
                HttpWebResponse Response = Request.GetResponse() as HttpWebResponse;
                if (!File.Exists(DownloadPath + ".dcj"))
                {
                    DownloadInfo info = new DownloadInfo
                    {
                        ContentLength=Response.ContentLength,
                        BlockLength=Response.ContentLength/ThreadNum,
                        DownloadUrl = Url
                    };
                    info.init(DownloadPath + ".dcj");
                }
                Info= JsonConvert.DeserializeObject<DownloadInfo>(File.ReadAllText(DownloadPath + ".dcj"));
                if (Info.Completed)
                {
                    Downloading = false;
                    Completed = true;
                    Percentage = 100F;
                    return;
                }
                if (!File.Exists(DownloadPath))
                {
                    FileStream Stream = new FileStream(DownloadPath, FileMode.CreateNew);
                    Stream.SetLength(Response.ContentLength);
                    Stream.Close();
                }
                Console.WriteLine(Info.DownloadBlockList.Count);
                Threads = new DownloadThread[Info.DownloadBlockList.Count];

                for(int i = 0; i < Info.DownloadBlockList.Count; i++)
                {
                    DownloadBlock Block= JsonConvert.DeserializeObject<DownloadBlock>(Info.DownloadBlockList[i].ToString());
                    Threads[i]=new DownloadThread
                    {
                        ID = i,
                        DownloadUrl = Url,
                        Path = DownloadPath,
                        Block = Block,
                        Info = Info
                    };
                    Threads[i].ThreadCompletedEvent += HttpDownload_ThreadCompletedEvent;
                }
                new Thread(a).Start();
            }
            catch(Exception ex)
            {
                Downloading = false;
                Stoped = true;
                Console.WriteLine("出现错误: "+ex.ToString());
            }
        }

        public void CreateDataFile()
        {
            try
            {
                HttpWebRequest Request = WebRequest.Create(Url) as HttpWebRequest;
                HttpWebResponse Response = Request.GetResponse() as HttpWebResponse;
                if (!File.Exists(DownloadPath + ".dcj"))
                {
                    DownloadInfo info = new DownloadInfo
                    {
                        ContentLength = Response.ContentLength,
                        BlockLength = Response.ContentLength / ThreadNum,
                        DownloadUrl = Url
                    };
                    info.init(DownloadPath + ".dcj");
                }
                Info = JsonConvert.DeserializeObject<DownloadInfo>(File.ReadAllText(DownloadPath + ".dcj"));
            }
            catch(Exception ex)
            {
                MessageBox.Show("创建数据文件时出现错误: "+ex.Message);
                File.Delete(DownloadPath + ".dcj");
            }
        }


        int CompletedThread = 0;
        private void HttpDownload_ThreadCompletedEvent()
        {
            lock (this)
            {
                CompletedThread++;
                if (CompletedThread >= Threads.Length)
                {
                    Downloading = false;
                    Completed = true;
                    Speed = 0L;
                    Percentage = 100F;
                    Info.Completed = true;
                    Info.Save(DownloadPath + ".dcj");
                }
            }
        }

        public void a()
        {
            long temp = 0L;
            while (Downloading)
            {
                Thread.Sleep(1000);
                if (temp == 0)
                {
                    temp = Info.CompletedLength;
                }
                else
                {
                    Speed = Info.CompletedLength - temp;
                    Percentage = (((float)Info.CompletedLength / (float)Info.ContentLength) * 100);
                    temp = Info.CompletedLength;
                    //Console.WriteLine("速度: " + Speed / 1024 / 1024+"MB/S\r\n进度: "+((float)Info.CompletedLength/(float)Info.ContentLength)*100);
                }
            }
        }
        /// <summary>
        /// 保存并结束
        /// </summary>
        public void StopAndSave()
        {
            if (Threads != null)
            {
                Downloading = false;
                Stoped = true;
                CompletedThread = 0;
                foreach (var Thread in Threads)
                {
                    Thread.Stop();
                }
                Info.Save(DownloadPath + ".dcj");
            }
        }
    }
}
