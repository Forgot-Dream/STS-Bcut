using System.Collections.Generic;
using System.Linq;

namespace STS_Bcut.src
{
    public class UploadStruct
    {
        public UploadStruct(string name, int size, string res)
        {
            this.name = name;
            this.size = size;
            ResourceFileType = res;
        }

        public int type = 2;
        public string name { get; set; }
        public int size { get; set; }
        public string ResourceFileType { get; set; }
        public string model_id = "7";
    }


    public class CommitUploadStruct
    {
        public CommitUploadStruct(string in_boss_key, string resource_id, string etags, string upload_id)
        {
            InBossKey = in_boss_key;
            ResourceId = resource_id;
            Etags = etags;
            UploadId = upload_id;
        }

        public string InBossKey;
        public string ResourceId;
        public string Etags;
        public string UploadId;
        public string model_id = "7";
    }


    public class CreateTaskStruct
    {
        public CreateTaskStruct(string resource)
        {
            this.resource = resource;
        }

        public string resource;
        public string model_id = "7";
    }

    public enum ResultStateEnum : int
    {
        STOP = 1,
        RUNNING = 2,
        ERROR = 3,
        COMLETE = 4
    }

    public class ResultResponse
    {
        public string task_id;
        public string result;
        public string remark;
        public ResultStateEnum state;
    }

    public class STSDataSeg
    {
        public class STSDataWords
        {
            public string label;
            public int start_time;
            public int end_time;
            public int confidence;
        }
        public int start_time;
        public int end_time;
        public string transcript;
        public List<STSDataWords> words;
        public int confidence;

        private (int, int, int, int) _Srt_Time_Conv_(int time) => (time / 3600000, time / 60000 % 60, time / 1000 % 60, time % 1000);
        private (int, int, int) _Lrc_Time_Conv_(int time) => (time / 60000, time / 1000 % 60, time % 1000 / 10);

        public string ToSrtTs()
        {
            var (s_h, s_m, s_s, s_ms) = _Srt_Time_Conv_(start_time);
            var (e_h, e_m, e_s, e_ms) = _Srt_Time_Conv_(end_time);
            return $"{s_h:02d}:{s_m:02d}:{s_s:02d},{s_ms:03d} --> {e_h:02d}:{e_m:02d}:{e_s:02d},{e_ms:03d}";
        }

        public string ToLrcTs()
        {
            var (s_m, s_s, s_ms) = _Lrc_Time_Conv_(start_time);
            return $"[{s_m:02d}:{s_s:02d}.{s_ms:02d}]";
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
            for(int i = 0 ; i < utterances.Count; i++)
            {
                srt.Add($"{i+1}\n{utterances[i].ToSrtTs()}\n{utterances[i].transcript}\n");
            }
            return string.Join("\n",srt);
        }

        public string ToLrc()
        {
            List<string> lrc = new();
            foreach (var item in utterances)
            {
                lrc.Add($"{item.ToLrcTs()}{item.transcript}");
            }
            return string.Join("\n", lrc);
        }

    }


}
