using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Windows.Data;

namespace STS_Bcut.src
{

    public class BcutAPI
    {

        /// <summary>
        /// 申请上传
        /// </summary>
        private const string API_REQ_UPLOAD = "https://member.bilibili.com/x/bcut/rubick-interface/resource/create";
        /// <summary>
        /// 提交上传
        /// </summary>
        private const string API_COMMIT_UPLOAD = "https://member.bilibili.com/x/bcut/rubick-interface/resource/create/complete";
        /// <summary>
        /// 创建任务
        /// </summary>
        private const string API_CREATE_TASK = "https://member.bilibili.com/x/bcut/rubick-interface/task";
        /// <summary>
        /// 查询结果
        /// </summary>
        private const string API_QUERY_RESULT = "https://member.bilibili.com/x/bcut/rubick-interface/task/result";

        public string SoundName;
        public byte[] SoundData;
        public string SoundFormat;
        public string TaskId;
        private string InBossKey;
        private string ResourceId;
        private string UploadId;
        private List<string> UploadUrls;
        private int ClipsPerSize;
        private int Clips;
        private List<string> Etags;
        private string DownloadUrl;
        private Action<string> logger;

        public BcutAPI(Action<string> logger, FileInfo? file = null)
        {
            Etags = new List<string>();
            this.logger = logger;
            if (file != null)
            {
                SetData(file);
            }
        }

        public void SetData(FileInfo? file = null, byte[]? raw_data = null, string? datafmt = null)
        {
            if (file != null && file.Exists)
            {
                SoundData = File.ReadAllBytes(file.FullName);
                SoundFormat = Path.GetExtension(file.FullName).ToLower().Replace(".", "");
                SoundName = Path.GetFileName(file.FullName);
            }
            else if (raw_data != null && datafmt != null)
            {
                SoundData = raw_data;
                SoundFormat = datafmt;
                SoundName = $"{Random.Shared.NextInt64()}.{datafmt}";
            }
            else
            {
                throw new ValueUnavailableException("无可用数据");
            }
            logger($"加载文件成功:{SoundName}");
        }

        public void Upload()
        {
            UploadStruct uploaddata = new(SoundName, SoundData.Length, SoundFormat);
            var reply = APIPost(API_REQ_UPLOAD, uploaddata);
            if (reply != null)
            {
                InBossKey = reply["data"]["in_boss_key"].Value<string>();
                ResourceId = reply["data"]["resource_id"].Value<string>();
                UploadId = reply["data"]["upload_id"].Value<string>();
                UploadUrls = reply["data"]["upload_urls"].Values<string>().ToList();
                ClipsPerSize = reply["data"]["per_size"].Value<int>();
                Clips = UploadUrls.Count;
            }
            UploadPart();
            CommitUpload();
        }

        private void UploadPart()
        {
            for (int clip = 0; clip < Clips; clip++)
            {
                int start_range = clip * ClipsPerSize;
                int size = Math.Min(SoundData.Count() - start_range, ClipsPerSize);
                logger($"开始上传切片{clip},{start_range} size:{size}");
                var reply = APIPut(UploadUrls[clip],
                    new ByteArrayContent(SoundData, start_range, size));
                if (reply != null && reply.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    string etag = reply.Headers.GetValues("ETag").First();
                    Etags.Add(etag);
                    logger($"{reply.StatusCode} 切片上传成功 {etag}");
                }
            }
        }

        private void CommitUpload()
        {
            var reply = APIPost(API_COMMIT_UPLOAD,
                new CommitUploadStruct(
                    InBossKey,
                    ResourceId,
                    string.Join(",", Etags),
                    UploadId));
            DownloadUrl = reply["data"]["download_url"].Value<string>();
        }

        public string? CreateTask()
        {
            var reply = APIPost(API_CREATE_TASK, new CreateTaskStruct(DownloadUrl));
            if (reply != null)
            {
                TaskId = reply["data"]["task_id"].Value<string>();
                logger($"任务已创建:{TaskId}");
                return TaskId;
            }
            return null;
        }

        public STSData? QueryResult()
        {
            Dictionary<string, string> json = new()
            {
                {"task_id",TaskId },
                {"model_id","7" }
            };
            var reply = APIGet(API_QUERY_RESULT, json);
            if (reply != null)
            {
                var data = JsonConvert.DeserializeObject<ResultResponse>(reply["data"].ToString());
                if (data.state == ResultStateEnum.COMLETE)
                {
                    logger("任务状态：完成");
                    return JsonConvert.DeserializeObject<STSData>(data.result);
                }else if(data.state == ResultStateEnum.STOP)
                {
                    logger("任务状态：停止");
                }else if(data.state == ResultStateEnum.RUNNING)
                {
                    logger("任务状态：运行中");
                }else if(data.state == ResultStateEnum.ERROR)
                {
                    logger("任务状态：错误");
                    return null;
                }
            }
            return null;

        }

        /// <summary>
        /// 向url发送post请求
        /// </summary>
        /// <param name="url">访问的url</param>
        /// <param name="Data">请求时候发送的数据</param>
        /// <returns>json对象</returns>
        private JToken? APIPost(string url, object Data)
        {
            HttpClient httpClient = new();
            HttpRequestMessage requestMessage = new(HttpMethod.Post, url);
            requestMessage.Headers.Add("Accept", "application/json");
            StringContent content = new(JsonConvert.SerializeObject(Data), Encoding.UTF8, "application/json");
            requestMessage.Content = content;
            try
            {
                var response = httpClient.Send(requestMessage);
                response.EnsureSuccessStatusCode();
                Stream myResponseStream = response.Content.ReadAsStream();
                StreamReader myStreamReader = new(myResponseStream, Encoding.UTF8);
                string retString = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                myResponseStream.Close();
                return JToken.Parse(retString);
            }
            catch (Exception e) { logger(e.Message); return null; }
        }

        private HttpResponseMessage? APIPut(string url, ByteArrayContent data)
        {
            HttpClient httpClient = new();
            HttpRequestMessage requestMessage = new(HttpMethod.Put, url);
            requestMessage.Headers.Add("Accept", "application/json");
            requestMessage.Content = data;
            try
            {
                var response = httpClient.Send(requestMessage);
                response.EnsureSuccessStatusCode();
                return response;
            }
            catch (Exception e) { logger(e.Message); return null; }
        }

        private JToken? APIGet(string url, Dictionary<string, string>? Params)
        {
            StringBuilder builder = new();
            builder.Append(url);
            if (Params != null && Params.Count > 0)
            {
                builder.Append('?');
                int i = 0;
                foreach (var item in Params)
                {
                    if (item.Value == null)
                        continue;
                    if (i > 0)
                        builder.Append('&');
                    builder.AppendFormat("{0}={1}", item.Key, item.Value);
                    i++;
                }
            }
            HttpClient httpClient = new();
            HttpRequestMessage request = new(HttpMethod.Get, builder.ToString());
            request.Headers.Add("Accept", "application/json"); //设置接受类型
            try
            {
                var response = httpClient.Send(request);
                response.EnsureSuccessStatusCode();
                Stream myResponseStream = response.Content.ReadAsStream();
                StreamReader myStreamReader = new(myResponseStream, Encoding.UTF8);
                string retString = myStreamReader.ReadToEnd();
                myStreamReader.Close();
                myResponseStream.Close();
                return JToken.Parse(retString);
            }
            catch (Exception e) { logger(e.Message); return null; }
        }

    }
}
