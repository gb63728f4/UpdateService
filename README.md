# UpdateService（WPF 版服務更新工具）

此專案已改寫為 .NET 8 WPF 桌面工具，用來批次執行服務檔案覆寫、安裝與解除安裝。

## 環境需求

- Windows 作業系統
- .NET 8 SDK（開發/建置）或對應 Runtime（執行）
- 系統管理員權限（執行安裝/解除安裝批次檔時）

## 專案結構

- `UpdateService/`：WPF 主程式
- `UpdateService/appsettings.json`：預設設定檔
- `Bat/`：批次檔範本（Install / UnInstall）
- `ServiceFiles/`：執行時讀取的服務來源資料夾（位於執行檔同層）

## 使用流程

1. 建置專案
   - `dotnet build UpdateService.sln`
2. 進入輸出目錄（例如 `UpdateService/bin/Debug/net8.0-windows/`）。
3. 在執行檔同層建立 `ServiceFiles` 資料夾。
4. 在 `ServiceFiles` 底下，為每個服務建立一個子資料夾，並放入：
   - 服務程式檔案（exe、dll、設定檔等）
   - `Install*.bat`（安裝批次檔，需 1 個）
   - `UnInstall*.bat`（解除安裝批次檔，需 1 個）
5. 啟動 `UpdateService.exe`：
   - 輸入或瀏覽「目的地安裝路徑」
   - 選擇安裝模式
   - 按下「執行」
6. 於「執行記錄」查看每個服務的處理結果。

## appsettings.json

程式啟動時會讀取 `appsettings.json` 作為預設值（可在 UI 內再修改）：

```json
{
  "DestinationFilePath": "C:\\Program Files (x86)\\Service",
  "ExecuteMode": "Complete"
}
```

- `DestinationFilePath`：目的地服務根路徑
- `ExecuteMode`：執行模式（`Complete` / `Install` / `UnInstall`）

## 執行模式說明

- `Complete`：先解除安裝，再覆寫檔案，最後安裝
- `Install`：覆寫檔案後安裝
- `UnInstall`：僅解除安裝

## 注意事項

- 每個服務資料夾必須同時存在且僅存在 1 個 `Install*.bat` 與 1 個 `UnInstall*.bat`，否則會在記錄中顯示錯誤。
- 建議以「系統管理員身分」執行批次檔流程，避免權限不足導致安裝失敗。
