pwsh ./build-debug-x86.ps1
SET PLUGIN_DIR=C:\Program Files (x86)\Notepad++\plugins\AnotherMarkdown

xcopy AnotherMarkdown\bin\Debug\* "%PLUGIN_DIR%\lib" /c /s /r /d /y /i
xcopy "%PLUGIN_DIR%\lib\AnotherMarkdown.*" "%PLUGIN_DIR%\" /c /s /r /d /y /i
del "%PLUGIN_DIR%\lib\AnotherMarkdown.*"
