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
using System.IO.Compression;

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
            RefreshLatestVersion();
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
            else if (sender == buttonLaunch)
                OnLaunch();
            else if (sender == buttonModsInstall)
                OnModsInstall();
            else if (sender == buttonModsOpenFolder)
                OnModsOpenFolder();
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

        public static string ModsDirectory
        {
            get { return System.IO.Path.Combine(GameDirectory, "Mods"); }
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
            if (!File.Exists(filename))
                return null;
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
            public string modLoaderFile;
            public string apiVersion;
            public string launcherVersion;

            public List<ModRelease> mods;
        }

        public class ReleaseList
        {
            public List<Release> releases;

            public string GetLatestModLoaderVersion()
            {
                for (var i = releases.Count - 1; i >= 0; --i)
                {
                    if (releases[i].modLoaderVersion != null)
                        return "m" + releases[i].modLoaderVersion;
                }
                return null;
            }

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

            public Dictionary<string, List<ModRelease>> GetModsReleases()
            {
                var dic = new Dictionary<string, List<ModRelease>>();
                foreach (var release in releases)
                {
                    if(release.mods != null)
                    {
                        foreach(var mod in release.mods)
                        {
                            if (!dic.ContainsKey(mod.name))
                                dic[mod.name] = new List<ModRelease>();
                            dic[mod.name].Add(mod);
                        }
                    }
                }
                return dic;
            }
        }

        private string ExeChecksum
        {
            get { return checkMD5(ExePath); }
        }

        private string ExeVersion
        {
            get { return releaseList?.GetExeVersion(ExeChecksum); }
        }

        private void OnChecksum()
        {
            MessageBox.Show(ExeChecksum);
        }

        private void RefreshLatestVersion()
        {
            var version = releaseList?.GetLatestModLoaderVersion();
            labelLatestVersion.Content = "Latest ModLoader version: " + (version == null ? "unknown" : version);
        }

        private void RefreshExeVersion()
        {
            var checksum = ExeChecksum;
            var version = ExeVersion;
            labelExeVersion.Content = "Secrets of Grindea.exe version: " + (version == null ? "unknown" : version + (version[0] == 'v' ? " (Vanilla)": " (ModLoader)"));
            labelExeChecksum.Content = "Checksum: " + checksum;
        }

        public static void ImprovedExtractToDirectory(string sourceArchiveFileName,
                                              string destinationDirectoryName)
        {
            //Opens the zip file up to be read
            using (var archive = ZipFile.OpenRead(sourceArchiveFileName))
            {
                //Loops through each file in the zip file
                foreach (ZipArchiveEntry file in archive.Entries)
                {
                    ImprovedExtractToFile(file, destinationDirectoryName);
                }
            }
        }

        public static void ImprovedExtractToFile(ZipArchiveEntry file,
                                            string destinationPath)
        {
            //Gets the complete path for the destination file, including any
            //relative paths that were in the zip file
            string destinationFileName = System.IO.Path.Combine(destinationPath, file.FullName);

            //Gets just the new path, minus the file name so we can create the
            //directory if it does not exist
            string destinationFilePath = System.IO.Path.GetDirectoryName(destinationFileName);

            //Creates the directory (if it doesn't exist) for the new path
            Directory.CreateDirectory(destinationFilePath);

            //Determines what to do with the file based upon the
            //method of overwriting chosen
            //Just put the file in and overwrite anything that is found
            if (Directory.Exists(destinationFileName))
                return;
            file.ExtractToFile(destinationFileName, true);
        }

        private void Unzip(string path)
        {
            if (!path.EndsWith(".zip"))
                return;
            ImprovedExtractToDirectory(path, System.IO.Path.GetDirectoryName(path));
            File.Delete(path);
        }

        private void OnInstall()
        {
            var checksum = ExeChecksum;
            for (int i = 0; i < releaseList.releases.Count; ++i)
            {
                var release = releaseList.releases[i];
                if (release.gameChecksum == checksum)
                {
                    for (int j = i; j >= 0; --j)
                    {
                        if (releaseList.releases[j].gameVersion != "")
                        {
                            var r = releaseList.releases[j];
                            var exeSrc = ExePath;
                            var exeDst = exeSrc + "_Backup";
                            if (File.Exists(exeDst))
                                File.Delete(exeDst);
                            File.Copy(exeSrc, exeDst);
                            WebClient webClient = new WebClient();
                            var dst = System.IO.Path.Combine(GameDirectory, r.modLoaderFile);
                            if (File.Exists(dst))
                                File.Delete(dst);
                            webClient.DownloadFile("https://raw.githubusercontent.com/Nauja/SoGModLoader/master/Releases/" + r.modLoaderVersion + "/ModLoader/" + r.modLoaderFile, dst);
                            Unzip(dst);
                            MessageBox.Show(string.Format("Installed ModLoader version m{0}", r.modLoaderVersion), "Install");
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
            var checksum = ExeChecksum;
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
            string exePath = ExePath;
            if (!File.Exists(exePath))
                MessageBox.Show("Couldn't find executable", "Launch");
            else
                System.Diagnostics.Process.Start(exePath);
        }

        public class ModReleaseItem
        {
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

            public List<ModRelease> Releases
            {
                get;
                set;
            }

            public ModReleaseItem(List<ModRelease> releases)
            {
                Version = "m" + releases[releases.Count - 1].version;
                Name = releases[0].name;
                Releases = releases;
            }
        }

        private void RefreshMods()
        {
            listMods.Items.Clear();
            if (releaseList == null)
                return;
            foreach (var releases in releaseList.GetModsReleases())
                listMods.Items.Add(new ModReleaseItem(releases.Value));
        }

        private void OnModsInstall()
        {
            var items = listMods.SelectedItems;
            if (items == null || items.Count == 0)
            {
                MessageBox.Show("Select a mod to install in the list above", "Install");
            }
            else
            {
                var exeVersion = ExeVersion;
                foreach (var item in items)
                {
                    var mod = (ModReleaseItem)item;
                    var name = mod.Releases[0].name;
                    var installed = false;
                    for (var i = releaseList.releases.Count - 1; i >= 0 && !installed; --i)
                    {
                        var release = releaseList.releases[i];
                        if ("m" + release.modLoaderVersion == exeVersion)
                        {
                            if (release.mods == null)
                                continue;
                            foreach (var modRelease in release.mods)
                            {
                                if (modRelease.name == name)
                                {
                                    if (!Directory.Exists(ModsDirectory))
                                        Directory.CreateDirectory(ModsDirectory);
                                    var dst = System.IO.Path.Combine(ModsDirectory, modRelease.file);
                                    WebClient webClient = new WebClient();
                                    var url = "https://raw.githubusercontent.com/Nauja/SoGModLoader/master/Releases/" + modRelease.version + "/Mods/" + modRelease.category + "/" + modRelease.file;
                                    webClient.DownloadFile(url, dst);
                                    Unzip(dst);
                                    installed = true;
                                    MessageBox.Show("Installed mod " + name + " version m" + modRelease.version, "Install");
                                    break;
                                }
                            }
                        }
                    }
                    if (!installed)
                    {
                        MessageBox.Show("Could not install mod " + name + ", no release for your version", "Install");
                    }
                }
                OnSavesRefresh();
            }
        }

        private void OnModsOpenFolder()
        {
            System.Diagnostics.Process.Start(ModsDirectory);
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
            if (e.AddedItems.Count > 0 && e.AddedItems[0] == tabMods)
            {
                RefreshMods();
            }
            else if (e.AddedItems.Count > 0 && e.AddedItems[0] == tabSaves)
            {
                RefreshSavesVersions();
                OnSavesRefresh();
            }
        }
    }
}
