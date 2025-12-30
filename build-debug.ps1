$msbuildPaths = @(
"C:\Program Files\Microsoft Visual Studio\2025\Professional\MSBuild\Current\Bin\MSBuild.exe",
"C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe",
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe",
"C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe" 
)

foreach($msbuild in $msbuildPaths) {
  if (test-path $msbuild) {
    break;
  }
}

& $msbuild AnotherMarkdown.sln /target:Clean /target:Build /p:Configuration=Debug /p:Platform=x86
& $msbuild AnotherMarkdown.sln /target:Clean /target:Build /p:Configuration=Debug /p:Platform=x64