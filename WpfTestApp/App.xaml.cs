using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using SingleInstanceHelper;

namespace WpfTestApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {

            var first = ApplicationActivator.LaunchOrReturn(otherInstance => { MessageBox.Show("got data"); }, e.Args);
            if (!first)
            {
                Shutdown();
            }


            base.OnStartup(e);
        }
    }
}
