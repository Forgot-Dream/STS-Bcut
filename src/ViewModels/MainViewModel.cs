﻿using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Microsoft.WindowsAPICodePack.Dialogs;
using Prism.Commands;
using Prism.Mvvm;
using STS_Bcut.src.Common;

namespace STS_Bcut.src.ViewModels;

public class MainViewModel : BindableBase
{
    public static Config? config;
    private readonly IDialogHostService dialogHostService;

    private ObservableCollection<AudioFile> files;

    /// <summary>
    ///     任务是否在运行
    /// </summary>
    private bool isrunning;

    private ObservableCollection<STSTask> tasks;

    public MainViewModel(IDialogHostService dialogHostService)
    {
        OpenFileCommand = new DelegateCommand<object>(OpenFile);
        StartRunCommand = new DelegateCommand<object>(StartRun);
        DeleteCommand = new DelegateCommand<object>(Delete);
        ShowDialogCommand = new DelegateCommand<string>(ShowDialog);
        Files = new ObservableCollection<AudioFile>();
        Tasks = new ObservableCollection<STSTask>();
        this.dialogHostService = dialogHostService;
        config = ConfigUtil.ReadConfig();
    }

    /// <summary>
    ///     音频文件的列表
    /// </summary>
    public ObservableCollection<AudioFile> Files
    {
        get => files;
        set
        {
            files = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    ///     任务队列
    /// </summary>
    public ObservableCollection<STSTask> Tasks
    {
        get => tasks;
        set
        {
            tasks = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    ///     是否所有都被选中 绑定用
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
            if (value.HasValue) SelectAll(value.Value, Files);
            RaisePropertyChanged();
        }
    }

    public bool StartButtonEnabled => !isrunning;
    public int OutputFmt { get; set; }

    public DelegateCommand<object> OpenFileCommand { get; private set; }
    public DelegateCommand<object> StartRunCommand { get; private set; }
    public DelegateCommand<object> DeleteCommand { get; private set; }
    public DelegateCommand<string> ShowDialogCommand { get; private set; }

    private void ShowDialog(string view)
    {
        dialogHostService.ShowDialog(view, null, "root");
    }

    /// <summary>
    ///     打开文件的方法
    /// </summary>
    /// <param name="obj"></param>
    private void OpenFile(object obj)
    {
        CommonOpenFileDialog openFileDialog = new("请选择你要打开的音频文件")
        {
            Multiselect = true
        };
        if (openFileDialog.ShowDialog() == CommonFileDialogResult.Cancel || !openFileDialog.FileNames.Any())
            return;
        foreach (var path in openFileDialog.FileNames)
            Files.Add(new AudioFile
            {
                FullName = Path.GetFileName(path),
                FullPath = path,
                IsSelected = false
            });
    }

    /// <summary>
    ///     删除文件的方法
    /// </summary>
    /// <param name="obj"></param>
    private void Delete(object obj)
    {
        if (Files == null || Files.Count == 0)
            return;
        var deletelist = Files.Where(x => x.IsSelected).ToList();
        foreach (var file in deletelist)
            Files.Remove(file);
    }

    /// <summary>
    ///     开始转字幕
    /// </summary>
    /// <param name="obj"></param>
    private async void StartRun(object obj)
    {
        Tasks.Clear();
        var i = 1;
        foreach (var file in Files) Tasks.Add(new STSTask(i++, OutputFmt, file));
        SetTaskState(true);
        foreach (var task in Tasks) _ = await task.Run();
        SetTaskState(false);
    }

    /// <summary>
    ///     设置状态
    /// </summary>
    /// <param name="state"></param>
    private void SetTaskState(bool state)
    {
        isrunning = state;
        RaisePropertyChanged(nameof(StartButtonEnabled));
    }

    /// <summary>
    ///     选择所有
    /// </summary>
    /// <param name="select"></param>
    /// <param name="audioFiles"></param>
    private static void SelectAll(bool select, ObservableCollection<AudioFile> audioFiles)
    {
        if (audioFiles == null || audioFiles.Count == 0)
            return;
        foreach (var model in audioFiles) model.IsSelected = select;
    }
}