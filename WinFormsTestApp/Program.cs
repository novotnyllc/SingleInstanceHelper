using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using SingleInstanceHelper;

namespace WinFormsTestApp
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static async Task Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var first = await ApplicationActivator.LaunchOrReturnAsync(otherInstance =>
            {
                MessageBox.Show("got data: " + otherInstance.Skip(1).FirstOrDefault());
            });
            if (!first)
                return;
            Application.Run(new Form1());
        }
    }
}
