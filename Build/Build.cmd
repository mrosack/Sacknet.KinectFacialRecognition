@echo on
call Clean.cmd
call "%VS120COMNTOOLS%vsvars32.bat"
mkdir lib\net45\

msbuild.exe /ToolsVersion:12.0 "..\Sacknet.KinectFacialRecognition\Sacknet.KinectFacialRecognition.csproj" /p:configuration=Release /p:platform=x86 /t:Rebuild
copy ..\Sacknet.KinectFacialRecognition\bin\x86\Release\Sacknet.KinectFacialRecognition.* lib\net45
copy ..\README.md README.txt
..\.nuget\NuGet.exe pack Sacknet.KinectV2FacialRecognition.nuspec -Exclude *.cmd

rmdir lib /s /q
mkdir lib\net45\

msbuild.exe /ToolsVersion:12.0 "..\Sacknet.KinectFacialRecognition\Sacknet.KinectFacialRecognition.csproj" /p:configuration=Release /p:platform=x64 /t:Rebuild
copy ..\Sacknet.KinectFacialRecognition\bin\x64\Release\Sacknet.KinectFacialRecognition.* lib\net45
copy ..\README.md README.txt
..\.nuget\NuGet.exe pack Sacknet.KinectV2FacialRecognition.x64.nuspec -Exclude *.cmd
pause