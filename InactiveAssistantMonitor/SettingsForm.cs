
using System;
using System.Windows.Forms;

namespace InactiveAssistantMonitor
{
    public partial class SettingsForm : Form
    {
        InactiveAssistantMonitorCmd aCmd;

        public SettingsForm(InactiveAssistantMonitorCmd iCmd)
        {
            this.aCmd = iCmd;
            InitializeComponent();
        }

        private void OKButton_Click(object sender, System.EventArgs e)
        {
            try
            {
                aCmd.NumberOfIntervalsSessionActivityCheckUntilKill = int.Parse(this.numberOfIntervalsTextBox.Text);
                aCmd.PeriodIntervalActivityCheckInSeconds = int.Parse(this.intervalTextBox.Text);
                if (int.Parse(this.numberOfIntervalsTextBox.Text) <= 0)
                {
                    throw new Exception("Integer not valid");
                }
                aCmd.stopTimerInactiveProcess();
                aCmd.startTimerInactiveProcess();
            }
            catch (Exception exc)
            {
                MessageBox.Show("Please input an integer different from 0.");
                return;
            }
            this.Hide();
        }

        private void CancelButton_Click(object sender, System.EventArgs e)
        {
            this.Hide();
        }
    }
}
