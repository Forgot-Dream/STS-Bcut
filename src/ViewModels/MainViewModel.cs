using Microsoft.WindowsAPICodePack.Dialogs;
using Prism.Commands;
using Prism.Mvvm;
using STS_Bcut.src.Common;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace STS_Bcut.src.ViewModels
{
    public class MainViewModel : BindableBase
    {
        private IDialogHostService dialogHostService;

        private ObservableCollection<AudioFile> files;
        /// <summary>
        /// 音频文件的列表
        /// </summary>
        public ObservableCollection<AudioFile> Files
        {
            get => files;
            set { files = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<STSTask> tasks;
        /// <summary>
        /// 任务队列
        /// </summary>
        public ObservableCollection<STSTask> Tasks
        {
            get => tasks;
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

        public bool StartButtonEnabled => !isrunning;
        public static Config? config;
        public int OutputFmt { get; set; }

        public DelegateCommand<object> OpenFileCommand { get; private set; }
        public DelegateCommand<object> StartRunCommand { get; private set; }
        public DelegateCommand<object> DeleteCommand { get; private set; }
        public DelegateCommand<string> ShowDialogCommand { get; private set; }

        public MainViewModel(IDialogHostService dialogHostService)
        {
            OpenFileCommand = new DelegateCommand<object>(OpenFile);
            StartRunCommand = new DelegateCommand<object>(StartRun);
            DeleteCommand = new DelegateCommand<object>(Delete);
            ShowDialogCommand = new DelegateCommand<string>(ShowDialog);
            Files = new();
            Tasks = new();
            this.dialogHostService = dialogHostService;
            config = ConfigUtil.ReadConfig();
        }

        void ShowDialog(string view)
        {
            dialogHostService.ShowDialog(view,null,dialogHostName:"root");
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
            if (openFileDialog.ShowDialog() == CommonFileDialogResult.Cancel || !openFileDialog.FileNames.Any())
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
            Tasks.Clear();
            var i = 1;
            foreach(var file in Files)
            {
                Tasks.Add(new STSTask(i++,OutputFmt,file));
            }
            SetTaskState(true);
            foreach(var task in Tasks)
            {
                _ = await task.Run();
            }
            SetTaskState(false);
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

    }
}