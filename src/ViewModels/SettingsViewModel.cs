using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using STS_Bcut.src.Common;

namespace STS_Bcut.src.ViewModels;

public class SettingsViewModel : BindableBase, IDialogHostAware
{
    public Config config
    {
        get => MainViewModel.config;
        set => MainViewModel.config = value;
    }

    public string DialogHostName { get; set; }
    public DelegateCommand SaveCommand { get; set; }
    public DelegateCommand CancelCommand { get; set; }

    public void OnDialogOpened(IDialogParameters parameters)
    {
    }
}