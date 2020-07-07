using System.Windows;
using SingleInstanceHelper;

namespace WpfTestApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override async void OnStartup(StartupEventArgs e)
        {
            var first = await ApplicationActivator.LaunchOrReturnAsync(
                otherInstance => { MessageBox.Show("got data"); });
            if (!first) Shutdown();

            base.OnStartup(e);
        }
    }
}
