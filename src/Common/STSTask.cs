using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using FFMpegCore.Exceptions;
using Prism.Mvvm;

namespace STS_Bcut.src.Common;

public class STSTask : BindableBase
{
    private readonly AudioFile AudioFile;
    private BcutAPI bcutAPI;

    private string iconKind;

    private string iconVisibility;
    private readonly int OutputFmt;

    private string showProgressBar;

    private string statuscolor;
    private string tip;

    public STSTask(int number, int outputfmt, AudioFile audioFile)
    {
        TaskNumber = number;
        StatusColor = "Blue";
        AudioFile = audioFile;
        OutputFmt = outputfmt;

        ShowProgressBar = "Visible";
        IconKind = "SuccessBold";
        IconVisibility = "Collapsed";

        Tip = "等待识别中...";
    }

    /// <summary>
    ///     提示
    /// </summary>
    public string Tip
    {
        get => tip;
        set
        {
            tip = value;
            RaisePropertyChanged();
        }
    }

    /// <summary>
    ///     状态提示的颜色
    /// </summary>
    public string StatusColor
    {
        get => statuscolor;
        set
        {
            statuscolor = value;
            RaisePropertyChanged();
        }
    }

    public string ShowProgressBar
    {
        get => showProgressBar;
        set
        {
            showProgressBar = value;
            RaisePropertyChanged();
        }
    }

    public string IconKind
    {
        get => iconKind;
        set
        {
            iconKind = value;
            RaisePropertyChanged();
        }
    }

    public string IconVisibility
    {
        get => iconVisibility;
        set
        {
            iconVisibility = value;
            RaisePropertyChanged();
        }
    }


    public int TaskNumber { get; set; }

    public async Task<bool> Run()
    {
        return await Task.Run(() =>
        {
            bcutAPI = new BcutAPI(this, new FileInfo(AudioFile.FullPath));
            try
            {
                var data = bcutAPI.Run();
                switch (OutputFmt)
                {
                    case 0:
                        File.WriteAllText(AudioFile.FullPath.Replace(Path.GetExtension(AudioFile.FullPath), ".srt"),
                            data.ToSrt());
                        break;
                    case 1:
                        File.WriteAllText(AudioFile.FullPath.Replace(Path.GetExtension(AudioFile.FullPath), ".lrc"),
                            data.ToLrc());
                        break;
                    case 2:
                        File.WriteAllText(AudioFile.FullPath.Replace(Path.GetExtension(AudioFile.FullPath), ".txt"),
                            data.ToTxt());
                        break;
                }

                StatusColor = "Green";
                Tip = "字幕已导出";
                ShowProgressBar = "Collapsed";
                IconVisibility = "Visible";
                return true;
            }
            catch (FFMpegException e)
            {
                Debug.WriteLine(e.Message);
                ThrowException("FFMpeg转码出错");
            }
            catch (HttpRequestException e)
            {
                Debug.WriteLine(e.Message);
                ThrowException("请求出错");
            }
            catch (ValueUnavailableException e)
            {
                Debug.WriteLine(e.Message);
                ThrowException("无可用数据");
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
                MessageBox.Show(e.Message);
                ThrowException("未知错误 请联系作者提供复现过程");
            }

            return false;
        });
    }

    private void ThrowException(string message)
    {
        Tip = message;
        StatusColor = "Red";
        ShowProgressBar = "Collapsed";
        IconVisibility = "Visible";
        IconKind = "ErrorOutline";
    }
}