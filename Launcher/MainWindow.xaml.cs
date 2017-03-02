using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Net;
using Newtonsoft.Json;

namespace Launcher
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            if (sender == buttonInstall)
                OnInstall();
            else if (sender == buttonUninstall)
                OnUninstall();
            else if (sender == buttonBackup)
                OnBackupSaves();
            else if (sender == buttonRestore)
                OnRestoreSaves();
            else if (sender == buttonInstallMod)
                OnInstallMod();
            else if (sender == buttonUninstallMod)
                OnUninstallMod();
        }

        public static string AppData
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); }
        }

        public static void Copy(string sourceDirectory, string targetDirectory)
        {
            DirectoryInfo diSource = new DirectoryInfo(sourceDirectory);
            DirectoryInfo diTarget = new DirectoryInfo(targetDirectory);

            CopyAll(diSource, diTarget);
        }

        public static void CopyAll(DirectoryInfo source, DirectoryInfo target)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                Console.WriteLine(@"Copying {0}\{1}", target.FullName, fi.Name);
                fi.CopyTo(System.IO.Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }

        public static string checkMD5(string filename)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    return BitConverter.ToString(md5.ComputeHash(stream)).Replace("-", "‌​").ToLower();
                }
            }
        }

        public class ModRelease
        {
            public string name;
            public string category;
            public string version;
            public string file;
        }

        public class Release
        {
            public string version;
            public string gameVersion;
            public string gameChecksum;
            public string modLoaderVersion;
            public string modLoaderChecksum;
            public string apiVersion;
            public string launcherVersion;

            public List<ModRelease> mods;
        }

        public class ReleaseList
        {
            public List<Release> releases;
        }

        private string exeChecksum()
        {
            return checkMD5(System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Secrets Of Grindea.exe"));
        }

        private void OnChecksum()
        {
            MessageBox.Show(exeChecksum());
        }

        private void OnInstall()
        {
            ReleaseList list = null;
            WebClient webClient = new WebClient();
            using (var stream = webClient.OpenRead("https://raw.githubusercontent.com/Nauja/SoGModLoader/master/Releases/list.json"))
            {
                using (var reader = new StreamReader(stream))
                {
                    var listContent = reader.ReadToEnd();
                    list = JsonConvert.DeserializeObject<ReleaseList>(listContent);
                }
            }
            if (list == null)
            {
                MessageBox.Show("Couldn't get list.json", "Install");
                return;
            }
            var checksum = exeChecksum();
            for (int i = 0; i < list.releases.Count; ++i)
            {
                var release = list.releases[i];
                if (release.gameChecksum == checksum)
                {
                    for (int j = i; j >= 0; --j)
                    {
                        if (list.releases[j].gameVersion != "")
                        {
                            var dst = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Secrets Of Grindea.exe");
                            var dstBackup = dst + "_Backup";
                            if (File.Exists(dstBackup))
                                File.Delete(dstBackup);
                            File.Copy(dst, dst + "_Backup");
                            webClient.DownloadFile("https://raw.githubusercontent.com/Nauja/SoGModLoader/master/Releases/" + list.releases[j].modLoaderVersion + "/ModLoader/Secrets Of Grindea.exe", dst);
                            MessageBox.Show(string.Format("Installed ModLoader version {0}", list.releases[j].modLoaderVersion), "Install");
                            return;
                        }
                    }
                }
            }
            MessageBox.Show("Error", "Install");
        }

        private void OnUninstall()
        {
            var dst = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Secrets Of Grindea.exe");
            var src = dst + "_Backup";
            if (!File.Exists(src))
            {
                MessageBox.Show("No exe backup", "Uninstall");
                return;
            }
            if (File.Exists(dst))
                File.Delete(dst);
            File.Copy(src, dst);
            File.Delete(src);
            MessageBox.Show("Done", "Uninstall");
        }

        private void OnBackupSaves()
        {
            if (MessageBox.Show("Are you sure you want to replace the old backup ?", "Backup Saves", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return;
            string src = System.IO.Path.Combine(AppData, "Secrets of Grindea");
            if (!Directory.Exists(src))
            {
                MessageBox.Show(string.Format("{0} doesn't exist", src), "Backup Saves");
                return;
            }
            string dst = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "BackupSaves");
            if (Directory.Exists(dst))
                Directory.Delete(dst, true);
            Copy(src, dst);
            MessageBox.Show("Done", "Backup Saves");
        }

        private void OnRestoreSaves()
        {
            if (MessageBox.Show("Are you sure you want to replace saves by the existing backup ?", "Restore Saves", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return;
            string src = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "BackupSaves");
            if (!Directory.Exists(src))
            {
                MessageBox.Show(string.Format("{0} doesn't exist", src), "Restore Saves");
                return;
            }
            string dst = System.IO.Path.Combine(AppData, "Secrets of Grindea");
            if (Directory.Exists(dst))
                Directory.Delete(dst, true);
            Copy(src, dst);
            MessageBox.Show("Done", "Restore Saves");
        }

        private void OnInstallMod()
        {
            ReleaseList list = null;
            WebClient webClient = new WebClient();
            using (var stream = webClient.OpenRead("https://raw.githubusercontent.com/Nauja/SoGModLoader/master/Releases/list.json"))
            {
                using (var reader = new StreamReader(stream))
                {
                    var listContent = reader.ReadToEnd();
                    list = JsonConvert.DeserializeObject<ReleaseList>(listContent);
                }
            }
            if (list == null)
            {
                MessageBox.Show("Couldn't get list.json", "Install");
                return;
            }
            var checksum = exeChecksum();
            for (int i = list.releases.Count - 1; i >= 0; --i)
            {
                var release = list.releases[i];
                if (release.modLoaderChecksum == checksum)
                {
                    for (int j = i; j >= 0; --j)
                    {
                        foreach (var mod in list.releases[j].mods)
                        {
                            if (mod.name == "Skin")
                            {
                                var dst = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Mods");
                                var dstBackup = dst + "_Backup";
                                if (File.Exists(dstBackup))
                                    File.Delete(dstBackup);
                                File.Copy(dst, dst + "_Backup");
                                webClient.DownloadFile("https://raw.githubusercontent.com/Nauja/SoGModLoader/master/Releases/" + mod.version + "/Mods/" + mod.category + "/" + mod.file, dst);
                                MessageBox.Show(string.Format("Installed Mod {0} version {1}", mod.name, mod.version), "Install");
                                return;
                            }
                        }
                    }
                }
            }
            MessageBox.Show("Error", "Install");
        }

        private void OnUninstallMod()
        {

        }
    }
}
