call git submodule update --init --recursive
call msbuild.exe PowerToys.sln /t:Build /p:Configuration=Release /p:Platform=x64
call nuget restore .\tools\BugReportTool\BugReportTool.sln
call msbuild -p:Platform=x64 -p:Configuration=Release .\tools\BugReportTool\BugReportTool.sln

call nuget restore .\tools\WebcamReportTool\WebcamReportTool.sln
call msbuild -p:Platform=x64 -p:Configuration=Release .\tools\WebcamReportTool\WebcamReportTool.sln

call nuget restore .\tools\StylesReportTool\StylesReportTool.sln
call msbuild -p:Platform=x64 -p:Configuration=Release .\tools\StylesReportTool\StylesReportTool.sln

cd installer
call msbuild.exe PowerToysSetup.sln /t:Build /p:Configuration=Release /p:Platform=x64
cd ..

