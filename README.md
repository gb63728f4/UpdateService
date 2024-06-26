# UpdateService 自動安裝服務
一鍵自動化安裝服務流程

# 自動安裝服務使用教學

### 基本說明

 1. 請將需要安裝/更新的服務從 **bin/Debug** or **bin/Release**資料夾下全部複製
 2. 於資料夾ServiceFiles底下新增要安裝服務名稱的資料夾並放入檔案
 3. 放入**Install.bat**和**UnInstall.bat**檔案(bat資料夾有提供範例)
 4. 修改上述兩個檔案，取代**@serviceName**並改成服務名稱
 5. 依據安裝模式更新**UpdateService.exe.config**的**ExecuteMode**參數
    1. **Complete**:完整安裝，會先解除再重新安裝，推薦更新服務使用
    2. **UnInstall**:僅解除安裝
    3. **Install**:僅安裝，推薦首次安裝服務時使用
 6. 執行**UpdateService.exe**
 7. 其他參數說明
    1. **DestinationFilePath**:服務安裝目的地路徑
    2. **ExecuteMode**:安裝模式，內容詳見第5點

### 安裝批次檔

> 以下為範例，專案底下也有提供現成檔案，請將@serviceName改成服務名稱
>
> SC那行設定是當服務遇到例外時，設定0秒重新啟動

```
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

> 以下為範例，專案底下也有提供現成檔案，請將@serviceName改成服務名稱

```
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

