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
using SoG.ModLoader.SaveConverter;
using Microsoft.Win32;

namespace Launcher
{
    /// <summary>
    /// Logique d'interaction pour MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ReleaseList releaseList;

        public MainWindow()
        {
            InitializeComponent();
            // Download list.json from github.
            WebClient webClient = new WebClient();
            using (var stream = webClient.OpenRead("https://raw.githubusercontent.com/Nauja/SoGModLoader/master/Releases/list.json"))
            {
                using (var reader = new StreamReader(stream))
                {
                    var listContent = reader.ReadToEnd();
                    releaseList = JsonConvert.DeserializeObject<ReleaseList>(listContent);
                }
            }
            if (releaseList == null)
            {
                MessageBox.Show("Couldn't get list.json", "Launcher");
            }
            // Refresh displayed exe version.
            RefreshExeVersion();                            
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
            else if (sender == buttonLaunch)
                OnLaunch();
            else if (sender == buttonSavesRefresh)
                OnSavesRefresh();
            else if (sender == buttonSavesConvert)
                OnSavesConvert();
            else if (sender == buttonSavesOpenFolder)
                OnSavesOpenFolder();
        }

        public static string AppData
        {
            get { return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData); }
        }

        public static string SavesDirectory
        {
            get { return System.IO.Path.Combine(AppData, "Secrets of Grindea"); }
        }

        public static string CharactersSavesDirectory
        {
            get { return System.IO.Path.Combine(SavesDirectory, "Characters"); }
        }

        public static string GameDirectory
        {
            get
            {
                RegistryKey regKey = Registry.CurrentUser;
                regKey = regKey.OpenSubKey(@"Software\Valve\Steam");

                if (regKey != null)
                {
                    string steamPath = regKey.GetValue("SteamPath").ToString();
                    return System.IO.Path.Combine(steamPath, "steamapps/common/SecretsOfGrindea/");
                }
                return Directory.GetCurrentDirectory();
            }
        }

        public static string ExePath
        {
            get { return System.IO.Path.Combine(GameDirectory, "Secrets Of Grindea.exe"); }
        }

        public static string BackupSavesDirectory
        {
            get { return System.IO.Path.Combine(GameDirectory, "BackupSaves"); }
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

            public string GetExeVersion(string checksum)
            {
                var version = GetGameVersion(checksum);
                if (version != null)
                    return version;
                return GetModLoaderVersion(checksum);
            }

            public string GetGameVersion(string checksum)
            {
                foreach (var release in releases)
                {
                    if (release.gameChecksum == checksum)
                        return "v" + release.gameVersion;
                }
                return null;
            }

            public string GetModLoaderVersion(string checksum)
            {
                foreach (var release in releases)
                {
                    if (release.modLoaderChecksum == checksum)
                        return "m" + release.modLoaderVersion;
                }
                return null;
            }
        }

        private string exeChecksum()
        {
            return checkMD5(ExePath);
        }

        private void OnChecksum()
        {
            MessageBox.Show(exeChecksum());
        }

        private void RefreshExeVersion()
        {
            var version = releaseList?.GetExeVersion(exeChecksum());
            labelExeVersion.Content = "Secrets of Grindea.exe version: " + (version == null ? "unknown" : version + (version[0] == 'v' ? " (Vanilla)": " (ModLoader)"));
        }

        private void OnInstall()
        {
            var checksum = exeChecksum();
            for (int i = 0; i < releaseList.releases.Count; ++i)
            {
                var release = releaseList.releases[i];
                if (release.gameChecksum == checksum)
                {
                    for (int j = i; j >= 0; --j)
                    {
                        if (releaseList.releases[j].gameVersion != "")
                        {
                            var dst = ExePath;
                            var dstBackup = dst + "_Backup";
                            if (File.Exists(dstBackup))
                                File.Delete(dstBackup);
                            File.Copy(dst, dst + "_Backup");
                            WebClient webClient = new WebClient();
                            webClient.DownloadFile("https://raw.githubusercontent.com/Nauja/SoGModLoader/master/Releases/" + releaseList.releases[j].modLoaderVersion + "/ModLoader/Secrets Of Grindea.exe", dst);
                            MessageBox.Show(string.Format("Installed ModLoader version {0}", releaseList.releases[j].modLoaderVersion), "Install");
                            RefreshExeVersion();
                            return;
                        }
                    }
                }
            }
            MessageBox.Show("Error", "Install");
        }

        private void OnUninstall()
        {
            var dst = ExePath;
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
            RefreshExeVersion();
        }

        private void OnBackupSaves()
        {
            if (MessageBox.Show("Are you sure you want to replace the old backup ?", "Backup Saves", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return;
            string src = SavesDirectory;
            if (!Directory.Exists(src))
            {
                MessageBox.Show(string.Format("{0} doesn't exist", src), "Backup Saves");
                return;
            }
            string dst = BackupSavesDirectory;
            if (Directory.Exists(dst))
                Directory.Delete(dst, true);
            Copy(src, dst);
            MessageBox.Show("Done", "Backup Saves");
        }

        private void OnRestoreSaves()
        {
            if (MessageBox.Show("Are you sure you want to replace saves by the existing backup ?", "Restore Saves", MessageBoxButton.OKCancel) != MessageBoxResult.OK)
                return;
            string src = BackupSavesDirectory;
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
            var checksum = exeChecksum();
            for (int i = releaseList.releases.Count - 1; i >= 0; --i)
            {
                var release = releaseList.releases[i];
                if (release.modLoaderChecksum == checksum)
                {
                    for (int j = i; j >= 0; --j)
                    {
                        foreach (var mod in releaseList.releases[j].mods)
                        {
                            if (mod.name == "Skin")
                            {
                                var dst = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "Mods/" + mod.file);
                                if (File.Exists(dst))
                                    File.Delete(dst);
                                WebClient webClient = new WebClient();
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
        
        private void OnLaunch()
        {
            MessageBox.Show("Sorry but I don't work for now, try to start it manually :(", "Launch");
            return;
            string exePath = ExePath;
            if (!File.Exists(exePath))
                MessageBox.Show("Couldn't find executable", "Launch");
            else
            {
                System.Diagnostics.ProcessStartInfo psi = new System.Diagnostics.ProcessStartInfo();
                psi.FileName = @"cmd";
                psi.Arguments = "/C start \"Secrets Of Grindea.exe\"";
                psi.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                System.Diagnostics.Process.Start(psi);
            }
        }

        public class SaveItem
        {
            public ICharacter Character
            {
                get;
                set;
            }

            public string Version
            {
                get;
                set;
            }

            public string Name
            {
                get;
                set;
            }

            public int Level
            {
                get;
                set;
            }

            public string AbsolutePath
            {
                get;
                set;
            }

            public string Path
            {
                get;
                set;
            }

            public SaveItem(ICharacter character, string path, string prefix = "")
            {
                Character = character;
                Version = character.Version;
                Name = character.Name;
                Level = character.Level;
                AbsolutePath = path;
                Path = prefix + System.IO.Path.GetFileName(path);
            }
        }

        private void RefreshSavesVersions()
        {
            listSavesVersions.Items.Clear();
            foreach (var version in Save.Versions)
                listSavesVersions.Items.Add(version);
            listSavesVersions.SelectedIndex = listSavesVersions.Items.Count - 1;
        }

        private void OnSavesRefresh()
        {
            listSaves.Items.Clear();
            var path = CharactersSavesDirectory;
            if (Directory.Exists(path))
            {
                foreach (var file in Directory.GetFiles(path))
                {
                    if (file.EndsWith(".cha"))
                    {
                        var character = CharacterLoader.Load(file);
                        listSaves.Items.Add(new SaveItem(character, file));
                    }
                }
            }
            path = System.IO.Path.Combine(BackupSavesDirectory, "Characters");
            if (Directory.Exists(path))
            {
                foreach (var file in Directory.GetFiles(path))
                {
                    if (file.EndsWith(".cha"))
                    {
                        var character = CharacterLoader.Load(file);
                        listSaves.Items.Add(new SaveItem(character, file, "Backup "));
                    }
                }
            }
        }

        private void OnSavesConvert()
        {
            var items = listSaves.SelectedItems;
            var version = (string)listSavesVersions.SelectedItem;
            if (items == null || items.Count == 0)
            {
                MessageBox.Show("Select a save to convert in the list above", "Convert Save");
            }
            else if (MessageBox.Show("Are you sure you want to convert selected saves to version " + version + " ?", "Convert Save", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                foreach (var item in items)
                {
                    var save = (SaveItem)item;
                    CharacterLoader.Convert(save.AbsolutePath, save.AbsolutePath, version);
                }
                OnSavesRefresh();
            }
        }

        private void OnSavesOpenFolder()
        {
            var items = listSaves.SelectedItems;
            if (items != null && items.Count > 0)
                System.Diagnostics.Process.Start(System.IO.Path.GetDirectoryName(((SaveItem)items[0]).AbsolutePath));
        }

        private void tabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count > 0 && e.AddedItems[0] == tabSaves)
            {
                RefreshSavesVersions();
                OnSavesRefresh();
            }
        }
    }
}
