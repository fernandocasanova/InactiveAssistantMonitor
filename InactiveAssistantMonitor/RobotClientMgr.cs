using InactiveAssistantMonitor.Properties;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Windows.Forms;
using UiPath.Robot.Api;

namespace InactiveAssistantMonitor
{
    public sealed class RobotClientMgr
    {
        RobotClient myRobotClient;

        private RobotClientMgr()
        {
            try
            {
                this.myRobotClient = new RobotClient();
            }
            catch(Exception ex)
            {
                FileManager.Instance.Log("!! UiPath Robot API is not present. Assuming no connectivity. " + ex.Message);
            }
        }

        public bool hasConnectivityToOrchestrator()
        {
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(Settings.Default.OrchestratorUrl.Trim('/') + "/api/Status/Get");
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

        public bool IsRobotConnectedToOrchestrator(bool force_connection = false, bool verbose = false)
        {
            DateTime next_check_time = FileManager.Instance.LastOrchestratorChecked();

            if (!force_connection)
            {
                if (next_check_time >= DateTime.Now)
                {
                    FileManager.Instance.Log("> RobotConnectedToOrchestrator: True (no check until " + next_check_time.ToString("s") + ")");
                    return true;
                }
            }

            if (this.hasConnectivityToOrchestrator())
            {
                try
                {
                    var processes = myRobotClient.GetProcesses().Result;

                    if (processes.Count > 0)
                    {
                        FileManager.Instance.Log("> RobotConnectedToOrchestrator: True");
                        FileManager.Instance.TouchLastOrchestratorCheckedFile(99999);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    FileManager.Instance.Log("!! UiPath Robot API is not present. Assuming no connectivity. " + ex.Message);
                }

                if (verbose)
                {
                    MessageBox.Show("This robot doesn't have access to the Orchestrator (no license, misconfigured). Please contact your RPA admin.", "No license/misconfigured", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            else
            {
                if (force_connection)
                {
                    MessageBox.Show("This robot cannot connect to the Orchestrator. Please check connectivity (are you connect to RAS?).", "No connectivity", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
            }
            FileManager.Instance.TouchLastOrchestratorCheckedFile(1);
            return false;
        }

        public static RobotClientMgr Instance { get { return Nested.instance; } }

        private class Nested
        {
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            static Nested()
            {
            }

            internal static readonly RobotClientMgr instance = new RobotClientMgr();
        }
    }
}