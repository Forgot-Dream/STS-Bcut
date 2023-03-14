using FFMpegCore.Exceptions;
using Prism.Mvvm;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Data;

namespace STS_Bcut.src.Common
{
    public class STSTask:BindableBase
    {
        private string tip;
        /// <summary>
        /// 提示
        /// </summary>
        public string Tip
        {
            get { return tip; }
            set { tip = value; RaisePropertyChanged(); }
        }

        private int percentage;
        /// <summary>
        /// 进度条的百分比
        /// </summary>
        public int Percentage
        {
            get { return percentage; }
            set { percentage = value; RaisePropertyChanged(); }
        }

        private string statuscolor;
        /// <summary>
        /// 状态提示的颜色
        /// </summary>
        public string StatusColor
        {
            get { return statuscolor; }
            set { statuscolor = value; RaisePropertyChanged(); }
        }

        public int TaskNumber { get; set; }
        private int OutputFmt;

        private AudioFile AudioFile;
        private BcutAPI bcutAPI;
        public STSTask(int number, int outputfmt, AudioFile audioFile)
        {
            TaskNumber = number;
            Percentage = 0;
            StatusColor = "Blue";
            AudioFile = audioFile;
            OutputFmt = outputfmt;
            Tip = "等待识别中...";
        }

        public async Task<bool> Run()
        {
            return await Task.Run(() =>
            {
                bcutAPI = new(this, new FileInfo(AudioFile.FullPath));
                try
                {
                    var data = bcutAPI.Run();
                    switch (OutputFmt)
                    {
                        case 0:
                            File.WriteAllText(AudioFile.FullPath.Replace(Path.GetExtension(AudioFile.FullPath), ".srt"), data.ToSrt());
                            break;
                        case 1:
                            File.WriteAllText(AudioFile.FullPath.Replace(Path.GetExtension(AudioFile.FullPath), ".lrc"), data.ToLrc());
                            break;
                        case 2:
                            File.WriteAllText(AudioFile.FullPath.Replace(Path.GetExtension(AudioFile.FullPath), ".txt"), data.ToTxt());
                            break;
                    }
                    Percentage = 100;
                    StatusColor = "Green";
                    Tip = "字幕已导出";
                    return true;

                }
                catch (FFMpegException e)
                {
                    Debug.WriteLine(e.Message);
                    ThrowExpection("FFMpeg转码出错");
                }
                catch (HttpRequestException e)
                {
                    Debug.WriteLine(e.Message);
                    ThrowExpection("请求出错");
                }
                catch (ValueUnavailableException e)
                {
                    Debug.WriteLine(e.Message);
                    ThrowExpection("无可用数据");
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    ThrowExpection("未知错误 请联系作者提供复现过程");
                }
                return false;
            });

        }

        private void ThrowExpection(string message)
        {
            Tip = message;
            StatusColor = "Red";
        }



    }
}
