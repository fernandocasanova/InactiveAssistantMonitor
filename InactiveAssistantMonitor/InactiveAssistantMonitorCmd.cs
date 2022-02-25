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

        int countInactive;
        string studioPath;

        System.Timers.Timer timerOrchestratorConnectivity;
        System.Timers.Timer timerInactiveProcess;

        public InactiveAssistantMonitorCmd()
        {
            FileManager.Instance.Log("InactiveAssistantMonitorCmd started...");

            this.studioPath = "";
            if (System.IO.Directory.Exists(Properties.Settings.Default.UiPathAssistantPathX86.Trim('\\')))
            {
                this.studioPath = Properties.Settings.Default.UiPathAssistantPathX86.Trim('\\');
            }
            else
            {
                if (System.IO.Directory.Exists(Properties.Settings.Default.UiPathAssistantPath.Trim('\\')))
                {
                    this.studioPath = Properties.Settings.Default.UiPathAssistantPath.Trim('\\');
                }
            }
            if (String.IsNullOrEmpty(this.studioPath))
            {
                FileManager.Instance.Log("UiPath Studio was not found. Please install it before you run this program.");
                MessageBox.Show("UiPath Studio was not found. Please install it before you run this program.", "UiPath Studio not found!", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.ServiceNotification);

                var timerInactiveProcess = new System.Threading.Timer((e) =>
                {
                    this.Exit();
                }, null, TimeSpan.FromMilliseconds(250), TimeSpan.FromMilliseconds(-1));

                return;
            }
            else
            {
                notifyIcon.Text = "Inactive UiPath Assistant Monitor";

                MenuItem connectAssistantMenuItem = new MenuItem("Connect Assistant to Orchestrator", new EventHandler(ConnectAssistantEH));
                MenuItem startAssistantMenuItem = new MenuItem("Start UiPath Assistant", new EventHandler(StartAssistantEH));
                MenuItem closeAssistantMenuItem = new MenuItem("Close UiPath Assistant", new EventHandler(KillAssistantEH));
                MenuItem exitMenuItem = new MenuItem("Exit", new EventHandler(ExitEH));

                this.timerInactiveProcess = new System.Timers.Timer(1000.0 * Properties.Settings.Default.PeriodIntervalInSeconds);
                this.timerInactiveProcess.Elapsed += CheckProcessRunnningEH;
                this.timerInactiveProcess.AutoReset = true;
                this.timerInactiveProcess.Start();

                this.timerOrchestratorConnectivity = new System.Timers.Timer(1000.0 * Properties.Settings.Default.PeriodIntervalConnectionToOrchestrator);
                this.timerOrchestratorConnectivity.Elapsed += checkConnectivityToOrchestratorEH;
                this.timerOrchestratorConnectivity.AutoReset = true;
                var delayedStart = new System.Threading.Timer((e) =>
                {
                    this.timerOrchestratorConnectivity.Start();
                }, null, TimeSpan.FromSeconds(Properties.Settings.Default.OffsetChecks), TimeSpan.FromMilliseconds(-1));

                notifyIcon.Icon = InactiveAssistantMonitor.Properties.Resources.AppIcon;

                notifyIcon.DoubleClick += ShowMessageBoxEH;
                notifyIcon.ContextMenu = new ContextMenu(new MenuItem[] {
                    connectAssistantMenuItem,
                    startAssistantMenuItem,
                    closeAssistantMenuItem,
                    exitMenuItem
                });
                notifyIcon.Visible = true;
            }
        }

        private void CheckProcessRunnning()
        {
            FileManager.Instance.Log("CheckProcessRunnning started...");

            Process thisProcess = Process.GetCurrentProcess();

            ITerminalServicesManager manager = new TerminalServicesManager();
            using (ITerminalServer server = manager.GetLocalServer())
            {
                server.Open();

                foreach (ITerminalServicesSession session in server.GetSessions())
                {
                    if (session.SessionId != 0)
                    {
                        if (session.SessionId == thisProcess.SessionId)
                        {
                            if (session.ConnectionState == ConnectionState.Active)
                            {
                                FileManager.Instance.Log("> Session active...");

                                if (!this.IsAssistantOn())
                                {
                                    this.StartAssistant();
                                }
                                this.countInactive = 1;
                            }
                            else
                            {
                                FileManager.Instance.Log("> Session inactive...");

                                if (this.IsAssistantOn())
                                {
                                    if (this.countInactive > Properties.Settings.Default.NumberOfIntervalsUntilKill)
                                    {
                                        this.KillAssistant();
                                    }
                                    else
                                    {
                                        FileManager.Instance.Log("> Assistant in inactive session. countInactive: " + countInactive.ToString());
                                        this.countInactive++;
                                    }
                                }
                            }
                        }
                    }
                }
            }

        }

        private void CheckProcessRunnningEH(object sender, EventArgs e)
        {
            try
            {
                this.CheckProcessRunnning();
            }
            catch(Exception ex)
            {
                FileManager.Instance.Log("!! Exception: " + ex.Message);
            }
        }

        private void StartAssistant()
        {
            Process process = new Process();

            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.StartInfo.FileName = this.studioPath + "\\" + 
                                         Properties.Settings.Default.UiPathAssistantExe;

            process.Start();
            process.WaitForExit();

            FileManager.Instance.Log("> Assistant started!");
        }

        private void StartAssistantEH(object sender, EventArgs e)
        {
            try 
            {
                this.StartAssistant();
            }
            catch(Exception ex)
            {
                FileManager.Instance.Log("!! Exception: " + ex.Message);
            }
        }

        private bool IsAssistantOn()
        {
            Process[] runningProcesses = Process.GetProcesses();
            Process thisProcess = Process.GetCurrentProcess();
            
            Process[] sameAsOriginalSession =
                runningProcesses.Where(p => p.SessionId == thisProcess.SessionId).ToArray();

            foreach (var p in sameAsOriginalSession)
            {
                if (p.ProcessName == "UiPath.Assistant")
                {
                    FileManager.Instance.Log("> Assistant is ON");
                    return true;
                }
            }
            FileManager.Instance.Log("> Assistant is OFF");
            return false;
        }

        private void KillAssistant()
        {
            Process[] runningProcesses = Process.GetProcesses();
            Process thisProcess = Process.GetCurrentProcess();

            Process[] sameAsOriginalSession =
                runningProcesses.Where(p => p.SessionId == thisProcess.SessionId).ToArray();

            foreach (var p in sameAsOriginalSession)
            {
                if (Regex.Match(p.ProcessName, Properties.Settings.Default.UiPathProcessRegexp).Success)
                {
                    p.Kill();
                }
            }
            FileManager.Instance.Log("> Assistant Killed!");
        }

        private void KillAssistantEH(object sender, EventArgs e)
        {
            try
            {
                this.KillAssistant();
            }
            catch (Exception ex)
            {
                FileManager.Instance.Log("!! Exception: " + ex.Message);
            }
        }


        private void DisconnectAndConnectRobot()
        {
            // Disconnect
            FileManager.Instance.Log("> Disconnect robot");
            Process disconnectProcess = new Process();
            disconnectProcess.StartInfo.RedirectStandardOutput = true;
            disconnectProcess.StartInfo.RedirectStandardError = true;
            disconnectProcess.StartInfo.UseShellExecute = false;
            disconnectProcess.StartInfo.CreateNoWindow = true;

            disconnectProcess.StartInfo.FileName = this.studioPath + "\\" +
                                         Properties.Settings.Default.UiPathRobot;
            disconnectProcess.StartInfo.Arguments = "disconnect";

            disconnectProcess.Start();
            disconnectProcess.WaitForExit();

            // Connect
            FileManager.Instance.Log("> Connect robot");
            Process connectProcess = new Process();
            connectProcess.StartInfo.RedirectStandardOutput = true;
            connectProcess.StartInfo.RedirectStandardError = true;
            connectProcess.StartInfo.UseShellExecute = false;
            connectProcess.StartInfo.CreateNoWindow = true;

            connectProcess.StartInfo.FileName = this.studioPath + "\\" +
                                            Properties.Settings.Default.UiPathRobot;
            connectProcess.StartInfo.Arguments = "connect" +
                                            " --url " + Properties.Settings.Default.OrchestratorUrl.Trim('/') +
                                            " --key " + Properties.Settings.Default.MachineKey;

            connectProcess.Start();
            connectProcess.WaitForExit();

            FileManager.Instance.Log("> Robot connected!");
        }

        private bool hasConnectivityToOrchestrator()
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(Properties.Settings.Default.OrchestratorUrl.Trim('/') + "/api/Status/Get");
            request.Headers.Add("UserAgent", "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1)");
            request.AllowAutoRedirect = false;

            try
            {
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    FileManager.Instance.Log("> Connectivity OK + Can connect to the Orchestrator");
                    return true;
                }
            }
            catch (Exception e)
            {
            }

            FileManager.Instance.Log("> Connectivity KO - Cannot connect to the Orchestrator");
            return false;
        }

        private void checkConnectivityToOrchestratorEH(object sender, ElapsedEventArgs e)
        {
            try
            {
                FileManager.Instance.Log("checkConnectivityToOrchestratorEH started... ");

                if (RobotClientMgr.Instance.IsRobotConnectedToOrchestrator())
                {
                    FileManager.Instance.Log("> RobotConnectedToOrchestrator: True");
                }
                else
                {
                    FileManager.Instance.Log("> RobotConnectedToOrchestrator: False");

                    if (this.hasConnectivityToOrchestrator())
                    {
                        this.DisconnectAndConnectRobot();
                    }
                }

            }
            catch (Exception ex)
            {
                FileManager.Instance.Log("!! Exception: " + ex.Message);
            }
        }

        private void ConnectAssistantEH(object sender, EventArgs e)
        {
            try
            {
                this.DisconnectAndConnectRobot();
            }
            catch(Exception ex)
            {
                FileManager.Instance.Log("!! Exception: " + ex.Message);
            }
        }

        private void ShowMessageBoxEH(object sender, EventArgs e)
        {
            MessageBox.Show("This program stops the UiPath Assistant when not in use and restarts it when in use.", "About InactiveAssistantMonitor", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Exit()
        {
            FileManager.Instance.Log("Exiting...");
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
