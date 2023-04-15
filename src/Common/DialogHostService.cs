using MaterialDesignThemes.Wpf;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace STS_Bcut.src.Common
{
    public class DialogHostService:DialogService,IDialogHostService
    {
        private readonly IContainerExtension containerExtension;
        public DialogHostService(IContainerExtension containerExtension) : base(containerExtension)
        {
            this.containerExtension = containerExtension;
        }

        public string DialogHostName { get; set; }

        public async Task<IDialogResult> ShowDialog(string viewName, IDialogParameters parameters,
            string dialogHostName = "root")
        {
            parameters ??= new DialogParameters();

            var content = containerExtension.Resolve<object>(viewName);

            if(!(content is FrameworkElement dialogContent))
                throw new NullReferenceException("A dialog's content must be a FrameWorkElement");
            if (dialogContent is FrameworkElement view && view.DataContext is null && ViewModelLocator.GetAutoWireViewModel(view) is null)
                ViewModelLocator.SetAutoWireViewModel(view, true);

            if (!(dialogContent.DataContext is IDialogHostAware viewModel))
                throw new NullReferenceException("A dialog's ViewModel must implement the IDialogAware interface");

            viewModel.DialogHostName = dialogHostName;

            DialogOpenedEventHandler eventHandler = (sender, eventargs) =>
            {
                if (viewModel is IDialogHostAware aware)
                {
                    aware.OnDialogOpened(parameters);
                }
                eventargs.Session.UpdateContent(content);
            };

            return await DialogHost.Show(dialogContent, viewModel.DialogHostName, eventHandler) as IDialogResult;

        
    }
    }
}
