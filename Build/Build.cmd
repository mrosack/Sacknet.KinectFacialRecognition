@echo on
call Clean.cmd
call "%VS100COMNTOOLS%vsvars32.bat"
mkdir lib\net40\
mkdir content

msbuild.exe /ToolsVersion:4.0 "..\Sacknet.KinectFacialRecognition\Sacknet.KinectFacialRecognition.csproj" /p:configuration=Release /t:Rebuild
copy ..\Sacknet.KinectFacialRecognition\bin\Release\Sacknet.KinectFacialRecognition.* lib\net40
copy ..\README.md README.txt
NuGet pack Sacknet.KinectFacialRecognition.nuspec -Exclude *.cmd
pause