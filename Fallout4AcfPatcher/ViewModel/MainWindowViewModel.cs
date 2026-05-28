using Fallout4AcfPatcher.Commands;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;

namespace Fallout4AcfPatcher.ViewModel
{
    class MainWindowViewModel: INotifyPropertyChanged
    {
        public RelayCommand FileBrowserCommand { get; set; }
        
        public RelayCommand PatchAcfCommand { get; set; }

        private string? _filepath;
        public string? FilePath { 
            get 
            { 
                return _filepath; 
            } 
            set 
            {
                _filepath = value;

                OnPropertyChanged();
            } 
        }

        public readonly Dictionary<string, string> gameMetadataDict = new Dictionary<string, string>
        {
            { "StateFlags", "4" },
            { "LastUpdated", "1575538257" },
            { "StagingSize", "0" },
            { "buildid", "14349213" },
            { "LastOwner", "76561197994992501" },
            { "UpdateResult", "0" },
            { "BytesToDownload", "0" },
            { "BytesDownloaded", "0" },
            { "BytesToStage", "0" },
            { "BytesStaged", "0" },
            { "TargetBuildID", "0" },
            { "AutoUpdateBehavior", "1" },
            { "AllowOtherDownloadsWhileRunning", "0" },
            { "ScheduledAutoUpdate", "0" },
        };

        // Steamdb cannot be scraped due to anti-bot measures
        // Steam api has a risk of api key leakage unless api calls are made on a secure server
        // Steam api for game depot data also requires steam game publisher api key (not easily obtainable)
        // This will be manually updated instead if necessary, not that tedious but requires a new build
        public readonly Dictionary<int, string> gameDepotDict = new Dictionary<int, string>
        {
            { 377161, "5983086794954940044" }, // Fallout 4 content_a
            { 377162, "387388833281246371" }, // Fallout 4.exe
            { 377163, "8363807899725426636" }, // Fallout 4 content_b
            { 377164, "8492427313392140315" }, // Fallout 4 english
            { 435870, "1213339795579796878" }, // Fallout 4 - Automatron
            { 435871, "4060235024162383907" },  // Fallout 4 - Automatron english
            { 435880, "7797822138743384972" }, // Fallout 4 - Wasteland Workshop
            { 435881, "1207717296920736193" }, // Fallout 4 - Far Harbor
            { 435882, "8482181819175811242" }, // Fallout 4 - Far Harbor english
            { 480630, "5527412439359349504" }, // Fallout 4 - Contraptions Workshop
            { 480631, "6588493486198824788" }, // Fallout 4 - Vault-Tec Workshop
            { 393885, "5000262035721758737" }, // Fallout 4 - Vault-Tec Workshop english
            { 490650, "4873048792354485093" }, // Fallout 4 - Nuka-World
            { 393895, "7677765994120765493" }, // Fallout 4 - Nuka-World english
        };

        public readonly Dictionary<string, string> creationKitMetadataDict = new Dictionary<string, string>
        {
            { "StateFlags", "4" },
            { "LastUpdated", "1575538257" },
            { "StagingSize", "0" },
            { "buildid", "8578741" },
            { "LastOwner", "76561197994992501" },
            { "UpdateResult", "0" },
            { "BytesToDownload", "0" },
            { "BytesDownloaded", "0" },
            { "BytesToStage", "0" },
            { "BytesStaged", "0" },
            { "TargetBuildID", "0" },
            { "AutoUpdateBehavior", "1" },
            { "AllowOtherDownloadsWhileRunning", "0" },
            { "ScheduledAutoUpdate", "0" },
        };

        // This will be manually updated instead if necessary, not that tedious but requires a new build
        public readonly Dictionary<int, string> creationKitDepotDict = new Dictionary<int, string>
        {
            { 1946161, "5210064669056346933" },
            { 1946162, "8888411475612042965" },
        };

        public MainWindowViewModel()
        {
            FileBrowserCommand = new RelayCommand(ExecuteFileBrowser, CanExecuteFileBrowser);
            PatchAcfCommand = new RelayCommand(ExecutePatchAcfFile, CanExecutePatchAcfFile);
        }

        public void ExecuteFileBrowser(Object obj)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Acf file (*.acf)|*.acf"
            };

            bool? result = openFileDialog.ShowDialog();

            if (result == true)
            {
                FilePath = openFileDialog.FileName;
                
            }
        }

        public bool CanExecuteFileBrowser(Object obj)
        {
            return true;
        }

        public void ExecutePatchAcfFile(Object obj)
        {
            if (FilePath == null)
            {
                MessageBox.Show(Application.Current.MainWindow, "Please select an ACF file first", "", MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            if (!File.Exists(FilePath))
            {
                MessageBox.Show(Application.Current.MainWindow, "Given file path doesn't exist", "", MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            string fileName = Path.GetFileName(FilePath);

            if (fileName != "appmanifest_377160.acf" && fileName != "appmanifest_1946160.acf")
            {
                MessageBox.Show(Application.Current.MainWindow, "Given file is not a Fallout 4/Creation Kit manifest file", "", MessageBoxButton.OK, MessageBoxImage.Warning);

                return;
            }

            try
            {
                // Copy the file, overwriting if already exists
                File.Copy(FilePath, FilePath + ".bak_" + DateTimeOffset.Now.ToUnixTimeSeconds(), true);
                MessageBox.Show(
                    Application.Current.MainWindow,
                    "A backup of your ACF file has been created in the same directory. " + Environment.NewLine +
                    "To restore backup file, please remove the file extention suffix (.bak_<timestamp>).",
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                 );
            }
            catch (IOException ex)
            {
                MessageBox.Show(
                    Application.Current.MainWindow, 
                    $"An error occurred during file copy: {ex.Message}",
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }

            // Read ACF file content
            String acfContent = File.ReadAllText(FilePath);

            if (fileName == "appmanifest_377160.acf")
            {
                acfContent = patchAcfFile(acfContent, gameMetadataDict, gameDepotDict);
            } else
            {
                acfContent = patchAcfFile(acfContent, creationKitMetadataDict, creationKitDepotDict);
            }

            // Write updated content to ACF file
            try
            {
                new FileInfo(FilePath).IsReadOnly = false;
                File.WriteAllText(FilePath, acfContent);
                new FileInfo(FilePath).IsReadOnly = true;
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show(
                    Application.Current.MainWindow,
                    $"An error occured while updating acf file contents: {ex.Message}",
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }

            MessageBox.Show(
                Application.Current.MainWindow,
                "ACF File Successfully Patched! Please Restart Steam",
                "",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        public bool CanExecutePatchAcfFile(Object obj)
        {
            return true;
        }

        public string patchAcfFile(string acfContent, Dictionary<string, string> metaDataDict, Dictionary<int, string> depotDict)
        {
            // Update ACF file content metadata
            foreach (KeyValuePair<string, string> entry in metaDataDict)
            {
                acfContent = Regex.Replace(
                    acfContent,
                    $"\"{entry.Key}\"\\s*\"(\\d+)\"",
                    m => m.Value.Replace(m.Groups[1].Value, entry.Value)
                );
            }

            // Update ACF file content depot data
            foreach (KeyValuePair<int, string> entry in depotDict)
            {
                acfContent = Regex.Replace(
                    acfContent,
                   $"\"{entry.Key}\"\\s*{{[\\s\\S]*?\"manifest\"\\s*\"(\\d+)\"",
                   m => m.Value.Replace(m.Groups[1].Value, entry.Value)
                 );
            }

            return acfContent;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string? property = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
