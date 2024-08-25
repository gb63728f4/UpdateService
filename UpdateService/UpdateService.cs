using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Web.Configuration;
using System.Text.RegularExpressions;

namespace UpdateService
{
    /// <summary>
    /// 安裝模式說明:Complete:解除再重新安裝，UnInstall:僅解除安裝，Install:僅安裝
    /// </summary>
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
                if (!Directory.Exists(ServiceFilePath)) Directory.CreateDirectory(ServiceFilePath);
                ExecuteProcess();
            }
            catch (Exception e)
            {
                Console.WriteLine("錯誤資訊：" + e.Message);
            }

            Console.ReadKey();
        }

        /// <summary>
        /// 執行流程
        /// </summary>
        private static void ExecuteProcess()
        {
            const string pattern = @".+\\";
            var listFiles = GetFolderName(ServiceFilePath);
            if (listFiles.Count > 0)
            {
                foreach (var folderName in listFiles)
                {
                    var sFilePath = ServiceFilePath + @"\" + folderName; //本地路徑
                    var dFilePath = DestinationFilePath + @"\" + folderName; //目的地路徑

                    var listUnInstall =
                        Directory.GetFiles(sFilePath, "UnInstall*.bat",
                            SearchOption.AllDirectories); //尋找UnInstall開頭bat檔
                    var listInstall =
                        Directory.GetFiles(sFilePath, "Install*.bat", SearchOption.AllDirectories); //尋找Install開頭bat檔
                    if (listUnInstall.Length == 1 && listInstall.Length == 1)
                    {
                        if (!Directory.Exists(dFilePath))
                        {
                            Console.WriteLine($"建立目的地服務資料夾 {folderName}");
                            Directory.CreateDirectory(dFilePath);
                        }

                        string batFile; //bat檔案名稱(含副檔名)
                        Match match;
                        if (ExecuteMode.Equals("Complete")) //完整安裝
                        {
                            //解除安裝
                            match = Regex.Match(listUnInstall[0], pattern);
                            batFile = listUnInstall[0].Replace(match.Value, "");
                            UnInstall(dFilePath, batFile, folderName);

                            //覆寫目的地檔案
                            OverWriteFile(dFilePath, sFilePath, folderName);

                            //安裝
                            match = Regex.Match(listInstall[0], pattern);
                            batFile = listInstall[0].Replace(match.Value, "");
                            Install(dFilePath, batFile, folderName);
                        }
                        else if (ExecuteMode.Equals("UnInstall")) //僅解除安裝
                        {
                            //解除安裝
                            match = Regex.Match(listUnInstall[0], pattern);
                            batFile = listUnInstall[0].Replace(match.Value, "");
                            UnInstall(dFilePath, batFile, folderName);
                        }
                        else if (ExecuteMode.Equals("Install")) //僅安裝
                        {
                            //覆寫目的地檔案
                            OverWriteFile(dFilePath, sFilePath, folderName);

                            //安裝
                            match = Regex.Match(listInstall[0], pattern);
                            batFile = listInstall[0].Replace(match.Value, "");
                            Install(dFilePath, batFile, folderName);
                        }
                        else Console.WriteLine("錯誤資訊：請確認安裝模式是否設定正確");
                    }
                    else Console.WriteLine($"錯誤資訊：服務 {folderName}資料夾未找到批次檔或是數量不對");
                }
            }
            else Console.WriteLine("錯誤資訊：ServiceFiles資料夾尚未放入任何服務");
        }

        /// <summary>
        /// 安裝服務
        /// </summary>
        /// <param name="dFilePath">目的地路徑</param>
        /// <param name="fileName">執行檔案名稱</param>
        /// <param name="serviceName">服務名稱</param>
        private static void InstallService(string dFilePath, string fileName, string serviceName)
        {
            Console.WriteLine($"開始安裝 {serviceName}服務");

            var proc = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = dFilePath,
                FileName = fileName,
                Verb = "runas"
            };

            using (var process = new Process())
            {
                process.EnableRaisingEvents = true;
                process.StartInfo = proc;
                process.Start();
                process.Exited += (sender, e) =>
                {
                    Console.WriteLine("安裝完成");
                    AutoResetEvent.Set();
                };
            }

            AutoResetEvent.WaitOne();
        }

        /// <summary>
        /// 解除安裝服務
        /// </summary>
        /// <param name="directory">執行檔案資料夾路徑</param>
        /// <param name="fileName">執行檔案名稱</param>
        /// <param name="serviceName">服務名稱</param>
        private static void UnInstall(string directory, string fileName, string serviceName)
        {
            Console.WriteLine($"開始解除安裝 {serviceName}服務");

            var processStartInfo = new ProcessStartInfo
            {
                UseShellExecute = true, //是否使用Shell來啟動
                WorkingDirectory = directory, //取得要執行處理序的工作目錄
                FileName = fileName, //要執行的檔案名稱
                Verb = "runas" //用admin權限執行
            };

            using (var process = new Process())
            {
                process.EnableRaisingEvents = true;
                process.StartInfo = processStartInfo;
                process.Start();
                process.Exited += (sender, e) =>
                {
                    Console.WriteLine("解除安裝完成");
                    AutoResetEvent.Set(); //繼續執行緒
                };
            }

            AutoResetEvent.WaitOne(); //暫停執行緒
        }

        /// <summary>
        /// 覆寫目的地資料夾檔案
        /// </summary>
        /// <param name="dFilePath">目的地路徑</param>
        /// <param name="sFilePath">本地路徑</param>
        /// <param name="folderName">資料夾名稱</param>
        private static void OverWriteFile(string dFilePath, string sFilePath, string folderName)
        {
            try
            {
                Console.WriteLine($"開始覆寫 {folderName}資料夾");
                var listFileFiles = Directory.GetFiles(sFilePath);
                foreach (var file in listFileFiles)
                {
                    var fileName = Path.GetFileName(file);
                    var destFile = Path.Combine(dFilePath, fileName);
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
            var listFilePath = Directory.GetDirectories(filePath);
            var listFile = new List<string>();
            if (listFilePath.Length > 0)
            {
                foreach (var fileName in listFilePath)
                {
                    listFile.Add(Path.GetFileNameWithoutExtension(fileName));
                }
            }

            return listFile;
        }
    }
}