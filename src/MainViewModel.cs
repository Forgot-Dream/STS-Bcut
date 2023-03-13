using FFMpegCore;
using FFMpegCore.Exceptions;
using Microsoft.WindowsAPICodePack.Dialogs;
using Prism.Commands;
using Prism.Mvvm;
using STS_Bcut.src.Common;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace STS_Bcut.src
{
    public class MainViewModel : BindableBase
    {
        /// <summary>
        /// 支持直接转写的文件格式列表
        /// </summary>
        private readonly List<string> supportedaudiofmt = new()
        {
            ".flac",
            ".aac",
            ".m4a",
            ".mp3",
            ".wav"
        };


        private ObservableCollection<AudioFile> files;
        /// <summary>
        /// 音频文件的列表
        /// </summary>
        public ObservableCollection<AudioFile> Files
        {
            get { return files; }
            set { files = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<STSTask> tasks;
        /// <summary>
        /// 任务队列
        /// </summary>
        public ObservableCollection<STSTask> Tasks
        {
            get { return tasks; }
            set { tasks = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 是否所有都被选中 绑定用
        /// </summary>
        public bool? IsAllItemsSelected
        {
            get
            {
                if (Files == null || Files.Count == 0)
                    return false;
                var selected = Files.Select(item => item.IsSelected).Distinct().ToList();
                return selected.Count == 1 ? selected.Single() : null;
            }
            set
            {
                if (value.HasValue)
                {
                    SelectAll(value.Value, Files);
                }
                RaisePropertyChanged();
            }
        }


        /// <summary>
        /// 任务是否在运行
        /// </summary>
        private bool isrunning = false;

        public bool StartButtonEnabled { get { return !isrunning; } }


        public int OutputFmt { get; set; }

        private string? SoundFilePath;

        public DelegateCommand<object> OpenFileCommand { get; private set; }
        public DelegateCommand<object> StartRunCommand { get; private set; }
        public DelegateCommand<object> DeleteCommand { get; private set; }

        MainViewModel()
        {
            OpenFileCommand = new DelegateCommand<object>(OpenFile);
            StartRunCommand = new DelegateCommand<object>(StartRun);
            DeleteCommand = new DelegateCommand<object>(Delete);
            Files = new();
            Tasks = new()
            {
                new STSTask("测试"),
                new STSTask("测试1"),
                new STSTask("测试2")
            };
        }

        /// <summary>
        /// 打开文件的方法
        /// </summary>
        /// <param name="obj"></param>
        void OpenFile(object obj)
        {
            CommonOpenFileDialog openFileDialog = new("请选择你要打开的音频文件")
            {
                Multiselect = true
            };
            if (openFileDialog.ShowDialog() == CommonFileDialogResult.Cancel || openFileDialog.FileNames.Count()==0)
                return;
            foreach (var path in openFileDialog.FileNames)
            {
                Files.Add(new AudioFile()
                {
                    FullName = Path.GetFileName(path),
                    FullPath = path,
                    IsSelected = false
                });
            }
        }

        /// <summary>
        /// 删除文件的方法
        /// </summary>
        /// <param name="obj"></param>
        void Delete(object obj)
        {
            if (Files == null || Files.Count == 0)
                return;
            var deletelist = Files.Where(x => x.IsSelected).ToList();
            foreach (var file in deletelist)
                Files.Remove(file);
        }

        /// <summary>
        /// 开始转字幕
        /// </summary>
        /// <param name="obj"></param>
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

        /// <summary>
        /// 设置状态
        /// </summary>
        /// <param name="state"></param>
        void SetTaskState(bool state)
        {
            isrunning = state;
            RaisePropertyChanged(nameof(StartButtonEnabled));
        }

        /// <summary>
        /// 选择所有
        /// </summary>
        /// <param name="select"></param>
        /// <param name="audioFiles"></param>
        private static void SelectAll(bool select, ObservableCollection<AudioFile> audioFiles)
        {
            if (audioFiles == null || audioFiles.Count == 0)
                return;
            foreach (var model in audioFiles)
            {
                model.IsSelected = select;
            }
        }

        void logger(string log)
        {
        }

        void clearlogger()
        {
        }
    }
}
