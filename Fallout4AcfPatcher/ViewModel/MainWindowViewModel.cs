using Fallout4AcfPatcher.Commands;
using Fallout4AcfPatcher.Services;
using Microsoft.Win32;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Windows;
using System.Collections.Generic;
using System.Threading.Tasks;

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
        // Steam api for game depot data also requires steam game publisher api key (not easily obtainable).
        // Manually updated through github gist.
        public Dictionary<int, string> gameDepotDict = [];
        
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
        
        // Manually updated through github gist.
        public Dictionary<int, string> creationKitDepotDict = [];
        
        public bool IsLoading { get; set; }
        private readonly DepotDataService _depotDataService = new DepotDataService();

        public MainWindowViewModel()
        {
            FileBrowserCommand = new RelayCommand(ExecuteFileBrowser, CanExecuteFileBrowser);
            PatchAcfCommand = new RelayCommand(ExecutePatchAcfFile, CanExecutePatchAcfFile);
        }

        public async Task InitializeAsync()
        {
            IsLoading = true;
            try
            {
                var gameDepots = await _depotDataService.FetchDepotDataAsync("https://gist.githubusercontent.com/FN-FAL113/77b11a4ccb1fe6f900c8a768b2f97152/raw/fallout4_acf_patcher_game_depot_metadata");
                if (gameDepots.Count > 0) gameDepotDict = gameDepots;
            
                var ckDepots = await _depotDataService.FetchDepotDataAsync("https://gist.githubusercontent.com/FN-FAL113/96a84dba2c1f19f23040a8c0278fe1ed/raw/fallout4_acf_patcher_ck_depot_metadata");
                if (ckDepots.Count > 0) creationKitDepotDict = ckDepots;
            }
            catch
            {
                MessageBox.Show(
                    Application.Current.MainWindow,
                    $"Failed to fetch game or ck depot data. Please try again or report this issue on GitHub.",
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                IsLoading = false;
            }
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
            if(IsLoading)
            {
                MessageBox.Show(
                   Application.Current.MainWindow,
                   "Fetching ACF metadata. Please try again...",
                   "",
                   MessageBoxButton.OK,
                   MessageBoxImage.Information
                );

                return;
            }

            if(gameDepotDict.Count == 0 || creationKitDepotDict.Count == 0)
            {
                MessageBox.Show(
                    Application.Current.MainWindow,
                    $"An error has occured: game or ck depot data is empty",
                    "",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );

                return;
            }

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
                    $"An error has occurred during file copy: {ex.Message}",
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
