# UpdateService 一鍵式服務安裝
## 環境要求

- Windows作業系統。

## 教學流程

1. 建置專案。
2. 複製`bin/Debug` or `bin/Release`資料夾底下檔案。
3. 於`ServiceFiles`資料夾底下新增服務名稱資料夾。
4. 貼上複製好的檔案。
5. 從Bat資料夾複製`Install.bat`和`UnInstall.bat`檔案並放到剛剛建立好的服務資料夾。
6. 將`.bat`檔案名稱改成`.txt`或者是點選編輯(後續記得要將檔案名稱改為`.bat`)。
7. 將`@serviceName`文字全部替換成服務名稱。
8. 開啟`UpdateService.exe.config`，修改`DestinationFilePath`參數內容為目的地路徑。
9. 執行`UpdateService.exe`。

## 補充說明

 1. `UpdateService.exe.config`參數

     1. `ExecuteMode`：安裝模式。
         1. `Complete`：完整安裝，會先解除再重新安裝，如果電腦已經有之前安裝好的服務推薦使用。
         2. `UnInstall`：僅解除安裝服務。
         3. `Install`：僅安裝服務，推薦首次安裝使用。

    1. `DestinationFilePath`：服務目的地路徑。

### 安裝批次檔

- 以下為範例，專案底下也有提供現成檔案，請將`@serviceName`改成服務名稱。
- SC那行設定是當服務遇到例外時，設定0秒重新啟動。

```cmd
@ECHO OFF

net session >nul 2>&1
IF NOT %ERRORLEVEL% EQU 0 (
   ECHO ERROR: Please run Bat as Administrator.
   PAUSE
   EXIT /B 1
)

@SETLOCAL enableextensions
@CD /d "%~dp0"

REM The following directory is for .NET 4
SET DOTNETFX4=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319
SET PATH=%PATH%;%DOTNETFX4%

ECHO Start to Install @serviceName
ECHO ---------------------------------------------------
InstallUtil /i .\@serviceName.exe
ECHO ---------------------------------------------------

ECHO Start @serviceName
ECHO ---------------------------------------------------
net start @serviceName
ECHO ---------------------------------------------------

SC failure @serviceName actions= restart/0/restart/0/restart/0 reset= 0

ECHO Done.
```

### 解除安裝批次檔

- 以下為範例，專案底下也有提供現成檔案，請將`@serviceName`改成服務名稱。

```cmd
@ECHO OFF

net session >nul 2>&1
IF NOT %ERRORLEVEL% EQU 0 (
   ECHO ERROR: Please run Bat as Administrator.
   PAUSE
   EXIT /B 1
)

@SETLOCAL enableextensions
@CD /d "%~dp0"

REM The following directory is for .NET 4
SET DOTNETFX4=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319
SET PATH=%PATH%;%DOTNETFX4%

ECHO Stopping @serviceName
ECHO ---------------------------------------------------
net stop @serviceName
ECHO ---------------------------------------------------

ECHO UnInstalling @serviceName
ECHO ---------------------------------------------------
InstallUtil /u .\@serviceName.exe
ECHO ---------------------------------------------------
ECHO Done.
```

