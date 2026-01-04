pwsh ./build-release.ps1

xcopy AnotherMarkdown\bin\Release-x64\AnotherMarkdown.* "C:\Program Files\Notepad++\plugins\AnotherMarkdown" /c /s /r /d /y /i
xcopy Webview2Viewer\bin\Release\*.* "c:\Program Files\Notepad++\plugins\AnotherMarkdown\lib" /c /s /r /d /y /i
xcopy PanelCommon\bin\Release\*.* "c:\Program Files\Notepad++\plugins\AnotherMarkdown\lib" /c /s /r /d /y /i
