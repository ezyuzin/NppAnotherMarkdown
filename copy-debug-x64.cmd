@REM pwsh ./build-debug-x64.ps1

SET PLUGIN_DIR=C:\Program Files\Notepad++\plugins\AnotherMarkdown

xcopy AnotherMarkdown\bin\Debug-x64\* "%PLUGIN_DIR%\lib" /c /s /r /d /y /i
xcopy "%PLUGIN_DIR%\lib\AnotherMarkdown.*" "%PLUGIN_DIR%\" /c /s /r /d /y /i
del "%PLUGIN_DIR%\lib\AnotherMarkdown.*"
PAUSE