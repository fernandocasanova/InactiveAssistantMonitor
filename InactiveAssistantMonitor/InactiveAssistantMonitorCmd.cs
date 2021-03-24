using Cassia;

using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace InactiveAssistantMonitor
{
    public class InactiveAssistantMonitorCmd : ApplicationContext
    {
        NotifyIcon notifyIcon = new NotifyIcon();

        int originalSessionId;
        int countInactive;
        bool killedAssistant;

        public InactiveAssistantMonitorCmd()
        {
            notifyIcon.Text = "Inactive UiPath Assistant Monitor";

            MenuItem startAssistantMenuItem = new MenuItem("Start UiPath Assistant", new EventHandler(StartAssistantEH));
            MenuItem closeAssistantMenuItem = new MenuItem("Close UiPath Assistant", new EventHandler(KillAssistantEH));
            MenuItem exitMenuItem = new MenuItem("Exit", new EventHandler(ExitEH));

            this.killedAssistant = false;

            var startTimeSpan = TimeSpan.Zero;
            var periodTimeSpan = TimeSpan.FromSeconds(Properties.Settings.Default.PeriodIntervalInSeconds);

            using (Process CurrentProcess = Process.GetCurrentProcess())
            {
                this.originalSessionId = CurrentProcess.SessionId;
            }

            var timer = new System.Threading.Timer((e) =>
            {
                CheckProcessRunnning();
            }, null, periodTimeSpan, periodTimeSpan);

            notifyIcon.Icon = InactiveAssistantMonitor.Properties.Resources.AppIcon;

            notifyIcon.DoubleClick += new EventHandler(ShowMessageBox);
            notifyIcon.ContextMenu = new ContextMenu(new MenuItem[] {
                startAssistantMenuItem,
                closeAssistantMenuItem,
                exitMenuItem
            });
            notifyIcon.Visible = true;
        }

        private void CheckProcessRunnning()
        {
            ITerminalServicesManager manager = new TerminalServicesManager();
            using (ITerminalServer server = manager.GetLocalServer())
            {
                server.Open();

                foreach (ITerminalServicesSession session in server.GetSessions())
                {
                    if (session.SessionId != 0)
                    {
                        if (session.SessionId == this.originalSessionId)
                        {
                            if (session.ConnectionState == ConnectionState.Active)
                            {
                                this.countInactive = 0;
                                if(this.killedAssistant)
                                {
                                    this.StartAssistant();
                                    this.killedAssistant = false;
                                }
                            }
                            else
                            {
                                if (this.countInactive > Properties.Settings.Default.NumberOfIntervalsUntilKill)
                                {
                                    if(!this.killedAssistant)
                                    {
                                        this.KillAssistant();
                                        this.killedAssistant = true;
                                    }
                                }
                                else
                                {
                                    this.countInactive++;
                                }
                            }
                        }
                    }
                }
            }

        }

        private void StartAssistant()
        {
            Process process = new Process();

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.StartInfo.FileName = Properties.Settings.Default.UiPathAssistantPath;

            process.Start();
        }

        private void StartAssistantEH(object sender, EventArgs e)
        {
            this.StartAssistant();
            this.killedAssistant = false;
        }

        private void KillAssistant()
        {
            Process[] runningProcesses = Process.GetProcesses();

            Process[] sameAsOriginalSession =
                runningProcesses.Where(p => p.SessionId == this.originalSessionId).ToArray();

            foreach (var p in sameAsOriginalSession)
            {
                if (Regex.Match(p.ProcessName, Properties.Settings.Default.UiPathProcessRegexp).Success)
                {
                    p.Kill();
                }
            }
        }

        private void KillAssistantEH(object sender, EventArgs e)
        {
            this.KillAssistant();
        }

        private void ShowMessageBox(object sender, EventArgs e)
        {
            MessageBox.Show("This program stops the UiPath Assistant when not in use and restarts it when in use.", "About InactiveAssistantMonitor", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Exit()
        {
            // We must manually tidy up and remove the icon before we exit.
            // Otherwise it will be left behind until the user mouses over.
            notifyIcon.Visible = false;

            Application.Exit();
        }

        private void ExitEH(object sender, EventArgs e)
        {
            this.Exit();
        }
    }
}
