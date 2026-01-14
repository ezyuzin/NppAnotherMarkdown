pwsh ./build-debug-x86.ps1

xcopy AnotherMarkdown\bin\Debug\AnotherMarkdown.* "C:\Program Files (x86)\Notepad++\plugins\AnotherMarkdown" /c /s /r /d /y /i
xcopy Webview2Viewer\bin\Debug\*.* "c:\Program Files (x86)\Notepad++\plugins\AnotherMarkdown\lib" /c /s /r /d /y /i
xcopy PanelCommon\bin\Debug\*.* "c:\Program Files\Notepad++\plugins\AnotherMarkdown\lib" /c /s /r /d /y /i