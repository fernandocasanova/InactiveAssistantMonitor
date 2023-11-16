using Cassia;

using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Timers;
using System.Runtime.InteropServices;
using InactiveAssistantMonitor.Properties;
using System.Threading;

namespace InactiveAssistantMonitor
{
    public class InactiveAssistantMonitorCmd : ApplicationContext
    {
        NotifyIcon notifyIcon = new NotifyIcon();

        string studioPath;

        public int PeriodIntervalConnectionToOrchestratorInSeconds;
        public int PeriodIntervalActivityCheckInSeconds;
        public int NumberOfIntervalsSessionActivityCheckUntilKill;
        public int NumberOfIntervalsWithoutInputUntilKill;

        public int countInactive;

        System.Timers.Timer timerOrchestratorConnectivity;
        System.Timers.Timer timerInactiveProcess;

        private struct LASTINPUTINFO
        {
            public uint cbSize;
            public uint dwTime;
        }

        [DllImport("user32.dll")]
        static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        ~InactiveAssistantMonitorCmd()
        {
            this.Exit();
        }

        public InactiveAssistantMonitorCmd()
        {
            this.countInactive = 0;

            FileManager.Instance.Log("InactiveAssistantMonitorCmd started...");

            this.studioPath = "";
            if (System.IO.File.Exists(Settings.Default.UiPathAssistantPathX86.Trim('\\') + "\\" +
                                         Settings.Default.UiPathAssistantExe ))
            {
                this.studioPath = Settings.Default.UiPathAssistantPathX86.Trim('\\');
            }
            else
            {
                if (System.IO.File.Exists(Settings.Default.UiPathAssistantPath.Trim('\\') + "\\" +
                                         Settings.Default.UiPathAssistantExe ))
                {
                    this.studioPath = Settings.Default.UiPathAssistantPath.Trim('\\');
                }
            }

            this.PeriodIntervalConnectionToOrchestratorInSeconds = Settings.Default.PeriodIntervalConnectionToOrchestratorInSeconds;
            this.PeriodIntervalActivityCheckInSeconds = Settings.Default.PeriodIntervalActivityCheckInSeconds;
            this.NumberOfIntervalsSessionActivityCheckUntilKill = Settings.Default.NumberOfIntervalsSessionActivityCheckUntilKill;
            this.NumberOfIntervalsWithoutInputUntilKill = Settings.Default.NumberOfIntervalsWithoutInputUntilKill;

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

                this.startTimerInactiveProcess();

                this.startTimerOrchestratorConnectivity();

                notifyIcon.Icon = Resources.AppIcon;

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

        private void SettingsEH(object sender, EventArgs e)
        {
            SettingsForm settingsForm = new SettingsForm(this);
            settingsForm.numberOfIntervalsTextBox.Text = this.NumberOfIntervalsSessionActivityCheckUntilKill.ToString();
            settingsForm.intervalTextBox.Text = this.PeriodIntervalActivityCheckInSeconds.ToString();
            settingsForm.Show();
        }

        private void startTimerOrchestratorConnectivity()
        {
            this.timerOrchestratorConnectivity = new System.Timers.Timer(1000.0 * this.PeriodIntervalConnectionToOrchestratorInSeconds);
            this.timerOrchestratorConnectivity.Elapsed += checkConnectivityToOrchestratorEH;
            this.timerOrchestratorConnectivity.AutoReset = true;
            var delayedStart = new System.Threading.Timer((e) =>
            {
                try
                {
                    this.timerOrchestratorConnectivity.Start();
                }
                catch(Exception)
                {
                }
            }, null, TimeSpan.FromSeconds(Settings.Default.OffsetConnectionActivityChecksInSeconds), TimeSpan.FromMilliseconds(-1));
        }

        private void stopTimerOrchestratorConnectivity()
        {
            this.timerOrchestratorConnectivity.Stop();
            this.timerOrchestratorConnectivity.Dispose();
        }

        public void startTimerInactiveProcess()
        {
            this.timerInactiveProcess = new System.Timers.Timer(1000.0 * this.PeriodIntervalActivityCheckInSeconds);
            this.timerInactiveProcess.Elapsed += CheckProcessRunnningEH;
            this.timerInactiveProcess.AutoReset = true;
            this.timerInactiveProcess.Start();
        }

        public void stopTimerInactiveProcess()
        {
            this.timerInactiveProcess.Stop();
            this.timerInactiveProcess.Dispose();
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
                                uint secondsSinceLastInput = GetLastInputTime();

                                int maxSecondsInactiveBeforeKill = this.NumberOfIntervalsWithoutInputUntilKill * this.PeriodIntervalActivityCheckInSeconds;

                                FileManager.Instance.Log("> Session active... secondsSinceLastInput: " + secondsSinceLastInput.ToString() + " / " + maxSecondsInactiveBeforeKill.ToString());

                                if (secondsSinceLastInput < maxSecondsInactiveBeforeKill)
                                {
                                    // session active with activity
                                    if (!this.IsAssistantOn())
                                    {
                                        this.StartAssistant();
                                    }
                                    this.countInactive = 1;
                                }
                                else
                                {
                                    // session active without activity
                                    if (this.IsAssistantOn())
                                    {
                                        this.KillAssistant("No input or activity");
                                    }
                                }
                            }
                            else
                            {
                                FileManager.Instance.Log("> Session inactive...");

                                if (this.IsAssistantOn())
                                {
                                    if (this.countInactive > Settings.Default.NumberOfIntervalsSessionActivityCheckUntilKill)
                                    {
                                        this.KillAssistant("Session inactive");
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

        static uint GetLastInputTime()
        {
            uint idleTime = 0;
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            //Gets the number of milliseconds elapsed since the system started.
            uint envTicks = (uint)Environment.TickCount;

            if (GetLastInputInfo(ref lastInputInfo))
            {
                uint lastInputTick = lastInputInfo.dwTime;
                idleTime = envTicks - lastInputTick;
            }

            return ((idleTime > 0) ? (idleTime / 1000) : 0); // in seconds
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

            process.StartInfo.FileName = this.studioPath + "\\" + Settings.Default.UiPathAssistantExe;

            process.Start();
            FileManager.Instance.Log("> Assistant started!");
        }

        private void StartAssistantEH(object sender, EventArgs e)
        {
            try 
            {
                this.StartAssistant();
                this.startTimerInactiveProcess();
                this.startTimerOrchestratorConnectivity();
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

        private void KillAssistant(string source = "")
        {
            Process[] runningProcesses = Process.GetProcesses();
            Process thisProcess = Process.GetCurrentProcess();

            Process[] sameAsOriginalSession =
                runningProcesses.Where(p => p.SessionId == thisProcess.SessionId).ToArray();

            foreach (var p in sameAsOriginalSession)
            {
                if (p.ProcessName == "UiPath.Executor")
                {
                    try
                    {
                        p.Kill();
                    }
                    catch (Exception ex)
                    {
                        FileManager.Instance.Log("!! Exception for process : " + p.ProcessName + " : " + ex.Message);
                    }
                }
            }

            foreach (var p in sameAsOriginalSession)
            {
                if (Regex.Match(p.ProcessName, "^(UiPath\\..+)$").Success && p.ProcessName != "UiPath.Executor")
                {
                    try
                    {
                        p.Kill();
                    }
                    catch (Exception ex)
                    {
                        FileManager.Instance.Log("!! Exception for process : " + p.ProcessName + " : " + ex.Message);
                    }
                }
            }
            FileManager.Instance.Log("> Assistant Killed! " + source);
        }

        private void KillAssistantEH(object sender, EventArgs e)
        {
            try
            {
                this.stopTimerInactiveProcess();
                this.stopTimerOrchestratorConnectivity();
                this.KillAssistant("User pressed button");
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

            disconnectProcess.StartInfo.FileName = this.studioPath + "\\" + Settings.Default.UiPathRobot;
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
                                            Settings.Default.UiPathRobot;
            connectProcess.StartInfo.Arguments = "connect" +
                                            " --url " + Settings.Default.OrchestratorUrl.Trim('/') +
                                            " --key " + Settings.Default.MachineKey;

            connectProcess.Start();
            connectProcess.WaitForExit();

            FileManager.Instance.Log("> Robot connected!");
        }

        private void checkConnectivityToOrchestratorEH(object sender, ElapsedEventArgs e)
        {
            this.checkConnectivityToOrchestrator();
        }

        private void checkConnectivityToOrchestrator(bool force_connection = false)
        {
            try
            {
                FileManager.Instance.Log("checkConnectivityToOrchestratorEH started... ");

                if (!RobotClientMgr.Instance.IsRobotConnectedToOrchestrator(force_connection))
                {
                    FileManager.Instance.Log("> RobotConnectedToOrchestrator: False");

                    if (RobotClientMgr.Instance.hasConnectivityToOrchestrator())
                    {
                        this.DisconnectAndConnectRobot();

                        Thread.Sleep(1000);

                        if (!RobotClientMgr.Instance.IsRobotConnectedToOrchestrator(force_connection, true))
                        {
                            FileManager.Instance.Log("> RobotConnectedToOrchestrator: False - Second and last attempt");
                        }
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
                FileManager.Instance.Log("Force Connection to Assistant");
                this.checkConnectivityToOrchestrator(true);
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

            try
            {
                this.stopTimerInactiveProcess();
                this.stopTimerOrchestratorConnectivity();
                this.KillAssistant("Shutting down");
            }
            catch (Exception ex)
            {
                FileManager.Instance.Log("!! Exception: " + ex.Message);
            }

            Application.Exit();
        }

        private void ExitEH(object sender, EventArgs e)
        {
            this.Exit();
        }
    }
}
