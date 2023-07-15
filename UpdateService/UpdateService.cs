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

        public static readonly string DestinationFilePath = WebConfigurationManager.AppSettings["DestinationFilePath"].ToString();//服務安裝位置
        public static readonly string ServiceFilePath = AppDomain.CurrentDomain.SetupInformation.ApplicationBase + @"\ServiceFiles";//服務本地位置
        public static readonly string ExecuteMode = WebConfigurationManager.AppSettings["ExecuteMode"].ToString();//服務安裝模式
        public static List<string> FList = GetFolderName(ServiceFilePath);

        public static void Main(string[] args)
        {
            try
            {
                //目的地無資料夾先建立
                if (!Directory.Exists(DestinationFilePath))
                {
                    Console.WriteLine("建立目的地資料夾");
                    Directory.CreateDirectory(DestinationFilePath);
                }

                foreach (string Name in FList)
                {
                    string Folder = DestinationFilePath + @"\" + Name;
                    if (!Directory.Exists(Folder))
                    {
                        Console.WriteLine("建立目的地服務資料夾");
                        Directory.CreateDirectory(Folder);
                    }
                }

                ExecuteProcess(AppDomain.CurrentDomain.SetupInformation.ApplicationBase);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            Console.ReadKey();
        }

        /// <summary>
        /// 執行流程
        /// </summary>
        static void ExecuteProcess(string FilePath)
        {
            string Pattern = @".+\\";
            string[] UFileArr = Directory.GetFiles(FilePath, "UnInstall_*.bat", SearchOption.AllDirectories);//尋找所有UnInstall_開頭bat檔
            string[] IFileArr = Directory.GetFiles(FilePath, "Install_*.bat", SearchOption.AllDirectories);//尋找所有Install_開頭bat檔
            if (ExecuteMode.Equals("Complete"))
            {
                if (UFileArr.Length != 0)
                {
                    //解除安裝
                    foreach (string BatPath in UFileArr)
                    {
                        Match m = Regex.Match(BatPath, Pattern);
                        string BatDirectory = m.Value;//UnInstall...bat資料夾路徑
                        string BatFile = BatPath.Replace(m.Value, "");//BAT檔案名稱(含副檔名)
                        UnInstall(BatDirectory, BatFile);
                    }

                    //覆寫檔案
                    foreach (string Name in FList)
                    {
                        string DFilePath = DestinationFilePath + @"\" + Name;//目的地路徑
                        string OFilePath = ServiceFilePath + @"\" + Name;//本地路徑
                        OverWriteFile(DFilePath, OFilePath, Name);
                    }

                    //安裝
                    int i = 0;
                    foreach (string BatPath in IFileArr)
                    {
                        string DFilePath = DestinationFilePath + @"\" + FList[i];//目的地路徑
                        Match m = Regex.Match(BatPath, Pattern);
                        string BatFile = BatPath.Replace(m.Value, "");//BAT檔案名稱(含副檔名)
                        i++;
                        Install(DFilePath, BatFile);
                    }
                }
                else Console.WriteLine("未找到任何開頭UnInstall...檔案");
            }
            else if (ExecuteMode.Equals("UnInstall"))
            {
                if (UFileArr.Length != 0)
                {
                    //解除安裝
                    foreach (string BatPath in UFileArr)
                    {
                        Match m = Regex.Match(BatPath, Pattern);
                        string BatDirectory = m.Value;//UnInstall...bat資料夾路徑
                        string BatFile = BatPath.Replace(m.Value, "");//BAT檔案名稱(含副檔名)
                        UnInstall(BatDirectory, BatFile);
                    }
                }
                else Console.WriteLine("未找到任何開頭UnInstall...檔案");
            }
            else if (ExecuteMode.Equals("Install"))
            {
                if (IFileArr.Length != 0)
                {
                    //覆寫檔案
                    foreach (string Name in FList)
                    {
                        string DFilePath = DestinationFilePath + @"\" + Name;//目的地路徑
                        string OFilePath = ServiceFilePath + @"\" + Name;//本地路徑
                        OverWriteFile(DFilePath, OFilePath, Name);
                    }

                    //安裝
                    int i = 0;
                    foreach (string BatPath in IFileArr)
                    {
                        string DFilePath = DestinationFilePath + @"\" + FList[i];//目的地路徑
                        Match m = Regex.Match(BatPath, Pattern);
                        string BatFile = BatPath.Replace(m.Value, "");//BAT檔案名稱(含副檔名)
                        i++;
                        Install(DFilePath, BatFile);
                    }
                }
                else Console.WriteLine("未找到任何開頭Install...檔案");
            }
        }

        /// <summary>
        /// 解除安裝服務
        /// </summary>
        /// <param name="Directory">執行檔案資料夾路徑</param>
        /// <param name="FileName">執行檔案名稱</param>
        static void UnInstall(string Directory, string FileName)
        {
            Console.WriteLine($"解除安裝 {FileName} 服務");

            //解除安裝
            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = Directory;
            proc.FileName = FileName;
            proc.Verb = "runas";

            Process p = new Process();
            p.EnableRaisingEvents = true;
            p.StartInfo = proc;
            p.Start();
            p.Exited += (sender, e) =>
            {
                Console.WriteLine("解除安裝完成");
                autoResetEvent.Set();
            };
            autoResetEvent.WaitOne();
            //Thread.Sleep(5 * 1000);
        }

        /// <summary>
        /// 覆寫目的地資料夾檔案
        /// </summary>
        /// <param name="DFilePath">目的地路徑</param>
        /// <param name="OFilePath">本地路徑</param>
        /// <param name="FolderName">資料夾名稱</param>
        static void OverWriteFile(string DFilePath, string OFilePath, string FolderName)
        {
            Console.WriteLine($"開始覆寫 {FolderName} 資料夾");
            string[] DFileFiles = Directory.GetFiles(OFilePath);
            foreach (string sourcefile in DFileFiles)
            {
                string FileName = Path.GetFileName(sourcefile);
                string DestFile = Path.Combine(DFilePath, FileName);
                if (File.Exists(DestFile)) File.Delete(DestFile);
                File.Copy(sourcefile, DestFile);
            }
            //Thread.Sleep(5 * 1000);
            Console.WriteLine("覆寫完成");
        }

        /// <summary>
        /// 安裝服務
        /// </summary>
        /// <param name="DFilePath">目的地路徑</param>
        /// <param name="FileName">執行檔案名稱</param>
        static void Install(string DFilePath, string FileName)
        {
            Console.WriteLine($"安裝 {FileName} 服務");

            ProcessStartInfo proc = new ProcessStartInfo();
            proc.UseShellExecute = true;
            proc.WorkingDirectory = DFilePath;
            proc.FileName = FileName;
            proc.Verb = "runas";

            Process p = new Process();
            p.EnableRaisingEvents = true;
            p.StartInfo = proc;
            p.Start();
            p.Exited += (sender, e) =>
            {
                Console.WriteLine("安裝完成");
                autoResetEvent.Set();
            };
            autoResetEvent.WaitOne();
            //Thread.Sleep(5 * 1000);
        }

        /// <summary>
        /// 取得指定路徑下所有資料夾名稱
        /// </summary>
        public static List<string> GetFolderName(string FilePath)
        {
            string[] FileArr = Directory.GetDirectories(FilePath);
            List<string> FList = new List<string>();
            foreach (string Name in FileArr)
            {
                FList.Add(Path.GetFileNameWithoutExtension(Name));
            }
            return FList;
        }
    }
}
