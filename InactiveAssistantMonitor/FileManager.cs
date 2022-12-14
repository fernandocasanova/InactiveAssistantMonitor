using System;
using System.IO;

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

            this.Log("** Start of logging **");
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

        private void CleanLogFolder()
        {
            string[] aListOfFiles = Directory.GetFiles(this.GetLogFolder(), "*_InactiveAssistantMonitor.log");
            Array.Sort(aListOfFiles);
            Array.Reverse(aListOfFiles);

            int keep = 2; // keep + 1 for today

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