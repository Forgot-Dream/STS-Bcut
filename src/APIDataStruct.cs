using System.Collections.Generic;
using System.Linq;

namespace STS_Bcut.src;

public class UploadStruct
{
    public string model_id = "7";

    public int type = 2;

    public UploadStruct(string name, int size, string res)
    {
        this.name = name;
        this.size = size;
        ResourceFileType = res;
    }

    public string name { get; set; }
    public int size { get; set; }
    public string ResourceFileType { get; set; }
}

public class CommitUploadStruct
{
    public string Etags;

    public string InBossKey;
    public string model_id = "7";
    public string ResourceId;
    public string UploadId;

    public CommitUploadStruct(string in_boss_key, string resource_id, string etags, string upload_id)
    {
        InBossKey = in_boss_key;
        ResourceId = resource_id;
        Etags = etags;
        UploadId = upload_id;
    }
}

public class CreateTaskStruct
{
    public string model_id = "7";

    public string resource;

    public CreateTaskStruct(string resource)
    {
        this.resource = resource;
    }
}

public enum ResultStateEnum
{
    WAITING = 0,
    RUNNING = 1,
    ERROR = 3,
    COMLETE = 4
}

public class ResultResponse
{
    public string remark;
    public string result;
    public ResultStateEnum state;
    public string task_id;
}

public class STSDataSeg
{
    public int confidence;
    public int end_time;
    public int start_time;
    public string transcript;
    public List<STSDataWords> words;

    private (int, int, int, int) _Srt_Time_Conv_(int time)
    {
        return (time / 3600000, time / 60000 % 60, time / 1000 % 60, time % 1000);
    }

    private (int, int, int) _Lrc_Time_Conv_(int time)
    {
        return (time / 60000, time / 1000 % 60, time % 1000 / 10);
    }

    public string ToSrtTs()
    {
        var (s_h, s_m, s_s, s_ms) = _Srt_Time_Conv_(start_time);
        var (e_h, e_m, e_s, e_ms) = _Srt_Time_Conv_(end_time);
        return string.Format("{0:00}:{1:00}:{2:00},{3:000} --> {4:00}:{5:00}:{6:00},{7:000}", s_h, s_m, s_s, s_ms, e_h,
            e_m, e_s, e_ms);
    }

    public string ToLrcTs()
    {
        var (s_m, s_s, s_ms) = _Lrc_Time_Conv_(start_time);
        return string.Format("[{0:00}:{1:00}.{2:00}]", s_m, s_s, s_ms);
    }

    public class STSDataWords
    {
        public int confidence;
        public int end_time;
        public string label;
        public int start_time;
    }
}

public class STSData
{
    public List<STSDataSeg> utterances;
    public string version;

    public bool HasData()
    {
        return utterances.Count > 0;
    }

    public string ToTxt()
    {
        return string.Join("\n", utterances.Select(x => x.transcript).ToList());
    }

    public string ToSrt()
    {
        List<string> srt = new();
        for (var i = 0; i < utterances.Count; i++)
            srt.Add($"{i + 1}\n{utterances[i].ToSrtTs()}\n{utterances[i].transcript}\n");
        return string.Join("\n", srt);
    }

    public string ToLrc()
    {
        List<string> lrc = new();
        foreach (var item in utterances) lrc.Add($"{item.ToLrcTs()}{item.transcript}");
        return string.Join("\n", lrc);
    }
}