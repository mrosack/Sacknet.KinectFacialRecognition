@echo on
call Clean.cmd
call "%VS100COMNTOOLS%vsvars32.bat"
mkdir lib\net40\

msbuild.exe /ToolsVersion:4.0 "..\Sacknet.KinectFacialRecognition\Sacknet.KinectFacialRecognition.csproj" /p:configuration=Release
copy ..\Sacknet.KinectFacialRecognition\bin\Release\Sacknet.KinectFacialRecognition.* lib\net40
NuGet pack Sacknet.KinectFacialRecognition.nuspec
pause