using Prism.Commands;
using Prism.Services.Dialogs;

namespace STS_Bcut.src.Common;

public interface IDialogHostAware
{
    string DialogHostName { get; set; }
    DelegateCommand SaveCommand { get; set; }
    DelegateCommand CancelCommand { get; set; }
    void OnDialogOpened(IDialogParameters parameters);
}