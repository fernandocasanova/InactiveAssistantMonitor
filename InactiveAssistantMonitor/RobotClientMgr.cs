using System;
using System.IO;
using System.Threading.Tasks;
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

        public bool IsRobotConnectedToOrchestrator()
        {
            try
            {
                DateTime last_checked = FileManager.Instance.LastOrchestratorChecked();

                if (last_checked >= DateTime.Now)
                {
                    FileManager.Instance.Log("> RobotConnectedToOrchestrator: True (no check until " + last_checked.ToString("s") + ")");
                    return true;
                }

                FileManager.Instance.TouchLastOrchestratorCheckedFile();

                var processes = myRobotClient.GetProcesses().Result;

                if (processes.Count > 0)
                {
                    FileManager.Instance.Log("> RobotConnectedToOrchestrator: True");
                    return true;
                }
            }
            catch(Exception ex)
            {
                FileManager.Instance.Log("!! UiPath Robot API is not present. Assuming no connectivity. " + ex.Message);
            }
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