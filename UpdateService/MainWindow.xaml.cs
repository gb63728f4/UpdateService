using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Forms;

namespace UpdateService
{
    public partial class MainWindow : Window
    {
        private static readonly string AppSettingsPath =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        public MainWindow()
        {
            InitializeComponent();
            LoadSettings();
            UpdateServiceFilesHint();
        }

        /// <summary>
        /// 從 appsettings.json 載入預設設定值
        /// </summary>
        private void LoadSettings()
        {
            if (File.Exists(AppSettingsPath))
            {
                try
                {
                    var json = File.ReadAllText(AppSettingsPath);
                    using var doc = JsonDocument.Parse(json);

                    if (doc.RootElement.TryGetProperty("DestinationFilePath", out var dest))
                        TxtDestinationFilePath.Text = dest.GetString() ?? string.Empty;

                    if (doc.RootElement.TryGetProperty("ExecuteMode", out var mode))
                    {
                        var modeStr = mode.GetString() ?? "Complete";
                        foreach (System.Windows.Controls.ComboBoxItem item in CmbExecuteMode.Items)
                        {
                            if ((string)item.Tag == modeStr)
                            {
                                CmbExecuteMode.SelectedItem = item;
                                break;
                            }
                        }
                    }
                }
                catch (JsonException)
                {
                    // 設定檔格式有誤，使用預設值
                }
                catch (IOException)
                {
                    // 無法讀取設定檔，使用預設值
                }
            }

            if (CmbExecuteMode.SelectedItem == null)
                CmbExecuteMode.SelectedIndex = 0;
        }

        /// <summary>
        /// 更新 ServiceFiles 路徑提示文字
        /// </summary>
        private void UpdateServiceFilesHint()
        {
            var serviceFilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ServiceFiles");
            TxtServiceFilesHint.Text = $"服務來源資料夾：{serviceFilesPath}";
        }

        /// <summary>
        /// 瀏覽目的地資料夾
        /// </summary>
        private void BtnBrowse_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                Description = "選擇目的地安裝路徑",
                SelectedPath = TxtDestinationFilePath.Text
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                TxtDestinationFilePath.Text = dialog.SelectedPath;
        }

        /// <summary>
        /// 執行服務更新
        /// </summary>
        private async void BtnExecute_Click(object sender, RoutedEventArgs e)
        {
            var destinationFilePath = TxtDestinationFilePath.Text.Trim();
            if (string.IsNullOrEmpty(destinationFilePath))
            {
                System.Windows.MessageBox.Show("請輸入目的地安裝路徑。", "提示",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CmbExecuteMode.SelectedItem is not System.Windows.Controls.ComboBoxItem selectedItem)
                return;

            var modeStr = (string)selectedItem.Tag;
            if (!Enum.TryParse<ExecuteModeEnum>(modeStr, out var executeMode))
            {
                Log("安裝模式設定有誤");
                return;
            }

            BtnExecute.IsEnabled = false;
            TxtLog.Clear();

            try
            {
                var service = new UpdateServiceLogic(destinationFilePath, executeMode, Log);
                await service.ExecuteAsync();
                Log("─── 所有操作已完成 ───");
            }
            catch (Exception ex)
            {
                Log($"錯誤資訊：{ex.Message}");
            }
            finally
            {
                BtnExecute.IsEnabled = true;
            }
        }

        /// <summary>
        /// 清除執行記錄
        /// </summary>
        private void BtnClearLog_Click(object sender, RoutedEventArgs e)
        {
            TxtLog.Clear();
        }

        /// <summary>
        /// 將訊息附加至執行記錄（執行緒安全）
        /// </summary>
        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                TxtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
                TxtLog.ScrollToEnd();
            });
        }
    }
}
