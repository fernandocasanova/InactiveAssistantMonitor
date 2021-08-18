using Cassia;

using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Timers;

namespace InactiveAssistantMonitor
{
    public class InactiveAssistantMonitorCmd : ApplicationContext
    {
        NotifyIcon notifyIcon = new NotifyIcon();

        int originalSessionId;
        int countInactive;
        bool killedAssistant;

        int countOrchestratorConnectivityAttemps;

        System.Timers.Timer timerOrchestratorConnectivity;

        public InactiveAssistantMonitorCmd()
        {
            notifyIcon.Text = "Inactive UiPath Assistant Monitor";

            MenuItem connectAssistantMenuItem = new MenuItem("Connect Assistant to Orchestrator", new EventHandler(ConnectAssistantEH));
            MenuItem startAssistantMenuItem = new MenuItem("Start UiPath Assistant", new EventHandler(StartAssistantEH));
            MenuItem closeAssistantMenuItem = new MenuItem("Close UiPath Assistant", new EventHandler(KillAssistantEH));
            MenuItem exitMenuItem = new MenuItem("Exit", new EventHandler(ExitEH));

            this.killedAssistant = false;

            var inactivePeriodTimeSpan = TimeSpan.FromSeconds(Properties.Settings.Default.PeriodIntervalInSeconds);
            var orchestratorInterval = Properties.Settings.Default.PeriodIntervalConnectionToOrchestrator;

            using (Process CurrentProcess = Process.GetCurrentProcess())
            {
                this.originalSessionId = CurrentProcess.SessionId;
            }

            var timerInactiveProcess = new System.Threading.Timer((e) =>
            {
                CheckProcessRunnning();
            }, null, inactivePeriodTimeSpan, inactivePeriodTimeSpan);

            this.countOrchestratorConnectivityAttemps = 0;

            // Create a timer with a two second interval.
            this.timerOrchestratorConnectivity = new System.Timers.Timer(1000.0 * orchestratorInterval);
            // Hook up the Elapsed event for the timer. 
            this.timerOrchestratorConnectivity.Elapsed += checkConnectivityToOrchestrator;
            this.timerOrchestratorConnectivity.AutoReset = true;
            this.timerOrchestratorConnectivity.Enabled = true;

            notifyIcon.Icon = InactiveAssistantMonitor.Properties.Resources.AppIcon;

            notifyIcon.DoubleClick += new EventHandler(ShowMessageBox);
            notifyIcon.ContextMenu = new ContextMenu(new MenuItem[] {
                connectAssistantMenuItem,
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

            process.StartInfo.FileName = Properties.Settings.Default.UiPathAssistantPath.Trim('\\') + "\\" + 
                                         Properties.Settings.Default.UiPathAssistantExe;

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


        private bool hasConnectivityToOrchestrator()
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(Properties.Settings.Default.OrchestratorUrl.Trim('/') + "/api/Status/Get");
            request.Headers.Add("UserAgent", "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1)");
            request.AllowAutoRedirect = false;
            
            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if(response.StatusCode == HttpStatusCode.OK)
                {
                    return true;
                }
            }
            catch (Exception e)
            {
            }

            return false;
        }

        private void DisconnectAndConnectRobot()
        {
            // Disconnect
            Process disconnectProcess = new Process();
            disconnectProcess.StartInfo.RedirectStandardOutput = true;
            disconnectProcess.StartInfo.RedirectStandardError = true;
            disconnectProcess.StartInfo.UseShellExecute = false;
            disconnectProcess.StartInfo.CreateNoWindow = true;

            disconnectProcess.StartInfo.FileName = Properties.Settings.Default.UiPathAssistantPath.Trim('\\') + "\\" +
                                         Properties.Settings.Default.UiPathRobot;
            disconnectProcess.StartInfo.Arguments = "disconnect";

            disconnectProcess.Start();

            // Connect
            Process connectProcess = new Process();
            connectProcess.StartInfo.RedirectStandardOutput = true;
            connectProcess.StartInfo.RedirectStandardError = true;
            connectProcess.StartInfo.UseShellExecute = false;
            connectProcess.StartInfo.CreateNoWindow = true;

            connectProcess.StartInfo.FileName = Properties.Settings.Default.UiPathAssistantPath.Trim('\\') + "\\" +
                                            Properties.Settings.Default.UiPathRobot;
            connectProcess.StartInfo.Arguments = "connect" +
                                            " --url " + Properties.Settings.Default.OrchestratorUrl.Trim('/') +
                                            " --key " + Properties.Settings.Default.MachineKey;

            connectProcess.Start();
        }

        private void checkConnectivityToOrchestrator(object sender, ElapsedEventArgs e)
        {
            this.countOrchestratorConnectivityAttemps++;

            if (this.countOrchestratorConnectivityAttemps >= Properties.Settings.Default.NumberOfChecksOfOrchestrator)
            {
                this.timerOrchestratorConnectivity.Stop();
                this.timerOrchestratorConnectivity.Dispose();
            }
            else
            {
                if(hasConnectivityToOrchestrator())
                {
                    this.DisconnectAndConnectRobot();

                    this.timerOrchestratorConnectivity.Stop();
                    this.timerOrchestratorConnectivity.Dispose();
                }
            }
        }

        private void ConnectAssistantEH(object sender, EventArgs e)
        {
            this.DisconnectAndConnectRobot();
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
