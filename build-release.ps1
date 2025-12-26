$msbuild = "C:\Program Files\Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\MSBuild.exe"
& $msbuild AnotherMarkdown.sln /target:Clean /target:Build /p:Configuration=Release /p:Platform=x86
& $msbuild AnotherMarkdown.sln /target:Clean /target:Build /p:Configuration=Release /p:Platform=x64