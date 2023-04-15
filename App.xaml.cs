using Prism.DryIoc;
using Prism.Ioc;
using System.Windows;
using Prism.Services.Dialogs;
using STS_Bcut.src;
using STS_Bcut.src.ViewModels;
using STS_Bcut.src.Views;
using STS_Bcut.src.Common;

namespace STS_Bcut
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainView>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<IDialogHostService, DialogHostService>();

            containerRegistry.RegisterForNavigation<AboutView,AboutViewModel>();
            containerRegistry.RegisterForNavigation<SettingsView,SettingsViewModel>();
            containerRegistry.RegisterForNavigation<MainView, MainViewModel>();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            ConfigUtil.WriteConfig(MainViewModel.config);
        }
    }
}
