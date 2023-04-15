using Prism.Services.Dialogs;
using System.Threading.Tasks;

namespace STS_Bcut.src.Common
{
    public interface IDialogHostService : IDialogService
    {
        string DialogHostName { get; set; }

        Task<IDialogResult> ShowDialog(string name, IDialogParameters? parameters, string dialogHostName = "Root");
    }
}
