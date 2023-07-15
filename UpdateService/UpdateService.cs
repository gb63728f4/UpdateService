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
    class UpdateService
    {
        public static AutoResetEvent autoResetEvent = new AutoResetEvent(false);

        public static readonly string destinationFilePath = WebConfigurationManager.AppSettings["DestinationFilePath"].ToString();//服務安裝位置
        public static readonly string serviceFilePath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"\ServiceFiles";//服務本地位置
        public static readonly string executeMode = WebConfigurationManager.AppSettings["ExecuteMode"].ToString();//服務安裝模式

        public static void Main(string[] args)
        {
            try
            {
                //建立ServiceFiles資料夾
                if (!Directory.Exists(serviceFilePath)) Directory.CreateDirectory(serviceFilePath);
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
        static void ExecuteProcess()
        {
            Match match;
            string pattern = @".+\\";
            List<string> list_Files = GetFolderName(serviceFilePath);
            if (list_Files.Count > 0)
            {
                foreach (string folderName in list_Files)
                {
                    string sFilePath = serviceFilePath + @"\" + folderName;//本地路徑
                    string dFilePath = destinationFilePath + @"\" + folderName;//目的地路徑

                    string[] list_UnInstall = Directory.GetFiles(sFilePath, "UnInstall*.bat", SearchOption.AllDirectories); //尋找UnInstall開頭bat檔
                    string[] list_Install = Directory.GetFiles(sFilePath, "Install*.bat", SearchOption.AllDirectories);//尋找Install開頭bat檔
                    if (list_UnInstall.Length == 1 && list_Install.Length == 1)
                    {
                        if (!Directory.Exists(dFilePath))
                        {
                            Console.WriteLine($"建立目的地服務資料夾 {folderName}");
                            Directory.CreateDirectory(dFilePath);
                        }

                        string batFile;//bat檔案名稱(含副檔名)
                        if (executeMode.Equals("Complete"))//完整安裝
                        {
                            //解除安裝
                            match = Regex.Match(list_UnInstall[0], pattern);
                            batFile = list_UnInstall[0].Replace(match.Value, "");
                            UnInstall(dFilePath, batFile, folderName);

                            //覆寫目的地檔案
                            OverWriteFile(dFilePath, sFilePath, folderName);

                            //安裝
                            match = Regex.Match(list_Install[0], pattern);
                            batFile = list_Install[0].Replace(match.Value, "");
                            Install(dFilePath, batFile, folderName);
                        }
                        else if (executeMode.Equals("UnInstall"))//僅解除安裝
                        {
                            //解除安裝
                            match = Regex.Match(list_UnInstall[0], pattern);
                            batFile = list_UnInstall[0].Replace(match.Value, "");
                            UnInstall(dFilePath, batFile, folderName);
                        }
                        else if (executeMode.Equals("Install"))//僅安裝
                        {
                            //覆寫目的地檔案
                            OverWriteFile(dFilePath, sFilePath, folderName);

                            //安裝
                            match = Regex.Match(list_Install[0], pattern);
                            batFile = list_Install[0].Replace(match.Value, "");
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
        /// 解除安裝服務
        /// </summary>
        /// <param name="directory">執行檔案資料夾路徑</param>
        /// <param name="fileName">執行檔案名稱</param>
        /// <param name="serviceName">服務名稱</param>
        private static void UnInstall(string directory, string fileName, string serviceName)
        {
            Console.WriteLine($"開始解除安裝 {serviceName}服務");

            ProcessStartInfo processStartInfo = new ProcessStartInfo
            {
                UseShellExecute = true,//是否使用Shell來啟動
                WorkingDirectory = directory,//取得要執行處理序的工作目錄
                FileName = fileName,//要執行的檔案名稱
                Verb = "runas"//用admin權限執行
            };

            Process process = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = processStartInfo
            };
            process.Start();
            process.Exited += (sender, e) =>
            {
                Console.WriteLine("解除安裝完成");
                autoResetEvent.Set();//繼續執行緒
            };
            autoResetEvent.WaitOne();//暫停執行緒
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
                string[] list_FileFiles = Directory.GetFiles(sFilePath);
                foreach (string sourcefile in list_FileFiles)
                {
                    string fileName = Path.GetFileName(sourcefile);
                    string destFile = Path.Combine(dFilePath, fileName);
                    if (File.Exists(destFile)) File.Delete(destFile);
                    File.Copy(sourcefile, destFile);
                }
                Console.WriteLine("覆寫完成");
            }
            catch (UnauthorizedAccessException)
            {
                autoResetEvent.WaitOne();//暫停執行緒
                Console.WriteLine($"錯誤資訊：覆寫 {folderName}資料夾權限不足");
                throw;
            }
            catch (DirectoryNotFoundException e)
            {
                autoResetEvent.WaitOne();//暫停執行緒
                Console.WriteLine($"錯誤資訊：{e.Message}檔案遺失");
                throw;
            }
        }

        /// <summary>
        /// 安裝服務
        /// </summary>
        /// <param name="dFilePath">目的地路徑</param>
        /// <param name="fileName">執行檔案名稱</param>
        /// <param name="serviceName">服務名稱</param>
        private static void Install(string dFilePath, string fileName, string serviceName)
        {
            Console.WriteLine($"開始安裝 {serviceName}服務");

            ProcessStartInfo proc = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = dFilePath,
                FileName = fileName,
                Verb = "runas"
            };

            Process p = new Process
            {
                EnableRaisingEvents = true,
                StartInfo = proc
            };
            p.Start();
            p.Exited += (sender, e) =>
            {
                Console.WriteLine("安裝完成");
                autoResetEvent.Set();
            };
            autoResetEvent.WaitOne();
        }

        /// <summary>
        /// 取得指定路徑下所有資料夾名稱
        /// </summary>
        public static List<string> GetFolderName(string filePath)
        {
            string[] list_FilePath = Directory.GetDirectories(filePath);
            List<string> list_File = new List<string>();
            if (list_FilePath.Length > 0)
            {
                foreach (string fileName in list_FilePath)
                {
                    list_File.Add(Path.GetFileNameWithoutExtension(fileName));
                }
            }
            return list_File;
        }
    }
}
