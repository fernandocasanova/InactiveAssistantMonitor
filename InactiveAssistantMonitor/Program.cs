using System;
using System.Windows.Forms;
using System.Threading;

namespace InactiveAssistantMonitor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            bool isNew;
            Mutex mutex = new Mutex(true, Application.ProductName, out isNew);
            if (isNew)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                Application.Run(new InactiveAssistantMonitorCmd());
                mutex.ReleaseMutex();
            }
        }
    }
}