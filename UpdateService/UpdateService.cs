using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace UpdateService
{
    /// <summary>
    /// 服務更新業務邏輯
    /// </summary>
    public class UpdateServiceLogic
    {
        private readonly string _destinationFilePath;
        private readonly string _serviceFilePath;
        private readonly ExecuteModeEnum _executeMode;
        private readonly Action<string> _log;

        public UpdateServiceLogic(string destinationFilePath, ExecuteModeEnum executeMode, Action<string> log)
        {
            _destinationFilePath = destinationFilePath;
            _executeMode = executeMode;
            _log = log;
            _serviceFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServiceFiles");
        }

        public string ServiceFilePath => _serviceFilePath;

        /// <summary>
        /// 執行服務更新流程
        /// </summary>
        public async Task ExecuteAsync()
        {
            if (!Directory.Exists(_serviceFilePath))
                Directory.CreateDirectory(_serviceFilePath);

            await ExecuteServiceAsync();
        }

        /// <summary>
        /// 執行服務
        /// </summary>
        private async Task ExecuteServiceAsync()
        {
            const string pattern = @".+\\";
            var folderNames = GetFolderName(_serviceFilePath);

            if (folderNames.Count == 0)
            {
                _log("錯誤資訊：ServiceFiles資料夾尚未放入任何服務");
                return;
            }

            foreach (var folderName in folderNames)
            {
                var serviceFilePath = Path.Combine(_serviceFilePath, folderName);
                var destinationFilePath = Path.Combine(_destinationFilePath, folderName);

                var unInstalls = Directory.GetFiles(serviceFilePath, "UnInstall*.bat", SearchOption.AllDirectories); //尋找UnInstall開頭bat檔
                var installs = Directory.GetFiles(serviceFilePath, "Install*.bat", SearchOption.AllDirectories);     //尋找Install開頭bat檔

                if (unInstalls.Length == 1 && installs.Length == 1)
                {
                    if (!Directory.Exists(destinationFilePath))
                    {
                        _log($"建立目的地服務資料夾 {folderName}");
                        Directory.CreateDirectory(destinationFilePath);
                    }

                    switch (_executeMode)
                    {
                        case ExecuteModeEnum.Complete:
                            await UnInstallAsync(unInstalls, pattern, destinationFilePath, folderName);
                            OverWriteDestinationFile(destinationFilePath, serviceFilePath, folderName);
                            await InstallAsync(installs, pattern, destinationFilePath, folderName);
                            break;
                        case ExecuteModeEnum.UnInstall:
                            await UnInstallAsync(unInstalls, pattern, destinationFilePath, folderName);
                            break;
                        case ExecuteModeEnum.Install:
                            OverWriteDestinationFile(destinationFilePath, serviceFilePath, folderName);
                            await InstallAsync(installs, pattern, destinationFilePath, folderName);
                            break;
                        default:
                            _log("錯誤資訊：請確認安裝模式是否設定正確");
                            break;
                    }
                }
                else
                {
                    _log($"錯誤資訊：服務 {folderName} 資料夾未找到批次檔或是數量不對");
                }
            }
        }

        /// <summary>
        /// 非同步執行批次檔
        /// </summary>
        /// <param name="workingDirectory">目的地路徑</param>
        /// <param name="fileName">執行檔案名稱</param>
        private async Task ExecuteProcessAsync(string workingDirectory, string fileName)
        {
            var tcs = new TaskCompletionSource<bool>();

            using var process = new Process();
            process.EnableRaisingEvents = true;
            process.StartInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = workingDirectory,
                FileName = fileName,
                Verb = "runas"
            };
            process.Exited += (sender, e) =>
            {
                _log("執行完成");
                tcs.TrySetResult(true);
            };
            process.Start();

            await tcs.Task;
        }

        /// <summary>
        /// 執行安裝流程
        /// </summary>
        private async Task InstallAsync(string[] installs, string pattern, string destinationFilePath, string folderName)
        {
            var match = Regex.Match(installs[0], pattern);
            var batFile = installs[0].Replace(match.Value, "");
            _log($"開始執行 安裝 {folderName} 服務");
            await ExecuteProcessAsync(destinationFilePath, batFile);
        }

        /// <summary>
        /// 執行解除安裝流程
        /// </summary>
        private async Task UnInstallAsync(string[] unInstalls, string pattern, string destinationFilePath, string folderName)
        {
            var match = Regex.Match(unInstalls[0], pattern);
            var batFile = unInstalls[0].Replace(match.Value, "");
            _log($"開始執行 解除安裝 {folderName} 服務");
            await ExecuteProcessAsync(destinationFilePath, batFile);
        }

        /// <summary>
        /// 覆寫目的地資料夾檔案
        /// </summary>
        /// <param name="destinationFilePath">目的地路徑</param>
        /// <param name="serviceFilePath">本地路徑</param>
        /// <param name="folderName">資料夾名稱</param>
        private void OverWriteDestinationFile(string destinationFilePath, string serviceFilePath, string folderName)
        {
            try
            {
                _log($"開始覆寫 {folderName} 資料夾");
                var files = Directory.GetFiles(serviceFilePath);
                foreach (var file in files)
                {
                    var fileName = Path.GetFileName(file);
                    var destFile = Path.Combine(destinationFilePath, fileName);
                    if (File.Exists(destFile)) File.Delete(destFile);
                    File.Copy(file, destFile);
                }

                _log("覆寫完成");
            }
            catch (UnauthorizedAccessException)
            {
                _log($"錯誤資訊：覆寫 {folderName} 資料夾權限不足");
            }
            catch (DirectoryNotFoundException e)
            {
                _log($"錯誤資訊：{e.Message} 檔案遺失");
            }
        }

        /// <summary>
        /// 取得指定路徑下所有資料夾名稱
        /// </summary>
        private static List<string> GetFolderName(string filePath)
        {
            var directories = Directory.GetDirectories(filePath);
            var names = new List<string>();
            if (directories.Length == 0)
                return names;

            names.AddRange(directories.Select(Path.GetFileNameWithoutExtension).Where(n => n != null)!);
            return names;
        }
    }
}