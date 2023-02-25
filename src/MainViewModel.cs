using FFMpegCore;
using FFMpegCore.Exceptions;
using Microsoft.WindowsAPICodePack.Dialogs;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input.StylusPlugIns;

namespace STS_Bcut.src
{
    public class MainViewModel : BindableBase
    {
        private List<string> supportedaudiofmt = new()
        {
            ".flac",
            ".aac",
            ".m4a",
            ".mp3",
            ".wav"
        };

        private ObservableCollection<string>? log;

        public ObservableCollection<string>? Log
        {
            get { return log; }
            set { log = value; RaisePropertyChanged(); }
        }

        private bool isrunning = false;

        public bool StartButtonEnabled { get { return !isrunning; } }


        public int OutputFmt { get; set; }

        private string? SoundFilePath;

        public DelegateCommand<object> OpenFileCommand { get; private set; }
        public DelegateCommand<object> StartRunCommand { get; private set; }

        MainViewModel()
        {
            Log = new();
            OpenFileCommand = new DelegateCommand<object>(OpenFile);
            StartRunCommand = new DelegateCommand<object>(StartRun);
        }

        void OpenFile(object obj)
        {
            CommonOpenFileDialog openFileDialog = new CommonOpenFileDialog("请选择你要打开的音频文件");
            openFileDialog.Multiselect = false;
            if (openFileDialog.ShowDialog() == CommonFileDialogResult.Cancel)
                return;
            SoundFilePath = openFileDialog.FileName;
            logger($"你选择了  {openFileDialog.FileName}");
        }

        async void StartRun(object obj)
        {
            await Task.Run(() =>
            {
                SetTaskState(true);
                clearlogger();
                if (string.IsNullOrEmpty(SoundFilePath) || OutputFmt == -1)
                {
                    logger("你还未选择音频文件或者输出格式");
                    SetTaskState(false);
                    return;
                }

                if (!supportedaudiofmt.Contains(Path.GetExtension(SoundFilePath)))
                {
                    logger("非支持音频文件 尝试使用ffmpeg转码");
                    try
                    {
                        var result = FFMpeg.ExtractAudio(SoundFilePath, SoundFilePath.Replace(Path.GetExtension(SoundFilePath), ".mp3"));
                        if (result)
                            logger("转码成功");
                        else
                        {
                            logger("转码失败");
                            SetTaskState(false);
                            return;
                        }
                    }
                    catch (FFMpegException e)
                    {
                        logger("转码出错");
                        logger(e.Message);
                        SetTaskState(false);
                        return;
                    }
                    SoundFilePath = SoundFilePath.Replace(Path.GetExtension(SoundFilePath), ".mp3");
                }

                try
                {
                    BcutAPI bcutAPI = new BcutAPI(logger, new System.IO.FileInfo(SoundFilePath));
                    bcutAPI.Upload();
                    bcutAPI.CreateTask();
                    STSData? rsp = null;
                    while (rsp == null)
                    {
                        rsp = bcutAPI.QueryResult();
                        Thread.Sleep(1000);
                    }
                    switch (OutputFmt)
                    {
                        case 0:
                            File.WriteAllText(SoundFilePath.Replace(Path.GetExtension(SoundFilePath), ".srt"), rsp.ToSrt());
                            break;
                        case 1:
                            File.WriteAllText(SoundFilePath.Replace(Path.GetExtension(SoundFilePath), ".lrc"), rsp.ToLrc());
                            break;
                        case 2:
                            File.WriteAllText(SoundFilePath.Replace(Path.GetExtension(SoundFilePath), ".txt"), rsp.ToTxt());
                            break;
                    }
                    logger("已经输出到同目录下");
                    SetTaskState(false);

                }
                catch (Exception e)
                {
                    logger("执行任务时出错");
                    logger(e.Message);
                    SetTaskState(false);
                }
            });
        }

        void SetTaskState(bool state)
        {
            isrunning = state;
            RaisePropertyChanged(nameof(StartButtonEnabled));
        }
        void logger(string log)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Log.Add(log);
            });
        }

        void clearlogger()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                Log.Clear();
            });
        }
    }
}
