@REM pwsh ./build-debug.ps1

xcopy AnotherMarkdown\bin\Debug\*.* "C:\Program Files (x86)\Notepad++\plugins\AnotherMarkdown" /c /s /r /d /y /i
xcopy Webview2Viewer\bin\Debug\*.* "c:\Program Files (x86)\Notepad++\plugins\AnotherMarkdown\lib" /c /s /r /d /y /i