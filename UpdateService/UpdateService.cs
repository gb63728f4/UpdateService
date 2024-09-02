using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Web.Configuration;
using System.Text.RegularExpressions;

namespace UpdateService
{
    public abstract class UpdateService
    {
        private static readonly AutoResetEvent AutoResetEvent = new AutoResetEvent(false);

        private static readonly string
            DestinationFilePath = WebConfigurationManager.AppSettings["DestinationFilePath"]; //服務安裝位置

        private static readonly string ServiceFilePath =
            AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"\ServiceFiles"; //服務本地位置

        private static readonly string ExecuteMode = WebConfigurationManager.AppSettings["ExecuteMode"]; //服務安裝模式

        public static void Main()
        {
            try
            {
                //建立ServiceFiles資料夾
                if (Directory.Exists(ServiceFilePath) == false)
                {
                    Directory.CreateDirectory(ServiceFilePath);
                }

                ExecuteService();
            }
            catch (Exception ex)
            {
                Console.WriteLine("錯誤資訊：" + ex.Message);
            }

            Console.ReadKey();
        }

        /// <summary>
        /// 執行服務
        /// </summary>
        private static void ExecuteService()
        {
            const string pattern = @".+\\";
            var folderNames = GetFolderName(ServiceFilePath);

            if (Enum.TryParse(ExecuteMode, true, out ExecuteModeEnum modeEnum) == false)
            {
                Console.WriteLine("安裝模式設定有誤");
                return;
            }

            if (folderNames.Count > 0)
            {
                foreach (var folderName in folderNames)
                {
                    var serviceFilePath = ServiceFilePath + @"\" + folderName; //本地路徑
                    var destinationFilePath = DestinationFilePath + @"\" + folderName; //目的地路徑

                    var unInstalls =
                        Directory.GetFiles(serviceFilePath, "UnInstall*.bat",
                            SearchOption.AllDirectories); //尋找UnInstall開頭bat檔
                    var installs=
                        Directory.GetFiles(serviceFilePath, "Install*.bat", SearchOption.AllDirectories); //尋找Install開頭bat檔
                    if (unInstalls.Length == 1 && installs.Length == 1)
                    {
                        if (!Directory.Exists(destinationFilePath))
                        {
                            Console.WriteLine($"建立目的地服務資料夾 {folderName}");
                            Directory.CreateDirectory(destinationFilePath);
                        }

                        switch (modeEnum)
                        {
                            case ExecuteModeEnum.Complete:
                                UnInstall(unInstalls, pattern, destinationFilePath, folderName);
                                OverWriteDestinationFile(destinationFilePath, serviceFilePath, folderName);
                                Install(installs, pattern, destinationFilePath, folderName);
                                break;
                            case ExecuteModeEnum.UnInstall:
                                UnInstall(unInstalls, pattern, destinationFilePath, folderName);
                                break;
                            case ExecuteModeEnum.Install:
                                OverWriteDestinationFile(destinationFilePath, serviceFilePath, folderName);
                                Install(installs, pattern, destinationFilePath, folderName);
                                break;
                            default:
                                Console.WriteLine("錯誤資訊：請確認安裝模式是否設定正確");
                                break;
                        }
                    }
                    else Console.WriteLine($"錯誤資訊：服務 {folderName}資料夾未找到批次檔或是數量不對");
                }
            }
            else Console.WriteLine("錯誤資訊：ServiceFiles資料夾尚未放入任何服務");
        }

        /// <summary>
        /// 執行批次檔
        /// </summary>
        /// <param name="workingDirectory">目的地路徑</param>
        /// <param name="fileName">執行檔案名稱</param>
        private static void ExecuteProcess(string workingDirectory, string fileName)
        {
            using (var process = new Process())
            {
                process.EnableRaisingEvents = true;
                process.StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    WorkingDirectory = workingDirectory,
                    FileName = fileName,
                    Verb = "runas"
                };
                process.Start();
                process.Exited += (sender, e) =>
                {
                    Console.WriteLine("執行完成");
                    AutoResetEvent.Set();
                };
            }

            AutoResetEvent.WaitOne();
        }

        /// <summary>
        /// 執行安裝流程
        /// </summary>
        /// <param name="installs"></param>
        /// <param name="pattern"></param>
        /// <param name="destinationFilePath"></param>
        /// <param name="folderName"></param>
        private static void Install(string[] installs, string pattern, string destinationFilePath, string folderName)
        {
            var match = Regex.Match(installs[0], pattern);
            var batFile = installs[0].Replace(match.Value, "");
            Console.WriteLine($"開始執行 安裝{folderName}服務");
            ExecuteProcess(destinationFilePath, batFile);
        }

        /// <summary>
        /// 執行解除安裝流程
        /// </summary>
        /// <param name="unInstalls"></param>
        /// <param name="pattern"></param>
        /// <param name="destinationFilePath"></param>
        /// <param name="folderName"></param>
        private static void UnInstall(string[] unInstalls, string pattern, string destinationFilePath, string folderName)
        {
            var match = Regex.Match(unInstalls[0], pattern);
            var batFile = unInstalls[0].Replace(match.Value, "");
            Console.WriteLine($"開始執行 解除安裝{folderName}服務");
            ExecuteProcess(destinationFilePath, batFile);
        }

        /// <summary>
        /// 覆寫目的地資料夾檔案
        /// </summary>
        /// <param name="destinationFilePath">目的地路徑</param>
        /// <param name="serviceFilePath">本地路徑</param>
        /// <param name="folderName">資料夾名稱</param>
        private static void OverWriteDestinationFile(string destinationFilePath, string serviceFilePath, string folderName)
        {
            try
            {
                Console.WriteLine($"開始覆寫 {folderName}資料夾");
                var listFileFiles = Directory.GetFiles(serviceFilePath);
                foreach (var file in listFileFiles)
                {
                    var fileName = Path.GetFileName(file);
                    var destFile = Path.Combine(destinationFilePath, fileName);
                    if (File.Exists(destFile)) File.Delete(destFile);
                    File.Copy(file, destFile);
                }

                Console.WriteLine("覆寫完成");
            }
            catch (UnauthorizedAccessException)
            {
                AutoResetEvent.WaitOne(); //暫停執行緒
                Console.WriteLine($"錯誤資訊：覆寫 {folderName}資料夾權限不足");
                throw;
            }
            catch (DirectoryNotFoundException e)
            {
                AutoResetEvent.WaitOne(); //暫停執行緒
                Console.WriteLine($"錯誤資訊：{e.Message}檔案遺失");
                throw;
            }
        }

        /// <summary>
        /// 取得指定路徑下所有資料夾名稱
        /// </summary>
        private static List<string> GetFolderName(string filePath)
        {
            var fileNames = Directory.GetDirectories(filePath);
            var files = new List<string>();
            if (fileNames.Length <= 0)
            {
                return files;
            }

            files.AddRange(fileNames.Select(Path.GetFileNameWithoutExtension));

            return files;
        }
    }
}