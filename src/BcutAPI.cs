using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Windows.Data;
using FFMpegCore;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using STS_Bcut.src.Common;
using STS_Bcut.src.ViewModels;

namespace STS_Bcut.src;

public class BcutAPI
{
    /// <summary>
    ///     申请上传
    /// </summary>
    private const string API_REQ_UPLOAD = "https://member.bilibili.com/x/bcut/rubick-interface/resource/create";

    /// <summary>
    ///     提交上传
    /// </summary>
    private const string API_COMMIT_UPLOAD =
        "https://member.bilibili.com/x/bcut/rubick-interface/resource/create/complete";

    /// <summary>
    ///     创建任务
    /// </summary>
    private const string API_CREATE_TASK = "https://member.bilibili.com/x/bcut/rubick-interface/task";

    /// <summary>
    ///     查询结果
    /// </summary>
    private const string API_QUERY_RESULT = "https://member.bilibili.com/x/bcut/rubick-interface/task/result";

    /// <summary>
    ///     支持直接转写的文件格式列表
    /// </summary>
    private readonly List<string> supportedaudiofmt = new()
    {
        ".flac",
        ".aac",
        ".m4a",
        ".mp3",
        ".wav"
    };

    private int Clips;
    private int ClipsPerSize;
    private string DownloadUrl;
    private readonly List<string> Etags;
    private readonly FileInfo fileInfo;

    private readonly HttpClient httpClient;
    private string InBossKey;
    private string ResourceId;
    public byte[] SoundData;
    public string SoundFormat;

    public string SoundName;
    private readonly STSTask task;
    public string TaskId;
    private string UploadId;
    private List<string> UploadUrls;


    public BcutAPI(STSTask task, FileInfo? file = null)
    {
        this.task = task;
        Etags = new List<string>();
        if (file != null) fileInfo = file;

        //init http client
        httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
    }

    public STSData Run()
    {
        SetData(fileInfo);
        Upload();
        CreateTask();
        STSData? data = null;
        while (data == null)
        {
            data = QueryResult();
            Thread.Sleep(1000);
        }

        return data;
    }

    public void SetData(FileInfo? file = null, string? datafmt = null)
    {
        if (file is { Exists: true })
        {
            var path = file.FullName;
            var isNeedConvent = !supportedaudiofmt.Contains(Path.GetExtension(file.Name));
            if (isNeedConvent)
            {
                UpdateMessage("非支持音频文件，FFMpeg转码中");
                FFMpeg.ExtractAudio(path, path.Replace(Path.GetExtension(file.Name), ".mp3"));
                path = path.Replace(Path.GetExtension(file.Name), ".mp3");
            }

            SoundData = File.ReadAllBytes(path);
            SoundFormat = Path.GetExtension(path).ToLower().Replace(".", string.Empty);
            SoundName = Path.GetFileName(file.FullName);
            if (MainViewModel.config != null && !MainViewModel.config.SaveConvertedAudio && isNeedConvent)
                File.Delete(path);
        }
        else
        {
            throw new ValueUnavailableException("无可用数据");
        }
    }

    public void Upload()
    {
        UploadStruct uploaddata = new(SoundName, SoundData.Length, SoundFormat);
        var reply = APIPost(API_REQ_UPLOAD, uploaddata);
        InBossKey = reply["data"]["in_boss_key"].Value<string>();
        ResourceId = reply["data"]["resource_id"].Value<string>();
        UploadId = reply["data"]["upload_id"].Value<string>();
        UploadUrls = reply["data"]["upload_urls"].Values<string>().ToList();
        ClipsPerSize = reply["data"]["per_size"].Value<int>();
        Clips = UploadUrls.Count;
        UploadPart();
        CommitUpload();
    }

    private void UploadPart()
    {
        for (var clip = 0; clip < Clips; clip++)
        {
            var start_range = clip * ClipsPerSize;
            var size = Math.Min(SoundData.Count() - start_range, ClipsPerSize);
            var reply = APIPut(
                UploadUrls[clip],
                new ByteArrayContent(SoundData, start_range, size)
            );
            if (reply == null || reply.StatusCode != HttpStatusCode.OK) continue;
            var etag = reply.Headers.GetValues("ETag").First();
            Etags.Add(etag);
            UpdateMessage($"上传分片:{clip + 1}");
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
        UpdateMessage("分片上传完成");
    }

    public void CreateTask()
    {
        var reply = APIPost(API_CREATE_TASK, new CreateTaskStruct(DownloadUrl));
        TaskId = reply["data"]["task_id"].Value<string>();
        UpdateMessage("任务已创建");
    }

    public STSData? QueryResult()
    {
        Dictionary<string, string> json = new()
        {
            { "task_id", TaskId },
            { "model_id", "7" }
        };
        var reply = APIGet(API_QUERY_RESULT, json);
        var data = JsonConvert.DeserializeObject<ResultResponse>(reply["data"].ToString());
        switch (data.state)
        {
            case ResultStateEnum.COMLETE:
                UpdateMessage("任务已完成");
                return JsonConvert.DeserializeObject<STSData>(data.result);
            case ResultStateEnum.WAITING:
                UpdateMessage("任务等待中");
                break;
            case ResultStateEnum.RUNNING:
                UpdateMessage("任务正在识别中");
                break;
            case ResultStateEnum.ERROR:
                throw new Exception("任务错误");
            default:
                throw new ArgumentOutOfRangeException();
        }

        return null;
    }

    /// <summary>
    ///     向url发送post请求
    /// </summary>
    /// <param name="url">访问的url</param>
    /// <param name="Data">请求时候发送的数据</param>
    /// <returns>json对象</returns>
    private JToken APIPost(string url, object Data)
    {
        HttpRequestMessage requestMessage = new(HttpMethod.Post, url);
        StringContent content = new(JsonConvert.SerializeObject(Data), Encoding.UTF8, "application/json");
        requestMessage.Content = content;
        var response = httpClient.Send(requestMessage);
        response.EnsureSuccessStatusCode();
        var myResponseStream = response.Content.ReadAsStream();
        StreamReader myStreamReader = new(myResponseStream, Encoding.UTF8);
        var retString = myStreamReader.ReadToEnd();
        myStreamReader.Close();
        myResponseStream.Close();
        return JToken.Parse(retString);
    }

    private HttpResponseMessage APIPut(string url, ByteArrayContent data)
    {
        HttpRequestMessage requestMessage = new(HttpMethod.Put, url);
        requestMessage.Content = data;
        var response = httpClient.Send(requestMessage);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private JToken APIGet(string url, Dictionary<string, string>? Params)
    {
        StringBuilder builder = new();
        builder.Append(url);
        if (Params != null && Params.Count > 0)
        {
            builder.Append('?');
            var i = 0;
            foreach (var item in Params.Where(item => item.Value != null))
            {
                if (i > 0)
                    builder.Append('&');
                builder.AppendFormat("{0}={1}", item.Key, item.Value);
                i++;
            }
        }

        HttpRequestMessage request = new(HttpMethod.Get, builder.ToString());
        var response = httpClient.Send(request);
        response.EnsureSuccessStatusCode();
        var myResponseStream = response.Content.ReadAsStream();
        StreamReader myStreamReader = new(myResponseStream, Encoding.UTF8);
        var retString = myStreamReader.ReadToEnd();
        myStreamReader.Close();
        myResponseStream.Close();
        return JToken.Parse(retString);
    }

    private void UpdateMessage(string message)
    {
        task.Tip = message;
    }
}