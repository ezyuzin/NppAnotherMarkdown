if (Test-Path Release\lib\) {Remove-Item Release\lib\ -Recurse -Force}
New-Item "Release\lib\" -itemType Directory
Copy-Item -Force -Recurse -Path PanelCommon\bin\Release\*.dll -Destination Release\lib\
Copy-Item -Force -Recurse -Path Webview2Viewer\bin\Release\*.dll -Destination Release\lib\
Copy-Item -Force -Recurse -Path Webview2Viewer\bin\Release\runtimes\ -Destination Release\lib\runtimes\

function makeReleaseZip($filename, $targetPlattform)
{
	$zipName = "Release\AnotherMarkdown-" + (Get-Item $filename).VersionInfo.FileVersion + "-" + $targetPlattform + ".zip"
	Compress-Archive `
    -Force `
    -DestinationPath $zipName `
    -LiteralPath $filename, 'Release\lib\', 'README.md', 'help\', 'assets\', 'License.txt' 
}

makeReleaseZip "AnotherMarkdown\bin\Release\AnotherMarkdown.dll" "x86"
makeReleaseZip "AnotherMarkdown\bin\Release-x64\AnotherMarkdown.dll" "x64"
pause