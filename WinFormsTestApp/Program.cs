using System;
using System.Collections.Generic;
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
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var first = ApplicationActivator.LaunchOrReturn(otherInstance => { MessageBox.Show("got data"); }, args);
            if(!first)
                return; 
            Application.Run(new Form1());
        }
    }
}
