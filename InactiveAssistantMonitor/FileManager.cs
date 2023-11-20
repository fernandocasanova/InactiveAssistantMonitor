using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Principal;
using System.Windows.Forms;

namespace InactiveAssistantMonitor
{
    public sealed class FileManager
    {
        private FileManager()
        {
            if (!Directory.Exists( this.GetUiPathFolder() ))
            {
                Directory.CreateDirectory( this.GetUiPathFolder() );
            }

            if (!Directory.Exists( this.GetLogFolder() ))
            {
                Directory.CreateDirectory( this.GetLogFolder() );
            }

            this.CleanLogFolder();

            if (!File.Exists(this.GetLogFilename()))
            {
                File.WriteAllText(this.GetLogFilename(), "");
            }

            if (!File.Exists(this.GetStatusFilename()))
            {
                File.WriteAllText(this.GetStatusFilename(), "");
            }

            this.Log("** Start of logging - Version: " + Application.ProductVersion + " **");
        }

        public void Log(string text)
        {
            int check = 0;
            int attempts = 0;
            string datenow = DateTime.Now.ToString("o");

            while (check < 1 && attempts < 1000)
            {
                try
                {
                    File.AppendAllText(this.GetLogFilename(), datenow + " : " + text + Environment.NewLine);
                    check++;
                }
                catch (Exception ex)
                {
                    System.Threading.Thread.Sleep(50 + 100 * attempts);
                    attempts++;
                }
            }
        }

        public DateTime LastOrchestratorChecked()
        {
            DateTime last = DateTime.MinValue;

            try
            {
                if (File.Exists(this.GetStatusFilename()))
                {
                    string contents = File.ReadAllText(this.GetStatusFilename());

                    Dictionary<string, string> decoded_contents = JsonConvert.DeserializeObject<Dictionary<string, string>>(contents);

                    if (decoded_contents["version"] == Application.ProductVersion)
                    {
                        DateTime output = DateTime.Parse(decoded_contents["next_check"]);
                        return output;
                    }
                }

            }
            catch (Exception ex)
            {
                this.Log("Exception while reading StatusFilename: " + ex.Message);
            }

            return last;
        }

        public void TouchLastOrchestratorCheckedFile(int number_of_days)
        {
            try
            {
                DateTime current_time = DateTime.Now;

                DateTime new_date = current_time.Date.AddDays(number_of_days);

                TimeSpan start = TimeSpan.FromHours(8);
                TimeSpan end = TimeSpan.FromHours(16);
                int maxSeconds = (int)((end - start).TotalSeconds);

                Random random = new Random();
                int randomSeconds = random.Next(maxSeconds);

                TimeSpan t = start.Add(TimeSpan.FromSeconds(randomSeconds));

                DateTime next_check = new_date + t;

                Dictionary<string, string> encoded_contents = new Dictionary<string, string>();
                encoded_contents["version"] = Application.ProductVersion;
                encoded_contents["next_check"] = next_check.ToString("s");

                string serialized_text = JsonConvert.SerializeObject(encoded_contents);

                File.WriteAllText(this.GetStatusFilename(), serialized_text);

                this.Log("Setting up orchestrator check in the future. Now: " + current_time.ToString("s") + " > Tomorrow: " + next_check.ToString("s"));
            }
            catch (Exception ex)
            {
                this.Log("Exception while writing StatusFilename: " + ex.Message);
            }
        }


        private void CleanLogFolder()
        {
            string[] aListOfFiles = Directory.GetFiles(this.GetLogFolder(), "*_InactiveAssistantMonitor.log");
            Array.Sort(aListOfFiles);
            Array.Reverse(aListOfFiles);

            int keep = 6; // days to keep + 1 for today

            foreach (var aFile in aListOfFiles)
            {
                if (aFile == this.GetLogFilename()) continue;

                if (keep > 0)
                {
                    keep--;
                }
                else
                {
                    int attempts = 0;
                    while (File.Exists(aFile) && attempts < 1000)
                    {
                        try
                        {
                            File.Delete(aFile);
                            this.Log("Deleted " + aFile);
                        }
                        catch (Exception ex)
                        {
                            System.Threading.Thread.Sleep(50 + 100 * attempts);
                            attempts++;
                        }
                    }
                }
            }
        }

        private string GetUiPathFolder()
        {
            string AppDataFolder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var ds = Path.DirectorySeparatorChar;

            return AppDataFolder + ds + "UiPath";
        }

        private string GetLogFolder()
        {
            var ds = Path.DirectorySeparatorChar;

            return this.GetUiPathFolder() + ds + "Logs";
        }

        private string GetLogFilename()
        {
            var ds = Path.DirectorySeparatorChar;
            string datetoday = DateTime.Now.ToString("yyyy-MM-dd");
            return this.GetLogFolder() + ds + datetoday + "_InactiveAssistantMonitor.log";
        }

        private string GetStatusFilename()
        {
            var ds = Path.DirectorySeparatorChar;
            return this.GetUiPathFolder() + ds + "InactiveAssistantMonitor.txt";
        }

        private string GetConfigFilename()
        {
            var ds = Path.DirectorySeparatorChar;
            return this.GetUiPathFolder() + ds + "InactiveAssistantMonitor.json";
        }

        public static FileManager Instance { get { return Nested.instance; } }

        private class Nested
        {
            // Explicit static constructor to tell C# compiler
            // not to mark type as beforefieldinit
            static Nested()
            {
            }

            internal static readonly FileManager instance = new FileManager();
        }
    }
}