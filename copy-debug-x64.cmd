@REM pwsh ./build-debug.ps1

xcopy AnotherMarkdown\bin\Debug-x64\*.* "C:\Program Files\Notepad++\plugins\AnotherMarkdown" /c /s /r /d /y /i
xcopy Webview2Viewer\bin\Debug\*.* "c:\Program Files\Notepad++\plugins\AnotherMarkdown\lib" /c /s /r /d /y /i
xcopy PanelCommon\bin\Debug\*.* "c:\Program Files\Notepad++\plugins\AnotherMarkdown\lib" /c /s /r /d /y /i
